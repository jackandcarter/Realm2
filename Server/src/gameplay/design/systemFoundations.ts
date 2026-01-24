import {
  EquipmentSlot,
  ItemCategory,
  ItemRarity,
  ResourceCategory,
  ResourceId,
  WeaponType,
  resourceCategories,
  resourceIds,
  weaponTypes,
} from '../../config/gameEnums';

export type ElementType =
  | 'fire'
  | 'ice'
  | 'water'
  | 'wind'
  | 'lightning'
  | 'earth'
  | 'shadow'
  | 'light'
  | 'physical'
  | 'tech';

export type PartyCategory = 'solo' | 'duo' | 'party' | 'raid';

export interface FusionComponent {
  element: ElementType;
  requiresAoE?: boolean;
  allowPhysicalOverlap?: boolean;
}

export interface FusionRule {
  id: string;
  name: string;
  components: FusionComponent[];
  resultAbilityId: string;
  timingToleranceMs: number;
  minimumOverlapMs: number;
}

export interface FusionCastSnapshot {
  casterId: string;
  abilityId: string;
  element: ElementType;
  startedAtMs: number;
  releasedAtMs: number;
  isAreaOfEffect: boolean;
  includesPhysicalPayload?: boolean;
}

export interface FusionOutcome {
  success: boolean;
  ruleId?: string;
  resultAbilityId?: string;
  reason?: string;
}

export function evaluateMagicFusion(
  casts: FusionCastSnapshot[],
  rules: FusionRule[],
): FusionOutcome {
  if (casts.length < 2) {
    return { success: false, reason: 'Fusion requires at least two simultaneous casts.' };
  }

  const duration = calculateOverlapWindow(casts);
  for (const rule of rules) {
    if (!matchesElements(rule, casts)) {
      continue;
    }

    if (!meetsOverlapRules(rule, duration)) {
      continue;
    }

    if (!validatePhysicalOverlap(rule, casts)) {
      continue;
    }

    return { success: true, ruleId: rule.id, resultAbilityId: rule.resultAbilityId };
  }

  return { success: false, reason: 'No fusion rule matched the overlapping spells.' };
}

function calculateOverlapWindow(casts: FusionCastSnapshot[]): number {
  const latestStart = Math.max(...casts.map((cast) => cast.startedAtMs));
  const earliestRelease = Math.min(...casts.map((cast) => cast.releasedAtMs));
  return Math.max(0, earliestRelease - latestStart);
}

function matchesElements(rule: FusionRule, casts: FusionCastSnapshot[]): boolean {
  if (rule.components.length !== casts.length) {
    return false;
  }

  const remainingCasts = [...casts];
  for (const component of rule.components) {
    const index = remainingCasts.findIndex((cast) => cast.element === component.element);
    if (index === -1) {
      return false;
    }

    const [matched] = remainingCasts.splice(index, 1);
    if (!matched) {
      return false;
    }
    if (component.requiresAoE && !matched.isAreaOfEffect) {
      return false;
    }
  }

  return true;
}

function meetsOverlapRules(rule: FusionRule, overlapDurationMs: number): boolean {
  if (overlapDurationMs <= 0) {
    return false;
  }

  const withinTolerance = Math.abs(overlapDurationMs - rule.minimumOverlapMs) <= rule.timingToleranceMs;
  return overlapDurationMs >= rule.minimumOverlapMs && withinTolerance;
}

function validatePhysicalOverlap(rule: FusionRule, casts: FusionCastSnapshot[]): boolean {
  const requiresPhysical = rule.components.some((component) => component.allowPhysicalOverlap);
  if (!requiresPhysical) {
    return true;
  }

  return casts.some((cast) => cast.includesPhysicalPayload === true);
}

export interface PartyMemberProfile {
  characterId: string;
  level: number;
  classId: string;
}

export interface PartySnapshot {
  members: PartyMemberProfile[];
  maxSize: number;
}

export interface PartyResolution {
  category: PartyCategory;
  levelSyncTarget?: number;
}

