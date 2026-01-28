import { findRealmById, listRealms, Realm } from '../db/realmRepository';
import {
  createMembership,
  findMembership,
  RealmMembership,
} from '../db/realmMembershipRepository';
import { updateUserRealmSelection } from '../db/userRepository';
import {
  listCharactersForRealm,
  listCharactersForUserInRealm,
  Character,
} from '../db/characterRepository';
import { HttpError } from '../utils/errors';
import { resolveRealmHosting } from '../config/realmHosting';

export interface RealmSummary extends Realm {
  membershipRole: RealmMembership['role'] | null;
  isMember: boolean;
  worldSceneName: string;
  worldServiceUrl?: string;
}

export interface RealmCharactersResult {
  realm: Realm;
  membership: RealmMembership;
  characters: Character[];
}

export async function listRealmsForUser(userId: string): Promise<RealmSummary[]> {
  const realms = await listRealms();
  const summaries = await Promise.all(
    realms.map(async (realm) => {
      const membership = await findMembership(userId, realm.id);
      const hosting = resolveRealmHosting(realm.id);
      return {
        ...realm,
        membershipRole: membership?.role ?? null,
        isMember: Boolean(membership),
        worldSceneName: hosting.worldSceneName,
        worldServiceUrl: hosting.worldServiceUrl,
      };
    })
  );
  return summaries;
}

export async function ensureMembership(userId: string, realmId: string): Promise<RealmMembership> {
  const existing = await findMembership(userId, realmId);
  if (existing) {
    return existing;
  }
  return createMembership(userId, realmId, 'player');
}

export async function getRealmCharacters(
  userId: string,
  realmId: string
): Promise<RealmCharactersResult> {
  const realm = await findRealmById(realmId);
  if (!realm) {
    throw new HttpError(404, 'Realm not found');
  }

  const membership = await findMembership(userId, realmId);
  if (!membership) {
    throw new HttpError(403, 'Join the realm before accessing its characters');
  }

  const characters =
    membership.role === 'builder'
      ? await listCharactersForRealm(realmId)
      : await listCharactersForUserInRealm(userId, realmId);

  return { realm, membership, characters };
}

export async function selectRealmForUser(userId: string, realmId: string): Promise<Realm> {
  const realm = await findRealmById(realmId);
  if (!realm) {
    throw new HttpError(404, 'Realm not found');
  }

  await ensureMembership(userId, realmId);
  await updateUserRealmSelection(userId, realmId);
  return realm;
}
