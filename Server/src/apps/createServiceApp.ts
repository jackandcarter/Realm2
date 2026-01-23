import express, { Request, Response, NextFunction, Router } from 'express';
import helmet from 'helmet';
import morgan from 'morgan';
import swaggerUi from 'swagger-ui-express';
import { swaggerSpec } from '../docs/swagger';
import { HttpError, isHttpError } from '../utils/errors';
import { metricsRegistry } from '../observability/metrics';
import { logger } from '../observability/logger';

export interface ServiceRouteDefinition {
  path: string;
  router: Router;
}

export interface ServiceAppOptions {
  serviceName: string;
  routes: ServiceRouteDefinition[];
  enableDocs?: boolean;
}

export function createServiceApp(options: ServiceAppOptions): express.Express {
  const app = express();

  app.use(helmet());
  app.use(express.json());
  if (process.env.NODE_ENV !== 'test') {
    app.use(morgan('dev'));
  }

  for (const route of options.routes) {
    app.use(route.path, route.router);
  }

  if (options.enableDocs) {
    app.use('/docs', swaggerUi.serve, swaggerUi.setup(swaggerSpec));
  }

  app.get('/health', (_req, res) => {
    res.json({ status: 'ok', service: options.serviceName });
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
      const httpError = err as HttpError;
      res.status(httpError.status).json({
        message: httpError.message,
        ...(httpError.details ? { details: httpError.details } : {}),
      });
      return;
    }
    if (process.env.NODE_ENV !== 'test') {
      logger.error({ err }, 'Unhandled application error');
    }
    res.status(500).json({ message: 'Internal server error' });
  });

  return app;
}
