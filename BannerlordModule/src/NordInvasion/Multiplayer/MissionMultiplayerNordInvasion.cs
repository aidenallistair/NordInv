using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.MissionRepresentatives;
using TaleWorlds.ObjectSystem;

namespace NordInvasion.Multiplayer
{
    /// <summary>
    /// Серверная логика режима NordInvasion.
    /// Наследует MissionMultiplayerGameModeBase — базовый класс для всех MP режимов Native.
    /// 
    /// Обязанности:
    /// - Создать команды Defender (игроки) и Attacker (боты-норды)
    /// - Зарегистрировать MissionRepresentative для каждого peer
    /// - Интегрировать WaveManager, Director, Supply, BuildManager
    /// - Обрабатывать стройку (сервер-авторитетно)
    /// - Синхронизировать золото через Representative
    /// 
    /// Это аналог того, как работает Full Invasion 3.
    /// </summary>
    public class MissionMultiplayerNordInvasion : MissionMultiplayerGameModeBase
    {
        private MissionScoreboardComponent _scoreboardComponent;

        public override bool IsGameModeHidingAllAgentVisuals => false;
        public override bool IsGameModeUsingOpposingTeams => true;

        public override MissionLobbyComponent.MultiplayerGameType GetMissionType()
        {
            // Тип миссии — Battle (осада/оборона), но можно и Siege для флагов
            return MissionLobbyComponent.MultiplayerGameType.Battle;
        }

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            _scoreboardComponent = Mission.GetMissionBehavior<MissionScoreboardComponent>();
        }

        public override void AfterStart()
        {
            base.AfterStart();

            // Культуры команд — берём из настроек или дефолт
            var defenderCulture = MBObjectManager.Instance.GetObject<BasicCultureObject>("vlandia")
                ?? MBObjectManager.Instance.GetObjectTypeList<BasicCultureObject>().FirstOrDefault(c => c.IsMainCulture);
            var attackerCulture = MBObjectManager.Instance.GetObject<BasicCultureObject>("sturgia")
                ?? MBObjectManager.Instance.GetObjectTypeList<BasicCultureObject>().LastOrDefault(c => c.IsMainCulture);

            if (defenderCulture == null || attackerCulture == null) return;

            var defenderBanner = new Banner(defenderCulture.BannerKey, defenderCulture.BackgroundColor1, defenderCulture.ForegroundColor1);
            var attackerBanner = new Banner(attackerCulture.BannerKey, attackerCulture.BackgroundColor1, attackerCulture.ForegroundColor1);

            // Добавляем команды если их ещё нет (в Dedicated сервер иногда уже есть)
            if (Mission.Teams.Count == 0)
            {
                Mission.Teams.Add(BattleSideEnum.Defender, defenderCulture.BackgroundColor1, defenderCulture.ForegroundColor1, defenderBanner, true, false, true);
                Mission.Teams.Add(BattleSideEnum.Attacker, attackerCulture.BackgroundColor1, attackerCulture.ForegroundColor1, attackerBanner, true, false, true);
            }
            else
            {
                // Переопределяем цвета/баннеры существующих
                foreach (var team in Mission.Teams)
                {
                    if (team.Side == BattleSideEnum.Defender)
                    {
                        team.Color = defenderCulture.BackgroundColor1;
                        team.Color2 = defenderCulture.ForegroundColor1;
                    }
                    else
                    {
                        team.Color = attackerCulture.BackgroundColor1;
                        team.Color2 = attackerCulture.ForegroundColor1;
                    }
                }
            }

            // Настройки MP опций (если нужно)
            MBMultiplayerOptionsAccessor.SetCultureTeam1(defenderCulture);
            MBMultiplayerOptionsAccessor.SetCultureTeam2(attackerCulture);

            // Запускаем первую волну через WaveManager
            var waveMgr = Mission.GetMissionBehavior<Behaviors.NordInvasionWaveManagerBehavior>();
            waveMgr?.SetupWave(1);

            InformationManager.DisplayMessage(new InformationMessage("Nord Invasion MP: Defend Swadia! Build with B, Shop with N", Color.FromUint(0xFF00FF00)));
        }

