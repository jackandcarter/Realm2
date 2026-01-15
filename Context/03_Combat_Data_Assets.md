Realm Combat Doc 03
Combat Data Assets (Definitions + Authoring Rules)
Purpose
Define the asset types (ScriptableObjects / data definitions) required to author combat content using your Unity editor tools, and the strict rules for how runtime systems consume these assets.
This doc is the data-layer source-of-truth for weapon combos, specials, abilities, effects, and restrictions.
Design Pillars
Everything is data: combos, hit shapes, costs, cooldowns, effects.
Stable IDs: every weapon/ability/step/effect must have a stable ID for networking and persistence.
Separation of concerns:
Weapon defines combos + special
Ability defines casting + effects
Effect primitives define outcomes
Server uses the same definitions (exported/serialized form) to avoid mismatch.
Required Asset Types
1) WeaponDefinition
Represents an equipable weapon item.
Fields
weaponId (stable string or GUID)
displayName
weaponType (Sword, Axe, Staff, Bow, etc.)
allowedClasses[]
baseStats (damage range, speed, crit, etc.)
combatDefinition : WeaponCombatDefinition
specialDefinition : WeaponSpecialDefinition (or reference to AbilityDefinition)
Rules
weaponId must never change once released.
combatDefinition must exist for all weapons that can attack.
2) WeaponCombatDefinition
Defines how the weapon performs basic attacks and combos.
2.1 Combo Graph
Define a directed graph of combo steps.
ComboNode
nodeId (stable)
step : ComboStepDefinition
ComboEdge
fromNodeId
input (Light / Medium / Heavy)
toNodeId
optional conditions (hitConfirmedRequired, staminaMin, etc.)
Rules
A weapon must define a starting node for each input OR a single universal start node.
Graph must not contain invalid references.
Tools should validate graph integrity.
2.2 ComboStepDefinition
A single attack step.
Fields
stepId (stable)
animationKey
damageProfile (references EffectList or DamageEffect)
hitShapes[] (one or multiple hit events)
timing
continueWindowStartNormalized
continueWindowEndNormalized
cancelIntoAbilityStartNormalized
cancelIntoAbilityEndNormalized
cancelIntoDodgeStartNormalized
cancelIntoDodgeEndNormalized
movementBehavior (optional: lunge distance, root during swing)
tags (DamageType, Element, etc.)
2.3 HitShapeDefinition
Defines what can be hit.
Types
Arc
Cone
Box
Sphere
Common fields
range
radius/width/angle (depending on type)
offset (forward offset)
requiresLoS (bool)
maxTargets (optional)
hitMomentNormalized (when the hit occurs relative to animation)
Rule
Hit shapes define target acquisition on server; client uses them for previews only.
3) WeaponSpecialDefinition
Defines how the weapon’s Special becomes available and what it does.
3.1 SpecialRuleDefinition
Defines unlocking condition.
Rule types
SequenceMatch: e.g., L-L-H
HitCount: e.g., land 5 hits within X seconds
FinisherReached: e.g., reach any node tagged “Finisher”
TimeInCombat: e.g., after 10 seconds in combat
MeterFill: e.g., build meter from hits
Fields
ruleType
parameters (typed fields, not freeform strings)
expiresAfterSeconds (optional)
3.2 SpecialActionDefinition
Defines the actual Special action.
Recommended model
Treat Special as an AbilityDefinition with a weaponRequired restriction.
Fields:
specialId (stable)
abilityRef (AbilityDefinition) OR inline effect list
cooldown (optional; some specials may be only combo-gated)
resourceCost (optional)
Rule
Special must be uniquely tied to the weapon.
4) AbilityDefinition
Defines any castable ability.
Fields
abilityId (stable)
displayName
icon
allowedClasses[]
weaponRequirements[] (optional)
targetingType
Self
AllyEntity
EnemyEntity
GroundPoint
ConeFromCaster
NoTargetForward
range
castModel
Instant
CastTime (seconds)
Channel (duration + tickRate)
Charged (min/max charge time)
cooldownModel
cooldownSeconds
charges (optional)
resourceCosts[] (mana/stamina/etc.)
effectList : EffectListDefinition
interruptRules
interruptedByDamage? (bool)
interruptedByStun? (bool)
interruptedByMove? (bool)
Rules
Ability must be valid without code changes.
If GroundPoint, must define placement constraints (max slope, min distance to caster, etc.) if you use them.
5) EffectListDefinition
Defines a list of effect primitives executed in order.
Fields
effects[] : EffectDefinition
Rule
Effects should not be “script strings”. They should be strongly typed definitions.
6) EffectDefinition (primitive types)
Define a base interface conceptually; implement as typed variants.
DamageEffect
baseValue
scalingStat
coefficient
damageType (Physical/Magical/Element)
canCrit (bool)
HealEffect
baseValue
scalingStat
coefficient
ApplyStatusEffect
statusId
duration
stacks
refreshRule (refresh duration, add stacks, ignore, etc.)
ModifyResourceEffect
resource type, delta (positive or negative)
SpawnProjectileEffect
projectile definition id
speed, lifetime, homing rule
SpawnZoneEffect
zone definition id
radius, duration, tick effects
7) StatusEffectDefinition
Defines buffs/debuffs and their behavior.
Fields
statusId (stable)
displayName
icon
type (Buff/Debuff/CC)
durationModel
stackingRules
modifiers
stat modifiers
action restrictions (silence blocks abilities, stun blocks everything, etc.)
optional periodic effects:
tick rate
tick effect list
Authoring Rules (Critical)
Stable IDs
Every definition must have a stable ID used by:
server
client
persistence
IDs must not be derived from Unity instance IDs.
Recommended: explicit string IDs + editor tool validation for uniqueness.
Validation Requirements (Editor Tools Must Enforce)
combo graph edges point to valid node IDs
step timings are within 0..1 normalized range
hit moment times are within 0..1
abilities have valid targeting + range rules
class and weapon restrictions are consistent
effect lists contain valid references (statusIds, projectileIds, zoneIds)
Runtime Consumption Rules
Client uses assets for:
animation keys
UI previews and telegraphs
local gating (quality-of-life)
Server uses assets for:
final validation
target set calculation
damage/heal/status application
If there is a mismatch, server wins.
Export Strategy (Client/Server Share)
You need a consistent way to ensure server and client use the same combat definitions.
Recommended baseline
Maintain definitions in Unity as ScriptableObjects
Export to a versioned JSON bundle for server:
CombatDefinitions_vX.json
Server loads that JSON at startup
Version rules
Every export increments definitionVersion
Requests/results include the version for debugging only
Server rejects clients with mismatched major versions if you choose strict mode
Data Deliverables (Codex Task List)
Create/extend ScriptableObject definitions:
WeaponDefinition
WeaponCombatDefinition
ComboGraph (nodes/edges)
ComboStepDefinition
HitShapeDefinition
WeaponSpecialDefinition + SpecialRuleDefinition
AbilityDefinition
EffectListDefinition + effect primitives
StatusEffectDefinition
Implement editor validation utilities:
unique ID checks
graph integrity checks
range/timing sanity checks
Implement export pipeline:
generate server JSON bundle
include versioning
Implement server loader for combat definitions JSON