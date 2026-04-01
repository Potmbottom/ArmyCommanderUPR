# Setup Guide

## Required Packages
- **VContainer** — dependency injection + ITickable loop
- **R3** — reactive properties and subjects
- **TextMeshPro** — UI text (used by UIControl)

---

## 1. ScriptableObjects

Create these assets once and reuse across all scenes. Place in `Assets/Configs/`.

### TroopsConfig
`Assets → Create → ArmyCommander → TroopsConfig`
Fill `Troops` list. Each entry:
| Field | Description |
|---|---|
| Index | Must match list position (0, 1, 2) |
| TroopType | Soldier / Veteran / Master |
| Prefab | GameObject with TroopControl + Animator + Rigidbody |
| MoveSpeed | Units per second |
| Health | Starting HP |
| AggressiveRange | Distance at which troop starts chasing |
| AttackRange | Distance at which troop stops and fires |
| AttackSpeed | Shots per second |
| SpawnSpeed | Seconds between spawns from barrack |
| ProjectileIndex | Index into ProjectileConfig.Projectiles |

### ProjectileConfig
`Assets → Create → ArmyCommander → ProjectileConfig`
Fill `Projectiles` list. Each entry:
| Field | Description |
|---|---|
| Index | Must match list position |
| Prefab | GameObject with ProjectileControl + Animator |
| MoveSpeed | Units per second |
| ColliderRadius | Hit detection radius (manual math, no Physics) |
| Damage | HP removed on hit |

### LevelConfig
`Assets → Create → ArmyCommander → LevelConfig`
Fill `Levels` list in order (index 0 = level 1, etc.):
| Field | Description |
|---|---|
| SceneName | Exact scene name string (must be in Build Settings) |
| UpgradeCostGold | Gold required to upgrade army tier |
| InitialSilver | Silver given to player at level start |

---

## 2. Prefabs

### Troop Prefab (one per TroopType)
```
TroopPrefab
 └─ TroopControl       (component)
     - Animator        (serialized ref → sibling/child Animator)
     - Rigidbody       (serialized ref → sibling Rigidbody)
 └─ Animator           (component)
 └─ Rigidbody          (component, Freeze Rotation XZ)
 └─ Visual mesh (child)
```
Animator must have an integer parameter named **"State"** mapped to TroopState enum values:
- 0 = Idle, 1 = Move, 2 = Attack, 3 = Dead

### Projectile Prefab (one per ProjectileDataModel)
```
ProjectilePrefab
 └─ ProjectileControl  (component)
     - Animator        (serialized ref)
 └─ Animator           (component)
 └─ Visual mesh (child)
```
Animator must have an integer parameter named **"State"** mapped to ProjectileState:
- 0 = Active, 1 = Destroyed

---

## 3. Boot Scene

**Scene name:** `Boot` (or as defined — must be first in Build Settings)

### Hierarchy
```
BootInstaller       (GameObject)
 └─ BootInstaller   (component — extends LifetimeScope)
```

### BootInstaller Inspector
| Field | Assign |
|---|---|
| Troops Config | TroopsConfig asset |
| Projectile Config | ProjectileConfig asset |
| Level Config | LevelConfig asset |

**BootInstaller is the root LifetimeScope.** All other scenes set their installer's `Parent` to this scope so configs are available globally.
Boot root object must persist between scene loads (`DontDestroyOnLoad`) so child scene scopes can resolve parent dependencies.

After setup, Boot scene auto-loads the Menu scene via `BootRoot`.

---

## 4. Menu Scene

### Hierarchy
```
MenuInstaller       (GameObject)
 └─ MenuInstaller   (component — LifetimeScope, Parent = BootInstaller)

MenuRoot            (GameObject)
 └─ MenuRoot        (component)

Canvas
 └─ StartButton     (Button → onClick: MenuRoot.OnStartButtonClicked)
```

### MenuInstaller Inspector
| Field | Assign |
|---|---|
| Parent | BootInstaller (drag BootInstaller GameObject) |

