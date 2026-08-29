using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;

namespace NordInvasion.Machines
{
    // Mechanic 23: Environmental traps
    public class RockTrapUsable : UsableMachine
    {
        private bool _triggered = false;

        public override void OnUse(Agent userAgent, int index = -1)
        {
            if (_triggered) return;
            base.OnUse(userAgent, index);

            // Cut rope -> rock falls
            var rockPos = this.GameEntity.GlobalPosition + new Vec3(0, 0, 10f);
            // Spawn falling rock
            Mission.Current.Scene.AddParticleSystem("psys_rock_fall", rockPos);

            // Damage agents below
            foreach (var agent in Mission.Current.GetNearbyAgents(rockPos.AsVec2, 5f))
            {
                if (agent.Team.Side == BattleSideEnum.Attacker)
                {
                    agent.SetHitPoints(0);
                    InformationManager.DisplayMessage(new InformationMessage($"Rock trap crushed {agent.Name}!", Colors.Green));
                }
            }

            _triggered = true;
        }
    }

    public class LogTrapUsable : UsableMachine
    {
        public override void OnUse(Agent userAgent, int index = -1)
        {
            base.OnUse(userAgent, index);
            // Log rolls
            var dir = this.GameEntity.GetGlobalFrame().rotation.f.AsVec3;
            // Simulate rolling log damage line
            for (int i = 0; i < 10; i++)
            {
                var checkPos = this.GameEntity.GlobalPosition + dir * i;
                foreach (var agent in Mission.Current.GetNearbyAgents(checkPos.AsVec2, 2f))
                {
                    if (agent.Team.Side == BattleSideEnum.Attacker)
                        agent.SetHitPoints(agent.Health - 50);
                }
            }
            InformationManager.DisplayMessage(new InformationMessage("Log trap triggered! Rolling!", Colors.Yellow));
        }
    }

    public class OilDitchUsable : UsableMachine
    {
        private bool _oilSpilled = false;
        private bool _ignited = false;

        public override void OnUse(Agent userAgent, int index = -1)
        {
            base.OnUse(userAgent, index);
            if (!_oilSpilled)
            {
                _oilSpilled = true;
                InformationManager.DisplayMessage(new InformationMessage("Oil spilled! Use torch to ignite!", Colors.Yellow));
                // Visual oil
                Mission.Current.Scene.AddParticleSystem("psys_oil_spill", this.GameEntity.GlobalPosition);
            }
            else if (!_ignited)
            {
                // Check if has torch
                if (userAgent.WieldedWeapon.Item?.StringId == "torch")
                {
                    _ignited = true;
                    InformationManager.DisplayMessage(new InformationMessage("Oil ignited! Wall of fire 10 sec!", Colors.Red));
                    Mission.Current.Scene.AddParticleSystem("psys_oil_fire", this.GameEntity.GlobalPosition);
                    // Damage tick for 10 sec
                    // Start coroutine...
                }
            }
        }
    }

    public class DrawbridgeUsable : UsableMachine
    {
        private bool _isRaised = false;

        public override void OnUse(Agent userAgent, int index = -1)
        {
            base.OnUse(userAgent, index);
            _isRaised = !_isRaised;
            if (_isRaised)
                InformationManager.DisplayMessage(new InformationMessage("Bridge raised! Flank cut off but you can't pass either!", Colors.Cyan));
            else
                InformationManager.DisplayMessage(new InformationMessage("Bridge lowered!", Colors.Cyan));

            // Animate bridge
            // this.GameEntity.SetAnimation...
        }
    }
}
