using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;

namespace NordInvasion.Components
{
    // Mechanic 3: Roles
    public enum PlayerClass { Infantry, Archer, Medic, Engineer, Banner }

    public class ClassComponent : AgentComponent
    {
        public PlayerClass Class = PlayerClass.Infantry;

        public ClassComponent(Agent agent, PlayerClass cls) : base(agent)
        {
            Class = cls;
        }
    }

    public class MedicComponent : AgentComponent
    {
        private float _reviveProgress = 0f;
        private Agent _target = null;

        public MedicComponent(Agent agent) : base(agent) { }

        public bool TryRevive(Agent fallen)
        {
            if (fallen == null) return false;
            var wound = fallen.GetComponent<WoundStaminaComponent>();
            if (wound == null || !wound.IsFallen) return false;

            float dist = Agent.Position.Distance(fallen.Position);
            if (dist > 2f) return false;

            // Start revive timer 5 sec
            _target = fallen;
            _reviveProgress = 0f;
            InformationManager.DisplayMessage(new InformationMessage($"Reviving {fallen.Name}... 5 sec hold F", Colors.Green));
            return true;
        }

        public override void OnTickAsAI(float dt)
        {
            if (_target != null && _target.IsActive())
            {
                float dist = Agent.Position.Distance(_target.Position);
                if (dist > 2.5f)
                {
                    _target = null;
                    _reviveProgress = 0f;
                    return;
                }

                _reviveProgress += dt;
                if (_reviveProgress >= 5f)
                {
                    var wound = _target.GetComponent<WoundStaminaComponent>();
                    wound?.Revive();
                    InformationManager.DisplayMessage(new InformationMessage($"Revived {_target.Name}! +5 gold", Colors.Green));

                    var goldComp = Agent.GetComponent<Managers.PersistenceManager.PlayerGoldComponent>();
                    goldComp?.AddGold(5);

                    // ранг Savior: 50 реанимаций (бэкенд считает)
                    Mission.Current.GetMissionBehavior<Managers.PersistenceManager>()?.OnMedicRevive(Agent);

                    // Last Stand check
                    var lastStand = Mission.Current.GetMissionBehavior<Behaviors.LastStandBehavior>();
                    lastStand?.OnPlayerRevived();

                    _target = null;
                    _reviveProgress = 0f;
                }
            }
        }

        public void Heal(Agent target)
        {
            if (target == null || !target.IsActive()) return;
            if (Agent.Position.Distance(target.Position) > 2f) return;

            target.SetHitPoints(System.Math.Min(target.Health + 30, target.HealthLimit));
            var goldComp = Agent.GetComponent<Managers.PersistenceManager.PlayerGoldComponent>();
            goldComp?.AddGold(2);
        }
    }

    public class EngineerComponent : AgentComponent
    {
        public EngineerComponent(Agent agent) : base(agent) { }

        public void Repair(GameEntity prop)
        {
            var destructible = prop.GetFirstScriptOfType<DestructibleComponent>();
            if (destructible != null)
            {
                int repair = 20;
                var perk = Agent.GetComponent<PerkAgentComponent>();
                if (perk != null) repair = (int)(repair * perk.BarricadeMod);

                destructible.SetHitPoints(destructible.HitPoints + repair);
                InformationManager.DisplayMessage(new InformationMessage($"Repaired {prop.Name} +{repair} HP", Colors.Cyan));
            }
        }

        public bool CanBuildTier2 => true; // Engineer can build Tier2
    }

    public class BannerComponent : AgentComponent
    {
        private float _lastBuffTime = 0f;

        public BannerComponent(Agent agent) : base(agent) { }

        public override void OnTickAsAI(float dt)
        {
            if (Mission.CurrentTime - _lastBuffTime > 2f)
            {
                _lastBuffTime = Mission.CurrentTime;
                ApplyBuff();
            }
        }

        void ApplyBuff()
        {
            float radius = 15f;
            var perk = Agent.GetComponent<PerkAgentComponent>();
            if (perk != null && perk.BarricadeMod > 1f) radius *= 2f; // Banner Master perk

            foreach (var ally in Mission.Current.GetNearbyAgents(Agent.Position.AsVec2, radius))
            {
                if (ally.Team == Agent.Team && ally != Agent)
                {
                    // +10% damage
                    // ally.SetDamageMultiplier(1.1f) - via driven properties
                    var allyPerk = ally.GetComponent<PerkAgentComponent>();
                    if (allyPerk != null) allyPerk.DamageMod = 1.1f;
                }
            }
        }
    }
}
