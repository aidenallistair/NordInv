using TaleWorlds.MountAndBlade;

namespace NordInvasion.Components
{
    // Mechanic 13: Wounds & Stamina
    public class WoundStaminaComponent : AgentComponent
    {
        public float Stamina = 100f;
        public int FallenCount = 0;
        public bool IsFallen = false;

        public WoundStaminaComponent(Agent agent) : base(agent) { }

        public void OnHit(float damage)
        {
            Stamina -= 5f;
            if (Stamina < 0) Stamina = 0;

            if (Stamina < 20)
            {
                // -50% damage
                Agent.SetMaximumSpeedFactor(0.7f);
            }
        }

        public bool TryFall()
        {
            if (FallenCount < 3)
            {
                FallenCount++;
                IsFallen = true;
                Agent.SetActionChannel(0, ActionIndexCache.act_fall_down, false, 0, 0, 1f, 1f, 0f, false, -0.2f, 0, true);
                // Agent becomes invulnerable until revived
                return true; // fallen, not dead
            }
            return false; // real death
        }

        public void Revive()
        {
            IsFallen = false;
            Agent.SetHitPoints(50);
            Agent.SetActionChannel(0, ActionIndexCache.act_stand_up, false, 0, 0, 1f, 1f, 0f, false, -0.2f, 0, true);
        }

        public override void OnTickAsAI(float dt)
        {
            // Regen stamina slowly
            if (Stamina < 100) Stamina += dt * 5f;
        }
    }

    public class PerkAgentComponent : AgentComponent
    {
        public float DamageMod = 1f;
        public float HpMod = 1f;
        public float BarricadeMod = 1f;

        public PerkAgentComponent(Agent agent) : base(agent) { }

        public void ApplyPerk(int perkId)
        {
            switch (perkId)
            {
                case 0: // Iron Skin
                    HpMod += 0.15f;
                    Agent.SetMaximumHitPoints((int)(Agent.HealthLimit * HpMod));
                    break;
                case 10: // Bloodlust
                    DamageMod += 0.1f;
                    break;
                case 20: // Engineer
                    BarricadeMod += 0.3f;
                    break;
            }
        }
    }
}
