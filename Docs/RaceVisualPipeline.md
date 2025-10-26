# Race Visual Content Pipeline

This document outlines the workflow for adding a new playable race (or updating an existing one) so that designers, engineers, and artists can collaborate without stepping on each other's toes.

## 1. Provide the Art Assets

1. **Prefab the preview model**
   - Create a self-contained prefab under `Assets/Art/Races/<RaceName>/Prefabs/` (or another agreed upon art directory).
   - Zero the root transform and make sure its scale is `(1,1,1)` so the character creation preview positions the model correctly.
   - Include any lights, VFX, or rigging that should be visible in the preview. Avoid gameplay scripts—keep the prefab presentation-only.
2. **Author material variants (optional)**
   - Create materials for skin tones, armor finishes, etc. Store them beside the prefab.
   - Organise multi-material meshes so material slots line up with how they should be swapped (e.g. slot 0 = body, slot 1 = clothing).

## 2. Update the Race Catalog (gameplay data)

Designers/engineers define the gameplay-facing data for a race in `Assets/Scripts/Client/CharacterCreation/RaceCatalog.cs`:

1. Add or update a `RaceDefinition` entry with lore, customization ranges, and identifiers.
2. Use a unique `Id`—the same string is used by the visual config to link the prefab.

## 3. Wire visuals with `RaceVisualConfig`

`RaceVisualConfig` lives at `Assets/ScriptableObjects/Races/RaceVisualConfig.asset`. It maps the gameplay `Id` to a prefab and optional material variants.

1. Open the asset in the Inspector.
2. Add a new entry (or edit an existing one) that matches the race `Id`.
3. Assign the preview prefab.
4. (Optional) Add material variants. Each variant lets you define a label and an ordered list of materials. The first variant is the default used in the UI today.
5. Reorder entries as needed; the UI automatically looks them up by `Id`.

## 4. Verify in the Character Creation Screen

1. Select the `CharacterCreationPanel` prefab (`Assets/UI/CharacterCreation/CharacterCreationPanel.prefab`).
2. Ensure the `Race Visual Config` and `Preview Root` fields point to the config asset and the preview mount transform respectively.
3. Enter Play Mode and open the character creation flow.
4. Select the new race and confirm that the preview spawns the correct prefab/materials.

## 5. Ship It

- Commit the updated prefab(s), material(s), `RaceVisualConfig.asset`, and `RaceCatalog.cs`.
- Coordinate with engineering if additional runtime behaviour is required (animation controllers, FX triggers, etc.).

By following this pipeline, artists only need to update assets and the shared config—no code changes are required to make a new race visible in the UI.
