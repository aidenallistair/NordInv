using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using NordInvasion.Managers;

namespace NordInvasion.UI
{
    /// <summary>
    /// Обработчик горячих клавиш и меню в миссии:
    ///   [B]  - Меню строительства форта (Fortress Builder)
    ///   [N]  - Магазин и оружейная (Shop & Armory, BattlePass)
    ///   [M]  - Карта кампании и голосование за деревни
    ///   [C]  - Выбор класса (Infantry, Archer, Medic, Engineer, Banner)
    ///   [K]  - Справка по управлению и клавишам
    /// Позволяет управлять меню как через Gauntlet UI, так и напрямую с клавиатуры.
    /// </summary>
    public class NI_UI_Input_Behavior : MissionBehavior
    {
        public enum ActiveMenu { None, Build, Shop, Campaign, ClassSelect }

        public ActiveMenu CurrentMenu { get; private set; } = ActiveMenu.None;

        public NI_BuildMenu_VM BuildVM { get; private set; }
        public NI_Shop_VM ShopVM { get; private set; }
        public NI_CampaignMap_VM CampaignVM { get; private set; }
        public NI_ClassSelect_VM ClassVM { get; private set; }

        private float _lastInputTime = 0f;
        private const float InputCooldown = 0.25f;

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            BuildVM = new NI_BuildMenu_VM();
            ShopVM = new NI_Shop_VM();
            CampaignVM = new NI_CampaignMap_VM();
            ClassVM = new NI_ClassSelect_VM();
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            if (Mission.Current == null || Mission.Current.MainAgent == null) return;
            if (Mission.CurrentTime - _lastInputTime < InputCooldown) return;

            // Обработка клавиш переключения меню
            if (Input.IsKeyPressed(InputKey.B))
            {
                _lastInputTime = Mission.CurrentTime;
                ToggleBuildMenu();
            }
            else if (Input.IsKeyPressed(InputKey.N) || Input.IsKeyPressed(InputKey.P))
            {
                _lastInputTime = Mission.CurrentTime;
                ToggleShopMenu();
            }
            else if (Input.IsKeyPressed(InputKey.M))
            {
                _lastInputTime = Mission.CurrentTime;
                ToggleCampaignMenu();
            }
            else if (Input.IsKeyPressed(InputKey.C))
            {
                _lastInputTime = Mission.CurrentTime;
                ToggleClassMenu();
            }
            else if (Input.IsKeyPressed(InputKey.K) || Input.IsKeyPressed(InputKey.F1))
            {
                _lastInputTime = Mission.CurrentTime;
                ShowHelp();
            }
            else if (Input.IsKeyPressed(InputKey.Escape) && CurrentMenu != ActiveMenu.None)
            {
                _lastInputTime = Mission.CurrentTime;
                CloseMenu();
            }

            // Быстрые цифровые действия в активном меню
            if (CurrentMenu != ActiveMenu.None)
            {
                HandleMenuNumericInputs();
            }
        }

        public void ToggleBuildMenu()
        {
            if (CurrentMenu == ActiveMenu.Build)
            {
                CloseMenu();
                return;
            }

            CurrentMenu = ActiveMenu.Build;
            var agent = Mission.Current?.MainAgent;
            BuildVM.Refresh(agent);

            InformationManager.DisplayMessage(new InformationMessage(
                "=== FORTRESS BUILDER [B] ===\n"
                + "1: Foundation (5w)  2: Wall (3w)  3: Door (5w 2m)  4: Stakes (4w)\n"
                + "5: Oil Cauldron (10w 5m)  6: Brazier (2w)  7: Shield Wall (6w)\n"
                + "8: Ballista (8w 6m)  9: Catapult (12w 10m)\n"
                + $"Resources: {BuildVM.Resources} | {BuildVM.LockedInfo}", Colors.Gold));
        }

        public void ToggleShopMenu()
        {
            if (CurrentMenu == ActiveMenu.Shop)
            {
                CloseMenu();
                return;
            }

            CurrentMenu = ActiveMenu.Shop;
            var agent = Mission.Current?.MainAgent;
            ShopVM.Refresh(agent);

            InformationManager.DisplayMessage(new InformationMessage(
                "=== SHOP & ARMORY [N] ===\n"
                + $"{ShopVM.GoldText}\n"
                + $"{ShopVM.BattlepassText}\n"
                + $"{ShopVM.ItemsText}\n"
                + "Press 1-8 to Buy, [/] for Prev/Next page, [P] Claim BattlePass, [ESC] Close", Colors.Cyan));
        }

        public void ToggleCampaignMenu()
        {
            if (CurrentMenu == ActiveMenu.Campaign)
            {
                CloseMenu();
                return;
            }

            CurrentMenu = ActiveMenu.Campaign;
            CampaignVM.Refresh();

            InformationManager.DisplayMessage(new InformationMessage(
                "=== SWADIA CAMPAIGN MAP [M] ===\n"
                + $"{CampaignVM.Info}\n"
                + (string.IsNullOrEmpty(CampaignVM.VillageRows) ? "No villages loaded (offline)\n" : CampaignVM.VillageRows)
                + "Press 1-8 to Vote for a village, [ESC] Close", Colors.Green));
        }

