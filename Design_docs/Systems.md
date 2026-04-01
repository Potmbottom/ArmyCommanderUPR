# Systems

## Architecture Rules
- **Control** — MonoBehaviour. Reads/sets data on its bound model. No public fields or methods. Only exception: pooled controls (`TroopControl`, `ProjectileControl`) expose `Tick`/`FixedTick` methods called by `SpawnService`. Fully data-driven. No knowledge of other models or services.
- **PresentationModel** — C# class. Has reactive fields. Logic limited to model-internal state changes.
- **Service** — C# class. Operates only on PresentationModels. No public fields or methods.
- **DataModel** — C# class. Pure serializable data, no logic, no methods.
- Controls and DataModels have no injections.
- Runtime reads/writes must be done in tick methods (`Tick`/`FixedTick`/`LateTick`), not in reactive subscriptions (avoids GC pressure).
- Whoever calls `new` on a model owns its `Dispose`.
- Every PresentationModel has a corresponding interface (e.g. `IFieldPModel`). All injections and bindings use the interface, never the concrete class.
- All public fields in PresentationModels are `readonly`. Mutable state is exposed via getter properties and mutated only through explicit public methods.
- `Subject<T>` fields are private and exposed as `Observable<T>` properties.
- Use reactive state only when consumers subscribe to change events.
- For polled/per-frame state, expose plain readonly properties and mutate via explicit public methods.
- If a value needs both polling and subscriptions, expose both: plain readonly current value + `Observable<T>` change stream.
- In classes that use DI: `SetDependency(...)` is for dependency assignment only (no logic, no subscriptions).
- Reactive subscriptions must be wired in `IInitializable.Initialize()`.

---

## Base Classes

### BaseControl\<TModel\>
- Manages `Model`, `Disposables`, `ReleaseToPool` (internal, set by SpawnService)
- `Bind(model)` — sets up disposables, assigns model, calls `OnModelBind(model)`
- `OnModelBind(model)` — abstract, implemented by subclass, used as initializer with reactive subscriptions
- `Release()` — disposes, nulls model, calls `ReleaseToPool` (returns to Unity pool)
- `OnDestroy` — disposes

---

## Models

### TroopDataModel (serializable)
Fields: Index, TroopType, Prefab, MoveSpeed, Health, AggressiveRange, AttackRange, AttackSpeed, SpawnSpeed, ProjectileIndex

### ProjectileDataModel (serializable)
Fields: Index, Prefab, MoveSpeed, LifeTime, ColliderRadius, Damage

### ResourceDropDataModel (serializable)
Fields: Index, ResourceType, Prefab, Amount

### LevelData (serializable)
Fields: LevelPrefab, UpgradeCostGold, BarrackSoldierCostSilver, BarrackVeteranCostSilver, BarrackMasterCostSilver, InitialSilver, BarrackTroopIdOverrides (list of BarrackTroopIdOverrideData)

### BarrackTroopIdOverrideData (serializable)
Fields: TroopType, IdOverride

### EnemySpawnPoint (serializable)
Fields: SpawnTransform, TroopDataIndex

### LevelRuntimeRoot
- MonoBehaviour placed on each level prefab root
- Holds per-level scene content references consumed by `GameRoot`:
  - `PlayerControl` (single)
  - `TrainingFieldControl` (single)
  - `ArmyUpgradeControl` (single)
  - `BarrackSlotControls` (list)
  - `EnemySpawnPoints` (list)

