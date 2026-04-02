# Iterations (Short)

Purpose: keep only durable outcomes from all iterations.
Details and temporary experiments are intentionally removed.

## Foundation

- Game direction defined: strategy loop with barracks, upgrades, resources, and level progression.
- Architecture fixed to 4 layers: Control, PresentationModel, Service, DataModel.
- Core systems established: AI, spawn/pool, barrack production, projectile, transform, UI, resources, upgrades.

## Core Architecture Decisions

- Interface-first DI for PresentationModels and services.
- `SetDependency(...)` is assignment-only; reactive subscriptions go to `Initialize()`.
- Reactive usage narrowed to subscription/event flows; polled state moved to plain fields/properties.
- Ownership/disposal responsibilities clarified for runtime-created models.

## Runtime/Scene Direction (Final)

- One shared `Game` scene is the runtime host.
- Level gameplay content is prefab-driven and loaded per level.
- `LevelRuntimeRoot` owns per-level refs (player/slots/spawns/controls).
- Final stable transition model: save next index, reload `Game`, rebind level prefab content.

## Simulation/Physics (Final)

- Hybrid loop adopted:
  - physics-critical movement in `FixedTick`
  - non-physics projectile visual sync in frame `Tick`
  - camera follow in `LateTick`
- Rigidbody constraints standardized: freeze Y position + freeze X/Z rotation.

## Combat and AI Outcomes

- Death ownership corrected to model damage path (`MakeDamage`), not AI-owned transition.
- Enemy targeting expanded to include the unique Player entity.
- Attack-state movement corrected (troops stop when attacking).
- AI stability improved (home hysteresis, aggressive return-home fallback).

## Projectile/Pooling Outcomes

- Projectiles use direction-based flight with deterministic cleanup.
- Orphan/frozen projectile cases removed (hit/miss/lifetime termination).
- Pool reuse artifacts reduced via spawn/bind ordering and stash-on-release.
- Released troop stash rule: set `x = 1000`, `z = 1000`, preserve current `y`.

## UI and Progression Outcomes

- Build popup is validity-gated (enough silver + empty slot).
- Next-level/end-game actions are code-bound (not inspector-fragile).
- End-game popup + same-level reload integrated.
- Barrack troop selection is level-driven via scalable `TroopType` mapping.

## Startup/DI Stability Outcomes

- Boot -> Menu startup flow stabilized.
- Root scope persistence and installer registrations hardened.
- Menu wiring moved to reliable code-driven button binding.

## Current Rule

- `Design_docs/Systems.md` is the source of truth for live contracts.
- This file stays concise and summary-only.

## Latest Iteration

- Added root `.gitignore` for Unity repository hygiene.
- Ignored generated/runtime folders (`Library`, `Temp`, `Obj`, `Build*`, `Logs`, `MemoryCaptures`, `UserSettings`) plus IDE-local files (`.vs`, `.vscode`, `.idea`, project/user artifacts) to reduce repository noise and prevent cache bloat from being tracked.
- Updated `Design_docs/Systems.md` with a new `Repository Hygiene` section documenting the `.gitignore` contract.

- Fixed troop facing behavior in combat: when a troop is in `TroopState.Attack`, `TroopControl.FixedTick` now rotates by target direction (`TargetPosition - transform.position`) instead of zero attack velocity.
- Kept movement-facing behavior unchanged for non-attack states (`Idle`/`Move`) and added a safe fallback to velocity when target direction is near zero.
- Updated `Design_docs/Systems.md` `TroopControl` contract to document attack-state target-facing rotation source.

- Added `FpsDisplayControl` in `Assets/Scripts/Controls/FpsDisplayControl.cs` for runtime FPS debug display (`OnGUI`), with configurable `_textColor`, `_fontSize`, and `_margin`.
- Optimized FPS label construction to use reusable `StringBuilder` (no per-frame string interpolation allocation).
- Synced `Design_docs/Systems.md` with the `FpsDisplayControl` contract and StringBuilder note.

- Cleaned up redundant menu presentation model layer:
  - removed unused `IMenuPModel` / `MenuPModel` files and DI registration from `MenuInstaller`;
  - simplified `MenuRoot` by removing write-only progression cache assignment.
