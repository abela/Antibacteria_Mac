# 02 - Enemy framework and enemy group A

Source root: `/home/claude/legacy` (paths below are relative to it; `AI/` unless another folder is given).
Units: positions in cocos2d points (pt). Enemy speeds are Box2D linear velocities in **metres per second**;
`PTM_RATIO = 32` (`GameCore/Physics/Abstracted/Physics.h:18`) and the code uses the same value as
`ASPECT_RATIO = 32.0f` (`ConfigConstants.h:353`) to convert, so **1 m = 32 pt** and `speed 10` = 320 pt/s.
Gravity is zero (`Physics.mm:28`). Physics: `world->Step(dt, 8, 2)` once per rendered frame with the
cocos2d frame `dt` (`Managers/GameManager.mm:420-427`). In slow motion ("bullet time") the cocos scheduler
time scale is `BULLETIN_TIME_DELTA = 0.35` (`GameManager.mm:32,336`), so `dt` and all cocos actions
run at 0.35x. **NSTimers are not scaled**; the code compensates by hand with `+ (1 - timeScale)` = +0.65 s.
No explicit frame rate is set anywhere (no `setAnimationInterval`), so cocos2d-mac's default of 60 fps
applies (inferred).

## Overview

| Class | Role | One-line summary |
|---|---|---|
| `Agent` | framework base | Figure subclass. Holds the AI state enum and speed, and builds the circular **sensor** Box2D body every enemy uses. |
| `EnemyAgent` | framework base | Base enemy: state-machine dispatch, homing/random-fly/flee movement, hit counter, big-explosion damage, shared stat fields. |
| `AgentsManager` | framework manager | Singleton list of top-level enemies. Spawns them at the play-field edge, updates them, removes dead ones, applies big explosions. |
| `EnemyAgentsFactory` | framework factory | `EnemyAgentType` to class switch. Returns `nil` for unmapped IDs. |
| `Spawner` | enemy (ID 13) | Big blue wanderer that spawns a random small enemy at its position every 1 s. |
| `AiInput` | framework blackboard | Singleton that publishes the player's position, rotation and shooting flag to all AI. |
| `EnemyBullet` | framework (dead) | Empty `Bullet` subclass. Never used. |
| `LongBacteria` | framework base | Base class for segmented enemies (`bodies` vector, empty `updateBodies`). Used only by group B. |
| `ShooterBoss` | dead | Empty class marked "reserved". Never used. |
| `BlueCorosia` | enemy (ID 0) | Small spinning blue cell that wanders between random points. 1 hit. |
| `Loctopus` | enemy (ID 1) | Small blue cell that homes straight at the player. 1 hit. |
| `RedNoise` | enemy (ID 2) | Small red cell that homes at the player with no spawn fade. Also the BigRedNoise minion. |
| `SpiralCorosia` | enemy (ID 3) | Cyan cell that homes at the player along looping, spiral paths. |
| `Gridium` | enemy (ID 4, unused) | Cell that shuttles back and forth between two points of a grid line. Never spawned in shipped content. |
| `BigBlueCorosia` | enemy (ID 6) | 4x-size BlueCorosia. 7 hits. |
| `ShootingCorosia` | enemy (ID 7) | 4x-size wanderer that fires 4 bullets (+X, -X, +Y, -Y) every 2 s. 10 hits. |
| `Adrenalium` | enemy (ID 9) | Very fast dasher. Charges at the player's last position or at random points. |
| `Chorux` | enemy (ID 11) | Red homing cell escorted by 2 invulnerable `ChoruxDefender`s. The defenders become killable once Chorux is hit. |
| `ChoruxDefender` | sub-enemy (ID 12, no factory case) | SpiralCorosia variant owned by a Chorux. Invulnerable while its Chorux lives. |
| `Tentacle` | enemy (ID 17) | Slow, invulnerable wanderer used only in Peace Walker mode. |
| `ZigZagus` | enemy (ID 18) | Homing cell with a diagonal sine wobble. |
| `Gnida` | enemy (ID 20) | Homing cell that dodges sideways when the player aims at it and fires. |
| `ZamriGnida` | enemy (ID 21) | Slower Gnida that also loops (spiral) and dodges harder. |
| `BigRedNoise` | boss (ID 24, level 30) | Huge red boss, 400 hits. Homes at the player and releases waves of 60/80/90 RedNoise at 300/200/100 HP. |

---

## 1. Enemy type ID table

The enum is `EnemyAgentType` in `ConfigConstants.h:225-258`. Level files reference enemies by these numbers
(`levels_all.json` field `enemies`, read by `Managers/Level.mm:217` and picked by
`LevelManager.mm:124-130`). The factory switch is `EnemyAgentsFactory.mm:42-98`.
"Levels" lists the campaign levels whose `enemies` array contains the ID (from `out/data/levels/levels_all.json`).

| ID | Enum | Class (factory) | Levels | Description |
|---|---|---|---|---|
| 0 | `kBlueCorosia` | `BlueCorosia` | 1-9, 11-19, 23, 24, 29 | Small spinning wanderer. 1 hit, 50 pts. |
| 1 | `kLoctopus` | `Loctopus` | 2-9, 11-19, 23, 24 | Small homing cell, 9 m/s. 1 hit, 80 pts. |
| 2 | `kRedNoise` | `RedNoise` | 4-9, 11-16, 25-29, 47-49 | Small red homing cell, 10 m/s. 1 hit, 80 pts. |
| 3 | `kSpiralCorosia` | `SpiralCorosia` | 6, 9, 17-19, 41, 42 | Spiral/looping homing cell. 1 hit, 90 pts. |
| 4 | `kGridium` | `Gridium` | none | Line shuttler. Only `AgentsManager createGridiums` (dead code) sets it up properly. |
| 5 | `kBigHunterus` | `BigHunterus` (group B) | 11, 12, 17-19, 41, 42 | Documented in group B. |
| 6 | `kBigBlueCorosia` | `BigBlueCorosia` | 13-16, 47-49 | Big wanderer. 7 hits, 100 pts. |
| 7 | `kShootingCorosia` | `ShootingCorosia` | 25, 26 | Big wanderer that shoots 4 ways. 10 hits, 130 pts. |
| 8 | `kSunux` | `Sunux` (group B) | none | Documented in group B. Appears in the Rush/Take-the-Flag pool. |
| 9 | `kAdrenalium` | `Adrenalium` | 25, 26, 43, 44 | Fast dasher, 18-25 m/s. 1 hit, 150 pts. |
| 10 | `kCaterpillar` | `CaterPillar` (group B) | none | Documented in group B. Rush/Take-the-Flag pool. |
| 11 | `kChorux` | `Chorux` | 21-24 | Homing cell with 2 shield defenders. |
| 12 | `kChoruxDefender` | **no factory case (returns nil)** | none | Created only by `Chorux`. |
| 13 | `kSpawner` | `Spawner` | none | Enemy generator. Rush/Take-the-Flag pool only. |
| 14 | `kSerpentus` | `Serpentus` (group B) | 31, 32 | Documented in group B. |
| 15 | `kLotusBacterius` | `LotusBacterius` (group B) | 33-36, 43, 44 | Documented in group B. |
| 16 | `kLotusBacteriusBody` | **no factory case (returns nil)** | none | Segment of LotusBacterius (group B). |
| 17 | `kTentacle` | `Tentacle` | none | Invulnerable wanderer, Peace Walker mode only. |
| 18 | `kZigzagus` | `ZigZagus` | 35, 36 | Homing cell with a wobble. 1 hit, 160 pts. |
| 19 | `kShootingSerpentus` | `ShootingSerpentus` (group B) | 37-39 | Documented in group B. |
| 20 | `kGnida` | `Gnida` | 41-48 (and 49) | Dodging homing cell. 1 hit, 200 pts. |
| 21 | `kZamriGnida` | `ZamriGnida` | 45-48 (and 49) | Spiral and dodging homing cell. 1 hit, 250 pts. |
| 22 | `kBigBoss` | `BigBoss` (group B) | **10** (boss 1) | Boss. Documented in group B. |
| 23 | `kInferno` | `Inferno` (group B) | **20** (boss 2) | Boss. Documented in group B. |
| 24 | `kBigRedNoise` | `BigRedNoise` | **30** (boss 3) | Boss, 400 hits. Spawns RedNoise waves. |
| 25 | `kAnakonda` | `Anakonda` (group B) | **40** (boss 4) | Boss. Documented in group B. |
| 26 | `kFrunctus` | `Frunctus` (group B) | **50** (boss 5, final) | Boss. Documented in group B. |
| 27 | `kArmagedonBacteria` | **factory case commented out (returns nil)** | none | Armagedon mode enemy (group B). |