### Flow
1. VContainer builds container, inheriting configs from BootInstaller
2. `MenuRoot` is resolved from scene hierarchy and injected by `MenuInstaller`
3. `MenuRoot.Start()` called → binds serialized `StartButton` click listener
4. Player clicks Start → `MenuRoot.OnStartButtonClicked()` → loads configured game scene name (`Game` by default)

---

## 5. Game Scene (per level)

### Full Hierarchy
```
GameInstaller           (GameObject)
 └─ GameInstaller       (component — LifetimeScope, Parent = BootInstaller)

GameRoot                (GameObject)
 └─ GameRoot            (component)

Player                  (GameObject)
 └─ PlayerControl       (component)
 └─ Animator            (component)
 └─ Rigidbody           (component)
 └─ CapsuleCollider     (component)

TrainingField           (GameObject)
 └─ TrainingFieldControl (component)
 └─ Slot_0             (child — empty Transform, marks slot position)
 └─ Slot_1             (child)
 └─ ...
 └─ OrderPoint          (child — empty Transform, marks order zone center)

ArmyUpgradeZone         (GameObject)
 └─ ArmyUpgradeControl  (component)

BarrackSlot_0           (GameObject)
 └─ BarrackSlotControl  (component)

BarrackSlot_1           (GameObject, disabled if not available this level)
 └─ BarrackSlotControl  (component)

Canvas
 └─ UIControl           (component)
 └─ ResourcePanel
     └─ GoldText        (TextMeshProUGUI)
     └─ SilverText      (TextMeshProUGUI)
 └─ EnemyProgressBar   (Slider)
 └─ BuildPopup          (GameObject, starts inactive)
     └─ BuildButton_0   (Button — Soldier)
     └─ BuildButton_1   (Button — Veteran)
     └─ BuildButton_2   (Button — Master)
     └─ LockIcon_0      (GameObject overlay for locked buttons)
     └─ LockIcon_1
     └─ LockIcon_2
 └─ NextLevelPopup      (GameObject, starts inactive)
     └─ NextLevelButton (Button → onClick: GameRoot.LoadNextLevel)

EnemySpawnPoints        (GameObject — organizational only)
 └─ SpawnPoint_0        (empty Transform)
 └─ SpawnPoint_1
 └─ ...
```

---

## 6. Inspector Setup (Game Scene)

### GameInstaller
| Field | Assign |
|---|---|
| Parent | BootInstaller |
| Game Root | GameRoot GameObject |

### GameRoot
| Field | Assign |
|---|---|
| Player Control | PlayerControl component |
| Training Field Control | TrainingFieldControl component |
| Army Upgrade Control | ArmyUpgradeControl component |
| UI Control | UIControl component |
| Barrack Slot Controls | List of all BarrackSlotControl components |
| Enemy Spawn Points | List of EnemySpawnPoint entries (Transform + TroopDataIndex) |

### TrainingFieldControl
| Field | Assign |
|---|---|
| Slot Points | List of Slot_X child Transforms |

### TrainingFieldOrderPointControl (on OrderPoint child)
| Field | Assign |
|---|---|
| Training Field Control | Parent `TrainingFieldControl` (optional if parent auto-resolve is used) |

OrderPoint requirements:
- Add a Collider with `Is Trigger = true`.
- Keep `OrderPoint` as child of the TrainingField object.

### PlayerControl
| Field | Assign |
|---|---|
| Animator | Player's Animator component |
| Rigidbody | Player's Rigidbody component |
| Move Speed | Player movement speed |

### ArmyUpgradeControl
| Field | Assign |
|---|---|
| Detection Radius | Zone radius for upgrade interaction |

### BarrackSlotControl (each)
| Field | Assign |
|---|---|
| Detection Radius | Zone radius for build popup trigger |

