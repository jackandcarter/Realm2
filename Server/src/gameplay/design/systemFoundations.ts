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
  weaponProficiencies: string[];
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
    primaryStats: ['stat.magic', 'stat.spirit'],
    weaponProficiencies: ['scythe', 'staff'],
    signatureAbilities: [],
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

export interface ProfessionDefinition {
  id: string;
  name: string;
  outputs: string[];
  inputs?: string[];
  unlocks?: string[];
}

export const professionDefinitions: ProfessionDefinition[] = [
  { id: 'farmer', name: 'Farmer', outputs: ['crops', 'herbs'] },
  { id: 'gatherer', name: 'Gatherer', outputs: ['ore', 'wood', 'rare-essence'] },
  { id: 'blacksmith', name: 'Blacksmith', outputs: ['weapons', 'armor-plates'], inputs: ['ore'] },
  { id: 'tailor', name: 'Tailor', outputs: ['cloth-armor'], inputs: ['cloth'] },
  { id: 'carpenter', name: 'Carpenter', outputs: ['wooden-structures'], inputs: ['wood'] },
  { id: 'painter', name: 'Painter', outputs: ['decor'], inputs: ['pigment'] },
  { id: 'mechanic', name: 'Mechanic', outputs: ['gadgets'], inputs: ['ore', 'tech-shards'] },
];

export const equipmentSlots = ['head', 'chest', 'legs', 'hands', 'feet', 'accessory', 'tool'] as const;

export type EquipmentSlot = (typeof equipmentSlots)[number];

export interface EquipmentDefinition {
  id: string;
  name: string;
  slot: EquipmentSlot;
  category: 'weapon' | 'armor' | 'consumable' | 'key-item';
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
