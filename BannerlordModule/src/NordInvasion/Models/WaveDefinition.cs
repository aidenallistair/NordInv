using System.Collections.Generic;

namespace NordInvasion.Models
{
    public enum WaveState { Idle, Preparing, Spawning, InProgress, Completed, Failed, Camp }
    public enum WaveObjective { KillAll, DestroyRam, Escort, BurnCamps, DefendTreasury }
    public enum MutatorType
    {
        None = 0,
        Berserk = 1,          // Thor
        HiddenArchers = 2,     // Skadi
        Greedy = 3,            // Loki - gold x2 but steal
        Marked = 4,            // Odin - marked player
        ShieldWall = 5,
        CavalryRush = 6,
        Poison = 7,
        Darkness = 8,
        Fortified = 9,
        BossRush = 10,
        NoAmmo = 11,
        HeavyRain = 12
    }

    public class WaveDefinition
    {
        public int WaveNumber;
        public List<string> TroopIds = new List<string>();
        public int BossCount;
        public WaveObjective Objective;
        public MutatorType Mutator;
        public int Weather;
    }

    public class PerkDefinition
    {
        public int Id;
        public string Name;
        public string Desc;
        public string Branch; // Survivor, Berserk, Tactician
        public string Icon;
        public float HpMod = 0;
        public float DamageMod = 0;
        public float BarricadeHpMod = 0;
        public float GoldMod = 0;
        public float SpeedMod = 0;
    }

    public static class PerkDatabase
    {
        public static List<PerkDefinition> AllPerks = new List<PerkDefinition>
        {
            // Survivor branch
            new PerkDefinition { Id = 0, Name = "Iron Skin I", Desc = "+15% HP", Branch = "Survivor", HpMod = 0.15f, Icon = "iron_skin" },
            new PerkDefinition { Id = 1, Name = "Iron Skin II", Desc = "+30% HP", Branch = "Survivor", HpMod = 0.30f, Icon = "iron_skin" },
            new PerkDefinition { Id = 2, Name = "Regeneration", Desc = "Regen 2 HP/sec outside combat", Branch = "Survivor", Icon = "regen" },
            new PerkDefinition { Id = 3, Name = "Second Wind", Desc = "Second chance when fallen (once per wave)", Branch = "Survivor", Icon = "second_wind" },
            new PerkDefinition { Id = 4, Name = "Tough", Desc = "-20% damage taken", Branch = "Survivor", Icon = "tough" },

            // Berserk branch
            new PerkDefinition { Id = 10, Name = "Bloodlust", Desc = "+10% damage per 20% lost HP", Branch = "Berserk", DamageMod = 0.10f, Icon = "bloodlust" },
            new PerkDefinition { Id = 11, Name = "Vampirism", Desc = "5% lifesteal", Branch = "Berserk", Icon = "vampirism" },
            new PerkDefinition { Id = 12, Name = "Frenzy", Desc = "+20% attack speed after kill for 3 sec", Branch = "Berserk", Icon = "frenzy" },
            new PerkDefinition { Id = 13, Name = "Executioner", Desc = "+50% damage to bosses below 30% HP", Branch = "Berserk", DamageMod = 0.5f, Icon = "executioner" },

            // Tactician branch
            new PerkDefinition { Id = 20, Name = "Engineer I", Desc = "Barricades +30% HP", Branch = "Tactician", BarricadeHpMod = 0.30f, Icon = "engineer" },
            new PerkDefinition { Id = 21, Name = "Engineer II", Desc = "Barricades +50% HP, repair 2x faster", Branch = "Tactician", BarricadeHpMod = 0.50f, Icon = "engineer" },
            new PerkDefinition { Id = 22, Name = "Gold Hunter", Desc = "+20% gold for team", Branch = "Tactician", GoldMod = 0.20f, Icon = "gold" },
            new PerkDefinition { Id = 23, Name = "Banner Master", Desc = "Banner radius x2, buff +20%", Branch = "Tactician", Icon = "banner" },
            new PerkDefinition { Id = 24, Name = "Scavenger", Desc = "+50% resources from scavenging", Branch = "Tactician", Icon = "scavenger" },
        };

        public static PerkDefinition GetById(int id) => AllPerks.Find(p => p.Id == id);
        public static List<PerkDefinition> GetRandomThree(System.Random rand)
        {
            var list = new List<PerkDefinition>(AllPerks);
            var result = new List<PerkDefinition>();
            for (int i = 0; i < 3; i++)
            {
                if (list.Count == 0) break;
                int idx = rand.Next(list.Count);
                result.Add(list[idx]);
                list.RemoveAt(idx);
            }
            return result;
        }
    }

    public class VillageDefinition
    {
        public int Id;
        public string Name;
        public string Owner;
        public int DefenseLevel;
        public int X, Y;
        public int BattlesWon;
        public int BattlesLost;
    }

    public class MutatorDefinition
    {
        public MutatorType Type;
        public string Name;
        public string Desc;
        public string God;
    }

    public static class MutatorDatabase
    {
        public static List<MutatorDefinition> All = new List<MutatorDefinition>
        {
            new MutatorDefinition { Type = MutatorType.Berserk, Name = "Thor's Fury", Desc = "All nords berserk, no block, +50% speed", God = "Thor" },
            new MutatorDefinition { Type = MutatorType.HiddenArchers, Name = "Skadi's Veil", Desc = "Archers invisible in fog", God = "Skadi" },
            new MutatorDefinition { Type = MutatorType.Greedy, Name = "Loki's Greed", Desc = "Gold x2 but hit steals 5 gold", God = "Loki" },
            new MutatorDefinition { Type = MutatorType.Marked, Name = "Odin's Mark", Desc = "One player marked, all bots chase him", God = "Odin" },
            new MutatorDefinition { Type = MutatorType.ShieldWall, Name = "Shield Wall", Desc = "All nords in shieldwall squads", God = "Tyr" },
            new MutatorDefinition { Type = MutatorType.CavalryRush, Name = "Cavalry Rush", Desc = "Only cavalry", God = "Freyr" },
            new MutatorDefinition { Type = MutatorType.BossRush, Name = "Boss Rush", Desc = "3 bosses at once", God = "Jormungandr" },
        };
    }
}
