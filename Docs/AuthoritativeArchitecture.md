# Authoritative Architecture Boundaries

This project follows a classic MMO separation of responsibility: the **client is a view/prediction layer** and the **server is the source of truth**. Use this document to keep data ownership, validation, and storage concerns cleanly separated.

## Mental model

- **Client = view + input + prediction.**
- **Server = truth + validation + persistence.**

Any outcome that affects gameplay, economy, or progression must be decided and stored by the server. The client may predict for UX, but the server is the judge.

## What lives on the server (authoritative)

### 1) Game rules that matter
The server owns and validates anything that affects outcomes:

- Combat resolution (hit/miss/crit, damage, mitigation, status effects)
- Inventory changes and currency adjustments
- Quest state, achievements, class unlocks
- Cooldowns, cast validity, resource costs
- Trading, mail, auction house actions
- Crafting results and loot generation
- Permission checks (GM tools, guild roles, instance access)
- Authoritative position rules (see movement section)

Every request from the client is validated against **server-side state** and **server-side world rules**.

### 2) Persistence source of truth
Server state is split into:

- **In-memory runtime state** (fast, volatile): session state, nearby entities, instance state, combat state, cooldown timers.
- **Durable persistence** (DB + logs): characters, inventory tables, quest progress, mail, guild membership.

Write strategies depend on criticality:

- **Dirty flag / periodic flush** for non-critical runtime changes.
- **Transactional writes** for critical operations (currency, trades, item grants).
- **Outbox/event logs** for reliability and auditing.

### 3) Security boundary
Assume the client is untrusted. Clients can:

- Send forged packets
- Replay actions
- Modify memory / speedhack
- Skip client-side validation

Therefore, the server must never trust client state as authoritative truth.

## What lives on the client (non-authoritative)

### 1) Presentation and UX state
Client-only data includes:

- UI layout, keybinds, hotbars
- Cached tooltips, icon metadata, localization
- Prediction/interpolation buffers
- Optional local logs/replays

### 2) UX validation (fast feedback)
The client can pre-check obvious invalid actions for responsiveness:

- “Not enough mana”
- “Skill on cooldown”
- “Target out of range”

These checks do **not** replace server validation. They exist only to improve feel.

## Movement: hybrid authority
Movement is typically hybrid, but server-authenticated:

- **Input-authoritative:** client sends inputs (WASD/jump), server simulates.
- **Position + reconciliation:** client sends positions, server sanity-checks and corrects.

In both cases, the server verifies:

- Max speed/accel
- Collision/nav constraints
- No teleports or invalid jumps

Clients smooth and interpolate server snapshots but accept corrections.

## Combat: server-first
The client requests actions; the server decides outcomes.

Client: “Cast spell X on target Y.”

Server validates:

- Does the player know the spell?
- Is it off cooldown?
- Is the target valid/in range/line-of-sight?
- Are resources available?
- Are there disallowed states (stun/silence/root)?

The server applies results and replicates authoritative events to clients.

## Repository boundaries (do not mix)

We maintain three layers of data and logic:

### A) Shared domain model (pure + deterministic)
Allowed in shared code:

- Domain concepts: `Character`, `Inventory`, `QuestState`
- Pure rules: `CanCastSpell(state, spell, target) -> bool`
- DTOs / request / response definitions

Restrictions:

- **No Unity dependencies.**
- **No server DB or persistence dependencies.**
- Must be deterministic and side-effect free.

### B) Server persistence repositories (server-only)
Server repositories are the **authoritative** source of truth:

- `ICharacterRepository` backed by MariaDB/Postgres
- Transactional logic, migrations, constraints, locks
- Runtime state caches (if needed)

This is never a Unity client repository or cache.

### C) Client caches / view-model stores (client-only)
Client stores provide presentation state only:

- `ClientCharacterStore`, `InventoryViewModel`
- Optimistic UI updates and local caches
- Formatting for UI

Client stores are **not** server truth and must not be imported into server projects.

## Validation: duplication with different intent
- **Client validation:** “Can I enable this button?” / “Should I send the request?”
- **Server validation:** “Is this request legal and will I apply it?”

Validation logic can be **shared** only if it is **pure** and does not depend on client storage or server DBs. The server still re-checks every request using server state.

## Persistence strategies seen in MMOs

1) **Snapshot + deltas**
   - Periodic character saves
   - Critical deltas (loot, trade, currency) saved immediately

2) **Event-sourcing-ish patterns**
   - Append-only event logs for audit/replay
   - Outbox for reliable cross-service actions

3) **Strong DB constraints**
   - Unique item instance IDs
   - Foreign keys and ownership checks
   - Non-negative currency
   - Transactional trades (all-or-nothing)

## Client intents + server action queue

Client requests that would change progression, inventory, or world state should be captured as **intents** and stored server-side for validation. The server writes intent payloads into a queue table (for example `character_action_requests`), validates them against authoritative state, and only then mutates gameplay tables. Clients never write progression tables directly; they submit intents and wait for server-confirmed snapshots.

## Rules of thumb for this repo

- **Server owns truth:** server repositories + runtime state are authoritative.
- **Client stores are forbidden in server:** no Unity repos, no client caches, no UI models inside server code.
- **Shared code is allowed only for pure domain rules + DTOs.**
- **Server validates every request** using server-loaded state.
- **Client is optimistic but correctable:** predict locally, accept server corrections.

Use these rules when introducing new features, data models, or refactors so the architecture stays clean and secure.