### TroopPModel : ITroopPModel (IDisposable)
Readonly properties: DataIndex, Team, HomePosition (set via constructor)
Mutable properties (via setter methods): Position, Velocity, TargetPosition, AIBehaviour
Private fields: _health (ReactiveProperty<float>) — initialized by constructor health, modified only by MakeDamage; _state (ReactiveProperty)
State exposure:
- State (plain readonly current value for polling in services)
- OnStateChanged (Observable stream for subscriptions in controls/services)
- CurrentHealth (plain readonly current value)
- OnHealthChanged (Observable stream for health subscriptions)
- **TroopState**: Idle | Move | Attack | Dead
- **AIBehaviour**: Home | Aggressive
  - Home → AIService drives Idle and Move toward HomePosition only
  - Aggressive → AIService drives Move toward enemy and Attack
  - Dead is sealed; set internally by MakeDamage when health reaches 0
- Methods: SetPosition, SetVelocity, SetTargetPosition, SetState, SetAIBehaviour, MakeDamage, Dispose

### ProjectilePModel : IProjectilePModel (IDisposable)
Readonly properties: DataIndex, OwnerTeam, TargetPosition, Direction (set via constructor, never changes)
Mutable properties (via setter methods): Position
Private: _state (ReactiveProperty)
State exposure:
- State (plain readonly current value for polling)
- OnStateChanged (Observable stream for subscriptions in controls/services)
- Methods: SetPosition, SetState, Dispose

### FieldPModel : IFieldPModel
- Single list of ITroopPModel (Team enum distinguishes allied/enemy)
- Single list of IProjectilePModel
- Single list of IResourceDropPModel
- Private subjects exposed as Observable: OnTroopAdded, OnTroopRemoved, OnProjectileAdded, OnProjectileRemoved, OnResourceDropAdded, OnResourceDropRemoved
- Creates models via factory methods, fires subjects on add/remove
- Calls model.Dispose() after firing OnRemoved subject
- Methods: CreateTroop (returns ITroopPModel), CreateProjectile (returns IProjectilePModel), CreateResourceDrop (returns IResourceDropPModel), RemoveTroop, RemoveProjectile, RemoveResourceDrop, GetAlliedCount, GetEnemyCount

### ResourceDropPModel : IResourceDropPModel (IDisposable)
Readonly properties: DataIndex, ResourceType, Amount, Position
Private field: _collected (bool)
Private subject exposed as Observable: OnCollected (fires once on first collect call)
- Methods: Collect, Dispose

### BarrackSlotPModel : IBarrackSlotPModel (IDisposable)
Mutable properties (via setter methods): BuildPoint (troop spawn position for this barrack), TroopType, IsPlayerInZone
Reactive: TroopType, IsPlayerInZone exposed as ReadOnlyReactiveProperty
- Created at runtime by GameRoot — GameRoot owns Dispose
- Methods: SetBuildPoint, SetTroopType, SetPlayerInZone, Dispose

### TrainingFieldPModel : ITrainingFieldPModel
Private subject exposed as Observable: OnOrderGiven
Readonly property: IsOrderActive (plain bool, polled by BarrackService)
Readonly property: CanGiveOrder (plain bool; true only when at least one barrack has an active troop production)
- Methods: SetPoints, GiveAttackOrder, ResetOrder, SetOrderAvailable

### PlayerPModel : IPlayerPModel
Readonly properties: MoveDirection (plain `Vector2`, not reactive), Position (plain `Vector3`), IsDead (plain `bool`)
Private fields: _currentHealth (float) — initialized in constructor, modified only by MakeDamage
Private subject exposed as Observable: OnDead (fires once on alive -> dead transition)
- Methods: SetMoveDirection, SetPosition, MakeDamage

### ArmyUpgradePModel : IArmyUpgradePModel
Readonly property: CurrentLevel (plain ArmyLevel)
Private subjects exposed as Observable: OnUpgradeRequested, OnUpgraded
- Methods: RequestUpgrade, Upgrade, SetLevel
  - `SetLevel(ArmyLevel)` is initialization-only and updates `CurrentLevel` without firing `OnUpgraded`

### ResourcePModel : IResourcePModel
Private ReactiveProperties exposed as ReadOnlyReactiveProperty: Gold, Silver
- Methods: AddGold, AddSilver, TrySpendGold, TrySpendSilver