### UIControl
| Field | Assign |
|---|---|
| Build Popup | BuildPopup GameObject |
| Build Buttons | [BuildButton_0, BuildButton_1, BuildButton_2] |
| Build Button Locks | [LockIcon_0, LockIcon_1, LockIcon_2] |
| Next Level Popup | NextLevelPopup GameObject |
| Gold Text | GoldText TextMeshProUGUI |
| Silver Text | SilverText TextMeshProUGUI |
| Enemy Progress Bar | EnemyProgressBar Slider |

---

## 7. Runtime Binding Flow

This is the exact sequence that runs when a game scene loads:

### Step 1 — VContainer builds container
`GameInstaller.Configure()` runs. All singletons and entry points are registered but not yet instantiated:
- PresentationModels: `FieldPModel`, `TrainingFieldPModel`, `PlayerPModel`, `ArmyUpgradePModel`, `ResourcePModel`, `UIModel`
- Services (ITickable): `SpawnService`, `AIService`, `BarrackService`, `ProjectileService`, `TransformService`
- Services (non-tickable): `ResourceService`, `UIService`, `ArmyUpgradeService`
- Root: `GameRoot`

### Step 2 — VContainer injects dependencies
Each service's `SetDependency([Inject])` is called with resolved instances:
- `SpawnService` → subscribes to `FieldPModel.OnTroopAdded/Removed` and `OnProjectileAdded/Removed`
- `AIService` → subscribes to `TrainingFieldPModel.OnOrderGiven`
- `BarrackService` → subscribes to `FieldPModel.OnTroopAdded/Removed`
- `ProjectileService` → subscribes to `FieldPModel.OnTroopAdded/Removed`
- `TransformService` → holds references, no subscriptions needed
- `ResourceService` → subscribes to `FieldPModel.OnTroopRemoved`
- `UIService` → subscribes to `UIModel.OnBuildSelected`
- `ArmyUpgradeService` → subscribes to `ArmyUpgradePModel.OnUpgraded`
- `GameRoot` → receives all models and services

### Step 3 — GameRoot.Start() (IStartable)
```
GameRoot.Start()
 ├─ PlayerControl.Bind(PlayerPModel)
 │   └─ subscribes PlayerPModel.Velocity → Animator "Speed"
 │
 ├─ TrainingFieldControl.Bind(TrainingFieldPModel)
 │   └─ sets TrainingFieldPModel slot positions from scene Transforms
 │
 ├─ ArmyUpgradeControl.Bind(ArmyUpgradePModel, playerTransform)
 │
 ├─ UIControl.Bind(UIModel, ResourcePModel, FieldPModel)
 │   └─ subscribes UIModel events → show/hide panels
 │   └─ subscribes ResourcePModel.Gold/Silver → text labels
 │   └─ stores initial enemy count for progress bar
 │
 ├─ For each BarrackSlotControl:
 │   ├─ new BarrackSlotPModel() created
 │   ├─ BarrackSlotControl.Bind(slotModel, playerTransform)
 │   │   └─ sets slotModel.BuildPoint = control transform position
 │   │   └─ subscribes slotModel.TroopType → activate/deactivate visual
 │   ├─ BarrackService.RegisterSlot(slotModel)
 │   │   └─ subscribes slotModel.TroopType → start/stop production
 │   └─ UIService.RegisterSlot(slotModel)
 │       └─ subscribes slotModel.IsPlayerInZone → show/hide build popup
 │
 ├─ SpawnEnemies()
 │   └─ For each EnemySpawnPoint:
 │       └─ FieldPModel.CreateTroop(dataIndex, Enemy, position, position, health, Aggressive)
 │           └─ OnTroopAdded fires → SpawnService instantiates prefab from pool → TroopControl.Bind(model)
 │
 └─ ResourcePModel.AddSilver(levelData.InitialSilver)
```

### Step 4 — VContainer PlayerLoop runs ITickables each frame
Tick order is fixed by registration order in `GameInstaller`:
1. `SpawnService.Tick()` — calls `TroopControl.Tick()` and `ProjectileControl.Tick()` on all active pooled controls
2. `AIService.Tick()` — per troop: reads AIBehaviour, sets State and TargetPosition
3. `BarrackService.Tick()` — accumulates time, spawns troops if not in order phase and slots available
4. `ProjectileService.Tick()` — fires projectiles from attacking troops, moves projectiles, checks collision
5. `TransformService.Tick()` — calculates Velocity per troop based on TargetPosition, sets on model

