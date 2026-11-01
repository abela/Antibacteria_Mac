# 03 - Enemies group B (segmented enemies) and Bosses

Source root: `/home/claude/legacy` (paths below are relative to it). All AI classes live in `AI/`.

## Overview

| Class | Role | One-line summary |
|---|---|---|
| `Serpentus` | Chain enemy (ID 14) | 6-segment snake. Only the front segment can be damaged; when it dies the next segment becomes the head. |
| `SerpentusBody` | Chain segment | Segment used by Serpentus, ShootingSerpentus and Anakonda. A `Loctopus` that never moves on its own and only takes damage while it is the head (`tag == 0`). |
| `ShootingSerpentus` | Chain enemy (ID 19) | 5-segment Serpentus whose head fires an aimed bullet every 1.5 s. |
| `Anakonda` | **Boss 4 (ID 25, level 40)** | 24-segment giant snake (segments drawn at 2x scale). Fast, shoots aimed bullets, and keeps adding new tail segments that fly in from the arena edge. |
| `AnakondaFeed` | Dead | Empty `EnemyAgent` subclass with no code and no references. |
| `CaterPillar` | Chain enemy (ID 10) | 20-segment worm. Killing the front segment kills the whole worm. |
| `CaterPillarBody` | Chain segment | Small segment with a sensor collider. Damage applies only on the head (10 hits). |
| `LotusBacterius` | Chain enemy (ID 15) | 7-segment snake. When the head dies, the remaining 6 segments split off as individual spiralling homing enemies. |
| `LotusBacteriaBody` | Chain segment | A `SpiralCorosia` (1 HP) that waits in the chain until the split. |
| `Frunctus` | **Boss 5 (ID 26, level 50, final boss)** | Spiralling, dodging, edge-escaping shooter. **Has 1 HP.** |
| `Sunux` | Turret enemy (ID 8) | Large spinning "sun" that never moves and fires an 8-way burst every 2 s. Not used in campaign levels. |
| `Inferno` | **Boss 2 (ID 23, level 20)** | Huge yellow spinning blob, 300 HP, gets faster as it is damaged. Brings 10/20/25 invulnerable wandering "Tentacles". |
| `BigHunterus` | Big chaser (ID 5) | Large, slow homing enemy with 5 HP. No attack apart from contact. |
| `BigBoss` | **Boss 1 (ID 22, level 10)** | Giant fast-roaming blob, 255 HP, darkens from white to black as it takes damage. Contact damage only. |
| `ArmagedonBacteria` | Dead | Small wanderer for an unfinished "Armagedon" mode. Its factory case is commented out, so it can never spawn. |
| *(cross-ref)* `BigRedNoise` | **Boss 3 (ID 24, level 30)** | Not one of my assigned classes, but it is the level-30 boss, so its boss behaviour is documented in section 6.3. |

Base classes (documented elsewhere and summarised here only where they affect behaviour): `Agent`, `EnemyAgent`, `LongBacteria`, `ShooterBoss`, plus the concrete bases `Loctopus` (for SerpentusBody) and `SpiralCorosia` (for LotusBacteriaBody).

---

## 1. Units, loop and inherited behaviour you need to know

### 1.1 Units
- **PTM ratio:** `PTM_RATIO 32` (`GameCore/Physics/Abstracted/Physics.h:18`). Gameplay code converts between Box2D and screen with `ASPECT_RATIO 32.0f` (`ConfigConstants.h:353`), so **1 m = 32 pt**.
- All `speed` values below are **Box2D linear velocities in m/s**, written directly into `SetLinearVelocity`. Multiply by 32 to get pt/s. Example: speed 10 = 320 pt/s.
- Collider sizes: `Agent::createNewBodyAt` builds a **circle of radius = width/2 metres** (`Agent.mm:97`). Example: width 2.0 gives r = 1 m = 32 pt.
- Gravity is zero (`Physics.mm:28`).
- Enemy sprite frames `Enemy1.png` and `Enemy3.png` have a 64x64 source size. `Shine1/2/3.png` glows are 128x128. Enemy `bullet.png` is 28x22 (`out/data/raw/characters_resources.json`). Visual diameter is 64 pt x `sprite.scale`.

### 1.2 Update loop (frame-based)
- `MainGameLayer` schedules `tick:` every frame (`Layers/MainGameLayer.mm:153`). That calls `GameManager managePhysics:` with the frame `dt`: `world->Step(dt, 8, 2)` (`Managers/GameManager.mm:420-427`). The `dt` is scaled by the cocos2d scheduler timeScale, which is 0.35 in "bullet time" (`GameManager.mm:32,336`).
- **Every enemy AI tick is per frame. There is no fixed timestep.**
- **QUIRK (double update):** after `Step`, the physics loop calls `update` on every body whose user data has `type == kEnemy` (`GameManager.mm:465-472`). Then `gameWorld update` -> `GameModeManager update` -> `AgentsManager update` calls `update` again on every agent in its list (`AgentsManager.mm:166-184`). The effect depends on where the object sits:
  - Enemies that own a body **and** are in the AgentsManager list (BigBoss, Inferno, Frunctus, Sunux, BigHunterus, BigRedNoise) are **updated twice per frame**.
  - Chain "head controller" objects own no body, so they are updated once per frame.
  - Chain segments are updated by the physics loop and sometimes also by their owner. LotusBacteriaBody after the split, Inferno's Tentacles and BigRedNoise's children get up to **3 updates per frame**.
  - This doesn't matter where the AI only *sets* a velocity. It **does** matter for per-update counters (`spiralAlpha += alphaDelta`) in Frunctus and SpiralCorosia: their spirals turn 2-3x faster than the numbers suggest.
- Shooting and spawning timers are `NSTimer`s. They count **real-time seconds**, ignore the cocos2d timeScale, keep running while paused (each callback checks `gameIsPaused` and returns early), and are re-created by hand in `refreshEnemy` when bullet time toggles.

### 1.3 EnemyAgent behaviour these classes inherit (summary)
- `moveToPosition:v` sets the body velocity to `v` (m/s) and snaps sprite and glow to the current body position (`EnemyAgent.mm:50-56`). Steering has **no acceleration and no turn rate**: the velocity is replaced every update.
- `update` is a switch on `agentStateType`: Waiting = nothing, Patrol = home on player, RandomMovement = `randomfly`, and so on. It then calls `updateEnemyBullets` (`EnemyAgent.mm:78-110`).
- `patrol`: velocity = normalise(player - pos) x `speed`. Sprite rotation = `acos(dir.y)` in degrees, negated when the player is to the left. That is cocos2d clockwise rotation from "up" (`EnemyAgent.mm:58-76`).
- `randomfly`: same as patrol but toward `randomMovePoint`, with no sprite rotation (`EnemyAgent.mm:121-140`).
- `hit`: `hitCount--`, and when `hitCount <= 0` it sets `flag = kDealloc` (`EnemyAgent.mm:266-270`). `bigExploded`: if the player is within `BIG_EXPLOSION_DISTANCE` (700 pt) of `sprite.position`, `hitCount -= BIG_EXPLOSION_HIT_CONST` (20) (`EnemyAgent.mm:252-259`, `ConfigConstants.h:370-371`).
- `creatingAnimationHasEnd:` sets opacity to 255 and activates the body (`EnemyAgent.mm:246-250`). Subclasses chain to it after their spawn fade-in.
- `Agent::createNewBodyAt`: dynamic body, `fixedRotation=false`, `allowSleep=false`, circle shape, `density 1`, **`isSensor = true`** (`Agent.mm:81-105`). No category or mask bits are set, so Box2D defaults apply. **Every enemy collider is a sensor.** Nothing is ever pushed physically. Contacts are only used for damage.
- Death and score are handled by `ContactListener::BeginContact`:
  - **Player bullet vs enemy:** the bullet is destroyed unless the enemy's flag is `kDying`. `[enemy hit]` is called. If the enemy is now `kDealloc` and the mode is Campaign, the enemy's `killScore` is added (`GameCore/Physics/Abstracted/ContactListener.mm:60-87`). Bullets tagged `SHOOTING_COROSIA_BULLET_TAG` (2) or `FRUNCTUS_BULLET_TAG` (3) never damage enemies (`ConfigConstants.h:429-431`).
  - **Player vs enemy (any enemy not `kDying`):** `[character hit]`, which costs 1 life and gives a 2 s blink of invulnerability (`ContactListener.mm:89-105`, `Characters/MainCharacter.mm:705-710`).
  - **Player vs any `kBullet`:** player is hit and the bullet is destroyed (`ContactListener.mm:107-119`).
  - **Bullet vs bullet:** both are destroyed (`ContactListener.mm:43-49`), so player shots cancel enemy shots.
  - **Bullet vs bounder (arena wall):** the bullet is destroyed (`ContactListener.mm:51-58`).
