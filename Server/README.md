# Realm2 Authentication Service

This directory hosts a lightweight Express + TypeScript backend that provides authentication APIs for Realm2. It exposes REST endpoints for registering accounts, logging in, logging out, and refreshing access tokens. A MariaDB database stores users, realms, characters, and progression data, and JWTs secure API access.

## Getting Started

### Prerequisites

- Node.js 18+
- npm 9+
- MariaDB 10.6+ (locally on macOS, or on your Ubuntu VPS)

### Install dependencies

```bash
npm install
```

### Prepare a local (non-Docker) environment

```bash
npm run setup:local
```

This copies `.env.example` to `.env` if needed.

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

### Deploy with migration

```bash
npm run deploy
```

### Environment variables

Create a `.env` file (see `.env.example`) to override defaults.

| Variable | Description | Default |
| --- | --- | --- |
| `PORT` | HTTP port | `3000` |
| `JWT_SECRET` | Secret key for signing JWT access tokens | `dev-secret-change-me` |
| `DB_HOST` | MariaDB host | `127.0.0.1` |
| `DB_PORT` | MariaDB port | `3306` |
| `DB_USER` | MariaDB username | `realm2` |
| `DB_PASSWORD` | MariaDB password | (empty) |
| `DB_NAME` | MariaDB database name | `realm2` |
| `DB_SSL` | Whether to enable SSL for MariaDB connections | `false` |
| `DB_POOL_LIMIT` | Connection pool size | `10` |
| `ACCESS_TOKEN_TTL` | Access token lifetime in seconds | `900` (15 minutes) |
| `REFRESH_TOKEN_TTL` | Refresh token lifetime in seconds | `604800` (7 days) |

## API Documentation

Swagger UI is available at `http://localhost:3000/docs` once the server is running. The OpenAPI definition is generated from the annotations in `src/routes/authRoutes.ts`.

## Database migrations

The service ships with an idempotent migration runner that upgrades the world-state schema automatically on startup. If tables, columns, or indexes are missing, startup migrations will recreate them before the HTTP server begins accepting requests. You can also apply migrations manually using the commands shown above.

## Observability

- Prometheus metrics are exposed at `GET /metrics` and include latency histograms, replication queue gauges, and conflict/error counters for persistence layers.
- Structured logs are emitted via `pino`, capturing migration progress and unexpected errors.

## MariaDB setup (local and VPS)

### Ubuntu VPS (MariaDB)

```bash
sudo apt update
sudo apt install mariadb-server
sudo mysql -e "CREATE DATABASE realm2;"
sudo mysql -e "CREATE USER 'realm2'@'%' IDENTIFIED BY 'change-me';"
sudo mysql -e "GRANT ALL PRIVILEGES ON realm2.* TO 'realm2'@'%'; FLUSH PRIVILEGES;"
```

Update `.env` with `DB_HOST`, `DB_USER`, `DB_PASSWORD`, and `DB_NAME`, then start the server.

### macOS (local)

```bash
brew install mariadb
brew services start mariadb
mysql -e "CREATE DATABASE realm2;"
mysql -e "CREATE USER 'realm2'@'localhost' IDENTIFIED BY 'change-me';"
mysql -e "GRANT ALL PRIVILEGES ON realm2.* TO 'realm2'@'localhost'; FLUSH PRIVILEGES;"
```

Point `DB_HOST` to `127.0.0.1` in `.env` and run the server with `npm run dev`.

### Allowing external access

Ensure your VPS firewall allows inbound traffic on the server `PORT` (default 3000). If your MariaDB instance lives on a different host, allow inbound MariaDB traffic (port 3306 by default) only from trusted IPs.
