export interface ZoneSpawnPoint {
  id: string;
  position: { x: number; y: number; z: number };
  rotation?: { x: number; y: number; z: number; w: number };
}

export interface ZoneDefinition {
  zoneId: string;
  sceneName: string;
  terrainRegionId: string;
  worldBounds: { center: { x: number; y: number; z: number }; size: { x: number; y: number; z: number } };
  spawnPoints: ZoneSpawnPoint[];
}

export const zoneRegistry: ZoneDefinition[] = [];

export function findZoneById(zoneId: string): ZoneDefinition | undefined {
  return zoneRegistry.find((zone) => zone.zoneId === zoneId);
}
