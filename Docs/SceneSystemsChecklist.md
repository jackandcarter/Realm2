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

## 8) Map & World State (Optional but typical)
**Purpose:** Provide spatial awareness and world progression hooks.
- Mini-map + world map overlay wiring.
- Region/zone state manager for map data.

## 9) Persistence & Save Systems
**Purpose:** Make progress durable across sessions.
- Plot save/load (per realm + character).
- Inventory/equipment persistence.
- Class unlock + quest state persistence.

---

## Immediate Next Task (Planned)
**Wire the Arkitect UI module into the Gameplay HUD class module list** so the Builder class mounts the Arkitect UI automatically in the HUD dock.
