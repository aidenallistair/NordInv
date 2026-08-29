using TaleWorlds.MountAndBlade;
using System.Collections.Generic;

namespace NordInvasion.Managers
{
    // Mechanic 24: Meta skill tree + 26 Ranks/cosmetics
    public class MetaProgressionManager : MissionBehavior
    {
        public class SkillNode
        {
            public string Id;
            public string Name;
            public string Desc;
            public int Cost; // Season Points
            public List<string> Requires;
            public bool Unlocked;
        }

        public List<SkillNode> SkillTree = new List<SkillNode>
        {
            new SkillNode { Id = "blacksmith_1", Name = "Apprentice Blacksmith", Desc = "Start with +1 wood, blueprints 10% cheaper", Cost = 10 },
            new SkillNode { Id = "blacksmith_2", Name = "Master Blacksmith", Desc = "Start with +2 metal, tempering 20% cheaper", Cost = 20, Requires = new List<string>{"blacksmith_1"} },
            new SkillNode { Id = "veteran_1", Name = "Veteran", Desc = "Start with 600 gold, +5% HP permanent", Cost = 10 },
            new SkillNode { Id = "veteran_2", Name = "Elite Veteran", Desc = "+10% HP, +10% damage", Cost = 25, Requires = new List<string>{"veteran_1"} },
            new SkillNode { Id = "engineer_1", Name = "Engineer Basics", Desc = "Barricades +10% HP from wave 1", Cost = 15 },
            new SkillNode { Id = "engineer_2", Name = "Fortress Architect", Desc = "Barricades +20% HP, repair 2x faster", Cost = 30, Requires = new List<string>{"engineer_1"} },
            new SkillNode { Id = "leader_1", Name = "Squad Leader", Desc = "Can become Commander, markers visible 2x further", Cost = 20 },
        };

        public class RankDefinition
        {
            public string Id;
            public string Title;
            public string Desc;
            public string Requirement;
            public string CosmeticItem; // item id for visual
        }

        public List<RankDefinition> Ranks = new List<RankDefinition>
        {
            new RankDefinition { Id = "wall", Title = "The Wall", Desc = "Survive 10 waves without death", Requirement = "survive_10", CosmeticItem = "ni_pauldron_wall" },
            new RankDefinition { Id = "savior", Title = "Savior", Desc = "Revive 50 players", Requirement = "revive_50", CosmeticItem = "ni_cloak_medic_white" },
            new RankDefinition { Id = "jarl_slayer", Title = "Jarl Slayer", Desc = "Kill 10 bosses", Requirement = "boss_10", CosmeticItem = "ni_sword_blood_jarl" },
            new RankDefinition { Id = "engineer_master", Title = "Master Engineer", Desc = "Build 100 barricades", Requirement = "build_100", CosmeticItem = "ni_helmet_engineer" },
        };

        public void ApplyMetaBonuses(Agent agent, PersistenceManager.PlayerData data)
        {
            // Apply skill tree bonuses from backend
            if (data.Blueprints == null) return;

            if (System.Array.Exists(data.Blueprints, b => b == "blacksmith_1"))
            {
                var comp = agent.GetComponent<PersistenceManager.PlayerGoldComponent>();
                if (comp != null) comp.Wood += 1;
            }
            if (System.Array.Exists(data.Blueprints, b => b == "veteran_1"))
            {
                agent.SetMaximumHitPoints((int)(agent.HealthLimit * 1.05f));
                var comp = agent.GetComponent<PersistenceManager.PlayerGoldComponent>();
                if (comp != null) comp.Gold += 100;
            }
        }

        public void ApplyCosmetics(Agent agent, string[] titles)
        {
            // Check titles and apply visual
            foreach (var title in titles)
            {
                var rank = Ranks.Find(r => r.Id == title);
                if (rank != null)
                {
                    // Equip cosmetic item
                    // agent.Equipment[EquipmentIndex.Armor] = ...
                    InformationManager.DisplayMessage(new InformationMessage($"{agent.Name} has title {rank.Title}!", Colors.Gold));
                }
            }
        }
    }
}
