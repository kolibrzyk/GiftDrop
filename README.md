# Drop After Kill Plugin (CS2 - CounterStrikeSharp)

A Counter-Strike 2 plugin for **CounterStrikeSharp** that gives players a chance to receive **random rewards after getting a kill**.  
Each kill has a configurable chance to drop a reward, and the reward is selected randomly from a predefined list.

---

## ✨ Features
- Random chance of receiving a reward after each kill.
- Rewards are picked randomly from a list.
- Fully configurable via `config.json`.
- Built-in rewards (examples):
  - 💰 Extra cash
  - ❤️ Heal HP
  - 🛡 Kevlar + Armor
  - 💣 Random Grenades
  - 🏃 Increased Speed
  - 🌀 Bunnyhop
  - ⭐ Temporary VIP *(from 5 minutes up to 24 hours – random duration)*
  - 🎟 Credits for store/shop system
  - 📈 Level / XP for rankup system

---

## ⚙️ Installation
1. Download/clone the repository or copy `DropAfterKillPlugin.cs` into your plugin project:
2. Compile your project with **.NET 8.0** and **CounterStrikeSharp.API**.
3. Place the built `.dll` into your CS2 server’s plugin folder:
5. Restart the server or change the map.

---

## 🔧 Configuration

Example `config.json`:

```json
{
"DropChancePercent": 30,
"Rewards": [
 { "Type": "Cash", "Amount": 1000 },
 { "Type": "HP", "Amount": 50 },
 { "Type": "Armor", "Kevlar": true, "Helmet": true },
 { "Type": "Grenade", "List": ["weapon_hegrenade", "weapon_flashbang", "weapon_smokegrenade", "weapon_molotov"] },
 { "Type": "Speed", "Multiplier": 1.2, "Duration": 30 },
 { "Type": "Bhop", "Duration": 60 },
 { "Type": "VIP", "MinMinutes": 5, "MaxMinutes": 1440 }
]
}
[Drop System] You received: VIP for 2 hours!
