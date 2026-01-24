# Realm2 World Sync & Character Appearance Specification

This document defines a **server-authoritative** world sync model, character creation pipeline, and
Unity client scripting responsibilities for Realm2. It complements the authoritative architecture
rules and provides a concrete, end-to-end data model for synchronized entities, appearance, and
equipment-driven visuals.

## Goals

- Provide a consistent **authoritative world state** for players, NPCs, and interactable objects.
- Define **appearance + equipment descriptors** (IDs/metadata only) that the client maps to prefabs,
  meshes, materials, and VFX.
- Establish a **spawn/update/despawn protocol** with interest management and zone transitions.
- Specify **Unity client components** needed for inspector and runtime wiring (prefab lookup, model
  assembly, animation, and network interpolation).

---

## 1) Authoritative World Sync Model

### 1.1 Entity Categories

Entities are synchronized as **authoritative server objects**. Each entity has a stable ID and type.

- **PlayerCharacter**: controllable player entity.
- **NPC**: server-controlled AI.
- **WorldObject**: chests, doors, resource nodes, interactables.
- **DynamicObject**: projectiles, dropped loot, temporary effects.

### 1.2 Server Entity Record (Canonical)

Every entity has:

```
EntityRecord {
  entityId: string
  entityType: "PlayerCharacter" | "NPC" | "WorldObject" | "DynamicObject"
  realmId: string
  zoneId: string
  position: Vector3
  rotation: Quaternion
  velocity: Vector3
  stateFlags: string[]
  appearanceProfileId?: string
  equipmentLoadoutId?: string
  ownerAccountId?: string
  ownerCharacterId?: string
  spawnTimeUtc: string
  lastUpdatedUtc: string
}
```

### 1.3 Interest Management

The server controls which entities are relevant to each client. Clients only receive entities within
their **interest region** (zone, cell, or radius).

Recommended tiers:
1. **Zone-based**: clients only see entities in the current zone.
2. **Cell-based**: subdivide zone into cells for coarse interest.
3. **Radius-based**: dynamic radius around the player for nearby relevance.

---

## 2) Character Creation & Appearance Model

### 2.1 Authoritative Character Creation

The server stores the canonical appearance and equipment IDs. The client uses those IDs to build the
visual representation.

**Server authoritative fields:**
- raceId
- classId
- bodyArchetypeId
- faceProfileId
- hairStyleId
- hairColorId
- skinToneId
- eyeColorId
- voiceProfileId
- emoteProfileId
- startingEquipmentLoadoutId

### 2.2 Appearance Profile

```
AppearanceProfile {
  appearanceProfileId: string
  raceId: string
  classId: string
  bodyArchetypeId: string
  faceProfileId: string
  hairStyleId: string
  hairColorId: string
  skinToneId: string
  eyeColorId: string
  tattooPatternId?: string
  scarPatternId?: string
  voiceProfileId?: string
  emoteProfileId?: string
}
```

### 2.3 Equipment Loadout

```
EquipmentLoadout {
  equipmentLoadoutId: string
  slots: {
    head?: string
    chest?: string
    hands?: string
    legs?: string
    feet?: string
    back?: string
    waist?: string
    mainHand?: string
    offHand?: string
    trinket1?: string
    trinket2?: string
  }
}
```

### 2.4 Client Visual Mapping

**The client resolves IDs to visuals.** Example lookups:
- `raceId` → base skeleton + body prefab
- `bodyArchetypeId` → rig scaling and blend rules
- `hairStyleId` → hair prefab
- `equipment slots` → weapon/armor prefabs or skinned meshes

The client may cache these mappings in an addressable registry or ScriptableObjects.

---

## 3) Spawn / Update / Despawn Protocol

### 3.1 Spawn Payload

```
EntitySpawn {
  entityId
  entityType
  realmId
  zoneId
  position
  rotation
  velocity
  stateFlags
  appearanceProfile?: AppearanceProfile
  equipmentLoadout?: EquipmentLoadout
}
```

### 3.2 Update Payload (Delta)

```
EntityDelta {
  entityId
  position
  rotation
  velocity
  stateFlags?
  animationState?
  timestampUtc
}
```

### 3.3 Despawn Payload

```
EntityDespawn {
  entityId
  reason: "OutOfInterest" | "ZoneTransfer" | "Destroyed"
  timestampUtc
}
```

### 3.4 Zone Transfer

1. Server sends `EntityDespawn` with reason `ZoneTransfer`.
2. Client loads zone assets, then receives new `EntitySpawn` packets.

---

## 4) Backend Data Tables (Suggested Schema)

### 4.1 Characters

