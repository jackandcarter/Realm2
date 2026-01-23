import type { DbExecutor } from './database';

interface CurrencySeed {
  id: string;
  name: string;
  description: string | null;
  isPremium: boolean;
  iconUrl: string | null;
}

export async function seedDefaultCurrencies(executor: DbExecutor): Promise<void> {
  const now = new Date().toISOString();
  const seeds: CurrencySeed[] = [
    {
      id: 'gold',
      name: 'Gold',
      description: 'Standard realm currency',
      isPremium: false,
      iconUrl: null,
    },
    {
      id: 'chrono-shards',
      name: 'Chrono Shards',
      description: 'Premium currency tied to the Chrono Nexus',
      isPremium: true,
      iconUrl: null,
    },
  ];

  for (const seed of seeds) {
    await executor.execute(
      `INSERT INTO currencies (id, name, description, is_premium, icon_url, created_at, updated_at)
       VALUES (?, ?, ?, ?, ?, ?, ?)
       ON DUPLICATE KEY UPDATE
         name = VALUES(name),
         description = VALUES(description),
         is_premium = VALUES(is_premium),
         icon_url = VALUES(icon_url),
         updated_at = VALUES(updated_at)`,
      [
        seed.id,
        seed.name,
        seed.description,
        seed.isPremium ? 1 : 0,
        seed.iconUrl,
        now,
        now,
      ]
    );
  }
}