- Updated documentation to match runtime:
  - `Design_docs/Systems.md` no longer lists `MenuPModel` and reflects direct `MenuRoot` startup flow;
  - `Design_docs/Setup.md` Menu scene flow now documents direct Start-button binding and game-scene loading.

- Added editor utility `ProgressionEditorTools` with menu command `Tools/ArmyCommander/Progression/Reset Level Progression` to reset saved level progression to `0` with a confirmation dialog.
- Introduced shared key constant `ProgressionKeys.CurrentLevel` and switched runtime reads/writes in `MenuRoot`, `GameRoot`, and `BarrackService` to use it, removing duplicated string literals.
- Updated systems documentation with the new editor-tool contract and shared progression key policy.
- Rebuilt `UIControl` from standalone `MonoBehaviour` into `BaseControl<IUIModel>` so UI view logic now binds to a single PresentationModel contract.
- Expanded `IUIModel`/`UIModel` with reactive UI state (`Gold`, `Silver`, `EnemyProgress`) and intent events (`OnNextLevelRequested`, `OnReloadRequested`) plus update/request methods.
- Migrated runtime wiring:
  - `UIService` now mirrors resource values into `UIModel` (`SetGold`, `SetSilver`).
  - `GameRoot` now binds `UIControl` with only `IUIModel`, listens to UI intent events for level navigation/reload, and pushes enemy progress through model (`InitializeEnemyProgress`, `UpdateEnemyProgress`) instead of UI polling field state directly.
- Updated `Design_docs/Systems.md` contracts for `UIModel`, `UIControl`, `UIService`, and `GameRoot` to match the new BaseControl + PresentationModel flow.
- Refactored end-game popup trigger to reactive flow:
  - Added `IPlayerPModel.OnDead` and implemented it in `PlayerPModel` (fires once on alive -> dead transition inside `MakeDamage`).
  - Removed `_isEndGamePopupShown` state-check branch from `GameRoot.Tick`.
  - `GameRoot` now subscribes to `IPlayerPModel.OnDead` during startup and calls `UIModel.ShowEndGamePopup()` on event.
- Result: `GameRoot.Tick` now stays focused on per-frame update work (enemy progress), while death popup logic is event-driven on data change.
- Refactored enemy-progress flow to reactive field observation:
  - `UIService` now depends on `IFieldPModel` and subscribes to `OnTroopAdded` / `OnTroopRemoved` (enemy-only) to push progress changes into `UIModel`.
  - Added baseline tracking in `UIService` (`initialEnemyCount` + `currentEnemyCount`) and re-initialize progress denominator when baseline grows.
  - Removed enemy-progress polling from `GameRoot` (`_uiModel.UpdateEnemyProgress(...)` in Tick).
  - `GameRoot` no longer implements `ITickable`; it keeps startup + fixed/late responsibilities only.
- Updated `Design_docs/Systems.md` contracts and tick-order section to reflect reactive enemy progress and removal of `GameRoot` from ITickable execution order.
- Added popup-driven HUD visibility behavior in `UIControl`:
  - gold/silver counters and enemy progress slider are hidden whenever any gameplay popup is active (`build`, `next-level`, `end-game`);
  - HUD is restored automatically when all popups are closed.
- Updated `UIControl` system contract in `Design_docs/Systems.md` with the new popup/HUD visibility rule.
- Added "all enemies dead" UI signal in `UIControl`:
  - introduced serialized `_allEnemiesDeadIndicator` (image object near enemy slider);
  - indicator is enabled when reactive enemy progress reaches completion and disabled otherwise;
  - indicator visibility also follows popup HUD visibility rules (hidden while any popup is active).
- Fixed barrack build unlock progression mismatch between levels:
  - added `SetLevel(ArmyLevel)` to `IArmyUpgradePModel` / `ArmyUpgradePModel` for initialization without firing `OnUpgraded`;
  - `GameRoot` now initializes army level from saved progression index (`CurrentLevel + 1`, clamped to `Level1..Level3`) at startup;
  - level 2 now correctly exposes two barrack build types (`Soldier`, `Veteran`) when build popup opens.
