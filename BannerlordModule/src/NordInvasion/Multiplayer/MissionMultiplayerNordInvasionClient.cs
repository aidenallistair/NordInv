using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;

namespace NordInvasion.Multiplayer
{
    /// <summary>
    /// Клиентская часть MP режима.
    /// Отвечает за HUD, звуки, обработку сетевых сообщений от сервера.
    /// </summary>
    public class MissionMultiplayerNordInvasionClient : MissionMultiplayerGameModeBaseClient
    {
        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
        }

        public override void AfterStart()
        {
            base.AfterStart();
            InformationManager.DisplayMessage(new InformationMessage("Nord Invasion MP Client started - Defend the fort!", Color.FromUint(0xFF00CCFF)));
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
        }

        public void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegistererContainer registerer)
        {
            try
            {
                var method = registerer.GetType().GetMethod("Register");
                if (method != null)
                {
                    var delServer = new GameNetworkMessage.ServerMessageHandlerDelegate<BuildPlacedMessage>(OnBuildPlaced);
                    method.MakeGenericMethod(typeof(BuildPlacedMessage)).Invoke(registerer, new object[] { delServer });

                    var delGold = new GameNetworkMessage.ServerMessageHandlerDelegate<GoldSyncMessage>(OnGoldSync);
                    method.MakeGenericMethod(typeof(GoldSyncMessage)).Invoke(registerer, new object[] { delGold });

                    var delWave = new GameNetworkMessage.ServerMessageHandlerDelegate<WaveStateMessage>(OnWaveState);
                    method.MakeGenericMethod(typeof(WaveStateMessage)).Invoke(registerer, new object[] { delWave });
                }
            }
            catch { }
        }

        void OnBuildPlaced(BuildPlacedMessage msg)
        {
            if (msg == null) return;
            var scene = Mission.Current.Scene;
            if (scene == null) return;
            var entity = Machines.PropSpawner.SpawnWithFallback(scene, msg.PropId, msg.FallbackId, msg.Position, msg.Yaw);
            if (entity != null)
            {
                // Добавляем компонент баррикады на клиенте тоже
                var buildMgr = Mission.GetMissionBehavior<Managers.FortressBuildManager>();
                // Клиент не добавляет в список Placed чтобы не дублировать, но можно
            }
            InformationManager.DisplayMessage(new InformationMessage($"Fort built by teammate: {msg.PropId}", Color.FromUint(0xFF00FF00)));
        }

        void OnGoldSync(GoldSyncMessage msg)
        {
            // Обновляем HUD
            var hud = Mission.GetMissionBehavior<UI.HUD.NI_HUD_Behavior>();
            // hud?.UpdateGold(msg.Gold, msg.Wood, msg.Metal);
        }

        void OnWaveState(WaveStateMessage msg)
        {
            var hud = Mission.GetMissionBehavior<UI.HUD.NI_HUD_Behavior>();
            // hud?.UpdateWave(msg.WaveNumber, msg.BotsTotal, msg.BotsAlive, ...);
        }
    }
}
