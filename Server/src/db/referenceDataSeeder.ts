import type { DbExecutor } from './database';
import { coreClassDefinitions, equipmentArchetypes, equipmentCatalog, resourceDefinitions } from '../gameplay/design/systemFoundations';
import { generatedAbilityDefinitions } from '../gameplay/combat/generated/abilityRegistry';

function buildWeaponTypes(): Array<{ id: string; displayName: string }> {
  const types = new Map<string, string>();
  const addType = (id: string | undefined) => {
    if (!id) {
      return;
    }
    if (!types.has(id)) {
      const display = id
        .split('-')
        .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
        .join(' ');
      types.set(id, display);
    }
  };

  for (const classDef of coreClassDefinitions) {
    for (const weaponType of classDef.weaponProficiencies) {
      addType(weaponType);
    }
  }

  for (const entry of equipmentArchetypes) {
    if (entry.category === 'weapon') {
      addType(entry.subtype);
    }
  }

  for (const entry of equipmentCatalog) {
    if (entry.category === 'weapon') {
      addType(entry.subtype);
    }
  }

  return Array.from(types.entries()).map(([id, displayName]) => ({ id, displayName }));
}

function buildResourceTypes(): Array<{ id: string; displayName: string; category: string }> {
  return resourceDefinitions.map((definition) => ({
    id: definition.id,
    displayName: definition.name,
    category: definition.category,
  }));
}

function buildAbilityTypes(): Array<{ id: string; displayName: string }> {
  const seen = new Set<string>();
  const types: Array<{ id: string; displayName: string }> = [];
  for (const ability of generatedAbilityDefinitions) {
    const type = (ability as { abilityType?: string }).abilityType ?? 'combat';
    if (seen.has(type)) {
      continue;
    }
    seen.add(type);
    types.push({ id: type, displayName: type.charAt(0).toUpperCase() + type.slice(1) });
  }
  if (!seen.has('combat')) {
    types.push({ id: 'combat', displayName: 'Combat' });
  }
  return types;
}

export async function seedReferenceData(db: DbExecutor): Promise<void> {
  const now = new Date().toISOString();

  const weaponTypes = buildWeaponTypes();
  for (const entry of weaponTypes) {
    await db.execute(
      `INSERT INTO weapon_types (id, display_name, created_at, updated_at)
       VALUES (?, ?, ?, ?)
       ON DUPLICATE KEY UPDATE
         display_name = VALUES(display_name),
         updated_at = VALUES(updated_at)`,
      [entry.id, entry.displayName, now, now]
    );
  }

  const resourceTypes = buildResourceTypes();
  for (const entry of resourceTypes) {
    await db.execute(
      `INSERT INTO resource_types (id, display_name, category, created_at, updated_at)
       VALUES (?, ?, ?, ?, ?)
       ON DUPLICATE KEY UPDATE
         display_name = VALUES(display_name),
         category = VALUES(category),
         updated_at = VALUES(updated_at)`,
      [entry.id, entry.displayName, entry.category, now, now]
    );
  }

  const abilityTypes = buildAbilityTypes();
  for (const entry of abilityTypes) {
    await db.execute(
      `INSERT INTO ability_types (id, display_name, created_at, updated_at)
       VALUES (?, ?, ?, ?)
       ON DUPLICATE KEY UPDATE
         display_name = VALUES(display_name),
         updated_at = VALUES(updated_at)`,
      [entry.id, entry.displayName, now, now]
    );
  }
}
