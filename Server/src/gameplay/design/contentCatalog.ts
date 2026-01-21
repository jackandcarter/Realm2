import { EquipmentSlot, equipmentSlots } from './systemFoundations';

export interface ContentCatalogMeta {
  generatedAt: string;
  schemaVersion?: string;
  unityVersion?: string;
  version?: string;
}

export interface ContentCatalog {
  meta: ContentCatalogMeta;
  stats: ContentStatDefinition[];
  statCategories: ContentStatCategory[];
  statProfiles: ContentStatProfile[];
  classes: ContentClassDefinition[];
  abilities: ContentAbilityDefinition[];
  weaponTypes: ContentWeaponTypeDefinition[];
  armorTypes: ContentArmorTypeDefinition[];
  weapons: ContentWeaponDefinition[];
  armors: ContentArmorDefinition[];
}

export interface ContentStatDefinition {
  guid: string;
  displayName: string;
  description?: string;
  ratios?: ContentStatRatio[];
}

export interface ContentStatRatio {
  sourceStatGuid?: string;
  ratio: number;
}

export interface ContentStatCategory {
  guid: string;
  displayName: string;
  description?: string;
  accentColor?: ContentColor;
  statGuids?: string[];
}

export interface ContentStatProfile {
  guid: string;
  displayName: string;
  description?: string;
  statCurves?: ContentStatCurve[];
}

export interface ContentClassDefinition {
  guid: string;
  classId: string;
  displayName: string;
  description?: string;
  statProfileGuid?: string;
  statCategoryGuids?: string[];
  allowedWeaponTypeGuids?: string[];
  allowedArmorTypeGuids?: string[];
  abilityUnlocks?: ContentClassAbilityUnlock[];
  baseStatCurves?: ContentStatCurve[];
  growthModifiers?: ContentStatCurve[];
}

export interface ContentClassAbilityUnlock {
  abilityGuid?: string;
  conditionType?: string;
  requiredLevel?: number;
  questId?: string;
  itemId?: string;
  notes?: string;
}

export interface ContentStatCurve {
  statGuid?: string;
  baseCurve?: ContentCurve;
  growthCurve?: ContentCurve;
  softCapCurve?: ContentCurve;
  jitterVariance?: ContentVector2;
  formulaTemplate?: string;
  formulaCoefficients?: ContentFormulaCoefficient[];
}

export interface ContentFormulaCoefficient {
  key?: string;
  value?: number;
}

export interface ContentAbilityDefinition {
  guid: string;
  abilityName: string;
  description?: string;
}

export interface ContentWeaponTypeDefinition {
  guid: string;
  displayName: string;
  description?: string;
}

export interface ContentArmorTypeDefinition {
  guid: string;
  displayName: string;
  description?: string;
}

export interface ContentEquipmentDefinitionBase {
  guid: string;
  displayName: string;
  description?: string;
  slot: EquipmentSlot;
  requiredClassIds?: string[];
}

export interface ContentWeaponDefinition extends ContentEquipmentDefinitionBase {
  weaponTypeGuid?: string;
  baseDamage?: number;
  specialAttackGuid?: string;
}

export interface ContentArmorDefinition extends ContentEquipmentDefinitionBase {
  armorTypeGuid?: string;
}

export interface ContentCurve {
  keys?: ContentCurveKey[];
}

export interface ContentCurveKey {
  time: number;
  value: number;
  inTangent: number;
  outTangent: number;
}

export interface ContentVector2 {
  x: number;
  y: number;
}

export interface ContentColor {
  r: number;
  g: number;
  b: number;
  a: number;
}

export interface ContentCatalogIssue {
  level: 'error' | 'warning';
  message: string;
}

let activeCatalog: ContentCatalog | null = null;
let activeVersion: string | null = null;
let equipmentByGuid: Map<string, ContentEquipmentDefinitionBase> = new Map();
let classById: Map<string, ContentClassDefinition> = new Map();
let statByGuid: Map<string, ContentStatDefinition> = new Map();
let abilityByGuid: Map<string, ContentAbilityDefinition> = new Map();
let weaponTypeByGuid: Map<string, ContentWeaponTypeDefinition> = new Map();
let armorTypeByGuid: Map<string, ContentArmorTypeDefinition> = new Map();

