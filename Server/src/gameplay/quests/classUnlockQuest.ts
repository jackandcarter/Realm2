import {
  ClassUnlockCollection,
  ClassUnlockInput,
  ForbiddenClassUnlockError,
  QuestStateInput,
  getCharacterProgressionSnapshot,
  replaceClassUnlocks,
  replaceQuestStates,
} from '../../db/progressionRepository';

export type QuestCompletionStatus = 'completed' | 'failed';
export type QuestNotificationType = 'success' | 'info' | 'error';

export interface QuestNotification {
  type: QuestNotificationType;
  message: string;
  classId: string;
  timestamp: string;
}

export interface QuestJournalEntry {
  title: string;
  body: string;
  timestamp: string;
}

export interface QuestFailureRecord {
  reason: string;
  occurredAt: string;
}

export interface QuestProgressData {
  notifications: QuestNotification[];
  journalEntries: QuestJournalEntry[];
  attempts: number;
  unlockedClassId?: string;
  completedAt?: string;
  failure?: QuestFailureRecord;
}

export interface QuestCompletionResult {
  questId: string;
  status: QuestCompletionStatus;
  unlockGranted: boolean;
  unlockError?: string;
  questStateVersion: number;
  classUnlockVersion: number;
  notifications: QuestNotification[];
  journalEntries: QuestJournalEntry[];
}

export interface JournalTemplate {
  title: string;
  body: string;
}

export interface ClassUnlockQuestOptions {
  questId: string;
  classId: string;
  classDisplayName: string;
  successNotificationMessage?: string;
  alreadyUnlockedNotificationMessage?: string;
  failureNotificationMessage?: string;
  successJournalEntry?: JournalTemplate;
  failureJournalEntry?: JournalTemplate;
}

interface GuardedUnlockOutcome {
  success: boolean;
  granted: boolean;
  collection: ClassUnlockCollection;
  error?: string;
}

export function completeClassUnlockQuest(
  characterId: string,
  options: ClassUnlockQuestOptions,
): QuestCompletionResult {
  const snapshot = getCharacterProgressionSnapshot(characterId);
  const now = new Date().toISOString();
  const targetQuestId = options.questId.trim();
  const targetClassId = options.classId.trim();

  const existingQuest = snapshot.quests.quests.find(
    (quest) => quest.questId.trim().toLowerCase() === targetQuestId.toLowerCase(),
  );

  const currentProgress = normalizeQuestProgress(existingQuest?.progressJson);
  currentProgress.notifications = [...currentProgress.notifications];
  currentProgress.journalEntries = [...currentProgress.journalEntries];
  currentProgress.attempts += 1;

  const unlockOutcome = attemptGuardedClassUnlock(
    characterId,
    targetClassId,
    snapshot.classUnlocks,
  );

  let status: QuestCompletionStatus;
  if (unlockOutcome.success) {
    status = 'completed';
    currentProgress.unlockedClassId = targetClassId;
    currentProgress.completedAt = now;
    delete currentProgress.failure;

    const notificationMessage = unlockOutcome.granted
      ? formatMessage(
          options.successNotificationMessage ?? 'Class {className} unlocked!',
          options.classDisplayName,
          unlockOutcome.error,
        )
      : formatMessage(
          options.alreadyUnlockedNotificationMessage ?? '{className} was already unlocked.',
          options.classDisplayName,
          unlockOutcome.error,
        );

    const notification: QuestNotification = {
      type: unlockOutcome.granted ? 'success' : 'info',
      message: notificationMessage,
      classId: targetClassId,
      timestamp: now,
    };
    currentProgress.notifications.push(notification);

    const journalTemplate = options.successJournalEntry ?? {
      title: 'Class Unlocked: {className}',
      body: 'You completed the {className} quest and earned the mantle.',
    };
    const journalEntry: QuestJournalEntry = {
      title: formatMessage(journalTemplate.title, options.classDisplayName, unlockOutcome.error),
      body: formatMessage(journalTemplate.body, options.classDisplayName, unlockOutcome.error),
      timestamp: now,
    };
    currentProgress.journalEntries.push(journalEntry);
  } else {
    status = 'failed';
    const errorMessage = unlockOutcome.error ?? 'Unknown error';
    currentProgress.failure = { reason: errorMessage, occurredAt: now };

    const notification: QuestNotification = {
      type: 'error',
      message: formatMessage(
        options.failureNotificationMessage ?? 'Unable to unlock {className}: {error}',
        options.classDisplayName,
        errorMessage,
      ),
      classId: targetClassId,
      timestamp: now,
    };
    currentProgress.notifications.push(notification);

    const journalTemplate = options.failureJournalEntry ?? {
      title: 'Class Unlock Failed: {className}',
      body: 'Your attempt to unlock {className} failed: {error}.',
    };
    const journalEntry: QuestJournalEntry = {
      title: formatMessage(journalTemplate.title, options.classDisplayName, errorMessage),
      body: formatMessage(journalTemplate.body, options.classDisplayName, errorMessage),
      timestamp: now,
    };
    currentProgress.journalEntries.push(journalEntry);
  }

  const questInputs = buildQuestStateInputs(snapshot.quests.quests, targetQuestId, {
    questId: targetQuestId,
    status,
    progress: currentProgress,
  });

  const questCollection = replaceQuestStates(
    characterId,
    questInputs,
    snapshot.quests.version,
  );

  return {
    questId: targetQuestId,
    status,
    unlockGranted: unlockOutcome.granted,
    unlockError: unlockOutcome.success ? undefined : unlockOutcome.error,
    questStateVersion: questCollection.version,
    classUnlockVersion: unlockOutcome.collection.version,
    notifications: currentProgress.notifications,
    journalEntries: currentProgress.journalEntries,
  };
}

