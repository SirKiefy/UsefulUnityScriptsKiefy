using System.Collections.Generic;
using UnityEngine;

namespace UsefulScripts.MathRoguelike.Relics
{
    public enum RelicEffectType
    {
        // Passive stat boosts
        BonusDamage,        // +N damage on correct answers
        DamageReduction,    // Reduce incoming damage by N
        BonusGold,          // Earn extra gold per room
        BonusMaxHp,         // Increase max HP
        BonusMaxMp,         // Increase max MP
        HealOnCorrect,      // Heal N HP when answering correctly
        MpOnCorrect,        // Restore N MP when answering correctly

        // Problem modifiers
        ExtraAnswerTime,    // +N seconds on timed problems
        HintFree,           // Hints cost 0 MP
        RerollOnce,         // Once per battle, re-roll the problem
        PartialCredit,      // Wrong but close answer still deals 50% damage

        // Meta effects
        ReviveOnce,         // Survive one lethal hit per run (consumed)
        DoubleScore         // Doubles score from next N battles
    }

    /// <summary>
    /// ScriptableObject defining a single relic's identity and effect.
    /// </summary>
    [CreateAssetMenu(fileName = "RelicData", menuName = "MathRoguelike/Relic")]
    public class RelicData : ScriptableObject
    {
        [Header("Identity")]
        public string relicId;
        public string displayName;
        [TextArea(1, 4)]
        public string description;
        public Sprite icon;

        [Header("Rarity")]
        [Range(1, 5)] public int rarity = 1; // 1 = common, 5 = legendary

        [Header("Effect")]
        public RelicEffectType effectType;
        public float effectValue = 1f;   // meaning depends on effectType

        [Header("Consumable")]
        public bool isConsumedOnUse = false;
    }
}
