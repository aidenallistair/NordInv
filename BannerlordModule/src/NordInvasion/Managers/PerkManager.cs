using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NordInvasion.Managers
{
    public class PerkManager : MissionBehavior
    {
        // Окно выбора: 15 сек (как в NI_PerkChoice_VM)
        private const float ChoiceWindowSec = 15f;

        private class PendingChoice
        {
            public List<Models.PerkDefinition> Perks;
            public float EndTime;
        }

        private Dictionary<Agent, PendingChoice> _pending = new Dictionary<Agent, PendingChoice>();
        private readonly Random _rand = new Random();
        private readonly List<Machines.NI_PerkTotemUsable> _totems = new List<Machines.NI_PerkTotemUsable>();

        /// <summary>Тройка, предложенная игроку (для UI/отладки). null - окна нет.</summary>
        public UI.NI_PerkChoice_VM CurrentChoice { get; private set; }

        public override void OnMissionTick(float dt)
        {
            if (Mission.PlayerTeam == null) return;
            var now = Mission.CurrentTime;

            // Убранные агенты
            foreach (var kvp in _pending.ToList())
                if (!kvp.Key.IsActive()) _pending.Remove(kvp.Key);

            // Тайм-аут: не выбрал за 15 сек - рандомный перк
            foreach (var kvp in _pending.ToList())
            {
                if (now > kvp.Value.EndTime)
                {
                    var perk = kvp.Value.Perks[Utils.NIMath.ClampInt(MBRandom.RandomInt(kvp.Value.Perks.Count), 0, kvp.Value.Perks.Count - 1)];
                    InformationManager.DisplayMessage(new InformationMessage($"No choice in time - got: {perk.Name}", Colors.Yellow));
                    _pending.Remove(kvp.Key);
                    ConsumeTotems();          // тотем больше не активен
                    ApplyPerk(kvp.Key, perk.Id);
                }
                else
                {
                    // обратный отсчёт в тотемах/VM
                    int left = (int)System.Math.Max(0f, kvp.Value.EndTime - now);
                    if (CurrentChoice != null && CurrentChoice.TimeLeft != left) CurrentChoice.TimeLeft = left;
                }
            }
        }

        public void ShowChoiceToAll()
        {
            if (Mission.PlayerTeam == null) return;
            foreach (var agent in Mission.PlayerTeam.ActiveAgents.ToList())
            {
                ShowChoice(agent);
            }
        }

        public void ShowChoice(Agent agent)
        {
            if (agent == null || _pending.ContainsKey(agent)) return;
            var perks = Models.PerkDatabase.GetRandomThree(_rand);
            _pending[agent] = new PendingChoice { Perks = perks, EndTime = Mission.CurrentTime + ChoiceWindowSec };

            // VM держит данные для будущего Gauntlet-экрана (NI_PerkChoice.xml)
            CurrentChoice = new UI.NI_PerkChoice_VM();
            CurrentChoice.SetPerks(perks[0], perks[1], perks[2]);

            // MVP-ввод: три тотема рядом с фортом, F = выбор (Gauntlet-подключение - следующий шаг)
            SpawnTotems(agent, perks);

            InformationManager.DisplayMessage(new InformationMessage(
                $"PERK CHOICE ({(int)ChoiceWindowSec}s): 1) {perks[0].Name}  2) {perks[1].Name}  3) {perks[2].Name} - "
                + "hit F on the glowing totem", Colors.Gold));
        }

        /// <summary>
        /// Ставит 3 тотема выбора у игрока. Если меша нет (см. docs/ART_TASKS.md),
        /// PropSpawner даёт vanilla-fallback; если и он не поднялся - остаётся
        /// только тайм-аут = случайный перк (окно выбора не ломается).
        /// </summary>
        void SpawnTotems(Agent agent, List<Models.PerkDefinition> perks)
        {
            var scene = Mission.Current != null ? Mission.Current.Scene : null;
            if (scene == null) return;

            for (int i = 0; i < perks.Count && i < 3; i++)
            {
                var pos = agent.Position + new Vec3((i - 1) * 2.5f, 2.5f, 0f);
                var entity = Machines.PropSpawner.SpawnWithFallback(scene, "ni_brazier",
                    Machines.PropSpawner.FallbackTorch, pos);
                if (entity == null) continue;

                var totem = new Machines.NI_PerkTotemUsable
                {
                    Slot = i,
                    PerkLabel = $"{perks[i].Name} - {perks[i].Desc}",
                };
                entity.AddComponent(totem);
                totem.Entity = entity;
                _totems.Add(totem);
            }
        }

        /// <summary>Есть ли у игрока активное окно выбора (для подсказки на F).</summary>
        public bool HasPendingChoice(Agent agent) => agent != null && _pending.ContainsKey(agent);

        /// <summary>
        /// Тотемы гаснут, когда окно закрылось У ВСЕХ: в кооперативе выбор per-agent,
        /// поэтому один сделанный выбор чужие тотемы не убирает.
        /// </summary>
        void ConsumeTotems()
        {
            if (_pending.Count > 0) return;
            for (int i = _totems.Count - 1; i >= 0; i--)
                _totems[i]?.Retire();
            _totems.Clear();
            CurrentChoice = null;
        }

        /// <summary>
        /// Выбор от Gauntlet-кнопок / тотема (ExecuteChoose1..3 в NI_PerkChoice_VM).
        /// index &lt; 0 - "Skip" = случайный перк из тройки.
        /// </summary>
        public void ChooseForAgent(Agent agent, int index)
        {
            if (agent == null) return;
            if (!_pending.TryGetValue(agent, out var choice)) return;

            int pick = index;
            if (pick < 0 || pick >= choice.Perks.Count)
                pick = Utils.NIMath.ClampInt(MBRandom.RandomInt(choice.Perks.Count), 0, choice.Perks.Count - 1);

            var perk = choice.Perks[pick];
            _pending.Remove(agent);
            ConsumeTotems();   // no-op, пока у других игроков окно открыто
            ApplyPerk(agent, perk.Id);
        }

        public void ApplyPerk(Agent agent, int perkId)
        {
            var goldComp = agent.GetComponent<PersistenceManager.PlayerGoldComponent>();
            goldComp?.AddPerk(perkId);

            var perkComp = agent.GetComponent<Components.PerkAgentComponent>();
            perkComp?.ApplyPerk(perkId);

            // сохраняем выбранный перк в бэкенд
            Mission.GetMissionBehavior<PersistenceManager>()?.ReportPerk(agent, perkId);

            var def = Models.PerkDatabase.GetById(perkId);
            if (def != null)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Perk applied: {def.Name} - {def.Desc}", Colors.Green));
                Audio.NISound.PlayPerkApplied();
            }
            if (!_pending.ContainsKey(agent)) ConsumeTotems();
        }
    }
}
