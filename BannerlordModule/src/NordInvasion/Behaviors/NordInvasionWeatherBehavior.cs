using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using System.Collections.Generic;
using TaleWorlds.Core;

namespace NordInvasion.Behaviors
{
    public class NordInvasionWeatherBehavior : MissionBehavior
    {
        public int CurrentWeather = 0; // 0 clear, 1 fog, 2 rain, 3 snow, 4 night

        public void SetRandomWeather()
        {
            CurrentWeather = MBRandom.RandomInt(0, 5);
            SetWeather(CurrentWeather);
        }

        public void SetWeather(int weather)
        {
            CurrentWeather = weather;
            switch (weather)
            {
                case 1:
                    Mission.Current.Scene.SetFog(30f, 0x888888);
                    InformationManager.DisplayMessage(new InformationMessage("Fog! Archers -50% range", Colors.Yellow));
                    break;
                case 2:
                    Mission.Current.Scene.SetRainDensity(0.8f);
                    InformationManager.DisplayMessage(new InformationMessage("Rain! Fire arrows disabled, oil not igniting", Colors.Cyan));
                    break;
                case 3:
                    Mission.Current.Scene.SetSnowDensity(0.8f);
                    InformationManager.DisplayMessage(new InformationMessage("Snowstorm! -10% speed", Colors.White));
                    foreach (var agent in Mission.Current.AllAgents) agent.SetMaximumSpeedFactor(0.9f);
                    break;
                case 4:
                    Mission.Current.Scene.SetTimeOfDay(2f); // night
                    InformationManager.DisplayMessage(new InformationMessage("Night! Torches needed!", Colors.Gold));
                    break;
                default:
                    Mission.Current.Scene.SetFog(100f, 0xFFFFFF);
                    Mission.Current.Scene.SetRainDensity(0f);
                    break;
            }
        }
    }

    /// <summary>
    /// Mechanic 4: Цели волн. Реально спавнит объекты цели и отслеживает их.
    /// </summary>
    public class NordInvasionObjectiveBehavior : MissionBehavior
    {
        public WaveObjective CurrentObjective = WaveObjective.KillAll;
        public bool ObjectiveCompleted = false;
        public bool ObjectiveFailed = false;

        private List<GameEntity> _objectiveEntities = new List<GameEntity>();
        private Agent _escortAgent = null;
        private GameEntity _treasuryEntity = null;

        /// <summary>Запускает цель волны (вызывается WaveManager на спавне волны).</summary>
        public void SetupObjective(WaveObjective objective)
        {
            CurrentObjective = objective;
            ObjectiveCompleted = false;
            ObjectiveFailed = false;
            _objectiveEntities.Clear();
            _escortAgent = null;
            _treasuryEntity = null;

            switch (objective)
            {
                case WaveObjective.DestroyRam:
                    InformationManager.DisplayMessage(new InformationMessage("OBJECTIVE: Destroy enemy ram! 2000 HP", Colors.Red));
                    SpawnRam();
                    break;
                case WaveObjective.Escort:
                    InformationManager.DisplayMessage(new InformationMessage("OBJECTIVE: Escort the villager! If he dies - defeat", Colors.Cyan));
                    SpawnEscort();
                    break;
                case WaveObjective.BurnCamps:
                    InformationManager.DisplayMessage(new InformationMessage("OBJECTIVE: Burn 3 nord camps with torch!", Colors.Yellow));
                    SpawnCamps();
                    break;
                case WaveObjective.DefendTreasury:
                    InformationManager.DisplayMessage(new InformationMessage("OBJECTIVE: Defend treasury chest until wave end!", Colors.Gold));
                    SpawnTreasury();
                    break;
            }
        }

        void SpawnRam()
        {
            var entry = Mission.Current.GetEntryPoint(48);
            if (entry == null) return;
            var entity = Machines.PropSpawner.SpawnWithFallback(
                Mission.Current.Scene, "ni_ram", Machines.PropSpawner.FallbackWall, entry.Position);
            if (entity == null) return;
            var destructible = new Machines.BarricadeDestructible { StartHitPoints = 2000 };
            entity.AddComponent(destructible);
            _objectiveEntities.Add(entity);
        }

        void SpawnEscort()
        {
            var villagerTroop = Game.Current.ObjectManager.GetObject<CharacterObject>("ni_villager");
            if (villagerTroop == null) return;
            var entry = Mission.Current.GetEntryPoint(32);
            if (entry == null) return;
            var playerTeam = Mission.PlayerTeam;
            if (playerTeam == null) return;
            _escortAgent = Mission.Current.SpawnAgent(new AgentBuildData(villagerTroop).Team(playerTeam).InitialPosition(entry.Position));
        }

