process.env.DB_PATH = ':memory:';
process.env.JWT_SECRET = 'test-secret';

import request from 'supertest';
import { app } from '../src/app';
import { resetDatabase } from '../src/db/database';
import { upsertMembership } from '../src/db/realmMembershipRepository';

interface RealmSummary {
  id: string;
  isMember: boolean;
  membershipRole?: string;
  [key: string]: unknown;
}

interface CharacterSummary {
  name: string;
  classId?: string | null;
  classStates?: unknown;
  lastKnownLocation?: string | null;
  [key: string]: unknown;
}

async function registerAndGetToken(email: string) {
  const username = email.split('@')[0];
  const response = await request(app)
    .post('/auth/register')
    .send({ email, username, password: 'Password123!' })
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
      .send({
        realmId: targetRealm.id,
        name: 'Chrono Seeker',
        raceId: 'felarian',
        appearance: { height: 1.7, build: 0.5 },
      })
      .expect(201);

    expect(createResponse.body.character.name).toBe('Chrono Seeker');
    expect(createResponse.body.character.raceId).toBe('felarian');
    expect(createResponse.body.character.appearance.height).toBeCloseTo(1.7);
    expect(createResponse.body.character.appearance.build).toBeCloseTo(0.5);
    expect(Array.isArray(createResponse.body.character.classStates)).toBe(true);
    expect(createResponse.body.character.classStates.length).toBeGreaterThan(0);
    expect(createResponse.body.character.classStates[0]).toHaveProperty('classId');
    expect(createResponse.body.character).toHaveProperty('lastKnownLocation');

    const realmsAfter = await request(app)
      .get('/realms')
      .set('Authorization', `Bearer ${accessToken}`)
      .expect(200);

    const updatedRealm = (realmsAfter.body.realms as RealmSummary[]).find(
      (realm) => realm.id === targetRealm.id
    );
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
    expect(Array.isArray(playerView.body.characters[0].classStates)).toBe(true);

    upsertMembership(playerOne.userId, realmId, 'builder');

    const builderView = await request(app)
      .get(`/realms/${realmId}/characters`)
      .set('Authorization', `Bearer ${playerOne.accessToken}`)
      .expect(200);

    expect(builderView.body.membership.role).toBe('builder');
    const names = (builderView.body.characters as CharacterSummary[]).map((character) => character.name);
    expect(names).toEqual(expect.arrayContaining(['Elysium Scout', 'Arcane Ranger']));
    expect(builderView.body.characters).toHaveLength(2);
    builderView.body.characters.forEach((character: CharacterSummary) => {
      expect(character).toHaveProperty('classStates');
      expect(character).toHaveProperty('lastKnownLocation');
    });
  });

  it('rejects classes that are forbidden to the selected race', async () => {
    const { accessToken } = await registerAndGetToken('classcheck@example.com');

    const realmsResponse = await request(app)
      .get('/realms')
      .set('Authorization', `Bearer ${accessToken}`)
      .expect(200);

    const targetRealm = realmsResponse.body.realms[0];

    await request(app)
      .post('/characters')
      .set('Authorization', `Bearer ${accessToken}`)
      .send({
        realmId: targetRealm.id,
        name: 'Rule Breaker',
        raceId: 'human',
        classId: 'ranger',
        classStates: [{ classId: 'ranger', unlocked: true }],
      })
      .expect(400);
  });

  it('requires quest classes to be unlocked before selection', async () => {
    const { accessToken } = await registerAndGetToken('buildercheck@example.com');

    const realmsResponse = await request(app)
      .get('/realms')
      .set('Authorization', `Bearer ${accessToken}`)
      .expect(200);

    const targetRealm = realmsResponse.body.realms[0];

    await request(app)
      .post('/characters')
      .set('Authorization', `Bearer ${accessToken}`)
      .send({
        realmId: targetRealm.id,
        name: 'Unlock Tester',
        raceId: 'human',
        classId: 'builder',
        classStates: [{ classId: 'builder', unlocked: false }],
      })
      .expect(400);
  });

  it('rejects invalid race selections', async () => {
    const { accessToken } = await registerAndGetToken('invalid-race@example.com');

    const realmsResponse = await request(app)
      .get('/realms')
      .set('Authorization', `Bearer ${accessToken}`)
      .expect(200);

    const targetRealm = realmsResponse.body.realms[0];

    await request(app)
      .post('/characters')
      .set('Authorization', `Bearer ${accessToken}`)
      .send({ realmId: targetRealm.id, name: 'Outsider', raceId: 'unknown-race' })
      .expect(400);
  });

  it('validates appearance ranges for the selected race', async () => {
    const { accessToken } = await registerAndGetToken('invalid-appearance@example.com');

    const realmsResponse = await request(app)
      .get('/realms')
      .set('Authorization', `Bearer ${accessToken}`)
      .expect(200);

    const targetRealm = realmsResponse.body.realms[0];

    await request(app)
      .post('/characters')
      .set('Authorization', `Bearer ${accessToken}`)
      .send({
        realmId: targetRealm.id,
        name: 'Too Tall Gearling',
        raceId: 'gearling',
        appearance: { height: 2.5 },
      })
      .expect(400);
  });
});