- `AgentsManager update` releases an agent the frame after its flag becomes `kDealloc` (`releaseAgent` + `release`, erase, then `break`, so only one removal per frame) (`AgentsManager.mm:166-184`). Big-explosion (`AgentsManager.mm:116-126`) and refresh (`AgentsManager.mm:155-164`) only reach objects that are **in the agents list**. Chain segments are not in that list.

### 1.4 Enemy bullets (used by ShootingSerpentus, Anakonda, Frunctus, Sunux)
- Fired with `Weapon shootAtPosition:andRotation:andDirection:andStartDelta:` (`Weapon/Weapon.mm:74-86`). The `Bullet` has `type kBullet` and `speed 30` m/s (960 pt/s), with velocity = dir x 30/|dir|, so the speed is always 30 for any direction length (`Weapon/Bullet.mm:67-69,90-93`).
- It spawns `startDelta x dir` points ahead of the shooter (`Bullet.mm:80-81`). The physics body is a 0.1 m half-extent box with `bullet = true` (`Bullet.mm:112,122`). Sprite `bullet.png` sits at z 5 and glow `Shine1.png` (scale 0.5) at z 6 (`Bullet.mm:71-77,95-96`).
- Enemy bullets only die by hitting the player, another bullet or the arena wall. They have no lifetime.

### 1.5 Spawn helpers referenced below (SpawnManager, documented elsewhere)
- `getValidOuterSpawn`: a random point on one of the 4 arena edges (`Managers/SpawnManager/SpawnManager.mm:73-104`). `AgentsManager createRandomAgentAtPosition` always uses it and ignores the requested position (`AgentsManager.mm:81-90`). **All bosses and chain enemies spawn on the arena border.**
- `getRandomPointAroundPoint(p)`: `p` offset by 500-520 pt in a random direction, clamped to the arena bounds inset by 30 pt (`SpawnManager.mm:327-349`, `ConfigConstants.h:405,410-411`).
- `getValidSpawnPosFromCharacter`: a point 500-600 pt from the player in a random direction, reflected back inside the arena (`SpawnManager.mm:192-225`, `ConfigConstants.h:406,409`).
- The arena bounds come from `CoreSettings` (background sprite rect inset by 20 pt, `GameWorld/GameWorld.mm:73-85`).
- "Random roam target" pattern, used by every class here: pick a uniform random point inside the arena, then `getRandomPointAroundPoint` it. Several classes instead pick the **player's current position (a snapshot, not a live track)** with some probability. A new target is picked when the controlled sprite is **within 10 pt** of the current one.

---

## 2. The chain ("LongBacteria") system

`LongBacteria` (`AI/LongBacteria.h:11-16`) only adds `vector<EnemyAgent*> bodies` and an empty `updateBodies` (`LongBacteria.mm:12`). All chain logic is copy-pasted into each subclass: Serpentus, ShootingSerpentus, Anakonda, CaterPillar, LotusBacterius. The common design is below. Per-class differences are in the class sections.

### 2.1 Structure
- The **chain object itself (the "controller") has no sprite and no physics body.** It is the object in `AgentsManager`. It owns N segment objects, each a full `EnemyAgent` with its own sprite, glow and sensor body.
- Segment `0` is the visible "head". The controller drives `bodies[0]`'s body directly.
- **Segments are built** in the controller's `createAtPosition`. Segment *i* is placed at `spawn + (40 * i, 0)`: a straight horizontal line to the right of the spawn point, 40 pt apart (`*_BODY_DELTA 40.0f`). Each gets `tag = i`, its sprite opacity is set to 0, and it receives a 0.1 s `CCFadeIn`. The fade's completion callback targets the **controller's** `creatingAnimationHasEnd:`, which fires N times (harmless). That callback picks a roam target, sets the controller to `kStateRandomMovement` and activates `bodies[0]`'s body.
- For Serpentus, ShootingSerpentus, Anakonda and Lotus, segment 0 gets sprite frame `Enemy1.png` with white colour. The others keep their class look. This is how the head is shown.

### 2.2 Linking: no joints, pure position following
There are **no Box2D joints**. Each frame the controller (in `randomfly`) does three things:
1. Sets `bodies[0]` velocity = normalise(target - head) x `speed`. Head sprite and glow snap to its body position and the head sprite is rotated toward the target.
2. Calls `updateBodies`. For `i = 0 .. size-2`, ascending, so the chain cascades within one frame:
   ```
   newPos = ccpLerp(bodies[i].pos, bodies[i+1].pos, lerpParameter);   // = 0.13*leader + 0.87*self
   bodies[i+1].sprite.position = newPos; rotation = facing toward bodies[i];
   bodies[i+1].body->SetTransform(newPos / 32, 0);                    // teleport the sensor body
   ```
3. `lerpParameter` = **0.87** normally and **0.95** while bullet time is on. It is re-evaluated in `refreshEnemy` (e.g. `Serpentus.mm:30,65`).

What the player sees: each follower closes 13% of the gap to its leader **per frame**. In steady state, gap = alpha x s / (1 - alpha), where s is the head's movement per frame. That is about **6.7 x the head's per-frame displacement**:
- Speed-10 chains (Serpentus, ShootingSerpentus, CaterPillar, Lotus) at 60 fps: s = 5.33 pt, so the gap is about **36 pt**.
- Anakonda (speed 17): about **61 pt**.
- In bullet time the head moves 0.35x as far per frame. Switching alpha to 0.95 keeps the gap almost identical (6.65 x 0.35 s vs 6.69 s). The 0.87 -> 0.95 change was evidently tuned for this.
- **QUIRK (frame-rate dependent):** segment spacing is proportional to the frame time. At 30 fps the chain is twice as long, at 120 fps half as long. The 40 pt spawn spacing is not kept.
- The segment colliders (r = 32 pt for SerpentusBody and LotusBacteriaBody) overlap heavily at 36 pt spacing.

### 2.3 Damage rules
- Each segment overrides `hit` and `bigExploded` to act **only when `tag == 0`** (`SerpentusBody.mm:54-68`, `CaterPillarBody.mm:40-59`, `LotusBacteriaBody.mm:57-71`). **Only the current head can be damaged.**
- Every other segment is still a live `kEnemy` sensor:
  - It **damages the player on contact**.
  - It **absorbs player bullets**: ContactListener destroys the bullet before calling the no-op `hit` (`ContactListener.mm:62-66`). The body acts as a shield for the head.
- Score is awarded **per segment**, using the segment's own `killScore`, because ContactListener calls `hit` on the segment's body, not on the controller. **The controller's declared `killScore` (400/500/300/400) is never awarded.** See the table in 2.5.
- **QUIRK (big explosion on chains):** `AgentsManager bigExplosion` calls `bigExploded` on the **controller**, not on the segments. Serpentus, CaterPillar, LotusBacterius and Anakonda don't override it, so `EnemyAgent bigExploded` runs with a nil `sprite`. A nil message returns position (0,0), so the test is the player's distance to **world origin**. `hitCount` is 0, so `0 - 20 <= 0` and **the whole chain is deleted instantly** (no score, no vitamins from the living segments) whenever the player is within 700 pt of (0,0), which is roughly the lower-left part of the arena. Otherwise nothing happens. ShootingSerpentus overrides `bigExploded` properly. The segment-level `bigExploded` overrides are dead code. (Inference: this relies on Obj-C nil-receiver struct returns being zero, which holds on x86_64/arm64 macOS.)

### 2.4 When the head or a segment dies
These rules are for Serpentus, ShootingSerpentus and Anakonda. CaterPillar and Lotus differ (sections 3.4 and 3.5).
- The head's `hit` drops it to `hitCount <= 0`, which sets `flag = kDealloc` on the segment.
- On the next controller update `updateBodies` sees `bodies[i].flag == kDealloc`:
  - **If only 2 segments remain**, the whole chain dies: the controller flag becomes `kDealloc` (Anakonda also triggers the boss win, ShootingSerpentus goes to `kDying`). The last living segment dies with it.
  - **Otherwise** the dead segment is released (it drops vitamins) and erased. The new `bodies[0]` gets `tag = 0`, frame `Enemy1.png` and white colour. It is now the vulnerable head. The function returns, so the chain doesn't move that frame (a one-frame hitch).
- Practical consequence: a chain of N segments dies after **N - 1 head kills**.
- The `for` loop only scans up to `size-2`, so a dead tail segment would never be noticed. Only the head can die, so this never happens.

### 2.5 Chain totals (Normal difficulty, campaign)

| Enemy | Segments | HP per head kill | Head kills to destroy | Total hits | Score actually awarded | Vitamins dropped |
|---|---|---|---|---|---|---|
| Serpentus | 6 | 7 | 5 | 35 | 5 x 80 = **400** | 5 x 3 = 15 |
| ShootingSerpentus | 5 | 4 | 4 | 16 | 4 x 80 = **320** | 4 x 3 = 12 |
| Anakonda (boss) | 24 + 1 every 3 s | 14 (originals) / 5 (added) | (current length - 1) | >= 322 + 5 per added segment | 80 per segment kill (score HUD hidden on boss levels) | 3 per segment, plus one "10" vitamin at (0,0) |
| CaterPillar | 20 | 10 | 1 | 10 | **0** (segment killScore unset) | 0 |
| LotusBacterius | 7 | head 10, then 1 each | 7 | 16 | 7 x 90 = **630** | 7 x 6 = 42 |

