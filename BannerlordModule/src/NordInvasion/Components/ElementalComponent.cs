using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;

namespace NordInvasion.Components
{
    // Mechanic 28: Elemental damage
    public enum ElementalType { None, Fire, Poison, Ice, Lightning, Bleed }

    public class ElementalComponent : AgentComponent
    {
        public ElementalType Type;
        private float _duration = 0f;
        private float _tickInterval = 1f;
        private float _nextTick = 0f;
        private int _stacks = 1;

        public ElementalComponent(Agent agent, ElementalType type, float duration = 3f) : base(agent)
        {
            Type = type;
            _duration = Mission.CurrentTime + duration;
            _nextTick = Mission.CurrentTime + _tickInterval;

            ApplyImmediate();
        }

        void ApplyImmediate()
        {
            switch (Type)
            {
                case ElementalType.Ice:
                    Agent.SetMaximumSpeedFactor(0.5f);
                    InformationManager.DisplayMessage(new InformationMessage($"{Agent.Name} frozen! -50% speed", Colors.Cyan));
                    break;
                case ElementalType.Poison:
                    Agent.SetMaximumSpeedFactor(0.7f);
                    break;
            }
        }

        public override void OnTick(float dt)
        {
            base.OnTick(dt);

            if (Mission.CurrentTime > _duration)
            {
                // Remove effect
                Agent.SetMaximumSpeedFactor(1f);
                Agent.RemoveComponent(this);
                return;
            }

            if (Mission.CurrentTime > _nextTick)
            {
                _nextTick = Mission.CurrentTime + _tickInterval;
                ApplyTick();
            }
        }

        void ApplyTick()
        {
            switch (Type)
            {
                case ElementalType.Fire:
                    Agent.Health = Agent.Health - 5 * _stacks;
                    // Check if in water/rain -> extinguish
                    var weather = Mission.Current.GetMissionBehavior<Behaviors.NordInvasionWeatherBehavior>();
                    if (weather != null && weather.CurrentWeather == 2) // rain
                    {
                        Agent.RemoveComponent(this);
                    }
                    break;

                case ElementalType.Poison:
                    Agent.Health = Agent.Health - 3 * _stacks;
                    // Needs medic with antidote
                    break;

                case ElementalType.Bleed:
                    Agent.Health = Agent.Health - 2 * _stacks;
                    _stacks = System.Math.Min(_stacks + 1, 5);
                    break;

                case ElementalType.Lightning:
                    // Chain to 3 nearby
                    var nearby = Mission.Current.GetNearbyAgents(Agent.Position.AsVec2, 3f);
                    int chained = 0;
                    foreach (var other in nearby)
                    {
                        if (other != Agent && other.Team == Agent.Team && chained < 3)
                        {
                            other.Health = other.Health - 10;
                            chained++;
                        }
                    }
                    // In rain x2 damage
                    var weather2 = Mission.Current.GetMissionBehavior<Behaviors.NordInvasionWeatherBehavior>();
                    if (weather2 != null && weather2.CurrentWeather == 2)
                    {
                        Agent.Health = Agent.Health - 10; // extra
                    }
                    break;
            }
        }

        public void AddStack()
        {
            _stacks = System.Math.Min(_stacks + 1, 5);
            _duration = Mission.CurrentTime + 3f; // refresh
        }
    }

    // For weapon tempering that applies elemental
    public class ElementalWeaponComponent : AgentComponent
    {
        public ElementalType WeaponElement;

        public ElementalWeaponComponent(Agent agent, ElementalType element) : base(agent)
        {
            WeaponElement = element;
        }

        public void OnHit(Agent target)
        {
            var existing = target.GetComponent<ElementalComponent>();
            if (existing != null && existing.Type == WeaponElement)
            {
                existing.AddStack();
            }
            else
            {
                target.AddComponent(new ElementalComponent(target, WeaponElement));
            }

            // Combo: oil + fire = explosion
            if (WeaponElement == ElementalType.Fire)
            {
                var oil = target.GetComponent<OilComponent>();
                if (oil != null)
                {
                    Mission.Current.AddExplosion(target.Position, 3f, 50f, Agent);
                    target.RemoveComponent(oil);
                }
            }
        }
    }

    public class OilComponent : AgentComponent
    {
        public OilComponent(Agent agent) : base(agent) { }
    }
}