function attemptGuardedClassUnlock(
  characterId: string,
  classId: string,
  collection: ClassUnlockCollection,
): GuardedUnlockOutcome {
  const normalizedId = classId.trim().toLowerCase();
  const existing = collection.unlocks.find(
    (entry) => entry.classId.trim().toLowerCase() === normalizedId,
  );

  if (existing?.unlocked) {
    return { success: true, granted: false, collection };
  }

  const normalizedInputs = normalizeUnlockInputs(collection.unlocks, normalizedId);
  if (!normalizedInputs.some((entry) => entry.classId.trim().toLowerCase() === normalizedId)) {
    normalizedInputs.push({ classId, unlocked: true });
  } else {
    for (const entry of normalizedInputs) {
      if (entry.classId.trim().toLowerCase() === normalizedId) {
        entry.unlocked = true;
      }
    }
  }

  try {
    const updated = replaceClassUnlocks(characterId, normalizedInputs, collection.version);
    return { success: true, granted: true, collection: updated };
  } catch (error) {
    if (error instanceof ForbiddenClassUnlockError) {
      return {
        success: false,
        granted: false,
        collection,
        error: error.message,
      };
    }
    throw error;
  }
}

function normalizeUnlockInputs(
  unlocks: ClassUnlockCollection['unlocks'],
  normalizedTargetId: string,
): ClassUnlockInput[] {
  const results: ClassUnlockInput[] = [];
  const seen = new Set<string>();
  for (const entry of unlocks) {
    const normalized = entry.classId.trim().toLowerCase();
    if (seen.has(normalized)) {
      continue;
    }
    seen.add(normalized);
    results.push({
      classId: entry.classId,
      unlocked: entry.unlocked,
      unlockedAt: entry.unlockedAt ?? null,
    });
  }

  if (!seen.has(normalizedTargetId)) {
    // Keep placeholder entry so the guarded unlock attempt can add it later.
  }

  return results;
}

interface QuestStateSeed {
  questId: string;
  status: QuestCompletionStatus;
  progress: QuestProgressData;
}

function buildQuestStateInputs(
  existing: { questId: string; status: string; progressJson?: string }[],
  questId: string,
  update: QuestStateSeed,
): QuestStateInput[] {
  const questInputs: QuestStateInput[] = [];
  const normalizedId = questId.trim().toLowerCase();
  const serializedProgress = serializeQuestProgress(update.progress);

  let replaced = false;
  for (const quest of existing) {
    const normalizedQuestId = quest.questId.trim().toLowerCase();
    if (normalizedQuestId === normalizedId) {
      questInputs.push({ questId: update.questId, status: update.status, progress: serializedProgress });
      replaced = true;
      continue;
    }

    questInputs.push({
      questId: quest.questId,
      status: quest.status,
      progress: deserializeStoredProgress(quest.progressJson),
    });
  }

  if (!replaced) {
    questInputs.push({ questId: update.questId, status: update.status, progress: serializedProgress });
  }

  return questInputs;
}

