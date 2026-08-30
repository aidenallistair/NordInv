using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using NordInvasion.Utils;
using TaleWorlds.Core;

namespace NordInvasion.Machines
{
    // Mechanic 2 + 14: Barricades destructible + burning
    public class BarricadeDestructible : DestructibleComponent
    {
        public int StartHitPoints = 800; // настраивается для foundation/wall/door
        private bool _isBurning = false;
        private float _burnEndTime = 0f;

        protected override void OnInit()
        {
            base.OnInit();
            HitPoints = StartHitPoints;
            MaxHitPoints = StartHitPoints;
        }

        public override void OnHit(Agent attackerAgent, int damage, Vec3 impactPosition, Vec3 impactDirection)
        {
            base.OnHit(attackerAgent, damage, impactPosition, impactDirection);

            // Check torch
            if (attackerAgent != null && attackerAgent.WieldedWeapon.Item != null && attackerAgent.WieldedWeapon.Item.StringId == "torch")
            {
                if (!_isBurning)
                {
                    _isBurning = true;
                    _burnEndTime = Mission.CurrentTime + 10f;
                    Mission.Current.Scene.AddParticleSystem(NIEffects.TorchFire, GameEntity.GlobalPosition);
                    InformationManager.DisplayMessage(new InformationMessage("Barricade ignited! Burning 10 sec", Colors.Red));
                }
            }

            // Scavenging on destroy
            if (HitPoints <= 0)
            {
                // Даем 2 дерева ближайшему игроку, разрушавшему
                if (attackerAgent != null)
                {
                    var comp = attackerAgent.GetComponent<Managers.PersistenceManager.PlayerGoldComponent>();
                    if (comp != null)
                    {
                        comp.AddWood(2);
                        InformationManager.DisplayMessage(new InformationMessage("Barricade destroyed! +2 wood", Colors.Yellow));
                    }
                }
            }
        }

        public override void OnTick(float dt)
        {
            base.OnTick(dt);
            if (_isBurning && Mission.CurrentTime < _burnEndTime)
            {
                // Tick damage to nearby nords
                foreach (var agent in Mission.Current.GetNearbyAgents(GameEntity.GlobalPosition.AsVec2, 3f))
                {
                    if (agent.Team.Side == BattleSideEnum.Attacker)
                        agent.Health = agent.Health - (int)(dt * 10f);
                }
            }
            else if (_isBurning && Mission.CurrentTime > _burnEndTime)
            {
                _isBurning = false;
            }
        }
    }

    public class StakesTrap : DestructibleComponent
    {
        protected override void OnInit()
        {
            base.OnInit();
            HitPoints = 400;
            MaxHitPoints = 400;
        }

        public override void OnHit(Agent attackerAgent, int damage, Vec3 impactPosition, Vec3 impactDirection)
        {
            // Mechanic 7: Anti-cav - horse hits stakes
            if (attackerAgent != null)
            {
                var mount = attackerAgent.MountAgent;
                if (mount != null)
                {
                    mount.Health = 0;
                    attackerAgent.Health = 0;
                    InformationManager.DisplayMessage(new InformationMessage("Horse impaled on stakes!", Colors.Green));
                }
            }
            base.OnHit(attackerAgent, damage, impactPosition, impactDirection);
        }
    }

    public class TreasuryChestUsable : UsableMachine
    {
        // Mechanic 8: Treasury - сдать мешок босса
        public override void OnUse(Agent userAgent, int index = -1)
        {
            base.OnUse(userAgent, index);
            var goldComp = userAgent.GetComponent<Managers.PersistenceManager.PlayerGoldComponent>();
            if (goldComp == null) return;

            if (goldComp.IsCarryingLoot)
            {
                int value = goldComp.CarriedLootValue > 0 ? goldComp.CarriedLootValue : 500;
                goldComp.AddGold(value);
                goldComp.IsCarryingLoot = false;
                goldComp.CarriedLootValue = 0;
                userAgent.SetMaximumSpeedFactor(1f);
                InformationManager.DisplayMessage(new InformationMessage($"Gold delivered to treasury! +{value}", Colors.Gold));
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage("Treasury: no loot to deliver. Kill a boss first!", Colors.Yellow));
            }
        }
    }

    public class LootBagUsable : UsableMachine
    {
        public int GoldValue = 500;

        public override void OnUse(Agent userAgent, int index = -1)
        {
            base.OnUse(userAgent, index);
            var goldComp = userAgent.GetComponent<Managers.PersistenceManager.PlayerGoldComponent>();
            if (goldComp == null) return;

            if (goldComp.IsCarryingLoot)
            {
                InformationManager.DisplayMessage(new InformationMessage("You are already carrying a loot bag! Deliver it to the treasury first.", Colors.Red));
                return;
            }

            // Pick up bag
            goldComp.IsCarryingLoot = true;
            goldComp.CarriedLootValue = GoldValue;
            userAgent.SetMaximumSpeedFactor(0.7f);
            InformationManager.DisplayMessage(new InformationMessage($"Picked up {GoldValue} gold bag! Carry to treasury! Speed -30%", Colors.Gold));
            // Мешок исчезает из сцены
            if (GameEntity != null)
                GameEntity.SetActive(false);
        }
    }

    public class CampfireUsable : UsableMachine
    {
        // Mechanic 9: Crafting at campfire
        public override void OnUse(Agent userAgent, int index = -1)
        {
            base.OnUse(userAgent, index);
            var comp = userAgent.GetComponent<Managers.PersistenceManager.PlayerGoldComponent>();
            if (comp == null) return;

            if (comp.Wood >= 3)
            {
                comp.Wood -= 3;
                // Give arrows
                InformationManager.DisplayMessage(new InformationMessage("Crafted arrows! -3 wood", Colors.Green));
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage("Need 3 wood for arrows", Colors.Red));
            }

            // Heal
            userAgent.Health = System.Math.Min(userAgent.Health + 20, userAgent.HealthLimit);
        }
    }

    public class BrazierUsable : UsableMachine
    {
        // Light source for night
        protected override void OnInit()
        {
            base.OnInit();
            Mission.Current.Scene.AddParticleSystem(NIEffects.TorchFire, GameEntity.GlobalPosition);
        }
    }
}
