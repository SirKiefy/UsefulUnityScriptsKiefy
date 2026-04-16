using System.Collections.Generic;
using UnityEngine;
using UsefulScripts.MathRoguelike.Dungeon;

namespace UsefulScripts.MathRoguelike.Core
{
    /// <summary>
    /// Manages the current floor layout and drives room-to-room progression.
    /// </summary>
    public class FloorManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DungeonGenerator generator;

        [Header("Settings")]
        [SerializeField] private int roomsPerFloor = 8;

        private List<RoomData> _rooms;
        private int _currentIndex;

        public RoomData CurrentRoom => (_rooms != null && _currentIndex < _rooms.Count)
            ? _rooms[_currentIndex] : null;

        public IReadOnlyList<RoomData> Rooms => _rooms;
        public int CurrentIndex => _currentIndex;
        public int TotalRooms   => _rooms?.Count ?? 0;
        public bool IsLastRoom  => _currentIndex >= (_rooms?.Count ?? 0) - 1;

        // Events
        public event System.Action<RoomData> OnRoomEntered;
        public event System.Action           OnFloorComplete;

        private void Start()
        {
            GenerateFloor();
        }

        /// <summary>Generates a new floor and enters the first room.</summary>
        public void GenerateFloor()
        {
            var run = MathRoguelikeGameManager.Instance.CurrentRun;
            _rooms = generator.GenerateFloor(run.currentFloor, roomsPerFloor);
            _currentIndex = 0;
            EnterCurrentRoom();
        }

        /// <summary>Advances to the next room.</summary>
        public void AdvanceToNextRoom()
        {
            if (IsLastRoom)
            {
                OnFloorComplete?.Invoke();
                MathRoguelikeGameManager.Instance.AdvanceFloor();
                return;
            }

            _currentIndex++;
            if (MathRoguelikeGameManager.Instance.CurrentRun != null)
                MathRoguelikeGameManager.Instance.CurrentRun.currentRoomIndex = _currentIndex;

            EnterCurrentRoom();
        }

        private void EnterCurrentRoom()
        {
            OnRoomEntered?.Invoke(CurrentRoom);
        }
    }
}