```
characters (
  character_id PK,
  account_id,
  realm_id,
  name,
  race_id,
  class_id,
  appearance_profile_id,
  equipment_loadout_id,
  last_zone_id,
  last_position_x,
  last_position_y,
  last_position_z,
  created_at,
  updated_at
)
```

### 4.2 Appearance Profiles

```
appearance_profiles (
  appearance_profile_id PK,
  race_id,
  class_id,
  body_archetype_id,
  face_profile_id,
  hair_style_id,
  hair_color_id,
  skin_tone_id,
  eye_color_id,
  tattoo_pattern_id,
  scar_pattern_id,
  voice_profile_id,
  emote_profile_id
)
```

### 4.3 Equipment Loadouts

```
equipment_loadouts (
  equipment_loadout_id PK,
  head_item_id,
  chest_item_id,
  hands_item_id,
  legs_item_id,
  feet_item_id,
  back_item_id,
  waist_item_id,
  main_hand_item_id,
  off_hand_item_id,
  trinket1_item_id,
  trinket2_item_id
)
```

---

## 5) Race & Class Visual Bindings

### 5.1 Base Race Prefabs (Unity)

Each race maps to a base prefab + skeleton:

- **Human**: `Assets/Prefabs/Races/HumanBase.prefab`
- **Felarian**: `Assets/Prefabs/Races/FelarianBase.prefab`
- **Crystallian**: `Assets/Prefabs/Races/CrystallianBase.prefab`
- **Revenant**: `Assets/Prefabs/Races/RevenantBase.prefab`
- **Gearling**: `Assets/Prefabs/Races/GearlingBase.prefab`

### 5.2 Class Visual Overrides

Class drives:
- idle animation sets
- VFX hooks for spells
- class-specific idle pose modifiers

Example mapping:

```
ClassVisualProfile {
  classId
  idleAnimationSetId
  castAnimationSetId
  weaponGripProfileId
  classVfxProfileId
}
```

---

## 6) Unity Client Components (Required)

### 6.1 Core Networking Components

- **NetworkEntity**: holds entityId and sync state.
- **EntityInterpolator**: smooths server updates (position/rotation).
- **EntityRegistry**: maps entityId → runtime GameObject.
- **ZoneLoader**: loads/unloads zone scenes and handles zone transfer.

### 6.2 Appearance + Equipment Components

- **AppearanceAssembler**
  - Reads `AppearanceProfile` and instantiates body + hair + cosmetics.
- **EquipmentVisualizer**
  - Attaches weapon/armor prefabs based on equipment loadout.
- **PrefabLookupRegistry**
  - ScriptableObject registry mapping IDs to prefabs/materials.

### 6.3 Character Creation UI Components

- **CharacterCreationController**
  - Writes appearance changes into a local draft model.
- **AppearancePreview**
  - Uses the same `AppearanceAssembler` logic in a preview scene.
- **CharacterCreationRequestBuilder**
  - Sends chosen IDs to the server to create the character.

---

## 7) Services & APIs (Suggested Endpoints)

### 7.1 Character Creation API

```
POST /characters
{
  name,
  raceId,
  classId,
  appearanceProfile,
  equipmentLoadout
}
```

### 7.2 Spawn/World Stream (WebSocket)

```
WS /ws/world?token=JWT
```

**Message types:**
- `entity.spawn`
- `entity.delta`
- `entity.despawn`
- `zone.transfer`

---

## 8) Debugging & Observability Hooks

### 8.1 Server Audit Events

Emit structured events:
- `character.created`
- `entity.spawned`
- `entity.despawned`
- `zone.entered`
- `zone.exited`

### 8.2 Client Diagnostics

Add a client debug overlay:
- Entities in interest
- Packet latency
- Last sync timestamp
- Current zone

---

## 9) Implementation Sequence (Recommended)

1. Define canonical schemas (AppearanceProfile, EquipmentLoadout).
2. Build spawn/update/despawn protocol in server and client.
3. Implement Unity appearance/equipment assembly pipeline.
4. Wire character creation UI → backend creation endpoint.
5. Add server logging + client debug overlay.

---

## 10) Non-Goals (Explicitly Out of Scope)

- Asset creation (textures, meshes, VFX).
- Full animation rigging beyond placeholders.
- Dedicated load-balancer / shard migration logic.

---

## Appendix A: Example Appearance IDs (Initial Set)

```
raceId: "human" | "felarian" | "crystallian" | "revenant" | "gearling"
classId: "warrior" | "rogue" | "ranger" | "wizard" | "sage" | "technomancer" | "timeMage" | "necromancer" | "mythologist"
bodyArchetypeId: "slim" | "athletic" | "heavy"
hairStyleId: "short_01" | "long_01" | "braided_01"
skinToneId: "tone_01" | "tone_02" | "tone_03"
```

