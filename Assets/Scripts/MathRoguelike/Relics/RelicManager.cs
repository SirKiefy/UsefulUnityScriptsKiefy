using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UsefulScripts.MathRoguelike.Entities;

namespace UsefulScripts.MathRoguelike.Relics
{
    /// <summary>
    /// Manages the player's relic collection, applies passive bonuses to
    /// <see cref="PlayerStats"/>, and exposes one-shot consumable effects.
    /// </summary>
    public class RelicManager : MonoBehaviour
    {
        [Header("Relic Database")]
        [SerializeField] private List<RelicData> allRelics = new List<RelicData>();

        [Header("References")]
        [SerializeField] private PlayerStats playerStats;

        private readonly List<RelicData> _activeRelics = new List<RelicData>();
        private readonly HashSet<string> _consumedRelics = new HashSet<string>();

        // Events
        public event System.Action<RelicData> OnRelicAdded;

        // ── Public API ────────────────────────────────────────────────

        public IReadOnlyList<RelicData> ActiveRelics => _activeRelics;

        /// <summary>Loads relics from the current RunData and applies them.</summary>
        public void InitialiseFromRun()
        {
            _activeRelics.Clear();
            var run = Core.MathRoguelikeGameManager.Instance?.CurrentRun;
            if (run == null) return;

            foreach (string id in run.relicIds)
            {
                var relic = allRelics.FirstOrDefault(r => r.relicId == id);
                if (relic != null) AddRelic(relic, persist: false);
            }
        }

        /// <summary>Adds a relic, applies its passive effect, and persists to RunData.</summary>
        public void AddRelic(RelicData relic, bool persist = true)
        {
            if (relic == null || _activeRelics.Contains(relic)) return;

            _activeRelics.Add(relic);
            ApplyPassive(relic);
            OnRelicAdded?.Invoke(relic);

            if (persist)
                Core.MathRoguelikeGameManager.Instance?.CurrentRun?.AddRelic(relic.relicId);
        }

        /// <summary>Returns N random relics of varying rarity as a reward offer.</summary>
        public List<RelicData> GetRelicOffers(int count)
        {
            var available = allRelics
                .Where(r => !_activeRelics.Contains(r))
                .OrderBy(_ => Random.value)
                .Take(count)
                .ToList();
            return available;
        }

        /// <summary>Check whether the player holds a specific relic effect (non-consumed).</summary>
        public bool HasEffect(RelicEffectType effect)
            => _activeRelics.Any(r => r.effectType == effect && !_consumedRelics.Contains(r.relicId));

        /// <summary>
        /// Consumes a one-use relic (e.g. ReviveOnce).
        /// Returns true if a matching relic was consumed.
        /// </summary>
        public bool ConsumeRelic(RelicEffectType effect)
        {
            var relic = _activeRelics.FirstOrDefault(
                r => r.effectType == effect &&
                     r.isConsumedOnUse &&
                     !_consumedRelics.Contains(r.relicId));
            if (relic == null) return false;

            _consumedRelics.Add(relic.relicId);
            return true;
        }

        /// <summary>Total bonus damage from all relics.</summary>
        public int TotalBonusDamage =>
            (int)_activeRelics
                .Where(r => r.effectType == RelicEffectType.BonusDamage)
                .Sum(r => r.effectValue);

        /// <summary>Total damage reduction from all relics.</summary>
        public int TotalDamageReduction =>
            (int)_activeRelics
                .Where(r => r.effectType == RelicEffectType.DamageReduction)
                .Sum(r => r.effectValue);

        // ─────────────────────────────────────────────────────────────
        //  Passive application
        // ─────────────────────────────────────────────────────────────

        private void ApplyPassive(RelicData relic)
        {
            if (playerStats == null) return;

            switch (relic.effectType)
            {
                case RelicEffectType.BonusDamage:
                    playerStats.BonusDamage += (int)relic.effectValue;
                    break;
                case RelicEffectType.DamageReduction:
                    playerStats.DamageReduction += (int)relic.effectValue;
                    break;
                case RelicEffectType.ExtraAnswerTime:
                    playerStats.AnswerTimeBonus += relic.effectValue;
                    break;
                case RelicEffectType.BonusMaxHp:
                    var runForHp = Core.MathRoguelikeGameManager.Instance?.CurrentRun;
                    if (runForHp != null)
                    {
                        runForHp.maxHp += (int)relic.effectValue;
                        runForHp.Heal((int)relic.effectValue);
                    }
                    break;
            }
        }
    }
}
