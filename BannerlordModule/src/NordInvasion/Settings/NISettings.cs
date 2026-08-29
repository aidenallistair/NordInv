using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace NordInvasion.Settings
{
    /// <summary>
    /// Конфигурация мода Nord Invasion (настройки сервера, баланса и клавиш).
    /// Поддерживает чтение из переменных окружения (NI_*), ручную настройку
    /// и интеграцию с MCM (Mod Configuration Menu v5).
    /// </summary>
    public class NISettings
    {
        private static NISettings _instance;
        public static NISettings Instance => _instance ?? (_instance = new NISettings());

        // Персистенция и бэкенд
        public string BackendUrl { get; set; } = "http://localhost:8080";
        public string ApiSecret { get; set; } = "";
        public int StartingGold { get; set; } = 500;

        // Игровой цикл
        public int MaxWaves { get; set; } = 25;
        public int RespawnWaveInterval { get; set; } = 4;
        public int CampPhaseInterval { get; set; } = 5;
        public float CampPhaseDuration { get; set; } = 90f;
        public int PerkChoiceInterval { get; set; } = 3;
        public float PerkChoiceTimeoutSec { get; set; } = 15f;

        // Директор и баланс
        public float DirectorStressMultiplier { get; set; } = 1.0f;
        public bool EnableDynamicWeather { get; set; } = true;
        public bool EnableStaminaSystem { get; set; } = true;
        public bool EnableEnvironmentalTraps { get; set; } = true;

        // Горячие клавиши (B = стройка, N = магазин, M = кампания, C = классы, K = помощь)
        public string KeyBuildMenu { get; set; } = "B";
        public string KeyShopMenu { get; set; } = "N";
        public string KeyCampaignMenu { get; set; } = "M";
        public string KeyClassSelect { get; set; } = "C";
        public string KeyHelp { get; set; } = "K";

        public NISettings()
        {
            LoadFromEnvironment();
        }

        public void LoadFromEnvironment()
        {
            var url = Environment.GetEnvironmentVariable("NI_BACKEND_URL");
            if (!string.IsNullOrEmpty(url)) BackendUrl = url.TrimEnd('/');

            var secret = Environment.GetEnvironmentVariable("NI_API_SECRET");
            if (!string.IsNullOrEmpty(secret)) ApiSecret = secret;

            var gold = Environment.GetEnvironmentVariable("NI_STARTING_GOLD");
            if (int.TryParse(gold, out int g) && g > 0) StartingGold = g;

            var waves = Environment.GetEnvironmentVariable("NI_MAX_WAVES");
            if (int.TryParse(waves, out int w) && w > 0) MaxWaves = w;
        }

        public static void SetInstance(NISettings custom)
        {
            if (custom != null) _instance = custom;
        }
    }
}
