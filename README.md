# UsefulUnityScriptsKiefy

A collection of useful, reusable Unity scripts ranging from simple utilities to complex systems. Designed with professional Unity development practices in mind.

## 📁 Project Structure

```
Assets/
└── Scripts/
    ├── Core/                  # Core systems (Singleton, GameManager)
    ├── Utilities/             # Helper utilities (Timer, Tween, MathUtils)
    ├── Player/                # Player controllers & health system
    ├── FPS/                   # Advanced FPS movement (Titanfall-inspired)
    ├── SpaceShip/             # Complete spaceship systems (Elite/Star Citizen-inspired)
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
    ├── ProceduralGeneration/  # Procedural content generation utilities
    └── RPG/                   # Complete RPG & JRPG systems
        ├── CharacterStatsSystem  # Comprehensive stats, attributes, leveling
        ├── CombatSystem          # Turn-based, ATB, real-time combat
        ├── PartySystem           # Party management, formations, synergies
        ├── ClassSystem           # Job/class changing, skill trees
        ├── CraftingSystem        # Recipes, professions, quality tiers
        ├── RelationshipSystem    # Social links, romance, factions
        ├── SkillSystem           # Skill learning, inheritance, cooldowns
        └── SummonSystem          # Monster taming, evolution, summoning
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

### FPS Advanced Movement (Titanfall-Inspired)

Complete advanced movement system inspired by Titanfall's fluid movement mechanics. Use individual components or the complete integrated controller.

#### TitanfallMovementController
Complete integrated movement controller combining all advanced mechanics:
- Wall running with camera tilt
- Momentum-based sliding
- Grappling hook with swing physics
- Ledge mantling and climbing
- Double jump with coyote time
- Sprint, crouch, and air control

```csharp
// Attach to player with CharacterController
// Configure all movement settings in Inspector
// Access state and events:
if (controller.IsWallRunning) { /* ... */ }
controller.OnGrappleStart += HandleGrapple;
controller.AddVelocity(explosionForce); // External forces
```

#### WallRunning
Standalone wall running system:
- Automatic wall detection on left/right
- Camera tilt effects
- FOV changes during wall run
- Wall jump with directional control
- Duration-based wall run timer

```csharp
wallRunning.OnWallRunStart += () => PlaySound("wallrun");
wallRunning.OnWallJump += () => PlayEffect("walljump");
if (wallRunning.IsWallRunning) { /* ... */ }
```

#### AdvancedSlide
Momentum-preserving slide system:
- Slope acceleration/deceleration
- Slide steering control
- Height transition with camera adjustment
- Slide jump momentum boost
- Cooldown management

```csharp
advancedSlide.OnSlideStart += () => PlaySound("slide");
Vector3 slideBoost = advancedSlide.GetSlideJumpBoost();
```

#### GrappleHook
Versatile grappling system with multiple modes:
- **PullToPoint**: Direct pull toward grapple point
- **Swing**: Pendulum physics with rope constraint
- **Hybrid**: Pull then swing when close
- Rope visual with wave animation
- Momentum preservation on release
- Launch boost mechanics

```csharp
grapple.OnGrappleStart += (point) => ShowRope();
grapple.OnGrappleLaunch += () => PlaySound("launch");
if (grapple.GetPredictedGrapplePoint(out Vector3 point, out float dist)) {
    ShowGrappleIndicator(point);
}
```

#### Mantle
Ledge climbing and mantling system:
- Automatic ledge detection
- Quick mantle for running players
- Full climb with stamina system
- Arc trajectory animation
- Auto-mantle or manual trigger

```csharp
mantle.OnMantleStart += () => PlayAnimation("mantle");
mantle.OnStaminaChanged += UpdateStaminaUI;
if (mantle.CanMantle) { ShowMantlePrompt(); }
```

---

### SpaceShip Systems (Elite Dangerous/Star Citizen-Inspired)

Complete spaceship movement, combat, and systems management inspired by Elite Dangerous, Star Citizen, No Man's Sky, and 4X strategy games. Use individual components or the complete integrated suite.

#### SpaceShipController
Core 6-DOF (degrees of freedom) ship movement with realistic Newtonian physics:
- Main, reverse, lateral, and vertical thrust
- Pitch, yaw, and roll rotation
- Boost system with cooldown
- Throttle control with presets
- Speed limiting and inertia

```csharp
// Attach to ship with Rigidbody
var controller = GetComponent<SpaceShipController>();

