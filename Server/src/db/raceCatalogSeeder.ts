import { coreClassDefinitions } from '../gameplay/design/systemFoundations';
import { getAllClassRules, isClassAllowedForRace } from '../config/classRules';
import { RACE_DEFINITIONS } from '../config/races';
import type { DbExecutor } from './database';

async function tableHasRows(db: DbExecutor, table: string): Promise<boolean> {
  const rows = await db.query<{ total: number }[]>(
    `SELECT COUNT(*) as total FROM ${table}`
  );
  return (rows[0]?.total ?? 0) > 0;
}

function serialize(value: unknown): string {
  return JSON.stringify(value ?? {});
}

export async function seedRaceCatalogData(db: DbExecutor): Promise<void> {
  const now = new Date().toISOString();
  const hasRaces = await tableHasRows(db, 'races');
  if (!hasRaces) {
    for (const race of RACE_DEFINITIONS) {
      await db.execute(
        `INSERT INTO races (id, display_name, customization_json, created_at, updated_at)
         VALUES (?, ?, ?, ?, ?)
         ON DUPLICATE KEY UPDATE
           display_name = VALUES(display_name),
           customization_json = VALUES(customization_json),
           updated_at = VALUES(updated_at)`,
        [race.id, race.displayName, serialize(race.customization ?? {}), now, now]
      );
    }
  }

  const hasRules = await tableHasRows(db, 'race_class_rules');
  if (!hasRules) {
    const classRules = getAllClassRules();
    for (const race of RACE_DEFINITIONS) {
      for (const rule of classRules) {
        if (!isClassAllowedForRace(rule.id, race.id)) {
          continue;
        }
        await db.execute(
          `INSERT INTO race_class_rules (race_id, class_id, unlock_method, created_at, updated_at)
           VALUES (?, ?, ?, ?, ?)
           ON DUPLICATE KEY UPDATE
             unlock_method = VALUES(unlock_method),
             updated_at = VALUES(updated_at)`,
          [race.id, rule.id, rule.unlockMethod, now, now]
        );
      }
    }
  }

  const hasProficiencies = await tableHasRows(db, 'class_weapon_proficiencies');
  if (!hasProficiencies) {
    for (const classDef of coreClassDefinitions) {
      for (const weaponType of classDef.weaponProficiencies) {
        await db.execute(
          `INSERT INTO class_weapon_proficiencies (class_id, weapon_type, created_at)
           VALUES (?, ?, ?)
           ON DUPLICATE KEY UPDATE
             created_at = VALUES(created_at)`,
          [classDef.id, weaponType, now]
        );
      }
    }
  }
}
