import { Router } from 'express';
import { getCatalogSnapshot } from '../services/catalogService';

export const catalogRouter = Router();

catalogRouter.get('/', async (_req, res, next) => {
  try {
    const snapshot = await getCatalogSnapshot();
    res.json(snapshot);
  } catch (error) {
    next(error);
  }
});
