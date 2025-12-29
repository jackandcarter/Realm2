# Terrain Regions

This document describes how terrain regions are configured in the client and which settings are available in the inspector for `TerrainRegion` components.

## TerrainRegion component

**Location:** `Assets/Scripts/Client/Terrain/TerrainRegion.cs`

### Identity

- **Region Id**: Unique identifier for the region. Use a stable key that matches backend or content data.
- **Zone Id**: Optional identifier used to group regions for streaming or gameplay rules.

### Terrains

- **Use Terrain Bounds**: When enabled, the region computes its world bounds from the assigned Unity `Terrain` objects.
- **Terrains**: The Unity `Terrain` references that define the region's surface and bounding volume.
- **Manual World Bounds**: Fallback bounds used when terrain bounds are disabled or no terrains are assigned.

### Chunking

- **Chunk Origin Offset**: Local-space offset applied before chunk coordinates are calculated.
- **Chunk Size Override**: Override for chunk size. Set to a value greater than zero to ignore the digger system size.
- **Digger System**: Optional reference used to provide a default chunk size when the override is not set.

### Map

- **Map World Bounds**: World-space rectangle used to place the region on 2D maps.
- **Mini Map Texture**: Texture used for the minimap representation of the region.
- **World Map Texture**: Texture used for the world map representation of the region.
