import { Router } from 'express';
import { login, logout, refresh, register } from '../services/authService';
import { usernameRegex, validatePassword } from '../utils/authValidation';

const router = Router();

/**
 * @openapi
 * /auth/register:
 *   post:
 *     summary: Register a new user account.
 *     tags:
 *       - Auth
 *     requestBody:
 *       required: true
 *       content:
 *         application/json:
 *           schema:
 *             type: object
 *             required: [email, username, password]
 *             properties:
 *               email:
 *                 type: string
 *                 format: email
 *               username:
 *                 type: string
 *                 minLength: 3
 *                 maxLength: 20
 *                 pattern: '^[a-zA-Z0-9_\\-]+$'
 *               password:
 *                 type: string
 *                 minLength: 8
 *                 description: Must include uppercase, lowercase, number, and special character.
 *     responses:
 *       '201':
 *         description: Successful registration
 *         content:
 *           application/json:
 *             schema:
 *               $ref: '#/components/schemas/AuthResponse'
 *       '400':
 *         description: Validation error
 */
router.post('/register', async (req, res) => {
  const { email, username, password } = req.body ?? {};
  if (typeof email !== 'string' || typeof username !== 'string' || typeof password !== 'string') {
    return res.status(400).json({ message: 'Email, username, and password are required.' });
  }

  const trimmedUsername = username.trim();
  if (!usernameRegex.test(trimmedUsername)) {
    return res.status(400).json({ message: 'Username must be 3-20 characters using letters, numbers, hyphens, or underscores.' });
  }

  const passwordError = validatePassword(password);
  if (passwordError) {
    return res.status(400).json({ message: passwordError });
  }

  try {
    const result = await register(email, trimmedUsername, password);
    res.status(201).json(result);
  } catch (error) {
    res.status(400).json({ message: (error as Error).message });
  }
});

/**
 * @openapi
 * /auth/login:
 *   post:
 *     summary: Log in and receive new auth tokens.
 *     tags:
 *       - Auth
 *     requestBody:
 *       required: true
 *       content:
 *         application/json:
 *           schema:
 *             type: object
 *             required: [email, password]
 *             properties:
 *               email:
 *                 type: string
 *               password:
 *                 type: string
 *     responses:
 *       '200':
 *         description: Login success
 *         content:
 *           application/json:
 *             schema:
 *               $ref: '#/components/schemas/AuthResponse'
 *       '400':
 *         description: Validation error
 *       '401':
 *         description: Invalid credentials
 */
router.post('/login', async (req, res) => {
  const { email, password } = req.body ?? {};
  if (typeof email !== 'string' || typeof password !== 'string') {
    return res.status(400).json({ message: 'Email and password are required.' });
  }

  try {
    const result = await login(email, password);
    res.status(200).json(result);
  } catch (error) {
    res.status(401).json({ message: (error as Error).message });
  }
});

/**
 * @openapi
 * /auth/logout:
 *   post:
 *     summary: Revoke a refresh token.
 *     tags:
 *       - Auth
 *     requestBody:
 *       required: true
 *       content:
 *         application/json:
 *           schema:
 *             type: object
 *             required: [refreshToken]
 *             properties:
 *               refreshToken:
 *                 type: string
 *     responses:
 *       '204':
 *         description: Token revoked
 *       '400':
 *         description: Validation error
 */
router.post('/logout', async (req, res) => {
  const { refreshToken } = req.body ?? {};
  if (typeof refreshToken !== 'string') {
    return res.status(400).json({ message: 'Refresh token is required.' });
  }

  await logout(refreshToken);
  res.status(204).send();
});

/**
 * @openapi
 * /auth/refresh:
 *   post:
 *     summary: Exchange a refresh token for new tokens.
 *     tags:
 *       - Auth
 *     requestBody:
 *       required: true
 *       content:
 *         application/json:
 *           schema:
 *             type: object
 *             required: [refreshToken]
 *             properties:
 *               refreshToken:
 *                 type: string
 *     responses:
 *       '200':
 *         description: Tokens refreshed
 *         content:
 *           application/json:
 *             schema:
 *               $ref: '#/components/schemas/AuthTokens'
 *       '400':
 *         description: Validation error
 *       '401':
 *         description: Invalid refresh token
 */
router.post('/refresh', async (req, res) => {
  const { refreshToken } = req.body ?? {};
  if (typeof refreshToken !== 'string') {
    return res.status(400).json({ message: 'Refresh token is required.' });
  }

  try {
    const tokens = await refresh(refreshToken);
    res.status(200).json(tokens);
  } catch (error) {
    res.status(401).json({ message: (error as Error).message });
  }
});

export { router as authRouter };