        protected override void HandleNewClientAfterSynchronized(NetworkCommunicator networkPeer)
        {
            // Каждому подключившемуся игроку добавляем представителя
            // В Native это делается для хранения золота, убийств и т.д.
            if (networkPeer.GetComponent<NIMissionRepresentative>() == null)
            {
                networkPeer.AddComponent<NIMissionRepresentative>();
            }

            // Также добавляем стандартный MP representative если нужен
            // networkPeer.AddComponent<MPMissionRepresentative>() - но у нас свой

            // Логика персистенции: логин в backend
            var persist = Mission.GetMissionBehavior<Managers.PersistenceManager>();
            if (persist != null)
            {
                // Peer name / steam id
                string peerName = networkPeer.UserName ?? "unknown";
                string steamId = "";
                try
                {
                    // Reflection-safe как в NIPeers
                    var peer = networkPeer;
                    var type = peer.GetType();
                    foreach (var propName in new[] { "SteamId64", "Id", "SessionId" })
                    {
                        var prop = type.GetProperty(propName);
                        if (prop == null || !prop.CanRead) continue;
                        object val = prop.GetValue(peer, null);
                        if (val == null) continue;
                        string s = val.ToString();
                        if (string.IsNullOrEmpty(s) || s == "0") continue;
                        steamId = s;
                        break;
                    }
                }
                catch { }

                string playerId = Utils.NIPeers.MakePlayerId(steamId, peerName);
                // Асинхронный логин уже есть в PersistenceManager, но для MP надо по peer
                // Упростим: логин через Task
            }
        }

        protected override void HandleNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
        {
            base.HandleNewClientAfterLoadingFinished(networkPeer);
            // Можно отправить текущее состояние волны новому игроку
            var waveMgr = Mission.GetMissionBehavior<Behaviors.NordInvasionWaveManagerBehavior>();
            if (waveMgr != null)
            {
                // Отправляем через GameNetwork сообщение о текущей волне
                // Для MVP - просто чат
                // GameNetwork.BeginModuleEventAsServer(networkPeer);
                // GameNetwork.WriteMessage(new NIWaveStateMessage(waveMgr.WaveNumber, waveMgr.BotsAlive, waveMgr.BotsTotal));
                // GameNetwork.EndModuleEventAsServer();
            }
        }

        public override void OnPeerChangedTeam(NetworkCommunicator peer, Team oldTeam, Team newTeam)
        {
            base.OnPeerChangedTeam(peer, oldTeam, newTeam);
            // В NI все игроки должны быть в Defender
            // Если кто-то перешёл в Attacker - возвращаем
            if (newTeam != null && newTeam.Side == BattleSideEnum.Attacker)
            {
                // Не позволяем играть за нордов (пока)
                var defenderTeam = Mission.Teams.FirstOrDefault(t => t.Side == BattleSideEnum.Defender);
                if (defenderTeam != null && peer.ControlledAgent != null)
                {
                    peer.ControlledAgent.Team = defenderTeam;
                }
            }
        }

        public override int GetScoreForKill(Agent killedAgent)
        {
            if (killedAgent == null || killedAgent.Character == null) return 1;
            var id = killedAgent.Character.StringId;
            if (id.Contains("chieftain")) return 10;
            if (id.Contains("jarl")) return 5;
            if (id.Contains("huscarl")) return 3;
            if (id.Contains("veteran")) return 2;
            return 1;
        }

        public override int GetScoreForAssist(Agent killedAgent)
        {
            return GetScoreForKill(killedAgent) / 2;
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);

            if (affectedAgent == null) return;

            // Обновляем скорборд
            if (blow.DamageType != DamageTypes.Invalid && (agentState == AgentState.Killed || agentState == AgentState.Unconscious))
            {
                if (affectorAgent != null && affectorAgent.IsEnemyOf(affectedAgent))
                {
                    // Даём очки команде убийцы
                    _scoreboardComponent?.ChangeTeamScore(affectorAgent.Team, GetScoreForKill(affectedAgent));

                    // Даём золото через Representative
                    if (affectorAgent.MissionPeer != null)
                    {
                        var rep = affectorAgent.MissionPeer.GetComponent<NIMissionRepresentative>();
                        if (rep != null)
                        {
                            int gold = 5;
                            if (affectedAgent.Character != null)
                            {
                                var sid = affectedAgent.Character.StringId;
                                if (sid.Contains("peasant")) gold = 3;
                                else if (sid.Contains("footman")) gold = 6;
                                else if (sid.Contains("veteran")) gold = 10;
                                else if (sid.Contains("huscarl")) gold = 15;
                                else if (sid.Contains("chieftain")) gold = 100;
                            }
                            rep.AddGold(gold);
                        }
                    }
                }
            }

