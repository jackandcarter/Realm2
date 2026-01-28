# System Implementation Plan

## Current gameplay foundations
- **Ability graph execution** already supports selecting targets, applying damage/heal, and propagating states based on generated stat/ability registries, giving us a reusable combat core to extend for class kits and fusion outputs. 【F:Server/src/gameplay/combat/abilityExecutor.ts†L1-L160】【F:Server/src/gameplay/combat/generated/abilityRegistry.ts†L3-L66】
- **Class unlock quest pipeline** persists quest attempts, emits notifications/journal entries, and guards against duplicate unlocks, providing the pattern for new story- and class-gated content. 【F:Server/src/gameplay/quests/classUnlockQuest.ts†L1-L200】

## New scaffolding added in this change
- **Magic Fusion + party logic**: shared typings and helpers to evaluate fusion timing/element matches, derive party category/level sync, and scale bosses by party composition. 【F:Server/src/gameplay/design/systemFoundations.ts†L1-L165】
- **Class/profession/item/world taxonomy**: normalized definitions for core classes, professions, equipment slots/archetypes, key locations, settlement tiers, and story arcs to anchor future data models and content ingestion. 【F:Server/src/gameplay/design/systemFoundations.ts†L167-L394】

## Task backlog to flesh out the documented systems
### Combat & Party Systems
- Wire **magic fusion** into the combat pipeline: convert fusion rules into ability graph outcomes, track overlapping AoE volumes/timers in the client, and emit fusion ability triggers to the executor when `evaluateMagicFusion` reports success.
- Add a **server-side fusion state machine** that records overlapping casts, validates timing windows, and persists fusion outcomes to combat logs for auditing.
- Implement **party category + level sync** usage in matchmaking and encounter instancing so boss scaling uses `resolvePartyCategory`/`scaleBossLevel` before spawning.
- Extend combat events with **fusion-specific VFX/state payloads** (e.g., fused states, failure penalties) to support UI feedback described in the context docs.
- Implement **combo system runtime (client)**: add `ComboSystem` that consumes `WeaponCombatDefinition.ComboGraph`, tracks current node, and returns `ComboStepDefinition` for L/M/H input.
- Implement **combat state machine (client)**: create `CombatStateMachine` with Idle/BasicAttack/Recovery states, action gating, and combo input buffering tied to combo continuation windows.
- Implement **animation combat driver hooks (client)**: emit normalized time events for hit windows, combo continuation windows, cancel-to-ability windows, and animation completion.
- Wire **dock input → combat pipeline (client)**: route `WeaponDockController` L/M/H/Special clicks through `CombatStateMachine` + `ComboSystem` and drive `AnimationCombatDriver`.

### Client Combat Runtime Task Stubs (ordered)
- [ ] **Task 1: ComboSystem runtime skeleton**
  - [ ] Add `ComboSystem` class (new file under `Assets/Scripts/Client/Combat/Runtime/`).
  - [ ] Define `ComboSystemState` (current node id, last input time, active step timings).
  - [ ] Build a graph lookup cache from `WeaponCombatDefinition.ComboGraph` (start nodes, node map, edge map).
  - [ ] Implement `TryAdvanceCombo(ComboInputType input, float now, out ComboStepDefinition step)` and `ResetCombo()`.
  - [ ] Add simple validation guards for missing start nodes / invalid edges.
- [ ] **Task 2: CombatStateMachine skeleton**
  - [ ] Add `CombatStateMachine` class (new file under `Assets/Scripts/Client/Combat/Runtime/`).
  - [ ] Define states `Idle`, `BasicAttack`, `Recovery`.
  - [ ] Define `ActionCategory`, `ActionRequirements`, and `BufferedAction` structs/classes.
  - [ ] Implement `CanStartAction`, `TryBufferAction`, `StartAction`, `EndAction`.
  - [ ] Add logic to accept buffered combo input only during continue windows.
- [ ] **Task 3: AnimationCombatDriver hooks**
  - [ ] Add `AnimationCombatDriver` component (new file under `Assets/Scripts/Client/Combat/Runtime/`).
  - [ ] Expose events: `OnHitEvent`, `OnContinueWindowOpened/Closed`, `OnCancelableIntoAbilityOpened/Closed`, `OnActionAnimationComplete`.
  - [ ] Implement normalized time sampling from the active animation state.
  - [ ] Map `ComboStepDefinition` timing fields to event thresholds.
- [ ] **Task 4: Pipeline wiring to existing UI**
  - [ ] Update `WeaponAttackController` to request state gating from `CombatStateMachine`.
  - [ ] Update `WeaponDockController` click handlers to call into the new `CombatStateMachine` + `ComboSystem` flow.
  - [ ] Replace or wrap `WeaponComboTracker` usage so specials rely on the new combo runtime.
  - [ ] Hook `AnimationCombatDriver` events to open/close combo buffers and end recovery.

### Classes, Professions, and Items
- Flesh out **class ability libraries** per `coreClassDefinitions`, mapping each signature and leveling perk into ability graphs/stat scalars that plug into the executor.
- Hook **class unlock quests** to the new class definitions (Rogue/Ranger/Builder/etc.) and gate progression using the existing quest journal pipeline.
- Build **profession crafting chains** and loot tables that respect `professionDefinitions` inputs/outputs and feed settlement construction/Arkitect materials.
- Expand **equipment catalogs** for weapons/armor/consumables/key items, linking subtype tags to class proficiencies and combat stats.

### World-Building Features
- Use **settlement tier definitions** to drive Arkitect UI unlocks, commission board availability, and facility build timers per location.
- Model **plot/blueprint ownership and permissions** so Builder players can place/snap grids and expose land settings through the Arkitect window.
- Implement **Builder class runtime scope** (abilities, plots, blueprints, construction queue, and client/server reconciliation) as detailed in `BuilderClassSpec.md`.
- Expose **kingdom progression** hooks (markets, embassies, governance rules) keyed off `settlementTiers` and `keyLocations`, including level/party gates for region entry.

### Narrative & Content
- Instantiate **story arcs and side-quests** from `storyArcs`, linking to NPCs, locations, and class unlocks while capturing branching choices/persistence.
- Create **race/class affinity modifiers** and racial ability hooks that integrate with the stat registry and quest unlock paths.
- Author **zone events** (e.g., time anomalies, forest spirit encounters) that reference location IDs and feed rewards into class/profession/equipment unlocks.

### Tooling & Data Flow
- Build **editor/ingestion scripts** to convert design spreadsheets into JSON/TS definitions that populate the registries and new taxonomy structures.
- Add **validation tests** to ensure fusion rules, class definitions, and equipment/profession data remain consistent (unique IDs, valid references, balanced scaling).
- Provide **documentation pages** for designers/developers explaining how to extend the new foundations (fusion rules, class kits, settlement tiers, Arkitect UI hooks).
