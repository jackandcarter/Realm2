# World Sync Setup Guide (Server + Unity Client)

This guide extends the existing world sync specification with **concrete setup steps** for mapping
terrain/zone identifiers to Unity scenes and server-side persistence. Use it alongside the
authoritative world sync spec to ensure consistent spawn locations, entity streams, and terrain
updates across the stack.【F:Docs/WorldSyncSpec.md†L3-L185】

---

## 1) Core Concepts to Align Up Front

### Authoritative world ownership
- The **world server** owns authoritative state for player/NPC/world objects and is responsible for
  spawn/update/despawn and zone transitions.【F:Docs/WorldSyncSpec.md†L19-L185】
- The **terrain server** owns *terrain mutations* (dig/raise/paint) and streams those changes to
  clients that are in the relevant terrain region.

### Zone + terrain are first-class identifiers
You must have **stable zone/region identifiers** shared between server and Unity:
- Server uses `zoneId` to place entities and filter interest lists.【F:Docs/WorldSyncSpec.md†L34-L61】
- Unity uses `TerrainRegion.Region Id` and `TerrainRegion.Zone Id` to identify terrain extents and
  grouping.【F:Docs/TerrainRegions.md†L7-L25】

**Recommendation:** Treat `TerrainRegion.Region Id` as the `zoneId` used by the world server and
`TerrainRegion.Zone Id` as a higher-level grouping if you need multi-region streaming.

---

## 2) Minimum Data Model (Server)

Add a **Zone Registry** (config or database) that defines where each zone lives in the world and how
to load it in Unity. A starter TypeScript registry lives in `Server/src/config/zoneRegistry.ts` and
is intentionally empty until you seed real data.

```
ZoneDefinition {
  zoneId: string
  sceneName: string
  terrainRegionId: string
  spawnPoints: { id: string, position: Vector3, rotation: Quaternion }[]
  worldBounds: { center: Vector3, size: Vector3 }
}
```

### Character location persistence
Persist **zone + position + rotation** on the server. The world sync spec already proposes a schema
with `last_zone_id` + `last_position_x/y/z`. Extend it with rotation if needed for spawn facing:

```
characters (
  ...
  last_zone_id,
  last_position_x,
  last_position_y,
  last_position_z,
  last_rotation_x,
  last_rotation_y,
  last_rotation_z,
  last_rotation_w
)
```

This makes the **world server the source of truth**, while still allowing the client to request
spawn data on connect.【F:Docs/WorldSyncSpec.md†L188-L209】

### Client-facing string (preview UI)
The preview flow expects `CharacterInfo.lastKnownLocation` as an `x,y,z` string, with a fallback spawn
point if missing or invalid.【F:Docs/WorldPreviewAndUISetup.md†L65-L84】

**Recommendation:** Generate `lastKnownLocation` from the persisted numeric fields (zone + position)
and send it only as a convenience for UI preview; the server should still treat the numeric fields
as authoritative.

---

## 3) World Server Responsibilities (Authoritative Entities)

Implement these responsibilities in the world server (mirroring the world sync spec):

1. **Spawn/Update/Despawn streams** for every connected client.【F:Docs/WorldSyncSpec.md†L138-L185】
2. **Interest management** by zone/cell/radius to scope what a client receives.【F:Docs/WorldSyncSpec.md†L53-L61】
3. **Zone transitions**: despawn with `ZoneTransfer`, trigger client zone load, respawn in new zone.
4. **Movement authority**: accept client movement inputs, validate, and publish authoritative
   position updates (clients should interpolate between authoritative updates).
5. **Persistence**: update last known zone/position on key events (logout, checkpoint, zone change).

---

## 4) Terrain Server Responsibilities (Terrain Mutations)

The terrain server should:

1. Track **terrain edits** by region (e.g., chunk coordinates).
2. Broadcast **terrain diff events** to clients in that region.
3. Optionally store **terrain snapshots** per chunk for persistence and quick joins.

The terrain server does **not** own player/NPC positions—only the mutable terrain state.

---

## 5) Unity Client Setup (World + Terrain)

### 5.1 Terrain regions in Unity
Use the `TerrainRegion` component to define where each terrain lives and to identify it consistently:

- Set **Region Id** to the same `zoneId` as the world server.
- Set **Zone Id** if you need higher-level grouping (optional).
- Use `Terrains` or `Manual World Bounds` to define the region’s extents for streaming decisions.【F:Docs/TerrainRegions.md†L7-L27】

### 5.2 Spawn points
Create `PlayerSpawnPoint` objects for fallback spawns and initial entry points.
The preview flow falls back to these if no valid location is provided.【F:Docs/WorldPreviewAndUISetup.md†L78-L84】

### 5.3 Scene loading
The preview flow loads the world scene additively and spawns the avatar based on
`CharacterInfo.lastKnownLocation` if available.【F:Docs/WorldPreviewAndUISetup.md†L65-L100】

**Recommendation:** When entering the world, always request authoritative spawn data from the world
server (zone + position + rotation), then:
1. Load the matching scene,
2. Find the matching `TerrainRegion` by `Region Id`,
3. Place the player entity at the authoritative position.

---

## 6) Sync Flow (End-to-End)

### 6.1 Connect & spawn
1. Client authenticates and selects character.
2. Client requests **authoritative spawn** from world server.
3. World server returns `zoneId`, `position`, `rotation`, and initial entity snapshot.
4. Client loads zone scene, resolves `TerrainRegion`, and spawns the player entity.

### 6.2 Movement updates
1. Client sends movement inputs to world server.
2. World server validates and emits authoritative `EntityDelta` updates.
3. Client interpolates between authoritative updates to smooth movement.【F:Docs/WorldSyncSpec.md†L157-L169】

### 6.3 Terrain updates
1. Client sends terrain edit commands (with zone/region + chunk coords).
2. Terrain server validates and emits terrain diff events.
3. Clients in the region apply diffs to local terrain meshes.

---

## 7) Practical Checklist

### Server
- [ ] Zone registry is defined and shared with the client.
- [ ] Characters persist `last_zone_id` + numeric position fields.
- [ ] World server owns entity sync and zone transitions.
- [ ] Terrain server owns terrain diffs only.

### Unity
- [ ] `TerrainRegion.Region Id` matches server `zoneId`.
- [ ] `PlayerSpawnPoint` exists for fallback spawns.
- [ ] Preview uses `lastKnownLocation` string format (`x,y,z`).【F:Docs/WorldPreviewAndUISetup.md†L65-L84】
- [ ] World entry loads correct scene based on `zoneId`.

---

## 8) Suggested Next Steps

1. Define the **Zone Registry** in server config.
2. Implement world server spawn endpoint that returns `zoneId + position + rotation`.
3. Add client-side zone resolver to map `zoneId → scene + TerrainRegion`.
4. Implement terrain diff pipeline keyed by `TerrainRegion.Region Id`.
