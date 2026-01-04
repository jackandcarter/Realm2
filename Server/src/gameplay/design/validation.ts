import {
  coreClassDefinitions,
  craftingRecipeDefinitions,
  equipmentArchetypes,
  equipmentCatalog,
  professionDefinitions,
  resourceDefinitions,
} from './systemFoundations';

export type ValidationLevel = 'error' | 'warning';

export interface ValidationIssue {
  level: ValidationLevel;
  message: string;
}

const buildIdSet = (ids: string[], label: string, issues: ValidationIssue[]): Set<string> => {
  const set = new Set<string>();
  for (const id of ids) {
    if (set.has(id)) {
      issues.push({ level: 'error', message: `${label} has duplicate id "${id}".` });
    }
    set.add(id);
  }
  return set;
};

export function validateSystemFoundations(): ValidationIssue[] {
  const issues: ValidationIssue[] = [];
  const classIds = buildIdSet(
    coreClassDefinitions.map((definition) => definition.id),
    'Class definitions',
    issues,
  );
  const resourceIds = buildIdSet(
    resourceDefinitions.map((definition) => definition.id),
    'Resource definitions',
    issues,
  );
  const professionIds = buildIdSet(
    professionDefinitions.map((definition) => definition.id),
    'Profession definitions',
    issues,
  );
  buildIdSet(
    equipmentArchetypes.map((definition) => definition.id),
    'Equipment archetypes',
    issues,
  );
  buildIdSet(
    equipmentCatalog.map((definition) => definition.id),
    'Equipment catalog',
    issues,
  );
  buildIdSet(
    craftingRecipeDefinitions.map((definition) => definition.id),
    'Crafting recipes',
    issues,
  );

  for (const profession of professionDefinitions) {
    for (const output of profession.outputs) {
      if (!resourceIds.has(output)) {
        issues.push({
          level: 'error',
          message: `Profession "${profession.id}" output "${output}" is not a known resource id.`,
        });
      }
    }

    if (profession.inputs) {
      for (const input of profession.inputs) {
        if (!resourceIds.has(input)) {
          issues.push({
            level: 'error',
            message: `Profession "${profession.id}" input "${input}" is not a known resource id.`,
          });
        }
      }
    }
  }

  const validateClassGate = (requiredClassIds: string[] | undefined, label: string) => {
    if (!requiredClassIds) {
      return;
    }
    for (const classId of requiredClassIds) {
      if (!classIds.has(classId)) {
        issues.push({
          level: 'error',
          message: `${label} references unknown class id "${classId}".`,
        });
      }
    }
  };

  for (const equipment of equipmentArchetypes) {
    validateClassGate(equipment.requiredClassIds, `Equipment archetype "${equipment.id}"`);
  }

  for (const equipment of equipmentCatalog) {
    validateClassGate(equipment.requiredClassIds, `Equipment catalog "${equipment.id}"`);
  }

  for (const recipe of craftingRecipeDefinitions) {
    if (!professionIds.has(recipe.professionId)) {
      issues.push({
        level: 'error',
        message: `Recipe "${recipe.id}" references unknown profession "${recipe.professionId}".`,
      });
    }

    if (!resourceIds.has(recipe.outputResourceId)) {
      issues.push({
        level: 'error',
        message: `Recipe "${recipe.id}" output "${recipe.outputResourceId}" is not a known resource id.`,
      });
    }

    for (const input of recipe.inputs) {
      if (!resourceIds.has(input.resourceId)) {
        issues.push({
          level: 'error',
          message: `Recipe "${recipe.id}" input "${input.resourceId}" is not a known resource id.`,
        });
      }
    }
  }

  return issues;
}
