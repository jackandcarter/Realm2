(() => {
  const defaultConfig = { API_BASE_URL: 'http://localhost:3000' };
  const runtimeConfig = window.REALM_PORTAL_CONFIG ?? {};
  const config = { ...defaultConfig, ...runtimeConfig };
  const apiBaseUrl = config.API_BASE_URL.replace(/\/$/, '');

  const form = document.getElementById('registration-form');
  const feedback = document.querySelector('.feedback');
  const submitButton = form.querySelector('button[type="submit"]');
  const yearEl = document.getElementById('year');
  if (yearEl) {
    yearEl.textContent = new Date().getFullYear().toString();
  }

  function setFeedback(message, type = 'info') {
    if (!feedback) return;
    feedback.textContent = message;
    feedback.classList.remove('feedback--success', 'feedback--error');
    if (type === 'success') {
      feedback.classList.add('feedback--success');
    } else if (type === 'error') {
      feedback.classList.add('feedback--error');
    }
  }

  function validateInputs(email, username, password) {
    const errors = [];
    if (!email) {
      errors.push('Email is required.');
    }

    if (!username) {
      errors.push('Username is required.');
    } else if (!/^[a-zA-Z0-9_\-]{3,20}$/.test(username)) {
      errors.push('Username must be 3-20 characters using letters, numbers, underscores, or hyphens.');
    }

    if (!password) {
      errors.push('Password is required.');
    } else {
      if (password.length < 8) {
        errors.push('Password must be at least 8 characters long.');
      }
      if (!/[A-Z]/.test(password)) {
        errors.push('Password must include an uppercase letter.');
      }
      if (!/[a-z]/.test(password)) {
        errors.push('Password must include a lowercase letter.');
      }
      if (!/[0-9]/.test(password)) {
        errors.push('Password must include a number.');
      }
      if (!/[^A-Za-z0-9]/.test(password)) {
        errors.push('Password must include a special character.');
      }
    }

    return errors;
  }

  async function handleSubmit(event) {
    event.preventDefault();
    setFeedback('');

    const formData = new FormData(form);
    const email = formData.get('email')?.toString().trim().toLowerCase();
    const username = formData.get('username')?.toString().trim();
    const password = formData.get('password')?.toString();

    const errors = validateInputs(email, username, password);
    if (errors.length > 0) {
      setFeedback(errors[0], 'error');
      return;
    }

    submitButton.disabled = true;
    submitButton.textContent = 'Creating account...';

    try {
      const response = await fetch(`${apiBaseUrl}/auth/register`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ email, username, password }),
      });

      if (!response.ok) {
        let errorMessage = 'Unable to create account. Please try again.';
        try {
          const errorBody = await response.json();
          if (errorBody?.message) {
            errorMessage = errorBody.message;
          }
        } catch (error) {
          // ignore json parse error
        }
        throw new Error(errorMessage);
      }

      const result = await response.json();
      const { user } = result;
      setFeedback(`Welcome, ${user.username}! Your account is ready.`, 'success');
      form.reset();
    } catch (error) {
      setFeedback(error.message, 'error');
    } finally {
      submitButton.disabled = false;
      submitButton.textContent = 'Create account';
    }
  }

  form.addEventListener('submit', handleSubmit);

  const abilityDesigner = document.getElementById('ability-designer');
  if (abilityDesigner) {
    const targetInputs = abilityDesigner.querySelectorAll('input[name="ability-target"]');
    const resourceTypeSelect = abilityDesigner.querySelector('#resource-type');
    const resourceCostInput = abilityDesigner.querySelector('#resource-cost');
    const cooldownInput = abilityDesigner.querySelector('#cooldown');
    const executionInputs = abilityDesigner.querySelectorAll('input[name="execution-condition"]');
    const customConditionInput = abilityDesigner.querySelector('#custom-condition');
    const effectTypeSelect = abilityDesigner.querySelector('#effect-type');
    const effectDescriptorInput = abilityDesigner.querySelector('#effect-descriptor');
    const effectMagnitudeInput = abilityDesigner.querySelector('#effect-magnitude');
    const effectDurationInput = abilityDesigner.querySelector('#effect-duration');
    const effectPriorityInput = abilityDesigner.querySelector('#effect-priority');
    const addEffectButton = abilityDesigner.querySelector('#add-effect');
    const effectForm = abilityDesigner.querySelector('#effect-form');
    const effectList = abilityDesigner.querySelector('.effect-list');
    const abilityValidation = abilityDesigner.querySelector('.ability-validation');
    const abilityPreview = abilityDesigner.querySelector('.ability-preview');
    const tooltipField = abilityDesigner.querySelector('#ability-tooltip');

    const TARGET_LABELS = {
      self: 'Self',
      ally: 'Allies',
      enemy: 'Enemies',
      area: 'Area (AoE)',
    };

    const CONDITION_LABELS = {
      lineOfSight: 'Requires line of sight',
      stationary: 'Caster must be stationary',
      comboWindow: 'Only during combo window',
      resourceThreshold: 'Requires resource threshold',
    };

    const EFFECT_LIBRARY = {
      damage: { label: 'Damage', allowedTargets: ['enemy', 'area'], requiresSingleTarget: false },
      heal: { label: 'Heal', allowedTargets: ['ally', 'self', 'area'], requiresSingleTarget: false },
      buff: { label: 'Buff', allowedTargets: ['ally', 'self'], requiresSingleTarget: true },
      debuff: { label: 'Debuff', allowedTargets: ['enemy', 'area'], requiresSingleTarget: false },
      state: { label: 'State change', allowedTargets: ['ally', 'self', 'enemy'], requiresSingleTarget: true },
    };

    const abilityState = {
      targets: new Set(),
      resource: {
        type: resourceTypeSelect?.value ?? 'Mana',
        cost: parseNumber(resourceCostInput?.value, 0),
        cooldown: parseNumber(cooldownInput?.value, 0),
      },
      conditions: {
        toggles: new Set(),
        custom: customConditionInput?.value.trim() ?? '',
      },
      effects: [],
    };

    let effectIdCounter = 0;

    function parseNumber(value, fallback = 0) {
      const parsed = Number.parseFloat(value ?? '');
      return Number.isFinite(parsed) ? parsed : fallback;
    }

    function formatValue(value, precision = 1) {
      if (!Number.isFinite(value)) {
        return '0';
      }
      if (precision === 0 || Number.isInteger(value)) {
        return Math.round(value).toString();
      }
      return value.toFixed(precision).replace(/\.0+$/, '').replace(/(\.\d*[1-9])0+$/, '$1');
    }

    function buildEffectSummary(effect) {
      const descriptor = effect.descriptor ? effect.descriptor.trim() : '';
      const durationPart = effect.duration > 0 ? ` for ${formatValue(effect.duration, 1)}s` : '';
      const magnitudePart = effect.magnitude > 0 ? formatValue(effect.magnitude, 0) : '';
      switch (effect.type) {
        case 'damage':
          return `Deals ${magnitudePart || '0'} damage${descriptor ? ` (${descriptor})` : ''}${durationPart}`;
        case 'heal':
          return `Restores ${magnitudePart || '0'} health${descriptor ? ` (${descriptor})` : ''}${durationPart}`;
        case 'buff': {
          const name = descriptor || 'a buff';
          const magText = magnitudePart ? ` (+${magnitudePart})` : '';
          return `Grants ${name}${magText}${durationPart}`;
        }
        case 'debuff': {
          const name = descriptor || 'a debuff';
          const magText = magnitudePart ? ` (${magnitudePart})` : '';
          return `Inflicts ${name}${magText}${durationPart}`;
        }
        case 'state': {
          const name = descriptor ? ` ${descriptor}` : '';
          const magText = magnitudePart ? ` (${magnitudePart})` : '';
          return `Applies state change${name}${magText}${durationPart}`;
        }
        default:
          return 'Custom effect';
      }
    }

    function syncTargetsFromInputs() {
      abilityState.targets.clear();
      targetInputs.forEach((input) => {
        if (input.checked) {
          abilityState.targets.add(input.value);
        }
      });
      refreshDesigner();
    }

    function syncConditionsFromInputs() {
      abilityState.conditions.toggles.clear();
      executionInputs.forEach((input) => {
        if (input.checked) {
          abilityState.conditions.toggles.add(input.value);
        }
      });
      refreshDesigner();
    }

    function refreshDesigner() {
      renderEffects();
      const errors = validateAbility();
      renderValidation(errors);
      renderPreview();
      updateTooltip(errors);
    }

    function validateAbility() {
      const errors = [];
      const hasArea = abilityState.targets.has('area');
      const primaryTargets = ['self', 'ally', 'enemy'].filter((target) => abilityState.targets.has(target));

      if (abilityState.targets.size === 0) {
        errors.push('Select at least one targeting option.');
      }

      if (hasArea && primaryTargets.length === 0) {
        errors.push('Area targeting requires a primary target (self, ally, or enemy).');
      }

      if (!Number.isFinite(abilityState.resource.cost) || abilityState.resource.cost < 0) {
        errors.push('Resource cost must be zero or greater.');
      }

      if (!Number.isFinite(abilityState.resource.cooldown) || abilityState.resource.cooldown < 0) {
        errors.push('Cooldown must be zero or greater.');
      }

      if (abilityState.effects.length === 0) {
        errors.push('Add at least one effect node to define the ability.');
      }

      abilityState.effects.forEach((effect, index) => {
        const def = EFFECT_LIBRARY[effect.type];
        if (!def) {
          errors.push(`Effect #${index + 1} has an unknown type.`);
          return;
        }

        if (!Number.isFinite(effect.magnitude) || effect.magnitude < 0) {
          errors.push(`${def.label} effect #${index + 1} requires a non-negative magnitude.`);
        }

        if (!Number.isFinite(effect.duration) || effect.duration < 0) {
          errors.push(`${def.label} effect #${index + 1} requires a non-negative duration.`);
        }

        if (!Number.isFinite(effect.priority) || effect.priority < 1) {
          errors.push(`${def.label} effect #${index + 1} must have a priority of at least 1.`);
        }

        const allowedTargets = new Set(def.allowedTargets);
        const matchesTarget = Array.from(abilityState.targets).some((target) => {
          if (target === 'area') {
            return allowedTargets.has('area');
          }
          return allowedTargets.has(target);
        });

        if (!matchesTarget) {
          errors.push(`${def.label} effect #${index + 1} cannot be combined with the selected targets.`);
        }

        if (def.requiresSingleTarget && hasArea) {
          errors.push(`${def.label} effect #${index + 1} requires a single target and is incompatible with area targeting.`);
        }
      });

      return errors;
    }

    function renderValidation(errors) {
      if (!abilityValidation) {
        return;
      }

      abilityValidation.classList.remove('ability-validation--error', 'ability-validation--ok');
      abilityValidation.innerHTML = '';

      if (errors.length === 0) {
        abilityValidation.classList.add('ability-validation--ok');
        abilityValidation.textContent = 'Ability is valid and ready to share.';
        return;
      }

      abilityValidation.classList.add('ability-validation--error');
      const intro = document.createElement('span');
      intro.textContent = 'Resolve the following issues:';
      const list = document.createElement('ul');
      errors.forEach((error) => {
        const item = document.createElement('li');
        item.textContent = error;
        list.appendChild(item);
      });
      abilityValidation.appendChild(intro);
      abilityValidation.appendChild(list);
    }

    function renderEffects() {
      if (!effectList) {
        return;
      }

      effectList.innerHTML = '';

      if (abilityState.effects.length === 0) {
        const empty = document.createElement('li');
        empty.className = 'effect-empty';
        empty.textContent = 'No effect nodes configured yet.';
        effectList.appendChild(empty);
        return;
      }

      abilityState.effects.forEach((effect, index) => {
        const def = EFFECT_LIBRARY[effect.type] ?? { label: 'Custom' };
        const item = document.createElement('li');
        item.className = 'effect-item';
        item.dataset.effectId = effect.id;

        const header = document.createElement('div');
        header.className = 'effect-header';
        const label = document.createElement('span');
        label.className = 'effect-label';
        label.textContent = `${index + 1}. ${def.label}`;
        header.appendChild(label);

        const meta = document.createElement('span');
        meta.className = 'effect-meta';
        const metaParts = [`Priority ${Math.round(effect.priority)}`];
        if (effect.duration > 0) {
          metaParts.push(`${formatValue(effect.duration, 1)}s duration`);
        }
        meta.textContent = metaParts.join(' • ');
        header.appendChild(meta);

        const description = document.createElement('p');
        description.textContent = `${buildEffectSummary(effect)}.`;

        const actions = document.createElement('div');
        actions.className = 'effect-actions';

        const upButton = document.createElement('button');
        upButton.type = 'button';
        upButton.dataset.action = 'up';
        upButton.textContent = 'Move up';
        actions.appendChild(upButton);

        const downButton = document.createElement('button');
        downButton.type = 'button';
        downButton.dataset.action = 'down';
        downButton.textContent = 'Move down';
        actions.appendChild(downButton);

        const removeButton = document.createElement('button');
        removeButton.type = 'button';
        removeButton.dataset.action = 'delete';
        removeButton.textContent = 'Remove';
        actions.appendChild(removeButton);

        item.appendChild(header);
        item.appendChild(description);
        item.appendChild(actions);
        effectList.appendChild(item);
      });
    }

    function buildTargetSummaryText() {
      const order = ['self', 'ally', 'enemy'];
      const primary = order.filter((target) => abilityState.targets.has(target));
      const hasArea = abilityState.targets.has('area');

      if (primary.length === 0 && !hasArea) {
        return 'No targets selected';
      }

      const base = primary.length
        ? primary.map((target) => TARGET_LABELS[target] ?? target).join(', ')
        : 'Unspecified primary target';

      if (hasArea) {
        return primary.length ? `${base} with area coverage` : 'Area-of-effect (needs a primary target)';
      }

      return base;
    }

    function buildResourceSummaryText() {
      const parts = [];
      const cost = Number.isFinite(abilityState.resource.cost)
        ? Math.max(0, abilityState.resource.cost)
        : 0;
      parts.push(`${formatValue(cost, 0)} ${abilityState.resource.type}`);
      const cooldown = Number.isFinite(abilityState.resource.cooldown)
        ? Math.max(0, abilityState.resource.cooldown)
        : 0;
      parts.push(cooldown > 0 ? `${formatValue(cooldown, 1)}s cooldown` : 'No cooldown');
      return parts.join(' • ');
    }

    function buildConditionSummaryText() {
      const toggles = Array.from(abilityState.conditions.toggles).map(
        (value) => CONDITION_LABELS[value] ?? value
      );
      const custom = abilityState.conditions.custom?.trim();
      if (custom) {
        toggles.push(custom);
      }
      if (toggles.length === 0) {
        return 'No additional requirements';
      }
      return toggles.join(' • ');
    }

    function renderPreview() {
      if (!abilityPreview) {
        return;
      }

      abilityPreview.innerHTML = '';

      const makeLine = (labelText, valueText) => {
        const paragraph = document.createElement('p');
        const strong = document.createElement('strong');
        strong.textContent = labelText;
        paragraph.appendChild(strong);
        paragraph.appendChild(document.createTextNode(' '));
        paragraph.appendChild(document.createTextNode(valueText));
        abilityPreview.appendChild(paragraph);
      };

      makeLine('Targeting:', buildTargetSummaryText());
      makeLine('Resource:', buildResourceSummaryText());
      makeLine('Execution:', buildConditionSummaryText());

      const effectsHeading = document.createElement('p');
      const effectsLabel = document.createElement('strong');
      effectsLabel.textContent = 'Effect order:';
      effectsHeading.appendChild(effectsLabel);
      abilityPreview.appendChild(effectsHeading);

      if (abilityState.effects.length === 0) {
        const placeholder = document.createElement('p');
        placeholder.className = 'effect-empty';
        placeholder.textContent = 'Add effect nodes to define how the ability resolves.';
        abilityPreview.appendChild(placeholder);
        return;
      }

      const list = document.createElement('ol');
      list.className = 'preview-effects';
      abilityState.effects.forEach((effect) => {
        const item = document.createElement('li');
        item.textContent = `${buildEffectSummary(effect)}.`;
        list.appendChild(item);
      });
      abilityPreview.appendChild(list);
    }

    function buildTooltip(errors) {
      if (errors.length > 0) {
        return 'Resolve validation issues to generate a tooltip preview.';
      }

      const baseTargets = ['self', 'ally', 'enemy'].filter((target) => abilityState.targets.has(target));
      const hasArea = abilityState.targets.has('area');
      const targetDescriptor = (() => {
        if (baseTargets.length === 0 && hasArea) {
          return 'Area ability';
        }
        if (baseTargets.length === 0) {
          return 'Untargeted ability';
        }
        if (baseTargets.length === 1) {
          const label = TARGET_LABELS[baseTargets[0]] ?? baseTargets[0];
          return hasArea ? `${label} (AoE) ability` : `${label} ability`;
        }
        const combined = baseTargets.map((target) => TARGET_LABELS[target] ?? target).join(' & ');
        return hasArea ? `AoE ${combined.toLowerCase()} ability` : `${combined} ability`;
      })();

      const cost = Number.isFinite(abilityState.resource.cost)
        ? Math.max(0, abilityState.resource.cost)
        : 0;
      const cooldown = Number.isFinite(abilityState.resource.cooldown)
        ? Math.max(0, abilityState.resource.cooldown)
        : 0;

      const lines = [];
      lines.push(`${targetDescriptor}.`);
      const resourceParts = [`Cost: ${formatValue(cost, 0)} ${abilityState.resource.type}`];
      resourceParts.push(
        cooldown > 0 ? `${formatValue(cooldown, 1)}s cooldown` : 'No cooldown'
      );
      lines.push(`${resourceParts.join(' • ')}.`);

      if (abilityState.effects.length > 0) {
        const effectSummaries = abilityState.effects.map((effect) => buildEffectSummary(effect));
        lines.push(`Effects: ${effectSummaries.join('; ')}.`);
      }

      const conditions = buildConditionSummaryText();
      if (conditions && conditions !== 'No additional requirements') {
        lines.push(`Requires: ${conditions}.`);
      }

      return lines.join('\n');
    }

    function updateTooltip(errors) {
      if (!tooltipField) {
        return;
      }
      tooltipField.value = buildTooltip(errors);
    }

    function addEffectFromForm() {
      const type = effectTypeSelect?.value;
      if (!type || !EFFECT_LIBRARY[type]) {
        return;
      }

      const effect = {
        id: `effect-${++effectIdCounter}`,
        type,
        descriptor: effectDescriptorInput?.value ?? '',
        magnitude: parseNumber(effectMagnitudeInput?.value, 0),
        duration: Math.max(0, parseNumber(effectDurationInput?.value, 0)),
        priority: Math.max(1, Math.round(parseNumber(effectPriorityInput?.value, 1))),
      };

      abilityState.effects.push(effect);
      refreshDesigner();

      if (effectDescriptorInput) {
        effectDescriptorInput.value = '';
      }
      if (effectDurationInput) {
        effectDurationInput.value = formatValue(effect.duration, 1);
      }
      if (effectPriorityInput) {
        effectPriorityInput.value = Math.round(effect.priority + 1);
      }
    }

    if (effectForm) {
      effectForm.addEventListener('submit', (event) => event.preventDefault());
    }

    if (addEffectButton) {
      addEffectButton.addEventListener('click', addEffectFromForm);
    }

    if (effectList) {
      effectList.addEventListener('click', (event) => {
        const button = event.target.closest('button[data-action]');
        if (!button) {
          return;
        }
        const action = button.dataset.action;
        const item = button.closest('li[data-effect-id]');
        if (!item) {
          return;
        }
        const id = item.dataset.effectId;
        const index = abilityState.effects.findIndex((effect) => effect.id === id);
        if (index === -1) {
          return;
        }

        if (action === 'delete') {
          abilityState.effects.splice(index, 1);
          refreshDesigner();
          return;
        }

        if (action === 'up' && index > 0) {
          const temp = abilityState.effects[index - 1];
          abilityState.effects[index - 1] = abilityState.effects[index];
          abilityState.effects[index] = temp;
          refreshDesigner();
          return;
        }

        if (action === 'down' && index < abilityState.effects.length - 1) {
          const temp = abilityState.effects[index + 1];
          abilityState.effects[index + 1] = abilityState.effects[index];
          abilityState.effects[index] = temp;
          refreshDesigner();
        }
      });
    }

    targetInputs.forEach((input) => {
      if (input.checked) {
        abilityState.targets.add(input.value);
      }
      input.addEventListener('change', syncTargetsFromInputs);
    });

    executionInputs.forEach((input) => {
      if (input.checked) {
        abilityState.conditions.toggles.add(input.value);
      }
      input.addEventListener('change', syncConditionsFromInputs);
    });

    if (customConditionInput) {
      abilityState.conditions.custom = customConditionInput.value.trim();
      customConditionInput.addEventListener('input', () => {
        abilityState.conditions.custom = customConditionInput.value.trim();
        refreshDesigner();
      });
    }

    if (resourceTypeSelect) {
      resourceTypeSelect.addEventListener('change', () => {
        abilityState.resource.type = resourceTypeSelect.value;
        refreshDesigner();
      });
    }

    if (resourceCostInput) {
      resourceCostInput.addEventListener('input', () => {
        abilityState.resource.cost = parseNumber(resourceCostInput.value, 0);
        refreshDesigner();
      });
    }

    if (cooldownInput) {
      cooldownInput.addEventListener('input', () => {
        abilityState.resource.cooldown = parseNumber(cooldownInput.value, 0);
        refreshDesigner();
      });
    }

    refreshDesigner();
  }

})();