Where the numbers come from:
- SerpentusBody score is `Loctopus` `killScore 80` (`Loctopus.mm:29`).
- Vitamins per SerpentusBody: 1 from its own `releaseAgent` (`SerpentusBody.mm:18,43-49`), plus a hard-coded 2 from `Loctopus releaseAgent` (`Loctopus.mm:85-90`), for **3**.
- LotusBacteriaBody: 3 from its own `releaseAgent` plus 3 from `SpiralCorosia releaseAgent` (`LotusBacteriaBody.mm:21,46-52`, `SpiralCorosia.mm:38,128-134`), for **6**. Its score is SpiralCorosia's 90 (`SpiralCorosia.mm:37`).
- **QUIRK:** these double drops come from both the subclass and the superclass dropping vitamins. Probably unintended.

**Unity mapping (chain system)**
- One `ChainController` MonoBehaviour owns an ordered list of segment GameObjects (kinematic Rigidbody2D + trigger CircleCollider2D). There are no joints.
- Replace the per-frame lerp with a frame-rate-independent follow. Either use `pos = Vector2.Lerp(leader, pos, Mathf.Pow(0.87f, dt*60))`, or better, follow at a fixed spacing (~36 pt / 1.1 m for speed-10 chains) sampled from a head position history.
- Segment state is just `isHead`, `hp` and `vulnerable = isHead`. Head death becomes an event on the controller that promotes the next segment.

---

## 3. Chain enemies

### 3.1 `SerpentusBody` (segment class; `AI/SerpentusBody.h/.mm`, base `Loctopus`)
Purpose: a passive chain link. It inherits everything from `Loctopus` (`AI/Loctopus.mm`) and changes the following:
- `speed 12.0f` (unused because it never patrols), `agentStateType = kStateWaiting`, `hitCount 7`, `vitaminsCount 1` (`SerpentusBody.mm:15-18`).
- `creatingAnimationHasEnd`: `Loctopus` sets Patrol, and SerpentusBody immediately sets it back to **Waiting** (`SerpentusBody.mm:24-28`). Its own update (from the physics loop) therefore does nothing except an empty bullet update.
- `hit` and `bigExploded` only when `tag == 0` (`SerpentusBody.mm:54-68`).
- `releaseAgent`: if dead, drops `vitaminsCount` vitamins of value 1, then `Loctopus` drops 2 more (see 2.5).

Inherited from Loctopus:
- Collider r = 1 m = 32 pt sensor (`width 2.0f`, `Loctopus.mm:32-33,43`).
- Sprite `Enemy3.png` tinted blue `(0,0,255)`, glow `Shine2.png`. Glow at z 0 and sprite at z 1 in the shared character batch node (`Loctopus.mm:35-36,45,49-50`).
- 0.1 s fade-in, body inactive until the fade completes (`Loctopus.mm:46-47,52-54`).
- `killScore 80` (`Loctopus.mm:29`).

The owning chain may override `hitCount` and `sprite.scale` (see below).

### 3.2 `Serpentus` (ID 14; `AI/Serpentus.mm`)
Purpose: a fast snake that roams the arena and periodically charges where the player was. You have to shoot through its head repeatedly; the body blocks shots.

| Item | Value | Ref |
|---|---|---|
| Segments | `SERPENTUS_BODIES_COUNT 6` | `Serpentus.mm:15` |
| Spawn spacing | `SERPENTUS_BODY_DELTA 40.0f` pt | `Serpentus.mm:16` |
| Head speed | `10.0f` m/s (320 pt/s) | `Serpentus.mm:27` |
| Segment HP | 7 (SerpentusBody default) | `SerpentusBody.mm:17` |
| lerp | 0.87 / 0.95 in bullet time | `Serpentus.mm:30,65` |
| killScore (declared, never awarded) | 400 | `Serpentus.mm:29` |
| Head frame | `Enemy1.png`, white | `Serpentus.mm:47-51` |

Behaviour:
- **Spawn:** the chain fades in over 0.1 s, then the controller goes to `kStateRandomMovement` (`Serpentus.mm:86-92`).
- **Movement:** `randomfly` steers the head at constant speed toward `randomMovePoint`, then runs `updateBodies` (`Serpentus.mm:94-114`). Target choice: 50% a random roam point, 50% the **player's current position snapshot** (`arc4random_uniform(2) == 0` gives random, `Serpentus.mm:75-84`). A new target is picked when the head is within 10 pt (`Serpentus.mm:122-123`).
- **Damage and death:** chain rules from section 2. When the second-to-last head dies, the whole thing is removed (`Serpentus.mm:131-136`).
- No attack apart from contact.

Dependencies: `SpawnManager`, `AiInput` (player position), `CoreSettings`, `GameManager.bulletinTimeIsOn`, `VitaminManager` (through the segments).

Quirks:
- **QUIRK:** big explosion kills it only near world origin (2.3).
- **QUIRK:** awarded score comes from the segments (5 x 80), not the declared 400. It happens to total 400.

Unity: `ChainController` with `segmentCount = 6`, `headSpeed = 10 m/s` and a `RoamTargetPicker` (50% player snapshot).

Used in campaign levels 31-32 and in the generic random pool of the other modes (`Managers/GameModeManager/GameModes/GameMode.mm:40`).

### 3.3 `ShootingSerpentus` (ID 19; `AI/ShootingSerpentus.mm`)
Purpose: a shorter, weaker Serpentus whose head shoots aimed bullets at the player.

| Item | Value | Ref |
|---|---|---|
| Segments | 5 | `ShootingSerpentus.mm:16` |
| Spawn spacing | 40 pt | `:17` |
| Head speed | 10 m/s | `:29` |
| Segment HP | **4** (overrides 7) | `:44` |
| Shoot interval | **1.5 s** (NSTimer, starts at creation, during the fade-in) | `:31,62,70-77` |
| Shoot interval in bullet time | `2.3 + (1 - timeScale)` = 2.95 s | `:114` |
| Bullet | aimed at player from head, startDelta 30 pt, tag `SHOOTING_COROSIA_BULLET_TAG` (2), fixture set to **sensor** | `:79-99` |
| killScore (declared, never awarded) | 500 | `:32` |

Behaviour:
- Movement and target choice are identical to Serpentus (`:126-177`).
- Shooting skips while paused (`:82-83`). Bullet rotation uses `acos(dir.y)` without the left/right sign (`:89-90`), so the bullet sprite is mirrored for leftward shots. **QUIRK (visual only).**
- **Death (different from Serpentus):** when only 2 segments remain and the head dies, the controller goes to `flag = kDying`, hides all segments (opacity 0, bodies inactive) and stops shooting (`:194-205`). The intent is to wait until all its bullets are gone (`[weapon isEmpty]`) and then become `kDealloc` (`:181-187`).
- `bigExploded`: when the player is within 700 pt of the head, all bullets are removed instantly and the chain is deleted (`:257-264`). No score.
- `releaseAgent` stops the timer and removes all bullets instantly (`:242-255`).

Quirks:
- **QUIRK / BUG (never despawns):** in `updateBodies` the `kDying -> kDealloc` check runs first. The loop then sees the still-present dead head (`bodies.size() == 2`) and **sets `flag = kDying` again** in the same call (`:181-205`). A killed ShootingSerpentus is never removed from `AgentsManager`. It stays an invisible zombie that keeps counting in `getEnemiesCount` and leaks.
  - It is still refreshed on bullet-time toggles, because `refreshAgentsManager` skips only `kDealloc` (`AgentsManager.mm:159`). `refreshEnemy` restarts the shooting timer (`:111-117`), so **after slow motion is toggled, a dead ShootingSerpentus starts firing again from where its head died.**
  - Recommended fix in the rewrite: despawn once the bullets are gone, or immediately.
- Bullet-time interval 2.95 s is hard-coded, not derived from the base rate.

Unity: ChainController + `AimedShooter` component (interval 1.5 s real time, or scaled time if the rewrite uses Unity timeScale for slow motion, in which case the manual refresh logic disappears).

Campaign levels 37-39. Also in the random pool (`GameMode.mm:42`).

### 3.4 `CaterPillar` (ID 10; `AI/CaterPillar.mm`) and `CaterPillarBody` (`AI/CaterPillarBody.mm`)
Purpose: a long, harmless-looking worm. Shoot the head and the whole worm pops.

**CaterPillar**

| Item | Value | Ref |
|---|---|---|
| Segments | `CATERPILAR_BODIES_COUNT 20` | `CaterPillar.mm:16` |
| Spawn spacing | `CATERPILLAR_BODY_DELTA 40.0f` | `:17` |
| Head speed | 10 m/s | `:31` |
| lerp | 0.87 / 0.95 | `:34,65` |
| killScore (declared, never awarded) | 300 | `:33` |