export function applyContentCatalog(catalog: ContentCatalog, version: string): void {
  activeCatalog = catalog;
  activeVersion = version;
  const normalized = new Map<string, ContentEquipmentDefinitionBase>();
  for (const weapon of catalog.weapons ?? []) {
    if (weapon.guid) {
      normalized.set(weapon.guid.trim(), weapon);
    }
  }
  for (const armor of catalog.armors ?? []) {
    if (armor.guid) {
      normalized.set(armor.guid.trim(), armor);
    }
  }
  equipmentByGuid = normalized;
  classById = new Map(
    (catalog.classes ?? [])
      .filter((entry) => entry.classId)
      .map((entry) => [entry.classId.trim().toLowerCase(), entry]),
  );
  statByGuid = new Map(
    (catalog.stats ?? [])
      .filter((entry) => entry.guid)
      .map((entry) => [entry.guid.trim(), entry]),
  );
  abilityByGuid = new Map(
    (catalog.abilities ?? [])
      .filter((entry) => entry.guid)
      .map((entry) => [entry.guid.trim(), entry]),
  );
  weaponTypeByGuid = new Map(
    (catalog.weaponTypes ?? [])
      .filter((entry) => entry.guid)
      .map((entry) => [entry.guid.trim(), entry]),
  );
  armorTypeByGuid = new Map(
    (catalog.armorTypes ?? [])
      .filter((entry) => entry.guid)
      .map((entry) => [entry.guid.trim(), entry]),
  );
}

export function getContentCatalog(): ContentCatalog | null {
  return activeCatalog;
}

export function getContentCatalogVersion(): string | null {
  return activeVersion;
}

export function findEquipmentDefinition(
  guid: string | undefined | null,
): ContentEquipmentDefinitionBase | undefined {
  if (!guid) {
    return undefined;
  }
  return equipmentByGuid.get(guid.trim());
}

export function isValidEquipmentSlot(slot: string | undefined | null): slot is EquipmentSlot {
  if (!slot) {
    return false;
  }
  return (equipmentSlots as readonly string[]).includes(slot);
}

