process.env.DB_PATH = ':memory:';
process.env.JWT_SECRET = 'test-secret';

import request from 'supertest';
import { app } from '../src/app';
import { resetDatabase } from '../src/db/database';
import { upsertMembership } from '../src/db/realmMembershipRepository';

async function registerAndGetToken(email: string) {
  const response = await request(app)
    .post('/auth/register')
    .send({ email, password: 'Password123' })
    .expect(201);

  return {
    accessToken: response.body.tokens.accessToken as string,
    userId: response.body.user.id as string,
  };
}

describe('Realm and character API', () => {
  beforeEach(() => {
    resetDatabase();
  });

  it('requires authentication to list realms', async () => {
    await request(app).get('/realms').expect(401);
  });

  it('lists seeded realms for an authenticated user', async () => {
    const { accessToken } = await registerAndGetToken('player@example.com');

    const response = await request(app)
      .get('/realms')
      .set('Authorization', `Bearer ${accessToken}`)
      .expect(200);

    expect(Array.isArray(response.body.realms)).toBe(true);
    expect(response.body.realms.length).toBeGreaterThanOrEqual(1);
    const realm = response.body.realms[0];
    expect(realm).toHaveProperty('id');
    expect(realm).toHaveProperty('name');
    expect(realm).toHaveProperty('narrative');
    expect(realm.isMember).toBe(false);
  });

  it('requires authentication to create characters', async () => {
    await request(app)
      .post('/characters')
      .send({ realmId: 'realm-elysium-nexus', name: 'Unauth Hero' })
      .expect(401);
  });

  it('creates a character and updates membership status', async () => {
    const { accessToken } = await registerAndGetToken('hero@example.com');

    const realmsResponse = await request(app)
      .get('/realms')
      .set('Authorization', `Bearer ${accessToken}`)
      .expect(200);

    const targetRealm = realmsResponse.body.realms[0];

    const createResponse = await request(app)
      .post('/characters')
      .set('Authorization', `Bearer ${accessToken}`)
      .send({ realmId: targetRealm.id, name: 'Chrono Seeker' })
      .expect(201);

    expect(createResponse.body.character.name).toBe('Chrono Seeker');

    const realmsAfter = await request(app)
      .get('/realms')
      .set('Authorization', `Bearer ${accessToken}`)
      .expect(200);

    const updatedRealm = realmsAfter.body.realms.find((r: any) => r.id === targetRealm.id);
    expect(updatedRealm.isMember).toBe(true);
    expect(updatedRealm.membershipRole).toBe('player');
  });

  it('blocks character roster access for non-members', async () => {
    const { accessToken } = await registerAndGetToken('observer@example.com');

    const realmsResponse = await request(app)
      .get('/realms')
      .set('Authorization', `Bearer ${accessToken}`)
      .expect(200);

    const targetRealm = realmsResponse.body.realms[0];

    await request(app)
      .get(`/realms/${targetRealm.id}/characters`)
      .set('Authorization', `Bearer ${accessToken}`)
      .expect(403);
  });

  it('returns only personal characters for regular players and full rosters for builders', async () => {
    const playerOne = await registerAndGetToken('player1@example.com');
    const playerTwo = await registerAndGetToken('player2@example.com');

    const realmsResponse = await request(app)
      .get('/realms')
      .set('Authorization', `Bearer ${playerOne.accessToken}`)
      .expect(200);

    const realmId = realmsResponse.body.realms[0].id as string;

    await request(app)
      .post('/characters')
      .set('Authorization', `Bearer ${playerOne.accessToken}`)
      .send({ realmId, name: 'Elysium Scout' })
      .expect(201);

    await request(app)
      .post('/characters')
      .set('Authorization', `Bearer ${playerTwo.accessToken}`)
      .send({ realmId, name: 'Arcane Ranger' })
      .expect(201);

    const playerView = await request(app)
      .get(`/realms/${realmId}/characters`)
      .set('Authorization', `Bearer ${playerOne.accessToken}`)
      .expect(200);

    expect(playerView.body.membership.role).toBe('player');
    expect(playerView.body.characters).toHaveLength(1);
    expect(playerView.body.characters[0].name).toBe('Elysium Scout');

    upsertMembership(playerOne.userId, realmId, 'builder');

    const builderView = await request(app)
      .get(`/realms/${realmId}/characters`)
      .set('Authorization', `Bearer ${playerOne.accessToken}`)
      .expect(200);

    expect(builderView.body.membership.role).toBe('builder');
    const names = builderView.body.characters.map((c: any) => c.name);
    expect(names).toEqual(expect.arrayContaining(['Elysium Scout', 'Arcane Ranger']));
    expect(builderView.body.characters).toHaveLength(2);
  });
});
