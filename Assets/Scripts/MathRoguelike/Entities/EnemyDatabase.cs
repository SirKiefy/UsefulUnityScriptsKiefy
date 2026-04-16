using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UsefulScripts.MathRoguelike.Entities
{
    /// <summary>
    /// Holds all <see cref="EnemyData"/> ScriptableObjects and provides
    /// filtered lookups for the dungeon generator.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyDatabase", menuName = "MathRoguelike/Enemy Database")]
    public class EnemyDatabase : ScriptableObject
    {
        [SerializeField] private List<EnemyData> enemies = new List<EnemyData>();

        public IReadOnlyList<EnemyData> AllEnemies => enemies;

        /// <summary>Returns a random non-boss enemy matching the given difficulty.</summary>
        public EnemyData GetRandomEnemy(MathDifficulty difficulty)
        {
            var candidates = enemies.Where(e =>
                !e.isBoss &&
                e.minDifficulty <= difficulty &&
                e.maxDifficulty >= difficulty).ToList();

            return (candidates.Count > 0)
                ? candidates[Random.Range(0, candidates.Count)]
                : enemies[Random.Range(0, enemies.Count)];
        }

        /// <summary>Returns a random boss enemy for the given difficulty.</summary>
        public EnemyData GetRandomBoss(MathDifficulty difficulty)
        {
            var candidates = enemies.Where(e =>
                e.isBoss &&
                e.minDifficulty <= difficulty &&
                e.maxDifficulty >= difficulty).ToList();

            return (candidates.Count > 0)
                ? candidates[Random.Range(0, candidates.Count)]
                : GetRandomEnemy(difficulty); // fallback
        }
    }
}
