export interface RealmHostingInfo {
  realmId: string;
  worldSceneName: string;
  worldServiceUrl?: string;
}

const defaultWorldSceneName = process.env.DEFAULT_WORLD_SCENE_NAME ?? 'SampleScene';
const defaultWorldServiceUrl = process.env.DEFAULT_WORLD_SERVICE_URL?.trim();

const registry: RealmHostingInfo[] = [
  {
    realmId: 'realm-elysium-nexus',
    worldSceneName: 'SampleScene',
  },
  {
    realmId: 'realm-arcane-haven',
    worldSceneName: 'SampleScene',
  },
  {
    realmId: 'realm-gearspring',
    worldSceneName: 'SampleScene',
  },
];

export function resolveRealmHosting(realmId: string): RealmHostingInfo {
  const match = registry.find((entry) => entry.realmId === realmId);
  return {
    realmId,
    worldSceneName: match?.worldSceneName ?? defaultWorldSceneName,
    worldServiceUrl: match?.worldServiceUrl ?? defaultWorldServiceUrl,
  };
}
