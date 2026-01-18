import { Counter, Gauge, Histogram, Registry, collectDefaultMetrics } from 'prom-client';

export const metricsRegistry = new Registry();
collectDefaultMetrics({ register: metricsRegistry });

export const persistenceLatencyHistogram = new Histogram({
  name: 'realm2_persistence_operation_duration_seconds',
  help: 'Latency distribution for persistence operations.',
  labelNames: ['operation'],
  registers: [metricsRegistry],
});

export const persistenceErrorCounter = new Counter({
  name: 'realm2_persistence_errors_total',
  help: 'Total number of persistence errors.',
  labelNames: ['operation', 'type'],
  registers: [metricsRegistry],
});

export const conflictCounter = new Counter({
  name: 'realm2_persistence_conflicts_total',
  help: 'Total number of optimistic concurrency conflicts.',
  labelNames: ['entity'],
  registers: [metricsRegistry],
});

export const replicationQueueGauge = new Gauge({
  name: 'realm2_replication_queue_length',
  help: 'Number of pending chunk changes awaiting replication.',
  labelNames: ['realmId'],
  registers: [metricsRegistry],
});

export function measurePersistenceOperation<T>(operation: string, fn: () => T): T {
  const end = persistenceLatencyHistogram.startTimer({ operation });
  try {
    return fn();
  } catch (error) {
    const type = error instanceof Error ? error.name : 'unknown_error';
    persistenceErrorCounter.inc({ operation, type });
    throw error;
  } finally {
    end();
  }
}

export async function measurePersistenceOperationAsync<T>(
  operation: string,
  fn: () => Promise<T>
): Promise<T> {
  const end = persistenceLatencyHistogram.startTimer({ operation });
  try {
    return await fn();
  } catch (error) {
    const type = error instanceof Error ? error.name : 'unknown_error';
    persistenceErrorCounter.inc({ operation, type });
    throw error;
  } finally {
    end();
  }
}

export function recordVersionConflict(entity: string): void {
  conflictCounter.inc({ entity });
}

export function setReplicationQueueLength(realmId: string, length: number): void {
  replicationQueueGauge.set({ realmId }, length);
}
