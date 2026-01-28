# MMO Scene Systems Checklist

This checklist summarizes the minimum in-scene (or persistent singleton) systems required to support Realm's MMO gameplay loop. It is intended as a shared reference when assembling playable scenes and bootstrapping core managers.

## 1) Session & Identity
**Purpose:** Resolve the active character/realm and provide the identity anchor for every other system.
- Session manager that exposes `SelectedCharacterId` and `SelectedRealmId`.
- Event hooks for character swaps and session resets.

## 2) Class State & Unlocks
**Purpose:** Drive class gating, ability availability, and class-specific UI.
- `PlayerClassStateManager` initialized and listening for class unlocks and character changes.
- Class unlock persistence + state sync with UI and gameplay.

## 3) Ability & Combat Runtime
**Purpose:** Activate abilities, handle cooldowns, and coordinate with the server-side combat pipeline.
- Ability activation controller (client-side) wired to gameplay input.
- Ability dock runtime bound to unlocked abilities.
- Network bridge for ability execution + server authority.

## 4) Inventory & Equipment State
**Purpose:** Provide item ownership, equipment stats, and crafting inputs.
- Inventory manager with item stacks and persistence.
- Equipment state manager feeding weapon/armor slots.
- Item definitions/catalogs for UI and combat stats.

## 5) World Interaction & Building
**Purpose:** Support Arkitect building loops and world interactions.
- `RuntimePlotManager` for plot ownership and terrain editing.
- `BlueprintSpawner` + `ArkitectRegistry` for blueprint placement.
- Build zone validation service and plot ownership checks.

## 6) Permissions & Land Settings
**Purpose:** Prevent griefing and enforce ownership-based interactions.
- Plot ownership data model and permission resolution.
- Land settings service exposed to Arkitect UI.
- Shared access rules for collaborative builds.

## 7) HUD & Class UI Modules
**Purpose:** Provide the main player interface and class-specific tools.
- Gameplay HUD canvas and class dock anchor mounted.
- Master dock prefab bound to the HUD controller.
- Class UI modules list populated (e.g., Arkitect UI for builder).
- World preview transition flow configured to hide/show HUD and class panels when entering the world.

## 8) Map & World State (Optional but typical)
**Purpose:** Provide spatial awareness and world progression hooks.
- Mini-map + world map overlay wiring.
- Region/zone state manager for map data.

## 9) Persistence & Save Systems
**Purpose:** Make progress durable across sessions.
- Plot save/load (per realm + character).
- Build-state snapshot persistence for plots + constructions.
- Inventory/equipment persistence.
- Class unlock + quest state persistence.

---

## Immediate Next Task (Planned)
**Wire the Arkitect UI module into the Gameplay HUD class module list** so the Builder class mounts the Arkitect UI automatically in the HUD dock.

---

## World Preview & UI Transition Checklist
**Goal:** Ensure the live world preview behind the main menu is configured correctly.
- `WorldPreviewTransitionManager` exists in the main menu runtime (auto-created by `MainMenuController` if missing).
- World scene HUD canvas (`ArkitectCanvas`) has a `CanvasGroup`.
- `WorldUITransitionController` attached and configured to hide HUD on preview and fade in on entry.
- `ArkitectUIManager.startHidden` is enabled so builder panels do not auto-open.
- At least one `PlayerSpawnPoint` exists for fallback spawns.

---

## Detailed Task Breakdown (Ordered Recommendations)

### 1) HUD Class Module Wiring (Arkitect UI)
**Goal:** Ensure the Builder class automatically mounts the Arkitect UI module when the HUD is initialized.
- **Identify the HUD controller + class module list owner**
  - Locate the Gameplay HUD root/controller that owns the class module list (the dock or module host that mounts `IClassUiModule` implementations).
  - Document the expected structure: where modules are registered, when the list is evaluated, and whether it expects prefabs or scene instances.
- **Define the Arkitect module entry**
  - Confirm `ArkitectUIManager` implements `IClassUiModule` and advertises the Builder class id.
  - Decide whether the module is created as a prefab reference or a scene object.
  - Ensure the module exposes mount/unmount behavior that can be triggered by class changes.
- **Wire module registration**
  - Add the Arkitect UI module to the HUD’s class module list with the correct class id mapping.
  - Ensure it is mounted only when the active class matches Builder and unmounted otherwise.
  - Add a fallback to disable the module when the class is unavailable or permissions are revoked.
