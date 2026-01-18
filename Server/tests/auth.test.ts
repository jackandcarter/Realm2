process.env.DB_NAME = process.env.DB_NAME ?? 'realm2_test';
process.env.JWT_SECRET = 'test-secret';

import request from 'supertest';
import { app } from '../src/app';
import { resetDatabase } from '../src/db/database';

describe('Auth API', () => {
  beforeEach(async () => {
    await resetDatabase();
  });

  it('registers a new user and returns tokens', async () => {
    const response = await request(app)
      .post('/auth/register')
      .send({ email: 'user@example.com', username: 'realmrunner', password: 'Password123!' })
      .expect(201);

    expect(response.body.user.email).toBe('user@example.com');
    expect(response.body.user.username).toBe('realmrunner');
    expect(response.body.tokens.accessToken).toBeDefined();
    expect(response.body.tokens.refreshToken).toBeDefined();
  });

  it('prevents registering with an existing email', async () => {
    await request(app)
      .post('/auth/register')
      .send({ email: 'user@example.com', username: 'realmrunner', password: 'Password123!' })
      .expect(201);

    const response = await request(app)
      .post('/auth/register')
      .send({ email: 'user@example.com', username: 'realmrunner2', password: 'Password123!' })
      .expect(400);

    expect(response.body.message).toMatch(/already registered/i);
  });

  it('prevents registering with an existing username', async () => {
    await request(app)
      .post('/auth/register')
      .send({ email: 'user@example.com', username: 'realmrunner', password: 'Password123!' })
      .expect(201);

    const response = await request(app)
      .post('/auth/register')
      .send({ email: 'another@example.com', username: 'realmrunner', password: 'Password123!' })
      .expect(400);

    expect(response.body.message).toMatch(/username already taken/i);
  });

  it('authenticates an existing user', async () => {
    await request(app)
      .post('/auth/register')
      .send({ email: 'user@example.com', username: 'realmrunner', password: 'Password123!' })
      .expect(201);

    const response = await request(app)
      .post('/auth/login')
      .send({ email: 'user@example.com', password: 'Password123!' })
      .expect(200);

    expect(response.body.tokens.accessToken).toBeDefined();
    expect(response.body.tokens.refreshToken).toBeDefined();
  });

  it('rejects invalid login attempts', async () => {
    await request(app)
      .post('/auth/register')
      .send({ email: 'user@example.com', username: 'realmrunner', password: 'Password123!' })
      .expect(201);

    const response = await request(app)
      .post('/auth/login')
      .send({ email: 'user@example.com', password: 'WrongPassword' })
      .expect(401);

    expect(response.body.message).toMatch(/invalid/i);
  });

  it('refreshes tokens using a valid refresh token', async () => {
    const registerResponse = await request(app)
      .post('/auth/register')
      .send({ email: 'user@example.com', username: 'realmrunner', password: 'Password123!' })
      .expect(201);

    const refreshToken = registerResponse.body.tokens.refreshToken;

    const refreshResponse = await request(app)
      .post('/auth/refresh')
      .send({ refreshToken })
      .expect(200);

    expect(refreshResponse.body.accessToken).toBeDefined();
    expect(refreshResponse.body.refreshToken).toBeDefined();
    expect(refreshResponse.body.refreshToken).not.toBe(refreshToken);
  });

  it('invalidates refresh tokens after logout', async () => {
    const registerResponse = await request(app)
      .post('/auth/register')
      .send({ email: 'user@example.com', username: 'realmrunner', password: 'Password123!' })
      .expect(201);

    const refreshToken = registerResponse.body.tokens.refreshToken;

    await request(app).post('/auth/logout').send({ refreshToken }).expect(204);

    const response = await request(app)
      .post('/auth/refresh')
      .send({ refreshToken })
      .expect(401);

    expect(response.body.message).toMatch(/invalid/i);
  });
});
