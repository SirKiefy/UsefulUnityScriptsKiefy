# UsefulUnityScriptsKiefy

A collection of useful, reusable Unity scripts ranging from simple utilities to complex systems. Designed with professional Unity development practices in mind.

## 📁 Project Structure

```
Assets/
└── Scripts/
    ├── Core/                  # Core systems (Singleton, GameManager)
    ├── Utilities/             # Helper utilities (Timer, Tween, MathUtils)
    ├── Player/                # Player controllers & health system
    ├── Camera/                # Camera follow & shake systems
    ├── UI/                    # UI management & transitions
    ├── Audio/                 # Audio management system
    ├── SaveSystem/            # Save/Load functionality
    ├── Events/                # Event management system
    ├── StateMachine/          # Finite state machine implementation
    ├── Dialogue/              # Dialogue system with choices
    ├── Pooling/               # Object pooling system
    ├── Extensions/            # C# extension methods
    ├── Inventory/             # Complete inventory & equipment system
    ├── Abilities/             # Ability system with cooldowns & status effects
    ├── Quest/                 # Quest tracking & objectives system
    ├── Input/                 # Rebindable input management
    ├── Achievement/           # Achievement tracking & rewards
    └── ProceduralGeneration/  # Procedural content generation utilities
```

## 🚀 Quick Start

1. Copy the `Assets/Scripts` folder into your Unity project
2. Scripts are organized by namespace (e.g., `UsefulScripts.Core`, `UsefulScripts.Player`)
3. Most manager classes are Singletons - access via `ClassName.Instance`

## 📦 Script Categories

### Core Systems

#### Singleton<T>
Generic singleton pattern for MonoBehaviours.
```csharp
public class MyManager : Singleton<MyManager>
{
    protected override void OnSingletonAwake()
    {
        // Initialization code
    }
}
// Access: MyManager.Instance.DoSomething();
```

#### GameManager
Central game state management with pause, scene loading, and game states.
```csharp
GameManager.Instance.StartGame();
GameManager.Instance.TogglePause();
GameManager.Instance.LoadScene("Level1");
```

---

### Player Scripts

#### CharacterController2D
Full-featured 2D platformer controller with:
- Smooth acceleration/deceleration
- Coyote time & jump buffering
- Variable jump height
- Air jumps
- Events for jump/land

#### FirstPersonController
Complete FPS controller with:
- WASD movement + mouse look
- Sprint & crouch
- Head bobbing
- Smooth camera

#### HealthSystem
Modular health component with:
- Health & shield
- Regeneration
- Invincibility frames
- Events for damage/heal/death

```csharp
healthSystem.TakeDamage(25f);
healthSystem.Heal(10f);
healthSystem.OnDeath += HandleDeath;
```

---

### Camera Scripts

#### CameraFollow
Flexible camera following with:
- Multiple follow modes (Instant, SmoothDamp, Lerp)
- Dead zones
- Look-ahead
- Bounds clamping

#### CameraShake
Dynamic camera shake with:
- Perlin noise-based shake
- Trauma system
- Multiple shake modes

```csharp
CameraShake.ShakeCamera(0.3f, 0.2f);
CameraShake.AddCameraTrauma(0.5f);
```

---

### UI Scripts

#### UIManager
Panel management with:
- Show/hide with transitions
- Navigation history
- Fade animations

```csharp
UIManager.Instance.ShowPanel("MainMenu");
UIManager.Instance.GoBack();
```

#### FadeController
Screen transitions and fades.
```csharp
FadeController.DoFadeToScene("Level1", 0.5f);
```

#### HealthBar
Visual health bar that connects to HealthSystem.

---

### Audio

#### AudioManager
Complete audio solution with:
- Sound effect library
- Music playback with crossfade
- Volume controls
- Positional audio

```csharp
AudioManager.Instance.PlaySound("Jump");
AudioManager.Instance.PlayMusic("BattleTheme");
AudioManager.Instance.MusicVolume = 0.5f;
```

---

### Save System

#### SaveManager
JSON-based save/load system.
```csharp
// Save
SaveManager.Save(myData, "save1.json");

// Load
var data = SaveManager.Load<GameSaveData>("save1.json");

// Quick prefs
SaveManager.SaveValue("HighScore", 1000);
int score = SaveManager.LoadValue<int>("HighScore");
```

