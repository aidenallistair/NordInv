using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace NordInvasion.Multiplayer
{
    /// <summary>
    /// Регистрация кастомного MP режима "NordInvasion".
    /// По доке Taleworlds: https://moddocs.bannerlord.com/multiplayer/custom_game_mode/
    /// Этот класс добавляется в SubModule.OnSubModuleLoad через
    /// Module.CurrentModule.AddMultiplayerGameMode(...)
    /// 
    /// GameType в DedicatedCustomServerConfig.xml должен совпадать с именем здесь: "NordInvasion"
    /// </summary>
    public class NordInvasionGameMode : MissionBasedMultiplayerGameMode
    {
        public NordInvasionGameMode(string name) : base(name) { }

        // Вызывается и на клиенте и на сервере, но с разным набором behaviors
        // Мы различаем сторону через GameNetwork.IsServer / IsDedicatedServer / IsClient
        public override void StartMultiplayerGame(string scene)
        {
            // Общие компоненты для обеих сторон
            // Сервер добавит дополнительно серверную логику, клиент - клиентскую
            // В Bannerlord 1.2+ MissionState.OpenNew ожидает MissionInitializerRecord

            bool isServer = GameNetwork.IsServerOrRecorder;

            if (isServer)
            {
                MissionState.OpenNew(Name, new MissionInitializerRecord(scene),
                    missionController => new MissionBehavior[]
                    {
                        MissionLobbyComponent.CreateBehavior(),
                        new MissionMultiplayerNordInvasion(),           // Server authoritative
                        new MissionMultiplayerNordInvasionClient(),     // Client part тоже нужен на сервере для HUD sync
                        new MultiplayerTimerComponent(),
                        new SpawnComponent(new NISpawnFrameBehavior(), new NISpawningBehavior()),
                        new MissionLobbyEquipmentNetworkComponent(),
                        new MultiplayerTeamSelectComponent(),
                        new MissionHardBorderPlacer(),
                        new MissionBoundaryPlacer(),
                        new MissionBoundaryCrossingHandler(),
                        new MultiplayerPollComponent(),
                        new MultiplayerAdminComponent(),
                        new MultiplayerGameNotificationsComponent(),
                        new MissionOptionsComponent(),
                        new MissionScoreboardComponent(new NIScoreboardData()),
                        new MissionAgentPanicHandler(),
                        new AgentHumanAILogic(),
                        new EquipmentControllerLeaveLogic(),
                        new MultiplayerPreloadHelper(),
                        // Наши общие behaviors - они уже проверяют mp_ni_* в SubModule, но для MP добавим явно
                        new Behaviors.NordInvasionWaveManagerBehavior(),
                        new Behaviors.NordInvasionDirectorBehavior(),
                        new Behaviors.NordInvasionWeatherBehavior(),
                        new Managers.FortressBuildManager(),
                        new Managers.PersistenceManager(),
                        new Managers.SquadManager(),
                        new UI.HUD.NI_HUD_Behavior(),
                    });
            }
            else
            {
                MissionState.OpenNew(Name, new MissionInitializerRecord(scene),
                    missionController => new MissionBehavior[]
                    {
                        MissionLobbyComponent.CreateBehavior(),
                        new MissionMultiplayerNordInvasionClient(),
                        new MultiplayerAchievementComponent(),
                        new MultiplayerTimerComponent(),
                        new MultiplayerMissionAgentVisualSpawnComponent(),
                        new MissionLobbyEquipmentNetworkComponent(),
                        new MultiplayerTeamSelectComponent(),
                        new MissionHardBorderPlacer(),
                        new MissionBoundaryPlacer(),
                        new MissionBoundaryCrossingHandler(),
                        new MultiplayerPollComponent(),
                        new MultiplayerAdminComponent(),
                        new MultiplayerGameNotificationsComponent(),
                        new MissionOptionsComponent(),
                        new MissionScoreboardComponent(new NIScoreboardData()),
                        new MissionMatchHistoryComponent(),
                        new EquipmentControllerLeaveLogic(),
                        new MissionRecentPlayersComponent(),
                        new MultiplayerPreloadHelper(),
                        // Клиентские HUD и т.д.
                        new UI.HUD.NI_HUD_Behavior(),
                    });
            }
        }
    }
}
