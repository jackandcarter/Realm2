# Builder Class Feature Spec

## Purpose
Define Builder progression, abilities, Arkitect tooling, and the client/server runtime contract for plots, blueprints, and realtime construction. This spec focuses on the **client-side logic** needed to align with server truth while remaining scalable for realtime placement and terrain edits.

## Lore & Unlock Context
- Builders unlock Arkitect through the main story quest chain and only then can **alter the world live** within their hosted realm.
- Arkitect progression ties into class progression, so Builder levels and Arkitect ability levels advance together.

## Unlock & Class Identity
- **Unlock Quest:** `quest-builder-arkitect` (builder quest chain). The Arkitect UI and builder abilities are gated until completion.
- **Role:** Builder (non-combat utility).
- **Weapon Proficiency:** toolkit.
- **Primary Stats:** Energy + Construction.

## Progression & Stats
### Non-combat stat model (authoritative)
Builders do **not** use combat stats. They use:
- **Energy**: consumed to place/remove objects and blueprints. If energy reaches zero mid-build, the build pauses until energy is restored.
- **Construction**: controls build time for blueprints (faster build at higher Construction).

### Build-time energy requirements
- Each blueprint has a **build time** and an **energy requirement** for completion.
- A builder can start a build even if the total required energy exceeds their current max, but completion is gated until they recover enough energy over time (rest, consumables, buffs).

### Passive abilities & buffs
Builders can unlock passive abilities or buffs that affect:
- Energy consumption per placement/removal.
- Energy regeneration while building or resting.
- Construction speed or partial refund on interrupted builds.

### Arkitect ability leveling
- Arkitect abilities gain experience through use.
- Milestones (e.g., placing 50 objects) unlock new buffs or abilities, which appear in the Builder dock once requirements are met.

## Builder Ability Suite (Client Feature Targets)
Builder abilities are expected to surface in the class ability dock, with Arkitect UI panels used for heavy building workflows.

### Core Abilities (runtime actions)
- **Plot Claim:** place a build plot on approved terrain (creates/claims plot).
- **Terrain Sculpt:** raise/lower terrain within owned plot boundaries.
- **Material Paint:** paint terrain layer/texture within plot.
- **Blueprint Place:** place prebuilt structures from blueprint catalog.
- **Structure Edit:** reposition/rotate/duplicate placed structures within plot.

### Terrain abilities (starting set)
- **Flatten**
- **Smooth**
- **Raise/Lower**
- **Relinquish** (revert a changed state to the last accepted server state)

### Terrain abilities (progression unlocks)
- **Add Water**
- **Soil/Material Paint** (textures like sand, snow, grass, flowers, etc. as content is authored)
- **Sculpt** (full brush suite for realtime terrain editing; final unlock)

### Arkitect UI Actions (panel-driven)
- **Plots Panel:** choose plot size, see ownership/permissions, and plot metadata.
- **Blueprints Panel:** show blueprint catalog + material requirements.
- **Materials Panel:** show resource inventory and a **3D model preview drawer** so builders can inspect appearance before placement.
- **Construction Queue Panel:** visualize active builds, queue, and errors.

## Blueprint & Construction Data Model
### Blueprint definition
- **BlueprintId**
- **PrefabId**
- **Bounds** (for collision/snap previews)
- **Material Requirements** (itemized list)
- **Allowed Placements** (plot-only, elevation bounds, terrain type tags)
- **Preview Thumbnail / UI descriptor**

### Construction instance
Stores runtime + persistence data:
- **ConstructionId** (uuid)
- **BlueprintId** (or custom structure tag)
- **PlotId** (required for authority)
- **Transform** (position/rotation/scale)
- **Placement State** (`preview`, `pending`, `placed`, `failed`)
- **Material Cost Snapshot** (materials consumed at placement time)

### Professions & material sources
- Builders do **not** gather materials directly.
- Materials are supplied by other professions (gatherer, farmer, painter, etc.) and routed into the Materials panel for placement/preview.

## Plot System Requirements
### Plot definition
- **PlotId** / **PlotIdentifier**
- **Bounds** (world bounds, elevation, material layer)
- **OwnerUserId** (server truth)
- **Permissions** (edit/build/invite lists)
- **Build Zone Compliance** (server validated)
- **Plot Size Grid** (server-defined sizes and coordinates)
- **Placement Templates** (preconfigured shape/size templates that render as highlighted terrain before placement)

### Build area limits
- The server defines **allowed build volumes** (horizontal bounds + min/max height).
- Terrain edits and structure placement must remain inside these limits.
- Builders cannot dig below or raise above the permitted height range; terrain holes that break navigation are disallowed.

### Plot operations (expected flows)
1. **Client preview** → build zone check (client-side if possible).
2. **Server submit** → server validates ownership, zone, overlaps, permissions.
3. **Server confirm** → client updates local cache.
4. **Failure** → client shows explicit reason and rolls back preview.