// Movement control
controller.SetThrustInput(strafe, vertical, forward);
controller.SetRotationInput(pitch, yaw, roll);
controller.SetThrottle(0.75f);

// Boost
controller.StartBoost();
controller.OnBoostStart += () => PlaySound("boost");

// Flight assist modes
controller.SetFlightAssist(FlightAssistMode.Full);   // Dampened movement
controller.SetFlightAssist(FlightAssistMode.Off);    // Pure Newtonian
controller.CycleFlightAssist();  // Toggle modes
```

#### FlightAssistSystem
Advanced flight assist with velocity matching and approach control:
- Linear and rotational dampening
- Velocity matching with targets
- Approach assist for docking
- Drift cancellation
- Auto-orient toward targets

```csharp
var flightAssist = GetComponent<FlightAssistSystem>();

// Match velocity with another ship
flightAssist.StartVelocityMatch(targetShip);

// Automatic slowdown when approaching target
flightAssist.StartApproachAssist(station, desiredSpeed: 50f);

// Auto-orient toward target
flightAssist.SetAutoOrientTarget(enemy);
```

#### WarpDriveSystem
Multiple FTL travel modes for interstellar gameplay:
- **Supercruise**: In-system fast travel with variable speed
- **Hyperspace Jump**: Inter-system travel with charge time
- **Quantum Travel**: Point-to-point fast travel

```csharp
var warpDrive = GetComponent<WarpDriveSystem>();

// Supercruise (in-system travel)
warpDrive.StartSupercruiseCharge();
warpDrive.SetSupercruiseThrottle(0.8f);
warpDrive.DisengageSupercruise();

// Hyperspace jump (inter-system)
warpDrive.StartHyperspaceCharge(destinationPosition);
warpDrive.OnHyperspaceJumpComplete += () => UpdateStarMap();

// Quantum travel (point-to-point)
warpDrive.SetQuantumTarget(beacon);
warpDrive.StartQuantumCharge();

// Fuel management
warpDrive.ConsumeFuel(amount);
warpDrive.Refuel();
```

#### TurretSystem
Weapon turret with tracking and firing:
- Fixed, gimballed, and turreted mount types
- Lead prediction for moving targets
- Heat and ammunition management
- Multi-fire point support

```csharp
var turret = GetComponent<TurretSystem>();

// Aiming
turret.SetAimPoint(worldPosition);
turret.TrackTarget(enemy);

// Firing
turret.StartFiring();
turret.Fire();  // Single shot
turret.StopFiring();

// Check lead position for UI
Vector3 lead = turret.CalculateLeadPosition(target);
if (turret.CanFire) { ShowFireIndicator(); }
```

#### ShipWeaponSystem
Complete weapon management for multiple hardpoints:
- Weapon hardpoint configuration
- Firing groups (primary/secondary)
- Power management for weapons
- Coordinated targeting

```csharp
var weapons = GetComponent<ShipWeaponSystem>();

// Firing groups
weapons.StartFiringGroup(0);  // Primary fire
weapons.StartFiringGroup(1);  // Secondary fire
weapons.CycleFiringGroup();

// Hardpoint management
weapons.AssignToFiringGroup(hardpointIndex: 0, groupIndex: 0);
weapons.AimAt(targetPosition);

// Power for weapons
weapons.ConsumePower(amount);
float heat = weapons.GetAverageHeat();
```

#### ShipTargetingSystem
Target acquisition and tracking:
- Target scanning and selection
- Lock-on mechanics with progress
- Lead indicator calculation
- Subsystem targeting
- IFF (Identification Friend/Foe)
- Threat assessment

```csharp
var targeting = GetComponent<ShipTargetingSystem>();

// Target selection
targeting.TargetInCrosshairs();
targeting.NextTarget();
targeting.TargetNearestHostile();
targeting.TargetHighestThreat();

// Lock-on
targeting.OnLockAcquired += () => PlaySound("locked");
if (targeting.IsLocked) { FireMissile(); }

