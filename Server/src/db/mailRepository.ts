import { randomUUID } from 'crypto';
import { db, DbExecutor } from './database';

export interface MailMessage {
  id: string;
  realmId: string;
  fromCharacterId: string;
  toCharacterId: string;
  subject: string;
  body?: string | null;
  sentAt: string;
  readAt?: string | null;
}

export interface MailItem {
  id: string;
  mailId: string;
  itemId: string;
  quantity: number;
  metadataJson: string;
}

function mapMailRow(row: any): MailMessage {
  return {
    id: row.id,
    realmId: row.realm_id,
    fromCharacterId: row.from_character_id,
    toCharacterId: row.to_character_id,
    subject: row.subject,
    body: row.body,
    sentAt: row.sent_at,
    readAt: row.read_at,
  };
}

function mapMailItemRow(row: any): MailItem {
  return {
    id: row.id,
    mailId: row.mail_id,
    itemId: row.item_id,
    quantity: row.quantity,
    metadataJson: row.metadata_json ?? '{}',
  };
}

export async function sendMail(
  realmId: string,
  fromCharacterId: string,
  toCharacterId: string,
  subject: string,
  body: string | null,
  executor: DbExecutor = db
): Promise<MailMessage> {
  const now = new Date().toISOString();
  const mail: MailMessage = {
    id: randomUUID(),
    realmId,
    fromCharacterId,
    toCharacterId,
    subject,
    body,
    sentAt: now,
    readAt: null,
  };

  await executor.execute(
    `INSERT INTO mail (id, realm_id, from_character_id, to_character_id, subject, body, sent_at, read_at)
     VALUES (?, ?, ?, ?, ?, ?, ?, ?)`,
    [
      mail.id,
      mail.realmId,
      mail.fromCharacterId,
      mail.toCharacterId,
      mail.subject,
      mail.body,
      mail.sentAt,
      mail.readAt,
    ]
  );

  return mail;
}

export async function addMailItem(
  mailId: string,
  itemId: string,
  quantity: number,
  metadataJson = '{}',
  executor: DbExecutor = db
): Promise<MailItem> {
  const record: MailItem = {
    id: randomUUID(),
    mailId,
    itemId,
    quantity: Math.max(1, Math.floor(quantity)),
    metadataJson,
  };

  await executor.execute(
    `INSERT INTO mail_items (id, mail_id, item_id, quantity, metadata_json)
     VALUES (?, ?, ?, ?, ?)`,
    [record.id, record.mailId, record.itemId, record.quantity, record.metadataJson]
  );

  return record;
}

export async function listMailForCharacter(
  characterId: string,
  executor: DbExecutor = db
): Promise<MailMessage[]> {
  const rows = await executor.query(
    `SELECT id, realm_id, from_character_id, to_character_id, subject, body, sent_at, read_at
     FROM mail
     WHERE to_character_id = ?
     ORDER BY sent_at DESC`,
    [characterId]
  );
  return rows.map(mapMailRow);
}

export async function listMailItems(
  mailId: string,
  executor: DbExecutor = db
): Promise<MailItem[]> {
  const rows = await executor.query(
    `SELECT id, mail_id, item_id, quantity, metadata_json
     FROM mail_items
     WHERE mail_id = ?`,
    [mailId]
  );
  return rows.map(mapMailItemRow);
}

export async function markMailRead(
  mailId: string,
  executor: DbExecutor = db
): Promise<void> {
  const now = new Date().toISOString();
  await executor.execute(
    `UPDATE mail
     SET read_at = ?
     WHERE id = ? AND read_at IS NULL`,
    [now, mailId]
  );
}
