using UnityEngine;

namespace UsefulScripts.MathRoguelike.Entities
{
    /// <summary>
    /// ScriptableObject that defines an enemy archetype.
    /// Instantiated at runtime into an <see cref="EnemyInstance"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyData", menuName = "MathRoguelike/Enemy")]
    public class EnemyData : ScriptableObject
    {
        [Header("Identity")]
        public string enemyId;
        public string displayName;
        [TextArea(1, 3)]
        public string description;
        public Sprite portrait;

        [Header("Stats")]
        public int  maxHp       = 80;
        public int  attackPower = 15;  // damage dealt when player answers wrong
        public int  defense     = 5;   // damage reduction on incoming hits
        public int  speed       = 10;  // higher = acts first

        [Header("Difficulty Range")]
        public MathDifficulty minDifficulty = MathDifficulty.High;
        public MathDifficulty maxDifficulty = MathDifficulty.Extreme;

        [Header("Preferred Topics (empty = any)")]
        public MathTopic[] preferredTopics;

        [Header("Rewards (on defeat)")]
        public int goldReward  = 15;
        public int scoreReward = 150;

        [Header("Lore / Flavour")]
        [TextArea(1, 5)]
        public string attackFlavour = "The enemy counters your mistake!";
        [TextArea(1, 5)]
        public string defeatFlavour = "Vanquished by your superior intellect!";

        [Header("Boss")]
        public bool isBoss = false;
    }
}
