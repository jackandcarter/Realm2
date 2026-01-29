import { Router } from 'express';

export const devToolkitUiRouter = Router();

devToolkitUiRouter.get('/', (_req, res) => {
  res.type('html').send(`<!doctype html>
<html lang="en">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Realm Dev Toolkit</title>
    <style>
      :root {
        color-scheme: dark;
        font-family: "Inter", system-ui, -apple-system, sans-serif;
        background: #0f141b;
        color: #e6edf3;
      }
      * {
        box-sizing: border-box;
      }
      body {
        margin: 0;
        padding: 2rem;
      }
      main {
        max-width: 1100px;
        margin: 0 auto;
        display: grid;
        gap: 1.5rem;
      }
      header {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        justify-content: space-between;
        gap: 1rem;
      }
      h1 {
        margin: 0;
      }
      .panel {
        background: #141b24;
        padding: 1.5rem;
        border-radius: 16px;
        box-shadow: 0 0 0 1px rgba(255, 255, 255, 0.06);
      }
      nav {
        display: flex;
        flex-wrap: wrap;
        gap: 0.75rem;
      }
      button,
      select,
      input,
      textarea {
        background: #0f1722;
        border: 1px solid rgba(148, 163, 184, 0.2);
        color: inherit;
        padding: 0.5rem 0.65rem;
        border-radius: 10px;
        font-size: 0.95rem;
      }
      button {
        cursor: pointer;
        border-color: rgba(125, 211, 252, 0.5);
      }
      button.secondary {
        border-color: rgba(148, 163, 184, 0.3);
      }
      label {
        font-size: 0.85rem;
        color: #cbd5f5;
      }
      form {
        display: grid;
        gap: 0.75rem;
      }
      .grid-2 {
        display: grid;
        gap: 0.75rem;
        grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
      }
      .list {
        max-height: 300px;
        overflow: auto;
        padding-right: 0.5rem;
      }
      .list-item {
        padding: 0.5rem 0.75rem;
        border-radius: 10px;
        border: 1px solid rgba(148, 163, 184, 0.1);
        margin-bottom: 0.5rem;
      }
      .list-item strong {
        display: block;
      }
      .status {
        font-size: 0.85rem;
        color: #94a3b8;
      }
      .split {
        display: grid;
        gap: 1.5rem;
        grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
      }
      textarea {
        min-height: 110px;
        resize: vertical;
      }
      code {
        background: rgba(148, 163, 184, 0.15);
        padding: 0.15rem 0.35rem;
        border-radius: 6px;
      }
    </style>
  </head>
  <body>
    <main>
      <header>
        <div>
          <h1>Realm Dev Toolkit</h1>
          <p class="status">Server-backed authoring workspace for catalog content.</p>
        </div>
        <nav>
          <button class="secondary" data-tab="items">Items</button>
          <button class="secondary" data-tab="weapons">Weapons</button>
          <button class="secondary" data-tab="armor">Armor</button>
          <button class="secondary" data-tab="classes">Classes</button>
          <button class="secondary" data-tab="classStats">Class Stats</button>
          <button class="secondary" data-tab="abilities">Abilities</button>
          <button class="secondary" data-tab="types">Types</button>
        </nav>
      </header>

      <section class="panel" id="panel-items" data-panel="items">
        <h2>Items</h2>
        <div class="split">
          <form id="form-items">
            <div class="grid-2">
              <label>Id <input name="id" placeholder="item-id" required /></label>
              <label>Name <input name="name" placeholder="Item name" required /></label>
              <label>Category
                <select name="category" required>
                  <option value="weapon">weapon</option>
                  <option value="armor">armor</option>
                  <option value="consumable">consumable</option>
                  <option value="key-item">key-item</option>
                </select>
              </label>
              <label>Rarity
                <select name="rarity">
                  <option value="common">common</option>
                  <option value="starter">starter</option>
                  <option value="standard">standard</option>
                  <option value="rare">rare</option>
                  <option value="legendary">legendary</option>
                </select>
              </label>
              <label>Stack Limit <input name="stackLimit" type="number" min="1" value="1" /></label>
              <label>Icon URL <input name="iconUrl" placeholder="https://..." /></label>
            </div>
            <label>Description <textarea name="description"></textarea></label>
            <label>Metadata (JSON) <textarea name="metadata">{}</textarea></label>
            <div>
              <button type="submit">Save Item</button>
              <button type="button" class="secondary" data-action="refresh-items">Refresh List</button>
            </div>
          </form>
          <div>
            <h3>Item Catalog</h3>
            <div class="list" id="list-items"></div>
          </div>
        </div>
      </section>

      <section class="panel" id="panel-weapons" data-panel="weapons" hidden>
        <h2>Weapons</h2>
        <div class="split">
          <form id="form-weapons">
            <div class="grid-2">
              <label>Item Id <input name="itemId" required /></label>
              <label>Weapon Type <input name="weaponType" placeholder="weapon-type-id" required /></label>
              <label>Handedness
                <select name="handedness">
                  <option value="one-hand">one-hand</option>
                  <option value="two-hand">two-hand</option>
                  <option value="off-hand">off-hand</option>
                </select>
              </label>
              <label>Required Level <input name="requiredLevel" type="number" min="1" value="1" /></label>
              <label>Min Damage <input name="minDamage" type="number" min="0" value="0" /></label>
              <label>Max Damage <input name="maxDamage" type="number" min="0" value="0" /></label>
              <label>Attack Speed <input name="attackSpeed" type="number" step="0.1" value="1" /></label>
              <label>Range (m) <input name="rangeMeters" type="number" step="0.1" value="1" /></label>
              <label>Required Class Id <input name="requiredClassId" /></label>
            </div>
            <label>Metadata (JSON) <textarea name="metadata">{}</textarea></label>
            <div>
              <button type="submit">Save Weapon</button>
              <button type="button" class="secondary" data-action="refresh-weapons">Refresh List</button>
            </div>
          </form>
          <div>
            <h3>Weapon Catalog</h3>
            <div class="list" id="list-weapons"></div>
          </div>
        </div>
      </section>

      <section class="panel" id="panel-armor" data-panel="armor" hidden>
        <h2>Armor</h2>
        <div class="split">
          <form id="form-armor">
            <div class="grid-2">
              <label>Item Id <input name="itemId" required /></label>
              <label>Slot
                <select name="slot" required>
                  <option value="weapon">weapon</option>
                  <option value="head">head</option>
                  <option value="chest">chest</option>
                  <option value="legs">legs</option>
                  <option value="hands">hands</option>
                  <option value="feet">feet</option>
                  <option value="accessory">accessory</option>
                  <option value="tool">tool</option>
                </select>
              </label>
              <label>Armor Type
                <select name="armorType" required>
                  <option value="cloth">cloth</option>
                  <option value="leather">leather</option>
                  <option value="plate">plate</option>
                </select>
              </label>
              <label>Defense <input name="defense" type="number" min="0" value="0" /></label>
              <label>Required Level <input name="requiredLevel" type="number" min="1" value="1" /></label>
              <label>Required Class Id <input name="requiredClassId" /></label>
            </div>
            <label>Resistances (JSON) <textarea name="resistances">{}</textarea></label>
            <label>Metadata (JSON) <textarea name="metadata">{}</textarea></label>
            <div>
              <button type="submit">Save Armor</button>
              <button type="button" class="secondary" data-action="refresh-armor">Refresh List</button>
            </div>
          </form>
          <div>
            <h3>Armor Catalog</h3>
            <div class="list" id="list-armor"></div>
          </div>
        </div>
      </section>

      <section class="panel" id="panel-classes" data-panel="classes" hidden>
        <h2>Classes</h2>
        <div class="split">
          <form id="form-classes">
            <div class="grid-2">
              <label>Id <input name="id" required /></label>
              <label>Name <input name="name" required /></label>
              <label>Role
                <select name="role">
                  <option value="">none</option>
                  <option value="tank">tank</option>
                  <option value="damage">damage</option>
                  <option value="support">support</option>
                  <option value="builder">builder</option>
                </select>
              </label>
              <label>Resource Type
                <select name="resourceType">
                  <option value="">none</option>
                  <option value="mana">mana</option>
                  <option value="stamina">stamina</option>
                  <option value="energy">energy</option>
                </select>
              </label>
              <label>Starting Level <input name="startingLevel" type="number" min="1" value="1" /></label>
            </div>
            <label>Description <textarea name="description"></textarea></label>
            <label>Metadata (JSON) <textarea name="metadata">{}</textarea></label>
            <div>
              <button type="submit">Save Class</button>
              <button type="button" class="secondary" data-action="refresh-classes">Refresh List</button>
            </div>
          </form>
          <div>
            <h3>Class Catalog</h3>
            <div class="list" id="list-classes"></div>
          </div>
        </div>
      </section>

      <section class="panel" id="panel-classStats" data-panel="classStats" hidden>
        <h2>Class Base Stats</h2>
        <div class="split">
          <form id="form-classStats">
            <div class="grid-2">
              <label>Class Id <input name="classId" required /></label>
              <label>Base Health <input name="baseHealth" type="number" value="0" /></label>
              <label>Base Mana <input name="baseMana" type="number" value="0" /></label>
              <label>Strength <input name="strength" type="number" value="0" /></label>
              <label>Agility <input name="agility" type="number" value="0" /></label>
              <label>Intelligence <input name="intelligence" type="number" value="0" /></label>
              <label>Vitality <input name="vitality" type="number" value="0" /></label>
              <label>Defense <input name="defense" type="number" value="0" /></label>
              <label>Crit Chance <input name="critChance" type="number" step="0.1" value="0" /></label>
              <label>Speed <input name="speed" type="number" step="0.1" value="0" /></label>
            </div>
            <div>
              <button type="submit">Save Base Stats</button>
              <button type="button" class="secondary" data-action="refresh-classStats">Refresh List</button>
            </div>
          </form>
          <div>
            <h3>Base Stats Catalog</h3>
            <div class="list" id="list-classStats"></div>
          </div>
        </div>
      </section>

      <section class="panel" id="panel-abilities" data-panel="abilities" hidden>
        <h2>Abilities</h2>
        <div class="split">
          <form id="form-abilities">
            <div class="grid-2">
              <label>Id <input name="id" required /></label>
              <label>Name <input name="name" required /></label>
              <label>Ability Type <input name="abilityType" placeholder="ability-type-id" /></label>
              <label>Cooldown (s) <input name="cooldownSeconds" type="number" step="0.1" value="0" /></label>
              <label>Resource Cost <input name="resourceCost" type="number" value="0" /></label>
              <label>Range (m) <input name="rangeMeters" type="number" step="0.1" value="0" /></label>
              <label>Cast Time (s) <input name="castTimeSeconds" type="number" step="0.1" value="0" /></label>
            </div>
            <label>Description <textarea name="description"></textarea></label>
            <label>Metadata (JSON) <textarea name="metadata">{}</textarea></label>
            <div>
              <button type="submit">Save Ability</button>
              <button type="button" class="secondary" data-action="refresh-abilities">Refresh List</button>
            </div>
          </form>
          <div>
            <h3>Ability Catalog</h3>
            <div class="list" id="list-abilities"></div>
          </div>
        </div>
      </section>

      <section class="panel" id="panel-types" data-panel="types" hidden>
        <h2>Reference Types</h2>
        <div class="split">
          <div>
            <h3>Weapon Types</h3>
            <form id="form-weaponTypes">
              <div class="grid-2">
                <label>Id <input name="id" required /></label>
                <label>Display Name <input name="displayName" required /></label>
              </div>
              <div>
                <button type="submit">Save Weapon Type</button>
                <button type="button" class="secondary" data-action="refresh-weaponTypes">Refresh List</button>
              </div>
            </form>
            <div class="list" id="list-weaponTypes"></div>
          </div>
          <div>
            <h3>Ability Types</h3>
            <form id="form-abilityTypes">
              <div class="grid-2">
                <label>Id <input name="id" required /></label>
                <label>Display Name <input name="displayName" required /></label>
              </div>
              <div>
                <button type="submit">Save Ability Type</button>
                <button type="button" class="secondary" data-action="refresh-abilityTypes">Refresh List</button>
              </div>
            </form>
            <div class="list" id="list-abilityTypes"></div>
          </div>
        </div>
      </section>
    </main>

    <script>
      const apiBase = '/api/devtools';
      const panelButtons = document.querySelectorAll('[data-tab]');
      const panels = document.querySelectorAll('[data-panel]');
      const statusEl = document.querySelector('.status');

      function setStatus(message) {
        if (statusEl) {
          statusEl.textContent = message;
        }
      }

      function setPanel(target) {
        panels.forEach((panel) => {
          panel.hidden = panel.dataset.panel !== target;
        });
        panelButtons.forEach((button) => {
          button.classList.toggle('active', button.dataset.tab === target);
        });
      }

      panelButtons.forEach((button) => {
        button.addEventListener('click', () => setPanel(button.dataset.tab));
      });
      setPanel('items');

      async function requestJson(path, options) {
        const response = await fetch(path, options);
        if (!response.ok) {
          const payload = await response.json().catch(() => ({}));
          throw new Error(payload.message || 'Request failed');
        }
        return response.json();
      }

      function parseJsonField(value) {
        if (!value) return {};
        try {
          return JSON.parse(value);
        } catch (error) {
          throw new Error('Invalid JSON payload');
        }
      }

      function attachForm(formId, endpoint, handler) {
        const form = document.getElementById(formId);
        if (!form) return;
        form.addEventListener('submit', async (event) => {
          event.preventDefault();
          const formData = new FormData(form);
          try {
            const payload = handler(formData);
            await requestJson(\`\${apiBase}/\${endpoint}\`, {
              method: 'POST',
              headers: { 'Content-Type': 'application/json' },
              body: JSON.stringify(payload),
            });
            setStatus(\`\${endpoint} saved.\`);
          } catch (error) {
            setStatus(error.message);
          }
        });
      }

      function attachRefresh(action, endpoint, renderer) {
        document.querySelectorAll(\`[data-action="\${action}"]\`).forEach((button) => {
          button.addEventListener('click', () => loadList(endpoint, renderer));
        });
      }

      async function loadList(endpoint, renderer) {
        try {
          const data = await requestJson(\`\${apiBase}/\${endpoint}\`);
          renderer(data);
          setStatus(\`\${endpoint} refreshed.\`);
        } catch (error) {
          setStatus(error.message);
        }
      }

      function renderList(containerId, items, getLabel) {
        const container = document.getElementById(containerId);
        if (!container) return;
        container.innerHTML = '';
        items.forEach((item) => {
          const row = document.createElement('div');
          row.className = 'list-item';
          row.innerHTML = getLabel(item);
          container.appendChild(row);
        });
      }

      attachForm('form-items', 'items', (data) => ({
        id: data.get('id'),
        name: data.get('name'),
        category: data.get('category'),
        rarity: data.get('rarity'),
        stackLimit: Number(data.get('stackLimit') || 1),
        iconUrl: data.get('iconUrl') || null,
        description: data.get('description') || null,
        metadata: parseJsonField(data.get('metadata')),
      }));

      attachForm('form-weapons', 'weapons', (data) => ({
        itemId: data.get('itemId'),
        weaponType: data.get('weaponType'),
        handedness: data.get('handedness'),
        minDamage: Number(data.get('minDamage') || 0),
        maxDamage: Number(data.get('maxDamage') || 0),
        attackSpeed: Number(data.get('attackSpeed') || 1),
        rangeMeters: Number(data.get('rangeMeters') || 1),
        requiredLevel: Number(data.get('requiredLevel') || 1),
        requiredClassId: data.get('requiredClassId') || null,
        metadata: parseJsonField(data.get('metadata')),
      }));

      attachForm('form-armor', 'armor', (data) => ({
        itemId: data.get('itemId'),
        slot: data.get('slot'),
        armorType: data.get('armorType'),
        defense: Number(data.get('defense') || 0),
        requiredLevel: Number(data.get('requiredLevel') || 1),
        requiredClassId: data.get('requiredClassId') || null,
        resistances: parseJsonField(data.get('resistances')),
        metadata: parseJsonField(data.get('metadata')),
      }));

      attachForm('form-classes', 'classes', (data) => ({
        id: data.get('id'),
        name: data.get('name'),
        description: data.get('description') || null,
        role: data.get('role') || null,
        resourceType: data.get('resourceType') || null,
        startingLevel: Number(data.get('startingLevel') || 1),
        metadata: parseJsonField(data.get('metadata')),
      }));

      attachForm('form-classStats', 'class-base-stats', (data) => ({
        classId: data.get('classId'),
        baseHealth: Number(data.get('baseHealth') || 0),
        baseMana: Number(data.get('baseMana') || 0),
        strength: Number(data.get('strength') || 0),
        agility: Number(data.get('agility') || 0),
        intelligence: Number(data.get('intelligence') || 0),
        vitality: Number(data.get('vitality') || 0),
        defense: Number(data.get('defense') || 0),
        critChance: Number(data.get('critChance') || 0),
        speed: Number(data.get('speed') || 0),
      }));

      attachForm('form-abilities', 'abilities', (data) => ({
        id: data.get('id'),
        name: data.get('name'),
        description: data.get('description') || null,
        abilityType: data.get('abilityType') || null,
        cooldownSeconds: Number(data.get('cooldownSeconds') || 0),
        resourceCost: Number(data.get('resourceCost') || 0),
        rangeMeters: Number(data.get('rangeMeters') || 0),
        castTimeSeconds: Number(data.get('castTimeSeconds') || 0),
        metadata: parseJsonField(data.get('metadata')),
      }));

      attachForm('form-weaponTypes', 'weapon-types', (data) => ({
        id: data.get('id'),
        displayName: data.get('displayName'),
      }));

      attachForm('form-abilityTypes', 'ability-types', (data) => ({
        id: data.get('id'),
        displayName: data.get('displayName'),
      }));

      attachRefresh('refresh-items', 'items', (data) =>
        renderList('list-items', data.items || [], (item) =>
          \`<strong>\${item.name}</strong><span class="status">\${item.id} · \${item.category}</span>\`
        )
      );

      attachRefresh('refresh-weapons', 'weapons', (data) =>
        renderList('list-weapons', data.weapons || [], (weapon) =>
          \`<strong>\${weapon.itemId}</strong><span class="status">\${weapon.weaponType} · \${weapon.handedness}</span>\`
        )
      );

      attachRefresh('refresh-armor', 'armor', (data) =>
        renderList('list-armor', data.armor || [], (armor) =>
          \`<strong>\${armor.itemId}</strong><span class="status">\${armor.slot} · \${armor.armorType}</span>\`
        )
      );

      attachRefresh('refresh-classes', 'classes', (data) =>
        renderList('list-classes', data.classes || [], (entry) =>
          \`<strong>\${entry.name}</strong><span class="status">\${entry.id} · \${entry.role || 'n/a'}</span>\`
        )
      );

      attachRefresh('refresh-classStats', 'class-base-stats', (data) =>
        renderList('list-classStats', data.classBaseStats || [], (entry) =>
          \`<strong>\${entry.classId}</strong><span class="status">HP \${entry.baseHealth} · STR \${entry.strength}</span>\`
        )
      );

      attachRefresh('refresh-abilities', 'abilities', (data) =>
        renderList('list-abilities', data.abilities || [], (entry) =>
          \`<strong>\${entry.name}</strong><span class="status">\${entry.id} · cooldown \${entry.cooldownSeconds}s</span>\`
        )
      );

      attachRefresh('refresh-weaponTypes', 'weapon-types', (data) =>
        renderList('list-weaponTypes', data.weaponTypes || [], (entry) =>
          \`<strong>\${entry.displayName}</strong><span class="status">\${entry.id}</span>\`
        )
      );

      attachRefresh('refresh-abilityTypes', 'ability-types', (data) =>
        renderList('list-abilityTypes', data.abilityTypes || [], (entry) =>
          \`<strong>\${entry.displayName}</strong><span class="status">\${entry.id}</span>\`
        )
      );

      loadList('items', (data) =>
        renderList('list-items', data.items || [], (item) =>
          \`<strong>\${item.name}</strong><span class="status">\${item.id} · \${item.category}</span>\`
        )
      );
    </script>
  </body>
</html>`);
});
