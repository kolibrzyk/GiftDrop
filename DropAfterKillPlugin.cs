using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace DropAfterKillPlugin
{
    public class DropAfterKillPlugin : BasePlugin
    {
        public override string ModuleName => "Drop After Kill (DB)";
        public override string ModuleVersion => "1.0.0";
        public override string ModuleAuthor => "YourName";

        private Random rng = new();
        private PluginConfig config;

        private MySqlConnection dbConnection;

        public override void Load(bool hotReload)
        {
            LoadConfig();
            ConnectDatabase();
            RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        }

        private void LoadConfig()
        {
            string path = Path.Combine(PluginPath, "config.json");
            if (!File.Exists(path))
            {
                PrintToConsole("[DropAfterKill] config.json not found, creating default...");
                config = PluginConfig.Default();
                File.WriteAllText(path, JsonConvert.SerializeObject(config, Formatting.Indented));
                return;
            }

            config = JsonConvert.DeserializeObject<PluginConfig>(File.ReadAllText(path)) ?? PluginConfig.Default();
        }

        private void ConnectDatabase()
        {
            try
            {
                string connStr = $"Server={config.DBHost};Database={config.DBName};Uid={config.DBUser};Pwd={config.DBPass};";
                dbConnection = new MySqlConnection(connStr);
                dbConnection.Open();
                PrintToConsole("[DropAfterKill] Connected to MySQL database.");
                CreateTablesIfNotExist();
            }
            catch (Exception ex)
            {
                PrintToConsole($"[DropAfterKill] Database connection failed: {ex.Message}");
                dbConnection = null;
            }
        }

        private void CreateTablesIfNotExist()
        {
            if (dbConnection == null) return;

            string query = @"
            CREATE TABLE IF NOT EXISTS PlayerData (
                SteamID VARCHAR(20) PRIMARY KEY,
                Credits INT DEFAULT 0,
                XP INT DEFAULT 0,
                VIPExpire DATETIME DEFAULT NULL
            );";

            MySqlCommand cmd = new MySqlCommand(query, dbConnection);
            cmd.ExecuteNonQuery();
        }

        private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            if (@event.Attacker == null || !@event.Attacker.IsValid || @event.Attacker.Team == CsTeam.Spectator)
                return HookResult.Continue;

            var attacker = @event.Attacker;

            int roll = rng.Next(0, 100);
            if (roll >= config.DropChancePercent) return HookResult.Continue;

            if (config.Rewards.Count == 0) return HookResult.Continue;

            var reward = config.Rewards[rng.Next(config.Rewards.Count)];
            ApplyReward(attacker, reward);

            return HookResult.Continue;
        }

        private void ApplyReward(CCSPlayerController player, Reward reward)
        {
            // Načti/ulož hráče z DB
            string steamID = player.SteamID.ToString();
            int currentCredits = 0;
            int currentXP = 0;
            DateTime? vipExpire = null;

            if (dbConnection != null)
            {
                string selectQuery = "SELECT Credits, XP, VIPExpire FROM PlayerData WHERE SteamID=@steamID;";
                MySqlCommand cmd = new MySqlCommand(selectQuery, dbConnection);
                cmd.Parameters.AddWithValue("@steamID", steamID);
                var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    currentCredits = reader.GetInt32("Credits");
                    currentXP = reader.GetInt32("XP");
                    vipExpire = reader.IsDBNull(reader.GetOrdinal("VIPExpire")) ? null : reader.GetDateTime("VIPExpire");
                }
                reader.Close();
            }

            switch (reward.Type.ToLower())
            {
                case "cash":
                    player.InGameMoneyServices.Account += reward.Amount;
                    player.PrintToChat($"[Drop System] You received: ${reward.Amount}!");
                    break;

                case "hp":
                    if (player.PlayerPawn.Value != null)
                    {
                        player.PlayerPawn.Value.Health += reward.Amount;
                        player.PrintToChat($"[Drop System] You received: +{reward.Amount} HP!");
                    }
                    break;

                case "armor":
                    if (reward.Kevlar) player.GiveNamedItem("item_kevlar");
                    if (reward.Helmet) player.GiveNamedItem("item_assaultsuit");
                    player.PrintToChat("[Drop System] You received: Armor!");
                    break;

                case "grenade":
                    if (reward.List != null && reward.List.Count > 0)
                    {
                        string grenade = reward.List[rng.Next(reward.List.Count)];
                        player.GiveNamedItem(grenade);
                        player.PrintToChat($"[Drop System] You received: {grenade}!");
                    }
                    break;

                case "speed":
                    player.ApplySpeedBoost(reward.Multiplier, reward.Duration);
                    player.PrintToChat($"[Drop System] You received: Speed x{reward.Multiplier} for {reward.Duration}s!");
                    break;

                case "bhop":
                    player.ApplyBunnyhop(reward.Duration);
                    player.PrintToChat($"[Drop System] You received: Bunnyhop for {reward.Duration}s!");
                    break;

                case "vip":
                    int vipMinutes = rng.Next(reward.MinMinutes, reward.MaxMinutes + 1);
                    vipExpire = (vipExpire ?? DateTime.Now).AddMinutes(vipMinutes);
                    player.ApplyVIP(vipMinutes);
                    player.PrintToChat($"[Drop System] You received: VIP for {vipMinutes} minutes!");
                    break;

                case "credits":
                    currentCredits += reward.Amount;
                    player.AddCredits(reward.Amount);
                    player.PrintToChat($"[Drop System] You received: {reward.Amount} credits!");
                    break;

                case "xp":
                    currentXP += reward.Amount;
                    player.AddXP(reward.Amount);
                    player.PrintToChat($"[Drop System] You received: {reward.Amount} XP!");
                    break;
            }

            // Ulož zpět do DB
            if (dbConnection != null)
            {
                string upsert = @"
                INSERT INTO PlayerData (SteamID, Credits, XP, VIPExpire) VALUES (@steamID, @credits, @xp, @vip)
                ON DUPLICATE KEY UPDATE Credits=@credits, XP=@xp, VIPExpire=@vip;";
                MySqlCommand cmd = new MySqlCommand(upsert, dbConnection);
                cmd.Parameters.AddWithValue("@steamID", steamID);
                cmd.Parameters.AddWithValue("@credits", currentCredits);
                cmd.Parameters.AddWithValue("@xp", currentXP);
                cmd.Parameters.AddWithValue("@vip", vipExpire.HasValue ? vipExpire.Value.ToString("yyyy-MM-dd HH:mm:ss") : (object)DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }
    }

    // Config classes
    public class PluginConfig
    {
        public int DropChancePercent { get; set; }
        public List<Reward> Rewards { get; set; } = new();

        // DB connection
        public string DBHost { get; set; } = "localhost";
        public string DBName { get; set; } = "cs2db";
        public string DBUser { get; set; } = "root";
        public string DBPass { get; set; } = "";

        public static PluginConfig Default() => new PluginConfig
        {
            DropChancePercent = 30,
            Rewards = new List<Reward>
            {
                new Reward { Type = "Cash", Amount = 1000 },
                new Reward { Type = "HP", Amount = 50 },
                new Reward { Type = "Armor", Kevlar = true, Helmet = true },
                new Reward { Type = "Grenade", List = new List<string>{ "weapon_hegrenade", "weapon_flashbang", "weapon_smokegrenade", "weapon_molotov" } },
                new Reward { Type = "Speed", Multiplier = 1.2f, Duration = 30 },
                new Reward { Type = "Bhop", Duration = 60 },
                new Reward { Type = "VIP", MinMinutes = 5, MaxMinutes = 1440 },
                new Reward { Type = "Credits", Amount = 50 },
                new Reward { Type = "XP", Amount = 100 }
            }
        };
    }

    public class Reward
    {
        public string Type { get; set; } = "";
        public int Amount { get; set; } = 0;
        public bool Kevlar { get; set; } = false;
        public bool Helmet { get; set; } = false;
        public List<string>? List { get; set; }
        public float Multiplier { get; set; } = 1f;
        public int Duration { get; set; } = 0;
        public int MinMinutes { get; set; } = 5;
        public int MaxMinutes { get; set; } = 1440;
    }

    // Placeholder methods — implement game logic as needed
    public static class PlayerExtensions
    {
        public static void ApplySpeedBoost(this CCSPlayerController player, float multiplier, int duration) { /* TODO */ }
        public static void ApplyBunnyhop(this CCSPlayerController player, int duration) { /* TODO */ }
        public static void ApplyVIP(this CCSPlayerController player, int minutes) { /* TODO */ }
        public static void AddCredits(this CCSPlayerController player, int amount) { /* TODO */ }
        public static void AddXP(this CCSPlayerController player, int amount) { /* TODO */ }
    }
}