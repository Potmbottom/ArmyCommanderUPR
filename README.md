# ArmyCommander

ArmyCommander is a 3D strategy-action game where the player builds barracks, gathers troops on a training field, gives attack orders, and clears enemy forces.
To complete a level, collect gold from combat and spend it at the army upgrade point (blacksmith).

## Core Gameplay

- 3 troop types with different stats: health, move speed, spawn speed, attack range, and attack speed/time.
- AI behavior is driven by aggressive range (units switch between home/engage behavior by range checks).
- 3 progression levels; each level unlocks an additional troop type for building.
- Projectiles are separate gameplay entities (not merged into troop objects), enabling flexible combat logic.
- Build barracks on available slots.
- Barracks produce troops into the training field.
- Give an order to send allied troops into combat.
- Enemies and allies drop different resources on death.
- Spend resources to build more barracks and upgrade army tier.
- Clear all enemies and complete progression levels.

## Platforms and Build

- Android build is available in `AndroidAPK`.
- Touchscreen control uses a virtual joystick.

## Technologies

- VContainer (dependency injection and unified tick loop)
- R3 (reactive streams for UI/events/state changes)
- DOTween (lightweight visual tweens, e.g. drop rotation)
- TextMeshPro (UI text)
- Shader Graph (custom fireball shader)
- MaterialPropertyBlock (per-renderer color overrides without material duplication)

## Architecture Pattern

The project uses **MVVM-inspired layered architecture** with strict boundaries:

- `Controls` (View adapters): MonoBehaviours bound to one PresentationModel interface.
- `PresentationModels` (ViewModel-like state): reactive + polled gameplay state, explicit state mutation methods.
- `Services` (Domain/application orchestration): per-system logic operating on PresentationModels only.
- `DataModels` (pure config/data): serializable structures with no behavior.

## Core Architecture Decisions

- Interface-first DI: services and runtime roots depend on interfaces, not concrete model classes.
- Single responsibility by system: each service owns one clear domain concern.
- Runtime loops are centralized via VContainer tickables (`Tick`, `FixedTick`, `LateTick`) instead of scattered update logic.
- Controls are data-driven and do not coordinate global gameplay directly.
- Ownership/disposal rules are explicit: creator/owner disposes runtime models.

## Optimization: Single-Responsibility Services

The game avoids heavy "god classes" by splitting logic into focused services:

- `AIService` -> target selection, state switching (`Idle/Move/Attack`).
- `BarrackService` -> troop production and order-phase gating.
- `ProjectileService` -> firing logic, target/collision resolution, projectile lifetime.
- `TransformService` -> **single entry point** for any gameplay transform movement (troops and projectiles).
- `ResourceService` -> drop spawn and resource payout on collect.
- `UIService` -> UI-facing decisions and model updates.

This improves maintainability, testability, and hot-path optimization control.

## Optimization: Single Tick Source

Instead of many `Update()` methods distributed across files, gameplay simulation runs through one orchestrated tick pipeline (via VContainer entry points):

- Frame `Tick`: input, AI, spawn/production, projectile simulation, transform resolution.
- `FixedTick`: physics-critical movement sync.
- `LateTick`: camera follow and final frame adjustments.

This keeps execution order deterministic and reduces update-order bugs.

## AI Flow (Agent Docs)

If an AI agent works in this repository, start with these files in this order:

1. `Design_docs/Systems.md` - source of truth for runtime contracts and architecture rules.
2. `Design_docs/Iterations.md` - concise history of accepted outcomes/decisions.
3. `Design_docs/Gamedesign.md` - gameplay goals, loops, and progression intent.
4. `Design_docs/Setup.md` - scene/setup/config wiring instructions.

## Rendering / Minor Technical Notes

- Most surfaces are rendered with a shared material and per-object color overrides through `MaterialPropertyBlock`.
- This avoids material instance spam and keeps GPU/CPU material management lighter.
- A custom fireball effect is authored with Shader Graph.

## How To Add a Feature (Small Example)

Example feature: "new aura buff that increases allied move speed near player."

1. Create/extend data:
   - Add fields to a DataModel/config (e.g. aura radius, speed multiplier).
2. Create state model:
   - Add a PresentationModel (or extend an existing one) with explicit state + events.
3. Create service:
   - Add `AuraService` that reads required models and applies buff logic in `Tick`.
4. Wire DI:
   - Register service + model interfaces in installer (`GameInstaller`).
5. Connect control/UI (if needed):
   - Bind visual/UI controls only to model interfaces; no global gameplay logic in controls.
6. Update docs:
   - Reflect contracts in `Design_docs/Systems.md` and record iteration outcome in `Design_docs/Iterations.md`.