### UIModel : IUIModel
Private reactive properties exposed as ReadOnlyReactiveProperty: Gold, Silver, EnemyProgress
Private subjects exposed as Observable: OnShowBuildPopup, OnHideBuildPopup, OnBuildSelected, OnShowNextLevelPopup, OnShowEndGamePopup, OnNextLevelRequested, OnReloadRequested
- Enemy progress flow:
  - `InitializeEnemyProgress(initialEnemyCount)` stores initial enemy count and resets progress
  - `UpdateEnemyProgress(remainingEnemyCount)` updates normalized progress `[0..1]`
- Methods: ShowBuildPopup, HideBuildPopup, SelectBuild, ShowNextLevelPopup, ShowEndGamePopup, SetGold, SetSilver, InitializeEnemyProgress, UpdateEnemyProgress, RequestNextLevel, RequestReload
  - `ShowBuildPopup(availableTypes, affordableTypes)` pushes both level-unlocked and currently affordable troop sets so view can lock unaffordable entries

---

## Controls

### TroopControl : BaseControl\<ITroopPModel\>
- OnModelBind:
  - sync pooled Rigidbody position to model.Position on bind (spawn/reuse correctness)
  - reset Rigidbody.velocity to zero before simulation resumes
  - restore Rigidbody interaction flags on reuse (`detectCollisions = true`)
  - re-enable all troop colliders on reuse (root + children)
  - enforce Rigidbody plane/tilt locks (`FreezePositionY`, `FreezeRotationX`, `FreezeRotationZ`) while preserving existing constraints
  - subscribe OnStateChanged → Animator integer `State` parameter with 4 states:
    - `0 = idle`
    - `1 = run`
    - `2 = attack`
    - `3 = death`
  - Animator controller requirement for death:
    - `Any State -> Death_A` transition by `State == 3` must have `Can Transition To Self = false`
    - prevents continuous self-reentry that restarts death clip each frame
    - each locomotion/combat state (`Idle_A`, `Running_A`, `Ranged_1H_Shoot`) also has explicit `State == 3 -> Death_A` transition as fallback wiring
  - on `Dead` state: immediately disable physics/collision/movement representation for the corpse:
    - set Rigidbody velocity/angularVelocity to zero
    - call `Rigidbody.Sleep()` to stop simulation jitter while keeping animator transition flow untouched
    - set `detectCollisions = false`
    - disable all colliders
  - applies current model state to Animator on bind (pooled reuse safety)
  - damage VFX safety:
    - health-change subscription is added to `Disposables` (disposed on release/destroy)
    - VFX access uses Unity object-null semantics (`_vfx == null` guard), not only null-conditional access, to avoid `MissingReferenceException` on destroyed pooled objects
- FixedTick: model.SetPosition(transform.position); read model.Velocity → Rigidbody.velocity
- FixedTick: instant yaw rotation on XZ plane, no smoothing:
  - default facing source is current movement direction (`model.Velocity`)
  - when `model.State == Attack`, facing source switches to target direction (`model.TargetPosition - transform.position`) so ranged attacks always aim at current target
- FixedTick: when model state is `Dead`, skips Rigidbody movement/rotation writes
- FixedTick: always re-syncs animator integer `State` from current `model.State` (authoritative poll) as safety against missed reactive event delivery

### ProjectileControl : BaseControl\<IProjectilePModel\>
- OnModelBind: subscribe OnStateChanged → Animator "State" parameter
- Tick: read model.Position → transform.position
- Tick: instant yaw rotation toward projectile flight direction (`model.Direction` on XZ), no smoothing

### ResourceDropControl : BaseControl\<IResourceDropPModel\>
- OnModelBind: sync transform position from model.Position
- OnTriggerEnter: calls `model.Collect()` once when collider tag is `Player`
- Uses trigger collider only (no physics simulation/forces)

