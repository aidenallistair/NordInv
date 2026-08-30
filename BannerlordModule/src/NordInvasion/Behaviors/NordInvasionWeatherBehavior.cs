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
}
