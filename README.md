# UsefulUnityScriptsKiefy

A collection of useful, reusable Unity scripts ranging from simple utilities to complex systems. Designed with professional Unity development practices in mind.

## 📁 Project Structure

```
Assets/
└── Scripts/
    ├── Core/           # Core systems (Singleton, GameManager)
    ├── Utilities/      # Helper utilities (Timer, Tween, MathUtils)
    ├── Player/         # Player controllers & health system
    ├── Camera/         # Camera follow & shake systems
    ├── UI/             # UI management & transitions
    ├── Audio/          # Audio management system
    ├── SaveSystem/     # Save/Load functionality
    ├── Events/         # Event management system
    ├── StateMachine/   # Finite state machine implementation
    ├── Dialogue/       # Dialogue system with choices
    ├── Pooling/        # Object pooling system
    └── Extensions/     # C# extension methods
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