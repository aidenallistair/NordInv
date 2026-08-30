using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;

namespace NordInvasion.Components
{
    // Mechanic 13: Wounds & Stamina
    public class WoundStaminaComponent : AgentComponent
    {
        public float Stamina = 100f;
        public int FallenCount = 0;
        public bool IsFallen = false;
        private float _lastHitTime = 0f;

        public WoundStaminaComponent(Agent agent) : base(agent) { }

        public void OnHit(float damage)
        {
            Stamina -= 5f;
            if (Stamina < 0) Stamina = 0;
            _lastHitTime = Mission.CurrentTime;

            if (Stamina < 20)
            {
                Agent.SetMaximumSpeedFactor(0.7f);
            }

            // Bleed chance from axes
            if (MBRandom.RandomFloat < 0.15f)
            {
                var bleed = Agent.GetComponent<ElementalComponent>();
                if (bleed == null || bleed.Type != ElementalType.Bleed)
                    Agent.AddComponent(new ElementalComponent(Agent, ElementalType.Bleed, 5f));
                else
                    bleed.AddStack();
            }
        }

        public bool TryFall()
        {
            // Second Wind perk check
            var perk = Agent.GetComponent<PerkAgentComponent>();
            if (perk != null && perk.HasPerk(3) && FallenCount == 0)
            {
                // Second chance once per wave
                InformationManager.DisplayMessage(new InformationMessage("Second Wind! Second chance!", Colors.Green));
                Agent.Health = 30;
                return true; // not fallen, just second wind
            }

            if (FallenCount < 3)
            {
                FallenCount++;
                IsFallen = true;
                // Визуал "упал" (ragdoll) - опционально, если нужен:
                // AnimationChannel через ActionIndexCache.act_fall_down.
                // Пока останавливаем агента и ждем медика.
                Agent.SetMaximumSpeedFactor(0f);
                return true; // fallen, not dead - can be revived
            }
            return false; // real death
        }

        public void Revive()
        {
            IsFallen = false;
            Agent.Health = 50;
            Agent.SetMaximumSpeedFactor(1f);
            Stamina = 50f;
        }

        public override void OnTick(float dt)
        {
            // Regen stamina slowly if not hit recently
            if (Mission.CurrentTime - _lastHitTime > 3f && Stamina < 100)
            {
                Stamina += dt * 8f;
                if (Stamina > 100) Stamina = 100;
                if (Stamina > 20) Agent.SetMaximumSpeedFactor(1f);
            }

            // Regen perk
            var perk = Agent.GetComponent<PerkAgentComponent>();
            if (perk != null && perk.HasPerk(2) && Stamina > 20)
            {
                // Regen 2 HP/sec outside combat
                if (Mission.CurrentTime - _lastHitTime > 5f && Agent.Health < Agent.HealthLimit)
                {
                    Agent.Health = Agent.Health + (int)(dt * 2f);
                }
            }
        }
    }

    public class PerkAgentComponent : AgentComponent
    {
        public float DamageMod = 1f;
        public float HpMod = 1f;
        public float BarricadeMod = 1f;
        public float GoldMod = 1f;
        public System.Collections.Generic.List<int> Perks = new System.Collections.Generic.List<int>();

        public PerkAgentComponent(Agent agent) : base(agent) { }

        public bool HasPerk(int id) => Perks.Contains(id);

        public void ApplyPerk(int perkId)
        {
            if (Perks.Contains(perkId)) return;
            Perks.Add(perkId);

            var def = Models.PerkDatabase.GetById(perkId);
            if (def == null) return;

            HpMod += def.HpMod;
            DamageMod += def.DamageMod;
            BarricadeMod += def.BarricadeHpMod;
            GoldMod += def.GoldMod;

            switch (perkId)
            {
                case 0: // Iron Skin I
                case 1: // Iron Skin II
                    Agent.HealthLimit = (int)(Agent.HealthLimit * (1f + def.HpMod));
                    Agent.Health = Agent.HealthLimit;
                    break;
                case 10: // Bloodlust handled in OnHit
                    break;
                case 20: // Engineer I
                case 21: // Engineer II
                    // BarricadeMod already set
                    break;
            }
        }

        public float GetDamageWithPerks(float baseDamage)
        {
            float dmg = baseDamage * DamageMod;

            // Bloodlust: +10% per 20% lost HP
            if (HasPerk(10))
            {
                float lostPercent = 1f - (Agent.Health / (float)Agent.HealthLimit);
                int stacks = (int)(lostPercent / 0.2f);
                dmg *= 1f + stacks * 0.1f;
            }

            // Executioner: +50% to bosses below 30%
            // Handled in WaveManager

            return dmg;
        }
    }
}
