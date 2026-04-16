using UnityEngine;

namespace UsefulScripts.MathRoguelike.Entities
{
    /// <summary>
    /// Runtime state for a single enemy encounter.
    /// Created from an <see cref="EnemyData"/> template.
    /// </summary>
    public class EnemyInstance
    {
        public EnemyData Data      { get; private set; }
        public int CurrentHp       { get; private set; }
        public bool IsAlive        => CurrentHp > 0;

        // ── Combat state ──────────────────────────────────────────────
        public int AttackPower     => Data.attackPower;
        public int Defense         => Data.defense;
        public int Speed           => Data.speed;

        // Events
        public event System.Action<int, int>  OnHpChanged;    // (current, max)
        public event System.Action<EnemyInstance> OnDefeated;

        public EnemyInstance(EnemyData data)
        {
            Data      = data;
            CurrentHp = data.maxHp;
        }

        /// <summary>Applies damage after subtracting defense.</summary>
        public void TakeDamage(int rawDamage)
        {
            int effective = Mathf.Max(1, rawDamage - Defense);
            CurrentHp = Mathf.Max(0, CurrentHp - effective);
            OnHpChanged?.Invoke(CurrentHp, Data.maxHp);

            if (!IsAlive)
                OnDefeated?.Invoke(this);
        }

        /// <summary>Returns damage this enemy deals when the player fails a problem.</summary>
        public int RollAttackDamage()
        {
            // Small variance (±20 %) around base attack
            float variance = Random.Range(0.8f, 1.2f);
            return Mathf.RoundToInt(AttackPower * variance);
        }
    }
}
