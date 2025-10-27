# Arkitect UI Overview

## Dock vs. Arkitect Panel
- **Class Dock prefab**
  - Shared hotbar used by every player class.
  - Lives near the bottom of the HUD and populates with ability/action icons for the active class.
  - Supports drag-and-drop so players can reorder abilities and add shortcuts from the abilities library.
  - Must remain class-agnostic; class-specific logic should plug in via data bindings or interfaces, not be hard-coded in the prefab.

- **Arkitect window**
  - Separate multitab panel dedicated to building systems.
  - Tabs surface materials, blueprints, and plots. Materials start with three base wood beam sizes and expand as players unlock more.
  - Blueprint entries list the material costs required to construct structure pieces; the player must hold those resources to place the blueprint in the world.
  - Plot tab provides small, medium, and large land grids that claim the underlying terrain for the player once placed.

## Integration Guidelines
1. **HUD binding** – The shared dock prefab is mounted under the HUD's class-dock anchor whenever a class becomes active. Individual classes should publish their ability sets so the dock can render the relevant icons.
2. **Ability management** – Ensure the dock exposes drag/drop hooks so users can customize slot order and populate additional slots from the class ability library.
3. **Builder interaction** – Builder-specific buffs and actions appear in the dock like any other class abilities. Builder construction tools live in the Arkitect window tabs instead of the dock.
4. **Future land settings** – When implementing land permissions, expose the plot ownership data so the Arkitect UI can surface settings once the feature arrives.

Keeping the dock prefab generic while treating the Arkitect window as the Builder's construction interface preserves consistent UX across classes and keeps future systems decoupled.
