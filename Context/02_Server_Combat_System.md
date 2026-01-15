Realm Combat Doc 02
Server Combat System (Authoritative)
Purpose
Define the server-side combat architecture that:
receives action requests
validates them against game rules
resolves hits/effects authoritatively
updates world state (hp/resources/status/cooldowns)
publishes results to clients
This doc is the server-side source-of-truth for validation, simulation, and replication.
Design Pillars
Server authoritative outcomes: damage/heals/status/cooldowns/resources are truth on server.
Unified action pipeline: attacks, specials, abilities all become validated action requests.
Data-driven behavior: server uses the same combat definitions (weapon/ability/effect assets) as the client, but in server format.
Deterministic resolution: for any action request, resolution depends on server world state, not client estimates.
Anti-cheat by design: client supplies targeting intent; server verifies feasibility.
Server Modules
CombatService (entry point)
Responsibilities
accept CombatActionRequest
route to validator
if accepted, execute through simulator
publish result events + update snapshots
Core function
HandleActionRequest(request, connectionContext)
CombatActionValidator
Validates the request against:
actor exists and is alive
actor is not in disallowed state (stunned, silenced, etc.)
class restrictions (weapon + ability)
cooldown ready
resources sufficient
request sequence is not stale/out-of-order (optional but recommended)
target validity for the action:
target exists, is targetable, in range
relation allowed (enemy/ally/self)
line-of-sight if required
ground point within range and in allowed area
Outputs
AcceptedActionTicket (contains normalized action data)
or Rejected with reason code
CombatSimulator
Executes the accepted action and produces a result.
Responsibilities
determine targets (single target, AoE list, cone list)
resolve hit checks
compute damage/heal values
apply mitigation/resists/crit rules
apply status effects and durations
spend resources
start cooldowns
apply knockback/pulls/etc. (movement authority rules)
Outputs
CombatActionResult event
updated authoritative state for involved entities
HitResolver
Handles spatial queries and hit feasibility.
Approach
Pick one baseline and lock it:
Baseline recommended for early Realm
Client sends targeting intent (targetId or direction/point)
Server computes actual target set using authoritative positions:
melee arc/cone based on attacker facing and weapon step hit shape
ranged line checks for projectile start
AoE radius around point
Core functions
ResolveMeleeArc(attacker, hitShape, maybeTargetId) -> List<EntityId>
ResolveCone(attacker, coneShape) -> List<EntityId>
ResolveAoERadius(center, radius) -> List<EntityId>
CheckRange(attacker, target, range) -> bool
CheckLoS(attacker, target) -> bool (if enabled)
EffectResolver
Applies effect lists from:
combo steps (weapon attacks)
weapon specials
abilities
Effect primitives
damage
heal
apply status
remove status
resource modify
spawn projectile (server-owned)
spawn AoE zone (server-owned)
crowd control (stun/root/silence)
displacement (knockback/pull) subject to movement authority rules
Important
EffectResolver is the only place that mutates HP/resources/status due to combat.
StatusEffectSystem
Responsibilities
track active statuses per entity (buffs/debuffs)
tick durations
apply periodic effects (DoT/HoT)
provide “can act” modifiers to validator/state
support stacking/refresh rules
CooldownResourceSystem
Responsibilities
maintain authoritative cooldown timers and charges
maintain authoritative resources (mana/stamina/etc.)
enforce global cooldown rules if you use them
send snapshots/deltas to clients
ThreatSystem (NPC)
Responsibilities
maintain threat tables per NPC
update threat on damage/heal/taunt
select current target for NPC AI
This can be minimal initially, but the hooks should exist so combat events can feed threat.
ReplicationPublisher
Responsibilities
publish CombatActionResult events to relevant clients
publish periodic snapshots for:
hp/resources
statuses
cooldowns
enforce interest management (nearby players)
Server Data Contracts
CombatActionRequest (Client → Server)
Fields:
actorId
sequenceNumber
clientTime (for debug/reconcile)
actionType:
BasicAttack
WeaponSpecial
Ability
actionId:
combo step id (for basic attack)
special id
ability id
equippedWeaponId (optional; server should know equipment, but this helps detect desync)
targetingSnapshot:
mode: None | Entity | Point | Direction
targetEntityId (optional)
worldPoint (optional)
aimDirection (optional)
CombatActionResult (Server → Clients)
Fields:
actorId
sequenceNumber
actionType
actionId
startedAtServerTime
outcome:
accepted/rejected + reason
hits[]:
targetId
resultType (damage/heal/miss/dodge/block/immune)
amount
crit bool
mitigationBreakdown (optional)
statusChanges[]:
applied/removed/stackChanged
resourceDeltas[]
cooldownChanges[]
spawnedProjectiles[] (if any)
spawnedZones[] (if any)
Authoritative Combat Rules (Baseline)
Range + Targeting
Server computes final target set using authoritative positions.
Hard target in request is treated as intent, not truth.
If target is invalid, server either:
rejects (strict)
or falls back to nearest valid in cone (lenient)
Pick one policy and keep consistent. Recommended: reject for targeted abilities; fallback for basic attacks.
Combo Integrity
Client says “I did step X”, but server ensures:
the step is valid for the weapon
the step is valid from previous step within time window (if server tracks combo state)
Two options:
Option A (recommended): Server tracks combo state
Server maintains per-actor combo node + expiry.
When action request arrives, server confirms the transition is valid.
Option B: Client-driven step with verification only
Server validates step exists in weapon definition, but doesn’t track sequence.
Easier, but less cheat-resistant.
Recommended: Option A, because combos are core to Realm identity.
Cooldowns + Resources
server checks, spends, starts cooldowns
client may show predicted cooldowns, but server snapshot corrects
Damage/Heal Formula (Server)
Lock in a consistent formula now. Example baseline:
Damage
base = effect.baseValue
scale = effect.statCoefficient * attackerStat(effect.scalingStat)
raw = base + scale
crit = raw * critMultiplier if roll succeeds
mitigated = ApplyDefenseAndResists(raw, targetStats, damageType)
clamp >= 0, apply shields first if you have them
Heal
raw = base + coefficient * healerStat
apply healing modifiers
clamp to target missing hp
Notes
define damage types: physical / magical / elemental types as needed
define mitigation model: armor/resist + level scaling
Server Implementation Phases
Phase 1: Melee online (combo + hit)
accept basic attack requests
validate equipped weapon and combo step
resolve melee arc hits
apply damage
publish results
Phase 2: Specials + casting
special availability tracked server-side
abilities (instant + cast-time)
status effects (buff/debuff, stuns, silences)
interrupts cancel cast
Phase 3: AoE + projectiles + PvP
ground targeting
server-owned zones (persistent AoE)
projectiles with server collision
PvP rules and safe zones
Server Deliverables (Codex Task List)
Define server combat request/result DTOs
Implement CombatService pipeline
Implement CombatActionValidator
Implement CombatSimulator
Implement HitResolver (melee arc + cone + AoE radius)
Implement EffectResolver (damage/heal/status/resource)
Implement StatusEffectSystem (durations + periodic ticks)
Implement CooldownResourceSystem
Implement ReplicationPublisher (events + snapshots)
Implement per-actor server combo tracking (Option A)