- Updated `Design_docs/Systems.md` contracts for `ArmyUpgradePModel`, `UIService` build availability context, and `GameRoot` startup progression-to-army-level mapping.
- Added tier-specific army upgrade price support in level data:
  - extended `LevelData` with `UpgradeToLevel2CostGold` and `UpgradeToLevel3CostGold`;
  - `ArmyUpgradeService` now resolves upgrade cost by current tier (`Level1->2` and `Level2->3`) from current `LevelConfig` level data;
  - kept backward compatibility: when tier-specific value is not set (`<= 0`), service falls back to legacy `UpgradeCostGold`.
- Updated `Design_docs/Systems.md` to reflect the new `LevelData` fields and tier-based upgrade cost contract in `ArmyUpgradeService`.
- Reverted the tier-specific **army** gold upgrade pricing path and restored `ArmyUpgradeService` to use `LevelData.UpgradeCostGold`.
- Introduced tier-specific **barrack build** silver prices in level data:
  - added `BarrackSoldierCostSilver`, `BarrackVeteranCostSilver`, and `BarrackMasterCostSilver` to `LevelData`;
  - `UIService` now resolves silver spend amount by selected `TroopType` from current level data;
  - build popup opens only when at least one available troop type is affordable by current silver.
- Added backward compatibility for barrack prices: if configured tier price is not set (`<= 0`), default build cost remains `1` silver.
- Updated `Design_docs/Systems.md` contracts for `LevelData`, `UIService`, and `ArmyUpgradeService` to reflect the corrected pricing ownership.
- Added `MeshMaterialColorControl` MonoBehaviour utility to set color on an attached object renderer material.
- Implemented serialized `_color` workflow with `Awake` and `OnValidate` application so runtime and editor preview both stay in sync.
- Documented the new visual utility contract in `Design_docs/Systems.md` under Controls.
- Fixed prefab edit-time safety in `MeshMaterialColorControl`: `OnValidate` now uses `Renderer.sharedMaterial` to avoid Unity error on prefab objects.
- Kept runtime per-instance behavior unchanged: `Awake` still applies color through `Renderer.material`.
- Refactored `MeshMaterialColorControl` to use `MaterialPropertyBlock` for per-renderer color override, avoiding `Renderer.material` instancing and shared material mutation.
- Kept shader compatibility by resolving and setting `_BaseColor` (URP) with fallback to `_Color`.
- Investigated blocked progression at level 2 ending and found an inverted max-tier check in `ArmyUpgradeService.OnUpgraded`.
- Fixed progression gate by changing popup trigger condition to `newLevel == ArmyLevel.Level3`, so successful tier-2->tier-3 upgrade now opens the next-level popup.
- Updated `Design_docs/Systems.md` `ArmyUpgradeService` contract to explicitly state max-tier completion (`Level3`) is the next-level popup trigger.
- Investigated regression where level 1 upgrade spent gold but did not open completion popup.
- Aligned `ArmyUpgradeService` with game design progression rule (one tier-upgrade completes current level): `OnUpgraded` now always triggers `UIModel.ShowNextLevelPopup()`.
- Updated `Design_docs/Systems.md` `ArmyUpgradeService` contract from max-tier-only completion to any successful tier-upgrade completion.
- Updated barrack build popup lock logic to include resource affordability per troop type.
- Extended `IUIModel`/`UIModel` build-popup payload to carry both `availableTypes` (level-unlocked) and `affordableTypes` (silver-gated) for UI rendering.
- Changed `UIService` slot popup flow to always show popup for empty slots with unlocked types and pass affordable subset instead of hiding popup when silver is insufficient.
- Updated `UIControl` build-button state so each type is locked/interactable only when both level-unlocked and currently affordable; unaffordable types now appear locked.
- Updated `Design_docs/Systems.md` contracts for `UIModel`, `UIService`, and `UIControl` to reflect affordability-based lock state in the barrack popup.
- Optimized `AIService` aggressive targeting math to remove sqrt calls in Tick hot path:
  - nearest-target selection now uses squared XZ distance comparisons;
  - attack/aggressive range checks now compare against squared ranges (`range * range`) with unchanged gameplay behavior.
- Updated `Design_docs/Systems.md` `AIService` contract with the squared-distance targeting rule.
- Optimized `ProjectileService` collision/targeting hot path without adding new gameplay systems:
  - added per-team troop caches maintained through existing troop add/remove events;
  - projectile collision now scans only opposite-team cache instead of all troops;
  - switched projectile collision, player collision, and target-nearest checks to squared-distance math (no sqrt);
  - added cheap axis-aligned prefilter before narrow-phase distance check in projectile/player collision.
