import type { DbExecutor } from '../database';
import {
  actionRequestTypes,
  abilityTypes,
  armorTypes,
  chatChannelTypes,
  classRoles,
  classResourceTypes,
  combatEventKinds,
  equipmentSlots,
  friendStatuses,
  guildRoles,
  itemCategories,
  itemRarities,
  partyRoles,
  questStatuses,
  raceIds,
  resourceIds,
  tradeStatuses,
  weaponHandedness,
  weaponTypes,
} from '../../config/gameEnums';

function buildEnumList(values: readonly string[]): string {
  return values.map((value) => `'${value}'`).join(', ');
}

async function normalizeEnumColumn(
  db: DbExecutor,
  table: string,
  column: string,
  values: readonly string[],
  defaultValue: string | null,
): Promise<void> {
  const enumList = buildEnumList(values);
  const columnRef = `\`${column}\``;
  const tableRef = `\`${table}\``;
  const invalidClause = `${columnRef} NOT IN (${enumList})`;
  const nullClause = defaultValue === null ? '' : ` OR ${columnRef} IS NULL`;
  const updateValue = defaultValue === null ? 'NULL' : `'${defaultValue}'`;

  await db.execute(
    `UPDATE ${tableRef}
     SET ${columnRef} = ${updateValue}
     WHERE ${invalidClause}${nullClause}`
  );
}

async function enforceEnumColumn(
  db: DbExecutor,
  table: string,
  column: string,
  values: readonly string[],
  options: { defaultValue?: string; nullable?: boolean } = {},
): Promise<void> {
  const { defaultValue, nullable } = options;
  const enumList = buildEnumList(values);
  const columnRef = `\`${column}\``;
  const tableRef = `\`${table}\``;
  const defaultClause = defaultValue ? `DEFAULT '${defaultValue}'` : '';
  const nullClause = nullable ? 'NULL' : 'NOT NULL';

  await db.execute(
    `ALTER TABLE ${tableRef}
     MODIFY ${columnRef} ENUM(${enumList}) ${nullClause} ${defaultClause}`
  );
}

export async function up(db: DbExecutor): Promise<void> {
  const weaponDefault = weaponTypes[0] ?? 'greatsword';

  await normalizeEnumColumn(db, 'characters', 'race_id', raceIds, 'human');
  await enforceEnumColumn(db, 'characters', 'race_id', raceIds, {
    defaultValue: 'human',
  });

  await normalizeEnumColumn(db, 'classes', 'role', classRoles, null);
  await enforceEnumColumn(db, 'classes', 'role', classRoles, { nullable: true });

  await normalizeEnumColumn(db, 'classes', 'resource_type', classResourceTypes, null);
  await enforceEnumColumn(db, 'classes', 'resource_type', classResourceTypes, { nullable: true });

  await normalizeEnumColumn(db, 'items', 'category', itemCategories, 'consumable');
  await enforceEnumColumn(db, 'items', 'category', itemCategories, {
    defaultValue: 'consumable',
  });

  await normalizeEnumColumn(db, 'items', 'rarity', itemRarities, 'common');
  await enforceEnumColumn(db, 'items', 'rarity', itemRarities, { defaultValue: 'common' });

  await normalizeEnumColumn(db, 'weapons', 'weapon_type', weaponTypes, weaponDefault);
  await enforceEnumColumn(db, 'weapons', 'weapon_type', weaponTypes, {
    defaultValue: weaponDefault,
  });

  await normalizeEnumColumn(db, 'weapons', 'handedness', weaponHandedness, 'one-hand');
  await enforceEnumColumn(db, 'weapons', 'handedness', weaponHandedness, {
    defaultValue: 'one-hand',
  });

  await normalizeEnumColumn(db, 'armor', 'slot', equipmentSlots, 'chest');
  await enforceEnumColumn(db, 'armor', 'slot', equipmentSlots, { defaultValue: 'chest' });

  await normalizeEnumColumn(db, 'armor', 'armor_type', armorTypes, 'cloth');
  await enforceEnumColumn(db, 'armor', 'armor_type', armorTypes, { defaultValue: 'cloth' });

  await normalizeEnumColumn(db, 'character_equipment_items', 'slot', equipmentSlots, 'weapon');
  await enforceEnumColumn(db, 'character_equipment_items', 'slot', equipmentSlots, {
    defaultValue: 'weapon',
  });

  await normalizeEnumColumn(db, 'abilities', 'ability_type', abilityTypes, 'combat');
  await enforceEnumColumn(db, 'abilities', 'ability_type', abilityTypes, { defaultValue: 'combat' });

  await normalizeEnumColumn(db, 'chat_channels', 'type', chatChannelTypes, 'global');
  await enforceEnumColumn(db, 'chat_channels', 'type', chatChannelTypes, {
    defaultValue: 'global',
  });

  await normalizeEnumColumn(db, 'realm_resource_wallets', 'resource_type', resourceIds, resourceIds[0]);
  await enforceEnumColumn(db, 'realm_resource_wallets', 'resource_type', resourceIds, {
    defaultValue: resourceIds[0],
  });

  await normalizeEnumColumn(db, 'character_resource_state', 'resource_type', classResourceTypes, 'mana');
  await enforceEnumColumn(db, 'character_resource_state', 'resource_type', classResourceTypes, {
    defaultValue: 'mana',
  });

  await normalizeEnumColumn(db, 'trades', 'status', tradeStatuses, 'pending');
  await enforceEnumColumn(db, 'trades', 'status', tradeStatuses, { defaultValue: 'pending' });

  await normalizeEnumColumn(db, 'friends', 'status', friendStatuses, 'pending');
  await enforceEnumColumn(db, 'friends', 'status', friendStatuses, { defaultValue: 'pending' });

  await normalizeEnumColumn(db, 'guild_members', 'role', guildRoles, 'member');
  await enforceEnumColumn(db, 'guild_members', 'role', guildRoles, { defaultValue: 'member' });

  await normalizeEnumColumn(db, 'party_members', 'role', partyRoles, 'member');
  await enforceEnumColumn(db, 'party_members', 'role', partyRoles, { defaultValue: 'member' });

  await normalizeEnumColumn(db, 'character_quest_states', 'status', questStatuses, 'active');
  await enforceEnumColumn(db, 'character_quest_states', 'status', questStatuses, {
    defaultValue: 'active',
  });

  await normalizeEnumColumn(db, 'character_action_requests', 'request_type', actionRequestTypes, 'progression.update');
  await enforceEnumColumn(db, 'character_action_requests', 'request_type', actionRequestTypes, {
    defaultValue: 'progression.update',
  });

  await normalizeEnumColumn(db, 'combat_event_logs', 'event_kind', combatEventKinds, combatEventKinds[0]);
  await enforceEnumColumn(db, 'combat_event_logs', 'event_kind', combatEventKinds, {
    defaultValue: combatEventKinds[0],
  });
}

export const id = '011_enum_constraints';
export const name = 'Enforce canonical enums for gameplay data';
