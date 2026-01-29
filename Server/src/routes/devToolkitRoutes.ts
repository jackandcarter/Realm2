import { Router } from 'express';
import {
  listDevToolkitAbilities,
  listDevToolkitAbilityTypes,
  listDevToolkitArmor,
  listDevToolkitEnemies,
  listDevToolkitEnemyBaseStats,
  listDevToolkitClassBaseStats,
  listDevToolkitClasses,
  listDevToolkitItems,
  listDevToolkitLevelProgression,
  listDevToolkitRaces,
  listDevToolkitResourceTypes,
  listDevToolkitWeaponTypes,
  listDevToolkitWeapons,
  saveAbility,
  saveAbilityType,
  saveArmor,
  saveEnemy,
  saveEnemyBaseStats,
  saveClass,
  saveClassBaseStats,
  saveItem,
  saveLevelProgression,
  saveRace,
  saveResourceType,
  saveWeapon,
  saveWeaponType,
} from '../services/devToolkitService';

export const devToolkitRouter = Router();

devToolkitRouter.get('/items', async (_req, res, next) => {
  try {
    res.json({ items: await listDevToolkitItems() });
  } catch (error) {
    next(error);
  }
});

devToolkitRouter.get('/races', async (_req, res, next) => {
  try {
    res.json({ races: await listDevToolkitRaces() });
  } catch (error) {
    next(error);
  }
});

devToolkitRouter.get('/weapons', async (_req, res, next) => {
  try {
    res.json({ weapons: await listDevToolkitWeapons() });
  } catch (error) {
    next(error);
  }
});

devToolkitRouter.get('/enemies', async (_req, res, next) => {
  try {
    res.json({ enemies: await listDevToolkitEnemies() });
  } catch (error) {
    next(error);
  }
});

devToolkitRouter.get('/enemy-base-stats', async (_req, res, next) => {
  try {
    res.json({ enemyBaseStats: await listDevToolkitEnemyBaseStats() });
  } catch (error) {
    next(error);
  }
});

devToolkitRouter.get('/armor', async (_req, res, next) => {
  try {
    res.json({ armor: await listDevToolkitArmor() });
  } catch (error) {
    next(error);
  }
});

devToolkitRouter.get('/level-progression', async (_req, res, next) => {
  try {
    res.json({ levelProgression: await listDevToolkitLevelProgression() });
  } catch (error) {
    next(error);
  }
});

devToolkitRouter.get('/classes', async (_req, res, next) => {
  try {
    res.json({ classes: await listDevToolkitClasses() });
  } catch (error) {
    next(error);
  }
});

devToolkitRouter.get('/class-base-stats', async (_req, res, next) => {
  try {
    res.json({ classBaseStats: await listDevToolkitClassBaseStats() });
  } catch (error) {
    next(error);
  }
});

devToolkitRouter.get('/abilities', async (_req, res, next) => {
  try {
    res.json({ abilities: await listDevToolkitAbilities() });
  } catch (error) {
    next(error);
  }
});

devToolkitRouter.get('/weapon-types', async (_req, res, next) => {
  try {
    res.json({ weaponTypes: await listDevToolkitWeaponTypes() });
  } catch (error) {
    next(error);
  }
});

devToolkitRouter.get('/ability-types', async (_req, res, next) => {
  try {
    res.json({ abilityTypes: await listDevToolkitAbilityTypes() });
  } catch (error) {
    next(error);
  }
});

devToolkitRouter.get('/resource-types', async (_req, res, next) => {
  try {
    res.json({ resourceTypes: await listDevToolkitResourceTypes() });
  } catch (error) {
    next(error);
  }
});

devToolkitRouter.post('/items', async (req, res, next) => {
  try {
    const item = await saveItem(req.body);
    res.json({ item });
  } catch (error) {
    next(error);
  }
});

devToolkitRouter.post('/races', async (req, res, next) => {
  try {
    const race = await saveRace(req.body);
    res.json({ race });
  } catch (error) {
    next(error);
  }
});

devToolkitRouter.post('/weapon-types', async (req, res, next) => {
  try {
    const weaponType = await saveWeaponType(req.body);
    res.json({ weaponType });
  } catch (error) {
    next(error);
  }
});

devToolkitRouter.post('/ability-types', async (req, res, next) => {
  try {
    const abilityType = await saveAbilityType(req.body);
    res.json({ abilityType });
  } catch (error) {
    next(error);
  }
});

devToolkitRouter.post('/resource-types', async (req, res, next) => {
  try {
    const resourceType = await saveResourceType(req.body);
    res.json({ resourceType });
  } catch (error) {
    next(error);
  }
});

devToolkitRouter.post('/weapons', async (req, res, next) => {
  try {
    const weapon = await saveWeapon(req.body);
    res.json({ weapon });
  } catch (error) {
    next(error);
  }
});

devToolkitRouter.post('/enemies', async (req, res, next) => {
  try {
    const enemy = await saveEnemy(req.body);
    res.json({ enemy });
  } catch (error) {
    next(error);
  }
});

devToolkitRouter.post('/enemy-base-stats', async (req, res, next) => {
  try {
    const enemyBaseStats = await saveEnemyBaseStats(req.body);
    res.json({ enemyBaseStats });
  } catch (error) {
    next(error);
  }
});

devToolkitRouter.post('/armor', async (req, res, next) => {
  try {
    const armor = await saveArmor(req.body);
    res.json({ armor });
  } catch (error) {
    next(error);
  }
});

devToolkitRouter.post('/level-progression', async (req, res, next) => {
  try {
    const levelProgression = await saveLevelProgression(req.body);
    res.json({ levelProgression });
  } catch (error) {
    next(error);
  }
});

devToolkitRouter.post('/classes', async (req, res, next) => {
  try {
    const classRecord = await saveClass(req.body);
    res.json({ class: classRecord });
  } catch (error) {
    next(error);
  }
});

devToolkitRouter.post('/class-base-stats', async (req, res, next) => {
  try {
    const classBaseStats = await saveClassBaseStats(req.body);
    res.json({ classBaseStats });
  } catch (error) {
    next(error);
  }
});

devToolkitRouter.post('/abilities', async (req, res, next) => {
  try {
    const ability = await saveAbility(req.body);
    res.json({ ability });
  } catch (error) {
    next(error);
  }
});
