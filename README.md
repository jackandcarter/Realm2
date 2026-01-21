# REALM MMO Project Overview

## What Is REALM?
REALM is an original massively multiplayer online RPG set in **Elysium**, a world where high fantasy and neo-magitech aesthetics intertwine. Players navigate sprawling cities, enchanted forests, and futuristic strongholds while uncovering the secrets of the Chrono Nexus—an ancient time-bending artifact now controlled by the enigmatic Shadow Enclave. The project aims to deliver a living world that evolves through player action, story updates, and community-built settlements.

## Vision & Core Goals
- **Player-Driven Worldbuilding** – Empower players to shape settlements and kingdoms with flexible building systems and commissions.
- **Blended Magic & Technology** – Merge arcane spellcraft with inventive gadgets to create unique play styles and storytelling opportunities.
- **Ongoing Narrative Evolution** – Tell an expanding saga where choices ripple across regions, factions, and future content drops.
- **Collaborative Development** – Build an inclusive project that welcomes contributors who want to craft art, gameplay systems, narrative arcs, and tools.

## Key Features in Development
- **Arkitect Builder UI** that unlocks when players switch to the Builder class, offering tabs for plots, materials, and blueprints along with a commission board for player-made requests.
- **Dynamic Settlement Progression** from small farming communities into bustling player-governed kingdoms with real-time construction, markets, embassies, and kingdom laws.
- **Class & Profession Ecosystem** spanning combat, support, and crafting roles that interact with the world narrative and unlock quests.
- **Evolving Storyline** centered on the Chrono Nexus, branching choices, and a cast of pivotal allies and antagonists whose loyalties can shift.

## World Aesthetics & Atmosphere
Elysium juxtaposes vibrant natural biomes with futuristic architecture. Expect bioluminescent forests, crystalline mountain ranges, bustling metropolis skylines, and floating constructs of arcane machinery. Player-built structures seamlessly integrate with these landscapes, making each server feel like a bespoke fantasy magitech realm shaped by its inhabitants.

## Main Story Premise
The Shadow Enclave—led by former heroes—seizes the Chrono Nexus to rewrite history and avert a looming catastrophe. Players ally with figures such as Captain Varian Stormforge, Professor Seraphina Frostwind, and Xander Ironspark to expose conspiracies within Eldoria, confront temporal anomalies, and determine the future of Elysium. Story arcs are designed to continue through patches and expansions, keeping the world responsive to community choices.

### Featured Side Adventures
- **Forgotten Relics** – Hunt powerful artifacts that can alter the main quest or grant unique abilities.
- **The Cursed Library** – Aid Arcane Haven’s mages to lift an ancient curse and uncover forbidden knowledge.
- **Tech Hunt** – Assist Xander Ironspark in recovering lost technology to unlock gadget upgrades.
- **Time-Traveling Anomalies** – Resolve random temporal events that reshape zones and rewards.

## Playable Races
REALM celebrates diverse cultures with bespoke abilities, aesthetics, and lore hooks:
- **Humans** – Adaptable explorers thriving in diplomacy, innovation, and balanced class choices.
- **Felarians** – Agile feline hybrids attuned to nature and time magic, excelling as Rangers and Time Mages.
- **Crystallians** – Regal dragonkin with gemstone skin, capable of wielding heavy arms and technomancy.
- **Revenants** – Ethereal undead with necromantic affinities and stealthy prowess.
- **Gearlings** – Mechanical magi who blend runes and machinery, perfect for Technomancers, Wizards, and Builders.

### Race Class Access
Each race can unlock multiple classes, with exclusives noted below:
- **Human** – Warrior, Wizard, Sage, Rogue, Ranger.
- **Felarian** – Warrior, Wizard, Ranger, Time Mage (exclusive), Sage.
- **Crystallian** – Mythologist (exclusive), Warrior, Rogue, Sage.
- **Revenant** – Necromancer (exclusive), Warrior, Rogue, Wizard.
- **Gearling** – Technomancer (exclusive), Warrior, Sage, Wizard.

## Class Archetypes
Combat and support roles are designed to encourage party synergy and role swapping:
- **Warrior, Rogue, Ranger** – Physical damage, frontline control, and tactical positioning.
- **Wizard, Time Mage, Necromancer** – Arcane DPS, temporal manipulation, and death magic (exclusive to Revenants).
- **Technomancer, Sage** – Hybrid magitech support and primary healers.
- **Builder** – Non-combat specialists who construct settlements via the Arkitect UI.

Each class ties into specific stat profiles and equipment sets, with unlock quests embedded across story chapters.

### Professions & Crafting
Major and minor roles (Farmer, Gatherer, Blacksmith, Tailor, Carpenter, Painter, Mechanic) fuel the player economy, settlement growth, and unique questlines, ensuring every artisan has a meaningful impact on community infrastructure.

## Combat & Equipment Philosophy
REALM utilizes a class-based action combat system where positioning, skill combos, and cooperative tactics are key. Weapon categories span melee, ranged, and magical archetypes with subtypes like double sabers, boomerangs, mech-rods, and books. Armor slots cover full body gear plus accessories, emphasizing customization for both aesthetics and stat optimization. Consumables, spell scrolls, and key relics further deepen strategic choices.