// Lead indicator
Vector3 leadPos = targeting.LeadPosition;
bool onTarget = targeting.IsLeadOnTarget(tolerance: 5f);

// Subsystem targeting
targeting.CycleSubsystem();
targeting.TargetSubsystem(SubsystemType.Engines);
```

#### ShipCombatManager
Combat state and damage management:
- Combat state tracking
- Damage distribution (shields → armor → hull)
- Point defense against missiles
- Countermeasures (chaff/flares)
- Evasive maneuvers

```csharp
var combat = GetComponent<ShipCombatManager>();

// Combat engagement
combat.EngageEnemy(enemy);
combat.OnEnemyEngaged += (e) => PlayBattleMusic();

// Countermeasures
combat.DeployChaff();
combat.DeployFlare();

// Evasive maneuvers
combat.StartEvasive();
Vector3 evasiveDir = combat.GetEvasiveDirection();

// Power priority
combat.SetPowerPriority(PowerPriority.Weapons);
```

#### ShipSubsystems
Module and power management:
- Power plant and distribution
- Multiple subsystem types (engines, shields, weapons, life support, etc.)
- Subsystem damage and repair
- Efficiency based on health

```csharp
var subsystems = GetComponent<ShipSubsystems>();

// Power distribution (Elite Dangerous style)
subsystems.SetPowerDistribution(weapons: 0.5f, shields: 0.3f, engines: 0.2f);
subsystems.IncreasePower(PowerCategory.Weapons);

// Subsystem management
subsystems.ToggleSubsystem(SubsystemType.Weapons);
float efficiency = subsystems.GetSubsystemEfficiency(SubsystemType.Engines);

// Damage and repair
subsystems.DamageSubsystem(SubsystemType.Engines, damage: 30f);
subsystems.RepairSubsystem(SubsystemType.Engines, amount: 50f);
subsystems.FullRepair();
```

#### ShipHealthSystem
Ship durability with multiple layers:
- Hull integrity
- Armor with damage reduction
- Shields with regeneration and types
- Critical damage states
- Destruction handling

```csharp
var health = GetComponent<ShipHealthSystem>();

// Take damage (full pipeline)
health.TakeDamage(100f, DamageType.Kinetic, hitPoint);

// Shield management
health.BoostShield(amount);  // Shield cell bank
health.OnShieldDepleted += () => PlayWarning();

// Status checks
if (health.IsCritical) { EnableEmergencyPower(); }
float shieldPercent = health.ShieldPercent;

// Shield types (like Elite Dangerous)
health.SetShieldType(ShieldType.BiWeave);    // Fast regen
health.SetShieldType(ShieldType.Prismatic);  // High capacity
```

#### ShipInputHandler
Complete input mapping for all ship systems:
- Flight controls (WASD, mouse)
- Weapon controls
- Targeting bindings
- Warp controls
- Power management shortcuts

```csharp
// Attach to ship - all input is handled automatically
var input = GetComponent<ShipInputHandler>();

// Configuration
input.SetMouseSensitivity(2f);
input.SetInvertPitch(true);
input.ToggleMouseMode();  // Relative vs absolute
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

### RPG Systems

The RPG module provides a complete set of interconnected systems for building deep RPG and JRPG games.

#### CharacterStatsSystem
Comprehensive character stats with primary attributes, derived stats, modifiers, and experience/leveling.
```csharp
var statsSystem = GetComponent<CharacterStatsSystem>();

// Get attributes and derived stats
float strength = statsSystem.GetAttribute(PrimaryAttribute.Strength);
float maxHP = statsSystem.GetDerivedStat(DerivedStat.MaxHealth);
float critChance = statsSystem.GetDerivedStat(DerivedStat.CriticalChance);

// Allocate attribute points on level up
statsSystem.AllocateAttributePoint(PrimaryAttribute.Strength, 3);

// Add temporary stat modifiers (buffs/debuffs)
var strengthBuff = new StatModifier("str_buff", "Power Potion", ModifierType.PercentAdd, 25f, 60f);
statsSystem.AddAttributeModifier(PrimaryAttribute.Strength, strengthBuff);

// Modify resources
statsSystem.ModifyHealth(-50f);  // Take damage
statsSystem.ModifyMana(-30f);    // Cast spell
statsSystem.FullRestore();       // Full heal

// Experience and leveling
statsSystem.AddExperience(1000);
Debug.Log($"Level: {statsSystem.Level}, EXP: {statsSystem.CurrentExperience}/{statsSystem.ExperienceToNextLevel}");

// Events
statsSystem.OnLevelUp += (prevLevel, newLevel) => Debug.Log($"Level Up! {prevLevel} -> {newLevel}");
statsSystem.OnDeath += () => Debug.Log("Character died!");
```