### ResourceDropRotateControl
- MonoBehaviour visual utility for resource drop idle spin.
- Uses DOTween local-y rotation loop with linear easing for slow continuous rotation.
- Starts tween in `OnEnable`, kills tween in `OnDisable`.
- Stores initial local rotation and restores it before each pooled reuse spin start.

### BarrackSlotControl : BaseControl\<IBarrackSlotPModel\>
- OnModelBind: sets `BuildPoint` from serialized spawn transform (`_spawnPoint`) with fallback to root transform.position; subscribes TroopType → toggle one of three serialized visuals (Soldier/Veteran/Master)
- TroopType.Empty → all three visuals disabled
- Root control object stays active; only child visuals are toggled
- OnTriggerEnter/Exit → model.SetPlayerInZone

### TrainingFieldControl : BaseControl\<ITrainingFieldPModel\>
- OnModelBind: call model.SetPoints with serialized slot transforms
- Receives order trigger via child relay control and calls `model.GiveAttackOrder` (model ignores call when `CanGiveOrder` is false)

### TrainingFieldOrderPointControl
- MonoBehaviour relay placed on external/child order trigger collider object (e.g. `OrderPoint`)
- OnTriggerEnter forwards to parent `TrainingFieldControl`
- Auto-resolves parent `TrainingFieldControl` via `GetComponentInParent` if not assigned explicitly

### MeshMaterialColorControl
- MonoBehaviour utility for scene visuals on a single attached object.
- Requires a local `Renderer` on the same GameObject.
- Applies per-renderer color override through `MaterialPropertyBlock` (no material instancing and no shared asset mutation).
- Reapplies in `Awake` and `OnValidate` so runtime and editor changes stay in sync.
- Resolves color property from renderer shared material and supports `_BaseColor` (URP) and `_Color`.

### ArmyUpgradeControl : BaseControl\<IArmyUpgradePModel\>
- OnTriggerEnter:
  - calls `model.RequestUpgrade()`

### PlayerControl : BaseControl\<IPlayerPModel\>
- OnModelBind: no reactive subscriptions; enforce Rigidbody plane/tilt locks (`FreezePositionY`, `FreezeRotationX`, `FreezeRotationZ`) while preserving existing constraints
- OnModelBind: force `Rigidbody.interpolation = Interpolate` for smoother visual motion at higher speed
- OnModelBind/LateTick: if serialized camera reference is missing (prefab cannot keep scene camera ref), auto-resolves camera via `Camera.main`
- FixedTick: read model.MoveDirection → set Rigidbody.velocity * moveSpeed, then sync model.SetPosition from Rigidbody.position
- FixedTick: instant yaw rotation toward move direction (XZ), no smoothing
- FixedTick: drives Animator integer `State` with 3 states:
  - `0 = idle` when not moving
  - `1 = run` when moving in any direction
  - `2 = death` when `model.IsDead`
- FixedTick: when `model.IsDead`, stops movement velocity and keeps death animation state active
- LateTick: follow player camera using `Rigidbody.position` + serialized `cameraOffset` / `cameraFollowSmooth`

### UIControl : BaseControl\<IUIModel\>
- Binds to single presentation model interface (`IUIModel`)
- OnModelBind:
  - resets runtime popups (`build`, `next-level`, `end-game`) to hidden state
  - toggles HUD visibility (gold/silver texts + enemy progress slider): hidden when any popup is active, visible when all popups are closed
  - toggles serialized "all enemies dead" indicator object near progress UI when enemy progress reaches completion
  - subscribes to popup/resource/progress streams from model
  - binds build button clicks to `model.SelectBuild(...)`
  - in build popup, each troop button is considered locked when type is not level-unlocked or when current silver is insufficient (type not in affordable set)
  - binds next/reload button clicks in code (serialized reference or auto-find fallback) and forwards intent through `model.RequestNextLevel()` / `model.RequestReload()`
