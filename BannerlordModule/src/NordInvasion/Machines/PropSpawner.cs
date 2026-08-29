using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace NordInvasion.Machines
{
    /// <summary>
    /// Спавн сценарных пропсов из SceneProps.xml.
    ///
    /// ВАЖНО: ID в SceneProps.xml (ni_foundation_wood и т.д.) требуют бинарных
    /// мешей (см. docs/ART_TASKS.md). Пока меши не готовы, LoadSceneProp
    /// вернет null - тогда спавним vanilla-fallback пропс, чтобы механика
    /// работала в игре уже сейчас (видимо как ближайший vanilla объект).
    /// </summary>
    public static class PropSpawner
    {
        /// <summary>ID vanilla-пропсов как fallback (гарантированно существуют в игре).</summary>
        public const string FallbackWall = "empire_garden_wall_a1";
        public const string FallbackChest = "vlandia_chest_c";
        public const string FallbackBarrel = "bd_barrel_a";
        public const string FallbackTorch = "torch_a_wm";
        public const string FallbackFence = "fence_empire_a";
        public const string FallbackWood = "bd_wood_heap_a";
        public const string FallbackCampfire = "fire_stones_bonfire";

        /// <summary>
        /// Загружает пропс по ID (свой или vanilla) и ставит в позицию.
        /// Возвращает null, если пропс не найден (ID опечатан / меш не импортирован).
        /// </summary>
        public static GameEntity Spawn(Scene scene, string propId, Vec3 pos, float yaw = 0f)
        {
            if (scene == null) return null;
            var entity = scene.LoadSceneProp(propId);
            if (entity == null) return null;

            entity.MoveToFrame(new Frame(pos, yaw));
            entity.SetActive(true);
            return entity;
        }

        /// <summary>Свой пропс; если нет меша - vanilla fallback.</summary>
        public static GameEntity SpawnWithFallback(Scene scene, string propId, string fallbackId, Vec3 pos, float yaw = 0f)
        {
            var entity = Spawn(scene, propId, pos, yaw);
            if (entity != null) return entity;
            return Spawn(scene, fallbackId, pos, yaw);
        }
    }
}
