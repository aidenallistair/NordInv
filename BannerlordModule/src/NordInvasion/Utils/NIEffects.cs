namespace NordInvasion.Utils
{
    /// <summary>
    /// Константы визуальных эффектов (particle systems).
    ///
    /// ВАЖНО: ID частиц ниже - базовые vanilla-названия. При первом запуске
    /// проверь rgl_log.txt: если движок пишет "particle system not found"
    /// для какого-то ID, поправь строку в этой таблице на существующий
    /// vanilla ID (их можно найти в Modules/Native/ModuleData/).
    /// Не найденная частица = только предупреждение, краша не будет.
    /// </summary>
    public static class NIEffects
    {
        public const string TorchFire = "torch_fire";
        public const string OilFire = "psys_oil_fire";
        public const string Sparks = "psys_sparks";
        public const string Explosion = "psys_explosion";
        public const string RockFall = "psys_rock_fall";
        public const string OilSpill = "psys_oil_spill";
    }
}