#### CombatSystem
Complete combat system supporting turn-based, Active Time Battle (ATB), and real-time combat.
```csharp
// Start combat encounter
CombatSystem.Instance.StartCombat(partyMembers, enemies);

// Execute actions in turn-based mode
var action = new CombatAction { 
    actionType = CombatActionType.Skill,
    damageType = DamageType.Fire,
    basePower = 1.5f 
};
CombatResult result = CombatSystem.Instance.ExecuteAction(action, targets);

// Access combo system for action RPG
CombatSystem.Instance.Combo.AddHit("slash");
float comboMultiplier = CombatSystem.Instance.Combo.CurrentMultiplier;

// Check elemental weaknesses
float multiplier = ElementalSystem.GetElementalMultiplier(DamageType.Fire, DamageType.Ice);  // 2x damage

// Limit Break system
var limitBreak = new LimitBreakSystem();
limitBreak.OnDealtDamage(100f, wasCritical: true);
if (limitBreak.IsReady)
{
    limitBreak.UseLimit();  // Execute ultimate attack
}

// Events
CombatSystem.Instance.OnCombatStart += () => Debug.Log("Battle Start!");
CombatSystem.Instance.OnTurnStart += turn => Debug.Log($"{turn.combatant.CombatantName}'s turn");
CombatSystem.Instance.OnCombatantDied += combatant => Debug.Log($"{combatant.CombatantName} was defeated!");
```

#### PartySystem
JRPG-style party management with formations, AI behaviors, and combo attacks.
```csharp
// Add party members
var warrior = new PartyMember("Cloud", "Warrior");
PartySystem.Instance.AddMember(warrior);

// Manage active party and reserves
PartySystem.Instance.AddToActiveParty(warrior);
PartySystem.Instance.SwapMembers(activeWarrior, reserveMage);
PartySystem.Instance.SetLeader(warrior);

// Formation and battle row
PartySystem.Instance.SetFormationPosition(warrior, FormationPosition.FrontLeft);
PartySystem.Instance.SetBattleRow(warrior, BattleRow.Front);
PartySystem.Instance.AutoFormation();  // Auto-arrange by class roles
float damageMultiplier = PartySystem.Instance.GetRowDamageMultiplier(warrior);

// AI behavior for party members
PartySystem.Instance.SetAIBehavior(mage, PartyAIBehavior.Support);
var recommendedAction = PartySystem.Instance.GetAIRecommendedAction(mage, allies, enemies);

// Party synergies (bonus effects from party composition)
float attackBonus = PartySystem.Instance.GetSynergyBonus("attack");
List<ComboAttack> combos = PartySystem.Instance.GetAvailableComboAttacks();
PartySystem.Instance.ExecuteComboAttack(combos[0], target);

// Trust and relationship between members
PartySystem.Instance.IncreaseTrust(warrior, 10);
PartySystem.Instance.DistributeExperience(1000);

// Events
PartySystem.Instance.OnMemberKnockedOut += member => QuickSwapKnockedOut(member);
PartySystem.Instance.OnComboAttackAvailable += combo => Debug.Log($"Combo ready: {combo.comboName}");
```

