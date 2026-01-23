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

export const armorTypes = ['cloth', 'leather', 'plate'] as const;
export type ArmorType = (typeof armorTypes)[number];

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
