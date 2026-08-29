using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.MissionRepresentatives;

namespace NordInvasion.Multiplayer
{
    /// <summary>
    /// Представитель игрока на сервере. Хранит золото/ресурсы/перки.
    /// В Native MP MissionRepresentativeBase уже имеет Gold и ControlledAgent.
    /// Мы расширяем под NI: wood, metal, blueprints, perks.
    /// Сервер-авторитетно: все покупки проверяются через этот класс.
    /// </summary>
    public class NIMissionRepresentative : MissionRepresentativeBase
    {
        public int Wood { get; set; }
        public int Metal { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int BestWave { get; set; }

        // Для синхронизации с клиентом через GameNetwork
        public string PlayerId { get; set; } = "";
        public string SteamId { get; set; } = "";

        public NIMissionRepresentative() : base()
        {
            Gold = 500; // стартовое золото из NISettings
        }

        // Вызывается когда peer синхронизирован
        protected override void OnPeerVariableChanged()
        {
            base.OnPeerVariableChanged();
        }

        public void AddGold(int amount)
        {
            Gold += amount;
            if (Gold < 0) Gold = 0;
        }

        public void AddWood(int amount)
        {
            Wood += amount;
            if (Wood < 0) Wood = 0;
        }

        public void AddMetal(int amount)
        {
            Metal += amount;
            if (Metal < 0) Metal = 0;
        }
    }
}