#### ClassSystem
Job/class system with multiclassing, skill trees, and class mastery.
```csharp
var classSystem = GetComponent<ClassSystem>();

// Change class
classSystem.ChangeClass("warrior");
classSystem.SetSecondaryClass("mage");  // Dual-class

// Class progression
classSystem.AddClassExperience(500);
int classLevel = classSystem.GetClassLevel("warrior");
bool isMastered = classSystem.IsClassMastered("warrior");

// Learn and manage skills
classSystem.LearnSkill("power_strike");
classSystem.EquipPassive("counter_attack");
bool hasSkill = classSystem.HasSkill("fireball");

// Skill inheritance from mastered classes
classSystem.InheritSkill("heal");  // Use healer skill as warrior
var inheritedSkills = classSystem.GetInheritedSkills();

// Check class requirements
bool canUnlock = classSystem.CanUnlockClass("paladin");
var requirements = classSystem.GetUnmetRequirements("dark_knight");

// Skill tree visualization
SkillTree tree = classSystem.GetSkillTree("warrior");
var tierNodes = tree.GetNodesAtTier(2);

// Respec
classSystem.RespecClass("warrior");  // Reset all skill points

// Events
classSystem.OnClassChanged += (old, current) => Debug.Log($"Changed from {old.className} to {current.className}");
classSystem.OnSkillLearned += skill => Debug.Log($"Learned {skill.skillName}!");
```

#### CraftingSystem
Complete crafting with multiple professions, recipes, quality tiers, and discovery.
```csharp
// Set up crafting station
CraftingSystem.Instance.SetStation(blacksmithStation);

// Learn and craft recipes
CraftingSystem.Instance.LearnRecipe("iron_sword");
CraftingResult result = CraftingSystem.Instance.CraftItem("iron_sword", quantity: 3);
Debug.Log($"Crafted {result.quality} quality items. Exp gained: {result.experienceGained}");

// Check craftable recipes
var craftable = CraftingSystem.Instance.GetCraftableRecipes(CraftingProfession.Blacksmithing);
var almostCraftable = CraftingSystem.Instance.GetAlmostCraftableRecipes(CraftingProfession.Alchemy, maxMissing: 2);

// Queue crafting
CraftingSystem.Instance.AddToQueue("health_potion", 10);
CraftingSystem.Instance.PauseCrafting();
CraftingSystem.Instance.ResumeCrafting();

// Profession leveling
int level = CraftingSystem.Instance.GetProfessionLevel(CraftingProfession.Alchemy);
var progress = CraftingSystem.Instance.GetProfessionProgress(CraftingProfession.Alchemy);

// Connect to inventory
CraftingSystem.Instance.GetMaterialCount = itemId => InventorySystem.Instance.GetItemCount(GetItemData(itemId));
CraftingSystem.Instance.ConsumeMaterial = (itemId, qty) => InventorySystem.Instance.RemoveItem(GetItemData(itemId), qty);
CraftingSystem.Instance.AddItem = (itemId, qty) => InventorySystem.Instance.AddItem(GetItemData(itemId), qty);

// Events
CraftingSystem.Instance.OnCraftingComplete += result => {
    if (result.wasCritical) Debug.Log("Critical craft! Bonus items received!");
};
CraftingSystem.Instance.OnRecipeDiscovered += recipe => Debug.Log($"Discovered: {recipe.recipeName}!");
```

#### RelationshipSystem
Social links, romance, gifts, and faction reputation for deep NPC relationships.
```csharp
// Build affinity with characters
RelationshipSystem.Instance.IncreaseAffinity("aerith", 15, InteractionType.Talk);
var relationship = RelationshipSystem.Instance.GetRelationshipType("aerith");  // Friend, CloseFriend, etc.

// Gift system with preferences
int affinityGained = RelationshipSystem.Instance.GiveGift("aerith", "flowers", "gift");
var recommendations = RelationshipSystem.Instance.GetGiftRecommendations("aerith");
var toAvoid = RelationshipSystem.Instance.GetGiftsToAvoid("aerith");

// Romance system
RelationshipSystem.Instance.StartRomance("aerith");
RelationshipSystem.Instance.Propose();
RelationshipSystem.Instance.GetMarried();
var romanceProgress = RelationshipSystem.Instance.GetRomanceProgress("aerith");

// Social events (Persona-style)
var availableEvents = RelationshipSystem.Instance.GetAvailableEvents("aerith");
var evt = RelationshipSystem.Instance.TriggerEvent("aerith_rank_5");
RelationshipSystem.Instance.CompleteEvent("aerith_rank_5", choiceIndex: 2);

// Faction reputation
RelationshipSystem.Instance.ModifyFactionReputation("knights_guild", 100);
FactionStanding standing = RelationshipSystem.Instance.GetFactionStanding("knights_guild");
var unlockedRewards = RelationshipSystem.Instance.GetUnlockedFactionRewards("knights_guild");

// Daily system
RelationshipSystem.Instance.OnNewDay("01/15");  // Reset daily limits, apply decay

// Events
RelationshipSystem.Instance.OnRankUp += (charId, rank) => Debug.Log($"{charId} rank up to {rank}!");
RelationshipSystem.Instance.OnFactionStandingChanged += (faction, standing) => Debug.Log($"{faction}: {standing}");
```