- Movement and target selection are the same as Serpentus (50% player snapshot) (`CaterPillar.mm:75-126`). No glow sprites are moved (`:122`).
- **Death:** if **any** segment it scans is `kDealloc`, which can only be the head, the **entire caterpillar** is flagged `kDealloc` (`:128-137`). There is no head promotion.

**CaterPillarBody**
- Built by `createNextCaterpillarBodyAtPosition:` (`CaterPillarBody.mm:15-38`): `type kEnemy`, sprite `Enemy3.png` with default colour (white), z 1, **no glow**.
- Collider: `width 0.95` gives r = 0.475 m = **15.2 pt** sensor (sprite is 64 pt wide).
- `hitCount 10` (`:25`). The body is **active immediately** (no `SetActive(false)`), so segments can hurt the player during the 0.1 s fade-in.
- `hit` and `bigExploded` only when `tag == 0` (`:40-59`).
- `releaseAgent` destroys the body directly. It would drop `vitaminsCount` vitamins, but that value is never set, so **0 vitamins** (`:61-75`).

Quirks:
- **QUIRK:** CaterPillarBody never sets `killScore`, so killing a caterpillar awards **0 points**. The declared 300 is dead.
- **QUIRK:** the head segment looks identical to the rest (no `Enemy1.png` swap), so the player can't see which end is vulnerable. The head is the end that leads the movement.
- **QUIRK:** CaterPillarBody never sets `agentStateType`. The ivar defaults to 0 = `kStateRandomMovement` (`ConfigConstants.h:213`), so each physics-loop update runs `EnemyAgent randomfly` with `speed 0` and resets the segment's velocity to 0. For the head this cancels the controller's velocity only *before* the controller sets it again later in the same frame, so it works by update order alone.
- Big explosion: controller-level bug (2.3). The segment overrides are unreachable.

Unity: ChainController with `killWholeChainOnHeadDeath = true` and smaller segment colliders. Give the head a distinct sprite if desired (design choice).

Not used in any campaign level. Only in the random pool of the other modes (`GameMode.mm:39`).

### 3.5 `LotusBacterius` (ID 15; `AI/LotusBacterius.mm`) and `LotusBacteriaBody` (`AI/LotusBacteriaBody.mm`)
Purpose: a snake that **blooms**. Killing its 10-HP head releases the remaining six petals as fast, spiralling, 1-HP homing enemies.

**LotusBacterius**

| Item | Value | Ref |
|---|---|---|
| Segments | `LOTUS_BODIES_COUNT 7` | `LotusBacterius.mm:15` |
| Spawn spacing | 40 pt | `:16` |
| Head speed (chain phase) | 10 m/s | `:27` |
| Head HP | **10** (segment 0 only) | `:48-53` |
| Petal HP | 1 | `LotusBacteriaBody.mm:19` |
| killScore (declared, never awarded) | 400 | `:30` |
| `revived` ivar | set to NO, never used (dead) | `LotusBacterius.h:14`, `.mm:29` |

State machine:
1. **Chain phase (`kStateRandomMovement`):** identical to Serpentus (50% player-snapshot targets, `:80-153`). Only the head is damageable.
2. **Head dies:** `updateBodies` sees the dead segment, sets the controller to **`kStatePatrol`**, releases the head (6 vitamins, 90 score already given by ContactListener) and erases it (`:155-167`). There is no head promotion.
3. **Bloom phase (`kStatePatrol`, `:100-121`):** every frame, for each remaining segment it sets `tag = 0` (**all petals become vulnerable**), sets `agentStateType = kStatePatrol` and calls its `update`. A dead petal is released and erased instead.
   - **QUIRK:** erasing inside the ascending loop skips the next petal for that frame.
   - When no segments remain, the controller is flagged `kDealloc` (`:102-106`).

**LotusBacteriaBody** (base `SpiralCorosia`)
- Overrides: `hitCount 1`, `speed 7.0f`, `vitaminsCount 3`, `enemyAgentType kLotusBacteriusBody` (16) (`LotusBacteriaBody.mm:17-21`).
- `creatingAnimationHasEnd` forces `kStateWaiting` (`:27-31`). `hit` and `bigExploded` only when `tag == 0` (`:57-71`).
- Inherited from SpiralCorosia:
  - Look: sprite `Enemy3.png` tinted cyan `(0,255,255)`, glow `Shine2.png`, r = 1 m = 32 pt sensor (`SpiralCorosia.mm:43-47,56`).
  - Patrol: velocity = (homing at `speed`) + (`radius*cos(spiralAlpha)`, `radius*sin(spiralAlpha)`) (`SpiralCorosia.mm:86-124`). Starting values: `radius` random 10-20 (m/s), `alphaDelta` random 1-3 degrees per update (`:40-41`). After each 360 degrees both are re-rolled: 10-20 / 1-3 normally, 15-25 / 2-4 in bullet time (`:118-123`).
- In the bloom phase each petal moves at 7 m/s homing plus a 10-20 m/s rotating component. That is a fast looping swarm.

Quirks:
- **QUIRK (spiral speed):** after the bloom, each petal is updated by the physics loop **and** by `LotusBacterius patrol`, so `spiralAlpha` advances twice per frame (2.2). At 60 fps that is 120-360 deg/s.
- **QUIRK:** 6 vitamins per petal (double drop), 42 per Lotus in total.
- Big explosion: controller bug (2.3). The petals never receive big explosions, even after blooming.

Unity: ChainController with an `OnHeadDeath -> Bloom` event. Detach the segments into independent `SpiralHomer` enemies (homing speed 7 m/s + rotating velocity offset 10-20 m/s, angular rate in deg/s instead of per update).

Campaign levels 33-36 and 43-44. Also in the random pool (`GameMode.mm:41`).

---

## 4. Non-chain enemies in this group

### 4.1 `Sunux` (ID 8; `AI/Sunux.mm`)
Purpose: a big, constantly spinning "sun" that **stays where it spawned** (on the arena edge) and sprays 8-way bullet bursts.

| Item | Value | Ref |
|---|---|---|
| HP | `hitCount 20` | `Sunux.mm:34` |
| Collider | `width 6*0.95` = 5.7, so r = 2.85 m = **91.2 pt**, sensor | `:39-40,66` |
| Visual | `Enemy3.png` (untinted) + `Shine3.png`, both scale **4.0** (256 pt) | `:43-47` |
| Spin | `CCRotateBy` 90 degrees per 0.3 s, forever (300 deg/s) | `:62` |
| Fade-in | 0.3 s | `:60` |
| Shoot interval | **2.0 s** (bullet time `2.3 + (1 - timeScale)` = 2.95 s) | `:35,90` |
| Burst | 8 bullets: dirs (-1,0) (-0.5,0.5) (0,1) (0.5,0.5) (1,0) (0.5,-0.5) (0,-1) (-0.5,-0.5), startDelta 100, rotation 0, tag 2 | `:94-141` |
| killScore | 150 | `:37` |
| Vitamins | 7, **always** (see quirk) | `:41,279-285` |
| Declared but unused | `speed 7`, `fleeSpeed 5`, `fleeDistance 70`, `shootDistance 100` | `:28-32` |

Behaviour:
- After the fade it activates, picks a roam point and starts the shooting timer (`:164-171`). It **never changes `agentStateType` from `kStateWaiting`** (`:25`), so `randomfly`, `attack` and `flee` (`:185-251`) never run. **Sunux is stationary.** Inferred from the code. Verify in play-test, but nothing else moves it.
- Diagonal directions have length 0.707. Bullet speed is normalised (1.4), so all 8 bullets travel at 30 m/s. The spawn offset is not normalised, though: diagonals spawn at 70.7 pt and cardinals at 100 pt. **QUIRK.**
- The bullets are not set to sensor (the other shooters do set it). They are physical bullet bodies.
- `hit`: at 0 HP it sets `kDealloc` immediately, hides itself and stops shooting (`:253-263`).
- `bigExploded` (player within 700 pt): -20 HP. At <= 0 it goes to **`kDying`**: invisible, not shooting, and removed once all its bullets are gone (`:173-183,265-277`).

Quirks:
- **QUIRK:** a `kDying` Sunux keeps an active body. Bullets pass through it (not destroyed) but still call `hit`, which can set `kDealloc` and award its score again (`ContactListener.mm:62-71`).
- **QUIRK:** `releaseAgent` drops 7 vitamins **unconditionally**, even when it is removed by level end or restart (`:279-285`). The other classes check `hitCount <= 0`.
- **QUIRK / BUG:** `releaseAgent` releases the weapon **without** `removeAllBulletsInstantly` (`:287-289`), and `Weapon` has no dealloc cleanup (`Weapon.mm`). Bullets in flight when Sunux dies are orphaned: their physics bodies keep flying and can still hit the player, while their sprites freeze in place and are never removed. Inferred from the code.

Unity: a static turret (no Rigidbody movement) with a `RadialBurstShooter` (8 directions, 2 s), a spin animation and a trigger collider of r 2.85 m.