            // Делегируем в WaveManager для подсчёта BotsAlive
            var waveMgr = Mission.GetMissionBehavior<Behaviors.NordInvasionWaveManagerBehavior>();
            if (affectedAgent.Team != null && affectedAgent.Team.Side == BattleSideEnum.Attacker)
            {
                waveMgr?.OnBotKilled(affectedAgent, affectorAgent);
            }
        }

        public override bool CheckForMatchEnd()
        {
            // Матч заканчивается когда WaveManager достиг победы или поражения
            var waveMgr = Mission.GetMissionBehavior<Behaviors.NordInvasionWaveManagerBehavior>();
            if (waveMgr == null) return false;

            if (waveMgr.State == Models.WaveState.Failed) return true;
            if (waveMgr.WaveNumber >= Behaviors.NordInvasionWaveManagerBehavior.VictoryWave && waveMgr.State == Models.WaveState.Completed)
                return true;

            // Также если все игроки мертвы и не респавн-волна
            var defenderTeam = Mission.Teams.FirstOrDefault(t => t.Side == BattleSideEnum.Defender);
            if (defenderTeam != null)
            {
                int alive = defenderTeam.ActiveAgents.Count;
                if (alive == 0 && !waveMgr.IsRespawnWave)
                {
                    // Проверяем есть ли ещё шанс респавна
                    return true;
                }
            }

            return false;
        }

        public override Team GetWinnerTeam()
        {
            var waveMgr = Mission.GetMissionBehavior<Behaviors.NordInvasionWaveManagerBehavior>();
            if (waveMgr == null) return null;

            if (waveMgr.State == Models.WaveState.Failed)
            {
                return Mission.Teams.FirstOrDefault(t => t.Side == BattleSideEnum.Attacker);
            }
            if (waveMgr.WaveNumber >= Behaviors.NordInvasionWaveManagerBehavior.VictoryWave)
            {
                return Mission.Teams.FirstOrDefault(t => t.Side == BattleSideEnum.Defender);
            }
            return null;
        }

        // Обработка кастомных сетевых сообщений (стройка)
        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
        }

        // Регистрация кастомных сетевых сообщений
        // В разных версиях Bannerlord сигнатура AddRemoveMessageHandlers отличается,
        // поэтому делаем несколько перегрузок и safe-регистрацию через reflection
        public void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegistererContainer registerer)
        {
            // Попытка зарегистрировать через контейнер (Bannerlord 1.2.10+)
            try
            {
                // Сервер получает запросы на стройку от клиентов
                var method = registerer.GetType().GetMethod("Register");
                if (method != null)
                {
                    // Register<T>(ClientMessageHandlerDelegate<T>)
                    var del = new GameNetworkMessage.ClientMessageHandlerDelegate<RequestBuildMessage>(HandleRequestBuild);
                    method.MakeGenericMethod(typeof(RequestBuildMessage)).Invoke(registerer, new object[] { del });
                }
            }
            catch { }
        }

        // Старый API (Bannerlord 1.0.x) - RegisterMode
        public void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegisterer.RegisterMode mode)
        {
            // В этом режиме регистрация идёт через GameNetwork.AddRemoveMessageHandlers
            // Мы регистрируем вручную через GameNetwork
            if (mode == GameNetwork.NetworkMessageHandlerRegisterer.RegisterMode.Add)
            {
                // Для сервера
                GameNetwork.AddNetworkHandler(new NIBuildRequestHandler(this));
            }
            else
            {
                // Remove
            }
        }

        private bool HandleRequestBuild(NetworkCommunicator peer, RequestBuildMessage message)
        {
            if (peer == null || message == null) return false;
            var buildMgr = Mission.GetMissionBehavior<Managers.FortressBuildManager>();
            if (buildMgr == null) return false;
            bool ok = buildMgr.TryPlaceMP(message.BuildType, message.Position, message.Yaw, peer);
            return ok;
        }

        // Фоллбек-обработчик для старого API
        private class NIBuildRequestHandler : TaleWorlds.MountAndBlade.IUdpNetworkHandler
        {
            private readonly MissionMultiplayerNordInvasion _parent;
            public NIBuildRequestHandler(MissionMultiplayerNordInvasion parent) { _parent = parent; }

            public void OnNewConnectionEstablished(NetworkCommunicator peer) { }
            public void OnConnectionFailed(NetworkCommunicator peer) { }
            public void OnPlayerDisconnected(NetworkCommunicator peer) { }
            public bool OnUdpNetworkHandlerTick(float dt) { return false; }
            public void OnHandleConsoleCommand(string command) { }
            public void OnHandlePacket(NetworkCommunicator peer, GameNetworkMessage baseMessage)
            {
                if (baseMessage is RequestBuildMessage msg)
                {
                    _parent.HandleRequestBuild(peer, msg);
                }
            }
        }
    }
}