Notes on the table:
- **QUIRK:** the source comment on `kArmagedonBacteria` says `//22` (`ConfigConstants.h:255`), but its real value is **27**.
- **QUIRK:** `ArmagedonGameMode.mm:30` spawns `kArmagedonBacteria`. Because the factory case is commented out
  (`EnemyAgentsFactory.mm:94-95`), the factory returns `nil`, and `nil` is pushed into the agent list, so Armagedon mode spawns no
  visible enemies. Messages to `nil` do nothing, and `nil.flag` reads 0 (`kDefault`), so the null entries are never removed and
  they count in `getEnemiesCount`. The same applies to IDs 12 and 16, and to any out-of-range ID.
- **QUIRK (data):** `level_49.plist` has `"number": 48` (see `levels_all.json`). `number` is used for the boss check
  (`number % 10 == 0`) and for `LAST_LEVEL = 50`, so this is harmless but should be fixed in the data.
- Boss levels are the levels where `number % BOSS_LEVEL_CREATE_CONST(10) == 0` (`CompaignGameMode.mm:60`, `ConfigConstants.h:457`). In each
  boss world, level 10 holds exactly one ID: 22, 23, 24, 25, 26.
- Duplicates in a level's `enemies` array act as weights, because the pick is a uniform random index
  (`LevelManager.mm:127-128`). For example, levels 47-49 list `[20,20,21,21,6,6,2,2]`.
- Other modes use a hard-coded pool `GameMode.mm:23-42`: `[0,1,2,11,9,5,6,8,7,0,13,18,20,18,21,10,14,15,19]`.
  **QUIRK:** the array is `malloc`'d for 19 entries, but Rush/Take-the-Flag let the index reach 19
  (`RushGameMode.mm:105-107`, `TakeTheFlagGameMode.mm:115-117`). That is an out-of-bounds read, giving a garbage type and so a `nil` agent.

---

## 2. Framework

### 2.1 Object model and lifecycle (Figure -> Agent -> EnemyAgent)

- `Figure` (`GameCore/Objects/Abstracted/Figure.h`) provides `sprite`, `body`, `width/height`, `type` (`GameObjectType`,
  enemies use `kEnemy`), and `flag` (`ObjectEvent`: `kDefault, kMoving, kStaying, kDealloc, kDying, kReset`,
  `ConfigConstants.h:129-138`).
- `flag` is the life state:
  - `kDefault`: alive.
  - `kDying`: logically dead but kept alive while it waits for something (its bullets or its children). The sprite is hidden.
    Bullets pass through it, and touching it does not hurt the player (`GameCore/Physics/Abstracted/ContactListener.mm:62,76,92,101`).
  - `kDealloc`: the owner removes it on its next update.
- Creation pattern: `[[Class alloc] createAtPosition:pos]`. **`init` is never called** on enemies. `EnemyAgent.createAtPosition`
  only returns `self` (`EnemyAgent.mm:38-43`), so `Figure.init` never runs. Every subclass sets `flag = kDefault` by hand.
  Subclasses chain to `super createAtPosition` to inherit sprite and body setup (Spawner from BigBlueCorosia,
  Chorux from RedNoise, ChoruxDefender from SpiralCorosia). ZamriGnida deliberately skips Gnida's version.
- Removal: `releaseAgent` destroys the body (`Figure.resetBody` deactivates it, then calls `DestroyBody`, `Figure.mm:243-247`) and removes
  the sprite and the glow (`EnemyAgent.mm:272-279`). Subclasses drop vitamins in their `releaseAgent` **before** calling super.

### 2.2 Agent (`Agent.h/.mm`)

Purpose: abstract AI base. Fields `health`, `armor`, `hasShield` are declared (`Agent.h:18-21`) but **never used** by any
enemy. Damage is `EnemyAgent.hitCount`. Empty virtuals: `evade, fastEvade, isSafe (YES), attack, patrol, deffence,
update, flee, idle, randomfly, releaseAgent` (`Agent.mm:33-109`).

Physics body, `Agent.createNewBodyAt:andDimension:` (`Agent.mm:81-105`). This overrides Figure's box body and is used by every enemy:
- Dynamic body, `fixedRotation = false`, `allowSleep = false`, `userData = self`.
- **Circle** with radius `dimension.x / 2` (metres), `density = 1.0`, **`isSensor = true`**. Friction and restitution are left at the Box2D defaults.
- No category or mask bits anywhere in the project (grep finds none), so everything overlaps everything. Because enemies are sensors,
  they never push each other, the player, or the world bounds. They only generate contact events.
- Radius in points = `width/2 * 32`. `width 2.0` gives r = 1 m = **32 pt**, `5.7` gives r = 2.85 m = **91.2 pt**,
  `10.3` gives r = 5.15 m = **164.8 pt**.

`AgentStateType` (`ConfigConstants.h:211-223`): `kStateRandomMovement, kStateAppearing, kStateWaiting, kStateAttack,
kStateDeffence, kStateEvade, kStatePatrol, kStateFlee, kStateIdle`. `kStateAppearing` and `kStateDeffence` are never used.
`kStateAttack` is never assigned anywhere in the project, so `attack` is dead code for every enemy. `kStateFlee` is only reachable from
`attack`, so it is dead for group A too.

### 2.3 EnemyAgent (`EnemyAgent.h/.mm`)

Shared fields (`EnemyAgent.h:20-37`): `enemyAgentType`, `weapon`, `shootDistance`, `fleeDistance`, `fleeSpeed`, `shootRate`,
`shootingTimer`, `movementVelocity`, `glowSprite`, `randomMovePoint`, `hitCount` (= HP in hits), `killScore`,
`opacityDecreaserStep`, `vitaminsCount`. Every group-A enemy sets `shootRate 0.5` and `shootDistance 100`, but only
ShootingCorosia uses them.

**Update dispatch** (`EnemyAgent.mm:78-110`). The update switches on `agentStateType`:
- `kStateWaiting`: nothing.
- `kStatePatrol`: `patrol`.
- `kStateAttack`: `attack`.
- `kStateFlee`: `flee`.
- `kStateRandomMovement`: `randomfly`.
- `kStateEvade`: `evade`.
- `kStateIdle`: `idle`.

After the switch it always calls `updateEnemyBullets` (`[weapon updateBullets]`). The weapon is `nil` for everyone except ShootingCorosia.

