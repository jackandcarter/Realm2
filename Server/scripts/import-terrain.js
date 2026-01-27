const fs = require('fs');
const path = require('path');

const DEFAULT_TERRAIN_URL = 'http://localhost:3007';
const DEFAULT_WORLD_URL = 'http://localhost:3000';

const args = process.argv.slice(2);

function readArg(flag) {
  const index = args.indexOf(flag);
  if (index === -1 || index === args.length - 1) {
    return null;
  }
  return args[index + 1];
}

function hasFlag(flag) {
  return args.includes(flag);
}

function printUsage() {
  console.log(`Usage:
  node scripts/import-terrain.js --file <path> --realm <realmId> --token <jwt> [--terrain-url <url>] [--world-url <url>]

Environment variables:
  REALM_AUTH_TOKEN   Optional auth token if --token is not provided.
  TERRAIN_API_URL    Optional base URL if --terrain-url is not provided.
  WORLD_API_URL      Optional base URL if --world-url is not provided.

Examples:
  node scripts/import-terrain.js --file ./terrain-import.json --realm realm-1 --token $JWT
  node scripts/import-terrain.js --file ./terrain-bundle.json --realm realm-1 --terrain-url http://localhost:3007 --world-url http://localhost:3000
`);
}

if (hasFlag('--help') || hasFlag('-h')) {
  printUsage();
  process.exit(0);
}

const filePath = readArg('--file');
const realmId = readArg('--realm');
const token = readArg('--token') || process.env.REALM_AUTH_TOKEN;
const legacyBaseUrl = readArg('--base-url');
const terrainUrl = (readArg('--terrain-url') || legacyBaseUrl || process.env.TERRAIN_API_URL || DEFAULT_TERRAIN_URL).replace(
  /\/$/,
  ''
);
const worldUrl = (readArg('--world-url') || process.env.WORLD_API_URL || DEFAULT_WORLD_URL).replace(/\/$/, '');

if (!filePath || !realmId) {
  console.error('Missing required arguments.');
  printUsage();
  process.exit(1);
}

if (!token) {
  console.error('Missing auth token. Provide --token or REALM_AUTH_TOKEN.');
  process.exit(1);
}

const resolvedPath = path.resolve(process.cwd(), filePath);

if (!fs.existsSync(resolvedPath)) {
  console.error(`File not found: ${resolvedPath}`);
  process.exit(1);
}

let payload;
try {
  const raw = fs.readFileSync(resolvedPath, 'utf8');
  payload = JSON.parse(raw);
} catch (error) {
  console.error(`Failed to read or parse JSON: ${error.message}`);
  process.exit(1);
}

function resolveTerrainPayload(source) {
  if (!source) {
    return null;
  }
  if (Array.isArray(source.chunks)) {
    return source;
  }
  if (source.terrain && Array.isArray(source.terrain.chunks)) {
    return source.terrain;
  }
  return null;
}

function resolveRegions(source) {
  if (Array.isArray(source?.regions)) {
    return source.regions;
  }
  return [];
}

function resolveBuildZones(source) {
  if (Array.isArray(source?.buildZones)) {
    return source.buildZones;
  }
  if (Array.isArray(source?.buildZones?.zones)) {
    return source.buildZones.zones;
  }
  return [];
}

const terrainPayload = resolveTerrainPayload(payload);
const regions = resolveRegions(payload);
const buildZones = resolveBuildZones(payload);

if (
  (!terrainPayload || !Array.isArray(terrainPayload.chunks) || terrainPayload.chunks.length === 0) &&
  regions.length === 0 &&
  buildZones.length === 0
) {
  console.error('Payload must include terrain chunks, regions, or build zones.');
  process.exit(1);
}

if (terrainPayload && (!Array.isArray(terrainPayload.chunks) || terrainPayload.chunks.length === 0)) {
  console.error('Terrain payload must include a non-empty "chunks" array.');
  process.exit(1);
}

const terrainImportUrl = `${terrainUrl}/realms/${encodeURIComponent(realmId)}/terrain/import`;
const regionBaseUrl = `${terrainUrl}/realms/${encodeURIComponent(realmId)}/terrain/regions`;
const buildZonesUrl = `${worldUrl}/realms/${encodeURIComponent(realmId)}/build-zones`;

async function importTerrain() {
  if (!terrainPayload) {
    return;
  }
  const response = await fetch(terrainImportUrl, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify(terrainPayload),
  });

  if (!response.ok) {
    const text = await response.text();
    console.error(`Terrain import failed (${response.status}): ${text}`);
    process.exit(1);
  }

  const data = await response.json();
  const changeCount = Array.isArray(data.changes) ? data.changes.length : 0;
  console.log(`Terrain import complete. Changes emitted: ${changeCount}`);
}

async function importRegions() {
  if (regions.length === 0) {
    return;
  }
  for (const region of regions) {
    const regionId = region?.regionId;
    if (!regionId || typeof regionId !== 'string') {
      console.error('Region entry missing regionId.');
      process.exit(1);
    }
    const response = await fetch(`${regionBaseUrl}/${encodeURIComponent(regionId)}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify(region),
    });

    if (!response.ok) {
      const text = await response.text();
      console.error(`Region import failed (${response.status}): ${text}`);
      process.exit(1);
    }
  }
  console.log(`Region import complete. Regions upserted: ${regions.length}`);
}

async function importBuildZones() {
  if (buildZones.length === 0) {
    return;
  }
  const response = await fetch(buildZonesUrl, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify({ zones: buildZones }),
  });

  if (!response.ok) {
    const text = await response.text();
    console.error(`Build zone import failed (${response.status}): ${text}`);
    process.exit(1);
  }

  console.log(`Build zone import complete. Zones upserted: ${buildZones.length}`);
}

async function run() {
  await importRegions();
  await importBuildZones();
  await importTerrain();
}

run().catch((error) => {
  console.error(`Import failed: ${error.message}`);
  process.exit(1);
});
