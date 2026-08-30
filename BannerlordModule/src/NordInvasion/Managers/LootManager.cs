using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NordInvasion.Managers
{
    public class LootManager : MissionBehavior
    {
        /// <summary>Спавнит физ-мешок с золотом босса. F - подобрать, донести до казны.</summary>
        public void SpawnLootBag(Vec3 position, int goldValue)
        {
            InformationManager.DisplayMessage(new InformationMessage($"Boss loot! {goldValue} gold bag - carry to treasury! F to pick", Colors.Gold));

            var entity = Machines.PropSpawner.SpawnWithFallback(
                Mission.Current.Scene, "ni_loot_bag_gold", Machines.PropSpawner.FallbackChest, position);
            if (entity == null)
            {
                // Fallback: без пропса золото просто падает в казну
                InformationManager.DisplayMessage(new InformationMessage($"(loot bag asset missing - +{goldValue} gold auto-deposited)", Colors.Yellow));
                var main = Mission.Current?.MainAgent;
                main?.GetComponent<PersistenceManager.PlayerGoldComponent>()?.AddGold(goldValue);
                return;
            }

            var bag = new Machines.LootBagUsable { GoldValue = goldValue };
            entity.AddComponent(bag);
        }
    }
}
