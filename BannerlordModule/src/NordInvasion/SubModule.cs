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
            InformationManager.DisplayMessage(new InformationMessage("Nord Invasion Better Edition v2.0 - 29 mechanics Loaded!", Colors.Green));
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

            if (mission.SceneName != null && mission.SceneName.StartsWith("mp_ni_"))
            {
                // Core - original 15
                mission.AddMissionBehavior(new UI.HUD.NI_HUD_Behavior());
                mission.AddMissionBehavior(new UI.NI_BuildMenu_Behavior()); // VM стройки: данные для NI_BuildMenu.xml
                mission.AddMissionBehavior(new NordInvasionWaveManagerBehavior());
                mission.AddMissionBehavior(new NordInvasionDirectorBehavior());
                mission.AddMissionBehavior(new NordInvasionWeatherBehavior());
                mission.AddMissionBehavior(new NordInvasionObjectiveBehavior());
                mission.AddMissionBehavior(new NordInvasionMutatorBehavior());
                mission.AddMissionBehavior(new FortressBuildManager());
                mission.AddMissionBehavior(new PerkManager()); // 1 (PerkManager + LootManager живут в PersistenceManager.cs)
                mission.AddMissionBehavior(new ScavengeManager());
                mission.AddMissionBehavior(new SquadManager());
                mission.AddMissionBehavior(new PersistenceManager());
                mission.AddMissionBehavior(new LootManager());

                // Level 2 - extra 14 (without pets)
                mission.AddMissionBehavior(new CommanderBehavior()); // 16
                mission.AddMissionBehavior(new MoraleBehavior()); // 17
                // 18 siege weapons are UsableMachines, not behaviors
                // 19 tempering is UsableMachine Forge
                mission.AddMissionBehavior(new CampPhaseBehavior()); // 20 + 21 dynamic NPCs
                mission.AddMissionBehavior(new BossPhaseBehavior()); // 22
                // 23 traps are UsableMachines
                mission.AddMissionBehavior(new MetaProgressionManager()); // 24 + 26 ranks
                mission.AddMissionBehavior(new SpectatorBettingBehavior()); // 27
                // 28 elemental is AgentComponent
                mission.AddMissionBehavior(new LastStandBehavior()); // 29
                mission.AddMissionBehavior(new SupplyBehavior()); // 30

                InformationManager.DisplayMessage(new InformationMessage(
                    $"Nord Invasion Better Edition: {mission.SceneName} | F at the armory chest = shop, "
                    + "F at a perk totem = perk choice, build/shop menus: NI_BuildMenu/NI_Shop (Gauntlet wiring pending)", Colors.Cyan));
            }
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
            // Честно про то, что доступно без Gauntlet-подключения экранов (docs/AUDIT.md):
            InformationManager.DisplayMessage(new InformationMessage("Nord Invasion - interact with the armory chest and perk totems by F; hotkeys come with the UI hookup", Colors.Yellow));
        }
    }
}
