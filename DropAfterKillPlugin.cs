using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;
using System;
using System.Collections.Generic;

namespace DropAfterKillPlugin
{
    public class DropAfterKillPlugin : BasePlugin
    {
        public override string ModuleName => "Drop After Kill";
        public override string ModuleVersion => "1.0.0";
        public override string ModuleAuthor => "YourName";

        // List of possible rewards
        private readonly List<string> rewards = new()
        {
            "Extra $1000",
            "50 HP Heal",
            "Kevlar + Helmet",
            "Random Grenade",
            "Double Jump for 10s"
        };

        // Random generator
        private readonly Random rng = new();

        // Drop chance (percentage)
        private const int DropChancePercent = 30; // 30% chance

        public override void Load(bool hotReload)
        {
            RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        }

        private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            if (@event.Attacker == null || @event.Attacker.SteamID == 0)
                return HookResult.Continue;

            CCSPlayerController attacker = @event.Attacker;

            if (!attacker.IsValid || attacker.Team == CsTeam.Spectator)
                return HookResult.Continue;

            // Roll for drop chance
            int roll = rng.Next(0, 100);
            if (roll < DropChancePercent)
            {
                string reward = rewards[rng.Next(rewards.Count)];

                attacker.PrintToChat($" \x07[Drop System]\x01 You received: \x04{reward}\x01!");

                // Reward handling
                switch (reward)
                {
                    case "Extra $1000":
                        attacker.InGameMoneyServices.Account += 1000;
                        break;
                    case "50 HP Heal":
                        if (attacker.PlayerPawn.Value != null)
                            attacker.PlayerPawn.Value.Health += 50;
                        break;
                    case "Kevlar + Helmet":
                        attacker.GiveNamedItem("item_kevlar");
                        attacker.GiveNamedItem("item_assaultsuit");
                        break;
                    case "Random Grenade":
                        string[] nades = { "weapon_hegrenade", "weapon_flashbang", "weapon_smokegrenade", "weapon_molotov" };
                        attacker.GiveNamedItem(nades[rng.Next(nades.Length)]);
                        break;
                    case "Double Jump for 10s":
                        // Placeholder for future ability system
                        break;
                }
            }

            return HookResult.Continue;
        }
    }
}