export function resolvePartyCategory(snapshot: PartySnapshot): PartyResolution {
  const memberCount = snapshot.members.length;
  let category: PartyCategory = 'solo';
  if (memberCount === 2) {
    category = 'duo';
  } else if (memberCount >= 3 && memberCount <= 6) {
    category = 'party';
  } else if (memberCount > 6) {
    category = 'raid';
  }

  const highestLevel = Math.max(...snapshot.members.map((member) => member.level));
  const levelSyncTarget = category === 'party' && memberCount < 4 ? highestLevel : undefined;

  return { category, levelSyncTarget };
}

export interface BossScalingInput {
  baseLevel: number;
  participantLevels: number[];
  category: PartyCategory;
}

export function scaleBossLevel(input: BossScalingInput): number {
  const highestLevel = Math.max(...input.participantLevels);
  const raidModifier = input.category === 'raid' ? 1.1 : 1;
  const smallPartyBonus = input.category === 'party' && input.participantLevels.length < 4 ? 0.95 : 1;
  const scaled = input.baseLevel * raidModifier * smallPartyBonus;
  return Math.max(input.baseLevel, Math.round(Math.max(scaled, highestLevel)));
}

export interface ClassDefinition {
  id: string;
  name: string;
  role: 'tank' | 'damage' | 'support' | 'builder';
  primaryStats: string[];
  weaponProficiencies: WeaponType[];
  signatureAbilities: string[];
  unlockQuestId?: string;
}

export const coreClassDefinitions: ClassDefinition[] = [
  {
    id: 'warrior',
    name: 'Warrior',
    role: 'tank',
    primaryStats: ['stat.strength', 'stat.vitality'],
    weaponProficiencies: ['greatsword', 'double-saber', 'shield'],
    signatureAbilities: ['ability.powerStrike'],
  },
  {
    id: 'wizard',
    name: 'Wizard',
    role: 'damage',
    primaryStats: ['stat.magic', 'stat.spirit'],
    weaponProficiencies: ['staff', 'book'],
    signatureAbilities: ['ability.spiritBlessing'],
  },
  {
    id: 'time-mage',
    name: 'Time Mage',
    role: 'support',
    primaryStats: ['stat.magic', 'stat.spirit'],
    weaponProficiencies: ['staff', 'book'],
    signatureAbilities: ['ability.spiritBlessing'],
    unlockQuestId: 'quest-time-mage-convergence',
  },
  {
    id: 'necromancer',
    name: 'Necromancer',
    role: 'damage',
    primaryStats: ['stat.strength', 'stat.magic'],
    weaponProficiencies: ['scythe', 'staff', 'sword'],
    signatureAbilities: ['ability.necromancer_reaper_combo', 'ability.necromancer_soul_bolt'],
  },
  {
    id: 'technomancer',
    name: 'Technomancer',
    role: 'support',
    primaryStats: ['stat.magic', 'stat.spirit'],
    weaponProficiencies: ['mech-rod', 'pistol'],
    signatureAbilities: [],
  },
  {
    id: 'sage',
    name: 'Sage',
    role: 'support',
    primaryStats: ['stat.magic', 'stat.spirit'],
    weaponProficiencies: ['staff', 'book'],
    signatureAbilities: ['ability.spiritBlessing'],
  },
  {
    id: 'rogue',
    name: 'Rogue',
    role: 'damage',
    primaryStats: ['stat.agility', 'stat.attackPower'],
    weaponProficiencies: ['dagger', 'dual-blades'],
    signatureAbilities: [],
  },
  {
    id: 'ranger',
    name: 'Ranger',
    role: 'damage',
    primaryStats: ['stat.agility', 'stat.attackPower'],
    weaponProficiencies: ['bow', 'boomerang'],
    signatureAbilities: [],
    unlockQuestId: 'quest-ranger-trial',
  },
  {
    id: 'mythologist',
    name: 'Mythologist',
    role: 'support',
    primaryStats: ['stat.magic', 'stat.spirit'],
    weaponProficiencies: ['book', 'staff'],
    signatureAbilities: [],
  },
  {
    id: 'builder',
    name: 'Builder',
    role: 'builder',
    primaryStats: ['stat.spirit'],
    weaponProficiencies: ['toolkit'],
    signatureAbilities: [],
    unlockQuestId: 'quest-builder-arkitect',
  },
];

