using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;

namespace NordInvasion.Machines
{
    // Mechanic 19: Weapon Tempering
    public class ForgeUsable : UsableMachine
    {
        public enum TemperType { Sharpened, Hardened, Poisoned, Flaming }

        public override void OnUse(Agent userAgent, int index = -1)
        {
            base.OnUse(userAgent, index);
            // Open Gauntlet UI Forge_VM
            // For now, simple logic
            var goldComp = userAgent.GetComponent<Managers.PersistenceManager.PlayerGoldComponent>();
            if (goldComp == null || goldComp.Metal < 3)
            {
                InformationManager.DisplayMessage(new InformationMessage("Need 3 metal to temper!", Colors.Red));
                return;
            }

            // Apply random temper for demo
            var weapon = userAgent.WieldedWeapon;
            if (weapon.IsEmpty)
            {
                InformationManager.DisplayMessage(new InformationMessage("Wield a weapon first!", Colors.Red));
                return;
            }

            goldComp.Metal -= 3;
            var temper = (TemperType)MBRandom.RandomInt(4);
            ApplyTemper(userAgent, weapon, temper);
        }

        void ApplyTemper(Agent agent, MissionWeapon weapon, TemperType type)
        {
            var perkComp = agent.GetComponent<Components.PerkAgentComponent>();
            if (perkComp == null) return;

            switch (type)
            {
                case TemperType.Sharpened:
                    perkComp.DamageMod += 0.1f;
                    InformationManager.DisplayMessage(new InformationMessage("Weapon sharpened! +10% damage", Colors.Green));
                    break;
                case TemperType.Hardened:
                    // +20% durability - less chance to break shield
                    InformationManager.DisplayMessage(new InformationMessage("Weapon hardened! +20% durability", Colors.Green));
                    break;
                case TemperType.Poisoned:
                    // Add poison component
                    agent.AddComponent(new Components.ElementalComponent(agent, Components.ElementalType.Poison));
                    InformationManager.DisplayMessage(new InformationMessage("Weapon poisoned! Tick damage", Colors.Green));
                    break;
                case TemperType.Flaming:
                    agent.AddComponent(new Components.ElementalComponent(agent, Components.ElementalType.Fire));
                    InformationManager.DisplayMessage(new InformationMessage("Weapon flaming! Burns enemies", Colors.Red));
                    break;
            }

            // Visual effect
            // Mission.Current.Scene.AddParticleSystem("psys_sparks", agent.Position);
        }
    }
}