- Added class-level TODO in `ProjectileService` documenting future upgrade path to uniform-grid spatial partitioning.
- Updated `Design_docs/Systems.md` `ProjectileService` contract with team-cache broad-phase and squared-distance collision/targeting details.
- Removed avoidable per-tick GC allocations in service hot loops:
  - `BarrackService.TickDeadTroops` no longer creates `new List<>(_deadTroopTimers.Keys)` each tick; it now reuses a class-level key cache list that is cleared/refilled per frame.
  - `ProjectileService.TickAttackTimers` no longer creates `new List<>(_attackTimers.Keys)` each tick; it now reuses a class-level key cache list and processes elapsed timers via `TryGetValue`.
- Updated `Design_docs/Systems.md` service contracts (`BarrackService`, `ProjectileService`) to record the no-allocation key-iteration rule for these runtime loops.

- Implemented save abstraction for progression persistence:
  - added `ISaveStorage` interface (`HasKey`, `GetInt`, `SetInt`, `DeleteKey`, `Save`) to decouple game systems from concrete storage backend;
  - added safe `PlayerPrefsStorage` wrapper with key validation and guarded read/write/save operations (warnings + fallback behavior instead of throwing).
- Wired runtime progression consumers to interface-first storage access:
  - `GameRoot`, `BarrackService`, and `UIService` now read/write progression key (`ProgressionKeys.CurrentLevel`) through injected `ISaveStorage` instead of direct `PlayerPrefs`.
- Registered storage in root DI scope:
  - `BootInstaller` now binds `ISaveStorage` to `PlayerPrefsStorage` as a singleton so all child scopes share the same persistence contract.
- Updated editor progression reset tool to use the same wrapper contract:
  - `ProgressionEditorTools` now uses `PlayerPrefsStorage` for read/reset/save flow, keeping editor/runtime key access semantics aligned.
- Updated `Design_docs/Systems.md` with new save-storage system contract, DI registration note, and editor tooling/runtime usage updates.
- Fixed training-field order gating when barracks are empty:
  - extended `ITrainingFieldPModel` / `TrainingFieldPModel` with `CanGiveOrder` and `SetOrderAvailable(bool)`;
  - `TrainingFieldPModel.GiveAttackOrder()` now ignores trigger requests when order is already active or when order availability is false.
- Updated `BarrackService` slot-type change flow to recompute order availability after every barrack assignment/removal:
  - when there are no active productions (all barracks effectively empty), `TrainingFieldPModel.SetOrderAvailable(false)` is applied;
  - when at least one production is active, order availability is enabled again.
- Updated `Design_docs/Systems.md` contracts for `TrainingFieldPModel`, `TrainingFieldControl`, and `BarrackService` to reflect the new "no barracks -> no order" rule.

- Applied platform-gated virtual joystick visibility:
  - `VirtualJoystick.Awake()` now disables joystick GameObject only on non-Android targets (`!UNITY_ANDROID || UNITY_EDITOR`);
  - removed runtime self-disable toggles from pointer handlers so Android joystick remains active and reusable across multiple drags.
- Updated `Design_docs/Systems.md` `VirtualJoystick` contract with the non-Android platform-gate rule.

- Fixed Android joystick disappearing after first interaction:
  - removed root `RectTransform` repositioning from `VirtualJoystick.OnPointerDown()` and kept joystick in a fixed scene position;
  - `OnPointerDown()` now only resets handle + direction state before drag.
- Updated `Design_docs/Systems.md` `VirtualJoystick` contract to state fixed-position behavior.

- Added `AndroidFrameRateControl` in `Assets/Scripts/Controls/AndroidFrameRateControl.cs` to set Android runtime frame cap.
- Implemented Android-only FPS setup in `Awake`: disable vSync and apply serialized `Application.targetFrameRate` (default `60`).
- Updated `Design_docs/Systems.md` with the new `AndroidFrameRateControl` contract and platform-gated behavior.