function assertWeaponProficienciesValid(): void {
  const allowed = new Set(weaponTypes);
  for (const definition of coreClassDefinitions) {
    for (const weapon of definition.weaponProficiencies) {
      if (!allowed.has(weapon)) {
        throw new Error(`Invalid weapon proficiency "${weapon}" for class ${definition.id}`);
      }
    }
  }
}

assertWeaponProficienciesValid();

export interface ProfessionDefinition {
  id: string;
  name: string;
  outputs: string[];
  inputs?: string[];
  unlocks?: string[];
}

export const professionDefinitions: ProfessionDefinition[] = [
  { id: 'farmer', name: 'Farmer', outputs: ['resource.crops', 'resource.herbs'] },
  { id: 'gatherer', name: 'Gatherer', outputs: ['resource.ore', 'resource.wood', 'resource.rare-essence'] },
  { id: 'blacksmith', name: 'Blacksmith', outputs: ['resource.iron-plate'], inputs: ['resource.ore'] },
  { id: 'tailor', name: 'Tailor', outputs: ['resource.cloth'], inputs: ['resource.cloth', 'resource.leather'] },
  { id: 'carpenter', name: 'Carpenter', outputs: ['resource.wood-beam-small', 'resource.wood-beam-medium'], inputs: ['resource.wood'] },
  { id: 'painter', name: 'Painter', outputs: ['resource.pigment'], inputs: ['resource.pigment'] },
  { id: 'mechanic', name: 'Mechanic', outputs: ['resource.tech-shards'], inputs: ['resource.ore', 'resource.tech-shards'] },
];


export interface EquipmentDefinition {
  id: string;
  name: string;
  slot: EquipmentSlot;
  category: ItemCategory;
  subtype?: string;
  requiredClassIds?: string[];
}

export const equipmentArchetypes: EquipmentDefinition[] = [
  { id: 'weapon.greatsword', name: 'Greatsword', slot: 'weapon', category: 'weapon', subtype: 'greatsword', requiredClassIds: ['warrior'] },
  { id: 'weapon.staff', name: 'Arcane Staff', slot: 'weapon', category: 'weapon', subtype: 'staff', requiredClassIds: ['wizard', 'time-mage', 'sage'] },
  { id: 'weapon.boomerang', name: 'Boomerang', slot: 'weapon', category: 'weapon', subtype: 'boomerang', requiredClassIds: ['ranger'] },
  { id: 'armor.robe', name: 'Mystic Robe', slot: 'chest', category: 'armor', subtype: 'cloth' },
  { id: 'consumable.potion', name: 'Restorative Potion', slot: 'tool', category: 'consumable', subtype: 'healing' },
  { id: 'key.chrono-shard', name: 'Chrono Nexus Shard', slot: 'accessory', category: 'key-item', subtype: 'artifact' },
];

export interface ResourceDefinition {
  id: ResourceId;
  name: string;
  category: ResourceCategory;
}

export const resourceDefinitions: ResourceDefinition[] = [
  { id: 'resource.wood', name: 'Timber', category: 'raw' },
  { id: 'resource.ore', name: 'Iron Ore', category: 'raw' },
  { id: 'resource.rare-essence', name: 'Rare Essence', category: 'raw' },
  { id: 'resource.herbs', name: 'Herbs', category: 'raw' },
  { id: 'resource.crops', name: 'Crops', category: 'raw' },
  { id: 'resource.cloth', name: 'Woven Cloth', category: 'processed' },
  { id: 'resource.leather', name: 'Tanned Leather', category: 'processed' },
  { id: 'resource.pigment', name: 'Pigment', category: 'processed' },
  { id: 'resource.tech-shards', name: 'Tech Shards', category: 'raw' },
  { id: 'resource.wood-beam-small', name: 'Small Wood Beam', category: 'crafted' },
  { id: 'resource.wood-beam-medium', name: 'Medium Wood Beam', category: 'crafted' },
  { id: 'resource.wood-beam-large', name: 'Large Wood Beam', category: 'crafted' },
  { id: 'resource.iron-plate', name: 'Iron Plate', category: 'crafted' },
  { id: 'resource.mana-resin', name: 'Mana Resin', category: 'crafted' },
  { id: 'resource.health-tonic', name: 'Health Tonic', category: 'consumable' },
  { id: 'resource.stamina-tonic', name: 'Stamina Tonic', category: 'consumable' },
  { id: 'resource.quest-chrono-shard', name: 'Chrono Shard Fragment', category: 'quest' },
];