        public void ToggleClassMenu()
        {
            if (CurrentMenu == ActiveMenu.ClassSelect)
            {
                CloseMenu();
                return;
            }

            CurrentMenu = ActiveMenu.ClassSelect;
            InformationManager.DisplayMessage(new InformationMessage(
                "=== CHOOSE CLASS [C] ===\n"
                + "1: Infantry (Melee master)\n"
                + "2: Archer (Ranged DPS)\n"
                + "3: Medic (Revive & Heal)\n"
                + "4: Engineer (Repair & Fort Tier2)\n"
                + "5: Banner (Ally damage buff)\n"
                + "Press 1-5 to select class, [ESC] Close", Colors.Yellow));
        }

        public void ShowHelp()
        {
            InformationManager.DisplayMessage(new InformationMessage(
                "=== NORD INVASION CONTROLS ===\n"
                + "[B] Fortress Builder | [N] Shop & Armory | [M] Campaign Map\n"
                + "[C] Choose Class | [F] Interact with Armory Chest & Perk Totems\n"
                + "[K] Help | [ESC] Close Menus | Hold Fort for 25 Waves!", Colors.Gold));
        }

        public void CloseMenu()
        {
            CurrentMenu = ActiveMenu.None;
            InformationManager.DisplayMessage(new InformationMessage("Menu closed", Colors.White));
        }

        void HandleMenuNumericInputs()
        {
            int num = -1;
            if (Input.IsKeyPressed(InputKey.D1) || Input.IsKeyPressed(InputKey.Numpad1)) num = 1;
            else if (Input.IsKeyPressed(InputKey.D2) || Input.IsKeyPressed(InputKey.Numpad2)) num = 2;
            else if (Input.IsKeyPressed(InputKey.D3) || Input.IsKeyPressed(InputKey.Numpad3)) num = 3;
            else if (Input.IsKeyPressed(InputKey.D4) || Input.IsKeyPressed(InputKey.Numpad4)) num = 4;
            else if (Input.IsKeyPressed(InputKey.D5) || Input.IsKeyPressed(InputKey.Numpad5)) num = 5;
            else if (Input.IsKeyPressed(InputKey.D6) || Input.IsKeyPressed(InputKey.Numpad6)) num = 6;
            else if (Input.IsKeyPressed(InputKey.D7) || Input.IsKeyPressed(InputKey.Numpad7)) num = 7;
            else if (Input.IsKeyPressed(InputKey.D8) || Input.IsKeyPressed(InputKey.Numpad8)) num = 8;
            else if (Input.IsKeyPressed(InputKey.D9) || Input.IsKeyPressed(InputKey.Numpad9)) num = 9;

            if (num > 0)
            {
                _lastInputTime = Mission.CurrentTime;
                ExecuteMenuNumericAction(num);
            }

            if (CurrentMenu == ActiveMenu.Shop)
            {
                if (Input.IsKeyPressed(InputKey.OpenBrackets))
                {
                    _lastInputTime = Mission.CurrentTime;
                    ShopVM.ExecutePrevPage();
                }
                else if (Input.IsKeyPressed(InputKey.CloseBrackets))
                {
                    _lastInputTime = Mission.CurrentTime;
                    ShopVM.ExecuteNextPage();
                }
            }
        }

        void ExecuteMenuNumericAction(int num)
        {
            switch (CurrentMenu)
            {
                case ActiveMenu.Build:
                    switch (num)
                    {
                        case 1: BuildVM.ExecuteBuildFoundation(); break;
                        case 2: BuildVM.ExecuteBuildWall(); break;
                        case 3: BuildVM.ExecuteBuildDoor(); break;
                        case 4: BuildVM.ExecuteBuildStakes(); break;
                        case 5: BuildVM.ExecuteBuildOil(); break;
                        case 6: BuildVM.ExecuteBuildBrazier(); break;
                        case 7: BuildVM.ExecuteBuildShieldWall(); break;
                        case 8: BuildVM.ExecuteBuildBallista(); break;
                        case 9: BuildVM.ExecuteBuildCatapult(); break;
                    }
                    break;

                case ActiveMenu.Shop:
                    if (num >= 1 && num <= 8)
                    {
                        var page = ShopVM.PageItems();
                        if (num - 1 < page.Count)
                            ShopVM.Buy(page[num - 1].Id);
                    }
                    break;

                case ActiveMenu.Campaign:
                    if (num >= 1 && num <= 8)
                    {
                        var persist = Mission.Current?.GetMissionBehavior<PersistenceManager>();
                        var agent = Mission.Current?.MainAgent;
                        if (persist != null && agent != null)
                            persist.VoteForVillage(agent, num - 1);
                        CampaignVM.Refresh();
                    }
                    break;

                case ActiveMenu.ClassSelect:
                    switch (num)
                    {
                        case 1: ClassVM.ExecuteInfantry(); CloseMenu(); break;
                        case 2: ClassVM.ExecuteArcher(); CloseMenu(); break;
                        case 3: ClassVM.ExecuteMedic(); CloseMenu(); break;
                        case 4: ClassVM.ExecuteEngineer(); CloseMenu(); break;
                        case 5: ClassVM.ExecuteBanner(); CloseMenu(); break;
                    }
                    break;
            }
        }
    }
}
