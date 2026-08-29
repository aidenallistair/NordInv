using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using TaleWorlds.Core;

namespace NordInvasion.Behaviors
{
    // Mechanic 20: Camp Phase + 21 Dynamic NPCs
    public class CampPhaseBehavior : MissionBehavior
    {
        public bool IsCampPhase = false;
        public float CampEndTime = 0f;
        private int _readyPlayers = 0;

        public void StartCampPhase()
        {
            IsCampPhase = true;
            CampEndTime = Mission.CurrentTime + 90f; // 90 sec camp
            InformationManager.DisplayMessage(new InformationMessage("CAMP PHASE! 90 sec - Trader, Smith, Campfire available! Vote Ready to skip", Colors.Gold));

            // Spawn NPCs
            SpawnTrader();
            SpawnSmith();
            TrySpawnDynamicNPC();
        }

        void SpawnTrader()
        {
            var traderTroop = Game.Current.ObjectManager.GetObject<TaleWorlds.Core.CharacterObject>("ni_trader");
            var pos = Mission.Current.GetEntryPoint(0).Position + new Vec3(2, 0, 0);
            var agent = Mission.Current.SpawnAgent(new AgentBuildData(traderTroop).Team(Mission.PlayerTeam).InitialPosition(pos));
            // Add trader component
        }

        void SpawnSmith()
        {
            var smithTroop = Game.Current.ObjectManager.GetObject<TaleWorlds.Core.CharacterObject>("ni_smith");
            var pos = Mission.Current.GetEntryPoint(0).Position + new Vec3(-2, 0, 0);
            Mission.Current.SpawnAgent(new AgentBuildData(smithTroop).Team(Mission.PlayerTeam).InitialPosition(pos));
        }

        void TrySpawnDynamicNPC()
        {
            // Mechanic 21
            int roll = MBRandom.RandomInt(4);
            switch (roll)
            {
                case 0:
                    SpawnRefugees();
                    break;
                case 1:
                    SpawnDeserter();
                    break;
                case 2:
                    SpawnScavengerTrader();
                    break;
                case 3:
                    SpawnWoundedKnight();
                    break;
            }
        }

        void SpawnRefugees()
        {
            InformationManager.DisplayMessage(new InformationMessage("Refugees spotted! Escort them to fort for +200 gold!", Colors.Cyan));
            var villager = Game.Current.ObjectManager.GetObject<TaleWorlds.Core.CharacterObject>("ni_villager");
            for (int i = 0; i < 3; i++)
            {
                var pos = Mission.Current.GetEntryPoint(32).Position;
                Mission.Current.SpawnAgent(new AgentBuildData(villager).Team(Mission.PlayerTeam).InitialPosition(pos));
            }
        }

        void SpawnDeserter()
        {
            InformationManager.DisplayMessage(new InformationMessage("Nord deserter! He knows boss weak point! +50% damage to boss for 30 sec", Colors.Yellow));
            // Apply buff to all players
            foreach (var agent in Mission.PlayerTeam.ActiveAgents)
            {
                var comp = agent.GetComponent<Components.PerkAgentComponent>();
                if (comp != null) comp.DamageMod += 0.5f;
            }
        }

        void SpawnScavengerTrader()
        {
            InformationManager.DisplayMessage(new InformationMessage("Rare blueprint trader! Sells rare blueprint for 1000 gold", Colors.Gold));
        }

        void SpawnWoundedKnight()
        {
            InformationManager.DisplayMessage(new InformationMessage("Wounded knight! Heal with medic to fight for you 1 wave", Colors.Green));
        }

        public void PlayerReady(Agent player)
        {
            _readyPlayers++;
            if (_readyPlayers >= Mission.PlayerTeam.ActiveAgents.Count)
            {
                EndCampPhase();
            }
        }

        void EndCampPhase()
        {
            IsCampPhase = false;
            _readyPlayers = 0;
            InformationManager.DisplayMessage(new InformationMessage("Camp phase ended! Next wave incoming!", Colors.Red));
            Mission.GetMissionBehavior<NordInvasionWaveManagerBehavior>()?.SetupWave(Mission.GetMissionBehavior<NordInvasionWaveManagerBehavior>().WaveNumber);
        }

        public override void OnMissionTick(float dt)
        {
            if (IsCampPhase && Mission.CurrentTime > CampEndTime)
                EndCampPhase();
        }
    }
}
