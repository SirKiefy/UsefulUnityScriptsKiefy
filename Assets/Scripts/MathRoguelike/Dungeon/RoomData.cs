using UnityEngine;

namespace UsefulScripts.MathRoguelike.Dungeon
{
    public enum RoomType
    {
        Combat,     // Fight one or more enemies
        Elite,      // Tougher enemy, better reward
        Boss,       // Floor boss
        Treasure,   // Relic or gold, no combat
        Rest,       // Restore HP/MP, no combat
        Shop        // Spend gold on relics/upgrades
    }

    /// <summary>
    /// ScriptableObject blueprint for a single dungeon room.
    /// </summary>
    [CreateAssetMenu(fileName = "RoomData", menuName = "MathRoguelike/Room Data")]
    public class RoomData : ScriptableObject
    {
        [Header("Room Identity")]
        public string  roomId;
        public RoomType roomType = RoomType.Combat;

        [Header("Enemy Spawning (Combat / Elite / Boss)")]
        [Tooltip("Number of enemies to spawn (ignored for non-combat rooms).")]
        public int enemyCount = 1;

        [Header("Treasure (Treasure rooms)")]
        public int guaranteedGold    = 0;
        public int relicChoiceCount  = 1;   // how many relics to offer

        [Header("Rest")]
        [Range(0f, 1f)]
        public float hpRestorePercent = 0.3f;   // 30 % of max HP
        [Range(0f, 1f)]
        public float mpRestorePercent = 0.5f;   // 50 % of max MP

        [Header("Shop")]
        public int shopItemCount = 3;
    }
}
