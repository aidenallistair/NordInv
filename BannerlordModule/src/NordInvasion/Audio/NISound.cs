using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace NordInvasion.Audio
{
    /// <summary>
    /// Звуковые триггеры мода (пункт плана "Звуки для мутаторов, Last Stand музыка").
    ///
    /// Как это работает:
    /// - Играет vanilla FMOD-события через SoundController (без внешних файлов).
    /// - Вызовы через рефлекссию: если сигнатура SoundController в твоей версии
    ///   игры отличается - звук молча не проигрывается, игра НЕ падает и НЕ ломается
    ///   компиляция.
    /// - Все ID собраны в одной таблице (EventIds) - поправка в одном месте.
    ///
    /// КАСТОМНЫЕ ЗВУКИ (свои .ogg): см. ModuleData/Sounds/README.md
    /// (FMOD Studio + BLSE CreateEventFromExternalFile / CreateEventFromSoundBuffer).
    /// </summary>
    public static class NISound
    {
        // Таблица event-ID. Формат "event:/..." - пути FMOD-событий Bannerlord.
        // После первого запуска проверь rgl_log.txt: если "cannot load sound event"
        // - замени ID на существующий (список: Modules/Native/ModuleData/).
        public static class EventIds
        {
            // Мутаторы богов (возглас/удар)
            public const string Mutator = "event:/combat/male_shout_1";
            // Босс: фаза 2/3
            public const string BossPhase2 = "event:/combat/male_shout_2";
            public const string BossPhase3 = "event:/combat/male_shout_3";
            // Last Stand - драм-ролл / сигнал
            public const string LastStandStart = "event:/combat/horn_1";
            public const string LastStandEnd = "event:/combat/horn_2";
            // Победа / поражение
            public const string Victory = "event:/combat/victory_horn";
            public const string Defeat = "event:/combat/defeat_horn";
            // Караван прибыл
            public const string CaravanArrived = "event:/ambience/bell_1";
            // Перк получен
            public const string PerkApplied = "event:/ui/click_1";
            // Босс спавн
            public const string BossSpawn = "event:/combat/drum_hit_1";
        }

        private static bool _warned = false;
        private static Type _soundControllerType;
        private static Type _soundEventType;
        private static MethodInfo _play2d;
        private static MethodInfo _playAt;
        private static bool _resolved;

        static NISound()
        {
            try
            {
                var asm = typeof(TaleWorlds.Core.Agent).Assembly; // TaleWorlds.Core.dll
                _soundControllerType = asm.GetType("TaleWorlds.Core.SoundController");
                _soundEventType = asm.GetType("TaleWorlds.Engine.SoundEvent")
                    ?? asm.GetType("TaleWorlds.Library.SoundEvent")
                    ?? Type.GetType("TaleWorlds.Engine.SoundEvent, TaleWorlds.Engine");
                if (_soundControllerType != null)
                {
                    _play2d = FindMethod(_soundControllerType, "PlaySound");
                    _playAt = FindMethod(_soundControllerType, "PlaySoundAtLocation");
                }
            }
            catch
            {
                // не критично - звук просто не будет играть
            }
            _resolved = true;
        }

        static MethodInfo FindMethod(Type type, string name)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == name).ToList();
            // Любая перегрузка с первым параметром типа SoundEvent
            return methods.FirstOrDefault(m =>
            {
                var p = m.GetParameters();
                return p.Length >= 1 && _soundEventType != null && _soundEventType.IsAssignableFrom(p[0].ParameterType);
            });
        }

        static void Warn(string msg)
        {
            if (!_warned)
            {
                _warned = true;
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[NI Sound] disabled: {msg} (см. ModuleData/Sounds/README.md)", Colors.Yellow));
            }
        }

        /// <summary>Проигрывает 2D-событие. Полностью безопасно для игры.</summary>
        public static void Play(string eventId)
        {
            if (!_resolved) return;
            try
            {
                if (_soundControllerType == null || _soundEventType == null || _play2d == null)
                {
                    Warn("SoundController API не найден в этой версии игры");
                    return;
                }
                var sound = Activator.CreateInstance(_soundEventType, new object[] { eventId });
                _play2d.Invoke(null, new[] { sound, 1f });
            }
            catch (Exception ex)
            {
                Warn($"'{eventId}': {ex.InnerException != null ? ex.InnerException.Message : ex.Message}");
            }
        }

        public static void PlayAt(string eventId, Vec3 position)
        {
            if (!_resolved) return;
            try
            {
                if (_soundControllerType == null || _soundEventType == null || _playAt == null)
                {
                    Play(eventId); // fallback на 2D
                    return;
                }
                var sound = Activator.CreateInstance(_soundEventType, new object[] { eventId });
                _playAt.Invoke(null, new[] { sound, position, 1f });
            }
            catch (Exception ex)
            {
                Warn($"'{eventId}': {ex.InnerException != null ? ex.InnerException.Message : ex.Message}");
            }
        }

        // --- Точки триггера (вызываются из behaviors) ---

        public static void PlayMutator() => Play(EventIds.Mutator);
        public static void PlayBossPhase2() => Play(EventIds.BossPhase2);
        public static void PlayBossPhase3() => Play(EventIds.BossPhase3);
        public static void PlayBossSpawn() => Play(EventIds.BossSpawn);
        public static void PlayLastStandStart() => Play(EventIds.LastStandStart);
        public static void PlayLastStandEnd() => Play(EventIds.LastStandEnd);
        public static void PlayVictory() => Play(EventIds.Victory);
        public static void PlayDefeat() => Play(EventIds.Defeat);
        public static void PlayCaravanArrived() => Play(EventIds.CaravanArrived);
        public static void PlayPerkApplied() => Play(EventIds.PerkApplied);
    }
}