function deserializeStoredProgress(progressJson: string | undefined): unknown {
  if (!progressJson) {
    return undefined;
  }
  try {
    return JSON.parse(progressJson);
  } catch (_error) {
    return undefined;
  }
}

function normalizeQuestProgress(progressJson: string | undefined): QuestProgressData {
  const base: QuestProgressData = {
    notifications: [],
    journalEntries: [],
    attempts: 0,
  };

  if (!progressJson) {
    return base;
  }

  let raw: unknown;
  try {
    raw = JSON.parse(progressJson);
  } catch (_error) {
    return base;
  }

  if (!raw || typeof raw !== 'object') {
    return base;
  }

  const record = raw as Record<string, unknown>;

  if (Array.isArray(record.notifications)) {
    base.notifications = record.notifications
      .map((entry) => normalizeNotification(entry))
      .filter((entry): entry is QuestNotification => entry !== null);
  }

  if (Array.isArray(record.journalEntries)) {
    base.journalEntries = record.journalEntries
      .map((entry) => normalizeJournalEntry(entry))
      .filter((entry): entry is QuestJournalEntry => entry !== null);
  }

  if (typeof record.attempts === 'number' && Number.isFinite(record.attempts)) {
    base.attempts = Math.max(0, Math.floor(record.attempts));
  }

  if (typeof record.unlockedClassId === 'string' && record.unlockedClassId.trim() !== '') {
    base.unlockedClassId = record.unlockedClassId;
  }

  if (typeof record.completedAt === 'string' && record.completedAt.trim() !== '') {
    base.completedAt = record.completedAt;
  }

  const failure = record.failure;
  if (failure && typeof failure === 'object') {
    const failureRecord = failure as Record<string, unknown>;
    const reason = failureRecord.reason;
    const occurredAt = failureRecord.occurredAt;
    if (typeof reason === 'string' && reason.trim() !== '' && typeof occurredAt === 'string') {
      base.failure = { reason, occurredAt };
    }
  }

  return base;
}

function normalizeNotification(entry: unknown): QuestNotification | null {
  if (!entry || typeof entry !== 'object') {
    return null;
  }
  const record = entry as Record<string, unknown>;
  const message = typeof record.message === 'string' ? record.message : null;
  const classId = typeof record.classId === 'string' ? record.classId : null;
  const timestamp = typeof record.timestamp === 'string' ? record.timestamp : null;

  if (!message || !classId || !timestamp) {
    return null;
  }

  const typeValue = typeof record.type === 'string' ? record.type.toLowerCase() : 'info';
  const type: QuestNotificationType = typeValue === 'success' || typeValue === 'error' ? (typeValue as QuestNotificationType) : 'info';

  return { type, message, classId, timestamp };
}

function normalizeJournalEntry(entry: unknown): QuestJournalEntry | null {
  if (!entry || typeof entry !== 'object') {
    return null;
  }
  const record = entry as Record<string, unknown>;
  const title = typeof record.title === 'string' ? record.title : null;
  const body = typeof record.body === 'string' ? record.body : null;
  const timestamp = typeof record.timestamp === 'string' ? record.timestamp : null;
  if (!title || !body || !timestamp) {
    return null;
  }
  return { title, body, timestamp };
}

function serializeQuestProgress(progress: QuestProgressData): Record<string, unknown> {
  const result: Record<string, unknown> = {
    notifications: progress.notifications.map((entry) => ({
      type: entry.type,
      message: entry.message,
      classId: entry.classId,
      timestamp: entry.timestamp,
    })),
    journalEntries: progress.journalEntries.map((entry) => ({
      title: entry.title,
      body: entry.body,
      timestamp: entry.timestamp,
    })),
    attempts: progress.attempts,
  };

  if (progress.unlockedClassId) {
    result.unlockedClassId = progress.unlockedClassId;
  }

  if (progress.completedAt) {
    result.completedAt = progress.completedAt;
  }

  if (progress.failure) {
    result.failure = {
      reason: progress.failure.reason,
      occurredAt: progress.failure.occurredAt,
    };
  }

  return result;
}

function formatMessage(template: string, classDisplayName: string, errorMessage?: string): string {
  return template
    .replaceAll('{className}', classDisplayName)
    .replaceAll('{error}', errorMessage ?? 'unknown error');
}
