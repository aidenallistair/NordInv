using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NordInvasion.Multiplayer
{
    /// <summary>
    /// Определяет где могут спавниться игроки (defender) и боты (attacker).
    /// Использует entry points сцены mp_ni_*:
    /// 0-31 игроки (западный форт), 32-63 норды, 64 босс
    /// </summary>
    public class NISpawnFrameBehavior : SpawnFrameBehaviorBase
    {
        public override void Initialize()
        {
            base.Initialize();
            // Можно заранее кешировать точки спавна
        }

        public override MatrixFrame GetSpawnFrame(Team team, bool hasMount, bool isInitialSpawn)
        {
            // Defender = игроки, Attacker = норды
            // Для игроков - точки 0-31, для нордов - 32-63
            List<GameEntity> spawnPoints = new List<GameEntity>();
            if (team != null && team.Side == BattleSideEnum.Defender)
            {
                for (int i = 0; i < 32; i++)
                {
                    var ep = Mission.Current.GetEntryPoint(i);
                    if (ep != null) spawnPoints.Add(ep);
                }
            }
            else
            {
                for (int i = 32; i < 64; i++)
                {
                    var ep = Mission.Current.GetEntryPoint(i);
                    if (ep != null) spawnPoints.Add(ep);
                }
                // босс точка 64
                var bossEp = Mission.Current.GetEntryPoint(64);
                if (bossEp != null && spawnPoints.Count == 0) spawnPoints.Add(bossEp);
            }

            if (spawnPoints.Count == 0)
            {
                // fallback - любая точка
                var any = Mission.Current.GetEntryPoint(0);
                if (any != null) return any.GetGlobalFrame();
                return MatrixFrame.Identity;
            }

            // Выбираем случайную
            var chosen = spawnPoints[MBRandom.RandomInt(spawnPoints.Count)];
            return chosen.GetGlobalFrame();
        }
    }

    /// <summary>
    /// Логика респавна игроков в MP.
    /// В NI игроки респавнятся только на респавн-волнах (каждые 4 волны) или после смерти с задержкой.
    /// В MP мы используем стандартный таймер респавна.
    /// </summary>
    public class NISpawningBehavior : SpawningBehaviorBase
    {
        public override void Initialize(SpawnComponent spawnComponent)
        {
            base.Initialize(spawnComponent);
            // Можно задать время респавна
            // В NI по умолчанию респавн только на волне 4,8,12... но для MP делаем 5 сек
        }

        public override void OnTick(float dt)
        {
            base.OnTick(dt);
            // Проверяем мертвых игроков и респавним если нужно
            if (!GameNetwork.IsServer) return;

            var waveMgr = Mission.GetMissionBehavior<Behaviors.NordInvasionWaveManagerBehavior>();
            if (waveMgr == null) return;

            // Если респавн-волна - респавним всех
            if (waveMgr.IsRespawnWave && waveMgr.State == Models.WaveState.Preparing)
            {
                // RespawnAllPlayers уже есть в WaveManager, но он работает для ActiveAgents
                // В MP мертвые агенты в DeathAgents, их надо респавнить через SpawnComponent
            }
        }

        protected override void SpawnAgents()
        {
            // Native логика спавна игроков через SpawnComponent
            base.SpawnAgents();
        }

        public override bool AllowEarlyAgentVisualsDespawning(MissionPeer peer)
        {
            return true;
        }

        public override int GetMaximumReSpawnPeriodForPeer(MissionPeer peer)
        {
            // Время до респавна после смерти - 5 сек обычно, но в NI на обычных волнах - до следующей респавн-волны
            // Для MP делаем 10 сек чтобы не ждать слишком долго
            return 10;
        }

        public override void RequestStartSpawnSession()
        {
            base.RequestStartSpawnSession();
        }
    }

    /// <summary>
    /// Данные для скорборда
    /// </summary>
    public class NIScoreboardData : IScoreboardData
    {
        public string GetFactionNameForScoreboard(Team team)
        {
            if (team == null) return "";
            return team.Side == BattleSideEnum.Defender ? "Defenders (Swadia)" : "Nords";
        }

        public TaleWorlds.Library.Color GetColorForScoreboard(Team team)
        {
            if (team == null) return TaleWorlds.Library.Color.White;
            return team.Side == BattleSideEnum.Defender ? TaleWorlds.Library.Color.FromUint(0xFF33AA33) : TaleWorlds.Library.Color.FromUint(0xFFAA3333);
        }
    }
}