function assertResourceDefinitionsValid(): void {
  const allowedIds = new Set(resourceIds);
  const allowedCategories = new Set(resourceCategories);
  const seen = new Set<string>();

  for (const definition of resourceDefinitions) {
    if (!allowedIds.has(definition.id)) {
      throw new Error(`Invalid resource id "${definition.id}"`);
    }
    if (!allowedCategories.has(definition.category)) {
      throw new Error(`Invalid resource category "${definition.category}" for ${definition.id}`);
    }
    if (seen.has(definition.id)) {
      throw new Error(`Duplicate resource definition "${definition.id}"`);
    }
    seen.add(definition.id);
  }

  for (const id of allowedIds) {
    if (!seen.has(id)) {
      throw new Error(`Missing resource definition for "${id}"`);
    }
  }
}

assertResourceDefinitionsValid();

export interface EquipmentCatalogEntry extends EquipmentDefinition {
  tier: ItemRarity;
  baseStats: Record<string, number>;
}

export const equipmentCatalog: EquipmentCatalogEntry[] = [
  {
    id: 'weapon.greatsword.starter',
    name: 'Ironclad Greatsword',
    slot: 'weapon',
    category: 'weapon',
    subtype: 'greatsword',
    requiredClassIds: ['warrior'],
    tier: 'starter',
    baseStats: { 'stat.attackPower': 12, 'stat.vitality': 2 },
  },
  {
    id: 'weapon.staff.initiate',
    name: 'Initiate Staff',
    slot: 'weapon',
    category: 'weapon',
    subtype: 'staff',
    requiredClassIds: ['wizard', 'time-mage', 'sage'],
    tier: 'starter',
    baseStats: { 'stat.magic': 10, 'stat.spirit': 4 },
  },
  {
    id: 'weapon.dagger.shadow',
    name: 'Shadow Dagger',
    slot: 'weapon',
    category: 'weapon',
    subtype: 'dagger',
    requiredClassIds: ['rogue'],
    tier: 'standard',
    baseStats: { 'stat.attackPower': 8, 'stat.agility': 6 },
  },
  {
    id: 'weapon.bow.ranger',
    name: 'Ranger Longbow',
    slot: 'weapon',
    category: 'weapon',
    subtype: 'bow',
    requiredClassIds: ['ranger'],
    tier: 'standard',
    baseStats: { 'stat.attackPower': 9, 'stat.agility': 5 },
  },
  {
    id: 'weapon.scythe.reaper',
    name: 'Reaper Scythe',
    slot: 'weapon',
    category: 'weapon',
    subtype: 'scythe',
    requiredClassIds: ['necromancer'],
    tier: 'rare',
    baseStats: { 'stat.attackPower': 11, 'stat.magic': 5 },
  },
  {
    id: 'weapon.pistol.tech',
    name: 'Tech Pistol',
    slot: 'weapon',
    category: 'weapon',
    subtype: 'pistol',
    requiredClassIds: ['technomancer'],
    tier: 'standard',
    baseStats: { 'stat.attackPower': 7, 'stat.spirit': 3 },
  },
  {
    id: 'weapon.toolkit.builder',
    name: 'Builder Toolkit',
    slot: 'weapon',
    category: 'weapon',
    subtype: 'toolkit',
    requiredClassIds: ['builder'],
    tier: 'starter',
    baseStats: { 'stat.spirit': 6, 'stat.vitality': 2 },
  },
  {
    id: 'armor.plate.vanguard',
    name: 'Vanguard Plate',
    slot: 'chest',
    category: 'armor',
    subtype: 'plate',
    requiredClassIds: ['warrior'],
    tier: 'standard',
    baseStats: { 'stat.vitality': 8, 'stat.strength': 3 },
  },
  {
    id: 'armor.leather.stalker',
    name: 'Stalker Leathers',
    slot: 'chest',
    category: 'armor',
    subtype: 'leather',
    requiredClassIds: ['rogue', 'ranger'],
    tier: 'standard',
    baseStats: { 'stat.agility': 6, 'stat.attackPower': 2 },
  },
  {
    id: 'armor.robe.arcane',
    name: 'Arcane Robe',
    slot: 'chest',
    category: 'armor',
    subtype: 'cloth',
    requiredClassIds: ['wizard', 'time-mage', 'sage', 'mythologist'],
    tier: 'standard',
    baseStats: { 'stat.magic': 6, 'stat.spirit': 4 },
  },
  {
    id: 'consumable.health-tonic',
    name: 'Health Tonic',
    slot: 'tool',
    category: 'consumable',
    subtype: 'healing',
    tier: 'starter',
    baseStats: { 'stat.healFlat': 120 },
  },
  {
    id: 'consumable.stamina-tonic',
    name: 'Stamina Tonic',
    slot: 'tool',
    category: 'consumable',
    subtype: 'stamina',
    tier: 'starter',
    baseStats: { 'stat.staminaRestore': 80 },
  },
  {
    id: 'key.chrono-shard',
    name: 'Chrono Nexus Shard',
    slot: 'accessory',
    category: 'key-item',
    subtype: 'artifact',
    tier: 'legendary',
    baseStats: {},
  },
];