- No per-frame polling inside control; enemy progress is pushed through model state

### VirtualJoystick (Android only)
- IPointerDownHandler, IDragHandler, IPointerUpHandler
- Exposes Direction (Vector2) — read by AndroidInputProvider
- Platform gate: in `Awake`, joystick GameObject disables itself on non-Android targets (`!UNITY_ANDROID || UNITY_EDITOR`)
- Uses fixed scene position at runtime (does not reposition root RectTransform on pointer down)

### FpsDisplayControl
- MonoBehaviour debug utility for lightweight runtime FPS visibility.
- Keeps internal smoothed delta time (`unscaledDeltaTime`) and draws FPS text in top-left corner using `OnGUI`.
- Builds FPS label text with reusable `StringBuilder` to avoid per-frame string interpolation allocations.
- Uses serialized display settings: text color, font size, and screen margin.

### AndroidFrameRateControl
- MonoBehaviour runtime utility for mobile frame cap setup.
- In `Awake`, disables vSync (`QualitySettings.vSyncCount = 0`) and applies configured `Application.targetFrameRate`.
- Applies frame-rate cap only on Android runtime (`UNITY_ANDROID && !UNITY_EDITOR`) so editor/other platforms keep their own defaults.
- Uses serialized target FPS value (default `60`) for easy tuning from inspector.

---

## Services

### InputService (ITickable)
- Tick: reads IInputProvider.GetMoveDirection() → IPlayerPModel.SetMoveDirection

### IInputProvider
- PCInputProvider: `Input.GetAxis` Horizontal/Vertical
- AndroidInputProvider: reads VirtualJoystick.Direction — injected via SetDependency

### SpawnService (ITickable, IFixedTickable)
- Listens to FieldPModel OnTroopAdded/Removed, OnProjectileAdded/Removed, OnResourceDropAdded/Removed
- Subscriptions are created in `Initialize()` (IInitializable), not in `SetDependency()`
- Uses Unity ObjectPool\<TroopControl\>, ObjectPool\<ProjectileControl\>, and ObjectPool\<ResourceDropControl\> (one pool per DataIndex)
- actionOnGet: SetActive(true), wires ReleaseToPool callback onto control
- actionOnRelease: SetActive(false)
- On add: pool.Get() → Bind(model)
- Troop pool safety: on release, before deactivation, move troop control to stash position with `x = 1000`, `z = 1000`, and keep current `y` unchanged so stale pooled transforms never appear in gameplay area
- On remove: control.Release() — single call, handles unbind + pool return
- Tick: calls Tick() on active ProjectileControls (non-physics visual transform sync)
- FixedTick: calls FixedTick() on active TroopControls (Rigidbody sync)

### AIService (ITickable)
- Listens to TrainingFieldPModel.OnOrderGiven → flips all allied troops to Aggressive
- Subscription is created in `Initialize()` (IInitializable), not in `SetDependency()`
- Tick: per troop, reads AIBehaviour, drives State and TargetPosition
  - Home: move toward HomePosition on XZ plane (ignores Y) → Idle on planar arrival
  - Home idle stabilization: uses arrival/keep-idle hysteresis so units already idling at home do not flip Move/Idle on tiny drift near threshold
  - Aggressive:
    - Allied troops: find nearest enemy troop
    - Enemy troops: find nearest target between allied troop and unique Player entity (if Player is alive)
    - Nearest-target checks and range gates use squared XZ distance (`sqrMagnitude`) to avoid per-comparison sqrt cost in Tick loop
    - If no valid target exists, or nearest target is outside AggressiveRange, troop uses home-return logic (move to HomePosition and hold idle there)
    - Move if target is in AggressiveRange → Attack if in AttackRange
- Does NOT handle death — TroopPModel.MakeDamage sets Dead internally