#### GameSaveData
Example save data structure with serializable Vector3/Quaternion/Color types.

---

### Events

#### EventManager
Type-safe event system for decoupled communication.
```csharp
// Define event
public struct PlayerDiedEvent { public GameObject Player; }

// Subscribe
EventManager.Subscribe<PlayerDiedEvent>(OnPlayerDied);

// Trigger
EventManager.Trigger(new PlayerDiedEvent { Player = gameObject });

// Unsubscribe
EventManager.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
```

#### GameEvents
Pre-defined common game events (PlayerDied, LevelCompleted, ScoreChanged, etc.)

---

### State Machine

#### StateMachine<T>
Generic finite state machine.
```csharp
// Create state machine
var fsm = new StateMachine<EnemyAI>(this);

// Add states
fsm.AddStates(new IdleState(), new PatrolState(), new ChaseState());

// Set initial state
fsm.SetInitialState<IdleState>();

// Change state
fsm.ChangeState<ChaseState>();

// Update in Update()
fsm.Update(Time.deltaTime);
```

#### EnemyAIExample
Complete example of state machine usage for enemy AI.

---

### Dialogue System

#### DialogueData (ScriptableObject)
Create dialogue assets with nodes, choices, and events.

#### DialogueManager
Handles dialogue display with:
- Typewriter effect
- Choices
- Speaker portraits
- Voice clips
- Dialogue flags/variables

---

### Object Pooling

#### ObjectPool<T>
Generic object pooling.
```csharp
var pool = new ObjectPool<Bullet>(bulletPrefab, 20);
Bullet bullet = pool.Get(spawnPosition);
pool.Return(bullet);
```

#### PoolManager
Centralized pool management.
```csharp
PoolManager.Instance.CreatePool("Bullets", bulletPrefab, 50);
GameObject bullet = PoolManager.Instance.Get("Bullets", position);
PoolManager.Instance.Return("Bullets", bullet);
```

---

### Utilities

#### Timer
Flexible countdown/stopwatch timer.
```csharp
var timer = new Timer(5f, Timer.TimerType.Countdown);
timer.OnTimerComplete += () => Debug.Log("Done!");
timer.Start();
// Call timer.Tick() in Update()
```

#### Tween
Simple tweening utilities.
```csharp
float value = Tween.Lerp(0, 100, t, Tween.EaseType.EaseInOut);
StartCoroutine(TweenRunner.MoveTo(transform, targetPos, 1f));
StartCoroutine(TweenRunner.Shake(transform, 0.5f, 0.3f));
```

#### MathUtils
Math helpers (Bezier curves, spring physics, random points, etc.)

---

### Extensions

#### VectorExtensions
```csharp
Vector3 newPos = position.WithY(0);
Vector3 direction = from.DirectionTo(target);
Vector2 rotated = vector.Rotate(45f);
```

#### TransformExtensions
```csharp
transform.SetX(10f);
transform.ResetLocal();
transform.DestroyChildren();
float dist = transform.DistanceTo(other);
```

#### GameObjectExtensions
```csharp
var rb = gameObject.GetOrAddComponent<Rigidbody>();
gameObject.SetLayerRecursively("Player");
Transform child = transform.FindDeepChild("Weapon");
```

#### GeneralExtensions
```csharp
float remapped = value.Remap(0, 100, 0, 1);
Color faded = color.WithAlpha(0.5f);
var random = myList.GetRandom();
myList.Shuffle();
```

---

### Inventory System

#### InventorySystem
Complete inventory management with slots, stacking, equipment, and item management.
```csharp
// Add items
int overflow = InventorySystem.Instance.AddItem(swordData, 1);

// Remove items
InventorySystem.Instance.RemoveItem(potionData, 5);

// Check inventory
bool hasItem = InventorySystem.Instance.HasItem(keyData, 1);
int count = InventorySystem.Instance.GetItemCount(goldData);

// Move items between slots
InventorySystem.Instance.MoveItem(fromSlot: 0, toSlot: 5);

// Equipment
InventorySystem.Instance.EquipItem(slotIndex: 3);
InventorySystem.Instance.UnequipItem(EquipmentSlot.MainHand);

// Get equipment stats
var stats = InventorySystem.Instance.GetEquipmentStats();

// Use consumables
InventorySystem.Instance.UseItem(slotIndex: 2);

// Sort inventory
InventorySystem.Instance.SortInventory(SortCriteria.Rarity);

// Events
InventorySystem.Instance.OnItemAdded += (item, slot) => { };
InventorySystem.Instance.OnItemEquipped += (item, slot) => { };
```

