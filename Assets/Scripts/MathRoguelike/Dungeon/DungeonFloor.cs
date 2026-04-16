using System.Collections.Generic;

namespace UsefulScripts.MathRoguelike.Dungeon
{
    /// <summary>
    /// Represents one fully-generated floor (an ordered list of rooms).
    /// Created by <see cref="DungeonGenerator"/> and consumed by <see cref="Core.FloorManager"/>.
    /// </summary>
    public class DungeonFloor
    {
        public int FloorNumber { get; }
        public IReadOnlyList<RoomData> Rooms { get; }

        public DungeonFloor(int floorNumber, List<RoomData> rooms)
        {
            FloorNumber = floorNumber;
            Rooms       = rooms.AsReadOnly();
        }

        public int  RoomCount                     => Rooms.Count;
        public bool IsValidIndex(int index)        => index >= 0 && index < RoomCount;
        public RoomData GetRoom(int index)         => IsValidIndex(index) ? Rooms[index] : null;
        public RoomData BossRoom                   => Rooms[RoomCount - 1];
    }
}
