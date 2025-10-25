import { NextFunction, Response, Request } from 'express';
import jwt from 'jsonwebtoken';
import { env } from '../config/env';
import { findUserById } from '../db/userRepository';
import { HttpError } from '../utils/errors';

export interface AuthPayload {
  sub: string;
  email: string;
  iat?: number;
  exp?: number;
}

export interface UserContext {
  id: string;
  email: string;
  username: string;
  createdAt: string;
}

declare module 'express-serve-static-core' {
  interface Request {
    user?: UserContext;
  }
}

function parseToken(authHeader: string | undefined): string {
  if (!authHeader) {
    throw new HttpError(401, 'Missing authorization header');
  }

  const [scheme, token] = authHeader.split(' ');
  if (!token || scheme.toLowerCase() !== 'bearer') {
    throw new HttpError(401, 'Authorization header must use Bearer scheme');
  }
  return token;
}

export function requireAuth(
  req: Request,
  _res: Response,
  next: NextFunction
): void {
  try {
    const token = parseToken(req.header('Authorization'));
    const payload = jwt.verify(token, env.jwtSecret) as AuthPayload;
    const user = findUserById(payload.sub);
    if (!user) {
      throw new HttpError(401, 'Invalid access token');
    }
    req.user = { id: user.id, email: user.email, username: user.username, createdAt: user.createdAt };
    next();
  } catch (error) {
    if (error instanceof HttpError) {
      next(error);
      return;
    }
    next(new HttpError(401, 'Invalid access token'));
  }
}