export interface CraftingRecipeInput {
  resourceId: string;
  quantity: number;
}

export interface CraftingRecipeDefinition {
  id: string;
  name: string;
  professionId: string;
  inputs: CraftingRecipeInput[];
  outputResourceId: string;
  outputQuantity: number;
}

export const craftingRecipeDefinitions: CraftingRecipeDefinition[] = [
  {
    id: 'recipe.wood-beam-small',
    name: 'Small Wood Beam',
    professionId: 'carpenter',
    inputs: [
      { resourceId: 'resource.wood', quantity: 6 },
      { resourceId: 'resource.mana-resin', quantity: 1 },
    ],
    outputResourceId: 'resource.wood-beam-small',
    outputQuantity: 2,
  },
  {
    id: 'recipe.wood-beam-medium',
    name: 'Medium Wood Beam',
    professionId: 'carpenter',
    inputs: [
      { resourceId: 'resource.wood', quantity: 10 },
      { resourceId: 'resource.mana-resin', quantity: 2 },
    ],
    outputResourceId: 'resource.wood-beam-medium',
    outputQuantity: 1,
  },
  {
    id: 'recipe.wood-beam-large',
    name: 'Large Wood Beam',
    professionId: 'carpenter',
    inputs: [
      { resourceId: 'resource.wood', quantity: 16 },
      { resourceId: 'resource.mana-resin', quantity: 3 },
    ],
    outputResourceId: 'resource.wood-beam-large',
    outputQuantity: 1,
  },
  {
    id: 'recipe.iron-plate',
    name: 'Iron Plate',
    professionId: 'blacksmith',
    inputs: [
      { resourceId: 'resource.ore', quantity: 8 },
      { resourceId: 'resource.mana-resin', quantity: 1 },
    ],
    outputResourceId: 'resource.iron-plate',
    outputQuantity: 2,
  },
  {
    id: 'recipe.health-tonic',
    name: 'Health Tonic',
    professionId: 'farmer',
    inputs: [
      { resourceId: 'resource.herbs', quantity: 4 },
      { resourceId: 'resource.crops', quantity: 2 },
    ],
    outputResourceId: 'resource.health-tonic',
    outputQuantity: 1,
  },
  {
    id: 'recipe.stamina-tonic',
    name: 'Stamina Tonic',
    professionId: 'farmer',
    inputs: [
      { resourceId: 'resource.herbs', quantity: 3 },
      { resourceId: 'resource.crops', quantity: 3 },
    ],
    outputResourceId: 'resource.stamina-tonic',
    outputQuantity: 1,
  },
];

