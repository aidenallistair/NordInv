using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using NordInvasion.Managers;

namespace NordInvasion.Machines
{
    // "Физический UI" забега: до тех пор, пока Gauntlet-экраны мода не подключены
    // к пайплайну экранов (см. docs/AUDIT.md, риск-пункт "Gauntlet-UI не подключен"),
    // магазин и выбор перка доступны через взаимодействующие пропсы - игрок
    // подходит и жмёт F. Работает и в кооперативе, и на dedicated-сервере,
    // потому что использует те же UsableMachine, что и кузница/жаровня.

    /// <summary>
    /// Оружейный ящик (ni_armory_chest) у форта: F - сервисные покупки магазина
    /// (аптечка/ящик снарядов/ремкомплект). Цена и баланс - через бэкенд.
    /// </summary>
    public class NI_ArmoryUsable : UsableMachine
    {
        static readonly string[] ServiceItems = { "heal_kit", "ammo_crate", "repair_kit" };

        public override void OnUse(Agent userAgent, int index = -1)
        {
            base.OnUse(userAgent, index);
            if (userAgent == null || !userAgent.IsActive()) return;

            var persist = Mission.Current.GetMissionBehavior<PersistenceManager>();
            if (persist == null) return;

            // F без индекса (обычный "use") - покупаем самую нужную услугу и показываем прайс
            if (index < 0)
            {
                var lines = new System.Text.StringBuilder("Armory: ");
                for (int i = 0; i < ServiceItems.Length; i++)
                {
                    var item = Models.ShopCatalog.Get(ServiceItems[i]);
                    if (item != null) lines.Append($"{i + 1}) {item.Name} {item.Gold}g  ");
                }
                InformationManager.DisplayMessage(new InformationMessage(lines.ToString(), Colors.Cyan));

                int pick = userAgent.Health < userAgent.HealthLimit * 0.7f ? 0
                    : (Mission.Current.GetMissionBehavior<FortressBuildManager>()?.FindNearestStructure(userAgent.Position, 25f) != null ? 2 : 1);
                persist.BuyShopItem(userAgent, ServiceItems[pick]);
                return;
            }

            if (index >= ServiceItems.Length) return;
            persist.BuyShopItem(userAgent, ServiceItems[index]);
        }
    }

    /// <summary>
    /// Тотем выбора перка (механика 1): PerkManager спавнит три рядом,
    /// F на нужном = выбор. Тайм-аут 15 сек = случайный перк (как в UI-версии).
    /// </summary>
    public class NI_PerkTotemUsable : UsableMachine
    {
        public int Slot;                   // 0/1/2 - индекс в тройке PerkManager
        public string PerkLabel = "";       // подпись для сообщения/отладки
        public GameEntity Entity;           // проп, который надо погасить после выбора
        public bool Retired;                // тотем погашен, на F больше не отвечает

        /// <summary>
        /// F на тотеме = выбор perks[Slot] ДЛЯ СВОЕГО агента. Окно выбора per-agent
        /// (_pending в PerkManager), поэтому второй игрок спокойно берёт свой перк
        /// с того же тотема; у того, кто уже выбрал, ChooseForAgent - no-op.
        /// </summary>
        public override void OnUse(Agent userAgent, int index = -1)
        {
            base.OnUse(userAgent, index);
            if (userAgent == null || !userAgent.IsActive()) return;

            var perkMgr = Mission.Current.GetMissionBehavior<PerkManager>();
            if (perkMgr == null) return;

            if (!perkMgr.HasPendingChoice(userAgent))
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "No perk choice pending for you", Colors.Yellow));
                return;
            }
            perkMgr.ChooseForAgent(userAgent, Slot);
        }

        /// <summary>Гасит проп (тот же SetActive, что использует PropSpawner при спавне).</summary>
        public void Retire()
        {
            if (Retired) return;
            Retired = true;
            if (Entity != null) Entity.SetActive(false);
        }
    }
}