**Movement primitive** `moveToPosition:(CGPoint)v` (`EnemyAgent.mm:50-56`). Despite its name, the argument is a **velocity in m/s**.
It calls `body->SetLinearVelocity(v)` and then copies the body position (before this frame's step) into the sprite and glow
(x 32). Velocity is set directly every update, with no acceleration or steering.

**Patrol = homing** (`EnemyAgent.mm:58-76`):
1. `dir = normalize(player - self)`.
2. `sprite.rotation = ±deg(acos(dir · (0,1)))`. The sign is negative when the player is to the left. So 0° = up and positive = clockwise (cocos convention).
3. Velocity = `dir * speed`.

Targeting is always the live player position from `AiInput.mainCharacterPosition`.

**Random fly** (`EnemyAgent.mm:121-140`): velocity = `normalize(randomMovePoint - self) * speed`. No rotation. Subclasses
pick a new `randomMovePoint` when they come within **10 pt** of it (their `moveToPosition` override).

**Flee** (`EnemyAgent.mm:142-178`, unreachable): if the player is closer than `fleeDistance`, move away at `fleeSpeed - currentVelocity`.
Otherwise switch to patrol and stop.
**Attack** (`EnemyAgent.mm:215-244`, unreachable): face the player and start the shooting timer. Return to patrol when distance >= `shootDistance`.

**Spawn-in** (pattern used by most subclasses):
1. `sprite.opacity = 0`, `body->SetActive(false)`.
2. Run `CCFadeIn` (duration per class).
3. When the fade ends, `creatingAnimationHasEnd:` (`EnemyAgent.mm:246-250`) sets opacity 255 and activates the body.

While the body is inactive the enemy can neither hurt nor be hurt. The fade runs on the cocos scheduler, so slow motion slows it too.

**Damage** `hit` (`EnemyAgent.mm:266-270`): `hitCount--`, then `flag = hitCount <= 0 ? kDealloc : kDefault`. One player bullet = 1 hit
(the bullet's `damage = 1.0` field is ignored).

**Big explosion** `bigExploded` (`EnemyAgent.mm:252-259`): if the player is within `BIG_EXPLOSION_DISTANCE = 700` pt,
then `hitCount -= BIG_EXPLOSION_HIT_CONST (20)` and the flag is set as in `hit` (`ConfigConstants.h:370-371`).

`refreshEnemy` (empty here) is called on every agent when slow motion toggles (`AgentsManager.refreshAgentsManager`), so
subclasses can rebuild their NSTimers with adjusted intervals.

Assets: sprites live in the batch node `characters_resources.png` (`Managers/GameResourceManager/GameResourceManager.mm:87-96`;
frames in `out/data/raw/characters_resources.json`):
- `Enemy1.png`: 56x64 trimmed, 64x64 source.
- `Enemy3.png`: 48x48 trimmed, 64x64 source.
- `Shine2.png` and `Shine3.png`: glows, 118x118 trimmed, 128x128 source.

Body sprite z = 1 and glow z = 0 within that batch, unless noted otherwise. Tint is applied with `setColor`. The batch uses the
blend `GL_DST_ALPHA, GL_DST_ALPHA` when slow motion is on (`GameResourceManager.mm:57-59`). None of the group-A classes play sounds,
post notifications or read user defaults.

Unity mapping:
- `EnemyBase : MonoBehaviour` with a `Rigidbody2D` (kinematic, or dynamic with gravity 0) plus a trigger `CircleCollider2D`.
  Put stats (speed, hits, score, vitamins, radius, sprite, tint, fade time) in an `EnemyDefinition` ScriptableObject keyed by the numeric ID.
- Write movement as small strategy components (Homing, RandomWander, Spiral, ZigZag, Dodge) that output a desired velocity
  in world units/s (divide pt values by your pixels-per-unit).
- Keep the life state as an explicit enum: Spawning, Alive, Dying, Dead.

### 2.4 AiInput (`AiInput.h/.mm`)

A blackboard singleton. The player writes into it every frame:
- `mainCharacterPosition` (`Characters/MainCharacter.mm:168`).
- `mainCharacterRotation` (`MainCharacter.mm:811`).
- `characterIsshooting` (`MainCharacter.mm:815`).
- `cursorPosition` (`Layers/HUDLayer.mm:48`).

On reset everything goes back to 0 (`MainCharacter.mm:920-922`). Its `update` is empty, and the ivars `agentsPosition`, `obstacles` and
`mainCharacterSpeed` are unused. **All enemies target only the player**. There is no other target selection anywhere in group A.
Unity: a `PlayerTracker` service or ScriptableObject runtime set, or simply a reference to the player Transform.

### 2.5 AgentsManager (`AgentsManager.h/.mm`)

Singleton owning `vector<Agent*> agents`, the **top-level** enemies only. Children of composite enemies (Spawner spawns,
BigRedNoise minions, Chorux defenders) are owned by their parent and are not in this list.

| Method | Behaviour |
|---|---|
| `createRandomAgentAtPosition:pos :type` (`:81-90`) | **Ignores `pos`.** Spawns `type` via the factory at `SpawnManager.getValidOuterSpawn` and appends it. **The only spawn path used by the shipped game modes.** |
| `update` (`:166-184`) | For each agent: if not `kDealloc`, call `update`. Otherwise call `releaseAgent`, release, erase, and **`break`**. |
| `bigExplosion` (`:116-126`) | Calls `bigExploded` on every agent whose `flag == kDefault`. |
| `refreshAgentsManager` (`:155-164`) | Calls `refreshEnemy` on every agent that is not `kDealloc` (slow-motion toggle). |
| `simulateAgentsManagerRestart` / `levelHasWon` (`:186-202`) | Calls `releaseAgent` on every agent that is not `kDealloc`, then clears the list. |
| `getEnemiesCount` (`:48-51`) | `agents.size()`. Used by the boss-win check. |
| `createAgentsCrowd`, `createAgentsCircleCrowd`, `createRandomEnemiesAtRandomPoints`, `createRandomEnemiesAroundRandomPoint`, `createOuterEnemiesCrowd`, `createGridiums` (`:53-153`) | **Dead code** (no callers). Ring or edge formations of SpiralCorosia, Tentacle, BlueCorosia and Gridium. |

Notes on the table:
- **QUIRK:** in `update`, the `break` after a removal means at most one dead agent is removed per frame. Agents later in the list are
  not updated by the manager that frame (they are still updated by the physics loop, see 2.9).
- **QUIRK:** in `createRandomEnemiesAroundRandomPoint`, Y is drawn from `bottomRight.y..bottomRight.y` (`:144-145`).

Spawn position, `SpawnManager.getValidOuterSpawn` (`Managers/SpawnManager/SpawnManager.mm:73-106`):
1. Pick one of the 4 sides of the play-field rectangle uniformly (`arc4random_uniform(4)`).
2. Pick a uniform random point along that side.

The rectangle corners are in `CoreSettings`, which `GameWorld.initBackground` sets to the `bg.jpg` bounds inset by
`ENEMY_CREATE_BORDER_DELTA = 20` pt (`GameWorld/GameWorld.mm:73-88`, `ConfigConstants.h:427`). So **every enemy appears on the
arena border**, which is off-screen in most cases.

Random wander targets (`SpawnManager.getRandomPointAroundPoint`, `SpawnManager.mm:327-352`):
1. Start from a uniformly random point in the field.
2. Offset it by `rand(500..520)·cos a` and `rand(500..520)·sin a` (two independent radius draws), with a uniform `a` in 0-359°
   (`ENEMY_ARROUND_RANDOM_POINT_RADIUS(2)`, `ConfigConstants.h:410-411`).
3. Clamp to the field inset by `BOUNDER_ESCAPE_DELTA = 30` pt (`ConfigConstants.h:405`).

`Utils getRandomNumber:a to:b` is inclusive of both ends (`Utils/Utils.mm:36-39`).

Unity mapping:
- An `EnemyRegistry` / `EnemySpawner` MonoBehaviour that owns a `List<EnemyBase>`, with pooled instantiation.
- Keep a separate "top-level count" for the boss-win rule.
- Remove all dead enemies every frame (do not reproduce the one-per-frame `break`).

### 2.6 EnemyAgentsFactory (`EnemyAgentsFactory.h/.mm`)

A static `getEnemyWithType:withPosition:` switch (`:39-101`) mapping the enum to `[[Class alloc] createAtPosition:]`. See the ID
table for the cases. `default` returns `nil`. Unity: a dictionary from ID to `EnemyDefinition` (prefab + stats).

### 2.7 How the campaign spawns enemies (context; game modes and levels are another area)

The flow is `CompaignGameMode` (`Managers/GameModeManager/GameModes/CompaignGameMode.mm`) plus `LevelManager` and `Level`:

- A repeating NSTimer fires every `enemiescreatetime` s (`:75-78`, `:92-95`). On each fire it spawns `totalEnemiesPerCreation`
  enemies, each with an independent random type from the level's `enemies` list (`:134-144`). It skips while paused.
- Difficulty shortens the interval. Hard is −0.5 s and Impossible is −1.0 s (`Managers/Level.mm:221-233`).
- On the very first level of a session the first wave is delayed by `enemiescreatetime + 2` s (`:65-71`).
- `totalEnemiesPerCreation` starts at 1. It goes up by 1 each time an internal counter reaches `enemiesgrowinterval`, capped at
  `maximumenemies` (`LevelManager.mm:148-174`).
  - **QUIRK (frame-based):** the counter adds `GAME_NEXT_SIMULATION_STEP_INTERVAL = 0.01` **per frame** (0.005 in slow motion)
    (`LevelManager.mm:154-156`, `ConfigConstants.h:25`). So a grow interval of 10 is 1000 frames, about 16.7 s at 60 fps.
  - **QUIRK:** the guard `gameIsPaused == NO || levelHasWon == NO` (`LevelManager.mm:151-152`) is almost always true. It was probably meant to be `&&`.
- There is no cap on how many enemies are alive at once.
- Boss levels (`number % 10 == 0`): the spawn timer is **non-repeating** (`repeats: !weHaveBossLevel`), so exactly one spawn event
  happens. **QUIRK (inferred):** level 20 has `maximumenemies 2, enemiesgrowinterval 1`. The per-frame counter reaches 1 after about
  100 frames (1.7 s), before the 2 s (or 4 s on first start) timer fires. Two Infernos would then spawn, but the boss-win check needs
  exactly one agent left (see 2.8). Verify in play-test.
- Normal levels are won by surviving until the level `time` counts down to 0 (`GameManager.mm:182-187`). Boss levels are won by
  killing the boss (`GameManager.levelHasWonAfterBigBoss`, `GameManager.mm:219-239`). That method only acts when
  `getEnemiesCount() == 1`, meaning the boss is the only agent still in the list at the moment it dies.
- Slow-motion toggle: `CompaignGameMode.refreshGameMode` rebuilds the spawn timer with `interval + 1 - timeScale` and calls
  `AgentsManager.refreshAgentsManager` (`:226-260`).
- Other modes call the same `createRandomAgentAtPosition`. Peace Walker spawns only `kTentacle` (`PeceWalkerGameMode.mm:92,150`).

### 2.8 Damage, death and rewards

All of this happens in `ContactListener::BeginContact` (`GameCore/Physics/Abstracted/ContactListener.mm:32-191`) during `world->Step`.

**Bullet vs enemy** (`:60-87`):
- If the enemy is not `kDying` and the bullet tag is not `SHOOTING_COROSIA_BULLET_TAG (2)` or `FRUNCTUS_BULLET_TAG (3)`
  (`ConfigConstants.h:429-431`), the bullet is destroyed. Bullets therefore pass through `kDying` enemies.
- For non-enemy-tagged bullets, `enemy.hit` is called.
- If after that `enemy.flag == kDealloc` **and** the mode is `kCompaignMode`, the score is awarded with
  `GameManager.updateLScoreWithValue(killScore)`.
- **Score exists only in campaign mode.** It is shown multiplied by `(difficulty + 1)`: Normal x1, Hard x2, Impossible x3
  (`GameManager.mm:619-624`).

Score is NOT awarded in these cases:
- Kills by the big explosion (`bigExploded` never reports score).
- Enemies whose `hit` sets `kDying` instead of `kDealloc`. **Chorux's 200 points are never awarded** (see 3.10).
- Enemies with `killScore` never set (0): `Gridium`, `Tentacle`, `BigRedNoise`.

**Enemy vs player** (`:89-105`): if the enemy is not `kDying`, `MainCharacter.hit` runs: −1 life and 2 s of blinking invulnerability
(`Characters/MainCharacter.mm:705-724`). **Touching does not damage the enemy.** Enemies persist after a ram.

**Enemy bullet vs player** (`:107-119`): `MainCharacter.hit`, and the bullet is destroyed.

**Bullet vs bullet** (`:43-49`): both are destroyed. The player can shoot down ShootingCorosia bullets, and two enemy bullets that cross also cancel.

**Bullet vs bounder**: the bullet is destroyed (`:51-58`).

**Big explosion**:
- Triggered by the `kExplostionBonus` pickup (`MainCharacter.mm:476-477`) or by the big-explosion special ability (`MainCharacter.mm:549-558`).
- `AgentsManager.bigExplosion` deals 20 hits to every top-level `kDefault` enemy within 700 pt of the player.
- Children of composite enemies are never affected.

**Vitamins** (the currency):
- In `releaseAgent`, if `hitCount <= 0` (it really died), the enemy spawns `vitaminsCount` vitamins. Each one is placed at a random
  point in a ±80 pt square around the enemy (`VITAMIN_CREATE_RADIUS 80`, `AI/VitaminManager.mm:13,46-73`), clamped to the field.
- Each vitamin is worth 1 (`createVitaminAtPosition:withScore:1`).
- **QUIRK:** `VitaminManager.createVitaminAtPosition` adds the value to the player's vitamin total **at creation time**
  (`VitaminManager.mm:75-85`). Picking it up only refreshes the HUD (`ContactListener.mm:177-189`). Dropped vitamins therefore count
  even if they are never collected. The Vitamin pickup itself is documented in another area.
- Enemies killed by the big explosion also drop vitamins, because `hitCount <= 0`.

**Bonuses (power-ups)**: no enemy in group A drops power-ups. Campaign bonuses are generated on a timer every 20 s
(`CompaignGameMode.mm:19,121-129`) and not by kills.

Unity mapping:
- Player projectiles call `IDamageable.TakeHit(1)`.
- On death raise an `EnemyKilled(def, cause)` event. A ScoreService (campaign only, x difficulty) and a VitaminService listen to it.
- Decide explicitly whether explosion kills award score. The original does not award it.

### 2.9 Update order and frame-rate dependence (important for the rewrite)

- **QUIRK (double update):** every frame `GameManager.managePhysics`:
  1. Steps the world.
  2. Iterates **all Box2D bodies** and calls `[enemy update]` on every body whose figure `type == kEnemy` (`GameManager.mm:466-472`).
  3. Calls `gameWorld update`, which reaches `CompaignGameMode.update` and then `AgentsManager.update`, which calls `update` on every
     top-level agent again (`GameManager.mm:481`, `GameWorld/GameWorld.mm:188-191`, `GameModeManager.mm:85-87`,
     `CompaignGameMode.mm:220-224`).

  Composite parents also update their children (Spawner, BigRedNoise, Chorux). So **every enemy's `update` runs about twice
  per frame**. For velocity-setting code this makes no difference. For per-update counters (spiral angle, zig-zag
  phase) the effective rate is **two increments per frame, about 120 per second at 60 fps**. The numbers below are given "per update"
  with this conversion where it matters.
- The per-frame body loop also updates enemies whose body is still inactive (during the fade-in). They sit in `kStateWaiting`, so nothing happens.
- **QUIRK (possible use-after-free):** a parent's update can `releaseAgent` a child (which destroys its b2Body) while
  `managePhysics` is still walking `world->GetBodyList()`. Box2D's block allocator usually hides this, but it is undefined behaviour.
- Speeds and velocities are frame-rate independent, because the velocity is set and Box2D integrates with `dt`.
  Angle increments (SpiralCorosia, ZamriGnida, ChoruxDefender, ZigZagus) are **per update**. On a high-refresh display they would
  run faster if cocos2d's display link ran above 60 Hz (inferred).
- NSTimers (spawn and shoot intervals) run in real time and keep firing during pause. Most handlers check `gameIsPaused`.

Unity mapping: run AI once per `FixedUpdate`. Convert per-update angle steps to degrees per second
(`step × 120`) so the 2013 feel is reproduced. Replace NSTimers with timers scaled by `Time.deltaTime`, which makes the
manual `+ (1 - timeScale)` compensation unnecessary.

---

## 3. Group A enemies

Stats summary. Speed is in m/s, with pt/s in brackets. "Fade" is the spawn fade-in in seconds; the body is inactive during it.
Cites are given in each section.

| ID | Class | Speed | Hits | killScore | Vitamins | Collider r | Sprite / tint / glow / scale | Fade | Initial state |
|---|---|---|---|---|---|---|---|---|---|
| 0 | BlueCorosia | 8 (256) | 1 | 50 | 2 | 32 pt | Enemy3 untinted, Shine3, x1, spins | 0.1 | RandomMovement |
| 1 | Loctopus | 9 (288) | 1 | 80 | **2** (field says 3) | 32 pt | Enemy3 blue (0,0,255), Shine2 | 0.1 | Patrol |
| 2 | RedNoise | 10 (320) | 1 | 80 | 3 (0 if boss-born) | 32 pt | Enemy1 red (255,0,0), no glow | none | Patrol |
| 3 | SpiralCorosia | 6 (192) + spiral | 1 | 90 | 3 | 32 pt | Enemy3 cyan (0,255,255), Shine2 | 0.1 | Patrol |
| 4 | Gridium | 10 (320) | 1 | 0 | 2 | 32 pt | Enemy3, Shine3 | 0.1 | Patrol (shuttle) |
| 6 | BigBlueCorosia | 7 (224) | 7 | 100 | 5 | 91.2 pt | Enemy3, Shine3, x4, spins | 0.3 | RandomMovement |
| 7 | ShootingCorosia | 7 (224) | 10 | 130 | 7 | 91.2 pt | Enemy3, Shine3, x4, spins | 0.3 | RandomMovement + shooting |
| 9 | Adrenalium | 20, then 18-25 (576-800) | 1 | 150 | 5 | 32 pt | Enemy3, Shine3, spins | 0.1 | RandomMovement |
| 11 | Chorux | 6 (192) | 1 (+2 defenders) | 200 (never paid) | 3 | 32 pt | Enemy1 red, no glow, spins | 0.1 | Patrol |
| 12 | ChoruxDefender | 7 (224) + spiral | 1 (shielded) | 90 (inherited) | 2 | 32 pt | Enemy3 cyan, Shine2 | 0.1 | Patrol |
| 13 | Spawner | 15 (480) | 10 | 160 | 10 (always) | 91.2 pt | Enemy3, Shine3, x4, spins | 0.3 | RandomMovement + spawning |
| 17 | Tentacle | 3 (96) | invulnerable | 0 | 0 | 32 pt | Enemy3, Shine3, spins | 0.1 | RandomMovement |
| 18 | ZigZagus | 7 (224) + wobble | 1 | 160 | 4 | 32 pt | Enemy3 blue, Shine2 | 0.1 | Patrol |
| 20 | Gnida | 10 (320) + dodge | 1 | 200 | 5 | 32 pt | Enemy3 blue (white while dodging), Shine2 | 0.1 | Patrol |
| 21 | ZamriGnida | 6 (192) + spiral + dodge | 1 | 250 | 6 | 32 pt | Enemy3 blue (white while dodging), Shine2 | 0.1 | Patrol |
| 24 | BigRedNoise | 12 / 15 / 18 by difficulty | 400 | 0 | 10 | 164.8 pt | Enemy1 red, x7, z 4, no glow | 1.0 (body active) | Waiting, then Patrol |

"Spins" means the sprite runs `CCRepeatForever(CCRotateBy 0.3 s, 90°)`, i.e. 300°/s in game time (slowed by slow motion).
These spinning enemies do not face their movement direction. **QUIRK:** Chorux also uses homing patrol, which writes
`sprite.rotation` every update while the spin action also writes it, so the two fight over the rotation.

Fields every group-A enemy sets but never uses: `fleeSpeed 5`, `fleeDistance 70` (Gnida 300), `shootRate 0.5`,
`shootDistance 100`. They only matter for the dead attack and flee states.

### 3.1 BlueCorosia (ID 0), `BlueCorosia.mm`

Purpose: the basic "fodder" cell. A small, gently glowing blue cell that drifts aimlessly around the arena while spinning.
It only hurts the player by contact.

Behaviour:
- Created on the border, invisible and with the body inactive. It fades in over 0.1 s (`:57-59`).
- `creatingAnimationHasEnd`: picks a wander target, enters `kStateRandomMovement`, and starts spinning (`:81-87`).
- Moves straight to the target at 8 m/s. Within 10 pt of the target it picks a new one (`:99-104`, target picker `:69-79`,
  see 2.5). It does not react to the player at all.
- Dies in 1 hit and drops 2 vitamins (`:106-117`).

Numbers: speed 8.0 (`:28`); hitCount 1 (`:34`); killScore 50 (`:35`); width/height 2.0, so r = 32 pt (`:37-38`); vitaminsCount 2
(`:39`); fade 0.1 s (`:57`); spin 90° / 0.3 s (`:86`); arrival threshold 10 pt (`:102`). The sensor is set again redundantly at `:61`.
Assets: `Enemy3.png` z 1, `Shine3.png` z 0 (`:41-42,54-55`).
Unity: a WanderMovement component plus a constant spin on the visual child.

### 3.2 Loctopus (ID 1), `Loctopus.mm`

Purpose: a small blue chaser that flies straight at the player.
Behaviour: fades in over 0.1 s, then `kStatePatrol`, which is the base homing (`EnemyAgent.patrol`). The sprite faces the player.
Speed 9 (`:24`), hitCount 1 (`:28`), killScore 80 (`:29`), r = 32 pt (`:32-33`), tint (0,0,255) (`:45`), fade 0.1 (`:52`).
**QUIRK:** `vitaminsCount = 3` (`:30`), but `releaseAgent` loops a hard-coded `i < 2` (`:87`), so it drops **2** vitamins.
Assets: `Enemy3.png` (tinted), glow `Shine2.png` (`:35-36`).
Unity: a HomingMovement component (velocity = dir × speed every FixedUpdate) plus "face target".

### 3.3 RedNoise (ID 2), `RedNoise.mm`

Purpose: a red chaser. Also the minion type released by the BigRedNoise boss.
Behaviour:
- **No fade-in.** It is created with full opacity, an active body, and `agentStateType = kStatePatrol` (`:23`), so it starts homing the moment it appears.
- Homes at 10 m/s (`:26`).
- 1 hit (`:30`), 80 points (`:31`), 3 vitamins (`:32`).
- If `bornFromBoss == YES` it drops **no** vitamins (`:73`). It still pays 80 points in campaign mode when shot.

Sprite `Enemy1.png` tinted (255,0,0), z 1, **no glow sprite** (`:37,46,48`). r = 32 pt (`:34-35`).
Unity: same HomingMovement. Give it a `droppedByBoss` flag that suppresses vitamin drops.

### 3.4 SpiralCorosia (ID 3), `SpiralCorosia.mm`

Purpose: a cyan cell that advances on the player while tracing big loops, so it is hard to hit.
Behaviour: fades in over 0.1 s (`:63`), then patrol (`:75-79`). Each update (`:86-124`):
1. Homing velocity `v = dir × 6` is applied and the sprite faces the player (`:89-105`).
2. `moveOnCircle` then **overwrites** the velocity with `v + radius·(cos α, sin α)` (`:113-116`). Here `radius` is
   a velocity magnitude in m/s.
3. `α += alphaDelta` degrees.
4. When `α > 360`: re-roll `radius` from 10..20 and `alphaDelta` from 1..3, then set `α = 0`. In slow motion the re-roll uses
   radius 15..25 and alphaDelta 2..4 (`:117-123`).
   Initial values are radius rand 10..20 and alphaDelta rand 1..3 (`:40-41`).

The rotating component (10-20 m/s) is larger than the forward component (6 m/s), so the path is a series of backward loops drifting
toward the player at about 192 pt/s on average.
- Loop period at 2 updates per frame and 60 fps: 360 / (alphaDelta × 120) gives 3 s, 1.5 s or 1 s for alphaDelta 1, 2 or 3.
- Loop radius ≈ radius / ω. For example, 15 m/s at 240°/s is about 3.6 m, or 115 pt (derived).

Numbers: speed 6.0 (`:31`), hitCount 1 (`:36`), killScore 90 (`:37`), vitamins 3 (`:38`), r = 32 pt (`:43-44`), tint (0,255,255) (`:56`).
`#define SPIRAL_RADIUS 15.0f` is unused (`:14`).
Assets: `Enemy3.png`, glow `Shine2.png` (`:46-47`).
**QUIRK:** the spiral speed depends on the frame rate (per-update angle steps, double-updated).
Unity: a SpiralMovement component with `orbitSpeed` (m/s), `angularSpeed` (°/s, re-rolled each full turn) and `forwardSpeed`.

### 3.5 Gridium (ID 4, effectively unused), `Gridium.mm`

Purpose (intended): sweeping "grid wall" cells. `AgentsManager.createGridiums` places N of them evenly along one arena edge.
Each one shuttles to the opposite edge and back (`AgentsManager.mm:92-104`, `SpawnManager.getGridiumPoints` `SpawnManager.mm:108-190`,
which uses the hard-coded corners `UPPER_LEFT_X -555`, `UPPER_LEFT_Y 1124`, `BOTTOM_LEFT_Y -375`, `UPPER_RIGHT_X 1576`,
`ConfigConstants.h:415-425`).
Behaviour (`:81-102`): velocity = `normalize(endPos - startPos) × 10`. When within 5 pt of `endPos`, it swaps start and end.
The `rotAlpha` property is stored but unused.
Numbers: speed 10 (`:30`), hitCount 1 (`:36`), vitamins 2 (`:37`), r = 32 pt, fade 0.1 (`:58`).
Gridium has these defects:
- **QUIRK:** `enemyAgentType = kBlueCorosia` (`:29`) is a copy-paste error.
- **QUIRK:** `killScore` is never set, so it pays 0.
- **QUIRK:** `createGridiums` has no callers, and ID 4 appears in no level. If a level did list 4, the factory would spawn it at an
  outer point with `endPos = (0,0)`, so it would shuttle between its spawn point and the world origin.

Unity: drop it, or re-implement it as a "sweeper wave" pattern.

### 3.6 BigBlueCorosia (ID 6), `BigBlueCorosia.mm`

Purpose: a large, tougher BlueCorosia. A slow, spinning, wandering blob that takes 7 shots.
Behaviour: like BlueCorosia. It fades in over 0.3 s (`:58`), then random movement at 7 m/s with spinning (`:82-88`), and picks a new
target within 10 pt (`:134-139`). Its overrides of `patrol` (`:95-112`) and `attack`/`flee` (`:141-180`, an attack that switches to
flee when closer than `fleeDistance` 70) are unreachable (no state ever sets Patrol or Attack for this class).
Numbers: speed 7.0 (`:28`), hitCount 7 (`:34`), killScore 100 (`:35`), vitamins 5 (`:36`), width 6×0.95 = 5.7, so r = 91.2 pt
(`:38-39`), sprite scale 4.0 for both sprite and glow (`:45`).
Assets: `Enemy3.png`, `Shine3.png`.
Unity: the same prefab as BlueCorosia with a scaled visual, a larger collider and more HP. This class is also the base for Spawner.

### 3.7 ShootingCorosia (ID 7), `ShootingCorosia.mm`

Purpose: a big spinning wanderer that periodically fires a "+"-shaped burst of 4 bullets. It is the only group-A enemy with a ranged attack.
Behaviour:
- Fades in over 0.3 s (`:60`), then random movement at 7 m/s with spinning, and **starts its shooting timer** (`:142-151`).
  It wanders like BigBlueCorosia (`:165-190`).
- Shooting (`:91-120`): a repeating NSTimer every `shootRate` seconds. The burst fires even when the player is far away or off-screen.
  - `shootRate` is **2.0 s**. If created during slow motion it is `2.0 + (1 − 0.35) = 2.65` s (`:31`).
  - On a slow-motion toggle `refreshEnemy` rebuilds the timer with `2.3 + 0.65 = 2.95` s in slow motion, or 2.0 s otherwise (`:84-89`).
  - Each tick (skipped when paused) spawns 4 `Bullet`s via its own `Weapon` (`:37`), with directions (−1,0), (0,1), (1,0), (0,−1).
    Each bullet starts 100 pt from the centre (`andStartDelta:100`).
  - Every bullet gets `tag = SHOOTING_COROSIA_BULLET_TAG (2)` (`:115-118`), so it ignores enemies.
  - Bullet properties come from `Bullet.initAtPosition:andRotation:andDirection:andStartDelta:` (`Weapon/Bullet.mm:62-99`):
    - Speed 30 m/s = 960 pt/s.
    - Box body with half-extents 0.1 m (3.2 pt), **not a sensor**, `bullet = true` (CCD), no density set.
    - Sprite `bullet.png` scale 1.0 at z 5, glow `Shine1.png` scale 0.5 at z 6.
  - Bullets fly until they hit the player (1 life), a bounder (the arena wall) or another bullet.
- Death by bullets (`hit`, `:197-206`): after 10 hits, `kDealloc` immediately. The sprite and glow are hidden and shooting stops.
  `releaseAgent` drops 7 vitamins and **removes all its live bullets instantly** (`:222-236`).
- Death by big explosion (`:208-220`): `kDying`. The sprite is hidden and shooting stops, but the body stays and keeps wandering
  invisibly. `update` turns it into `kDealloc` once `weapon isEmpty`, meaning all its bullets are gone (`:153-163`).

Numbers: speed 7 (`:29`), hitCount 10 (`:35`), killScore 130 (`:36`), vitamins 7 (`:41`), r = 91.2 pt (`:39-40`), scale 4 (`:47`).
ShootingCorosia has these defects:
- **QUIRK:** the slow-motion intervals are inconsistent (2.0 + 0.65 at creation, 2.3 + 0.65 on refresh).
- **QUIRK:** every bullet sprite gets rotation `0 − 90 = −90°` whatever its direction, so horizontal and vertical bullets look identical in orientation.
- **QUIRK:** `Weapon.updateBullets` multiplies `bullet.speed` by `timeScale` every frame (`Weapon/Weapon.mm:94`), but that field is
  never applied again after creation. It has no effect, because Box2D's scaled `dt` already slows the bullets.

Unity mapping:
- A `RadialBurstShooter` component (count 4, start angle 0°, step 90°, spawn offset, interval, projectile prefab).
- Use pooled enemy projectiles on a dedicated layer.
- Choose whether enemy projectiles die when the shooter dies (the original: yes for bullet kills, no for explosion kills).

### 3.8 Adrenalium (ID 9), `Adrenalium.mm`

Purpose: a hyperactive dasher. It rockets between points, and half of those points are where the player was standing.
Behaviour:
- Fades in over 0.1 s (`:60`), then random movement with spinning (`:86-92`).
- Each new target (`:72-84`): with 50% probability (`arc4random_uniform(2)`) it is the player's **current position, snapshotted**.
  Otherwise it is a random wander point (2.5).
- The speed is re-rolled from 18..25 m/s (576-800 pt/s) for every new target (`:83`). The initial speed of 20 (`:31`) is only used
  until the first target is chosen, which happens right after the fade.
- Within 10 pt of the target it picks the next one (`:119-124`).

Numbers: hitCount 1 (`:37`), killScore 150 (`:38`), vitamins 5 (`:39`), r = 32 pt. `NEXT_LENGTH_DELTA 500` is unused (`:16`).
Assets: `Enemy3.png`, `Shine3.png` (`:44-45`).
Unity: the Wander component with a target provider "50% player snapshot / 50% random" and a per-leg speed re-roll.
At 800 pt/s keep the arrival threshold larger than the per-step travel distance.

### 3.9 Chorux (ID 11), `Chorux.mm`

Purpose: a red homing cell protected by two orbiting cyan defenders. Shooting the defenders first is pointless, because they are
invulnerable until the Chorux core has been hit once.
Behaviour:
- Built on RedNoise (`super createAtPosition`, so the sprite is `Enemy1` red with no glow, `:29`). Then:
  - `speed 6`, `hitCount 1`, `killScore 200`, `vitamins 3` (`:36-42`).
  - The state is reset to Waiting and a 0.1 s fade runs (`:44-46`). The body is not deactivated during the fade, unlike most enemies.
  - It creates `CHORUX_DEFENDERS_COUNT = 2` `ChoruxDefender`s at the same position, with `targetToDeffend = self` (`:16,48-53`).
- After the fade: patrol (homing at the player) with spinning (`:73-79`). The generated `randomMovePoint` is unused.
- `update` runs RedNoise/EnemyAgent update, then updates the defenders and removes dead ones, one per frame (`:82-104`).
  **When the defender list is empty, Chorux sets itself `kDealloc`** (`:84-85`).
- `hit` (`:116-128`): ignored if already `kDying`. Otherwise `hitCount--`. At 0 it becomes **`kDying`** and the sprite is hidden.
  It keeps moving invisibly and can no longer hurt the player or absorb bullets. The defenders then become vulnerable.
- Big explosion (`:130-141`): same result (`kDying`). The defenders are not damaged by it.
- `releaseAgent` (`:143-159`): drops 3 vitamins if `hitCount <= 0` and releases any remaining defenders. Remaining defenders vanish
  with no reward. That happens only on a level reset.

**QUIRK:** Chorux's `hit` never produces `kDealloc`, so the contact listener's "flag == kDealloc then add score" never fires for it.
**Its 200 points are never awarded**. The group pays 2 × 90 (defenders) = 180 points in total.
Unity: a `Chorux` root with a `ShieldLinkedChildren` component. Model the core state explicitly (Alive, then CoreBroken, which is
invisible, harmless and intangible), and award score when the core breaks if 200 was the intent.

### 3.10 ChoruxDefender (ID 12, no factory case), `ChoruxDefender.mm`

Purpose: the Chorux bodyguard. It is not a satellite: it homes at the **player** with the SpiralCorosia loop pattern, and it
intercepts bullets while shielded.
Behaviour:
- Built on SpiralCorosia (inherits the cyan `Enemy3`, `Shine2`, the 0.1 s fade, and patrol after the fade).
  Then: speed 7 (`:29`), hitCount 1 (`:34`), vitamins 2 (`:35`), radius re-rolled 10..20 (`:36`). `killScore` stays at the
  inherited **90** (`SpiralCorosia.mm:37`).
- Patrol (`:56-78`): the same as SpiralCorosia, but `moveOnCircle` (`:80-93`) re-rolls radius 10..20 and alphaDelta 1..3 with no slow-motion variant.
- `canHit` becomes YES once `targetToDeffend.flag == kDying` (`:49-54`). Until then `hit` and `bigExploded` are ignored (`:114-124`).
  Bullets are still destroyed on contact, because the defender is not `kDying`, so it acts as a moving bullet shield. It always hurts
  the player on contact.
- Dies in 1 hit once vulnerable and drops 2 vitamins (`:100-112`).
- `pointToDeffend` is unused.

**QUIRK:** `targetToDeffend` is an `assign` pointer. It is safe only because Chorux releases its defenders before it is freed.
Unity: a SpiralMovement prefab with an `Invulnerable` flag bound to the owner's state.

### 3.11 Spawner (ID 13), `Spawner.mm`

Purpose: a big wandering "nest" that continuously spawns small enemies at its position until it is destroyed.
It is not used in campaign levels. It appears in the Rush/Take-the-Flag pool (`GameMode.mm:34`).
Behaviour:
- Built on BigBlueCorosia (scale-4 sprite, r = 91.2 pt, 0.3 s fade, spinning). Overrides: speed **15** (`:36`), hitCount 10 (`:40`),
  killScore 160 (`:41`), vitamins 10 (`:42`).
- After the fade (`:51-61`): it wanders (random movement, 2.5) and starts a repeating NSTimer.
- Spawn timer:
  - `spawnTime` is 1.0 s, or 1.65 s if created during slow motion (`:43`).
  - On a slow-motion toggle, `refreshEnemy` re-creates it with **0.5 s**, or 1.15 s in slow motion (`:63-76`).
  - Each tick (skipped when paused) picks uniformly from {Loctopus, Adrenalium, RedNoise, SpiralCorosia} (`:153-170`) and creates it
    at the Spawner's position via the factory. The child goes into the Spawner's private `spawnuses` list (`:172-173`).
  - **There is no cap on the number of children.**
- Children are updated by the Spawner (`:84-107`) and by the physics loop. When one is `kDealloc` it is released, and its vitamins
  drop if it was killed. Children are not in AgentsManager, so they are not hit by the big explosion and do not count for `getEnemiesCount`.
- `hit` (`:109-124`): at 0 HP it is `kDealloc` immediately, hidden, and the timer stops. Then `releaseAgent` **deletes all living
  children instantly**, without reward.
- Big explosion (`:126-145`): becomes `kDying`. It is hidden, its body is deactivated, and the timer stops. It finally becomes
  `kDealloc` once all its children have been killed (`:99-105`).
- `releaseAgent` (`:176-202`): releases the children, then **always** drops 10 vitamins (there is no `hitCount <= 0` check).

Spawner has these defects:
- **QUIRK:** after any slow-motion toggle the spawn interval halves, from 1.0 s to 0.5 s.
- **QUIRK:** the 10 vitamins (credited immediately, see 2.8) are also granted when a Spawner is cleaned up by a level restart, a win or a game over.
- **QUIRK:** `vitaminsCount` and the other stats inherited from BigBlueCorosia are set twice. This is harmless.

Unity: an `EnemySpawnerBehaviour` component with a weighted child table, an interval and an optional maximum number of live children.
Make children ordinary registry members, or keep them parented. In either case decide what happens to them when the nest dies.

### 3.12 Tentacle (ID 17), `Tentacle.mm`

Purpose: the hazard in Peace Walker mode (a non-violent mode). It is a slow, spinning, **indestructible** wanderer that you must avoid.
Behaviour: wanders like BlueCorosia at 3 m/s (`:27`), with a 0.1 s fade (`:54`) and spinning (`:82`). `hit` and `bigExploded` are
empty (`:102-110`), so it can never die. Bullets that touch it are still destroyed. Contact hurts the player. No score (killScore unset)
and no vitamins.
Spawned by `PeceWalkerGameMode` (2 at start, plus 1 per collected flag, `PeceWalkerGameMode.mm:21-22,90-93,146-151`) and by the
dead `AgentsManager.createAgentsCircleCrowd`.
Unity: a Wander prefab with `IDamageable` disabled (or absorbing hits).

### 3.13 ZigZagus (ID 18), `ZigZagus.mm`

Purpose: a homing cell that wobbles as it approaches.
Behaviour:
- Fades in over 0.1 s (`:57`), then patrol (`:69-73`). Each update (`:95-117`):
  1. Homing `v = dir × 7`, sprite faces the player.
  2. Add a wobble: `v += 20·cos(θ)·(1, 1)` with `θ = sinAlpha + sprite.rotation` (degrees). The method is misnamed
     `generateNextRandomPoint` (`:75-88`).
  3. `sinAlpha -= alphaConst` (initially 5).
  4. Once `|sinAlpha| > 360`: `alphaConst = −rand(3..7)`.

Numbers: speed 7 (`:26`), `sineMoveSpeed 20` m/s (`:27`), `alphaConst 5` (`:37`), hitCount 1 (`:31`), killScore 160 (`:32`),
vitamins 4 (`:38`), tint blue, glow `Shine2.png`.

ZigZagus has these defects:
- **QUIRK:** x and y both use `cos` (`:78-79`), so the wobble is always along the fixed world diagonal (1,1). It is not perpendicular
  to the motion. The y term was probably meant to be `sin`.
- **QUIRK:** after the first flip, `sinAlpha` climbs forever and `|sinAlpha| > 360` stays true. So `alphaConst` is re-rolled to a new
  random negative value **every update**, and the phase advances a random 3-7° per update instead of oscillating cleanly.
- The wobble is about ±20 m/s at about 600°/s (5°/update × 120 updates/s). That gives a lateral amplitude of about 20 / 10.5 rad/s ≈ 1.9 m ≈ 61 pt (derived).
  It is frame-rate dependent.

Unity: a ZigZag component with an amplitude (m/s), a frequency (Hz) and an axis (perpendicular to the heading, if the fix is taken).

### 3.14 Gnida (ID 20), `Gnida.mm`

Purpose: a homing cell that **dodges** sideways when the player is aiming at it and shooting. It flashes white while dodging.
Behaviour:
- Fades in over 0.1 s (`:58`), then patrol (`:68-72`). Each update (`:79-109`):
  1. Homing `v = dir × 10`, sprite faces the player.
  2. Aim test (`:95-97`). Let `r` be the enemy's facing angle toward the player and `p` the player's rotation (`AiInput.mainCharacterRotation`).
     With `s = (r > 0) ? −1 : 1`, the test is true when `| |r| − |180 − s·p| | ≤ EVADE_DELTA_ALPHA (30°)` (`Gnida.h:11`).
     In other words, the player's facing points within ±30° of the enemy (this assumes `p` is in (−180, 180]; inferred).
  3. If aimed at **and** `AiInput.characterIsshooting`: tint white (255,255,255) and `fastEvade`.
     If not aimed at, the tint returns to blue (aimed at but not shooting keeps the current tint).
  4. Apply `movementVelocity`.
- `fastEvade` (`:111-117`):
  1. `perp = (perpendicularRandomer == 0) ? rperp(v) : perp(v)`. The side is fixed per enemy at spawn by `arc4random_uniform(2)` (`:39`).
  2. `perp ×= speed/2` (× 1.3 in slow motion).
  3. `perp.y = −perp.y`.
  4. `v += perp`.

Numbers: speed 10 (`:27`), hitCount 1 (`:31`), fleeDistance 300 (unused, `:32`), killScore 200 (`:33`), vitamins 5 (`:37`),
r = 32 pt, tint (0,0,255) (`:51`), glow `Shine2.png`.
Gnida has these defects:
- **QUIRK:** `perp` is built from the un-normalised velocity (magnitude 10) and then multiplied by `speed/2` (5). The sideways
  component is therefore **50 m/s (1600 pt/s)**, a teleport-like sidestep. The y-flip also makes it not truly perpendicular.
  Normalising first was probably intended. Play-test before deciding.
- The dodge applies only on frames where both conditions hold, so in practice it jitters sideways while the fire button is held and aimed.

Unity: a DodgeWhenAimedAt component that reads the player's aim vector (dot product against a cone of 30°) and `isFiring`, and adds a
lateral impulse or velocity. Use proper vector math instead of the angle hack.

### 3.15 ZamriGnida (ID 21), `ZamriGnida.mm`

Purpose: a Gnida variant ("zamri" is Russian for "freeze") that loops like a SpiralCorosia and dodges even harder.
Behaviour:
- `createAtPosition` does not call Gnida's version. It builds its own sprite and body (`:16-62`), which avoids a duplicate sprite.
  It never calls any `super` initialiser, which matches the other enemies (see 2.1).
- Patrol (`:75-108`) has the same aim/dodge logic as Gnida. Then `moveOnCircle` (`:110-124`) **overwrites** the velocity with
  `movementVelocity + radius·(cos α, sin α)`. Because `movementVelocity` already includes the dodge, the dodge survives.
  - Initial radius is rand 10..20 and alphaDelta rand 1..3 (`:31-32`).
  - Re-roll at 360°: radius 7..16 (12..20 in slow motion), alphaDelta 1..3 (2..4 in slow motion) (`:120-121`).
- `fastEvade` (`:126-132`) scales `perp` by `speed`, not `speed/2` (× 1.3 in slow motion). That is 6 × 6 = **36 m/s** sideways.

Numbers: speed 6 (`:23`), hitCount 1 (`:27`), killScore 250 (`:29`), vitamins 6 (`:36`), r = 32 pt, tint blue, glow `Shine2.png`, fade 0.1 (`:57`).
QUIRKS: the same as Gnida and SpiralCorosia (un-normalised dodge, per-update angles).
Unity: compose Homing + Spiral + DodgeWhenAimedAt.

### 3.16 BigRedNoise (ID 24, boss of world 3, level 30), `BigRedNoise.mm`

Purpose: a giant red cell boss. It chases the player and fades from opaque to translucent as it takes damage. At three health
thresholds it stops, spins up and spews a stream of RedNoise minions. It is invulnerable while it does so.
Behaviour:
- Creation (`:38-97`):
  - Speed **12 / 15 / 18 m/s** for Normal / Hard / Impossible (`:48-59`).
  - `hitCount = MAXIMUM_HIT_COUNT 400` (`:22,64`), width 10.3, so r = 164.8 pt (`:66-67`), vitamins 10 (`:69`).
  - Sprite `Enemy1.png` tinted red, scale 7, z **4** (`:71-85`). The `glowSprite` is never created (its scale is set on `nil`).
  - 1.0 s fade (`:87`). Unlike small enemies, **the body stays active during the fade**: it is stationary (Waiting) but already harmful and hittable.
  - `opacityDecreaserStep = 255/400` (`:91`).
- After the fade: `kStatePatrol`, i.e. base homing (`:99-102`). It does not call super, which is harmless here.
- `hit` (`:240-279`) is ignored while spawning (`bornOfChilds`). Otherwise:
  - `hitCount--`.
  - The sprite opacity decreases by 0.6375, which in practice is 1 per hit because GLubyte truncates. It is clamped at
    `FINAL_OPACITY_LIMIT 70` (`:21,246-250`).
  - At 0 HP: `kDealloc` and a call to `GameManager.levelHasWonAfterBigBoss` (`:253-259`).
  - Wave triggers, each at most once (`:262-276`):
    - First wave when `200 < hitCount < 300` (`FIRST/SECOND/THIRD_CHILD_BORN_LIMIT 300/200/100`, `:16-18`).
    - Second wave when `100 < hitCount < 200`.
    - Third wave when `0 < hitCount < 100`.
- `bornChilds` (`:104-117`): enter Waiting, set velocity 0 (`update` forces 0 while spawning, `:150-156`), and spin at 90°/0.1 s
  (900°/s). Then start a repeating NSTimer of 0.1 s (0.4 s in slow motion; refresh uses the same values, `:183-201`).
- Each tick (`:119-148`), skipped when paused:
  - Create a `RedNoise` at the boss position with **`SetSensor(false)`** (a solid body) and `bornFromBoss = YES`. It goes into the
    boss's private `childrens` list.
  - When `childrens.size() == enemiesWaves[wave]`, with `enemiesWaves = {60, 80, 90}` (`:25-28`), the wave ends:
    - **only that last minion** gets ×1.1 speed;
    - the boss stores `prevSpeed = speed` and moves at **speed / 3**;
    - the timer stops, the spin stops, and the boss returns to Patrol.
- `update` (`:150-166`): while alive it updates its children and removes dead ones. When no children remain, the speed is restored to `prevSpeed`.
- `releaseAgent` (`:213-238`): 10 vitamins if killed, stops the timer, and releases all remaining minions. They give no reward.

Rewards: killScore is never set, so the boss pays **0 points**. Each minion pays 80 in campaign mode when shot, with no vitamins.
The level is won by the boss kill (5 s later the next level loads, or the game is finished if this was level 50; `GameManager.mm:219-239`).

BigRedNoise has these defects:
- **QUIRK:** wave size counts **all living minions**, including survivors of earlier waves. If the player has not cleaned up, the next
  wave spawns fewer new ones. The total live count reached is 60, 80, 90.
- **QUIRK:** only the final minion of a wave gets the ×1.1 speed bonus (`:132`). It probably was meant for all of them.
- **QUIRK:** the strict inequalities mean that exactly 300, 200 or 100 HP does not trigger a wave. The next hit does, so this is harmless.
- **QUIRK (potential soft-lock):** BigRedNoise does not override `bigExploded`. The base version (−20 HP) can bring it to 0 and set
  `kDealloc` **without** calling `levelHasWonAfterBigBoss`. The boss would disappear and the level would never end (the boss level has no
  timer win). It also bypasses the invulnerability while spawning, and does not trigger waves.
- **QUIRK:** the minions are non-sensor dynamic bodies (density 1). They physically shove each other, the player, and possibly the
  bounders. Normal enemies are sensors.

Unity mapping:
- A `BossBigRedNoise` state machine: FadeIn (1 s, damageable), Chase, SpawnWave (invulnerable, stationary, spin, one spawn per 0.1 s
  until N alive), SlowChase (speed/3 until all minions are dead), back to Chase.
- Put the HP thresholds and wave sizes in a ScriptableObject.
- Route every death path (bullets and explosion) through one `OnBossDefeated`.

### 3.17 Dead or trivial framework classes

- **EnemyBullet** (`EnemyBullet.h/.mm`): only forwards to `Bullet`'s initialiser. It is never instantiated (ShootingCorosia uses a
  plain `Bullet`). `kEnemyBullet` (`ConfigConstants.h:85`) is also unused. Dead.
- **LongBacteria** (`LongBacteria.h/.mm`): an `EnemyAgent` subclass with `vector<EnemyAgent*> bodies` and an empty `updateBodies`.
  It is the base for CaterPillar, Serpentus, LotusBacterius and ShootingSerpentus (group B).
  Unity: a `SegmentedEnemy` base with a segment list.
- **ShooterBoss** (`ShooterBoss.h/.mm`): an empty `EnemyAgent` subclass, commented `// erserved` (reserved). Unused. Dead.

---

## 4. Top findings for the rewrite (summary)

1. **Every enemy `update` runs about twice per frame**: once from the Box2D body loop and once from AgentsManager or the parent
   (`GameManager.mm:466-472` plus `AgentsManager.mm:166-184`). Per-update angle steps (spiral, zig-zag) are effectively doubled.
   Velocities are unaffected. Tune Unity angular speeds as `step × 120 °/s`.
2. **Scoring holes**: points are paid only in campaign mode, only for bullet kills, and only when `hit` returns `kDealloc`.
   Chorux (200) is never paid. BigRedNoise, Gridium and Tentacle pay 0. Big-explosion kills pay 0 but still drop vitamins.
   Vitamins are credited when they drop, not when collected.
3. **Factory gaps**: IDs 12, 16 and 27 return `nil`, so Armagedon mode spawns `nil`s. The enum comment for 27 says 22.
   The Rush/Take-the-Flag pool reads one entry past its 19-element array.
4. **Boss edge cases**: BigRedNoise can be killed by the big explosion without triggering the level win (soft-lock). Level 20's data
   probably spawns 2 bosses because the frame-based grow counter beats the 2 s timer (inferred). The win check needs exactly 1 agent.
5. **Movement numbers look unintended in places**: the Gnida and ZamriGnida dodge uses an un-normalised perpendicular (50 and 36 m/s
   sidesteps). The ZigZagus wobble is locked to the world diagonal and its phase gets re-randomised every update. The Spawner interval
   halves after any slow-motion toggle. Loctopus drops 2 vitamins, not 3. Decide case by case whether to keep each one.
