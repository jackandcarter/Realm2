# Arkitect UI Overview

## Dock vs. Arkitect Panel
- **Master Dock prefab**
  - Single dock instance that lives near the bottom of the HUD and remains mounted across class changes.
  - The center section is updated dynamically with class-specific modules (ability dock + other class UI) when the active class changes.
  - Supports drag-and-drop so players can reorder abilities and add shortcuts from the abilities library.
  - Must remain class-agnostic; class-specific logic should plug in via data bindings or interfaces, not be hard-coded in the prefab.

- **Arkitect window**
  - Separate multitab panel dedicated to building systems.
  - Tabs surface materials, blueprints, and plots. Materials start with three base wood beam sizes and expand as players unlock more.
  - Blueprint entries list the material costs required to construct structure pieces; the player must hold those resources to place the blueprint in the world.
  - Plot tab provides small, medium, and large land grids that claim the underlying terrain for the player once placed.

## Integration Guidelines
1. **HUD binding** – The master dock prefab mounts under the HUD's class-dock anchor and persists while the active class changes. The center section is the mount point for class UI modules, including the class ability dock.
2. **Class module binding** – Class-specific UI should be delivered via `IClassUiModule` implementations. `GameplayHudController` looks up the active class, unmounts the previous module, and mounts the new module prefab from the `classUiModules` list.
3. **Ability management** – The `ClassAbilityDockModule` is a class UI module that rebuilds ability slots when the active class changes, and it persists layout per class. Keep the ability dock in the master dock center section so it can rebind cleanly.
4. **Builder interaction** – Builder-specific buffs and actions appear in the dock like any other class abilities. Builder construction tools live in the Arkitect window tabs instead of the dock.
5. **Future land settings** – When implementing land permissions, expose the plot ownership data so the Arkitect UI can surface settings once the feature arrives.

## Usage Checklist
- **Do** keep the master dock prefab mounted once under `ClassDockAnchor` (the HUD generator already wires this up).
- **Do** add new class UI prefabs to the `GameplayHudController` `classUiModules` list and implement `IClassUiModule` to mount into the center section.
- **Do** keep class-specific logic inside class modules and data bindings so the dock remains class-agnostic.
- **Do** keep `ArkitectUIManager.startHidden` enabled so builder panels stay closed until explicitly opened by class logic or player input.
- **Do not** create or swap per-class dock prefabs; the master dock is persistent and only the class modules change.
- **Do not** mount class modules outside the master dock center section unless you are intentionally overriding HUD layout behavior.

Keeping the master dock prefab generic while treating the Arkitect window as the Builder's construction interface preserves consistent UX across classes and keeps future systems decoupled.
