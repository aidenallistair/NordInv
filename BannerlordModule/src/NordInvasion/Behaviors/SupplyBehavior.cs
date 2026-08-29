using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;

namespace NordInvasion.Behaviors
{
    // Mechanic 30: Supply lines and logistics
    public class SupplyBehavior : MissionBehavior
    {
        public int WoodStock = 50;
        public int MetalStock = 20;
        public int MaxWood = 50;
        public int MaxMetal = 30;
        public int WarehouseLevel = 1;

        private bool _caravanEnRoute = false;
        private float _nextCaravanTime = 0f;
        private System.Collections.Generic.List<Agent> _caravanAgents = new System.Collections.Generic.List<Agent>();

        public override void OnMissionTick(float dt)
        {
            // Check caravan timer every 3 waves
            var waveManager = Mission.GetMissionBehavior<NordInvasionWaveManagerBehavior>();
            if (waveManager == null) return;

            if (waveManager.WaveNumber % 3 == 0 && !_caravanEnRoute && Mission.CurrentTime > _nextCaravanTime)
            {
                SpawnCaravan();
            }

            // Auto repair if warehouse level 2
            if (WarehouseLevel >= 2 && WoodStock > 0)
            {
                // Every 30 sec repair 1 barricade for 5 wood
                if (Mission.CurrentTime % 30f < 0.1f && WoodStock >= 5)
                {
                    WoodStock -= 5;
                    // Find damaged barricade and repair
                    InformationManager.DisplayMessage(new InformationMessage("Warehouse auto-repaired a barricade! -5 wood", Colors.Cyan));
                }
            }
        }

        void SpawnCaravan()
        {
            _caravanEnRoute = true;
            InformationManager.DisplayMessage(new InformationMessage("SUPPLY CARAVAN incoming! 2 carts + guards - protect from ambush! +20 wood, +10 metal if arrives", Colors.Gold));

            var cartTroop = Game.Current.ObjectManager.GetObject<TaleWorlds.Core.CharacterObject>("ni_cart");
            var guardTroop = Game.Current.ObjectManager.GetObject<TaleWorlds.Core.CharacterObject>("ni_caravan_guard");

            var entry = Mission.Current.GetEntryPoint(32).Position;
            // 2 carts
            for (int i = 0; i < 2; i++)
            {
                var cart = Mission.Current.SpawnAgent(new AgentBuildData(cartTroop).Team(Mission.PlayerTeam).InitialPosition(entry + new Vec3(i * 2, 0, 0)));
                _caravanAgents.Add(cart);
            }
            // 4 guards
            for (int i = 0; i < 4; i++)
            {
                var guard = Mission.Current.SpawnAgent(new AgentBuildData(guardTroop).Team(Mission.PlayerTeam).InitialPosition(entry + new Vec3(0, i, 0)));
                _caravanAgents.Add(guard);
            }

            // Spawn ambush nords
            var ambushTroop = Game.Current.ObjectManager.GetObject<TaleWorlds.Core.CharacterObject>("ni_nord_raider_mounted");
            for (int i = 0; i < 6; i++)
            {
                var ambushPos = Mission.Current.GetEntryPoint(40).Position;
                Mission.Current.SpawnAgent(new AgentBuildData(ambushTroop).Team(Mission.Current.Teams.First(t => t.Side == BattleSideEnum.Attacker)).InitialPosition(ambushPos));
            }

            _nextCaravanTime = Mission.CurrentTime + 300f; // next in 5 min
        }

        public void OnCaravanReached()
        {
            if (!_caravanEnRoute) return;

            WoodStock = System.Math.Min(WoodStock + 20, MaxWood);
            MetalStock = System.Math.Min(MetalStock + 10, MaxMetal);
            _caravanEnRoute = false;
            _caravanAgents.Clear();

            InformationManager.DisplayMessage(new InformationMessage($"Caravan arrived! Stock: {WoodStock}/{MaxWood} wood, {MetalStock}/{MaxMetal} metal", Colors.Green));

            // Reward players
            foreach (var agent in Mission.PlayerTeam.ActiveAgents)
            {
                var comp = agent.GetComponent<Managers.PersistenceManager.PlayerGoldComponent>();
                comp?.AddGold(50);
            }
        }

        public void OnCaravanDestroyed()
        {
            if (!_caravanEnRoute) return;

            _caravanEnRoute = false;
            _caravanAgents.Clear();

            InformationManager.DisplayMessage(new InformationMessage("Caravan destroyed! No resources for next 3 waves! Only scavenging!", Colors.Red));
            // No resources for 3 waves - handled by WaveManager checking stock
        }

        public bool TrySpendWood(int amount)
        {
            if (WoodStock >= amount)
            {
                WoodStock -= amount;
                return true;
            }
            return false;
        }

        public void UpgradeWarehouse()
        {
            if (WarehouseLevel == 1 && WoodStock >= 30 && MetalStock >= 10)
            {
                WoodStock -= 30; MetalStock -= 10;
                WarehouseLevel = 2;
                MaxWood = 100;
                MaxMetal = 60;
                InformationManager.DisplayMessage(new InformationMessage("Warehouse upgraded to Level 2! 100 wood limit + auto-repair!", Colors.Gold));
            }
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            if (_caravanAgents.Contains(affectedAgent))
            {
                _caravanAgents.Remove(affectedAgent);
                // If all carts dead -> caravan failed
                if (_caravanAgents.Count == 0)
                {
                    OnCaravanDestroyed();
                }
                // If carts reached warehouse (check position)
                else if (affectedAgent.Position.Distance(Mission.Current.GetEntryPoint(0).Position) < 5f)
                {
                    OnCaravanReached();
                }
            }
        }
    }
}
