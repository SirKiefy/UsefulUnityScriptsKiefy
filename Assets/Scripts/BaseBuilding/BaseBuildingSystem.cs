using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UsefulScripts.BaseBuilding
{
    #region Enums

    /// <summary>
    /// Defines the category of building pieces.
    /// </summary>
    public enum BuildingCategory
    {
        Foundation,         // Base structure that sits on ground
        Floor,              // Horizontal surfaces (can be walked on)
        Wall,               // Vertical barriers
        WallFrame,          // Wall with opening for doors/windows
        Door,               // Placeable in wall frames
        Window,             // Placeable in wall frames
        Roof,               // Angled roof pieces
        RoofFlat,           // Flat roof/ceiling pieces
        Stairs,             // Vertical traversal
        Ramp,               // Sloped surface for movement
        Fence,              // Half-height walls
        Pillar,             // Vertical support columns
        Beam,               // Horizontal support beams
        Decoration,         // Non-structural decorative pieces
        Functional          // Crafting stations, storage, etc.
    }

    /// <summary>
    /// Defines the material tier of building pieces.
    /// </summary>
    public enum BuildingMaterial
    {
        Thatch,             // Tier 0 - Very weak, cheap
        Wood,               // Tier 1 - Weak but accessible
        Stone,              // Tier 2 - Moderate durability
        Metal,              // Tier 3 - Strong
        Armored,            // Tier 4 - Very strong
        Concrete,           // Tier 5 - Maximum durability
        Glass               // Special - Transparent but fragile
    }

    /// <summary>
    /// Defines the socket type for snapping building pieces.
    /// </summary>
    public enum SocketType
    {
        None,               // No snapping
        Foundation,         // Foundation-to-foundation
        Floor,              // Floor connections
        WallBottom,         // Bottom of walls
        WallTop,            // Top of walls
        WallSide,           // Side-to-side wall connections
        DoorFrame,          // Door/window frames
        RoofEdge,           // Roof edge connections
        RoofPeak,           // Roof peak connections
        Pillar,             // Pillar connections
        Ceiling,            // Ceiling attachments
        Universal           // Connects to anything
    }

    /// <summary>
    /// Defines the current state of a building piece.
    /// </summary>
    public enum BuildingState
    {
        Preview,            // Ghost/preview mode
        Constructing,       // Being built
        Complete,           // Fully constructed
        Damaged,            // Has taken damage
        Decaying,           // Losing health over time
        Destroyed           // About to be removed
    }

    /// <summary>
    /// Defines placement validation results.
    /// </summary>
    public enum PlacementResult
    {
        Valid,              // Can be placed
        InvalidTerrain,     // Terrain doesn't support placement
        Overlapping,        // Overlaps with existing structure
        NoSnapPoint,        // No valid snap point found
        NoSupport,          // Would have no structural support
        TooFar,             // Too far from player
        Blocked,            // Something blocking placement
        InsufficientResources,  // Not enough materials
        InvalidOwnership,   // Can't build on others' territory
        InvalidAngle        // Surface angle too steep
    }

    #endregion

    #region Data Classes

    /// <summary>
    /// Defines the resource cost for building or upgrading.
    /// </summary>
    [Serializable]
    public class BuildingCost
    {
        public string resourceId;
        public string resourceName;
        public int amount;

        public BuildingCost(string id, string name, int qty)
        {
            resourceId = id;
            resourceName = name;
            amount = qty;
        }
    }

    /// <summary>
    /// Defines a socket point on a building piece for snapping.
    /// </summary>
    [Serializable]
    public class BuildingSocket
    {
        public string socketId;
        public SocketType socketType;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public List<BuildingCategory> acceptedCategories = new List<BuildingCategory>();
        public bool isOccupied;
        public string occupiedByPieceId;

        public BuildingSocket(string id, SocketType type, Vector3 pos)
        {
            socketId = id;
            socketType = type;
            localPosition = pos;
            localRotation = Quaternion.identity;
        }

        /// <summary>
        /// Checks if this socket can accept a specific building category.
        /// </summary>
        public bool CanAccept(BuildingCategory category)
        {
            if (isOccupied) return false;
            if (acceptedCategories.Count == 0) return true;
            return acceptedCategories.Contains(category);
        }
    }

    /// <summary>
    /// Represents an upgrade path for a building piece.
    /// </summary>
    [Serializable]
    public class BuildingUpgrade
    {
        public BuildingMaterial targetMaterial;
        public List<BuildingCost> costs = new List<BuildingCost>();
        public float upgradeTime = 5f;
        public float healthMultiplier = 1.5f;
        public string upgradedPrefabName;

        public BuildingUpgrade(BuildingMaterial material)
        {
            targetMaterial = material;
        }
    }

    /// <summary>
    /// ScriptableObject defining a buildable structure piece.
    /// </summary>
    [CreateAssetMenu(fileName = "NewBuildable", menuName = "UsefulScripts/BaseBuilding/BuildableData")]
    public class BuildableData : ScriptableObject
    {
        [Header("Basic Info")]
        public string buildableId;
        public string displayName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;
        public BuildingCategory category;
        public BuildingMaterial material;

        [Header("Prefabs")]
        public GameObject prefab;
        public GameObject previewPrefab;
        public GameObject constructionPrefab;

        [Header("Stats")]
        public float maxHealth = 100f;
        public float constructionTime = 3f;
        public bool isDestructible = true;
        public bool canDecay = true;
        public float decayRate = 0.1f;          // Health loss per hour when decaying

        [Header("Placement")]
        public Vector3 size = Vector3.one;
        public float rotationSnap = 90f;         // Degrees to snap rotation
        public bool requiresFoundation = false;
        public bool providesSupport = true;
        public float maxPlacementDistance = 10f;
        public float groundOffset = 0f;
        public LayerMask placementBlockers;
        public float maxSlopeAngle = 45f;

        [Header("Sockets")]
        public List<BuildingSocket> sockets = new List<BuildingSocket>();
        public SocketType[] compatibleSnapTypes;

        [Header("Cost")]
        public List<BuildingCost> buildCosts = new List<BuildingCost>();
        public List<BuildingCost> repairCosts = new List<BuildingCost>();
        public float repairHealthPercent = 0.5f;    // How much health is restored per repair

        [Header("Upgrades")]
        public List<BuildingUpgrade> availableUpgrades = new List<BuildingUpgrade>();

        [Header("Audio/VFX")]
        public string placeSoundId;
        public string constructSoundId;
        public string destroySoundId;
        public string hitSoundId;
        public GameObject constructionVFX;
        public GameObject destroyVFX;

        /// <summary>
        /// Gets the socket at a specific local position.
        /// </summary>
        public BuildingSocket GetSocketAt(Vector3 localPosition, float tolerance = 0.1f)
        {
            return sockets.FirstOrDefault(s =>
                Vector3.Distance(s.localPosition, localPosition) <= tolerance);
        }

        /// <summary>
        /// Gets all sockets of a specific type.
        /// </summary>
        public List<BuildingSocket> GetSocketsByType(SocketType type)
        {
            return sockets.Where(s => s.socketType == type).ToList();
        }
    }

    /// <summary>
    /// Represents an active building piece in the world.
    /// </summary>
    [Serializable]
    public class BuildingPiece
    {
        public string instanceId;
        public BuildableData data;
        public GameObject gameObject;
        public Transform transform;
        public BuildingState state;
        public BuildingMaterial currentMaterial;

        public float currentHealth;
        public float maxHealth;
        public float constructionProgress;
        public float decayTimer;

        public string ownerId;
        public string ownerName;
        public DateTime placedTime;
        public DateTime lastInteractionTime;

        public Vector3 position;
        public Quaternion rotation;
        public Vector3Int gridPosition;

        public string parentPieceId;
        public List<string> connectedPieceIds = new List<string>();
        public List<BuildingSocket> sockets = new List<BuildingSocket>();
        public bool hasSupport;
        public float stabilityValue;

        public BuildingPiece(BuildableData buildableData, Vector3 pos, Quaternion rot)
        {
            instanceId = Guid.NewGuid().ToString();
            data = buildableData;
            state = BuildingState.Preview;
            currentMaterial = buildableData.material;

            maxHealth = buildableData.maxHealth;
            currentHealth = maxHealth;
            constructionProgress = 0f;
            decayTimer = 0f;

            position = pos;
            rotation = rot;
            placedTime = DateTime.Now;
            lastInteractionTime = DateTime.Now;

            // Clone sockets from data
            sockets = buildableData.sockets.Select(s => new BuildingSocket(
                s.socketId, s.socketType, s.localPosition)
            {
                localRotation = s.localRotation,
                acceptedCategories = new List<BuildingCategory>(s.acceptedCategories)
            }).ToList();

            hasSupport = true;
            stabilityValue = 1f;
        }

        /// <summary>
        /// Gets the health percentage (0-1).
        /// </summary>
        public float HealthPercent => maxHealth > 0 ? currentHealth / maxHealth : 0f;

        /// <summary>
        /// Gets whether the piece is fully constructed.
        /// </summary>
        public bool IsComplete => state == BuildingState.Complete;

        /// <summary>
        /// Gets whether the piece can be interacted with.
        /// </summary>
        public bool CanInteract => state == BuildingState.Complete ||
                                    state == BuildingState.Damaged ||
                                    state == BuildingState.Decaying;

        /// <summary>
        /// Applies damage to the building piece.
        /// </summary>
        public float TakeDamage(float damage)
        {
            if (!data.isDestructible) return 0f;

            float actualDamage = Mathf.Min(damage, currentHealth);
            currentHealth -= actualDamage;

            if (currentHealth <= 0)
            {
                state = BuildingState.Destroyed;
            }
            else if (currentHealth < maxHealth * 0.5f)
            {
                state = BuildingState.Damaged;
            }

            return actualDamage;
        }

        /// <summary>
        /// Repairs the building piece.
        /// </summary>
        public float Repair(float amount)
        {
            float toRepair = Mathf.Min(amount, maxHealth - currentHealth);
            currentHealth += toRepair;

            if (currentHealth >= maxHealth)
            {
                currentHealth = maxHealth;
                state = BuildingState.Complete;
            }
            else if (currentHealth >= maxHealth * 0.5f)
            {
                state = BuildingState.Complete;
            }

            lastInteractionTime = DateTime.Now;
            return toRepair;
        }

        /// <summary>
        /// Gets an available socket that can accept the specified category.
        /// </summary>
        public BuildingSocket GetAvailableSocket(BuildingCategory category)
        {
            return sockets.FirstOrDefault(s => s.CanAccept(category));
        }

        /// <summary>
        /// Marks a socket as occupied.
        /// </summary>
        public void OccupySocket(string socketId, string connectedPieceId)
        {
            var socket = sockets.FirstOrDefault(s => s.socketId == socketId);
            if (socket != null)
            {
                socket.isOccupied = true;
                socket.occupiedByPieceId = connectedPieceId;
                if (!connectedPieceIds.Contains(connectedPieceId))
                {
                    connectedPieceIds.Add(connectedPieceId);
                }
            }
        }

        /// <summary>
        /// Frees a socket when a connected piece is removed.
        /// </summary>
        public void FreeSocket(string socketId)
        {
            var socket = sockets.FirstOrDefault(s => s.socketId == socketId);
            if (socket != null)
            {
                if (!string.IsNullOrEmpty(socket.occupiedByPieceId))
                {
                    connectedPieceIds.Remove(socket.occupiedByPieceId);
                }
                socket.isOccupied = false;
                socket.occupiedByPieceId = null;
            }
        }
    }

    /// <summary>
    /// Holds snap information for placement preview.
    /// </summary>
    public class SnapInfo
    {
        public bool isSnapped;
        public BuildingPiece snapToPiece;
        public BuildingSocket socket;
        public Vector3 snapPosition;
        public Quaternion snapRotation;
    }

    /// <summary>
    /// Result of a building placement attempt.
    /// </summary>
    public class PlacementInfo
    {
        public PlacementResult result;
        public string message;
        public Vector3 position;
        public Quaternion rotation;
        public SnapInfo snapInfo;
        public List<BuildingCost> missingResources;

        public bool IsValid => result == PlacementResult.Valid;
    }

    /// <summary>
    /// Result of a building destruction.
    /// </summary>
    public class DestructionResult
    {
        public BuildingPiece destroyedPiece;
        public List<BuildingPiece> collapsedPieces;
        public Dictionary<string, int> returnedResources;
        public float totalDamageDealt;
    }

    /// <summary>
    /// Grid cell for spatial organization.
    /// </summary>
    [Serializable]
    public class BuildingGridCell
    {
        public Vector3Int gridPosition;
        public List<string> pieceIds = new List<string>();
        public bool isOccupied => pieceIds.Count > 0;
    }

    #endregion

    #region Building Preview

    /// <summary>
    /// Handles the ghost/preview visualization for building placement.
    /// </summary>
    public class BuildingPreview : MonoBehaviour
    {
        [Header("Materials")]
        [SerializeField] private Material validPreviewMaterial;
        [SerializeField] private Material invalidPreviewMaterial;
        [SerializeField] private Color validColor = new Color(0f, 1f, 0f, 0.5f);
        [SerializeField] private Color invalidColor = new Color(1f, 0f, 0f, 0.5f);

        private GameObject previewObject;
        private BuildableData currentBuildable;
        private MeshRenderer[] renderers;
        private bool isVisible;
        private bool isValid;

        /// <summary>
        /// Gets the current preview object.
        /// </summary>
        public GameObject PreviewObject => previewObject;

        /// <summary>
        /// Gets whether the preview is currently visible.
        /// </summary>
        public bool IsVisible => isVisible;

        /// <summary>
        /// Shows the preview for a buildable.
        /// </summary>
        public void Show(BuildableData buildable, Vector3 position, Quaternion rotation)
        {
            if (currentBuildable != buildable)
            {
                Hide();
                currentBuildable = buildable;
                CreatePreviewObject();
            }

            if (previewObject != null)
            {
                previewObject.transform.position = position;
                previewObject.transform.rotation = rotation;
                previewObject.SetActive(true);
                isVisible = true;
            }
        }

        /// <summary>
        /// Hides the preview.
        /// </summary>
        public void Hide()
        {
            if (previewObject != null)
            {
                Destroy(previewObject);
                previewObject = null;
            }
            currentBuildable = null;
            isVisible = false;
        }

        /// <summary>
        /// Updates the preview position and rotation.
        /// </summary>
        public void UpdateTransform(Vector3 position, Quaternion rotation)
        {
            if (previewObject != null)
            {
                previewObject.transform.position = position;
                previewObject.transform.rotation = rotation;
            }
        }

        /// <summary>
        /// Sets the validity visual state.
        /// </summary>
        public void SetValid(bool valid)
        {
            if (isValid == valid) return;
            isValid = valid;

            if (renderers == null) return;

            Material mat = valid ? validPreviewMaterial : invalidPreviewMaterial;
            Color color = valid ? validColor : invalidColor;

            foreach (var renderer in renderers)
            {
                if (mat != null)
                {
                    renderer.material = mat;
                }
                else
                {
                    // Create simple transparent material
                    var material = new Material(Shader.Find("Standard"));
                    material.SetFloat("_Mode", 3);
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.EnableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = 3000;
                    material.color = color;
                    renderer.material = material;
                }
            }
        }

        private void CreatePreviewObject()
        {
            if (currentBuildable == null) return;

            // Use preview prefab if available, otherwise use main prefab
            GameObject prefab = currentBuildable.previewPrefab != null
                ? currentBuildable.previewPrefab
                : currentBuildable.prefab;

            if (prefab == null) return;

            previewObject = Instantiate(prefab);
            previewObject.name = $"Preview_{currentBuildable.displayName}";

            // Disable colliders and rigidbodies
            foreach (var collider in previewObject.GetComponentsInChildren<Collider>())
            {
                collider.enabled = false;
            }
            foreach (var rb in previewObject.GetComponentsInChildren<Rigidbody>())
            {
                Destroy(rb);
            }

            // Get renderers for material changes
            renderers = previewObject.GetComponentsInChildren<MeshRenderer>();
            isValid = true;
            SetValid(true);
        }

        private void OnDestroy()
        {
            Hide();
        }
    }

    #endregion

    #region Building Grid

    /// <summary>
    /// Manages the spatial grid for building organization and snapping.
    /// </summary>
    public class BuildingGrid
    {
        private readonly float cellSize;
        private readonly Dictionary<Vector3Int, BuildingGridCell> cells = new Dictionary<Vector3Int, BuildingGridCell>();

        public float CellSize => cellSize;

        public BuildingGrid(float size = 1f)
        {
            cellSize = size;
        }

        /// <summary>
        /// Converts a world position to grid position.
        /// </summary>
        public Vector3Int WorldToGrid(Vector3 worldPosition)
        {
            return new Vector3Int(
                Mathf.FloorToInt(worldPosition.x / cellSize),
                Mathf.FloorToInt(worldPosition.y / cellSize),
                Mathf.FloorToInt(worldPosition.z / cellSize)
            );
        }

        /// <summary>
        /// Converts a grid position to world position (center of cell).
        /// </summary>
        public Vector3 GridToWorld(Vector3Int gridPosition)
        {
            return new Vector3(
                (gridPosition.x + 0.5f) * cellSize,
                gridPosition.y * cellSize,
                (gridPosition.z + 0.5f) * cellSize
            );
        }

        /// <summary>
        /// Snaps a world position to the nearest grid point.
        /// </summary>
        public Vector3 SnapToGrid(Vector3 worldPosition)
        {
            return GridToWorld(WorldToGrid(worldPosition));
        }

        /// <summary>
        /// Adds a piece to the grid.
        /// </summary>
        public void AddPiece(BuildingPiece piece)
        {
            Vector3Int gridPos = WorldToGrid(piece.position);
            piece.gridPosition = gridPos;

            if (!cells.TryGetValue(gridPos, out var cell))
            {
                cell = new BuildingGridCell { gridPosition = gridPos };
                cells[gridPos] = cell;
            }

            if (!cell.pieceIds.Contains(piece.instanceId))
            {
                cell.pieceIds.Add(piece.instanceId);
            }
        }

        /// <summary>
        /// Removes a piece from the grid.
        /// </summary>
        public void RemovePiece(BuildingPiece piece)
        {
            if (cells.TryGetValue(piece.gridPosition, out var cell))
            {
                cell.pieceIds.Remove(piece.instanceId);
                if (cell.pieceIds.Count == 0)
                {
                    cells.Remove(piece.gridPosition);
                }
            }
        }

        /// <summary>
        /// Gets all piece IDs in a cell.
        /// </summary>
        public List<string> GetPiecesInCell(Vector3Int gridPosition)
        {
            if (cells.TryGetValue(gridPosition, out var cell))
            {
                return new List<string>(cell.pieceIds);
            }
            return new List<string>();
        }

        /// <summary>
        /// Gets all piece IDs in cells near a position.
        /// </summary>
        public List<string> GetPiecesNear(Vector3 worldPosition, int radius = 1)
        {
            var result = new List<string>();
            Vector3Int center = WorldToGrid(worldPosition);

            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    for (int z = -radius; z <= radius; z++)
                    {
                        var checkPos = center + new Vector3Int(x, y, z);
                        result.AddRange(GetPiecesInCell(checkPos));
                    }
                }
            }

            return result.Distinct().ToList();
        }

        /// <summary>
        /// Checks if a cell is occupied.
        /// </summary>
        public bool IsCellOccupied(Vector3Int gridPosition)
        {
            return cells.TryGetValue(gridPosition, out var cell) && cell.isOccupied;
        }

        /// <summary>
        /// Clears all grid data.
        /// </summary>
        public void Clear()
        {
            cells.Clear();
        }
    }

    #endregion

    #region Main System

    /// <summary>
    /// Main manager for the base building system.
    /// Handles placement, validation, construction, destruction, and structural integrity.
    /// </summary>
    public class BaseBuildingSystem : MonoBehaviour
    {
        public static BaseBuildingSystem Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float gridSize = 1f;
        [SerializeField] private float snapDistance = 0.5f;
        [SerializeField] private float maxBuildDistance = 20f;
        [SerializeField] private bool useStructuralIntegrity = true;
        [SerializeField] private float stabilityDecayPerConnection = 0.1f;
        [SerializeField] private float minStabilityThreshold = 0.1f;
        [SerializeField] private bool enableDecay = true;
        [SerializeField] private float decayCheckInterval = 60f;
        [SerializeField] private float resourceReturnPercentage = 0.5f;

        [Header("Layers")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private LayerMask buildingLayer;
        [SerializeField] private LayerMask obstacleLayer;

        [Header("Preview")]
        [SerializeField] private BuildingPreview previewHandler;

        // State
        private BuildingGrid grid;
        private Dictionary<string, BuildingPiece> allPieces = new Dictionary<string, BuildingPiece>();
        private Dictionary<string, List<string>> piecesByOwner = new Dictionary<string, List<string>>();
        private Dictionary<string, BuildableData> buildableRegistry = new Dictionary<string, BuildableData>();
        private BuildableData selectedBuildable;
        private bool isBuildModeActive;
        private float lastDecayCheck;

        // Player reference
        private Transform playerTransform;
        private string currentPlayerId;
        private string currentPlayerName;

        // Resource callbacks
        public Func<string, int> GetResourceCount;
        public Func<string, int, bool> ConsumeResources;
        public Action<string, int> AddResources;

        #region Events

        /// <summary>
        /// Fired when a building piece is placed.
        /// </summary>
        public event Action<BuildingPiece> OnPiecePlaced;

        /// <summary>
        /// Fired when a building piece starts construction.
        /// </summary>
        public event Action<BuildingPiece> OnConstructionStarted;

        /// <summary>
        /// Fired when a building piece finishes construction.
        /// </summary>
        public event Action<BuildingPiece> OnConstructionCompleted;

        /// <summary>
        /// Fired when a building piece is destroyed.
        /// </summary>
        public event Action<BuildingPiece, DestructionResult> OnPieceDestroyed;

        /// <summary>
        /// Fired when a building piece is damaged.
        /// </summary>
        public event Action<BuildingPiece, float> OnPieceDamaged;

        /// <summary>
        /// Fired when a building piece is repaired.
        /// </summary>
        public event Action<BuildingPiece, float> OnPieceRepaired;

        /// <summary>
        /// Fired when a building piece is upgraded.
        /// </summary>
        public event Action<BuildingPiece, BuildingMaterial, BuildingMaterial> OnPieceUpgraded;

        /// <summary>
        /// Fired when build mode is toggled.
        /// </summary>
        public event Action<bool> OnBuildModeChanged;

        /// <summary>
        /// Fired when the selected buildable changes.
        /// </summary>
        public event Action<BuildableData> OnBuildableSelected;

        /// <summary>
        /// Fired when structural integrity is recalculated.
        /// </summary>
        public event Action<List<BuildingPiece>> OnStabilityChanged;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            grid = new BuildingGrid(gridSize);
        }

        private void Update()
        {
            if (isBuildModeActive && selectedBuildable != null)
            {
                UpdatePreview();
            }

            if (enableDecay && Time.time - lastDecayCheck > decayCheckInterval)
            {
                ProcessDecay();
                lastDecayCheck = Time.time;
            }
        }

        #endregion

        #region Build Mode Control

        /// <summary>
        /// Sets the player reference for build operations.
        /// </summary>
        public void SetPlayer(Transform player, string playerId, string playerName)
        {
            playerTransform = player;
            currentPlayerId = playerId;
            currentPlayerName = playerName;
        }

        /// <summary>
        /// Enters build mode with the specified buildable.
        /// </summary>
        public void EnterBuildMode(BuildableData buildable)
        {
            if (buildable == null) return;

            selectedBuildable = buildable;
            isBuildModeActive = true;

            OnBuildableSelected?.Invoke(buildable);
            OnBuildModeChanged?.Invoke(true);
        }

        /// <summary>
        /// Exits build mode.
        /// </summary>
        public void ExitBuildMode()
        {
            isBuildModeActive = false;
            selectedBuildable = null;

            if (previewHandler != null)
            {
                previewHandler.Hide();
            }

            OnBuildModeChanged?.Invoke(false);
        }

        /// <summary>
        /// Toggles build mode.
        /// </summary>
        public void ToggleBuildMode(BuildableData buildable = null)
        {
            if (isBuildModeActive)
            {
                ExitBuildMode();
            }
            else if (buildable != null)
            {
                EnterBuildMode(buildable);
            }
        }

        /// <summary>
        /// Selects a different buildable while in build mode.
        /// </summary>
        public void SelectBuildable(BuildableData buildable)
        {
            if (!isBuildModeActive) return;

            selectedBuildable = buildable;
            if (previewHandler != null)
            {
                previewHandler.Hide();
            }
            OnBuildableSelected?.Invoke(buildable);
        }

        /// <summary>
        /// Gets whether build mode is active.
        /// </summary>
        public bool IsBuildModeActive => isBuildModeActive;

        /// <summary>
        /// Gets the currently selected buildable.
        /// </summary>
        public BuildableData SelectedBuildable => selectedBuildable;

        #endregion

        #region Preview and Validation

        /// <summary>
        /// Updates the build preview based on current aiming position.
        /// </summary>
        private void UpdatePreview()
        {
            if (playerTransform == null || selectedBuildable == null) return;

            Ray ray = new Ray(playerTransform.position + Vector3.up * 1.5f, playerTransform.forward);
            Vector3 targetPosition;
            Quaternion targetRotation = playerTransform.rotation;

            // Raycast to find placement point
            if (Physics.Raycast(ray, out RaycastHit hit, maxBuildDistance, groundLayer | buildingLayer))
            {
                targetPosition = hit.point + Vector3.up * selectedBuildable.groundOffset;

                // Try to snap to nearby sockets
                var snapInfo = FindSnapPoint(targetPosition, selectedBuildable);
                if (snapInfo.isSnapped)
                {
                    targetPosition = snapInfo.snapPosition;
                    targetRotation = snapInfo.snapRotation;
                }
                else
                {
                    // Snap to grid if not snapping to socket
                    targetPosition = grid.SnapToGrid(targetPosition);
                }
            }
            else
            {
                // Place at max distance if no hit
                targetPosition = ray.origin + ray.direction * maxBuildDistance;
                targetPosition = grid.SnapToGrid(targetPosition);
            }

            // Apply rotation snapping
            if (selectedBuildable.rotationSnap > 0)
            {
                float yRotation = Mathf.Round(targetRotation.eulerAngles.y / selectedBuildable.rotationSnap) *
                                  selectedBuildable.rotationSnap;
                targetRotation = Quaternion.Euler(0, yRotation, 0);
            }

            // Update preview visual
            if (previewHandler != null)
            {
                previewHandler.Show(selectedBuildable, targetPosition, targetRotation);

                var placementInfo = ValidatePlacement(selectedBuildable, targetPosition, targetRotation);
                previewHandler.SetValid(placementInfo.IsValid);
            }
        }

        /// <summary>
        /// Finds the best snap point for a buildable near a position.
        /// </summary>
        public SnapInfo FindSnapPoint(Vector3 position, BuildableData buildable)
        {
            var result = new SnapInfo { isSnapped = false };

            // Find nearby pieces
            var nearbyPieceIds = grid.GetPiecesNear(position, 2);

            float closestDistance = snapDistance;
            BuildingSocket bestSocket = null;
            BuildingPiece bestPiece = null;

            foreach (var pieceId in nearbyPieceIds)
            {
                if (!allPieces.TryGetValue(pieceId, out var piece)) continue;
                if (!piece.IsComplete) continue;

                foreach (var socket in piece.sockets)
                {
                    if (!socket.CanAccept(buildable.category)) continue;
                    if (!IsSocketCompatible(socket.socketType, buildable.compatibleSnapTypes)) continue;

                    Vector3 socketWorldPos = piece.transform.TransformPoint(socket.localPosition);
                    float distance = Vector3.Distance(position, socketWorldPos);

                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        bestSocket = socket;
                        bestPiece = piece;
                    }
                }
            }

            if (bestSocket != null && bestPiece != null)
            {
                result.isSnapped = true;
                result.snapToPiece = bestPiece;
                result.socket = bestSocket;
                result.snapPosition = bestPiece.transform.TransformPoint(bestSocket.localPosition);
                result.snapRotation = bestPiece.transform.rotation * bestSocket.localRotation;
            }

            return result;
        }

        /// <summary>
        /// Validates whether a buildable can be placed at the specified position.
        /// </summary>
        public PlacementInfo ValidatePlacement(BuildableData buildable, Vector3 position, Quaternion rotation)
        {
            var result = new PlacementInfo
            {
                position = position,
                rotation = rotation,
                snapInfo = FindSnapPoint(position, buildable)
            };

            // Check distance from player
            if (playerTransform != null)
            {
                float distance = Vector3.Distance(playerTransform.position, position);
                if (distance > buildable.maxPlacementDistance)
                {
                    result.result = PlacementResult.TooFar;
                    result.message = "Too far away to build.";
                    return result;
                }
            }

            // Check resources
            if (GetResourceCount != null)
            {
                result.missingResources = new List<BuildingCost>();
                foreach (var cost in buildable.buildCosts)
                {
                    int available = GetResourceCount(cost.resourceId);
                    if (available < cost.amount)
                    {
                        result.missingResources.Add(new BuildingCost(
                            cost.resourceId,
                            cost.resourceName,
                            cost.amount - available));
                    }
                }

                if (result.missingResources.Count > 0)
                {
                    result.result = PlacementResult.InsufficientResources;
                    result.message = "Not enough resources.";
                    return result;
                }
            }

            // Check ground/slope if requiring foundation
            if (buildable.requiresFoundation)
            {
                if (!result.snapInfo.isSnapped)
                {
                    result.result = PlacementResult.NoSnapPoint;
                    result.message = "Must snap to existing structure.";
                    return result;
                }
            }
            else
            {
                // Check slope angle
                if (Physics.Raycast(position + Vector3.up, Vector3.down, out RaycastHit hit, 2f, groundLayer))
                {
                    float angle = Vector3.Angle(hit.normal, Vector3.up);
                    if (angle > buildable.maxSlopeAngle)
                    {
                        result.result = PlacementResult.InvalidAngle;
                        result.message = "Surface too steep.";
                        return result;
                    }
                }
            }

            // Check for overlapping structures
            Collider[] overlaps = Physics.OverlapBox(
                position,
                buildable.size * 0.45f,
                rotation,
                buildingLayer);

            if (overlaps.Length > 0)
            {
                result.result = PlacementResult.Overlapping;
                result.message = "Overlapping with existing structure.";
                return result;
            }

            // Check for obstacles
            overlaps = Physics.OverlapBox(
                position,
                buildable.size * 0.45f,
                rotation,
                obstacleLayer);

            if (overlaps.Length > 0)
            {
                result.result = PlacementResult.Blocked;
                result.message = "Placement blocked by obstacle.";
                return result;
            }

            // Check structural support
            if (useStructuralIntegrity && buildable.requiresFoundation)
            {
                if (!result.snapInfo.isSnapped || !WouldHaveSupport(buildable, result.snapInfo))
                {
                    result.result = PlacementResult.NoSupport;
                    result.message = "Insufficient structural support.";
                    return result;
                }
            }

            result.result = PlacementResult.Valid;
            result.message = "Ready to build.";
            return result;
        }

        private bool IsSocketCompatible(SocketType socketType, SocketType[] compatibleTypes)
        {
            if (compatibleTypes == null || compatibleTypes.Length == 0) return true;
            if (socketType == SocketType.Universal) return true;
            return compatibleTypes.Contains(socketType);
        }

        private bool WouldHaveSupport(BuildableData buildable, SnapInfo snapInfo)
        {
            if (!snapInfo.isSnapped) return false;

            var parentPiece = snapInfo.snapToPiece;
            if (parentPiece == null) return false;

            // Foundations always have support on ground
            if (buildable.category == BuildingCategory.Foundation) return true;

            // Check if parent has support
            if (!parentPiece.hasSupport) return false;

            // Calculate stability based on distance from foundation
            float parentStability = parentPiece.stabilityValue;
            float newStability = parentStability - stabilityDecayPerConnection;

            return newStability >= minStabilityThreshold;
        }

        #endregion

        #region Building Placement

        /// <summary>
        /// Attempts to place a building at the current preview location.
        /// </summary>
        public bool TryPlaceBuilding()
        {
            if (!isBuildModeActive || selectedBuildable == null) return false;
            if (previewHandler == null || !previewHandler.IsVisible) return false;

            Vector3 position = previewHandler.PreviewObject.transform.position;
            Quaternion rotation = previewHandler.PreviewObject.transform.rotation;

            return PlaceBuilding(selectedBuildable, position, rotation);
        }

        /// <summary>
        /// Places a building piece at the specified position.
        /// </summary>
        public bool PlaceBuilding(BuildableData buildable, Vector3 position, Quaternion rotation)
        {
            // Validate placement
            var placementInfo = ValidatePlacement(buildable, position, rotation);
            if (!placementInfo.IsValid)
            {
                Debug.LogWarning($"Cannot place building: {placementInfo.message}");
                return false;
            }

            // Consume resources
            if (ConsumeResources != null)
            {
                foreach (var cost in buildable.buildCosts)
                {
                    if (!ConsumeResources(cost.resourceId, cost.amount))
                    {
                        Debug.LogWarning($"Failed to consume resource: {cost.resourceId}");
                        return false;
                    }
                }
            }

            // Create piece
            var piece = new BuildingPiece(buildable, position, rotation)
            {
                ownerId = currentPlayerId,
                ownerName = currentPlayerName
            };

            // Spawn game object
            if (buildable.prefab != null)
            {
                piece.gameObject = Instantiate(
                    buildable.constructionTime > 0 && buildable.constructionPrefab != null
                        ? buildable.constructionPrefab
                        : buildable.prefab,
                    position,
                    rotation);
                piece.gameObject.name = $"{buildable.displayName}_{piece.instanceId.Substring(0, 8)}";
                piece.transform = piece.gameObject.transform;
            }

            // Link to parent if snapped
            if (placementInfo.snapInfo != null && placementInfo.snapInfo.isSnapped)
            {
                var parentPiece = placementInfo.snapInfo.snapToPiece;
                piece.parentPieceId = parentPiece.instanceId;

                // Occupy socket
                parentPiece.OccupySocket(placementInfo.snapInfo.socket.socketId, piece.instanceId);

                // Calculate stability
                piece.hasSupport = true;
                piece.stabilityValue = parentPiece.stabilityValue - stabilityDecayPerConnection;
            }
            else
            {
                // Foundation on ground has full stability
                piece.hasSupport = true;
                piece.stabilityValue = 1f;
            }

            // Register piece
            allPieces[piece.instanceId] = piece;
            grid.AddPiece(piece);

            if (!piecesByOwner.ContainsKey(currentPlayerId))
            {
                piecesByOwner[currentPlayerId] = new List<string>();
            }
            piecesByOwner[currentPlayerId].Add(piece.instanceId);

            // Start construction
            if (buildable.constructionTime > 0)
            {
                piece.state = BuildingState.Constructing;
                StartCoroutine(ConstructionCoroutine(piece));
                OnConstructionStarted?.Invoke(piece);
            }
            else
            {
                piece.state = BuildingState.Complete;
                OnConstructionCompleted?.Invoke(piece);
            }

            OnPiecePlaced?.Invoke(piece);

            return true;
        }

        /// <summary>
        /// Handles construction progress over time.
        /// </summary>
        private System.Collections.IEnumerator ConstructionCoroutine(BuildingPiece piece)
        {
            float elapsed = 0f;
            float duration = piece.data.constructionTime;

            while (elapsed < duration && piece.state == BuildingState.Constructing)
            {
                elapsed += Time.deltaTime;
                piece.constructionProgress = elapsed / duration;
                yield return null;
            }

            if (piece.state == BuildingState.Constructing)
            {
                piece.state = BuildingState.Complete;
                piece.constructionProgress = 1f;

                // Swap to completed prefab
                if (piece.data.prefab != null && piece.data.constructionPrefab != null)
                {
                    Vector3 pos = piece.transform.position;
                    Quaternion rot = piece.transform.rotation;
                    Destroy(piece.gameObject);

                    piece.gameObject = Instantiate(piece.data.prefab, pos, rot);
                    piece.gameObject.name = $"{piece.data.displayName}_{piece.instanceId.Substring(0, 8)}";
                    piece.transform = piece.gameObject.transform;
                }

                OnConstructionCompleted?.Invoke(piece);
            }
        }

        #endregion

        #region Damage and Destruction

        /// <summary>
        /// Damages a building piece.
        /// </summary>
        public void DamageBuilding(string pieceId, float damage, string attackerId = null)
        {
            if (!allPieces.TryGetValue(pieceId, out var piece)) return;
            if (!piece.data.isDestructible) return;

            float actualDamage = piece.TakeDamage(damage);
            OnPieceDamaged?.Invoke(piece, actualDamage);

            if (piece.state == BuildingState.Destroyed)
            {
                DestroyBuilding(pieceId, true);
            }
        }

        /// <summary>
        /// Destroys a building piece and handles structural collapse.
        /// </summary>
        public DestructionResult DestroyBuilding(string pieceId, bool wasDestroyed = false)
        {
            if (!allPieces.TryGetValue(pieceId, out var piece)) return null;

            var result = new DestructionResult
            {
                destroyedPiece = piece,
                collapsedPieces = new List<BuildingPiece>(),
                returnedResources = new Dictionary<string, int>()
            };

            // Calculate returned resources
            if (!wasDestroyed)
            {
                foreach (var cost in piece.data.buildCosts)
                {
                    int returnAmount = Mathf.FloorToInt(cost.amount * resourceReturnPercentage * piece.HealthPercent);
                    if (returnAmount > 0)
                    {
                        result.returnedResources[cost.resourceId] = returnAmount;
                        AddResources?.Invoke(cost.resourceId, returnAmount);
                    }
                }
            }

            // Free sockets from connected pieces
            if (!string.IsNullOrEmpty(piece.parentPieceId) &&
                allPieces.TryGetValue(piece.parentPieceId, out var parentPiece))
            {
                var socketToFree = parentPiece.sockets.FirstOrDefault(s =>
                    s.occupiedByPieceId == pieceId);
                if (socketToFree != null)
                {
                    parentPiece.FreeSocket(socketToFree.socketId);
                }
            }

            // Check for structural collapse
            if (useStructuralIntegrity)
            {
                var dependentPieces = allPieces.Values
                    .Where(p => p.parentPieceId == pieceId)
                    .ToList();

                foreach (var dependent in dependentPieces)
                {
                    // Check if piece still has support without this parent
                    dependent.hasSupport = CheckHasAlternateSupport(dependent, pieceId);

                    if (!dependent.hasSupport)
                    {
                        dependent.state = BuildingState.Destroyed;
                        result.collapsedPieces.Add(dependent);
                    }
                }

                // Recursively destroy collapsed pieces
                foreach (var collapsed in result.collapsedPieces.ToList())
                {
                    var childResult = DestroyBuilding(collapsed.instanceId, true);
                    if (childResult != null)
                    {
                        result.collapsedPieces.AddRange(childResult.collapsedPieces);
                    }
                }
            }

            // Cleanup
            RemovePiece(piece);

            OnPieceDestroyed?.Invoke(piece, result);

            return result;
        }

        private void RemovePiece(BuildingPiece piece)
        {
            // Spawn destruction VFX
            if (piece.data.destroyVFX != null && piece.transform != null)
            {
                Instantiate(piece.data.destroyVFX, piece.transform.position, piece.transform.rotation);
            }

            // Destroy game object
            if (piece.gameObject != null)
            {
                Destroy(piece.gameObject);
            }

            // Remove from tracking
            grid.RemovePiece(piece);
            allPieces.Remove(piece.instanceId);

            if (piecesByOwner.TryGetValue(piece.ownerId, out var ownerPieces))
            {
                ownerPieces.Remove(piece.instanceId);
            }
        }

        private bool CheckHasAlternateSupport(BuildingPiece piece, string excludePieceId)
        {
            // Foundations are always supported
            if (piece.data.category == BuildingCategory.Foundation) return true;

            // Check if any connected pieces (other than excluded) can provide support
            foreach (var connectedId in piece.connectedPieceIds)
            {
                if (connectedId == excludePieceId) continue;
                if (!allPieces.TryGetValue(connectedId, out var connected)) continue;

                if (connected.hasSupport && connected.data.providesSupport)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Repair and Upgrade

        /// <summary>
        /// Repairs a building piece.
        /// </summary>
        public bool RepairBuilding(string pieceId)
        {
            if (!allPieces.TryGetValue(pieceId, out var piece)) return false;
            if (piece.currentHealth >= piece.maxHealth) return false;

            // Check resources
            if (GetResourceCount != null && ConsumeResources != null)
            {
                foreach (var cost in piece.data.repairCosts)
                {
                    if (GetResourceCount(cost.resourceId) < cost.amount)
                    {
                        return false;
                    }
                }

                foreach (var cost in piece.data.repairCosts)
                {
                    ConsumeResources(cost.resourceId, cost.amount);
                }
            }

            float repairAmount = piece.maxHealth * piece.data.repairHealthPercent;
            float actualRepair = piece.Repair(repairAmount);

            OnPieceRepaired?.Invoke(piece, actualRepair);
            return true;
        }

        /// <summary>
        /// Upgrades a building piece to a higher material tier.
        /// </summary>
        public bool UpgradeBuilding(string pieceId, BuildingMaterial targetMaterial)
        {
            if (!allPieces.TryGetValue(pieceId, out var piece)) return false;
            if (!piece.IsComplete) return false;
            if ((int)targetMaterial <= (int)piece.currentMaterial) return false;

            // Find upgrade path
            var upgrade = piece.data.availableUpgrades
                .FirstOrDefault(u => u.targetMaterial == targetMaterial);

            if (upgrade == null)
            {
                Debug.LogWarning($"No upgrade path to {targetMaterial}");
                return false;
            }

            // Check resources
            if (GetResourceCount != null && ConsumeResources != null)
            {
                foreach (var cost in upgrade.costs)
                {
                    if (GetResourceCount(cost.resourceId) < cost.amount)
                    {
                        return false;
                    }
                }

                foreach (var cost in upgrade.costs)
                {
                    ConsumeResources(cost.resourceId, cost.amount);
                }
            }

            // Apply upgrade
            BuildingMaterial oldMaterial = piece.currentMaterial;
            piece.currentMaterial = targetMaterial;
            piece.maxHealth *= upgrade.healthMultiplier;
            piece.currentHealth = piece.maxHealth;

            OnPieceUpgraded?.Invoke(piece, oldMaterial, targetMaterial);
            return true;
        }

        #endregion

        #region Decay System

        /// <summary>
        /// Processes decay for all buildings.
        /// </summary>
        private void ProcessDecay()
        {
            if (!enableDecay) return;

            float hoursSinceCheck = decayCheckInterval / 3600f;

            foreach (var piece in allPieces.Values.ToList())
            {
                if (!piece.data.canDecay) continue;
                if (!piece.IsComplete) continue;

                // Check if piece should decay (no recent interaction)
                TimeSpan timeSinceInteraction = DateTime.Now - piece.lastInteractionTime;
                if (timeSinceInteraction.TotalHours < 24) continue; // Grace period

                float decayDamage = piece.data.decayRate * hoursSinceCheck;
                piece.TakeDamage(decayDamage);
                piece.state = BuildingState.Decaying;

                if (piece.currentHealth <= 0)
                {
                    piece.state = BuildingState.Destroyed;
                    DestroyBuilding(piece.instanceId, true);
                }
            }
        }

        /// <summary>
        /// Resets the decay timer for a building (when interacted with).
        /// </summary>
        public void ResetDecayTimer(string pieceId)
        {
            if (allPieces.TryGetValue(pieceId, out var piece))
            {
                piece.lastInteractionTime = DateTime.Now;
                if (piece.state == BuildingState.Decaying)
                {
                    piece.state = piece.currentHealth >= piece.maxHealth * 0.5f
                        ? BuildingState.Complete
                        : BuildingState.Damaged;
                }
            }
        }

        #endregion

        #region Structural Integrity

        /// <summary>
        /// Recalculates structural integrity for all connected pieces.
        /// </summary>
        public void RecalculateStability(string startPieceId = null)
        {
            if (!useStructuralIntegrity) return;

            var affectedPieces = new List<BuildingPiece>();

            // Start from foundations and propagate stability
            var foundations = allPieces.Values
                .Where(p => p.data.category == BuildingCategory.Foundation && p.hasSupport)
                .ToList();

            // Reset all stability
            foreach (var piece in allPieces.Values)
            {
                if (piece.data.category != BuildingCategory.Foundation)
                {
                    piece.hasSupport = false;
                    piece.stabilityValue = 0f;
                }
            }

            // Propagate from foundations
            var processed = new HashSet<string>();
            var queue = new Queue<BuildingPiece>(foundations);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (processed.Contains(current.instanceId)) continue;
                processed.Add(current.instanceId);

                foreach (var connectedId in current.connectedPieceIds)
                {
                    if (!allPieces.TryGetValue(connectedId, out var connected)) continue;
                    if (processed.Contains(connectedId)) continue;

                    float newStability = current.stabilityValue - stabilityDecayPerConnection;
                    if (newStability >= minStabilityThreshold)
                    {
                        connected.hasSupport = true;
                        connected.stabilityValue = Mathf.Max(connected.stabilityValue, newStability);
                        affectedPieces.Add(connected);
                        queue.Enqueue(connected);
                    }
                }
            }

            // Check for pieces that lost support
            foreach (var piece in allPieces.Values.Where(p => !p.hasSupport && p.state != BuildingState.Destroyed))
            {
                piece.state = BuildingState.Destroyed;
                DestroyBuilding(piece.instanceId, true);
            }

            OnStabilityChanged?.Invoke(affectedPieces);
        }

        #endregion

        #region Queries

        /// <summary>
        /// Gets a building piece by ID.
        /// </summary>
        public BuildingPiece GetPiece(string pieceId)
        {
            return allPieces.TryGetValue(pieceId, out var piece) ? piece : null;
        }

        /// <summary>
        /// Gets all building pieces.
        /// </summary>
        public List<BuildingPiece> GetAllPieces()
        {
            return allPieces.Values.ToList();
        }

        /// <summary>
        /// Gets all building pieces owned by a player.
        /// </summary>
        public List<BuildingPiece> GetPiecesByOwner(string ownerId)
        {
            if (!piecesByOwner.TryGetValue(ownerId, out var pieceIds))
                return new List<BuildingPiece>();

            return pieceIds
                .Where(id => allPieces.ContainsKey(id))
                .Select(id => allPieces[id])
                .ToList();
        }

        /// <summary>
        /// Gets all building pieces within a radius.
        /// </summary>
        public List<BuildingPiece> GetPiecesInRadius(Vector3 center, float radius)
        {
            float sqrRadius = radius * radius;
            return allPieces.Values
                .Where(p => (p.position - center).sqrMagnitude <= sqrRadius)
                .ToList();
        }

        /// <summary>
        /// Gets all building pieces of a specific category.
        /// </summary>
        public List<BuildingPiece> GetPiecesByCategory(BuildingCategory category)
        {
            return allPieces.Values
                .Where(p => p.data.category == category)
                .ToList();
        }

        /// <summary>
        /// Gets the building piece at a world position (using raycast).
        /// </summary>
        public BuildingPiece GetPieceAtPosition(Vector3 position, float maxDistance = 2f)
        {
            var nearbyIds = grid.GetPiecesNear(position, 1);

            float closestDist = maxDistance;
            BuildingPiece closest = null;

            foreach (var id in nearbyIds)
            {
                if (!allPieces.TryGetValue(id, out var piece)) continue;

                float dist = Vector3.Distance(position, piece.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = piece;
                }
            }

            return closest;
        }

        /// <summary>
        /// Gets total piece count.
        /// </summary>
        public int TotalPieceCount => allPieces.Count;

        /// <summary>
        /// Gets piece count by owner.
        /// </summary>
        public int GetPieceCount(string ownerId)
        {
            return piecesByOwner.TryGetValue(ownerId, out var pieces) ? pieces.Count : 0;
        }

        #endregion

        #region Registry

        /// <summary>
        /// Registers a buildable data for quick lookup.
        /// </summary>
        public void RegisterBuildable(BuildableData buildable)
        {
            if (buildable != null && !string.IsNullOrEmpty(buildable.buildableId))
            {
                buildableRegistry[buildable.buildableId] = buildable;
            }
        }

        /// <summary>
        /// Gets a registered buildable by ID.
        /// </summary>
        public BuildableData GetBuildableById(string buildableId)
        {
            return buildableRegistry.TryGetValue(buildableId, out var buildable) ? buildable : null;
        }

        /// <summary>
        /// Gets all registered buildables.
        /// </summary>
        public List<BuildableData> GetAllBuildables()
        {
            return buildableRegistry.Values.ToList();
        }

        /// <summary>
        /// Gets all registered buildables of a specific category.
        /// </summary>
        public List<BuildableData> GetBuildablesByCategory(BuildingCategory category)
        {
            return buildableRegistry.Values
                .Where(b => b.category == category)
                .ToList();
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Clears all building data.
        /// </summary>
        public void ClearAllBuildings()
        {
            foreach (var piece in allPieces.Values)
            {
                if (piece.gameObject != null)
                {
                    Destroy(piece.gameObject);
                }
            }

            allPieces.Clear();
            piecesByOwner.Clear();
            grid.Clear();
        }

        /// <summary>
        /// Destroys all buildings owned by a player.
        /// </summary>
        public void ClearPlayerBuildings(string ownerId)
        {
            if (!piecesByOwner.TryGetValue(ownerId, out var pieceIds)) return;

            foreach (var pieceId in pieceIds.ToList())
            {
                DestroyBuilding(pieceId, true);
            }

            piecesByOwner.Remove(ownerId);
        }

        #endregion
    }

    #endregion
}