### BarrackService (ITickable)
- Listens to FieldPModel OnTroopAdded → subscribes to troop State for death detection
- Field subscriptions are created in `Initialize()` (IInitializable), not in `SetDependency()`
- On State = Dead: starts delayed removal timer (`2s`) so death animation can play
- During delayed death window, troop remains in field list but is non-interactive (handled by `TroopControl` dead-state disable path)
- After timer elapses: calls FieldPModel.RemoveTroop
- Dead-troop timer tick uses a reusable key cache list (cleared/reused each frame) to avoid per-tick `new List<>(dict.Keys)` allocations
- On all allied troops removed: calls TrainingFieldPModel.ResetOrder
- RegisterSlot(IBarrackSlotPModel): subscribes to TroopType changes → start/stop production
- Recomputes training-field order availability from active productions:
  - if all barrack slots are `TroopType.Empty`, sets TrainingFieldPModel.SetOrderAvailable(false)
  - if at least one barrack has troop type, sets TrainingFieldPModel.SetOrderAvailable(true)
- Tick: accumulates Time.deltaTime per active slot, spawns troop when SpawnSpeed elapsed
  - Production paused while TrainingFieldPModel.IsOrderActive
  - No spawn if all training field slots are occupied
  - Spawn data resolves by selected `TroopType` with per-level override from `LevelData.BarrackTroopIdOverrides`
  - `BarrackTroopIdOverrideData` item contract:
    - `TroopType` (key)
    - `IdOverride` (troop data index to instantiate)
  - If override index is invalid/out of range, service falls back to `TroopsConfig` lookup by `TroopType`

### ProjectileService (ITickable)
- Listens to FieldPModel OnTroopAdded → subscribes to State changes
- Field subscriptions are created in `Initialize()` (IInitializable), not in `SetDependency()`
- Maintains per-team alive troop caches (`allied`, `enemy`) for candidate filtering in targeting/collision checks
- On State = Attack: starts attack timer for that troop
- On State != Attack: removes timer
- Tick:
  - Attack timers: accumulate delta, fire at 1/AttackSpeed rate
    - Attack-timer key iteration uses a reusable key cache list (cleared/reused each frame) to avoid per-tick `new List<>(dict.Keys)` allocations
    - Allied shooters target nearest enemy troop
    - Enemy shooters target nearest between allied troop and unique Player entity (if Player is alive)
    - Creates projectile in FieldPModel toward chosen target position
  - Projectiles: move each projectile by fixed `Direction` and speed, manual collision check against opposite-team cache only
  - Collision/target nearest checks use squared distance comparisons (no sqrt) with an AABB prefilter before narrow-phase check
  - On hit:
    - Damage enemy troop (existing path), or
    - Damage Player when projectile owner is enemy and Player collider-radius distance check passes
    - RemoveProjectile from FieldPModel
  - Per-projectile lifetime timer: if alive time reaches `ProjectileDataModel.LifeTime`, RemoveProjectile from FieldPModel

### TransformService (ITickable)
- Tick: per troop, calculates XZ direction to TargetPosition, sets Velocity on model
  - Stop epsilon is evaluated on XZ plane (ignores Y)
  - Idle/Attack/Dead troops get zero velocity
  - Projectile movement handled by ProjectileService

### ResourceService
- Listens to FieldPModel.OnTroopRemoved
- Listens to FieldPModel.OnResourceDropAdded and tracks drop collect events
- Subscription is created in `Initialize()` (IInitializable), not in `SetDependency()`
- Enemy removed → FieldPModel.CreateResourceDrop(gold drop)
- Allied removed → FieldPModel.CreateResourceDrop(silver drop)
- On drop collect:
  - gold drop → ResourcePModel.AddGold(amount)
  - silver drop → ResourcePModel.AddSilver(amount)
  - remove collected drop from FieldPModel