Not used in campaign levels. Random pool of the other modes only (`GameMode.mm:31`).

### 4.2 `BigHunterus` (ID 5; `AI/BigHunterus.mm`)
Purpose: a big, slow, tanky homing blob. It always heads for the player.

| Item | Value | Ref |
|---|---|---|
| HP | 5 | `BigHunterus.mm:34` |
| Speed | 7 m/s (224 pt/s) | `:28` |
| Collider | `width 7.0` gives r = 3.5 m = **112 pt**, sensor | `:38-39,62` |
| Visual | `Enemy1.png` (untinted) + `Shine3.png`, both scale 4.0 | `:41-45` |
| killScore | 120 | `:35` |
| Vitamins | 5 (on death) | `:36,168-179` |

Behaviour:
- After a 0.1 s fade it enters `kStatePatrol` (`:82-87`) and homes on the player forever with `EnemyAgent patrol` (sprite rotated toward the player).
- The `attack`, `flee` and `randomfly` overrides (`:99-165`) are **unreachable**, because patrol never changes state. It has no weapon.
- It also picks a roam target at spawn, which is unused.

Unity: a simple `Homer` (speed 7 m/s, HP 5, scale 4).

Campaign levels 11-12, 17-19, 41-42, plus the random pool.

### 4.3 `ArmagedonBacteria` (ID 27 in reality; `AI/ArmagedonBacteria.mm`): DEAD
- It would be a small wanderer: speed 8, HP 1, r = 1 m, `Enemy3.png` + `Shine3.png`, spinning 300 deg/s, random roaming, 1 vitamin, killScore 50 (`ArmagedonBacteria.mm:27-34,39-40,84-85,105-110`).
- Its factory case is **commented out** (`AI/EnemyAgentsFactory.mm:94-95`), so the factory returns nil and `ArmagedonGameMode` pushes a nil agent (`Managers/GameModeManager/GameModes/ArmagedonGameMode.mm:30`).
- The "Armagedon" menu item is created but never added to the menu (`Layers/ChooseGameModeLayer.mm:79-81`).
- **QUIRK:** the enum comment says `//22`, but its real value is **27** (`ConfigConstants.h:255`).

Recommendation: drop it.

### 4.4 `AnakondaFeed`: DEAD
An empty `EnemyAgent` subclass (`AI/AnakondaFeed.h:11-13`, `AI/AnakondaFeed.mm:11-13`), referenced nowhere. Anakonda's growth uses `SerpentusBody` instead.

### 4.5 Base stubs in this area
- `ShooterBoss` (`AI/ShooterBoss.mm:11-13`, comment "erserved"): empty, and no class inherits from it.
- `LongBacteria`: see section 2.

---

## 5. Boss levels: shared rules

### 5.1 Which class is each boss ID
`EnemyAgentType` (`ConfigConstants.h:225-258`), mapped through `EnemyAgentsFactory getEnemyWithType` (`AI/EnemyAgentsFactory.mm:39-101`). The ID/level pairs are from `out/data/levels/levels_all.json`.

| Level (`boss`) | `enemies` | Enum | Class | Factory line |
|---|---|---|---|---|
| 10 (boss 1) | [22] | `kBigBoss` | `BigBoss` | `:84-85` |
| 20 (boss 2) | [23] | `kInferno` | `Inferno` | `:88-89` |
| 30 (boss 3) | [24] | `kBigRedNoise` | `BigRedNoise` | `:86-87` |
| 40 (boss 4) | [25] | `kAnakonda` | `Anakonda` | `:90-91` |
| 50 (boss 5) | [26] | `kFrunctus` | `Frunctus` | `:92-93` |

### 5.2 Boss-level flow (campaign)
1. **Detection:** `level.number % BOSS_LEVEL_CREATE_CONST(10) == 0` (`CompaignGameMode.mm:60-62`, `ConfigConstants.h:457`).
2. **Spawning:** the enemy-creation timer is created **non-repeating** on boss levels (`repeats:(!weHaveBossLevel)`, `CompaignGameMode.mm:75-78,92-95`). After `enemiescreatetime` seconds it fires once and spawns `totalEnemiesPerCreation` enemies of the level's type, via `getValidOuterSpawn`, on the arena edge. `enemiescreatetime` is 2 s in the data; Hard -0.5, Impossible -1.0 (`Managers/Level.mm:45-56`). On the very first level of a session, the creation timer starts only after an extra `enemiescreatetime + 2` s (`CompaignGameMode.mm:65-71`).
   - **QUIRK (level 20 may spawn two Infernos):** level 20 has `maximumenemies 2, enemiesgrowinterval 1`. `LevelManager update` adds `GAME_NEXT_SIMULATION_STEP_INTERVAL` (0.01) **per frame** and raises `totalEnemiesPerCreation` 1 -> 2 when that reaches 1, i.e. after 100 frames, about 1.67 s at 60 fps (`Managers/LevelManager.mm:148-174`).
   - At 60 fps on Normal (timer 2.0 s), on a first start, or at high frame rates, **two Infernos** spawn. On Hard (1.5 s) or Impossible (1.0 s) at 60 fps, only one does.
   - The other boss levels have `maximumenemies 1`, so they always get a single boss.
   - This is inferred from the timing and should be verified in play-test. The rewrite should state the intended count explicitly.
3. **No time limit:** the 1 s level-clock timer is also non-repeating on boss levels (`CompaignGameMode.mm:80-84,97-101`). It ticks once and stops, so the `time: 80` in the data never expires.
4. **No bonuses** spawn on boss levels (`CompaignGameMode.mm:120-129`).
5. **Special abilities** (slow motion, big explosion and so on) are disabled: `canUseSpecialAbilitie = NO` on boss levels (`Characters/MainCharacter.mm:407-412`), checked in `GameManager characterSpecialAbilitie` (`GameManager.mm:363`). Bullet time and big explosion, and with them every boss's `refreshEnemy`/`bigExploded` path, are therefore normally unreachable during boss fights.
   - **QUIRK:** `simulateCharacterRestartAtPosition` sets `canUseSpecialAbilitie = YES` (`MainCharacter.mm:878`). After a restart on a boss level these paths may become reachable. Inferred, not play-tested.
6. **HUD on boss levels:** score counter, level timer and the ability meter are hidden (opacity 0), and the label shows `_GET_READY_FOR_BOSS` (`Layers/LevelOverLayer.mm:160-164,400-408,588-596`).
7. **Win:** each boss calls `GameManager levelHasWonAfterBigBoss` when it dies (`GameManager.mm:219-238`). That function proceeds only if `AgentsManager getEnemiesCount() == 1` (the dying boss is still in the list) and `levelHasWon == NO`.
   - **Level 50** (`LAST_LEVEL 50`, `GameManager.mm:33`): sets `gameIsFinished`, shows `_FINISHED_GAME`, and after 5 s goes to the About scene (`GameManager.mm:242-252`).
   - **Otherwise:** `levelHasWon = YES` and the level-won UI is shown. After 5 s the next level loads.
   - Unlike normal levels (`gameLevelHasWon`, `GameManager.mm:204-217`), this path doesn't call `ScoreManager synchronizeLevelScore` or `gameWorldSpecialAction`. **QUIRK:** boss levels probably don't save a level score (inferred).
   - **QUIRK (double win trigger):** on level 50 `levelHasWon` is never set to YES. If several player bullets hit Frunctus in the same physics step, each `hit` calls `levelHasWonAfterBigBoss` again while the count is still 1. That schedules several 5 s "finish" timers, so the reset and scene replace run more than once. The same can happen with Inferno or BigBoss multi-hits on their killing step, but there the `levelHasWon` guard blocks repeats.
8. **Player death on a boss level:** agents are **not** cleared (`CompaignGameMode.mm:167-168`). The restart path then clears them and re-arms the one-shot spawn timer (`CompaignGameMode.mm:183-218`).

### 5.3 The "boss bar"
**There is no boss health bar driven by boss HP.**
- The HUD element built with the texture `boss_life_on.png`, under the comment `// init boss life` (`LevelOverLayer.mm:157-158,216-238`), is actually the **special-ability meter**. It is a `CCProgressTimer` bar at top centre, scale 0.4, starting at 100%. It drains by `SPECIAL_ABILITE_PROGRESS_STEP` (1%) per 0.1 s while an ability runs and refills at 2% per 0.1 s (`LevelOverLayer.mm:245-299`, driven from `MainCharacter.mm:340-380`).
- On boss levels this bar is **hidden** (`abilitieTimer.opacity = 0`, `LevelOverLayer.mm:405,593`).
- No code anywhere reads a boss's `hitCount` for the UI (searched for `boss`, `abilitieTimer` and `boss_life` across the source).
- Boss health is shown **only on the boss sprite itself**: BigBoss greys to black, Inferno and BigRedNoise fade toward opacity 70, Anakonda gets shorter.
- **Recommendation for the rewrite:** if a boss bar is wanted (the asset name suggests it was planned), drive it from `currentHP / maxHP`:
  - BigBoss: max 255.
  - Inferno: max 300 per Inferno, or the sum if two spawn.
  - BigRedNoise: max 400, plus an "invulnerable" state during spawn waves.
  - Anakonda: segments remaining, or summed segment HP (the maximum grows over time).
  - Frunctus: 1, so a bar is pointless unless its HP is fixed (see 6.5).

