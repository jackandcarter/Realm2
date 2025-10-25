import { findRealmById, listRealms, Realm } from '../db/realmRepository';
import {
  createMembership,
  findMembership,
  RealmMembership,
} from '../db/realmMembershipRepository';
import {
  listCharactersForRealm,
  listCharactersForUserInRealm,
  Character,
} from '../db/characterRepository';
import { HttpError } from '../utils/errors';

export interface RealmSummary extends Realm {
  membershipRole: RealmMembership['role'] | null;
  isMember: boolean;
}

export interface RealmCharactersResult {
  realm: Realm;
  membership: RealmMembership;
  characters: Character[];
}

export function listRealmsForUser(userId: string): RealmSummary[] {
  const realms = listRealms();
  return realms.map((realm) => {
    const membership = findMembership(userId, realm.id);
    return {
      ...realm,
      membershipRole: membership?.role ?? null,
      isMember: Boolean(membership),
    };
  });
}

export function ensureMembership(userId: string, realmId: string): RealmMembership {
  const existing = findMembership(userId, realmId);
  if (existing) {
    return existing;
  }
  return createMembership(userId, realmId, 'player');
}

export function getRealmCharacters(
  userId: string,
  realmId: string
): RealmCharactersResult {
  const realm = findRealmById(realmId);
  if (!realm) {
    throw new HttpError(404, 'Realm not found');
  }

  const membership = findMembership(userId, realmId);
  if (!membership) {
    throw new HttpError(403, 'Join the realm before accessing its characters');
  }

  const characters =
    membership.role === 'builder'
      ? listCharactersForRealm(realmId)
      : listCharactersForUserInRealm(userId, realmId);

  return { realm, membership, characters };
}
