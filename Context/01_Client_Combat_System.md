Realm Combat Doc 01
Client Combat System (Unity)
Purpose
Define the Unity-side runtime combat architecture that:
	•	reads player input
	•	handles targeting
	•	runs combo chains (light/medium/heavy)
	•	runs ability casting (melee/ranged/magic/heal)
	•	drives animations and VFX
	•	communicates combat requests to the server
	•	applies server-confirmed results (and optionally predicts feel)
This doc is the client-side source-of-truth for managers, states, flow, and responsibilities.

Design Pillars
	1	Asset-driven: weapons/abilities authored in toolset are consumed at runtime; combat behavior is not hardcoded.
	2	Unified action pipeline: all combat interactions become a CombatActionRequest.
	3	Weapon combos are state graphs: weapon defines L/M/H transitions and timings.
	4	Targeting is a system: not “sprinkled” logic; it provides target data to action requests.
	5	Server authoritative: client may predict visuals, but server confirms damage/heal/status/cooldowns/resources.

Client Runtime Modules
PlayerCombatController (root orchestrator)
Owns:
	•	CombatStateMachine
	•	ComboSystem
	•	AbilitySystem
	•	ResourceClientModel (client-side mirror)
	•	CooldownClientModel (client-side mirror)
	•	TargetingSystem
	•	AnimationCombatDriver
	•	CombatNetClient
Responsibilities
	•	convert input into “intent” requests
	•	ask state machine if requests can start
	•	call combo/ability subsystems
	•	dispatch CombatActionRequest to server
	•	apply server results to local presentation (health bars, floaters, VFX triggers)

CombatInputRouter
Responsibilities
	•	map player input to combat intents:
	◦	Light / Medium / Heavy
	◦	Special
	◦	Ability hotbar buttons (1–0, mouse buttons, etc.)
	◦	Target select / target cycle / clear target
	◦	Ground-target confirm / cancel
	•	produce clean events:
	◦	OnAttackPressed(AttackType)
	◦	OnSpecialPressed()
	◦	OnAbilityPressed(slotIndex)
	◦	OnTargetCycle(), OnTargetClear()
	◦	OnGroundTargetConfirm(point), OnGroundTargetCancel()
Rule Input does not execute combat directly. It always routes through PlayerCombatController.

TargetingSystem
Responsibilities
	•	maintain targeting mode and selected target (hard lock)
	•	provide action-time target data (soft targeting)
	•	provide aim direction / reticle ray / ground point for ground-target abilities
	•	surface target UI data (nameplate highlight, outline, target frame)
Supported target modes
	•	Hard Lock Target: persistent target ID until cleared or invalid.
	•	Soft Target: computed at action-time (nearest in cone, nearest to reticle).
	•	Ground Target: obtains world point for AoE placement.
	•	No Target: self/forward-only actions.
Core functions
	•	GetHardTargetId() : EntityId?
	•	TryGetSoftTarget(SoftTargetQuery) : EntityId?
	•	TryGetGroundPoint(out Vector3 point) : bool
	•	GetAimDirection() : Vector3
	•	IsTargetValid(EntityId, TargetConstraints) : bool
Target constraints examples
	•	range
	•	LOS (client can approximate; server re-validates)
	•	faction/team relationship
	•	alive/targetable flags

CombatStateMachine
Purpose Gate what actions can be started right now; enforce recovery windows; handle casting/channeling; handle interrupts.
State list
	•	Idle
	•	BasicAttack (combo step in progress)
	•	Casting (cast time in progress)
	•	Channeling (channel ticks in progress)
	•	Recovery (global or per-action lockout)
	•	Disabled overlays (stun, knockdown, silence) applied by statuses
Key rules
	•	Inputs can be buffered:
	◦	next combo input buffered during continuation window
	◦	queued ability buffered during late recovery
	•	State machine decides if:
	◦	starting an action is allowed
	◦	buffering is allowed
	◦	interrupts are applied
Core functions
	•	CanStartAction(ActionCategory category, ActionRequirements req) : bool
	•	TryBufferAction(BufferedAction action) : bool
	•	StartAction(LocalActionTicket ticket)
	•	EndAction(LocalActionTicket ticket)
	•	Interrupt(InterruptReason reason)

ComboSystem
Purpose Given a weapon combat definition, handle L/M/H sequences and determine which combo step to execute next.
Owned data
	•	current ComboNodeId (or null = idle)
	•	last input time
	•	sequence history for special conditions (bounded list)
	•	special availability flag
	•	“combo window ends at” timestamps
Inputs
	•	AttackType.Light / Medium / Heavy
Outputs
	•	next ComboStep (contains animation key + hit definition + timing windows)
	•	“special available changed” events for UI
Core functions
	•	TryAdvanceCombo(AttackType input, float now, out ComboStep step) : bool
	•	ResetCombo()
	•	IsSpecialAvailable() : bool
	•	ConsumeSpecial(out WeaponSpecialAction special) : bool
Timing Combo step exposes:
	•	ContinueWindowStartNormalized
	•	ContinueWindowEndNormalized
	•	CancelableIntoDodgeStart/End
	•	CancelableIntoAbilityStart/End
	•	HitEvents (one or more hit moments)
The AnimationCombatDriver emits normalized time events so combo windows can be evaluated accurately.

