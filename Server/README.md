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

### Environment variables

Create a `.env` file (see `.env.example`) to override defaults.

| Variable | Description | Default |
| --- | --- | --- |
| `PORT` | HTTP port | `3000` |
| `JWT_SECRET` | Secret key for signing JWT access tokens | `dev-secret-change-me` |
| `DB_PATH` | Path to SQLite database file | `./data/app.db` |
| `ACCESS_TOKEN_TTL` | Access token lifetime in seconds | `900` (15 minutes) |
| `REFRESH_TOKEN_TTL` | Refresh token lifetime in seconds | `604800` (7 days) |

## API Documentation

Swagger UI is available at `http://localhost:3000/docs` once the server is running. The OpenAPI definition is generated from the annotations in `src/routes/authRoutes.ts`.

## Docker support

Build the production image:

```bash
docker build -t realm2-auth .
```

Run locally with Docker Compose (hot reload + TypeScript):

```bash
docker compose up --build
```

Data is stored in the `./data` directory on the host machine.
