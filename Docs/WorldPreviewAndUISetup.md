# World Preview & UI Transition Setup Guide

This guide documents how to configure the **main menu → world preview → world entry** flow, including UI panel visibility, HUD activation, and scene wiring. Follow the checklist to verify your Unity and server configuration.

---

## 1) Scene Overview (High-Level Flow)
1. **Main Menu scene** loads first and shows login/realm/character selection UI.
2. **World scene** is loaded additively when a character is highlighted or created, and a preview avatar appears at the last-known or fallback spawn location.
   - The world scene must be selected based on the **currently selected realm** so the preview reflects the correct realm background.
3. **Enter World** finalizes the transition, fades out the menu, and enables the world HUD + input.

Key runtime components:
- `MainMenuController` creates/locates the transition managers and kicks off preview/entry flows.
- `WorldPreviewTransitionManager` loads the world additively and spawns the preview avatar.
- `WorldUITransitionController` manages HUD fade + which panels show/hide on entry.

---

## 2) Unity Scene Setup (Step-by-Step)

### Main Menu Scene
1. **Ensure Main Menu UI canvases are named**:
   - `LoginCanvas`
   - `CreateAccountCanvas`
   - `RealmCanvas`
   - `CharacterCanvas`
   These names are used for menu fade logic during transition.

2. **MainMenuController must exist** on a GameObject in the scene.
   - In play mode it will auto-create:
     - `SceneTransitionController` (fallback for direct scene transition)
     - `WorldPreviewTransitionManager` (for world preview + entry flow)

3. **Main Camera**
   - Keep the default camera (`Main Camera` tag = `MainCamera`) for menu UI.
   - It will be disabled once the world preview is shown so the world camera renders behind the menu UI.

### World Scene
1. **World HUD Canvas**
   - Ensure the root HUD canvas is named `ArkitectCanvas` (or assign a different name via `WorldPreviewTransitionManager.worldHudName`).
   - This is the HUD canvas that fades in when entering the world.

2. **WorldUITransitionController**
   - Add to the HUD root or another world scene object.
   - Assign:
     - `Hud Group` (CanvasGroup on the HUD root)
     - `Panels Visible On Enter` → main HUD panels (profile, party, minimap, dock)
     - `Panels Hidden On Enter` → panels that should never auto-open (Arkitect, big map, etc.)
     - `Arkitect UI Manager` (optional, for ensuring builder panels remain closed)

3. **Arkitect UI Manager**
   - The `ArkitectUIManager` now defaults to `startHidden = true`.
   - This ensures the builder panels do not open on world load unless explicitly activated.

4. **Spawn Points**
   - Add one or more `PlayerSpawnPoint` objects.
   - Mark one as `UseAsFallback` for new characters or missing last-known locations.

5. **Preview Avatar Prefab (Optional)**
   - If you want a specific preview model, assign it to `WorldPreviewTransitionManager.previewAvatarPrefab`.
   - If not assigned, a capsule avatar with a controller will be spawned automatically.

---

## 3) Character Location & Preview Logic

### Last Known Location (Server → Client)
To place a preview avatar correctly, the server should include a location in `CharacterInfo.lastKnownLocation`.

**Expected format (string):**
```
x,y,z
```
Examples:
- `"12.5,1.0,-48.2"`
- `"0 2 0"`

If missing or invalid, the system uses `PlayerSpawnService.ResolveSpawnPosition(...)` to fall back to a `PlayerSpawnPoint`.

### New Characters
For new characters:
1. Server should return a starting location if possible.
2. If not available, they spawn at the fallback spawn point.

---

## 4) UI Transition Behavior

### Preview State
When a character is highlighted:
- World scene loads additively.
- HUD is hidden.
- Arkitect UI panels are kept closed.
- Preview avatar spawns at last known location.

### Enter World
When “Enter World” is clicked:
- Menu panels fade out.
- World HUD fades in.
- Arkitect UI remains closed unless explicitly opened by gameplay.
- Menu scene can be unloaded.

---

## 5) Camera & Input Expectations

- Menu camera is disabled once world preview is active.
- Player input is disabled during preview.
- Input is enabled after final “Enter World”.

---

## 6) Checklist (Validation Steps)

### Main Menu Scene
✅ `MainMenuController` present  
✅ Menu canvas objects named correctly  
✅ Enter World button triggers `SelectCharacterRoutine`  

### World Scene
✅ HUD root canvas is named correctly or referenced  
✅ HUD has a `CanvasGroup`  
✅ `WorldUITransitionController` is attached and configured  
✅ `ArkitectUIManager` exists and `startHidden = true`  
✅ `PlayerSpawnPoint` exists for fallback spawn  

### Data
✅ `CharacterInfo.lastKnownLocation` contains valid coordinates  
✅ Realm selection sets `SessionManager.SelectedRealmId`  
✅ Selected realm resolves to the correct world scene for preview/background loading  

---

## 7) Recommended Order of Setup
1. Confirm HUD and Arkitect canvas objects exist in the world scene.
2. Add/configure `WorldUITransitionController`.
3. Add at least one `PlayerSpawnPoint`.
4. Confirm Main Menu scene has `MainMenuController`.
5. Validate `CharacterInfo.lastKnownLocation` formatting on server responses.

---

## 8) Troubleshooting

**World not visible behind menu**
→ Ensure world scene is loading additively and menu camera is being disabled.

**HUD visible during preview**
→ Ensure `WorldUITransitionController` is assigned and `HideHudOnAwake` enabled.

**Arkitect UI opens on load**
→ Ensure `ArkitectUIManager.startHidden = true` and no scripts are calling `ShowPanel(...)` on load.

**Preview avatar spawns at (0,0,0)**
→ Check lastKnownLocation format and ensure a fallback spawn point is present.
