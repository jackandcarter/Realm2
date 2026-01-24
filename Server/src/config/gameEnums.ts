export const raceIds = ['human', 'felarian', 'crystallian', 'revenant', 'gearling'] as const;
export type RaceId = (typeof raceIds)[number];

export const classRoles = ['tank', 'damage', 'support', 'builder'] as const;
export type ClassRole = (typeof classRoles)[number];

export const itemCategories = ['weapon', 'armor', 'consumable', 'key-item'] as const;
export type ItemCategory = (typeof itemCategories)[number];

export const itemRarities = ['common', 'starter', 'standard', 'rare', 'legendary'] as const;
export type ItemRarity = (typeof itemRarities)[number];

export const equipmentSlots = [
  'weapon',
  'head',
  'chest',
  'legs',
  'hands',
  'feet',
  'accessory',
  'tool',
] as const;
export type EquipmentSlot = (typeof equipmentSlots)[number];

export const weaponTypes = [
  'greatsword',
  'double-saber',
  'shield',
  'staff',
  'book',
  'scythe',
  'sword',
  'mech-rod',
  'pistol',
  'dagger',
  'dual-blades',
  'bow',
  'toolkit',
  'boomerang',
] as const;
export type WeaponType = (typeof weaponTypes)[number];

export const weaponHandedness = ['one-hand', 'two-hand', 'off-hand'] as const;
export type WeaponHandedness = (typeof weaponHandedness)[number];

export const armorTypes = ['cloth', 'leather', 'plate'] as const;
export type ArmorType = (typeof armorTypes)[number];

export const classResourceTypes = ['mana', 'stamina', 'energy'] as const;
export type ClassResourceType = (typeof classResourceTypes)[number];

export const abilityTypes = ['combat'] as const;
export type AbilityType = (typeof abilityTypes)[number];

export const tradeStatuses = ['pending', 'accepted', 'cancelled', 'completed'] as const;
export type TradeStatus = (typeof tradeStatuses)[number];

export const friendStatuses = ['pending', 'accepted', 'blocked'] as const;
export type FriendStatus = (typeof friendStatuses)[number];

export const guildRoles = ['leader', 'officer', 'member'] as const;
export type GuildRole = (typeof guildRoles)[number];

export const partyRoles = ['leader', 'member'] as const;
export type PartyRole = (typeof partyRoles)[number];

export const questStatuses = ['active', 'completed', 'failed'] as const;
export type QuestStatus = (typeof questStatuses)[number];

export const actionRequestTypes = ['progression.update', 'quest.complete'] as const;
export type ActionRequestType = (typeof actionRequestTypes)[number];

export const actionRequestStatuses = ['pending', 'processing', 'completed', 'rejected'] as const;
export type ActionRequestStatus = (typeof actionRequestStatuses)[number];

export const chatChannelTypes = ['global', 'party', 'guild', 'direct', 'system'] as const;
export type ChatChannelType = (typeof chatChannelTypes)[number];

export const combatEventKinds = ['damage', 'heal', 'stateApplied'] as const;
export type CombatEventKind = (typeof combatEventKinds)[number];

export const resourceCategories = ['raw', 'processed', 'crafted', 'consumable', 'quest'] as const;
export type ResourceCategory = (typeof resourceCategories)[number];

export const resourceIds = [
  'resource.wood',
  'resource.ore',
  'resource.rare-essence',
  'resource.herbs',
  'resource.crops',
  'resource.cloth',
  'resource.leather',
  'resource.pigment',
  'resource.tech-shards',
  'resource.wood-beam-small',
  'resource.wood-beam-medium',
  'resource.wood-beam-large',
  'resource.iron-plate',
  'resource.mana-resin',
  'resource.health-tonic',
  'resource.stamina-tonic',
  'resource.quest-chrono-shard',
] as const;
export type ResourceId = (typeof resourceIds)[number];
