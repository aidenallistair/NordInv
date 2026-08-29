using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;

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
                    InformationManager.DisplayMessage(new InformationMessage("Night! Torches needed!", Colors.Black));
                    break;
                default:
                    Mission.Current.Scene.SetFog(100f, 0xFFFFFF);
                    Mission.Current.Scene.SetRainDensity(0f);
                    break;
            }
        }
    }

    public class NordInvasionObjectiveBehavior : MissionBehavior
    {
        public WaveObjective CurrentObjective = WaveObjective.KillAll;

        public void SetupObjective(WaveObjective objective)
        {
            CurrentObjective = objective;
            switch (objective)
            {
                case WaveObjective.DestroyRam:
                    InformationManager.DisplayMessage(new InformationMessage("OBJECTIVE: Destroy enemy ram! 2000 HP", Colors.Red));
                    // Spawn ram
                    break;
                case WaveObjective.Escort:
                    InformationManager.DisplayMessage(new InformationMessage("OBJECTIVE: Escort villager!", Colors.Cyan));
                    break;
                case WaveObjective.BurnCamps:
                    InformationManager.DisplayMessage(new InformationMessage("OBJECTIVE: Burn 3 nord camps with torch!", Colors.Yellow));
                    break;
                case WaveObjective.DefendTreasury:
                    InformationManager.DisplayMessage(new InformationMessage("OBJECTIVE: Defend treasury chest!", Colors.Gold));
                    break;
            }
        }
    }

    public class NordInvasionMutatorBehavior : MissionBehavior
    {
        public Models.MutatorType CurrentMutator = Models.MutatorType.None;

        public void ApplyMutator(Models.MutatorType mutator)
        {
            CurrentMutator = mutator;
            InformationManager.DisplayMessage(new InformationMessage($"MUTATOR: {mutator}!", Colors.Red));
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
            // Campaign map logic
        }

        public override void SyncData(IDataStore dataStore) { }
    }
}
