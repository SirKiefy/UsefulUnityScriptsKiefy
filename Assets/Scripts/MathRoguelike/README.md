# MathRoguelike

A roguelike dungeon-crawler where **combat is solved through advanced mathematics**.
Defeat enemies by answering high-to-extreme level math problems before time runs out.

---

## Folder Structure

```
Assets/Scripts/MathRoguelike/
│
├── Core/
│   ├── MathRoguelikeGameManager.cs   Singleton – owns RunData, scene transitions
│   ├── RunData.cs                    All persistent per-run state (HP, MP, gold, floor…)
│   └── FloorManager.cs              Drives room-to-room progression on a floor
│
├── Math/
│   ├── MathProblem.cs               ScriptableObject – problem template (question, equation, answer)
│   ├── MathProblemGenerator.cs      Selects / generates problems by difficulty & topic
│   ├── EquationRenderer.cs          Converts LaTeX-like strings → TMP Rich Text
│   ├── ProblemValidator.cs          Validates player answers (numeric tolerance, expression normalise)
│   └── Problems/
│       ├── TrigonometryGenerator.cs
│       ├── CalculusGenerator.cs
│       ├── LinearAlgebraGenerator.cs
│       ├── DifferentialEquationsGenerator.cs
│       ├── ComplexAnalysisGenerator.cs
│       ├── SeriesSequencesGenerator.cs
│       ├── FourierAnalysisGenerator.cs
│       ├── VectorCalculusGenerator.cs
│       ├── NumberTheoryGenerator.cs
│       └── GroupTheoryGenerator.cs
│
├── Combat/
│   ├── BattleManager.cs             Orchestrates problem-present → answer → resolve loop
│   └── DamageCalculator.cs         Converts correctness + difficulty → damage numbers
│
├── Entities/
│   ├── PlayerStats.cs               RunData wrapper + relic modifier application
│   ├── EnemyData.cs                 ScriptableObject – enemy archetype
│   ├── EnemyInstance.cs             Runtime enemy HP/attack state
│   └── EnemyDatabase.cs             Collection SO – random enemy lookup by difficulty
│
├── Dungeon/
│   ├── RoomData.cs                  ScriptableObject – room type + config
│   ├── DungeonFloor.cs              Ordered list of rooms for one floor
│   └── DungeonGenerator.cs          Procedurally builds a floor's room sequence
│
├── Relics/
│   ├── RelicData.cs                 ScriptableObject – relic identity + effect
│   └── RelicManager.cs             Holds active relics, applies passives, exposes consumables
│
├── UI/
│   ├── PlayerHUD.cs                 HP/MP bars, gold, score, floor/room labels
│   ├── EquationDisplayUI.cs        Renders problem + multiple-choice buttons + hint panel
│   ├── MathInputUI.cs              TMP InputField + special character buttons (π, ∞, √, /)
│   ├── BattleUI.cs                  Master battle-screen controller
│   └── DungeonMapUI.cs             Horizontal room-strip map for the current floor
│
└── Data/
    └── MathEnums.cs                 MathDifficulty, MathTopic, ProblemFormat enums
```

---

## Difficulty Tiers

| Tier | Topics |
|------|--------|
| **High** | Trigonometry, Derivatives, Basic Integrals, Logarithms, Vectors |
| **Very High** | Differential Equations, Matrices/Eigenvalues, Complex Numbers, Series |
| **Extreme** | Fourier Analysis, Tensor / Vector Calculus, Number Theory, Group Theory |

---

## Equation Display

`EquationRenderer` converts a simplified LaTeX-like string into TextMeshPro rich text.

**Supported tokens (selection):**

| LaTeX | Output |
|-------|--------|
| `\frac{a}{b}` | (a)/(b) |
| `\sqrt{x}` | √(x) |
| `^{n}` | `<sup>n</sup>` |
| `_{n}` | `<sub>n</sub>` |
| `\int` | ∫ |
| `\sum` | Σ |
| `\pi` | π |
| `\partial` | ∂ |
| `\nabla` | ∇ |
| `\leq / \geq / \neq` | ≤ ≥ ≠ |

---

## Combat Loop

```
StartBattle(EnemyInstance)
  └─▶ PresentNextProblem()
        └─▶ MathProblemGenerator.GetProblem(difficulty)
        └─▶ EquationDisplayUI.Display(problem)
        └─▶ Start countdown timer
              ├─ Player submits answer → ResolveAnswer()
              │     ├─ Correct  → DamageCalculator.PlayerAttackDamage() → Enemy.TakeDamage()
              │     └─ Wrong    → DamageCalculator.EnemyAttackDamage()  → Player.TakeDamage()
              └─ Timer expires → treat as wrong
```

---

## Relic System

Add `RelicData` ScriptableObjects to the **EnemyDatabase** or Treasure rooms.
Relics grant passive bonuses (bonus damage, HP, hint cost reduction) or one-shot
effects (ReviveOnce, RerollOnce).

---

## Getting Started

1. Create a `MathRoguelikeGameManager` singleton in a persistent scene.
2. Add an `EnemyDatabase` SO and populate with `EnemyData` assets.
3. Create `RelicData` SOs and add them to the `RelicManager`'s **All Relics** list.
3. Set up a Dungeon scene with `DungeonGenerator`, `FloorManager`, `BattleManager`.
4. Wire the `BattleUI` Canvas with `EquationDisplayUI`, `MathInputUI`, `PlayerHUD`.
5. Call `MathRoguelikeGameManager.Instance.StartNewRun()` from a UI button.
