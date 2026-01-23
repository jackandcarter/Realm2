import express, { Request, Response, NextFunction } from 'express';
import helmet from 'helmet';
import morgan from 'morgan';
import swaggerUi from 'swagger-ui-express';
import { authRouter } from './routes/authRoutes';
import { realmRouter } from './routes/realmRoutes';
import { characterRouter } from './routes/characterRoutes';
import { realmChunkRouter } from './routes/chunkRoutes';
import { combatRouter } from './routes/combatRoutes';
import { catalogRouter } from './routes/catalogRoutes';
import { economyRouter } from './routes/economyRoutes';
import { socialRouter } from './routes/socialRoutes';
import { swaggerSpec } from './docs/swagger';
import { HttpError, isHttpError } from './utils/errors';
import { metricsRegistry } from './observability/metrics';
import { logger } from './observability/logger';

const app = express();

app.use(helmet());
app.use(express.json());
if (process.env.NODE_ENV !== 'test') {
  app.use(morgan('dev'));
}

app.use('/auth', authRouter);
app.use('/realms', realmRouter);
app.use('/realms', realmChunkRouter);
app.use('/characters', characterRouter);
app.use('/combat', combatRouter);
app.use('/catalog', catalogRouter);
app.use('/economy', economyRouter);
app.use('/social', socialRouter);
app.use('/docs', swaggerUi.serve, swaggerUi.setup(swaggerSpec));

app.get('/health', (_req, res) => {
  res.json({ status: 'ok' });
});

app.get('/metrics', async (_req, res, next) => {
  try {
    res.set('Content-Type', metricsRegistry.contentType);
    res.send(await metricsRegistry.metrics());
  } catch (error) {
    next(error);
  }
});

app.use((err: unknown, _req: Request, res: Response, _next: NextFunction) => {
  if (isHttpError(err)) {
    res.status((err as HttpError).status).json({ message: (err as HttpError).message });
    return;
  }
  if (process.env.NODE_ENV !== 'test') {
    logger.error({ err }, 'Unhandled application error');
  }
  res.status(500).json({ message: 'Internal server error' });
});

export { app };