### UIService
- RegisterSlot(IBarrackSlotPModel): subscribes IsPlayerInZone
  - true and slot troop type is `Empty` → UIModel.ShowBuildPopup(availableTypes based on ArmyUpgradePModel.CurrentLevel, affordable subset by current silver)
  - true and slot troop type is not `Empty` → does not show build popup
  - false → UIModel.HideBuildPopup
- Listens to UIModel.OnBuildSelected:
  - resolves selected troop silver cost from current `LevelData` (`BarrackSoldierCostSilver` / `BarrackVeteranCostSilver` / `BarrackMasterCostSilver`)
  - fallback when cost is not configured (`<= 0`) uses default `1`
  - runs `TrySpendSilver(cost)` → activeSlot.SetTroopType
- Mirrors ResourcePModel values into UIModel state:
  - `ResourcePModel.Gold` → `UIModel.SetGold`
  - `ResourcePModel.Silver` → `UIModel.SetSilver`
- Listens to FieldPModel troop changes and updates enemy progress reactively:
  - `OnTroopAdded` / `OnTroopRemoved` (enemy-only) → `UIModel.UpdateEnemyProgress`
  - tracks initial enemy baseline and re-initializes progress denominator when baseline grows
- Root UIModel subscription is created in `Initialize()` (IInitializable), not in `SetDependency()`

### ArmyUpgradeService
- Listens to ArmyUpgradePModel.OnUpgradeRequested and OnUpgraded
- Subscription is created in `Initialize()` (IInitializable), not in `SetDependency()`
- On upgrade request:
  - computes current level index from `CurrentLevel`
  - runs `TryUpgrade(currentLevelIndex)` for resource-gated upgrade flow
- On any successful tier upgrade (`OnUpgraded`) → `UIModel.ShowNextLevelPopup` (level completion gate for current progression step)
- TryUpgrade: checks gold cost from `LevelConfig.UpgradeCostGold` → `ResourcePModel.TrySpendGold` → `ArmyUpgradePModel.Upgrade`

### Save Storage (`ISaveStorage`)
- Unified save abstraction for runtime/editor key-value progression data access.
- Current implementation: `PlayerPrefsStorage` wraps Unity `PlayerPrefs`.
- Interface contract:
  - `HasKey(string key)`
  - `GetInt(string key, int defaultValue = 0)`
  - `SetInt(string key, int value)`
  - `DeleteKey(string key)`
  - `Save()`
- Safety rules in wrapper:
  - validates keys (`null`/empty/whitespace` =>` safe fallback + warning)
  - wraps backend reads/writes with guarded execution and non-throwing fallback behavior
- Runtime consumers (`GameRoot`, `BarrackService`, `UIService`) read progression index through `ISaveStorage`, not direct `PlayerPrefs`.
- Editor tools can instantiate `PlayerPrefsStorage` directly when DI container is not available.

---

## Update Tick Execution Order (ITickable, defined by GameInstaller bind order)
1. InputService
2. SpawnService (drives ProjectileControl.Tick)
3. AIService
4. BarrackService
5. ProjectileService
6. TransformService

## Fixed Tick Execution Order (IFixedTickable, defined by GameInstaller bind order)
1. SpawnService (drives TroopControl.FixedTick)
2. GameRoot (drives PlayerControl.FixedTick)

## Late Tick Execution Order (ILateTickable, defined by GameInstaller bind order)
1. GameRoot (drives PlayerControl.LateTick)

---

## Enemy Spawn
- Level prefab (`LevelRuntimeRoot`) holds list of EnemySpawnPoint (Transform + TroopDataIndex)
- GameRoot loads level prefab on Start, then creates enemy TroopPModels via IFieldPModel with AIBehaviour = Aggressive
- Enemy spawn/home Y is normalized to loaded PlayerControl Y so prefab placement differences do not lift enemies above/below gameplay plane
- No production loop — spawned once at level start

---

