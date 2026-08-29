using System;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using NordInvasion.Behaviors;
using NordInvasion.Managers;
using NordInvasion.Settings;

namespace NordInvasion
{
    public class SubModule : MBSubModuleBase
    {
        public static SubModule Instance { get; private set; }

        protected override void OnSubModuleLoad()
        {
            Instance = this;
            base.OnSubModuleLoad();
            NISettings.Instance.LoadFromEnvironment();

            // Регистрируем кастомный MP режим NordInvasion для DedicatedCustomServer
            // GameType в DedicatedCustomServerConfig.xml должен быть "NordInvasion"
            // Это основной стабильный путь (как Full Invasion 3), в отличие от Co-op мода
            try
            {
                Module.CurrentModule.AddMultiplayerGameMode(new Multiplayer.NordInvasionGameMode("NordInvasion"));
                InformationManager.DisplayMessage(new InformationMessage("Nord Invasion MP GameType 'NordInvasion' registered (Dedicated Server)", Colors.Green));
            }
            catch (Exception ex)
            {
                // На клиенте без Multiplayer модуля или в старых версиях - не критично
                System.Diagnostics.Debug.WriteLine("Failed to register MP GameMode: " + ex.Message);
            }

            InformationManager.DisplayMessage(new InformationMessage("Nord Invasion Better Edition v2.1 - 29 mechanics Loaded!", Colors.Green));
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);
            if (game.GameType is Campaign)
            {
                var campaignStarter = (CampaignGameStarter)gameStarterObject;
                campaignStarter.AddBehavior(new NordInvasionCampaignBehavior());
            }

            // Загружаем строки для MP режима (имя режима в браузере серверов)
            try
            {
                // ModuleHelper доступен в TaleWorlds.MountAndBlade
                string modulePath = TaleWorlds.MountAndBlade.ModuleHelper.GetModuleFullPath("NordInvasion");
                game.GameTextManager.LoadGameTexts(modulePath + "ModuleData/multiplayer_strings.xml");
            }
            catch { }
        }

        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            base.OnMissionBehaviorInitialize(mission);

            if (mission.SceneName != null && mission.SceneName.StartsWith("mp_ni_"))
            {
                // UI and Input
                mission.AddMissionBehavior(new UI.HUD.NI_HUD_Behavior());
                mission.AddMissionBehavior(new UI.NI_BuildMenu_Behavior()); // VM стройки: данные для NI_BuildMenu.xml
                mission.AddMissionBehavior(new UI.NI_UI_Input_Behavior());  // Горячие клавиши B/N/M/C/K

                // Core - original 15
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
                    $"Nord Invasion Better Edition: {mission.SceneName} | [B] Build | [N] Shop | [M] Campaign | [C] Classes | [K] Help | [F] Armory/Totems", Colors.Cyan));
            }
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
            InformationManager.DisplayMessage(new InformationMessage(
                "Nord Invasion - [B] Build Menu, [N] Shop & BattlePass, [M] Campaign Map, [C] Class Select, [F] Chests & Totems", Colors.Yellow));
        }
    }
}
