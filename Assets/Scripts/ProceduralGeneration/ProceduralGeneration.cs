using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace UsefulScripts.ProceduralGeneration
{
    /// <summary>
    /// Noise generation utilities for procedural content.
    /// </summary>
    public static class NoiseGenerator
    {
        /// <summary>
        /// Generates a 2D Perlin noise map.
        /// </summary>
        public static float[,] GeneratePerlinNoiseMap(int width, int height, float scale, int octaves, 
            float persistence, float lacunarity, Vector2 offset, int seed)
        {
            float[,] noiseMap = new float[width, height];
            
            System.Random prng = new System.Random(seed);
            Vector2[] octaveOffsets = new Vector2[octaves];
            
            for (int i = 0; i < octaves; i++)
            {
                float offsetX = prng.Next(-100000, 100000) + offset.x;
                float offsetY = prng.Next(-100000, 100000) + offset.y;
                octaveOffsets[i] = new Vector2(offsetX, offsetY);
            }
            
            if (scale <= 0) scale = 0.0001f;
            
            float maxNoiseHeight = float.MinValue;
            float minNoiseHeight = float.MaxValue;
            
            float halfWidth = width / 2f;
            float halfHeight = height / 2f;
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float amplitude = 1;
                    float frequency = 1;
                    float noiseHeight = 0;
                    
                    for (int i = 0; i < octaves; i++)
                    {
                        float sampleX = (x - halfWidth + octaveOffsets[i].x) / scale * frequency;
                        float sampleY = (y - halfHeight + octaveOffsets[i].y) / scale * frequency;
                        
                        float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                        noiseHeight += perlinValue * amplitude;
                        
                        amplitude *= persistence;
                        frequency *= lacunarity;
                    }
                    
                    maxNoiseHeight = Mathf.Max(maxNoiseHeight, noiseHeight);
                    minNoiseHeight = Mathf.Min(minNoiseHeight, noiseHeight);
                    
                    noiseMap[x, y] = noiseHeight;
                }
            }
            
            // Normalize
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
                }
            }
            
            return noiseMap;
        }
        
        /// <summary>
        /// Generates simplex-like noise value at a point.
        /// </summary>
        public static float SimplexNoise(float x, float y, float z = 0)
        {
            // Simplified noise using multiple Perlin samples
            float value = 0;
            value += Mathf.PerlinNoise(x, y) * 0.5f;
            value += Mathf.PerlinNoise(x + 1.2f, y + 0.8f) * 0.25f;
            value += Mathf.PerlinNoise(x + z * 0.5f, y + z * 0.5f) * 0.25f;
            return value;
        }
        
        /// <summary>
        /// Generates Worley (cellular) noise.
        /// </summary>
        public static float[,] GenerateWorleyNoise(int width, int height, int numPoints, int seed, bool invert = false)
        {
            float[,] noiseMap = new float[width, height];
            System.Random prng = new System.Random(seed);
            
            Vector2[] points = new Vector2[numPoints];
            for (int i = 0; i < numPoints; i++)
            {
                points[i] = new Vector2(prng.Next(0, width), prng.Next(0, height));
            }
            
            float maxDist = Mathf.Sqrt(width * width + height * height);
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float minDist = float.MaxValue;
                    
                    foreach (var point in points)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), point);
                        minDist = Mathf.Min(minDist, dist);
                    }
                    
                    float normalized = minDist / maxDist;
                    noiseMap[x, y] = invert ? 1f - normalized : normalized;
                }
            }
            
            return noiseMap;
        }
        
        /// <summary>
        /// Generates ridged multi-fractal noise.
        /// </summary>
        public static float[,] GenerateRidgedNoise(int width, int height, float scale, int octaves, 
            float persistence, float lacunarity, Vector2 offset, int seed)
        {
            var baseNoise = GeneratePerlinNoiseMap(width, height, scale, octaves, persistence, lacunarity, offset, seed);
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Create ridges by using abs and inverting
                    baseNoise[x, y] = 1f - Mathf.Abs(baseNoise[x, y] * 2f - 1f);
                }
            }
            
            return baseNoise;
        }
    }
    
    /// <summary>
    /// Dungeon generation algorithms.
    /// </summary>
    public static class DungeonGenerator
    {
        /// <summary>
        /// Represents a room in the dungeon.
        /// </summary>
        [Serializable]
        public class Room
        {
            public RectInt bounds;
            public List<Vector2Int> tiles = new List<Vector2Int>();
            public List<Room> connectedRooms = new List<Room>();
            public RoomType roomType = RoomType.Normal;
            public int id;
            
            public Vector2Int Center => new Vector2Int(
                bounds.x + bounds.width / 2,
                bounds.y + bounds.height / 2
            );
            
            public bool Intersects(Room other, int spacing = 1)
            {
                return bounds.x - spacing < other.bounds.xMax + spacing &&
                       bounds.xMax + spacing > other.bounds.x - spacing &&
                       bounds.y - spacing < other.bounds.yMax + spacing &&
                       bounds.yMax + spacing > other.bounds.y - spacing;
            }
        }
        
        public enum RoomType
        {
            Normal,
            Start,
            End,
            Boss,
            Treasure,
            Shop,
            Secret
        }
        
        /// <summary>
        /// Generates a dungeon using BSP (Binary Space Partitioning).
        /// </summary>
        public static DungeonData GenerateBSPDungeon(int width, int height, int minRoomSize, int maxRoomSize, 
            int numIterations, int seed)
        {
            Random.InitState(seed);
            var dungeon = new DungeonData(width, height);
            
            // Start with full area
            var rootNode = new BSPNode(new RectInt(0, 0, width, height));
            
            // Split recursively
            SplitBSP(rootNode, minRoomSize, numIterations);
            
            // Create rooms in leaf nodes
            CreateRoomsInLeaves(rootNode, dungeon, minRoomSize, maxRoomSize);
            
            // Connect rooms
            ConnectRooms(rootNode, dungeon);
            
            // Assign room types
            AssignRoomTypes(dungeon);
            
            return dungeon;
        }
        
        private class BSPNode
        {
            public RectInt bounds;
            public BSPNode left;
            public BSPNode right;
            public Room room;
            
            public BSPNode(RectInt bounds)
            {
                this.bounds = bounds;
            }
            
            public bool IsLeaf => left == null && right == null;
        }
        
        private static void SplitBSP(BSPNode node, int minSize, int iterations)
        {
            if (iterations <= 0 || node.bounds.width < minSize * 2 || node.bounds.height < minSize * 2)
                return;
            
            bool splitHorizontally = Random.value > 0.5f;
            
            // Prefer splitting along longer axis
            if (node.bounds.width > node.bounds.height * 1.25f)
                splitHorizontally = false;
            else if (node.bounds.height > node.bounds.width * 1.25f)
                splitHorizontally = true;
            
            int max = (splitHorizontally ? node.bounds.height : node.bounds.width) - minSize;
            if (max <= minSize) return;
            
            int split = Random.Range(minSize, max);
            
            if (splitHorizontally)
            {
                node.left = new BSPNode(new RectInt(node.bounds.x, node.bounds.y, node.bounds.width, split));
                node.right = new BSPNode(new RectInt(node.bounds.x, node.bounds.y + split, node.bounds.width, node.bounds.height - split));
            }
            else
            {
                node.left = new BSPNode(new RectInt(node.bounds.x, node.bounds.y, split, node.bounds.height));
                node.right = new BSPNode(new RectInt(node.bounds.x + split, node.bounds.y, node.bounds.width - split, node.bounds.height));
            }
            
            SplitBSP(node.left, minSize, iterations - 1);
            SplitBSP(node.right, minSize, iterations - 1);
        }
        
        private static void CreateRoomsInLeaves(BSPNode node, DungeonData dungeon, int minSize, int maxSize)
        {
            if (node.IsLeaf)
            {
                int roomWidth = Random.Range(minSize, Mathf.Min(node.bounds.width - 2, maxSize));
                int roomHeight = Random.Range(minSize, Mathf.Min(node.bounds.height - 2, maxSize));
                int roomX = Random.Range(node.bounds.x + 1, node.bounds.xMax - roomWidth);
                int roomY = Random.Range(node.bounds.y + 1, node.bounds.yMax - roomHeight);
                
                var room = new Room
                {
                    bounds = new RectInt(roomX, roomY, roomWidth, roomHeight),
                    id = dungeon.rooms.Count
                };
                
                for (int x = roomX; x < roomX + roomWidth; x++)
                {
                    for (int y = roomY; y < roomY + roomHeight; y++)
                    {
                        room.tiles.Add(new Vector2Int(x, y));
                        dungeon.SetTile(x, y, TileType.Floor);
                    }
                }
                
                node.room = room;
                dungeon.rooms.Add(room);
            }
            else
            {
                if (node.left != null) CreateRoomsInLeaves(node.left, dungeon, minSize, maxSize);
                if (node.right != null) CreateRoomsInLeaves(node.right, dungeon, minSize, maxSize);
            }
        }
        
        private static void ConnectRooms(BSPNode node, DungeonData dungeon)
        {
            if (node.IsLeaf) return;
            
            ConnectRooms(node.left, dungeon);
            ConnectRooms(node.right, dungeon);
            
            var leftRoom = GetRoomFromNode(node.left);
            var rightRoom = GetRoomFromNode(node.right);
            
            if (leftRoom != null && rightRoom != null)
            {
                CreateCorridor(dungeon, leftRoom.Center, rightRoom.Center);
                leftRoom.connectedRooms.Add(rightRoom);
                rightRoom.connectedRooms.Add(leftRoom);
            }
        }
        
        private static Room GetRoomFromNode(BSPNode node)
        {
            if (node.room != null) return node.room;
            
            Room leftRoom = node.left != null ? GetRoomFromNode(node.left) : null;
            Room rightRoom = node.right != null ? GetRoomFromNode(node.right) : null;
            
            if (leftRoom == null) return rightRoom;
            if (rightRoom == null) return leftRoom;
            
            return Random.value > 0.5f ? leftRoom : rightRoom;
        }
        
        private static void CreateCorridor(DungeonData dungeon, Vector2Int from, Vector2Int to)
        {
            Vector2Int current = from;
            
            while (current.x != to.x)
            {
                dungeon.SetTile(current.x, current.y, TileType.Corridor);
                current.x += current.x < to.x ? 1 : -1;
            }
            
            while (current.y != to.y)
            {
                dungeon.SetTile(current.x, current.y, TileType.Corridor);
                current.y += current.y < to.y ? 1 : -1;
            }
        }
        
        private static void AssignRoomTypes(DungeonData dungeon)
        {
            if (dungeon.rooms.Count == 0) return;
            
            // First room is start
            dungeon.rooms[0].roomType = RoomType.Start;
            
            // Last room is end/boss
            if (dungeon.rooms.Count > 1)
            {
                dungeon.rooms[dungeon.rooms.Count - 1].roomType = RoomType.Boss;
            }
            
            // Assign treasure rooms randomly
            int treasureCount = Mathf.Max(1, dungeon.rooms.Count / 5);
            for (int i = 0; i < treasureCount && dungeon.rooms.Count > 3; i++)
            {
                int idx = Random.Range(1, dungeon.rooms.Count - 1);
                if (dungeon.rooms[idx].roomType == RoomType.Normal)
                {
                    dungeon.rooms[idx].roomType = RoomType.Treasure;
                }
            }
        }
        
        /// <summary>
        /// Generates a dungeon using Random Walk algorithm.
        /// </summary>
        public static DungeonData GenerateRandomWalkDungeon(int width, int height, int walkLength, 
            int numWalkers, int seed)
        {
            Random.InitState(seed);
            var dungeon = new DungeonData(width, height);
            
            Vector2Int startPos = new Vector2Int(width / 2, height / 2);
            HashSet<Vector2Int> floorPositions = new HashSet<Vector2Int>();
            
            for (int w = 0; w < numWalkers; w++)
            {
                Vector2Int position = startPos;
                
                for (int step = 0; step < walkLength; step++)
                {
                    floorPositions.Add(position);
                    position = position + GetRandomDirection();
                    position.x = Mathf.Clamp(position.x, 1, width - 2);
                    position.y = Mathf.Clamp(position.y, 1, height - 2);
                }
            }
            
            foreach (var pos in floorPositions)
            {
                dungeon.SetTile(pos.x, pos.y, TileType.Floor);
            }
            
            // Create rooms from clusters
            dungeon.DetectRooms();
            
            return dungeon;
        }
        
        private static Vector2Int GetRandomDirection()
        {
            int dir = Random.Range(0, 4);
            return dir switch
            {
                0 => Vector2Int.up,
                1 => Vector2Int.down,
                2 => Vector2Int.left,
                _ => Vector2Int.right
            };
        }
        
        /// <summary>
        /// Generates a dungeon using cellular automata (cave-like).
        /// </summary>
        public static DungeonData GenerateCaveDungeon(int width, int height, float fillProbability, 
            int smoothIterations, int seed)
        {
            Random.InitState(seed);
            var dungeon = new DungeonData(width, height);
            
            // Initialize randomly
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                    {
                        dungeon.SetTile(x, y, TileType.Wall);
                    }
                    else
                    {
                        dungeon.SetTile(x, y, Random.value < fillProbability ? TileType.Wall : TileType.Floor);
                    }
                }
            }
            
            // Smooth using cellular automata rules
            for (int i = 0; i < smoothIterations; i++)
            {
                SmoothMap(dungeon, width, height);
            }
            
            dungeon.DetectRooms();
            
            return dungeon;
        }
        
        private static void SmoothMap(DungeonData dungeon, int width, int height)
        {
            TileType[,] newTiles = new TileType[width, height];
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int wallCount = CountWallNeighbors(dungeon, x, y, width, height);
                    
                    if (wallCount > 4)
                        newTiles[x, y] = TileType.Wall;
                    else if (wallCount < 4)
                        newTiles[x, y] = TileType.Floor;
                    else
                        newTiles[x, y] = dungeon.GetTile(x, y);
                }
            }
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    dungeon.SetTile(x, y, newTiles[x, y]);
                }
            }
        }
        
        private static int CountWallNeighbors(DungeonData dungeon, int x, int y, int width, int height)
        {
            int count = 0;
            
            for (int nx = x - 1; nx <= x + 1; nx++)
            {
                for (int ny = y - 1; ny <= y + 1; ny++)
                {
                    if (nx == x && ny == y) continue;
                    
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                    {
                        count++;
                    }
                    else if (dungeon.GetTile(nx, ny) == TileType.Wall)
                    {
                        count++;
                    }
                }
            }
            
            return count;
        }
    }
    
    public enum TileType
    {
        Empty,
        Wall,
        Floor,
        Corridor,
        Door,
        Stairs,
        Water,
        Lava,
        Trap
    }
    
    /// <summary>
    /// Contains the generated dungeon data.
    /// </summary>
    [Serializable]
    public class DungeonData
    {
        public int width;
        public int height;
        public TileType[,] tiles;
        public List<DungeonGenerator.Room> rooms = new List<DungeonGenerator.Room>();
        
        public DungeonData(int width, int height)
        {
            this.width = width;
            this.height = height;
            tiles = new TileType[width, height];
            
            // Initialize with walls
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    tiles[x, y] = TileType.Wall;
                }
            }
        }
        
        public void SetTile(int x, int y, TileType type)
        {
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                tiles[x, y] = type;
            }
        }
        
        public TileType GetTile(int x, int y)
        {
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                return tiles[x, y];
            }
            return TileType.Empty;
        }
        
        public bool IsWalkable(int x, int y)
        {
            TileType tile = GetTile(x, y);
            return tile == TileType.Floor || tile == TileType.Corridor || tile == TileType.Door;
        }
        
        public void DetectRooms()
        {
            bool[,] visited = new bool[width, height];
            rooms.Clear();
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (!visited[x, y] && IsWalkable(x, y))
                    {
                        var room = FloodFillRoom(x, y, visited);
                        if (room.tiles.Count > 9) // Minimum room size
                        {
                            room.id = rooms.Count;
                            rooms.Add(room);
                        }
                    }
                }
            }
        }
        
        private DungeonGenerator.Room FloodFillRoom(int startX, int startY, bool[,] visited)
        {
            var room = new DungeonGenerator.Room();
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(new Vector2Int(startX, startY));
            
            int minX = startX, maxX = startX;
            int minY = startY, maxY = startY;
            
            while (queue.Count > 0)
            {
                var pos = queue.Dequeue();
                
                if (pos.x < 0 || pos.x >= width || pos.y < 0 || pos.y >= height)
                    continue;
                if (visited[pos.x, pos.y] || !IsWalkable(pos.x, pos.y))
                    continue;
                
                visited[pos.x, pos.y] = true;
                room.tiles.Add(pos);
                
                minX = Mathf.Min(minX, pos.x);
                maxX = Mathf.Max(maxX, pos.x);
                minY = Mathf.Min(minY, pos.y);
                maxY = Mathf.Max(maxY, pos.y);
                
                queue.Enqueue(new Vector2Int(pos.x + 1, pos.y));
                queue.Enqueue(new Vector2Int(pos.x - 1, pos.y));
                queue.Enqueue(new Vector2Int(pos.x, pos.y + 1));
                queue.Enqueue(new Vector2Int(pos.x, pos.y - 1));
            }
            
            room.bounds = new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
            return room;
        }
    }
    
    /// <summary>
    /// Terrain generation utilities.
    /// </summary>
    public static class TerrainGenerator
    {
        /// <summary>
        /// Represents a biome type.
        /// </summary>
        [Serializable]
        public class BiomeType
        {
            public string name;
            public float minHeight;
            public float maxHeight;
            public float minMoisture;
            public float maxMoisture;
            public Color color;
            
            public bool Matches(float height, float moisture)
            {
                return height >= minHeight && height <= maxHeight &&
                       moisture >= minMoisture && moisture <= maxMoisture;
            }
        }
        
        /// <summary>
        /// Generates a height map for terrain.
        /// </summary>
        public static float[,] GenerateHeightMap(int width, int height, TerrainSettings settings)
        {
            return NoiseGenerator.GeneratePerlinNoiseMap(
                width, height,
                settings.noiseScale,
                settings.octaves,
                settings.persistence,
                settings.lacunarity,
                settings.offset,
                settings.seed
            );
        }
        
        /// <summary>
        /// Generates a biome map based on height and moisture.
        /// </summary>
        public static int[,] GenerateBiomeMap(float[,] heightMap, float[,] moistureMap, List<BiomeType> biomes)
        {
            int width = heightMap.GetLength(0);
            int height = heightMap.GetLength(1);
            int[,] biomeMap = new int[width, height];
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float h = heightMap[x, y];
                    float m = moistureMap[x, y];
                    
                    for (int i = 0; i < biomes.Count; i++)
                    {
                        if (biomes[i].Matches(h, m))
                        {
                            biomeMap[x, y] = i;
                            break;
                        }
                    }
                }
            }
            
            return biomeMap;
        }
        
        /// <summary>
        /// Applies hydraulic erosion to a height map.
        /// </summary>
        public static void ApplyErosion(float[,] heightMap, int iterations, float erosionStrength)
        {
            int width = heightMap.GetLength(0);
            int height = heightMap.GetLength(1);
            
            for (int iter = 0; iter < iterations; iter++)
            {
                int x = Random.Range(1, width - 1);
                int y = Random.Range(1, height - 1);
                
                float sediment = 0;
                float speed = 1;
                float water = 1;
                
                for (int step = 0; step < 30; step++)
                {
                    if (x <= 0 || x >= width - 1 || y <= 0 || y >= height - 1) break;
                    
                    float currentHeight = heightMap[x, y];
                    
                    // Find lowest neighbor
                    int lowestX = x, lowestY = y;
                    float lowestHeight = currentHeight;
                    
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            
                            float neighborHeight = heightMap[x + dx, y + dy];
                            if (neighborHeight < lowestHeight)
                            {
                                lowestHeight = neighborHeight;
                                lowestX = x + dx;
                                lowestY = y + dy;
                            }
                        }
                    }
                    
                    float heightDiff = currentHeight - lowestHeight;
                    
                    if (heightDiff <= 0)
                    {
                        // Deposit sediment
                        heightMap[x, y] += sediment;
                        break;
                    }
                    
                    // Erode and carry sediment
                    float erosionAmount = Mathf.Min(heightDiff, erosionStrength * speed * water);
                    heightMap[x, y] -= erosionAmount;
                    sediment += erosionAmount;
                    
                    // Move water droplet
                    x = lowestX;
                    y = lowestY;
                    speed = Mathf.Sqrt(speed * speed + heightDiff);
                    water *= 0.99f;
                }
            }
        }
    }
    
    /// <summary>
    /// Settings for terrain generation.
    /// </summary>
    [Serializable]
    public class TerrainSettings
    {
        public float noiseScale = 50f;
        public int octaves = 4;
        public float persistence = 0.5f;
        public float lacunarity = 2f;
        public Vector2 offset = Vector2.zero;
        public int seed = 12345;
        public float heightMultiplier = 10f;
        public AnimationCurve heightCurve = AnimationCurve.Linear(0, 0, 1, 1);
    }
    
    /// <summary>
    /// Name generation utilities.
    /// </summary>
    public static class NameGenerator
    {
        private static readonly string[] Prefixes = { "Al", "Bel", "Car", "Dra", "El", "Fal", "Gor", "Hal", "Il", "Jar", 
            "Kal", "Lor", "Mal", "Nor", "Or", "Pir", "Qua", "Ral", "Sar", "Tal", "Ul", "Val", "Wor", "Xan", "Yar", "Zar" };
        
        private static readonly string[] Middles = { "an", "ar", "as", "en", "er", "es", "in", "ir", "is", "on", "or", "os", 
            "un", "ur", "us", "ae", "ai", "ao", "ea", "ei", "eo", "ia", "io", "oa", "oi", "ua", "ue" };
        
        private static readonly string[] Suffixes = { "a", "ah", "ar", "as", "ax", "el", "en", "er", "ia", "iel", "ien", 
            "ion", "ius", "ix", "on", "or", "os", "th", "us", "yn" };
        
        private static readonly string[] TownPrefixes = { "Black", "White", "Green", "Red", "Blue", "Gold", "Silver", 
            "Iron", "Stone", "Wood", "River", "Lake", "Mountain", "Valley", "Shadow", "Sun", "Moon", "Star", "Dark", "Bright" };
        
        private static readonly string[] TownSuffixes = { "ton", "ville", "burg", "berg", "ford", "port", "haven", 
            "gate", "hold", "keep", "watch", "guard", "helm", "hollow", "vale", "dale", "wood", "field", "meadow", "shore" };
        
        /// <summary>
        /// Generates a random fantasy name.
        /// </summary>
        public static string GenerateName(int seed = -1)
        {
            if (seed >= 0) Random.InitState(seed);
            
            string name = Prefixes[Random.Range(0, Prefixes.Length)];
            
            if (Random.value > 0.5f)
            {
                name += Middles[Random.Range(0, Middles.Length)];
            }
            
            name += Suffixes[Random.Range(0, Suffixes.Length)];
            
            return name;
        }
        
        /// <summary>
        /// Generates a random town/city name.
        /// </summary>
        public static string GenerateTownName(int seed = -1)
        {
            if (seed >= 0) Random.InitState(seed);
            
            return TownPrefixes[Random.Range(0, TownPrefixes.Length)] + 
                   TownSuffixes[Random.Range(0, TownSuffixes.Length)];
        }
        
        /// <summary>
        /// Generates multiple unique names.
        /// </summary>
        public static List<string> GenerateNames(int count, int baseSeed = 0)
        {
            HashSet<string> names = new HashSet<string>();
            
            while (names.Count < count)
            {
                names.Add(GenerateName(baseSeed + names.Count));
            }
            
            return names.ToList();
        }
    }
    
    /// <summary>
    /// Loot table and item generation utilities.
    /// </summary>
    public static class LootGenerator
    {
        /// <summary>
        /// Represents an item in a loot table.
        /// </summary>
        [Serializable]
        public class LootEntry
        {
            public string itemId;
            public float weight = 1f;
            public int minQuantity = 1;
            public int maxQuantity = 1;
            public int minLevel = 1;
            public int maxLevel = 100;
            public float rarity = 1f;
        }
        
        /// <summary>
        /// Represents a loot table.
        /// </summary>
        [Serializable]
        public class LootTable
        {
            public string tableId;
            public List<LootEntry> entries = new List<LootEntry>();
            public int minDrops = 1;
            public int maxDrops = 3;
            public float nothingChance = 0.1f;
            
            public float TotalWeight => entries.Sum(e => e.weight);
        }
        
        /// <summary>
        /// Result of loot generation.
        /// </summary>
        public struct LootResult
        {
            public string itemId;
            public int quantity;
        }
        
        /// <summary>
        /// Generates loot from a loot table.
        /// </summary>
        public static List<LootResult> GenerateLoot(LootTable table, int playerLevel, float luckModifier = 1f)
        {
            var results = new List<LootResult>();
            
            if (Random.value < table.nothingChance / luckModifier)
            {
                return results;
            }
            
            int numDrops = Random.Range(table.minDrops, table.maxDrops + 1);
            
            // Filter entries by level
            var validEntries = table.entries.Where(e => 
                playerLevel >= e.minLevel && playerLevel <= e.maxLevel).ToList();
            
            if (validEntries.Count == 0) return results;
            
            float totalWeight = validEntries.Sum(e => e.weight * (e.rarity * luckModifier));
            
            for (int i = 0; i < numDrops; i++)
            {
                float roll = Random.Range(0f, totalWeight);
                float cumWeight = 0f;
                
                foreach (var entry in validEntries)
                {
                    cumWeight += entry.weight * (entry.rarity * luckModifier);
                    
                    if (roll <= cumWeight)
                    {
                        results.Add(new LootResult
                        {
                            itemId = entry.itemId,
                            quantity = Random.Range(entry.minQuantity, entry.maxQuantity + 1)
                        });
                        break;
                    }
                }
            }
            
            // Combine duplicates
            return results.GroupBy(r => r.itemId)
                         .Select(g => new LootResult { itemId = g.Key, quantity = g.Sum(r => r.quantity) })
                         .ToList();
        }
    }
    
    /// <summary>
    /// Wave function collapse for tile-based generation.
    /// </summary>
    public static class WaveFunctionCollapse
    {
        /// <summary>
        /// Represents a tile pattern.
        /// </summary>
        [Serializable]
        public class TilePattern
        {
            public int tileId;
            public string name;
            public float weight = 1f;
            public int[] allowedTop;
            public int[] allowedBottom;
            public int[] allowedLeft;
            public int[] allowedRight;
        }
        
        /// <summary>
        /// Cell in the WFC grid.
        /// </summary>
        private class Cell
        {
            public HashSet<int> possibleTiles = new HashSet<int>();
            public int? collapsedTile = null;
            public int x, y;
            
            public bool IsCollapsed => collapsedTile.HasValue;
            public int Entropy => possibleTiles.Count;
        }
        
        /// <summary>
        /// Generates a tilemap using wave function collapse.
        /// </summary>
        public static int[,] Generate(int width, int height, List<TilePattern> patterns, int seed)
        {
            Random.InitState(seed);
            
            Cell[,] grid = new Cell[width, height];
            int[,] result = new int[width, height];
            
            // Initialize all cells with all possibilities
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    grid[x, y] = new Cell { x = x, y = y };
                    for (int i = 0; i < patterns.Count; i++)
                    {
                        grid[x, y].possibleTiles.Add(i);
                    }
                }
            }
            
            // Main loop
            while (true)
            {
                // Find cell with lowest entropy
                Cell lowestEntropy = null;
                
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        var cell = grid[x, y];
                        if (cell.IsCollapsed) continue;
                        
                        if (lowestEntropy == null || cell.Entropy < lowestEntropy.Entropy)
                        {
                            lowestEntropy = cell;
                        }
                    }
                }
                
                if (lowestEntropy == null) break; // All cells collapsed
                
                if (lowestEntropy.possibleTiles.Count == 0)
                {
                    // Contradiction - restart or backtrack
                    Debug.LogWarning("WFC: Contradiction reached");
                    break;
                }
                
                // Collapse the cell (weighted random)
                float totalWeight = lowestEntropy.possibleTiles.Sum(t => patterns[t].weight);
                float roll = Random.Range(0f, totalWeight);
                float cumWeight = 0f;
                
                foreach (int tileIdx in lowestEntropy.possibleTiles)
                {
                    cumWeight += patterns[tileIdx].weight;
                    if (roll <= cumWeight)
                    {
                        lowestEntropy.collapsedTile = tileIdx;
                        lowestEntropy.possibleTiles.Clear();
                        lowestEntropy.possibleTiles.Add(tileIdx);
                        break;
                    }
                }
                
                // Propagate constraints
                PropagateConstraints(grid, width, height, lowestEntropy, patterns);
            }
            
            // Build result
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    result[x, y] = grid[x, y].collapsedTile ?? 0;
                }
            }
            
            return result;
        }
        
        private static void PropagateConstraints(Cell[,] grid, int width, int height, Cell cell, List<TilePattern> patterns)
        {
            var stack = new Stack<Cell>();
            stack.Push(cell);
            
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                
                // Check all neighbors
                var neighbors = new (int dx, int dy, Func<TilePattern, int[]> getConstraint)[]
                {
                    (-1, 0, p => p.allowedLeft),
                    (1, 0, p => p.allowedRight),
                    (0, -1, p => p.allowedBottom),
                    (0, 1, p => p.allowedTop)
                };
                
                foreach (var (dx, dy, getConstraint) in neighbors)
                {
                    int nx = current.x + dx;
                    int ny = current.y + dy;
                    
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                    
                    var neighbor = grid[nx, ny];
                    if (neighbor.IsCollapsed) continue;
                    
                    // Build set of valid tiles for neighbor
                    HashSet<int> validForNeighbor = new HashSet<int>();
                    
                    foreach (int currentTile in current.possibleTiles)
                    {
                        var constraint = getConstraint(patterns[currentTile]);
                        if (constraint != null)
                        {
                            foreach (int allowed in constraint)
                            {
                                validForNeighbor.Add(allowed);
                            }
                        }
                    }
                    
                    // Remove invalid options from neighbor
                    int beforeCount = neighbor.possibleTiles.Count;
                    neighbor.possibleTiles.IntersectWith(validForNeighbor);
                    
                    if (neighbor.possibleTiles.Count < beforeCount)
                    {
                        stack.Push(neighbor);
                    }
                }
            }
        }
    }
}