## Lifecycle / Ownership
- TroopPModel: created by FieldPModel, disposed by FieldPModel.RemoveTroop (after OnTroopRemoved fires)
- ProjectilePModel: created by FieldPModel, disposed by FieldPModel.RemoveProjectile (after OnProjectileRemoved fires)
- ResourceDropPModel: created by FieldPModel, disposed by FieldPModel.RemoveResourceDrop (after OnResourceDropRemoved fires)
- BarrackSlotPModel: created by GameRoot, disposed by GameRoot.Dispose (VContainer calls on scope destroy)
- VContainer-registered PresentationModels: disposed by VContainer on scope destroy if IDisposable

---

## Configs (ScriptableObjects, bound in Boot Scene via VContainer)
1. LevelConfig
2. TroopsConfig
3. ProjectileConfig
4. ResourceDropConfig

---

## Scenes

### Boot Scene
- BootInstaller (LifetimeScope root) — binds all four configs as singletons
- BootInstaller also binds `ISaveStorage` to `PlayerPrefsStorage` as singleton at root scope.
- BootRoot (MonoBehaviour) — marks root object as `DontDestroyOnLoad`, then loads Menu scene on Start using serialized `MenuSceneName`

### Menu Scene
- MenuInstaller (LifetimeScope, parent = BootInstaller)
- MenuInstaller registers `MenuRoot` from scene hierarchy (no manual serialized installer link)
- MenuRoot (IStartable): binds serialized Start button in code and loads single `Game` scene on start button click

### Game Scene
- GameInstaller (LifetimeScope, parent = BootInstaller)
- GameInstaller registers PresentationModels by interface (`IFieldPModel`, `ITrainingFieldPModel`, etc.)
- `BarrackService` is registered as entry point and `AsSelf()` so `GameRoot` can resolve it for slot registration
- GameRoot (IStartable, IFixedTickable, ILateTickable, IDisposable):
  - loads current level prefab from `LevelConfig` and reads level refs via `LevelRuntimeRoot`
  - reads saved progression index through `ISaveStorage` (`ProgressionKeys.CurrentLevel`)
  - maps saved progression index to army tier (`Level1..Level3`) and initializes `ArmyUpgradePModel` through `SetLevel` before gameplay interactions
  - binds `PlayerControl`, `TrainingFieldControl`, `ArmyUpgradeControl` from loaded prefab
  - keeps only shared scene refs (`UIControl`, optional level container)
  - subscribes to `UIModel.OnNextLevelRequested` and calls `LoadNextLevel()`
  - subscribes to `UIModel.OnReloadRequested` and calls `ReloadCurrentLevel()`
  - subscribes to `IPlayerPModel.OnDead` in startup and triggers `UIModel.ShowEndGamePopup()`
  - wires controls from loaded prefab, spawns enemies, disposes slot models
- GameRoot startup wiring runs through DI `IStartable.Start` lifecycle (not Unity message `Start`) to avoid duplicate initialization/spawn.
- `LoadNextLevel()` increments saved level index and reloads the single `Game` scene.
- `ReloadCurrentLevel()` reloads the current `Game` scene without changing saved level index.
- Next level prefab is loaded during fresh `GameRoot` startup after scene reload.

---

## Editor Tooling

- `ProgressionEditorTools` provides menu command `Tools/ArmyCommander/Progression/Reset Level Progression`.
- Reset action writes saved progression key back to level index `0` through `PlayerPrefsStorage` and calls storage `Save()`.
- Runtime and editor progression access share one constant key (`ProgressionKeys.CurrentLevel`) to avoid key drift across systems.

---

## Repository Hygiene

- Root `.gitignore` follows Unity project conventions.
- Generated/runtime folders are not tracked by VCS: `Library`, `Temp`, `Obj`, `Build`, `Builds`, `Logs`, `MemoryCaptures`, `UserSettings`.
- IDE/workstation-specific files are ignored (`.vs`, `.vscode`, `.idea`, solution/project user files) to keep commits deterministic across machines.
