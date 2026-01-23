import { Router } from 'express';
import { requireAuth } from '../middleware/authMiddleware';
import {
  acceptTrade,
  addTradeItemForCharacter,
  adjustCharacterCurrencies,
  createTradeRequest,
  listAvailableCurrencies,
  listCharacterBalances,
  listTrades,
  cancelTrade,
} from '../services/economyService';
import { HttpError } from '../utils/errors';

export const economyRouter = Router();

/**
 * @openapi
 * /economy/currencies:
 *   get:
 *     summary: List available currencies.
 *     tags:
 *       - Economy
 *     responses:
 *       '200':
 *         description: Currency list
 */
economyRouter.get('/currencies', requireAuth, async (_req, res, next) => {
  try {
    const currencies = await listAvailableCurrencies();
    res.json({ currencies });
  } catch (error) {
    next(error);
  }
});

/**
 * @openapi
 * /economy/currencies/{characterId}:
 *   get:
 *     summary: Get currency balances for a character.
 *     tags:
 *       - Economy
 */
economyRouter.get('/currencies/:characterId', requireAuth, async (req, res, next) => {
  try {
    const { characterId } = req.params as { characterId: string };
    const balances = await listCharacterBalances(req.user!.id, characterId);
    res.json({ balances });
  } catch (error) {
    next(error);
  }
});

/**
 * @openapi
 * /economy/currencies/{characterId}/adjust:
 *   post:
 *     summary: Adjust currency balances for a character.
 *     tags:
 *       - Economy
 */
economyRouter.post('/currencies/:characterId/adjust', requireAuth, async (req, res, next) => {
  try {
    const { characterId } = req.params as { characterId: string };
    const adjustments = Array.isArray(req.body?.adjustments) ? req.body.adjustments : null;
    if (!adjustments) {
      throw new HttpError(400, 'adjustments must be an array');
    }
    const balances = await adjustCharacterCurrencies(req.user!.id, characterId, adjustments);
    res.json({ balances });
  } catch (error) {
    next(error);
  }
});

/**
 * @openapi
 * /economy/trades:
 *   post:
 *     summary: Create a trade request.
 *     tags:
 *       - Economy
 */
economyRouter.post('/trades', requireAuth, async (req, res, next) => {
  try {
    const { initiatorCharacterId, targetCharacterId } = req.body ?? {};
    if (typeof initiatorCharacterId !== 'string' || typeof targetCharacterId !== 'string') {
      throw new HttpError(400, 'initiatorCharacterId and targetCharacterId are required');
    }
    const trade = await createTradeRequest(
      req.user!.id,
      initiatorCharacterId,
      targetCharacterId
    );
    res.status(201).json({ trade });
  } catch (error) {
    next(error);
  }
});

/**
 * @openapi
 * /economy/trades/{characterId}:
 *   get:
 *     summary: List trades for a character.
 *     tags:
 *       - Economy
 */
economyRouter.get('/trades/:characterId', requireAuth, async (req, res, next) => {
  try {
    const { characterId } = req.params as { characterId: string };
    const trades = await listTrades(req.user!.id, characterId);
    res.json({ trades });
  } catch (error) {
    next(error);
  }
});

/**
 * @openapi
 * /economy/trades/{tradeId}/items:
 *   post:
 *     summary: Add or update a trade item.
 *     tags:
 *       - Economy
 */
economyRouter.post('/trades/:tradeId/items', requireAuth, async (req, res, next) => {
  try {
    const { tradeId } = req.params as { tradeId: string };
    const { characterId, itemId, quantity, metadataJson } = req.body ?? {};
    if (typeof characterId !== 'string' || typeof itemId !== 'string') {
      throw new HttpError(400, 'characterId and itemId are required');
    }
    const item = await addTradeItemForCharacter(
      req.user!.id,
      tradeId,
      characterId,
      itemId,
      Number(quantity),
      typeof metadataJson === 'string' ? metadataJson : undefined
    );
    res.json({ item });
  } catch (error) {
    next(error);
  }
});

/**
 * @openapi
 * /economy/trades/{tradeId}/accept:
 *   post:
 *     summary: Accept a trade.
 *     tags:
 *       - Economy
 */
economyRouter.post('/trades/:tradeId/accept', requireAuth, async (req, res, next) => {
  try {
    const { tradeId } = req.params as { tradeId: string };
    const { characterId } = req.body ?? {};
    if (typeof characterId !== 'string') {
      throw new HttpError(400, 'characterId is required');
    }
    const trade = await acceptTrade(req.user!.id, tradeId, characterId);
    res.json({ trade });
  } catch (error) {
    next(error);
  }
});

/**
 * @openapi
 * /economy/trades/{tradeId}/cancel:
 *   post:
 *     summary: Cancel a trade.
 *     tags:
 *       - Economy
 */
economyRouter.post('/trades/:tradeId/cancel', requireAuth, async (req, res, next) => {
  try {
    const { tradeId } = req.params as { tradeId: string };
    const { characterId } = req.body ?? {};
    if (typeof characterId !== 'string') {
      throw new HttpError(400, 'characterId is required');
    }
    const trade = await cancelTrade(req.user!.id, tradeId, characterId);
    res.json({ trade });
  } catch (error) {
    next(error);
  }
});