## Notable Locations
- **Eldoria** – Central capital of trade and governance, home to the royal guard.
- **Arcane Haven** – Mystical Felarian city of scholars and rangers within the Eldros forests.
- **Nexus Outpost** – Futuristic Shadow Enclave stronghold surrounding the Chrono Nexus.

Expect additional story hubs, side-quest zones, and dynamic events that evolve alongside the community.

## How to Get Involved
We welcome collaborators across disciplines—designers, engineers, artists, writers, audio experts, community managers, and more. To contribute:
1. Review this README and the `Context/` documents for lore and system references.
2. Join discussions on our community channels to align on priorities and style guides.
3. Submit ideas, prototypes, or lore expansions through issues and pull requests.
4. Share feedback regularly so we can refine the world together.

REALM is built by fans of immersive MMOs who want to push the boundaries of player-driven storytelling and construction. If that resonates with you, we’d love your help forging the future of Elysium.

## Development Setup (Current)

### Backend API (Server/)

The backend in `Server/` provides authentication, realm membership, character progression, and realm
world-state persistence (terrain/plots/structures) for Realm2. Terrain changes are stored per realm chunk,
then fanned out through a change feed and real-time channels. Builders are the only role allowed to mutate
chunk/structure data, while all realm members can read snapshots and change feeds.

#### Prerequisites

- Node.js 18+
- npm 9+
- MariaDB 10.6+ (local or remote)

#### Install and configure

```bash
cd Server
npm install
npm run setup:local
```

Edit `Server/.env` (created from `.env.example`) with your MariaDB connection details. On startup, the
server runs migrations and seeds the default realms automatically.

#### Run the API

```bash
npm run dev
```

The API listens on <http://localhost:3000>. Swagger docs are served from `/docs` once the server is running.

#### World-state sync endpoints

- **Snapshot**: `GET /realms/:realmId/chunks` (optional `?since=` ISO timestamp)
- **Change feed**: `GET /realms/:realmId/chunks/changes` (optional `?since=` and `?limit=`)
- **Server-sent events**: `GET /realms/:realmId/chunks/stream`
- **WebSocket (chunks)**: `ws://localhost:3000/ws/chunks?token=JWT`
- **WebSocket (progression)**: `ws://localhost:3000/ws/progression?token=JWT&characterId=...`

#### Quality checks

```bash
npm run lint
npm test
```

### Unity Client

1. Install the Unity Editor version **6000.2.8f1** via Unity Hub.
2. Add the project by selecting the root of this repository (the directory containing `Assets/`, `Packages/`, and `ProjectSettings/`).
3. Open the project and allow Unity to import packages. Any missing package errors can typically be resolved by opening the Package Manager and clicking **Refresh**.
4. When developing gameplay features, keep scenes and assets grouped logically under `Assets/` to keep automated build validation fast.

### Procedural Character Assets

- A reusable script at `Tools/ProceduralModels/create_feline_character.py` procedurally sculpts a stylized Felarian (feline humanoid) body plan that can be imported directly into Unity.
- Run `python Tools/ProceduralModels/create_feline_character.py` to rebuild the mesh. The script exports `Assets/Resources/Models/FelineHumanoid.obj`, ensuring the project always has a clean source of truth for the mesh data.
- Modify the profile curves or proportions in the script to iterate on silhouettes without re-authoring meshes in an external DCC tool. The generated OBJ can be assigned materials, rigs, and prefabs like any other Unity model asset.
- Inside the Unity Editor, add the `CharacterMorphController` component (found under `Scripts/CharacterCustomization/`) to a prefab or scene instance of the Felarian. Each entry in the controller lets you assign a transform (abdomen, neck, limbs, etc.) and exposes height/width sliders so designers can dial in proportions without leaving the inspector. Use **Capture Current Scales** to treat the current mesh as the neutral pose, then tweak sliders to build race or class variants and save them as prefabs.
- Because this workflow manipulates standard transforms, it does **not** require compute shaders or blend shapes. However, the same concept can later be extended with Unity deformers, GPU blend-shape baking, or procedural animation graphs if we need higher fidelity morphs.

## Continuous Integration

Automated checks run for every push to `main` and on pull requests that modify backend or Unity content.

- **Backend CI** installs dependencies, runs ESLint, and executes the Jest unit tests.
- **Unity Build Validation** performs a headless Linux build using the Unity Builder action and uploads the resulting artifact.

### Required GitHub Secrets

Add the following repository secrets so the workflows can authenticate and connect to required services:

| Secret | Used By | Description |
| --- | --- | --- |
| `BACKEND_DATABASE_URL` | Backend CI | File path or connection string for the SQLite database used during automated tests. A value such as `./data/app.db` is sufficient for CI runs. |
| `BACKEND_JWT_SECRET` | Backend CI | Secret used to sign JWTs when the backend test suite runs. |
| `UNITY_LICENSE` | Unity Build Validation | Serialized Unity license (ULF) for the Unity Builder action. Generate this through the [GameCI licensing guide](https://game.ci/docs/github/activation). |

Enable branch protection on `main` so that pull requests require both workflows to succeed before merging. This guarantees that new features do not regress backend behavior or break Unity builds.