### 5.4 Boss difficulty table

| Boss | Normal | Hard | Impossible | Ref |
|---|---|---|---|---|
| BigBoss speed | 17 m/s | 20 | 23 | `BigBoss.mm:29-40` |
| Inferno tentacles | 10 | 20 | 25 | `Inferno.mm:53-63` |
| BigRedNoise speed | 12 | 15 | 18 | `BigRedNoise.mm:48-59` |
| Anakonda shoot interval / growth interval | 1.5 s / 3.0 s | 1.0 / 2.0 | 0.5 / 1.8 | `Anakonda.mm:36-38,70-81` |
| Frunctus shoot interval | 1.5 s | 0.7 | 0.3 | `Frunctus.mm:45,48-57` |
| Spawn delay (`enemiescreatetime`) | 2.0 s | 1.5 | 1.0 | `Level.mm:45-56` |

---

## 6. Bosses in detail

### 6.1 Boss 1: `BigBoss` (ID 22, level 10; `AI/BigBoss.mm`)
Purpose: a screen-filling, rapidly spinning white blob that roams the arena at high speed and sometimes charges where you stand. It has no projectiles; the danger is its size and speed.

| Item | Value | Ref |
|---|---|---|
| HP | **255** | `BigBoss.mm:46` |
| Speed | 17 / 20 / 23 m/s (544 / 640 / 736 pt/s) | `:29-40` |
| Collider | `width 13.55` gives r = 6.775 m = **216.8 pt**, sensor | `:48-49,73` |
| Visual | `Enemy3.png` + `Shine3.png`, both scale **10** (640 pt sprite, 1280 pt glow), glow z 0, sprite z 1 | `:52-53,63,66-67` |
| Fade-in | 0.1 s, body inactive until it finishes | `:62-71` |
| Spin | 90 degrees per 0.3 s forever (300 deg/s), starts after the fade | `:100` |
| Vitamins | 10 on death | `:50,120-132` |
| killScore | unset (0); score is hidden on boss levels anyway | - |
| Unused fields | `fleeSpeed 5`, `shootRate 0.5`, `shootDistance 100`, `fleeDistance 70` | `:41-44` |

Behaviour (single phase):
- **Appear:** spawns on the arena edge and fades in over 0.1 s. Then `kStateRandomMovement` (`:95-101`).
- **Move:** `EnemyAgent randomfly` toward `randomMovePoint` at full speed with no steering inertia. The target is re-picked within 10 pt (`:113-118`). Each pick has a **1/3 chance** of being the player's current position (snapshot) and a 2/3 chance of being a random roam point (`arc4random_uniform(3) == 1`, `:81-93`).
- **Attack:** contact only (any overlap with its 216.8 pt radius costs the player a life).
- **Damage feedback:** each hit sets `sprite.color = (hitCount, hitCount, hitCount)` (`:134-137`). The 255 HP maps exactly onto the colour byte, so the boss fades linearly from white to black.
- **Death / win:** `hit` reaches 0, which sets `kDealloc` and calls `levelHasWonAfterBigBoss` (`:138-139`). It drops 10 vitamins of value 1.

Quirks:
- **QUIRK:** updated twice per frame (2.2). Harmless here.
- **QUIRK:** the colour is computed from an int and cast to a byte. Extra hits on the killing frame make `hitCount` negative, which wraps (e.g. -1 gives 255, white) for one frame.
- **QUIRK:** at Impossible speed and low frame rates, the 10 pt arrival radius can be overshot (12.3 pt per frame at 60 fps), causing brief jitter around a target.
- **QUIRK:** `bigExploded` is not overridden. A big explosion would remove it without calling the win, which would soft-lock the level. It is unreachable while abilities are disabled.

Unity:
- Kinematic Rigidbody2D + trigger CircleCollider2D (r 6.775 m).
- `BossRoamer` state machine with a single Roam state (target picker: 1/3 player snapshot).
- Tint = HP/255 greyscale. Stats in a ScriptableObject per difficulty.

### 6.2 Boss 2: `Inferno` (ID 23, level 20; `AI/Inferno.mm`)
Purpose: a huge yellow spinning mass that roams and charges like BigBoss but more slowly at first. It accelerates as it is damaged and fills the arena with invulnerable wandering "Tentacles" that you have to dodge.

| Item | Value | Ref |
|---|---|---|
| HP | **300** | `Inferno.mm:70` |
| Base speed | 7 m/s (224 pt/s) | `:52` |
| Speed tiers | `speeds[] = {10, 16}`; thresholds `HIT_LIMIT_1 200`, `HIT_LIMIT_2 100`, `HIT_LIMIT_3 50` | `:17-19,22-26,171-174` |
| Collider | `width 8.95` gives r = 4.475 m = **143.2 pt**, sensor | `:72-73,97` |
| Visual | `Enemy3.png` tinted **yellow** (255,255,0) + `Shine3.png`, both scale 5 (320 pt) | `:76-77,87,98` |
| Fade-in | 0.5 s, body inactive until it finishes | `:86-95` |
| Spin | 300 deg/s after the fade | `:139` |
| Opacity damage feedback | step = 255/300 = 0.85 per hit, floor `MIN_OPACITY 70` | `:20,100,176-179` |
| Tentacles | `TENTACLE_COUNT 10` (+10 Hard, +15 Impossible), speed set to 5 m/s | `:16,53-63,111-119` |
| Vitamins | 10 on death | `:74,191-197` |
| Unused | `fleeSpeed 5`, `shootRate 0.5`, `shootDistance 100`, `fleeDistance 70`, global `wavesAreCreated[2]` | `:28-32,65-68` |

Phases (driven by HP on each hit, `:168-184`):

| HP after the hit | Speed |
|---|---|
| 300 ... 200 | 7 m/s (initial) |
| 199 ... 101 | **10 m/s** |
| 99 ... 51 | **16 m/s** |
| 50 ... 1 | stays 16 m/s (`HIT_LIMIT_3` is only a lower bound; there is no 4th tier) |

The comparisons are strict (`<` and `>`): a hit that leaves exactly 200 or exactly 100 HP doesn't change speed, and the next hit does.

Behaviour:
- **Appear:** at spawn (before its own fade-in) it creates all its tentacles at random arena-edge points (`createTentacles`, `:103,111-119`).
- **Move:** after the fade, `kStateRandomMovement`. The target has a **50% chance of being the player snapshot** (`arc4random_uniform(2) == 1`, `:121-132`) and is re-picked within 10 pt (`:161-166`).
- **Tentacles (`AI/Tentacle.mm`, documented elsewhere):** each frame Inferno calls `update` on every tentacle (`:142-154`). They are not in AgentsManager.
  - Each tentacle is r = 1 m (32 pt) sensor, `Enemy3.png` (untinted) + `Shine3.png`, spinning 300 deg/s. It roams between random points at 5 m/s after its 0.1 s fade (`Tentacle.mm:27,35-58,77-100`; speed overridden at `Inferno.mm:116`).
  - **`hit` and `bigExploded` are empty** (`Tentacle.mm:102-110`), so tentacles are **invulnerable**. They still **absorb player bullets** and **damage the player on contact**.
  - They disappear only when Inferno is released (`Inferno.mm:199-204`).
- **Attack:** contact with the body or any tentacle. No projectiles.
- **Death / win:** at HP <= 0, `levelHasWonAfterBigBoss` (`:181-182`) and 10 vitamins. If two Infernos spawned (see 5.2), the win fires only when the second dies, because the first death sees `getEnemiesCount() == 2`.

Quirks:
- **QUIRK (opacity rounds to 1 per hit):** cocos2d opacity is a byte, so `254.15` becomes 254, and so on. The fade is about -1 per hit and reaches the floor of 70 after ~185 hits, not linearly over 300 (inferred from GLubyte truncation).
- **QUIRK:** tentacles get up to 3 updates per frame (Inferno is updated twice, plus the physics loop). Harmless because they only set a velocity.
- **QUIRK:** the private category is mistakenly declared on `Tentacle` instead of `Inferno` (`:34-37`). Harmless.
- **QUIRK:** `bigExploded` is not overridden, which has the same soft-lock risk as BigBoss.

Unity:
- `BossRoamer` with a `SpeedByHpPhase` table: {300: 7, <200: 10, <100: 16}.
- On spawn it instantiates N `InvulnerableWanderer` minions parented to the boss lifetime. Opacity = lerp(1, 70/255, damage).

### 6.3 Boss 3: `BigRedNoise` (ID 24, level 30; `AI/BigRedNoise.mm`). Cross-reference: not one of my assigned classes
Purpose: a giant red blob that chases the player. At 3 HP thresholds it stops, spins wildly, becomes invulnerable and births a stream of small red chasers (`RedNoise`). Afterwards it is slowed until all its children are dead.