export function validateContentCatalog(catalog: ContentCatalog): ContentCatalogIssue[] {
  const issues: ContentCatalogIssue[] = [];

  const requireUnique = (ids: string[], label: string) => {
    const seen = new Set<string>();
    for (const id of ids) {
      const normalized = id?.trim();
      if (!normalized) {
        issues.push({ level: 'error', message: `${label} has an entry with an empty id.` });
        continue;
      }
      if (seen.has(normalized)) {
        issues.push({ level: 'error', message: `${label} has duplicate id "${normalized}".` });
      }
      seen.add(normalized);
    }
  };

  requireUnique(
    (catalog.classes ?? []).map((entry) => entry.classId),
    'Class definitions',
  );
  requireUnique(
    (catalog.stats ?? []).map((entry) => entry.guid),
    'Stat definitions',
  );
  requireUnique(
    (catalog.statProfiles ?? []).map((entry) => entry.guid),
    'Stat profiles',
  );
  requireUnique(
    (catalog.weaponTypes ?? []).map((entry) => entry.guid),
    'Weapon types',
  );
  requireUnique(
    (catalog.armorTypes ?? []).map((entry) => entry.guid),
    'Armor types',
  );
  requireUnique(
    (catalog.abilities ?? []).map((entry) => entry.guid),
    'Ability definitions',
  );
  requireUnique(
    [
      ...(catalog.weapons ?? []).map((entry) => entry.guid),
      ...(catalog.armors ?? []).map((entry) => entry.guid),
    ],
    'Equipment definitions',
  );

  const statLookup = new Set((catalog.stats ?? []).map((entry) => entry.guid));
  const abilityLookup = new Set((catalog.abilities ?? []).map((entry) => entry.guid));
  const weaponTypeLookup = new Set((catalog.weaponTypes ?? []).map((entry) => entry.guid));
  const armorTypeLookup = new Set((catalog.armorTypes ?? []).map((entry) => entry.guid));
  const classLookup = new Set((catalog.classes ?? []).map((entry) => entry.classId));

  for (const category of catalog.statCategories ?? []) {
    for (const statGuid of category.statGuids ?? []) {
      if (!statLookup.has(statGuid)) {
        issues.push({
          level: 'error',
          message: `Stat category "${category.guid}" references unknown stat "${statGuid}".`,
        });
      }
    }
  }

  const validateCurves = (curves: ContentStatCurve[] | undefined, label: string) => {
    for (const curve of curves ?? []) {
      if (curve.statGuid && !statLookup.has(curve.statGuid)) {
        issues.push({
          level: 'error',
          message: `${label} references unknown stat "${curve.statGuid}".`,
        });
      }
    }
  };

  for (const profile of catalog.statProfiles ?? []) {
    validateCurves(profile.statCurves, `Stat profile "${profile.guid}"`);
  }

  for (const classDefinition of catalog.classes ?? []) {
    if (classDefinition.statProfileGuid && !catalog.statProfiles?.some((profile) => profile.guid === classDefinition.statProfileGuid)) {
      issues.push({
        level: 'error',
        message: `Class "${classDefinition.classId}" references unknown stat profile "${classDefinition.statProfileGuid}".`,
      });
    }
    for (const ability of classDefinition.abilityUnlocks ?? []) {
      if (ability.abilityGuid && !abilityLookup.has(ability.abilityGuid)) {
        issues.push({
          level: 'error',
          message: `Class "${classDefinition.classId}" references unknown ability "${ability.abilityGuid}".`,
        });
      }
    }
    for (const typeGuid of classDefinition.allowedWeaponTypeGuids ?? []) {
      if (!weaponTypeLookup.has(typeGuid)) {
        issues.push({
          level: 'error',
          message: `Class "${classDefinition.classId}" references unknown weapon type "${typeGuid}".`,
        });
      }
    }
    for (const typeGuid of classDefinition.allowedArmorTypeGuids ?? []) {
      if (!armorTypeLookup.has(typeGuid)) {
        issues.push({
          level: 'error',
          message: `Class "${classDefinition.classId}" references unknown armor type "${typeGuid}".`,
        });
      }
    }
    for (const categoryGuid of classDefinition.statCategoryGuids ?? []) {
      if (!(catalog.statCategories ?? []).some((entry) => entry.guid === categoryGuid)) {
        issues.push({
          level: 'error',
          message: `Class "${classDefinition.classId}" references unknown stat category "${categoryGuid}".`,
        });
      }
    }
    validateCurves(classDefinition.baseStatCurves, `Class "${classDefinition.classId}" base curve`);
    validateCurves(classDefinition.growthModifiers, `Class "${classDefinition.classId}" growth curve`);
  }

  const validateEquipment = (
    entry: ContentEquipmentDefinitionBase,
    label: string,
  ) => {
    if (!isValidEquipmentSlot(entry.slot)) {
      issues.push({
        level: 'error',
        message: `${label} has invalid slot "${entry.slot}".`,
      });
    }
    for (const classId of entry.requiredClassIds ?? []) {
      if (!classLookup.has(classId)) {
        issues.push({
          level: 'error',
          message: `${label} references unknown class "${classId}".`,
        });
      }
    }
  };

  for (const weapon of catalog.weapons ?? []) {
    validateEquipment(weapon, `Weapon "${weapon.guid}"`);
    if (weapon.weaponTypeGuid && !weaponTypeLookup.has(weapon.weaponTypeGuid)) {
      issues.push({
        level: 'error',
        message: `Weapon "${weapon.guid}" references unknown weapon type "${weapon.weaponTypeGuid}".`,
      });
    }
    if (weapon.specialAttackGuid && !abilityLookup.has(weapon.specialAttackGuid)) {
      issues.push({
        level: 'error',
        message: `Weapon "${weapon.guid}" references unknown ability "${weapon.specialAttackGuid}".`,
      });
    }
  }

  for (const armor of catalog.armors ?? []) {
    validateEquipment(armor, `Armor "${armor.guid}"`);
    if (armor.armorTypeGuid && !armorTypeLookup.has(armor.armorTypeGuid)) {
      issues.push({
        level: 'error',
        message: `Armor "${armor.guid}" references unknown armor type "${armor.armorTypeGuid}".`,
      });
    }
  }

  return issues;
}

export function findClassDefinition(
  classId: string | undefined | null,
): ContentClassDefinition | undefined {
  if (!classId) {
    return undefined;
  }
  return classById.get(classId.trim().toLowerCase());
}

export function findAbilityDefinition(
  guid: string | undefined | null,
): ContentAbilityDefinition | undefined {
  if (!guid) {
    return undefined;
  }
  return abilityByGuid.get(guid.trim());
}

export function findStatDefinition(
  guid: string | undefined | null,
): ContentStatDefinition | undefined {
  if (!guid) {
    return undefined;
  }
  return statByGuid.get(guid.trim());
}

export function findWeaponTypeDefinition(
  guid: string | undefined | null,
): ContentWeaponTypeDefinition | undefined {
  if (!guid) {
    return undefined;
  }
  return weaponTypeByGuid.get(guid.trim());
}

export function findArmorTypeDefinition(
  guid: string | undefined | null,
): ContentArmorTypeDefinition | undefined {
  if (!guid) {
    return undefined;
  }
  return armorTypeByGuid.get(guid.trim());
}