#### ItemData (ScriptableObject)
Define items with properties, stats, and consumable effects.
- Stackable items with max stack sizes
- Item types (Weapon, Armor, Consumable, Material, etc.)
- Rarity levels (Common to Mythic)
- Equipment slots and stat bonuses
- Consumable effects with cooldowns

---

### Ability System

#### AbilitySystem
Complete ability/skill system with cooldowns, casting, and status effects.
```csharp
var abilitySystem = GetComponent<AbilitySystem>();

// Use abilities
abilitySystem.UseAbility(fireballAbility, targetPosition);
abilitySystem.UseAbilityById("fireball", targetPosition);

// Check ability state
bool canUse = abilitySystem.CanUseAbility(ability);
bool onCooldown = abilitySystem.IsOnCooldown("fireball");
float cdRemaining = abilitySystem.GetCooldownRemaining("fireball");

// Status effects
abilitySystem.ApplyEffect(new StatusEffect("burn", "Burning", EffectType.DamageOverTime, 5f, 3f, true));
abilitySystem.Cleanse(); // Remove all debuffs
abilitySystem.Dispel();  // Remove all buffs

// Check status
bool stunned = abilitySystem.IsStunned();
bool silenced = abilitySystem.IsSilenced();
float speedMod = abilitySystem.GetSpeedMultiplier();

// Resource management
abilitySystem.RestoreMana(50f);
abilitySystem.ConsumeMana(25f);

// Learn/forget abilities
abilitySystem.LearnAbility(newAbility);
abilitySystem.ForgetAbility(oldAbility);

// Events
abilitySystem.OnAbilityUsed += ability => { };
abilitySystem.OnEffectApplied += effect => { };
abilitySystem.OnManaChanged += (current, max) => { };
```

#### AbilityData (ScriptableObject)
Define abilities with:
- Cooldowns, mana/stamina costs
- Cast times with cancellation options
- Targeting (Self, Single, AoE, Directional, Projectile)
- Multiple effects (damage, healing, crowd control, buffs/debuffs)
- Audio/visual feedback

---

### Quest System

#### QuestSystem
Complete quest tracking with objectives, rewards, and progress.
```csharp
// Accept quests
QuestSystem.Instance.AcceptQuest(mainQuest);
bool canAccept = QuestSystem.Instance.CanAcceptQuest(sideQuest);

// Update progress
QuestSystem.Instance.UpdateObjective(ObjectiveType.Kill, "goblin", 1);
QuestSystem.Instance.UpdateObjectiveProgress(questId, objectiveId, 5);

// Complete quests
QuestSystem.Instance.CompleteQuest(questId);
var readyQuests = QuestSystem.Instance.GetReadyToCompleteQuests();

// Manage quests
QuestSystem.Instance.AbandonQuest(questId);
QuestSystem.Instance.TrackQuest(questId);
var tracked = QuestSystem.Instance.TrackedQuest;

// Query quests
var activeQuests = QuestSystem.Instance.GetActiveQuests();
var availableQuests = QuestSystem.Instance.GetAvailableQuests();
bool isComplete = QuestSystem.Instance.IsQuestCompleted(questId);

// Stats
var stats = QuestSystem.Instance.GetQuestStats();

// Events
QuestSystem.Instance.OnQuestAccepted += quest => { };
QuestSystem.Instance.OnQuestCompleted += quest => { };
QuestSystem.Instance.OnObjectiveProgress += (quest, objId, current, max) => { };
```

#### QuestData (ScriptableObject)
Define quests with:
- Multiple objective types (Kill, Collect, Talk, Explore, Craft, etc.)
- Prerequisites and level requirements
- Rewards (XP, currency, items)
- Time limits and failure conditions
- Repeatable quests with cooldowns

---

### Input System

