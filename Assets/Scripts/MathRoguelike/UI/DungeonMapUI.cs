using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UsefulScripts.MathRoguelike.Core;
using UsefulScripts.MathRoguelike.Dungeon;

namespace UsefulScripts.MathRoguelike.UI
{
    /// <summary>
    /// Renders the current floor's room sequence as a horizontal strip of icons.
    /// Highlights the current room and marks completed rooms.
    /// </summary>
    public class DungeonMapUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private FloorManager floorManager;

        [Header("Prefab & Container")]
        [SerializeField] private GameObject    roomIconPrefab;
        [SerializeField] private Transform     iconContainer;

        [Header("Icon Sprites")]
        [SerializeField] private Sprite combatSprite;
        [SerializeField] private Sprite eliteSprite;
        [SerializeField] private Sprite bossSprite;
        [SerializeField] private Sprite treasureSprite;
        [SerializeField] private Sprite restSprite;
        [SerializeField] private Sprite shopSprite;

        [Header("Colours")]
        [SerializeField] private Color currentRoomColour  = Color.yellow;
        [SerializeField] private Color completedColour    = Color.grey;
        [SerializeField] private Color upcomingColour     = Color.white;

        private readonly List<Image> _icons = new List<Image>();

        private void Start()
        {
            if (floorManager)
                floorManager.OnRoomEntered += _ => Refresh();

            Refresh();
        }

        /// <summary>Rebuilds the icon strip to reflect the current floor state.</summary>
        public void Refresh()
        {
            if (floorManager == null) return;

            // Clear existing icons
            foreach (Transform child in iconContainer) Destroy(child.gameObject);
            _icons.Clear();

            for (int i = 0; i < floorManager.TotalRooms; i++)
            {
                var icon = Instantiate(roomIconPrefab, iconContainer).GetComponent<Image>();
                _icons.Add(icon);
            }

            UpdateIcons();
        }

        private void UpdateIcons()
        {
            if (floorManager == null) return;

            for (int i = 0; i < _icons.Count; i++)
            {
                var room = GetRoom(i);
                if (_icons[i] == null || room == null) continue;

                _icons[i].sprite = SpriteForRoom(room.roomType);
                _icons[i].color  = i < floorManager.CurrentIndex  ? completedColour
                                 : i == floorManager.CurrentIndex ? currentRoomColour
                                 : upcomingColour;
            }
        }

        private RoomData GetRoom(int index)
        {
            var rooms = floorManager.Rooms;
            if (rooms == null || index < 0 || index >= rooms.Count) return null;
            return rooms[index];
        }

        private Sprite SpriteForRoom(RoomType type) => type switch
        {
            RoomType.Combat   => combatSprite,
            RoomType.Elite    => eliteSprite,
            RoomType.Boss     => bossSprite,
            RoomType.Treasure => treasureSprite,
            RoomType.Rest     => restSprite,
            RoomType.Shop     => shopSprite,
            _                 => combatSprite
        };
    }
}