        void SpawnCamps()
        {
            // 3 лагеря по углам спавн-зоны нордов
            for (int i = 0; i < 3; i++)
            {
                var entry = Mission.Current.GetEntryPoint(40 + i * 8);
                if (entry == null) continue;
                var pos = entry.Position + new Vec3(MBRandom.RandomFloat * 10f - 5f, 0f, MBRandom.RandomFloat * 10f - 5f);
                var entity = Machines.PropSpawner.SpawnWithFallback(
                    Mission.Current.Scene, "ni_camp_nord", "village_tent_e", pos);
                if (entity == null) continue;
                entity.AddComponent(new Machines.BarricadeDestructible { StartHitPoints = 300 });
                _objectiveEntities.Add(entity);
            }
        }

        void SpawnTreasury()
        {
            var entry = Mission.Current.GetEntryPoint(8);
            if (entry == null) return;
            var pos = entry.Position + new Vec3(5f, 0f, 0f);
            _treasuryEntity = Machines.PropSpawner.SpawnWithFallback(
                Mission.Current.Scene, "ni_treasury_chest", Machines.PropSpawner.FallbackChest, pos);
            if (_treasuryEntity != null)
                _treasuryEntity.AddComponent(new Machines.TreasuryChestUsable());
        }

        bool IsEntityDestroyed(GameEntity entity)
        {
            if (entity == null || !entity.IsActive()) return true;
            var destructible = entity.GetFirstScriptOfType<DestructibleComponent>();
            if (destructible != null && destructible.HitPoints <= 0) return true;
            return false;
        }

        public override void OnMissionTick(float dt)
        {
            if (CurrentObjective == WaveObjective.KillAll || ObjectiveCompleted || ObjectiveFailed) return;

            switch (CurrentObjective)
            {
                case WaveObjective.DestroyRam:
                    if (_objectiveEntities.Count > 0 && IsEntityDestroyed(_objectiveEntities[0]))
                    {
                        ObjectiveCompleted = true;
                        InformationManager.DisplayMessage(new InformationMessage("RAM DESTROYED! Wave objective completed!", Colors.Green));
                    }
                    break;

                case WaveObjective.BurnCamps:
                    int destroyed = 0;
                    foreach (var e in _objectiveEntities)
                        if (IsEntityDestroyed(e)) destroyed++;
                    if (_objectiveEntities.Count > 0 && destroyed == _objectiveEntities.Count)
                    {
                        ObjectiveCompleted = true;
                        InformationManager.DisplayMessage(new InformationMessage("All camps burned! Wave objective completed!", Colors.Green));
                    }
                    break;

                case WaveObjective.Escort:
                    if (_escortAgent != null && !_escortAgent.IsActive())
                    {
                        ObjectiveFailed = true;
                        InformationManager.DisplayMessage(new InformationMessage("The villager is dead. DEFEAT!", Colors.Red));
                    }
                    break;

                case WaveObjective.DefendTreasury:
                    if (_treasuryEntity != null && IsEntityDestroyed(_treasuryEntity))
                    {
                        ObjectiveFailed = true;
                        InformationManager.DisplayMessage(new InformationMessage("Treasury destroyed! DEFEAT!", Colors.Red));
                    }
                    break;
            }
        }

        /// <summary>WaveManager вызывает: цель выполнена, можно завершать волну.</summary>
        public bool ShouldEndWave() => CurrentObjective != WaveObjective.KillAll && ObjectiveCompleted;
    }

    public class NordInvasionMutatorBehavior : MissionBehavior
    {
        public Models.MutatorType CurrentMutator = Models.MutatorType.None;

        public void ApplyMutator(Models.MutatorType mutator)
        {
            CurrentMutator = mutator;
            var def = Models.MutatorDatabase.All.Find(m => m.Type == mutator);
            InformationManager.DisplayMessage(new InformationMessage(
                $"MUTATOR: {(def != null ? def.Name + " (" + def.God + ")" : mutator.ToString())}!", Colors.Red));
            // Звук объявления (vanilla event, см. Audio/NISound.cs)
            Audio.NISound.PlayMutator();
        }
    }

    public class NordInvasionCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        void OnSessionLaunched(CampaignGameStarter starter)
        {
            // Campaign map logic - голосование деревень через NI_CampaignMap_VM
        }

        public override void SyncData(IDataStore dataStore) { }
    }
}
