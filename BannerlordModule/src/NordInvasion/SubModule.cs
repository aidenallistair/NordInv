using System;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using NordInvasion.Behaviors;
using NordInvasion.Managers;

namespace NordInvasion
{
    public class SubModule : MBSubModuleBase
    {
        public static SubModule Instance { get; private set; }

        protected override void OnSubModuleLoad()
        {
            Instance = this;
            base.OnSubModuleLoad();
            InformationManager.DisplayMessage(new InformationMessage("Nord Invasion Better Edition v2.0 Loaded!", Colors.Green));
            // Harmony patches
            // var harmony = new Harmony("com.fianna.nordinvasion.better");
            // harmony.PatchAll();
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);
            if (game.GameType is Campaign)
            {
                var campaignStarter = (CampaignGameStarter)gameStarterObject;
                campaignStarter.AddBehavior(new NordInvasionCampaignBehavior());
            }
        }

        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            base.OnMissionBehaviorInitialize(mission);

            // Only for our missions mp_ni_*
            if (mission.SceneName != null && mission.SceneName.StartsWith("mp_ni_"))
            {
                mission.AddMissionBehavior(new NordInvasionWaveManagerBehavior());
                mission.AddMissionBehavior(new NordInvasionDirectorBehavior());
                mission.AddMissionBehavior(new NordInvasionWeatherBehavior());
                mission.AddMissionBehavior(new NordInvasionObjectiveBehavior());
                mission.AddMissionBehavior(new NordInvasionMutatorBehavior());
                mission.AddMissionBehavior(new FortressBuildManager());
                mission.AddMissionBehavior(new ScavengeManager());
                mission.AddMissionBehavior(new SquadManager());
                mission.AddMissionBehavior(new PersistenceManager());
                
                InformationManager.DisplayMessage(new InformationMessage($"Nord Invasion Started: {mission.SceneName} Wave 1", Colors.Cyan));
            }
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
            InformationManager.DisplayMessage(new InformationMessage("Nord Invasion - Press B for Build, M for Medic, N for Shop", Colors.Yellow));
        }
    }
}