| Item | Value | Ref |
|---|---|---|
| HP | `MAXIMUM_HIT_COUNT 400` | `BigRedNoise.mm:22,64` |
| Speed | 12 / 15 / 18 m/s | `:48-59` |
| Collider | `width 10.3` gives r = 5.15 m = **164.8 pt**, sensor; body **active from spawn** (no `SetActive(false)`) | `:66-67,79,83` |
| Visual | `Enemy1.png` tinted red, scale 7 (448 pt), z 4, **no glow** | `:71-74,81,85` |
| Fade-in | **1.0 s**, then `kStatePatrol` (homes on the player) | `:87-89,99-102` |
| Wave triggers | HP < `FIRST_CHILD_BORN_LIMIT 300`, < `SECOND 200`, < `THIRD 100` | `:16-18,262-276` |
| Wave sizes | `enemiesWaves[] = {60, 80, 90}` | `:25-28` |
| Child spawn rate | 1 per **0.1 s** (0.4 s in bullet time) | `:110-115,193` |
| Spin while spawning | 90 degrees per 0.1 s (900 deg/s) | `:109` |
| Opacity feedback | step 255/400, floor `FINAL_OPACITY_LIMIT 70` | `:21,91,246-250` |
| Vitamins | 10 on death; children drop none (`bornFromBoss`) | `:69,216-222`; `RedNoise.mm:73` |
| Unused | `WAVE_ENEMIES_COUNT 150`, `MAXIMUM_SPRITE_OPACITY 255` | `:20,23` |

State machine:
1. **Chase (Patrol):** homes on the player at `speed`.
2. **Wave start:** triggered by the first hit that brings HP inside a new band (`firstWave`, `secondWave` and `thirdWave` flags, `:262-276`). It enters `kStateWaiting` with `bornOfChilds = YES` (`:104-117`).
   - Every update its velocity is forced to 0 (`:152-156`).
   - **`hit` is ignored while spawning** (`:242`), so it is invulnerable. Player bullets are still destroyed on contact.
   - Every 0.1 s it spawns a `RedNoise` at its centre. Each child has `speed 10`, 1 HP, homes on the player, is r = 1 m and **non-sensor** (`SetSensor(false)`), `Enemy1.png` red, `killScore 80` (`RedNoise.mm:23-46`; `BigRedNoise.mm:126-129`).
3. **Wave end:** when the **number of currently alive children equals** `enemiesWaves[wave]` (`:130-147`):
   - the *last* child spawned gets speed x1.1;
   - the boss stores `prevSpeed = speed` and sets `speed /= 3`;
   - spawning stops, the spin stops and it returns to Patrol.
4. **Recovery:** while any children are alive, the boss moves at 1/3 speed. When `childrens.size() == 0` the speed returns to `prevSpeed` (`:158-165`).
5. **Death / win:** HP <= 0 calls `levelHasWonAfterBigBoss` and returns before any wave check (`:253-259`). Releasing the boss also releases all its children (`:232-237`).

Children are updated by the boss (2x per frame) plus the physics loop. They are not in AgentsManager, so the win count stays correct.

Quirks:
- **QUIRK (wave sizes are alive-count thresholds):** wave 2 ends when 80 children are alive *in total*, including survivors of wave 1. If the player kills children as fast as they spawn (>= 10/s), the count never reaches the target, so **the boss stays invulnerable and keeps spawning indefinitely**. The rewrite should count spawned-this-wave instead.
- **QUIRK:** the body is active during the 1 s fade-in, so it can be damaged and can hurt the player before it is fully visible.
- **QUIRK:** only one child (the last) gets the x1.1 speed bonus. That looks unintended.

Unity:
- `BossChaser` with a `SpawnWave` state: invulnerable, zero velocity, spawn interval 0.1 s, end after N spawns.
- HP thresholds at 300/200/100. Post-wave speed multiplier 1/3 until the minions are cleared.

### 6.4 Boss 4: `Anakonda` (ID 25, level 40; `AI/Anakonda.mm`)
Purpose: a giant, very fast snake made of 24 large segments. It roams and charges, shoots aimed bullets from its head, and **keeps growing**: new segments spawn at the arena edge and streak across the arena to join its tail. You win by shooting through head after head until only two segments are left.

| Item | Value | Ref |
|---|---|---|
| Segments at start | `SERPENTUS_BODIES_COUNT 24` (SerpentusBody) | `Anakonda.mm:16,40-64` |
| Spawn spacing | 40 pt (920 pt-long line) | `:17` |
| Head speed | **17 m/s** (544 pt/s) | `:32` |
| Original segment HP | **14** | `:46` |
| Added segment HP | **5** | `:267` |
| Segment visual | SerpentusBody (blue `Enemy3.png`, `Shine2.png` glow), sprite scale **2.0** (glow not scaled). Collider stays r = 32 pt. Head is `Enemy1.png` white. | `:49,58-62,270` |
| Growth | 1 new segment every 3.0 / 2.0 / 1.8 s (NSTimer, repeating) | `:38,70-87,258-273` |
| Shooting | aimed at player from head every 1.5 / 1.0 / 0.5 s, startDelta 30, tag 2, sensor | `:36,70-81,99-128` |
| lerp | 0.87 / 0.95 | `:35,146` |
| killScore | unset (0); segments give 80 each, hidden on boss levels | - |

Behaviour:
- **Appear:** chain construction from section 2.1, fade 0.1 s, then `kStateRandomMovement` (`:169-175`). Shooting and growth timers start at creation (`:83-89`).
- **Move:** like Serpentus at 17 m/s. Target is 50% the player snapshot (`:156-167`), re-picked within 10 pt (`:207-208`).
- **Shoot:** one aimed bullet at the player per interval (skipped while paused) (`:108-128`). The bullet sprite has the same left/right mirroring quirk as ShootingSerpentus.
- **Growth / "feeding":** `addBody` creates a SerpentusBody at a **random arena-edge point** (`getValidOuterSpawn`). It sets HP 5, `tag = bodies.size() + 1` (non-zero, so invulnerable), scale 2, and appends it to the tail (`:258-273`).
  - Its own 0.1 s fade makes it visible and activates its body (inherited from Loctopus).
  - From the next frame the tail lerp pulls it toward the current tail, covering 13% of the remaining distance per frame. It crosses the arena in roughly 0.3-0.5 s at 60 fps (0.87^30 = 0.015).
  - **While flying in it is a live contact-damage hazard**, sweeping across the arena from the edge toward the snake.
- **Damage:** only the head (tag 0) takes damage. Each head death promotes the next segment (section 2.4), and the original segments take 14 hits each.
- **Death / win:** when the head dies with exactly 2 segments left, flag `kDealloc` and `levelHasWonAfterBigBoss` (`:216-222`).
  - On Normal, 23 x 14 = **322 hits** clears the original body, plus 5 hits for every segment added meanwhile (one per 3 s). The player must out-damage the growth rate: on Normal that means about 1.7 hits/s just to keep pace with growth.
- **Release:** stops both timers, removes all bullets instantly, releases the segments (each dead one drops 3 vitamins). It also drops one vitamin of value 10 at `sprite.position` (`:275-305`).

Quirks:
- **QUIRK / BUG (Normal fire-rate after bullet time):** `prevShootRate` is assigned only in the Hard and Impossible cases (`:70-81`), so on Normal it stays 0. `refreshEnemy` restores `shootRate = prevShootRate` when bullet time ends (`:141-147`), which re-creates the shooting `NSTimer` with **interval 0**. NSTimer clamps that to 0.1 ms, so the boss fires about once per run-loop pass, i.e. **a bullet every frame**.
  - Entering bullet time also *accumulates*: `shootRate + 1 + (1 - 0.35)` on every toggle.
  - Normally unreachable on boss levels (5.2), but reachable if Anakonda appears elsewhere or abilities get re-enabled.
- **QUIRK:** the release vitamin uses the controller's nil `sprite`, so a **value-10 vitamin spawns at world origin (0,0)**. `hitCount` is always 0, so this happens on every release, including level restart.
- **QUIRK:** `bigExploded` is not overridden. It uses the origin-distance bug (2.3), and if it fires it removes the boss **without** the win call, which would soft-lock. Unreachable while abilities are disabled.
- **QUIRK:** the growth timer is not adjusted for bullet time or pause, apart from skipping while paused.

Unity:
- `ChainController` (24 segments, head speed 17 m/s, segment scale 2) + `AimedShooter` + `ChainGrower` (spawn a segment at an arena-edge point every N s, then attach it at the tail and let the follow logic pull it in).
- Make the fly-in speed explicit (time-based) so it doesn't depend on frame rate.

### 6.5 Boss 5 (final): `Frunctus` (ID 26, level 50; `AI/Frunctus.mm`)
Purpose: the final boss. A blue blob (2x scale) that spirals toward the player, sidesteps when the player aims at it while shooting, dashes away whenever it nears the arena edge, and fires aimed bullets. **It dies to a single hit.**

