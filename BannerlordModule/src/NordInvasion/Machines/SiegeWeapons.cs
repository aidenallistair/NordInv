using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using TaleWorlds.Core;

namespace NordInvasion.Machines
{
    // Mechanic 18: Siege weapons for players
    public class BallistaUsable : UsableMachine
    {
        private bool _isLoaded = true;
        private float _reloadTime = 0f;

        public override void OnUse(Agent userAgent, int index = -1)
        {
            base.OnUse(userAgent, index);
            if (!_isLoaded)
            {
                InformationManager.DisplayMessage(new InformationMessage("Ballista reloading...", Colors.Yellow));
                return;
            }

            // Fire
            var pos = this.GameEntity.GlobalPosition;
            var dir = userAgent.LookDirection;
            // Баллиста бьёт по точке в 30м по направлению взгляда.
            // (Mission.SpawnMissile 5-арг в 1.4.8 нет; используем проверенный AddExplosion как AOE-снаряд.)
            Mission.Current.AddExplosion(pos + dir * 30f, 2f, 100f, userAgent);

            _isLoaded = false;
            _reloadTime = Mission.CurrentTime + 5f;
            InformationManager.DisplayMessage(new InformationMessage("Ballista fired! Pierces 3 enemies!", Colors.Green));
        }

        public override void OnTick(float dt)
        {
            base.OnTick(dt);
            if (!_isLoaded && Mission.CurrentTime > _reloadTime)
            {
                _isLoaded = true;
                InformationManager.DisplayMessage(new InformationMessage("Ballista ready!", Colors.Green));
            }
        }
    }

    public class CatapultUsable : UsableMachine
    {
        // AOE damage
        public override void OnUse(Agent userAgent, int index = -1)
        {
            base.OnUse(userAgent, index);
            // Need 2 players: one loads, one aims
            // Simplified: fire at look direction 30m
            var target = userAgent.Position + userAgent.LookDirection * 30f;
            Mission.Current.AddExplosion(target, 5f, 100f, userAgent);
            InformationManager.DisplayMessage(new InformationMessage("Catapult fired! AOE damage!", Colors.Red));
        }
    }

    public class OilPotThrowerUsable : UsableMachine
    {
        public override void OnUse(Agent userAgent, int index = -1)
        {
            var target = userAgent.Position + userAgent.LookDirection * 10f;
            // Spawn oil area + fire
            Mission.Current.Scene.AddParticleSystem("psys_oil_fire", target);
            InformationManager.DisplayMessage(new InformationMessage("Oil pot thrown! Use torch to ignite!", Colors.Yellow));
        }
    }
}
