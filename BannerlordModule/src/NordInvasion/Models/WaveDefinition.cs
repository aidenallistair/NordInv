using System.Collections.Generic;

namespace NordInvasion.Models
{
    public enum MutatorType
    {
        None = 0,
        Berserk = 1,          // Thor - all berserk, no block, +50% speed
        HiddenArchers = 2,     // Skadi
        Greedy = 3,            // Loki - gold x2 but hit steals gold
        Marked = 4,            // Odin - one player marked
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
        public List<string> TroopIds;
        public int BossCount;
        public WaveObjective Objective;
        public MutatorType Mutator;
    }

    public class PerkDefinition
    {
        public int Id;
        public string Name;
        public string Desc;
        public string Branch; // Survivor, Berserk, Tactician
        public float HpMod = 0;
        public float DamageMod = 0;
        public float BarricadeHpMod = 0;
        public float GoldMod = 0;
    }

    public class VillageDefinition
    {
        public int Id;
        public string Name;
        public string Owner; // swadia, nords
        public int DefenseLevel;
        public int X, Y;
    }
}