| Item | Value | Ref |
|---|---|---|
| HP | **`hitCount = 1.0f;//150`**, i.e. 1. The comment shows 150 was intended. | `Frunctus.mm:44` |
| Spawn position | ignores the factory position. Uses `getValidSpawnPosFromCharacter` (500-600 pt from the player) | `:33` |
| Normal speed | `NORMAL_SPEED 6` m/s (192 pt/s) | `:19,40` |
| Escape dash speed | `ULTRA_SPEED 80` m/s (**2560 pt/s**) | `:18,210` |
| Edge margin / arrival radius | `LEAVE_EVADE_DISTANCE 40` pt | `:17` |
| Spiral | velocity offset (`radius*cos(a)`, `radius*sin(a)`). Initial `radius` 10-20 m/s, `alphaDelta` 1-3 deg/update. After 360 degrees: 12-20 / 2-5 (bullet time 15-25 / 4-6). | `:59-60,264-277` |
| Dodge trigger cone | +/- 1.5 x `EVADE_DELTA_ALPHA` (30 deg, `AI/Gnida.h:11`) = **+/-45 deg** | `:245-254` |
| Shoot interval | 1.5 / 0.7 / 0.3 s, aimed, startDelta **50**, tag `FRUNCTUS_BULLET_TAG` (3), sensor | `:45,48-57,105-125` |
| Collider | `width 2.3` gives r = 1.15 m = **36.8 pt**, sensor | `:62-63,76` |
| Visual | `Enemy3.png` tinted blue (0,0,255), `Shine2.png`, both scale 2.0 (128 pt). Turns **white** while dodging. | `:66-70,78,251,256` |
| Fade-in | 0.1 s (body inactive until it finishes) | `:79,84-86` |
| Damage feedback | opacity -= 255/hitCount per hit, floor `MIN_OPACITY 70` | `:20,90,290-293` |
| Vitamins | `vitaminsCount 10` | `:46,305-314` |
| killScore | unset (0) | - |

State machine (updated twice per frame, 2.2):
- **Patrol** (`:229-262`):
  - Base velocity = normalise(player - pos) x `speed` (6). Sprite rotated toward the player.
  - **Dodge:** if the boss's facing angle is within +/-45 degrees of directly opposite the player's aim (i.e. the player is aiming roughly at it) **and** the player is shooting (`AiInput characterIsshooting`), the sprite turns white and `fastEvade` adds a perpendicular component of magnitude `speed`: 6, or 9 in bullet time. The side (left or right) is fixed per boss by `perpendicularRandomer` (`:64`), and the y of the perpendicular is flipped (`:279-285`). Otherwise the colour resets to blue.
  - The velocity is then applied, and `moveOnCircle` **replaces** it with base + spiral offset. The spiral term (10-20 m/s) dominates the homing term (6 m/s), so it loops wildly while drifting toward the player.
- **Edge check** (every update while not evading, `:222-227,154-183`): if the sprite is within 40 pt of any arena bound, pick `escapePoint = getValidSpawnPosFromCharacter` (500-600 pt from the player, inside the arena) and switch to **Evade**.
- **Evade** (`:185-220`): dash at 80 m/s straight to `escapePoint`. Within 40 pt it sets `speed = 6` and returns to Patrol.
- **Shooting:** an NSTimer started at creation fires an aimed bullet at the player every interval (skipped while paused). Its rotation is the sprite rotation.
- **Death / win:** any player bullet calls `hit`, which drops HP to 0 and sets `kDealloc`. `hit` then calls `levelHasWonAfterBigBoss` (`:287-298`). It is level 50, so this triggers the **game-finished** flow (5.2 step 7). Releasing it drops 10 vitamins, stops the timer and removes all bullets (`:305-327`).

Quirks:
- **QUIRK (critical design issue):** `hitCount = 1` makes the final boss die to one bullet. The commented-out `150` was the intended value. Decide the real HP for the rewrite.
- **QUIRK:** `createAtPosition` never calls `[super init]` or `[self init]` (`:30-93`), so `Figure init` (which sets `physics` and the default flag) is skipped. It works by accident because the fields it needs are set by hand.
- **QUIRK:** in Evade the local `speed` is set to the bullet-time-scaled value, but the velocity uses the constant `ULTRA_SPEED` (`:188-190` vs `:210`). The dash is not slowed by bullet time.
- **QUIRK:** at 60 fps the dash moves ~42.7 pt per frame, more than the 40 pt arrival radius, so it can overshoot and ping-pong for a few frames. It gets worse at lower frame rates.
- **QUIRK:** same Normal-difficulty `prevShootRate = 0` bug as Anakonda (`:48-57,137-143`). After bullet time the boss fires every frame.
- **QUIRK:** `lerpParameter` is set in `refreshEnemy` (`:142`) but unused. `#import "Gnida.h"` is used only for `EVADE_DELTA_ALPHA`.
- **QUIRK:** the spiral angle advances twice per frame because of the double update (240-600 deg/s at 60 fps on the initial values).
- **QUIRK:** multi-bullet killing hits can trigger the finish sequence more than once (5.2).

Unity:
- `FinalBoss` state machine {Patrol (homing + rotating offset + dodge-on-aim), Evade (dash to a point)}.
- Edge detection against the arena bounds. `AimedShooter`.
- Express spiral rates in deg/s and dash arrival with a sweep/overshoot check. Stats per difficulty in a ScriptableObject.

---

## 7. Assets used by this group
- **Sprite frames** (`characters_resources` atlas): `Enemy1.png` (chain heads, BigHunterus, BigRedNoise), `Enemy3.png` (segments, Sunux, Inferno, BigBoss, Frunctus, CaterPillar, Tentacles), `Shine2.png` glow (SerpentusBody, LotusBacteriaBody, Frunctus), `Shine3.png` glow (Sunux, BigHunterus, Inferno, BigBoss, Tentacle), `bullet.png` + `Shine1.png` (enemy bullets).
- **Tints in code:**
  - blue (0,0,255): SerpentusBody, Frunctus;
  - cyan (0,255,255): LotusBacteriaBody;
  - yellow (255,255,0): Inferno;
  - red (255,0,0): BigRedNoise, RedNoise;
  - greyscale by HP: BigBoss.
- **HUD:** `boss_life_on.png` is the special-ability meter, not a boss bar (5.3).
- **Sounds:** none. No class in this group calls `SoundManager`.
- **Fonts:** none.
- **Z-order:** all sprites go into `GameResourceManager sharedMainCharacterSpriteSheet` (a batch node): glow z 0, body z 1. Exceptions: BigRedNoise z 4, bullets z 5 (glow z 6).

## 8. Dependencies (whole group)
- `AgentsManager` (lifetime, big explosion, refresh), `EnemyAgentsFactory`, `ContactListener` (damage and score).
- `GameManager`: `bulletinTimeIsOn`, `gameIsPaused`, `levelHasWonAfterBigBoss`, `updateLScoreWithValue`.
- `AiInput`: `mainCharacterPosition`, `mainCharacterRotation`, `characterIsshooting`.
- `SpawnManager`: `getValidOuterSpawn`, `getRandomPointAroundPoint`, `getValidSpawnPosFromCharacter`.
- `CoreSettings` arena bounds.
- `VitaminManager`: `createVitaminAtPosition:withScore:`, `getValidBonusSpawnPointAtPoint:`.
- `GameSettingsManager.gameDifficulty`, `Weapon` / `Bullet`, `GameResourceManager` (batch node), `CCDirector` scheduler timeScale (`Utils timeScale`).
- No notifications are posted or observed, and no NSUserDefaults are read by these classes.

## 9. Top issues for the rewrite (summary)
1. **Final boss Frunctus has 1 HP** (`Frunctus.mm:44`, intended 150 per the comment). Pick a real value.
2. **No boss HP bar exists.** `boss_life_on.png` is the special-ability meter and is hidden on boss levels. Build a real bar from current/max HP if wanted (5.3).
3. **Chain enemies use per-frame lerp following, not joints.** Spacing and fly-in speed are frame-rate dependent (about 6.7 x head displacement per frame). Only the head is damageable, body segments absorb bullets, and score is paid per segment (CaterPillar pays 0).
4. **Several kill or despawn paths are broken:**
   - ShootingSerpentus never despawns, and resumes shooting after a slow-motion toggle.
   - Big explosion on chain enemies tests distance to world origin and deletes the whole chain.
   - Sunux orphans its live bullets.
   - On Normal, Anakonda and Frunctus fire every frame after bullet time ends (`prevShootRate` = 0).
5. **Boss-level rules are implicit:**
   - one-shot spawn timer;
   - level 20 may spawn 2 Infernos depending on frame rate and difficulty;
   - no time limit;
   - no bonuses or abilities;
   - win = `getEnemiesCount() == 1` at boss death.
   - BigRedNoise can become permanently invulnerable if its children are killed as fast as they spawn.