#### InputManager
Rebindable input system with gamepad support and input contexts.
```csharp
// Check input
bool pressed = InputManager.Instance.IsPressed("Jump");
bool justPressed = InputManager.Instance.WasJustPressed("Attack");
bool held = InputManager.Instance.IsHeld("Crouch");
float holdTime = InputManager.Instance.GetHoldDuration("Charge");

// Axes
float horizontal = InputManager.Instance.GetAxis("Horizontal");
Vector2 movement = InputManager.Instance.GetMovementInput();
Vector2 mouseDelta = InputManager.Instance.GetMouseDelta();

// Rebinding
InputManager.Instance.StartRebind("Jump", primary: true);
InputManager.Instance.CancelRebind();
InputManager.Instance.ResetBinding("Jump");
InputManager.Instance.ResetAllBindings();

// Settings
InputManager.Instance.MouseSensitivity = 1.5f;
InputManager.Instance.InvertMouseY = true;
InputManager.Instance.GamepadSensitivity = 1.2f;

// Input contexts (for different game states)
InputManager.Instance.PushContext(new InputContext("UI", "User Interface", priority: 10));
InputManager.Instance.PopContext("UI");

// Save/Load bindings
InputManager.Instance.SaveBindings();
InputManager.Instance.LoadBindings();

// Events
InputManager.Instance.OnActionPressed += actionId => { };
InputManager.Instance.OnActionDoubleTapped += actionId => { };
InputManager.Instance.OnRebindComplete += (actionId, newKey) => { };
InputManager.Instance.OnInputDeviceChanged += device => { };
```

---

### Achievement System

#### AchievementSystem
Complete achievement tracking with progress, rewards, and notifications.
```csharp
// Progress tracking
AchievementSystem.Instance.ReportProgress("kill_100_enemies", 1);
AchievementSystem.Instance.SetProgress("collect_coins", 50);

// Direct unlock
AchievementSystem.Instance.UnlockAchievement("first_blood");

// Stats tracking (auto-checks achievements)
AchievementSystem.Instance.IncrementStat(ConditionType.TotalKills, 1);
AchievementSystem.Instance.UpdateStat(ConditionType.MaxLevel, 10);

// Query achievements
var allAchievements = AchievementSystem.Instance.GetAllAchievements();
var unlocked = AchievementSystem.Instance.GetUnlockedAchievements();
var nearComplete = AchievementSystem.Instance.GetNearlyCompleteAchievements(5, 0.75f);
var recentlyUnlocked = AchievementSystem.Instance.GetRecentlyUnlocked(5);
bool isUnlocked = AchievementSystem.Instance.IsUnlocked("achievement_id");
float progress = AchievementSystem.Instance.GetProgressPercentage("achievement_id");

// Summary
var summary = AchievementSystem.Instance.GetSummary();
Debug.Log($"Completed: {summary.CompletionPercentage}%");
Debug.Log($"Total Points: {summary.TotalPoints}");

// Save/Load
AchievementSystem.Instance.SaveProgress();
AchievementSystem.Instance.LoadProgress();

// Events
AchievementSystem.Instance.OnAchievementUnlocked += achievement => { };
AchievementSystem.Instance.OnAchievementProgress += (achievement, current, max) => { };
AchievementSystem.Instance.OnNotificationStart += achievement => { };
```

#### AchievementData (ScriptableObject)
Define achievements with:
- Categories and rarity levels
- Progress tracking (single, cumulative, milestone)
- Multiple unlock conditions
- Rewards (points, currency, items, unlockables)
- Hidden/secret achievements

---

### Procedural Generation

#### NoiseGenerator
Noise generation utilities for procedural content.
```csharp
// Perlin noise map
float[,] heightMap = NoiseGenerator.GeneratePerlinNoiseMap(
    width: 256, height: 256, scale: 50f,
    octaves: 4, persistence: 0.5f, lacunarity: 2f,
    offset: Vector2.zero, seed: 12345
);

// Ridged noise (for mountains)
float[,] ridgedMap = NoiseGenerator.GenerateRidgedNoise(
    256, 256, 50f, 4, 0.5f, 2f, Vector2.zero, 12345
);

// Worley/cellular noise (for cells, caves)
float[,] worleyMap = NoiseGenerator.GenerateWorleyNoise(
    256, 256, numPoints: 50, seed: 12345, invert: false
);

// Sample noise at a point
float value = NoiseGenerator.SimplexNoise(x, y, z);
```