export interface LocationDefinition {
  id: string;
  name: string;
  biome: string;
  role: string;
}

export const keyLocations: LocationDefinition[] = [
  { id: 'eldoria', name: 'Eldoria', biome: 'capital', role: 'trade and governance hub' },
  { id: 'arcane-haven', name: 'Arcane Haven', biome: 'enchanted forest', role: 'scholar city and ranger refuge' },
  { id: 'nexus-outpost', name: 'Nexus Outpost', biome: 'futuristic stronghold', role: 'Shadow Enclave base around the Chrono Nexus' },
  { id: 'gearspring', name: 'Gearspring', biome: 'industrial city', role: 'technomancer research center' },
  { id: 'drakoria', name: 'Drakoria', biome: 'volcanic range', role: 'dragonkin and Crystallian fortress' },
  { id: 'netheris', name: 'Netheris', biome: 'shadowlands', role: 'Revenant enclave and necromancy seat' },
  { id: 'luminara', name: 'Luminara', biome: 'floating isles', role: 'mythic convergence and endgame story node' },
];

export interface SettlementTierDefinition {
  id: string;
  name: string;
  unlockRequirements: string[];
  facilities: string[];
}

export const settlementTiers: SettlementTierDefinition[] = [
  {
    id: 'village',
    name: 'Village',
    unlockRequirements: ['starter-blueprints', 'basic-materials'],
    facilities: ['farm', 'market-stall', 'workshop'],
  },
  {
    id: 'town',
    name: 'Town',
    unlockRequirements: ['village', 'mid-tier-blueprints', 'embassy-request'],
    facilities: ['arcane-lab', 'blacksmith', 'commission-board'],
  },
  {
    id: 'kingdom',
    name: 'Kingdom',
    unlockRequirements: ['town', 'royal-charter', 'player-governance'],
    facilities: ['grand-market', 'embassies', 'arkitect-hall'],
  },
];

export interface StoryArcDefinition {
  id: string;
  title: string;
  synopsis: string;
  keyCharacters: string[];
  branchingChoices?: string[];
}

export const storyArcs: StoryArcDefinition[] = [
  {
    id: 'chrono-nexus',
    title: 'Chrono Nexus Conflict',
    synopsis: 'Shadow Enclave seizes the Chrono Nexus; players align with allies to reclaim it.',
    keyCharacters: ['Aria Shadowheart', 'Varian Stormforge', 'Seraphina Frostwind', 'Xander Ironspark'],
    branchingChoices: ['side-with-royal-guard', 'ally-with-shadow-enclave', 'protect-felarian-forest'],
  },
  {
    id: 'forgotten-relics',
    title: 'Forgotten Relics',
    synopsis: 'Recover artifacts that alter the main quest and unlock unique abilities.',
    keyCharacters: ['Professor Seraphina Frostwind'],
  },
  {
    id: 'cursed-library',
    title: 'The Cursed Library',
    synopsis: 'Aid Arcane Haven in lifting an ancient curse to access forbidden knowledge.',
    keyCharacters: ['Arcane Haven Librarians'],
  },
  {
    id: 'tech-hunt',
    title: 'Tech Hunt',
    synopsis: 'Help Xander Ironspark retrieve lost technology for gadget upgrades.',
    keyCharacters: ['Xander Ironspark'],
  },
  {
    id: 'spirit-of-the-forest',
    title: 'Spirit of the Forest',
    synopsis: 'Broker peace between Felarians and encroaching settlers by aiding the forest spirit.',
    keyCharacters: ['Felarian Rangers'],
  },
  {
    id: 'time-anomalies',
    title: 'Time-Traveling Anomalies',
    synopsis: 'Seal random temporal rifts that reshape zones and rewards.',
    keyCharacters: ['Aria Shadowheart', 'Seraphina Frostwind'],
  },
];
