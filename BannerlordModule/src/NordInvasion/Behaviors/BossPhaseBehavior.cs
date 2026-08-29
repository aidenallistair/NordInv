using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;

namespace NordInvasion.Behaviors
{
    // Mechanic 22: Boss with phases
    public class BossPhaseBehavior : MissionBehavior
    {
        public class BossState
        {
            public Agent BossAgent;
            public int Phase = 1;
            public float MaxHP;
        }

        private System.Collections.Generic.List<BossState> _bosses = new System.Collections.Generic.List<BossState>();

        public void RegisterBoss(Agent boss)
        {
            _bosses.Add(new BossState { BossAgent = boss, Phase = 1, MaxHP = boss.HealthLimit });
            InformationManager.DisplayMessage(new InformationMessage($"BOSS SPAWNED: {boss.Name} - Phase 1", Colors.Red));
        }

        public override void OnMissionTick(float dt)
        {
            foreach (var boss in _bosses.ToArray())
            {
                if (!boss.BossAgent.IsActive()) continue;

                float hpPercent = boss.BossAgent.Health / boss.MaxHP * 100f;

                if (hpPercent < 66f && boss.Phase == 1)
                    TransitionToPhase(boss, 2);
                else if (hpPercent < 33f && boss.Phase == 2)
                    TransitionToPhase(boss, 3);
            }
        }

        void TransitionToPhase(BossState boss, int newPhase)
        {
            boss.Phase = newPhase;
            var agent = boss.BossAgent;

            switch (newPhase)
            {
                case 2:
                    InformationManager.DisplayMessage(new InformationMessage($"BOSS {agent.Name} Phase 2! Enraged! Minions summoned!", Colors.Red));
                    // Summon 2 minions
                    var minionTroop = Game.Current.ObjectManager.GetObject<TaleWorlds.Core.CharacterObject>("ni_nord_berserker");
                    for (int i = 0; i < 2; i++)
                    {
                        Mission.Current.SpawnAgent(new AgentBuildData(minionTroop).Team(agent.Team).InitialPosition(agent.Position + new Vec3(i, 0, 0)));
                    }
                    // Buff nearby nords +50% speed
                    foreach (var ally in Mission.Current.GetNearbyAgents(agent.Position.AsVec2, 10f))
                    {
                        if (ally.Team == agent.Team) ally.SetMaximumSpeedFactor(1.5f);
                    }
                    // Throw axes AOE
                    break;

                case 3:
                    InformationManager.DisplayMessage(new InformationMessage($"BOSS {agent.Name} Phase 3! BERSERK! Fire around! Kite him!", Colors.Red));
                    // Ignite ground
                    Mission.Current.Scene.AddParticleSystem("psys_oil_fire", agent.Position);
                    // Berserk speed
                    agent.SetMaximumSpeedFactor(1.8f);
                    break;
            }
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            var boss = _bosses.Find(b => b.BossAgent == affectedAgent);
            if (boss != null)
            {
                InformationManager.DisplayMessage(new InformationMessage($"BOSS {affectedAgent.Name} DEFEATED! Explosion in 3 sec - RUN!", Colors.Gold));
                // Explosion after 3 sec
                Mission.Current.AddExplosion(affectedAgent.Position, 5f, 150f, affectorAgent);
                _bosses.Remove(boss);
            }
        }
    }
}
