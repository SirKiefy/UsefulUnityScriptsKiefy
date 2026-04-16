using System.Collections.Generic;
using UnityEngine;
using UsefulScripts.MathRoguelike.Entities;

namespace UsefulScripts.MathRoguelike.Dungeon
{
    /// <summary>
    /// Procedurally generates a sequence of <see cref="RoomData"/> objects
    /// that form a single dungeon floor. Room layout follows a weighted
    /// distribution that shifts with floor number to increase challenge.
    /// </summary>
    public class DungeonGenerator : MonoBehaviour
    {
        [Header("Databases")]
        [SerializeField] private EnemyDatabase enemyDatabase;

        [Header("Room Templates")]
        [SerializeField] private RoomData combatRoomTemplate;
        [SerializeField] private RoomData eliteRoomTemplate;
        [SerializeField] private RoomData bossRoomTemplate;
        [SerializeField] private RoomData treasureRoomTemplate;
        [SerializeField] private RoomData restRoomTemplate;
        [SerializeField] private RoomData shopRoomTemplate;

        // ── Weights (adjusted by floor) ───────────────────────────────
        // [combat, elite, treasure, rest, shop] (boss is always last)
        private static readonly float[] BaseWeights = { 0.50f, 0.20f, 0.12f, 0.10f, 0.08f };

        public List<RoomData> GenerateFloor(int floorNumber, int roomCount)
        {
            var rooms = new List<RoomData>(roomCount);

            // Adjust weights: more combat on higher floors
            float[] weights = BuildWeights(floorNumber);

            for (int i = 0; i < roomCount - 1; i++)
                rooms.Add(PickRoom(weights));

            // Final room is always the boss
            rooms.Add(CreateInstance(bossRoomTemplate));
            return rooms;
        }

        // ─────────────────────────────────────────────────────────────
        //  Private helpers
        // ─────────────────────────────────────────────────────────────

        private float[] BuildWeights(int floor)
        {
            float[] w = (float[])BaseWeights.Clone();
            // Shift 5 % from rest/treasure to combat per floor beyond floor 1
            float shift = Mathf.Min((floor - 1) * 0.05f, 0.20f);
            w[0] += shift;          // more combat
            w[2] = Mathf.Max(0.05f, w[2] - shift * 0.5f); // fewer treasure
            w[3] = Mathf.Max(0.04f, w[3] - shift * 0.5f); // fewer rest
            Normalise(w);
            return w;
        }

        private static void Normalise(float[] w)
        {
            float sum = 0f;
            foreach (float v in w) sum += v;
            for (int i = 0; i < w.Length; i++) w[i] /= sum;
        }

        private RoomData PickRoom(float[] weights)
        {
            float roll = Random.value;
            float cumulative = 0f;
            RoomData[] templates =
            {
                combatRoomTemplate,
                eliteRoomTemplate,
                treasureRoomTemplate,
                restRoomTemplate,
                shopRoomTemplate
            };

            for (int i = 0; i < weights.Length; i++)
            {
                cumulative += weights[i];
                if (roll <= cumulative)
                    return CreateInstance(templates[i]);
            }
            return CreateInstance(combatRoomTemplate);
        }

        /// <summary>Clones a template so each room is an independent instance.</summary>
        private static RoomData CreateInstance(RoomData template)
        {
            if (template == null)
            {
                var fallback = ScriptableObject.CreateInstance<RoomData>();
                fallback.roomType = RoomType.Combat;
                return fallback;
            }
            return Object.Instantiate(template);
        }
    }
}