### Plot progression & settlements
- Builders start with a default plot size; larger plots unlock over time.
- Multiple connected plots can contribute to settlement evolution (settlements → houses → kingdoms).

## Client-Side Runtime Contract
### Authoritative sources
1. **Server**: Ownership, plot data, construction records.
2. **Client**: Preview state, optimistic placement visuals, UI/ability gating.

### Client caches
- **Plot Cache**: Derived from chunk/plot payloads (`RealmChunkCache`).
- **Construction Registry**: runtime instances tied to `ArkitectRegistry`.
- **Build State Snapshot**: local aggregator for UI (materials, queue, active plot).

### Placement & Edit Pipeline
1. **Preview**: spawn a local preview object (ghost), collision & boundary checks.
2. **Validation**: client checks plot permissions + bounds if known (server is authoritative).
3. **Submission**: send placement request containing blueprintId, plotId, transform, material snapshot.
4. **Server Ack**: updates plot/terrain or construction list in chunk stream.
5. **Reconcile**: client destroys preview, spawns server-backed construction instance.

### Plot size & highlight system (client responsibilities)
- The server defines plot **grid sizes** and authoritative **coordinates**.
- The client renders a **ground highlight** for the plot preview using server-provided size/shape data.
- The client must never assume plot sizes or accept placement without server validation.

### Realtime changes + sync (design options to finalize)
We need an agreed approach that avoids overloading server while keeping server authority:
- **Batching**: group terrain/placement edits per chunk/plot, with server-defined caps.
- **Backpressure**: queue client edits and block new high-cost edits while awaiting server acks.
- **Error correction**: if server rejects, revert to last known good snapshot and rebase queued edits.

### Sync fidelity tiers
1. **Immediate**: placement create/destroy events.
2. **Batch**: terrain/paint edits.
3. **Deferred**: complex edits (large blueprint builds) with progress stages.

## UI + Ability Gating
### Arkitect availability
- Controlled by `PlayerClassStateManager.IsArkitectAvailable`.
- Arkitect UI should mount only for Builder class while respecting quest unlock state.

### Permissions in UI
The client UI must reflect:
- **Plot ownership** (editable, read-only, or locked).
- **Build zone validity** (visual feedback).
- **Blueprint material sufficiency** (requirements vs inventory).

### Multi-builder support
- Multiple builders can contribute to the same blueprint build to reduce total time/energy required.
- No global cap on concurrent builders; server still enforces per-plot/zone limits and conflict resolution.

## Missing Feature Inventory (What’s Left)
### Recently completed
- **Build-state persistence** for plots + constructions (server endpoints, repository, and client cache/persistence).
- **Build-state schema alignment** with plot/construction payloads in the DB and migration runner.
- **Construction instance persistence** (placement state, plot assignment, material snapshot) via serialized state + Arkitect registry reconciliation.
- **Runtime plot gating + persistence** (Arkitect availability checks, build zone validation, and plot snapshot saving).
- **Plot identifiers + build-zone metadata** in `BuildPlotDefinition`, with Arkitect plot dropdown using the identifier.
- **Plot permission endpoints + client models** (UI wiring still pending).

### Data + Progression
- **Energy/Construction stat tables** and level progression in server definitions.
- **Builder ability definitions** in ability registry (design + data).
- **Blueprint catalog** with materials and prefabs (authoring pipeline).
- **Plot permission UI** that surfaces server-provided permissions.
- **Arkitect ability XP tables** and milestone unlock tracking.

### Client Runtime
- **Construction queue manager** for batching edits and showing failures.
- **Authoritative reconcile logic** (server confirmation vs local previews).
- **Preview collision + snapping rules** (grid/plot boundaries).
- **Material checks** that consume inventory only on server ack.
- **Ground highlight renderer** for plot size previews using server-defined sizes.
- **Ability XP tracking hooks** to mirror server-authoritative progression.

### Server Support
- **Construction persistence** in plot/chunk payloads beyond build-state snapshots.
- **Validated terrain edit batching** (rate limits, conflict resolution).
- **Blueprint ownership tracking** (vendor/chest acquisition systems).
- **Build-area enforcement** (volume limits + min/max height) in plot/terrain validation.
- **Co-builder contribution tracking** for shared builds.

### UX/Tooling
- **Arkitect UI tabs** for blueprints/materials/plots (UI prefabs & binding).
- **Telemetry** for build action errors (debugging + balancing).
- **Editor tooling** for blueprint definition previews + auto-bounds capture.

## Open Questions
- Cross-realm plot sharing is **out of scope for now** (characters do not hop realms yet).
- Should blueprint placements be **instant** or **timed builds** with progress?
- Are there **global limits** for active constructions per plot/realm?
- Should terrain edits be **continuous sculpt** or **discrete tool strokes**?