#### SkillSystem
Comprehensive skill management with learning, leveling, inheritance, and cooldowns.
```csharp
var skillSystem = GetComponent<SkillSystem>();

// Learn and upgrade skills
skillSystem.LearnSkill("fireball");
skillSystem.UpgradeSkill("fireball");  // Uses skill points
skillSystem.AddSkillExperience("fireball", 100);

// Equip skills
skillSystem.EquipSkill("fireball", slotIndex: 0);
skillSystem.EquipPassive("magic_boost");
skillSystem.SwapSkillSlots(0, 2);

// Use skills
if (skillSystem.CanUseSkill("fireball", out string reason))
{
    skillSystem.UseSkill("fireball");
}

// Skill cooldowns
bool onCooldown = skillSystem.IsOnCooldown("fireball");
float remaining = skillSystem.GetCooldownRemaining("fireball");
skillSystem.ReduceCooldown("fireball", 2f);
skillSystem.ResetAllCooldowns();

// Skill inheritance from other classes
skillSystem.InheritSkill("heal");
var inherited = skillSystem.GetInheritedSkills();

// Manage skill points
skillSystem.AddSkillPoints(5);
skillSystem.RespecSkills();  // Full respec

// Events
skillSystem.OnSkillLearned += skill => Debug.Log($"Learned {skill.skillData.skillName}!");
skillSystem.OnSkillUsed += skillId => Debug.Log($"Used {skillId}");
```

#### SummonSystem
Monster taming, evolution, and summoning system (Pokémon/SMT-style).
```csharp
// Capture monsters
bool captured = SummonSystem.Instance.AttemptCapture(slimeData, currentHPPercent: 0.2f, hasStatusEffect: true);

// Party management
SummonSystem.Instance.MoveToParty(monster.instanceId);
SummonSystem.Instance.SwapMonsters(partyMonsterId, storageMonsterId);
SummonSystem.Instance.ReorderParty(0, 2);

// Summon monsters in battle
SummonSystem.Instance.SummonMonster(monster.instanceId, position);
SummonSystem.Instance.RecallSummon(monster.instanceId);
SummonSystem.Instance.SwapSummon(activeId, replacementId);

// Monster progression
monster.AddExperience(500);
monster.LearnSkill("fire_breath");
monster.AddBondPoints(10);  // Increase bond through interaction

// Evolution
if (SummonSystem.Instance.CanEvolve(monster.instanceId, out EvolutionPath evolution))
{
    SummonSystem.Instance.EvolveMonster(monster.instanceId, evolution.evolutionId);
}

// Monster stats
float attack = monster.GetAttack();
float defense = monster.GetDefense();
float bondMultiplier = monster.GetBondMultiplier();  // Higher bond = better stats

// Interact with monsters
SummonSystem.Instance.Interact(monster.instanceId, "pet");
SummonSystem.Instance.Interact(monster.instanceId, "feed");
SummonSystem.Instance.RenameMonster(monster.instanceId, "Sparky");

// Healing
monster.Heal(50f);
monster.Revive(0.5f);
SummonSystem.Instance.HealAllParty();

// Experience distribution
SummonSystem.Instance.DistributeExperience(1000, participantIds);

// Statistics
int uniqueSpecies = SummonSystem.Instance.GetUniqueSpeciesCount();
var captureStats = SummonSystem.Instance.GetCaptureStatistics();

// Events
SummonSystem.Instance.OnMonsterCaptured += monster => Debug.Log($"Captured {monster.nickname}!");
SummonSystem.Instance.OnMonsterEvolved += (monster, evo) => Debug.Log($"Evolved into {evo.evolvedMonsterName}!");
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