#### DungeonGenerator
Multiple dungeon generation algorithms.
```csharp
// BSP (Binary Space Partitioning) dungeon
DungeonData dungeon = DungeonGenerator.GenerateBSPDungeon(
    width: 100, height: 100,
    minRoomSize: 6, maxRoomSize: 15,
    numIterations: 5, seed: 12345
);

// Random walk dungeon
DungeonData caveDungeon = DungeonGenerator.GenerateRandomWalkDungeon(
    width: 100, height: 100,
    walkLength: 500, numWalkers: 10, seed: 12345
);

// Cellular automata cave
DungeonData cave = DungeonGenerator.GenerateCaveDungeon(
    width: 100, height: 100,
    fillProbability: 0.45f, smoothIterations: 5, seed: 12345
);

// Access dungeon data
foreach (var room in dungeon.rooms)
{
    Debug.Log($"Room {room.id}: {room.roomType} at {room.Center}");
}
bool walkable = dungeon.IsWalkable(x, y);
TileType tile = dungeon.GetTile(x, y);
```

#### TerrainGenerator
Terrain and biome generation.
```csharp
// Generate height map
var settings = new TerrainSettings
{
    noiseScale = 50f, octaves = 4,
    persistence = 0.5f, lacunarity = 2f,
    seed = 12345
};
float[,] heightMap = TerrainGenerator.GenerateHeightMap(256, 256, settings);

// Apply erosion
TerrainGenerator.ApplyErosion(heightMap, iterations: 1000, erosionStrength: 0.1f);

// Generate biome map
float[,] moistureMap = NoiseGenerator.GeneratePerlinNoiseMap(256, 256, 100f, 4, 0.5f, 2f, Vector2.zero, 54321);
int[,] biomeMap = TerrainGenerator.GenerateBiomeMap(heightMap, moistureMap, biomeList);
```

#### NameGenerator
Fantasy name generation.
```csharp
// Generate character names
string name = NameGenerator.GenerateName(seed: 12345);  // e.g., "Valandris"

// Generate town names
string town = NameGenerator.GenerateTownName(seed: 12345);  // e.g., "Shadowhaven"

// Generate multiple unique names
List<string> names = NameGenerator.GenerateNames(count: 10, baseSeed: 0);
```

#### LootGenerator
Weighted loot table generation.
```csharp
var lootTable = new LootGenerator.LootTable
{
    tableId = "chest_common",
    minDrops = 1, maxDrops = 3,
    nothingChance = 0.1f,
    entries = new List<LootGenerator.LootEntry>
    {
        new() { itemId = "gold", weight = 10f, minQuantity = 10, maxQuantity = 50 },
        new() { itemId = "potion", weight = 5f, minQuantity = 1, maxQuantity = 3 },
        new() { itemId = "rare_gem", weight = 1f, minQuantity = 1, maxQuantity = 1, rarity = 0.5f }
    }
};

List<LootGenerator.LootResult> drops = LootGenerator.GenerateLoot(
    lootTable, playerLevel: 10, luckModifier: 1.2f
);
```

#### WaveFunctionCollapse
Advanced tile-based procedural generation.
```csharp
var patterns = new List<WaveFunctionCollapse.TilePattern>
{
    new() { tileId = 0, name = "Grass", weight = 3f, allowedTop = new[] { 0, 1 }, ... },
    new() { tileId = 1, name = "Water", weight = 1f, allowedTop = new[] { 1 }, ... },
    // Define adjacency rules for each tile
};

int[,] generatedMap = WaveFunctionCollapse.Generate(
    width: 50, height: 50, patterns: patterns, seed: 12345
);
```

---

## 🎮 Usage Tips

1. **Namespaces**: All scripts use `UsefulScripts.*` namespace. Add `using UsefulScripts.Core;` etc.

2. **Singletons**: Access manager classes via `.Instance` property.

3. **Events**: Use the EventManager for decoupled communication between systems.

4. **Extensions**: Import extension namespaces to enhance built-in types.

5. **Customization**: These scripts are starting points - modify them for your needs!

## 📝 Requirements

- Unity 2021.3+ (uses modern APIs)
- TextMeshPro (for UI text)

## 📄 License

Feel free to use these scripts in your projects! Attribution appreciated but not required.