- **Acceptance criteria**
  - Switching to the Builder class mounts the Arkitect UI automatically in the HUD dock.
  - Switching away cleanly unmounts the UI and releases any UI state.
  - No duplicate mounts or orphaned UI objects after class swaps.

### 2) Combat Pipeline Foundations (Targeting + Effects)
**Goal:** Build a reliable client-side combat pipeline that can drive melee, ranged, and magic abilities while remaining compatible with server authority.
- **Target selection + validation layer**
  - Implement a target resolver that reads `AbilityTargetingConfig` (self/ally/enemy/area/global).
  - Enforce `MaxTargets`, `RequiresPrimaryTarget`, and `CanAffectCaster`.
  - Build area targeting logic for circle/cone/line, including range checks.
- **Hitbox evaluation**
  - Implement hit detection using `AbilityHitboxConfig` and shapes (sphere/capsule/box/cone).
  - Tie hitbox size/offset to the caster’s position and facing (when `UseCasterFacing` is enabled).
  - Provide a deterministic, testable query path (e.g., physics overlap + filtering).
- **Effect application**
  - Create an effect resolver that translates `AbilityEffect` entries into gameplay actions.
  - Support damage, heal, buff, debuff, state change, and custom effect types.
  - Apply scaling rules based on stat ratios (see task 3).
- **Server-authoritative bridge**
  - Define a client request payload (ability id, target ids, location, timing).
  - Integrate a handshake that waits for server confirmation before finalizing results.
  - Provide client prediction hooks for UI responsiveness (cooldowns, cast bars).
- **Acceptance criteria**
  - Abilities can resolve valid targets across melee, ranged, and magic modes.
  - Effects are applied to resolved targets and are stat-scaled.
  - Client-side results reconcile correctly with server authority.

### 3) Stat Ratio System Runtime
**Goal:** Provide a consistent stat scaling system that powers combat effects and supports MMO-style attribute ratios.
- **Stat profile evaluation**
  - Load the active class’s `StatProfileDefinition`.
  - Evaluate curves to produce base stats at a given level.
- **Modifier pipeline**
  - Apply equipment, buffs, debuffs, and temporary modifiers.
  - Support additive and multiplicative modifiers with defined ordering.
- **Derived stat mapping**
  - Map primary stats to derived combat values (e.g., Strength → melee power, Intellect → spell power).
  - Define standardized conversion ratios so designers can tune balance.
- **Runtime API**
  - Provide a stat calculator service that combat and UI can query.
  - Cache computed values and invalidate on changes (gear swap, buffs, level up).
- **Acceptance criteria**
  - Stat outputs are stable and predictable with clear modifier ordering.
  - Combat effects consume stat-scaled values.
  - UI panels can display computed stats and derived values.

### 4) Editor Workflow Integration (Registry + Data Tools)
**Goal:** Ensure designers can author and sync data assets without manual wiring.
- **Registry synchronization**
  - Define when and how the StatRegistry is refreshed (manual button or automated on asset changes).
  - Ensure new stat/class/ability assets appear in runtime lists after sync.
- **Tooling documentation**
  - Document steps for designers: create assets → sync registry → verify runtime references.
  - Add a quick checklist for validating new abilities and class unlocks.
- **Validation diagnostics**
  - Provide editor warnings for missing GUIDs or broken references (weapon/ability ids).
  - Surface tooltips or help boxes where data is incomplete.
- **Acceptance criteria**
  - Designers can add a new class or ability without touching runtime code.
  - Registry updates reflect new assets across UI and combat.
  - Editor tools surface data issues before play mode.

### 5) UI Tooltips + Presentation Hooks
**Goal:** Provide rich UI metadata for abilities, stats, and interactions (hover tooltips, panels).
- **Tooltip data model**
  - Define a structured tooltip payload that includes name, description, targeting, cost, cooldown, effects, and stat scaling.
  - Ensure it is generated from `AbilityDefinition` and stat calculator outputs.
- **UI integration**
  - Add hover triggers on HUD panels and ability slots.
  - Map tooltip data to a reusable tooltip panel component.
- **Visual feedback**
  - Include cast time, cooldown timers, and target restrictions in the tooltip display.
  - Display warnings if requirements are not met (e.g., range, target type, resource).
- **Acceptance criteria**
  - Hovering a UI element consistently shows a fully populated tooltip.
  - Tooltip data matches the runtime ability configuration.
  - Errors are surfaced clearly when requirements are unmet.