AbilitySystem
Purpose Manage ability selection (hotbar), cooldowns, costs, cast/channel lifecycle, and generate server action requests.
Owned
	•	ability loadout (slot → ability asset id)
	•	cooldown tracker (client mirror)
	•	casting controller (cast bar timing / UI)
	•	channeling controller (tick schedule / UI)
Core functions
	•	TryStartAbility(slotIndex, TargetingSnapshot target) : AbilityStartResult
	•	CancelCasting()
	•	OnServerCooldownSync(CooldownSnapshot snapshot)
	•	OnServerResourceSync(ResourceSnapshot snapshot)
Casting model support
	•	instant
	•	cast time (interruptible)
	•	channel (interruptible; tick events)
Client prediction
	•	start animation and cast bar immediately on local approval
	•	server can reject; if rejected, stop animations/cast bar and show “failed” feedback

AnimationCombatDriver
Purpose Bridge gameplay and animation without hard-coding timings.
Responsibilities
	•	play animation by key (from combo step or ability)
	•	emit events:
	◦	OnHitEvent(hitIndex)
	◦	OnContinueWindowOpened/Closed
	◦	OnCancelableIntoAbilityOpened/Closed
	◦	OnActionAnimationComplete
	•	optionally spawn client-only VFX early (with server confirmation later)
Rule Damage is not applied by animation events. Animation events are used to time requests and visuals.

CombatUIController
Responsibilities
	•	hotbar display and cooldown overlays
	•	special button state
	•	cast bar (casting/channeling)
	•	target frame (health/name/status)
	•	feedback: “out of range”, “no target”, “silenced”, “cooldown”, etc.
UI inputs
	•	events from TargetingSystem, ComboSystem, AbilitySystem, and server results

CombatNetClient
Responsibilities
	•	send CombatActionRequest to server
	•	receive:
	◦	accepted/rejected
	◦	action result events (hits, heals, statuses)
	◦	snapshots for hp/resources/cooldowns/status lists
	•	route server confirmations to systems
Client request data
	•	sequenceNumber increasing per actor
	•	clientTime for reconciliation
	•	targetingSnapshot (targetId or point or direction)

Unified Client Action Flow
A) Basic Attacks (Light/Medium/Heavy)
	1	Player presses L/M/H.
	2	CombatInputRouter → PlayerCombatController.OnAttackPressed(type)
	3	Controller obtains WeaponCombatDefinition from equipped weapon.
	4	Controller asks CombatStateMachine.CanStartAction(BasicAttack, req)
	5	ComboSystem.TryAdvanceCombo(type, now, out step)
	6	AnimationCombatDriver.Play(step.animationKey)
	7	Controller builds CombatActionRequest:
	◦	ActionType = BasicAttack
	◦	ActionId = step.stepId
	◦	WeaponId
	◦	TargetingSnapshot (hard target if exists, else aim direction)
	8	CombatNetClient.Send(request)
	9	Server returns CombatActionResult (hits/status/resource deltas)
	10	Client applies result to UI + spawns confirmed VFX.
B) Special Attack
	1	ComboSystem signals SpecialAvailable = true
	2	UI lights “Special”.
	3	Player presses Special.
	4	PlayerCombatController calls ComboSystem.ConsumeSpecial(out special)
	5	State machine gates; animation triggers.
	6	Send request:
	◦	ActionType = WeaponSpecial
	◦	ActionId = special.specialId (or ability-like id)
	7	Apply server results.
C) Abilities
	1	Player presses hotbar slot.
	2	AbilitySystem.TryStartAbility(slot, targetSnapshot)
	3	If valid locally:
	◦	show cast bar / play cast anim (instant/cast/channel)
	◦	send CombatActionRequest with ActionType = Ability, ActionId = abilityId
	4	If server rejects:
	◦	cancel cast
	◦	UI feedback.

Client Data: TargetingSnapshot
A client must always send some targeting data. Even “no target” uses direction.
TargetingSnapshot fields:
	•	mode: None | Entity | Point | Direction
	•	targetEntityId (optional)
	•	worldPoint (optional)
	•	aimDirection (optional)
	•	clientCameraPos (optional for verification heuristics)
	•	rangeHint (optional, used for UI only)
Server re-validates everything.

Client Integration With Your Editor Toolset
Your toolset already generates:
	•	weapons
	•	armor
	•	stats
	•	abilities
The client runtime must consume them via:
	•	EquipmentSystem → provides EquippedWeaponId
	•	WeaponDatabase → fetch WeaponCombatDefinition
	•	AbilityLoadoutSystem → slot → AbilityDefinition
	•	StatAggregator → effective stats for UI display (server is truth)
Rule Client can compute “expected” outcomes for tooltips, but not final damage.

Minimum “Vertical Slice” Milestone
To be considered “combat online” on client:
	•	hard lock targeting + soft target fallback
	•	L/M/H combo chain executes animations and sends requests
	•	special lights up and sends request
	•	at least one instant ability + one cast-time ability
	•	server results reflected in UI (health changes, floaters, basic statuses)

Client Deliverables (Codex Task List)
	1	Implement TargetingSystem (hard lock + soft cone + ground point)
	2	Implement CombatStateMachine (idle/basic/casting/channel/recovery + buffering)
	3	Implement ComboSystem consuming WeaponCombatDefinition graph
	4	Implement AbilitySystem consuming AbilityDefinition assets
	5	Implement AnimationCombatDriver event callbacks
	6	Implement CombatNetClient request/response wiring
	7	Implement CombatUIController (hotbar, special, cast bar, target frame)