- Implemented pooled resource-drop flow instead of immediate resource gain on troop removal:
  - added `ResourceType` enum and new drop model contract (`IResourceDropPModel` / `ResourceDropPModel`) with one-shot `Collect()` and `OnCollected` event;
  - extended `IFieldPModel` / `FieldPModel` with drop storage/events (`ResourceDrops`, `OnResourceDropAdded`, `OnResourceDropRemoved`) and factory/remove methods (`CreateResourceDrop`, `RemoveResourceDrop`).
- Added drop data/config pipeline for prefab-driven setup:
  - added `ResourceDropDataModel` (`Index`, `ResourceType`, `Prefab`, `Amount`) and `ResourceDropConfig` (ScriptableObject with index/type lookup).
  - updated `BootInstaller` to bind `ResourceDropConfig` in root scope.
- Added pooled drop visuals/collection path in runtime:
  - added `ResourceDropControl` (binds position from model; trigger enter with `PlayerControl` calls `Collect()`).
  - extended `SpawnService` with `ObjectPool<ResourceDropControl>` and subscriptions to field drop add/remove events.
- Reworked `ResourceService` reward flow:
  - `OnTroopRemoved` now spawns a drop by team (`Enemy -> Gold`, `Allied -> Silver`) via `ResourceDropConfig` + `FieldPModel.CreateResourceDrop(...)`;
  - on collected drop, applies `AddGold/AddSilver` by drop amount and removes drop from `FieldPModel`.
- Updated `Design_docs/Systems.md` contracts before code changes to reflect new `ResourceDrop` model/control/config, `FieldPModel` API/events, `SpawnService` pooling scope, `ResourceService` collect flow, lifecycle ownership, and Boot config list update.
- Updated `ResourceDropControl` pickup filter from component lookup to tag compare:
  - `OnTriggerEnter` now collects only when `other.CompareTag("Player")` is true;
  - synced `Design_docs/Systems.md` `ResourceDropControl` contract to tag-based pickup rule.
- Added simple slow rotation visual script for resource drops:
  - created `Assets/Scripts/Controls/ResourceDropRotateControl.cs` with serialized `_loopDuration` and `_rotationDegreesPerLoop`;
  - uses DOTween local-y loop (`Ease.Linear`) when `DOTWEEN_ENABLED` define is present;
  - starts tween in `OnEnable`, kills on `OnDisable`, and restores initial local rotation for pooled reuse consistency.
- Updated `Design_docs/Systems.md` with new `ResourceDropRotateControl` contract before code update.

- Added troop health reactive exposure in presentation model contract:
  - extended `ITroopPModel` with `CurrentHealth` and `OnHealthChanged` (`Observable<float>`);
  - refactored `TroopPModel` health storage from private float to `ReactiveProperty<float> _health`.
- Updated damage/death flow to publish health updates through reactive property:
  - `MakeDamage` now writes clamped health into `_health.Value` and keeps existing dead-state transition behavior when health reaches zero.
  - `Dispose` now disposes both `_health` and `_state`.
- Updated `Design_docs/Systems.md` `TroopPModel` contract before code changes to document `CurrentHealth` + `OnHealthChanged` exposure and reactive health ownership.

- Fixed troop death-time `MissingReferenceException` from damage VFX path in `TroopControl`:
  - replaced null-conditional VFX access with Unity-safe destroyed-object guard (`if (_vfx == null) return`) before using `gameObject`/`Play`;
  - added `OnHealthChanged` subscription to `Disposables` so pooled release/destroy always unsubscribes callback and prevents stale calls against destroyed visual references.
- Updated `Design_docs/Systems.md` `TroopControl` contract with damage-VFX lifetime/null-safety rules.

- Implemented projectile movement ownership split between services with no direct service coupling:
  - moved projectile position-step logic (`MoveSpeed * deltaTime` along projectile `Direction`) from `ProjectileService` into private `TransformService` movement flow;
  - kept `ProjectileService` focused on projectile gameplay logic only: firing cadence, target/collision checks, damage application, and lifetime-based removal.
- Preserved the "no public methods on services" rule:
  - no new public API was added to `TransformService`; projectile movement runs only inside its internal tick path.
- Updated runtime tick registration order in `GameInstaller` and documented it in `Design_docs/Systems.md`:
  - `TransformService` now ticks before `ProjectileService` so projectile movement happens before collision/lifetime resolution in frame update flow.

