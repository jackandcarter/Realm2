# Realm2 Authentication Service

This directory hosts a lightweight Express + TypeScript backend that provides authentication APIs for Realm2. It exposes REST endpoints for registering accounts, logging in, logging out, and refreshing access tokens. A SQLite database stores users and refresh tokens, and JWTs secure API access.

## Getting Started

### Prerequisites

- Node.js 18+
- npm 9+

### Install dependencies

```bash
npm install
```

### Prepare a local (non-Docker) environment

```bash
npm run setup:local
```

This copies `.env.example` to `.env` if needed and creates the local `data/` directories
used for SQLite and backups.

### Run in development mode

```bash
npm run dev
```

The service listens on `http://localhost:3000` by default.

### Run the test suite

```bash
npm test
```

### Build for production

```bash
npm run build
npm start
```

### Run schema migrations manually

```bash
npm run build
npm run migrate
```

### Deploy with automated backup + migration

```bash
npm run deploy
```

### Environment variables

Create a `.env` file (see `.env.example`) to override defaults.

| Variable | Description | Default |
| --- | --- | --- |
| `PORT` | HTTP port | `3000` |
| `JWT_SECRET` | Secret key for signing JWT access tokens | `dev-secret-change-me` |
| `DB_PATH` | Path to SQLite database file (takes precedence over `DATABASE_URL`) | `./data/app.db` |
| `DATABASE_URL` | Alternate way to provide the SQLite database path (useful for CI secrets) | `./data/app.db` |
| `ACCESS_TOKEN_TTL` | Access token lifetime in seconds | `900` (15 minutes) |
| `REFRESH_TOKEN_TTL` | Refresh token lifetime in seconds | `604800` (7 days) |
| `DB_BACKUP_DIR` | Directory where SQLite backups are written | `./data/backups` |
| `DB_BACKUP_INTERVAL_MINUTES` | Minutes between automatic snapshots (`0` disables scheduling) | `60` |
| `DB_BACKUP_RETENTION_DAYS` | Days to keep snapshot files before pruning | `7` |

## API Documentation

Swagger UI is available at `http://localhost:3000/docs` once the server is running. The OpenAPI definition is generated from the annotations in `src/routes/authRoutes.ts`.

## Database migrations

The service ships with an idempotent migration runner that upgrades the world-state schema automatically on startup. You can also apply migrations manually using the commands shown above. During Docker container startup the migration runner executes before the HTTP server begins accepting requests.

## Backups and restores

Automated snapshots copy the primary SQLite database file plus WAL/SHM journals into `DB_BACKUP_DIR` on an interval configured by `DB_BACKUP_INTERVAL_MINUTES`. Set the interval to `0` to disable scheduling. Old snapshots are pruned after `DB_BACKUP_RETENTION_DAYS`.

- Manual snapshot: `npm run build && npm run backup`
- Restore snapshot: `npm run build && npm run restore -- <snapshot-path>`

Restore operations should be performed while the service is offline. Restart the service after a restore so a new database connection is established.

## Observability

- Prometheus metrics are exposed at `GET /metrics` and include latency histograms, replication queue gauges, and conflict/error counters for persistence layers.
- Structured logs are emitted via `pino`, capturing migration progress, backup lifecycle, and unexpected errors.

## Docker support

Docker is optional. Use it only if you want container parity with CI or a production image.

Build the production image:

```bash
docker build -t realm2-auth .
```

Run locally with Docker Compose (hot reload + TypeScript):

```bash
docker compose up --build
```

Data is stored in the `./data` directory on the host machine.