Each `TroopControl.Tick()` (called from SpawnService):
- writes `transform.position` → `TroopBaseModel.Position`
- writes `TroopBaseModel.Velocity` → `Rigidbody.linearVelocity`

---

## 8. Key Gameflow Events

### Player gives charge order
```
Player walks into OrderPoint zone
→ OrderPoint `TrainingFieldOrderPointControl.OnTriggerEnter`
→ forwards to parent `TrainingFieldControl.OnOrderTriggerEnter`
→ TrainingFieldPModel.GiveAttackOrder()
→ OnOrderGiven fires
→ AIService: all allied troops SetAIBehaviour(Aggressive)
→ TrainingFieldPModel.IsOrderActive = true
→ BarrackService.Tick() stops production while IsOrderActive
```

### Troop attacks and kills enemy
```
AIService sets troop State = Attack
→ ProjectileService starts attack timer for that troop
→ Timer fires → ProjectileService.FireProjectile()
→ FieldPModel.CreateProjectile(...)
→ OnProjectileAdded fires → SpawnService spawns projectile GameObject → ProjectileControl.Bind(model)
→ ProjectileService.TickProjectiles() moves projectile each frame
→ Distance check hits enemy troop
→ enemyTroop.MakeDamage(damage)
→ enemyTroop.CurrentHealth reaches 0 → TroopBaseModel sets State = Dead internally
→ BarrackService death sub fires → FieldPModel.RemoveTroop(enemy)
→ OnTroopRemoved fires:
    ├─ SpawnService: Unbind + return to pool
    └─ ResourceService: ResourcePModel.AddGold(1)
→ ProjectileService: RemoveProjectile → OnProjectileRemoved → SpawnService returns projectile to pool
```

### All allied troops die (wave end)
```
Last allied troop removed from FieldPModel
→ BarrackService.OnTroopRemoved checks GetAlliedCount() == 0
→ TrainingFieldPModel.ResetOrder()  (IsOrderActive = false)
→ BarrackService.Tick() resumes production on next frame
```

### Player builds a barrack
```
Player walks into BarrackSlotControl zone
→ BarrackSlotControl.Tick() detects overlap
→ BarrackSlotPModel.SetPlayerInZone(true)
→ UIService sub fires → UIModel.ShowBuildPopup(availableTypes)
→ UIControl sub fires → BuildPopup panel activates, locks applied by army level
→ Player clicks a build button
→ UIControl: UIModel.SelectBuild(TroopType)
→ UIService.OnBuildSelected fires
→ ResourcePModel.TrySpendSilver(1) checked
→ ActiveSlot.SetTroopType(type)
→ BarrackSlotControl sub fires (visual update)
→ BarrackService.OnSlotTypeChanged fires → starts production timer for that slot
→ UIModel.HideBuildPopup()
```

### Army upgrade
```
Player walks into ArmyUpgradeZone and presses E
→ ArmyUpgradeControl.Tick() detects → ArmyUpgradePModel.Upgrade()
→ ArmyUpgradePModel.CurrentLevel increments
→ OnUpgraded fires → ArmyUpgradeService.OnUpgraded
→ If Level3: UIModel.ShowNextLevelPopup()
→ Player clicks Next Level button → GameRoot.LoadNextLevel()
→ PlayerPrefs["CurrentLevel"] incremented → next scene loaded
```

---

## 9. Adding a New Level

1. Duplicate an existing Game scene
2. Adjust terrain, enemy spawn points, barrack slot positions, training field slots
3. Set `GameInstaller.Parent` to the BootInstaller in the new scene
4. Reassign all serialized references in GameRoot, TrainingFieldControl, UIControl
5. Add the scene name to `LevelConfig.Levels` in the correct index position
6. Add scene to Build Settings
