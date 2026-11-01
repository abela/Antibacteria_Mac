# 01 - Player, Weapons, Bonuses/Vitamins, Input, Core Physics/Camera

Design reference for the Unity 6 rewrite of AntiBacteria (2013, cocos2d-mac 2.0 + Box2D 2.2).
All paths are relative to `Mac/Bacteria/` (= `/home/claude/legacy`). Coordinates are cocos2d points
(origin bottom-left; logical screen 1024 pt wide). "m" = Box2D metres.

**Unit conversion: 1 m = 32 pt.** `ASPECT_RATIO 32.0f` (ConfigConstants.h:353) is the ratio used in
every gameplay conversion. `PTM_RATIO 32` (GameCore/Physics/Abstracted/Physics.h:18) has the same value
but is only passed to the debug drawer. Every Box2D speed below is in m/s, so multiply by 32 to get pt/s.

## Overview

| Class | Role | One-line summary |
|---|---|---|
| `Character` | Abstract base | Empty base class for characters (subclass of `Figure`). Every method throws "Must override". |
| `CharacterController` | Abstract base | Base controller interface. Every method throws. |
| `MainCharacter` | **Player** | The player cell: movement, aiming, 5 shot types, lives, hit/invulnerability, bonuses, Slow-Motion ability. |
| `MainCharacterController` | Player facade | Singleton wrapper that creates the player at screen centre and forwards calls to it. |
| `CharacterPropertiesManager` | Persistent upgrades | Stores the chosen default shot type and the Double Speed and Immortality toggles in encrypted NSUserDefaults. |
| `EnemyDirection`, `EnemyDirectionManager` | Dead code | Off-screen enemy arrow indicator that was never finished or used. |
| `Weapon` | Bullet pool | Spawns `Bullet`s and updates or removes them. Has one stat: `shootSpeed` = 0.1 s. |
| `WeaponFactory` | Dead code | Returns nil. |
| `WeaponManager` | Player gun singleton | Holds a single `Weapon` and forwards shoot/update/restart calls to it. |
| `Bullet` | Projectile | Box2D "bullet" body with a fixed velocity. Player variant: 40 m/s. Enemy variant: 30 m/s. |
| `Vitamin` | Currency pickup | Dropped by dead enemies. Homes in on the player inside 500 pt. |
| `VitaminManager` | Currency bookkeeping | Spawns vitamins and adds to the persistent vitamin count **when the vitamin spawns** (not when it is picked up). |
| `BonusManager` | Bonus spawner | Spawns usable bonuses, flags, peace flags and rush stations, and updates or removes them. |
| `BonusObject` | Bonus base | Base class for pickups (an `Agent` with a `bonusObjectType`). |
| `UsableBonus` | Power-up | Drifting, fading power-up orb. In Campaign mode its type cycles through all 8 types. |
| `InputManager` | Input | Turns key and mouse state into movement, aim, fire, ability and pause. Contains stub code for HID joysticks. |
| `KeyBind` | Value object | A key code wrapper. There is **no** rebinding. |
| `CCBlade` | Dead code | Third-party swipe-trail renderer. Imported but never instantiated. |
| `Utils` | Math helpers | Random ints, angle↔vector convention, unused intersection maths. |
| `Camera` | Camera | Smoothed follow of the player via the cocos2d `CCCamera` eye/center, plus a constant zoom-out. |
| `CoreSettings` | Globals | Arena corner points, UI scale factor, main window. |
| `Figure` | Physics object base | Sprite + b2Body + `type` + lifecycle `flag`, and the shared Box2D world singleton. |
| `Bounder` | Arena walls | 4 static box bodies around the arena. |
| `Physics` | World | b2World with zero gravity, continuous physics and the listeners. |
| `ContactListener` | Collision → gameplay | All collision rules: bullet/enemy/player/bonus/vitamin/rush station. |
| `DestructionListener` | Empty | Has no behaviour. |
| `CCSpriteBatchNodeBlur` | Rendering | Sprite batch with a 9-tap blur shader, used to blur the player, bullets and vitamins during slow motion. |
| `GameWorld` | Level shell | Background sprite, arena bounds and walls. Everything else is delegated to `GameModeManager`. |
| `ConfigConstants` | Constants | Enums and defines (see the last section). |
| `GLES-Render` | Debug | Box2D debug drawer. It is set up but `DrawDebugData` is never called, so it never draws. |

---

## 1. Core loop and time base (needed to read every other section)

- `MainGameLayer` schedules `tick:` every frame. That calls `GameManager.manageLoop:dt` → `managePhysics:`.
  - `world->Step(dt, 8, 2)` uses a **variable** timestep (velocity iterations 8, position iterations 2) (Managers/GameManager.mm:420-427).
  - It then iterates the bodies. For the player it updates the camera, and it updates `kEnemy` agents (GameManager.mm:429-476).
  - `mainCharacterController update` → `gameWorld update` → `InputManager inputEventListener` (GameManager.mm:479-484).
- Slow motion ("bulletin time") sets the cocos2d scheduler `timeScale` to `BULLETIN_TIME_DELTA 0.35f` (GameManager.mm:32, 336).
  The scaled `dt` slows the physics, CCActions and per-frame counters that multiply by `[Utils timeScale]` (Utils.mm:88-91).
- **Gameplay also runs on `NSTimer`s, which use real time and ignore `timeScale`:** the fire rate, Slow-Motion duration and refill, the auto-fire start delay, the bullet-free death → game-over delay, and the campaign 1 s tick.
- `GAME_NEXT_SIMULATION_STEP_INTERVAL 0.01f` (ConfigConstants.h:25) is added **once per frame** by several counters, which makes them frame-based (see the quirks).
- **QUIRK (frame rate):** the original effectively ran at the cocos2d-mac default of 60 fps (inferred: there is no `setAnimationInterval` call in the code). All "per frame" numbers below assume 60 fps.

---

## 2. Player - `MainCharacter` (Characters/MainCharacter.mm)

### Purpose
The player is a single cell/ship in a big arena. It moves freely in 2D, always faces its aim direction and
fires continuously while the fire button is held (or always, with Auto Shoot). It has 3 lives and blinks
while invulnerable after a hit. It can pick up timed power-ups and has one rechargeable special ability
(Slow Motion).

### Creation (`createAtPosition:withGameLayer:`, :63-115)
- Spawn position: screen centre `winSize/2` (MainCharacterController.mm:25-31). The arena is centred on the same point (GameWorld.mm:65-71).
- `width = height = 1.3` m (:72-73) gives a physics circle of radius **0.65 m = 20.8 pt** (:138-139). The sprite `character.png` is 64x64 pt (characters_resources sheet).
- Initial state: `life = 3` (:80), `shootCount = 1` (:75), `characterDirection = (1,0)` (:74), `currentSpecialAbilitie = kSlowMotion` (:70, the only value it ever gets), `characterShootType = kNormalShoot`, which is then overwritten from `CharacterPropertiesManager` (:110-112).
- If Auto Shoot is enabled, a one-shot NSTimer starts shooting after `AUTOFIRE_START_SHOOT_TIME 2.0f` s (:35, :102-108).
- `updateCharacterSettingsAfterLevel` is applied (see Progression).

### Physics body (`createNewBodyAt:`, :124-146)
`b2_dynamicBody`, `fixedRotation=false`, `allowSleep=false`, one circle fixture, `density 1.0`, friction and
restitution left at the Box2D defaults (0.2 / 0), **not a sensor**, no collision filter.
That gives a mass of about π·0.65² ≈ **1.327 kg** (derived).

### Movement
Each frame `update` calls `moveCharacterWithVelocity:velocityForMovement` (:814). That method:
```
velocity = velocity * timeScale;
body->ApplyLinearImpulse(velocity, center);
body->SetLinearDamping(6.0f);          // :162-165
```
The movement is "impulse every frame + linear damping 6". `velocityForMovement` is computed by input:

| Mode | Impulse per frame (N·s) | Source |
|---|---|---|
| Keyboard, one axis (e.g. only W) | `STANDART_CHARACTER_VELOCITY 1.7` × `STRAIGHT_MOVEMENT_VELOCITY_SCALE 1.3` = **2.21** | :24, :26, :187-190 |
| Keyboard, diagonal | 1.7 on each axis (magnitude 2.40) | :177-184 |
| Mouse-only | `MOUSE_MOVE_CHARACTER_VELOCITY 2.1` toward the cursor (normalised) | :25, :256-257 |
| × Fast Movement bonus **or** Double Speed upgrade | ×2. The two do not stack (`if(isFastRun && !isDoubleSpeed)` / `if(isDoubleSpeed)`) | :226-229, :259-263 |

Resulting top speeds, derived assuming Box2D 2.2 damping `v *= 1 - h·c` at 60 fps (terminal v = 9·I/m):

| Case | m/s | pt/s |
|---|---|---|
| Keyboard straight | ≈15.0 | ≈480 |
| Keyboard diagonal | ≈16.3 | ≈522 |
| Mouse-only | ≈14.2 | ≈456 |
| ×2 (bonus/upgrade) | ≈30 | ≈960 |

The player reaches about 90 % of top speed in roughly 22 frames (≈0.37 s) and stops just as fast.

- **QUIRK (frame rate):** the impulse is applied per frame and damping is per step, so the top speed is about proportional to the frame rate. At 120 fps the player would move about twice as fast. In Unity, drive the velocity directly (or use FixedUpdate at a fixed 60 Hz).
- **QUIRK:** diagonal keyboard movement is about 9 % faster than straight movement. The 1.3 "straight" boost was meant to compensate, but it over/under-shoots.
- **QUIRK:** mouse-only mode **never stops**: the player always accelerates toward the cursor. If the cursor is exactly on the player, `ccpNormalize(0,0)` gives NaN (potential NaN impulse; inferred).
- `currentSpeed = velocity.x * velocity.y` (:163) is meaningless and unused.
- Every frame the sprite position is copied from the body, and `AiInput.mainCharacterPosition` is published for the enemies (:166-168).

### Aiming / rotation
- **Angle convention (important for Unity):** rotation is in degrees, **0° = up (+Y), clockwise positive**. The direction vector is `(sin θ, cos θ)` (Utils.mm:83-86). Unity angle = 90° − cocos rotation.
- Mouse aim (`rotateCharacterWithMousePos:`, :232-246): direction = normalise(cursorWorld − playerPos). `rotation = ±acos(dir.y)`, with the sign taken from the x side. The cursor world position is `screenMouse + camera.cameraPosition` (InputManager.mm:245-254).
- Keyboard aim (only in "Only Arrows" mode, :204-225): the 8 arrow combinations map to 0/45/90/135/180/-135/-90/-45°, and `characterDirection = keyBoardShootDirection` (which is (0,0) when no arrow is held).
- Each frame the sprite rotation is set to `characterRotation` (:812). The physics body does not rotate the sprite.
- `AiInput` receives the rotation and the `shooting` flag every frame (:811, :815). Some enemies (Gnida, Frunctus, ZamriGnida) use them to dodge.

### Firing (`startShoot` :271-307, `shooting:` :309-336, `endShoot` :799-807)
- `startShoot` does nothing if `flag == kDealloc` or `canShoot == NO`. Peace Walker mode sets `canShoot = NO`.
- It schedules a **repeating real-time NSTimer** with an interval that depends on the shot type and on slow motion (see the Weapons table).
- Each tick: skip if the game is paused or `life == 0`. Skip if `characterDirection == (0,0)`. Otherwise fire the pattern for the current `characterShootType`.
- `MainCharacterController.startShoot` guards against double timers with the `shooting` flag (MainCharacterController.mm:90-98). Other callers (auto-fire timer, `refresh`, restart) call `MainCharacter.startShoot` directly.
- **QUIRK:** a duplicate timer is possible, e.g. if the 2 s auto-fire timer fires after a restart has already started shooting. That would double the fire rate. Inferred, not observed.
- `resume` (after pause): when Auto Shoot is off, shooting stops and the player has to press fire again (:604-608).

### Lives, damage, death (`hit` :705-724)
- Lives: 3, maximum 3. They are reset to 3 at every level start (:413) and on restart (:875). **Lives do not carry over between levels.**
- `hit` is ignored while `characterIsHitting` (blinking), `isInvisible` (Invisible bonus) or `immortal` (Immortality upgrade) is set.
- Otherwise: `life--`, then blink `CCBlink 2.0 s, 10 blinks`. When the blink ends, `characterIsHitting` is cleared. This gives 2 s of invulnerability (:710-712, 726-730).
- At `life == 0`: special ability disabled, actions stopped, `flag = kDealloc`, opacity 0, and every player bullet is flagged for removal (`weaponManager gameOver`) (:713-720). `GameManager` notices `kDealloc` on the player body and, after an NSTimer of 0.1 s, shows game over (GameManager.mm:438-448).
- The HUD heart count is updated through `GameManager updateHeartCountOnLevelOverLayer:` (:722, :465).
- Sources of `hit` (ContactListener): touching any non-dying `kEnemy`, and touching **any** `kBullet` (see Physics).
- **QUIRK:** enemies are sensors and damage only on `BeginContact`. If the player is still overlapping an enemy when the 2 s blink ends, it takes no more damage until it leaves and re-enters.
- **QUIRK:** picking up any bonus calls `[sprite stopAllActions]` and clears `characterIsHitting` (:423-428). A bonus pickup cancels the post-hit invulnerability immediately.
- **QUIRK:** the body is not destroyed at death. It stays as an invisible, solid obstacle until the level is restarted.

### Progression within a run (`updateCharacterSettingsAfterLevel` :387-419, `levelUp:` :533-546)
"Bullets per volley" (`shootCount`) for Normal/Water shots, by campaign level number:

| Level | shootCount | Spread |
|---|---|---|
| 1-10 | 1 (initial value; not set again for this range) | - |
| 11-20 | 2 | ±2° |
| 21-30 | 3 | -4°, 0°, +4° |
| 31-50 | 4 | -6°, -2°, +2°, +6° |

- The spread step is `SHOOT_DIFFERENCE_ANGLE 4.0f` (:29). The first angle is `-(n-1)·4/2` (:629).
- Boss levels (`level % BOSS_LEVEL_CREATE_CONST(10) == 0`) disable the special ability (:409-412).
- Each level start also resets life = 3 and clears active bonuses and the shot type.
- Capture-the-Flag and Rush modes call `levelUp:YES` every 8 captures, which gives `shootCount++`, and `levelUp:NO` on reset, which sets shootCount = 1 (TakeTheFlagGameMode.mm:128-131, 161; RushGameMode.mm:118-121, 149).
- **QUIRK:** `if(shootCount == 4) shootCount = 4;` (:538) is not a cap, so shootCount grows without limit in those modes.
- **QUIRK:** the level-number rule also applies in non-campaign modes, using whatever `LevelManager.currentLevel` is.

### Special ability - Slow Motion (`slowMotion` :586-597, `runSpecialAbilitiesTimer` :338-346, `specialAbilitieActionTimer:` :348-385)
- Triggered by **Space** → `GameManager characterSpecialAbilitie`. It does nothing unless `canUseSpecialAbilitie` is set (GameManager.mm:360-382). Only `kSlowMotion` is ever selected. The `bigExplosion`, `invisible` and `fastRun` ability paths exist but are unreachable.
- Effect: `goBulletinTine` toggles scheduler timeScale between 1.0 and **0.35**. It also sets the blur on the player/bullet sprite sheet (2.0 when on, 1.0 when off), changes the blend func on the player and vitamin sheets, and calls `refresh` on the player and game world (GameManager.mm:327-358; GameResourceManager.mm:52-73).
- Duration: a 0.1 s real-time NSTimer adds `SPECIAL_ABILITIE_TIMER_STEP 0.1` per tick, up to `SPECIAL_POWER_TIME_LIMIT 10`. That is **10 s real time**; the counter only advances while unpaused (ConfigConstants.h:436-437). The ability also ends early if the level is won.
- When it ends: `canUseSpecialAbilitie = NO` and slow motion is toggled off. The HUD bar, which drained 1 % per tick (`SPECIAL_ABILITE_PROGRESS_STEP 1.0`), then refills at 2 % per 0.1 s. That takes **5 s**, after which `reFillSpecialAbilitieBar` sets `canUseSpecialAbilitie = YES` (Layers/LevelOverLayer.mm:245-298).
- Total cycle: 10 s active + 5 s recharge. It cannot be cancelled early: pressing Space again does nothing because `isInSpecialAbilitie` is set.
- **QUIRK (bug, inferred from the code path):** pressing Space **while paused** sets `isSlowMotion = YES` and starts the timer, but `goBulletinTine` returns early because the game is paused. After resuming, 10 s pass with no slow motion. The end-of-ability toggle then turns slow motion **on** and leaves it on until something else resets the timeScale.
- **QUIRK:** `bigExplosion`/`fastRun` never reset `isBigExplosion`/`isFastRun` when the ability ends. This is dead code today.

### Bonuses on the player (`getBonusWithType:` :421-499, `characterBonusActionHasEnded` :501-531, timer in `update` :817-842)
See the Bonuses section for the effects. Timer mechanics:
- `bonusTimeCounterTick += 0.01 × timeScale` **per frame**. At `BONUS_DISAPPEAR_LIMIT 7.0` the "about to expire" warning plays: tint back to white over 1.5 s (weapon/fast bonus) or `CCFadeIn 1.0 s` (invisible). At `BONUS_TIME_COUNTER_LIMIT 8.0` the bonus ends (:33-34).
  - That is 800 frames: **≈13.3 s at 60 fps** (warning at ≈11.7 s). In slow motion it is still 13.3 s of game time.
- **QUIRK (frame rate):** the duration is frame-based. Use 8.0 × (1/0.6) ≈ 13.3 s of scaled game time in Unity, or decide on a clean value.
- **QUIRK:** picking up a new timed bonus while one is active does **not** reset the counter. The new bonus only gets the remaining time.
- **QUIRK (bug):** picking up Life or BOOM while a weapon bonus is active sets `currentBonusObjectType` to Life/BOOM. When the timer ends, the switch finds no weapon type, so the bonus weapon (and the magenta tint) **stays for the rest of the level**.
- Restart/death/level change clear all bonus state (:853-908, :911-936, :413-418).

### Assets
`character.png` (sheet `characters_resources`, z 0, tag 1). Tint `ccc3(255,0,255)` (magenta) while a weapon or Fast bonus is active. Opacity 50 while Invisible.
`glowSprite` is declared but never created. There are no sounds in this class.

### Dependencies
`WeaponManager`, `GameManager` (pause state, slow motion, HUD updates, score), `AiInput` (publishes position/rotation/shooting), `InputManager` (move type), `CharacterPropertiesManager`, `GameSettingsManager` (auto-fire), `LevelManager` (level number), `AgentsManager` (BOOM), `LocalizationManager` (bonus names).

### Dead fields
`currentScale`, `scaleCounter`, `SCALE_CONST 0.1` (MainCharacter.h:18), `MAIN_SCALE_FACTOR 0.12`, `MAX_SCALE_LIMIT 6` (:27-28), `bullets` array, `moveCharacterWithPosition:` (empty), `resetCharacterBody` (empty), `pause` (empty), `reset` (empty).

### Unity mapping
- `Rigidbody2D` (gravity 0, kinematic-like velocity control, or dynamic with drag) + `CircleCollider2D` r = 0.65 u, if 1 Unity unit = 1 Box2D metre. `PlayerController` state: Alive / Invulnerable(2 s) / Dead.
- Stats in a `PlayerStats` ScriptableObject: speeds, lives, invulnerability, bonus duration, ability duration/recharge. Read upgrades from the save system.
- Firing as a `ShotPattern` strategy per shot type (ScriptableObject) driven by a cooldown in scaled time.

---

## 3. `MainCharacterController` (Characters/MainCharacterController.mm)
A thin singleton facade (`sharedCharacterController` returns the last created instance, :38-41).
- `initWithgameLayer:` creates `MainCharacter` at `winSize/2` and stores `startPos` (:18-35).
- `simulateCharacterRestartAtPosition:` ignores its argument and uses `startPos` (:79-82).
- `startShoot` checks `shooting` before starting (:90-98). Everything else is forwarded.
- `Die` and `rotateCharacter` are unused or trivial. `resetCharacterBody` calls `Figure.resetBody`, which destroys the body.
- Unity: not needed. Fold it into the player MonoBehaviour plus a `PlayerSpawner`.

## 4. `Character` / `CharacterController` (abstract)
Every method throws `"Must Oveeride!"` (Character.mm:18-58; CharacterController.mm:16-50). There is no behaviour.
`MOVEMENT_SPEED 2` (CharacterController.h:10) is unused. Drop both classes.

---

## 5. Persistent upgrades - `CharacterPropertiesManager` (Characters/CharacterPropertiesManager.mm)

### Stats stored
| Property | Meaning | NSUserDefaults key | Default when absent |
|---|---|---|---|
| `currentShootType` (ShootType 0-4) | Which shot type the player uses by default | `"shoottype"` (:13) | 0 = Normal |
| `isDoubleSpeed` (BOOL) | ×2 movement impulse, always on | `"doublespeed"` (:15) | NO |
| `isImmortal` (BOOL) | `hit` is ignored completely; the player can never lose a life | `"immortality"` (:14) | NO |

Values are written as `NSKeyedArchiver(NSNumber)` and then `AES256Encrypt` (`NSData+AES256` category in Managers/InAppManager/), in `saveProperties` (:60-78). They are read and decrypted in `init` (:39-55).
The player reads them once, at creation (MainCharacter.mm:110-112).

### Purchase UI and costs (in `Layers/CharacterUpgradeLayer.mm`, documented here for completeness)
These are one-off unlocks paid with **vitamins**. There are no multi-level stat upgrades.

| Row | Item | Cost (vitamins) | After purchase |
|---|---|---|---|
| 0 | Normal Shoot | free ("") | Select as the default shot |
| 1 | Water Shoot | 20000 | Select as the default shot |
| 2 | Windmill Shoot | 20000 | Select as the default shot |
| 3 | Circle Shoot (= `kExplosionShoot`) | 20000 | Select as the default shot |
| 4 | Tornado Shoot | 20000 | Select as the default shot |
| 5 | Double Speed | 40000 | Toggle on/off freely |
| 6 | Immortality | 1000000 | Toggle on/off freely |

Costs: CharacterUpgradeLayer.mm:126-132. Row index = `ShootType` value for rows 0-4 (:337).
The "bought" flags are stored under keys `watershootisboughtkey`, `windmillshootisboughtkey`, `circleshootisboughtkey`, `tornadoshootisboughtkey`, `doublespeedisboughtkey` and `immortalityisboughtkey` (ConfigConstants.h:443-448). Each value is the AES256-encrypted UTF-8 of the matching `*_VALUE` string (ConfigConstants.h:450-455; CharacterUpgradeLayer.mm:333-335).
The vitamin balance is `GameSettingsManager.vitaminsCount` (key `"vitamins"`, encrypted).
If the player cannot afford an item, an NSAlert shows `_NO_VITAMINS` (:47-64).
- **QUIRK:** `encriptedValuesArray` (:95-124) is built and never used.
- **QUIRK:** the Normal row uses the key `""`. It works by accident: it "buys" for 0 vitamins.

### Unity mapping
- Save data: `ownedShotTypes` (flags), `selectedShotType`, `doubleSpeedOwned/Enabled`, `immortalityOwned/Enabled`, `vitamins`. Keep the costs in a ScriptableObject price table. Use a platform save API (Steam Cloud / console save) instead of NSUserDefaults+AES.

---

## 6. Weapons - `Weapon`, `WeaponManager`, `WeaponFactory`, `Bullet`

### Structure
- `WeaponManager` is a singleton that owns **one** `Weapon` (WeaponManager.mm:30-37). All player fire goes through `shootAtPosition:andWithDirection:andRotateAngle:andCharacterVelocity:` (:42-51). `update` → `updateBullets`. `restart` destroys all bullets. `gameOver` flags all bullets for removal.
- `Weapon` keeps a `vector<Bullet*>`. Its only stat is `shootSpeed = 0.1f` s (Weapon.mm:33). Enemies create their own `Weapon` instances and use the `andStartDelta:` variant (enemy doc).
- `WeaponFactory getWeaponWithType:` returns `nil` for every input (WeaponFactory.mm:14-23). It is dead code. The `WeaponType` and `BulletType` enums (Pistol, Shotgun, Rifle, Sniper, Grenade, Javelin) are leftovers from an earlier game and are unused.
- `+sharedWeapon` exists but is unused.

### Bullet (Weapon/Bullet.mm)
| Property | Player bullet (`initAtPosition:...andCharacterVelocity:`) | Enemy bullet (`...andStartDelta:`) |
|---|---|---|
| Speed | `40.0` m/s = **1280 pt/s** (:29) | `30.0` m/s = 960 pt/s (:69) |
| Spawn offset | `bulletDelta 40` pt ahead along the direction (:30, :40-41) | `startDelta`, given by the enemy (:70) |
| Body | dynamic, `bullet=true` (CCD), `allowSleep=false`, box half-extents **0.0005 m** (:48, :105-127) | box half-extents 0.1 m (3.2 pt) (:88) |
| Fixture | density not set (0 → Box2D gives the body 1 kg mass), friction 0.2 default, **not a sensor**, no filter | same. Some enemies set it to a sensor afterwards |
| Damage | `damage = 1.0` is never read. Each contact = 1 `hit` (:28) | same (:68) |
| Visual | `bullet.png` (28x22) scale 1.5 + `Shine1.png` glow scale 0.5, rotation = angle − 90, z 5 / z 6 in the player sheet (:31-38, :56-57) | scale 1.0, same glow (:71-78) |
| Lifetime | **none**. It lives until it touches a wall, an enemy, another bullet or the player. `bulletLifeCycleTimer:` exists but is never scheduled (:138-141) | same |
| Ammo | unlimited | - |

- The velocity is set once: `SetLinearVelocity(dir/|dir| × speed)` (:51-54). The `characterVelocity` argument is **ignored**, so bullets do not inherit the player's velocity.
- Every frame the sprite follows the body (:129-136). The tag is 0 for the player. Enemies use the tags `SHOOTING_COROSIA_BULLET_TAG 2` and `FRUNCTUS_BULLET_TAG 3`. `CHARACTER_BULLET_TAG 1` is unused.
- **QUIRK:** `Weapon.updateBullets` multiplies `bullet.speed` by timeScale every frame (Weapon.mm:94). This has no effect, because speed is only used at spawn. It is also compounding. Remove it.
- **QUIRK:** `updateBullets` erases inside a forward loop without `i--` (Weapon.mm:105), so the next bullet's update is skipped for one frame. Harmless.
- **QUIRK:** the bullet has about 1 kg mass and is non-sensor, so it physically pushes whatever it hits. A non-sensor enemy bullet hitting the player gives a real knockback in the same step before it is removed (inferred from the Box2D contact order). Player bullets against enemies give no push, because enemies are sensors.

### Player shot types (patterns in MainCharacter.mm; rates in `startShoot` :278-300)
Base rate `R = Weapon.shootSpeed = 0.1 s`. Intervals are real-time NSTimer intervals.

| ShootType | Name (UI) | Bullets per volley | Pattern | Interval (normal) | Interval (slow motion) | Code |
|---|---|---|---|---|---|---|
| 0 `kNormalShoot` | Normal Shoot | `shootCount` (1-4) | Aim ± 4° steps | **0.1 s** | 0.1 × 2.4 = 0.24 s | `shoot` :610-646 |
| 1 `kWaterShoot` | Water Shoot | `shootCount` | Same as Normal | **0.1/5 = 0.02 s** | 0.1/2.4 ≈ 0.042 s | :281-283, :292-294 |
| 2 `kWindilShoot` | Windmill Shoot | 8 | 4 pairs at aim + {-5,+5}, {85,95}, {175,185}, {265,275}° (a "+" cross of double shots) | 0.1 s | 0.24 s | `windmillShoot` :677-703 |
| 3 `kExplosionShoot` | Circle Shoot | 36 | Full ring every 10°, starting at aim (`EXPLOSION_SHOOT_VARIANCE 36`) | **0.2 s** | 0.5 s | `explosionShoot` :782-797, :285-297 |
| 4 `kTornadoShoot` | Tornado Shoot | 3 | Absolute angle α−10°, α, α+10°. α starts at 0 (up) and advances 3 × 10 = **30° per volley** (3 × 8 = 24° in slow motion). It ignores the aim | 0.02 s | ≈0.042 s | `tornadoShoot` :648-675, `TORNADO_DEFAULT_ALPHA_INCREASER 10/8` :31-32 |

- Windmill, Circle and Tornado ignore `shootCount`.
- Unlocks: the default type comes from the upgrade shop (section 5). Any type can also be granted temporarily by a weapon bonus (section 8).
- **QUIRK:** `refresh` (called when slow motion toggles) recomputes the rate. In Auto Shoot mode it uses `/3` instead of `/5` for Water/Tornado (:754-757), so their rate becomes **0.033 s** after a slow-motion toggle until shooting restarts.
- **QUIRK:** 0.02 s intervals are below one frame (16.7 ms). The effective rate is limited by run-loop granularity (≈1 volley per frame; inferred).
- **QUIRK:** the slow-motion fire interval ×2.4 (real time) partly compensates the 0.35 timescale (×2.86). Shots per game-second therefore rise slightly during slow motion. Water/Tornado use /2.4 instead of /5, i.e. ×2.08 slower.
- The tornado angle α persists across activations and bonus pickups (it is never reset).

### Unity mapping
- `Bullet` prefab: `Rigidbody2D` (Continuous, gravity 0, velocity set once) + small trigger collider. Pool it (`ObjectPool<T>`).
- `ShotPattern` ScriptableObjects (count, angles, rate, slow-motion rate) + a `Weapon` component with a cooldown in **scaled** time. Pick one clean behaviour instead of the NSTimer real-time mix.
- Add a max lifetime (e.g. 3 s) as a safety net. The original relied on the arena walls.

---

## 7. Vitamins (currency) - `Vitamin`, `VitaminManager` (AI/)

### Behaviour
- **Drop:** when an enemy is released with `hitCount <= 0` (killed), it spawns `vitaminsCount` vitamins of score 1 (e.g. BlueCorosia.mm:107-113).
  Per-enemy counts (enemy doc for details): BlueCorosia 2, Gridium 2, ChoruxDefender 2, Loctopus 3, RedNoise 3, SpiralCorosia 3, Chorux 3, LotusBacteriaBody 3, SerpentusBody 1, ZigZagus 4, Adrenalium 5, BigBlueCorosia 5, BigHunterus 5, Gnida 5, ZamriGnida 6, ShootingCorosia 7, Sunux 7, Spawner 10, Frunctus 10, BigBoss 10, BigRedNoise 10, Inferno 10.
  Anakonda drops one vitamin worth 10 (Anakonda.mm:280). ArmagedonBacteria drops 1 at its position. Peace Walker mode spawns vitamins itself (PeceWalkerGameMode.mm:129-131).
- **Spawn position** (`getValidBonusSpawnPointAtPoint:`, VitaminManager.mm:46-73): a uniform random point in a ±`VITAMIN_CREATE_RADIUS 80` pt square around the enemy, clamped into the arena with a `BOUNDER_ESCAPE_DELTA 30` pt margin. `alpha` is computed but unused.
- **Vitamin object** (Vitamin.mm:25-65): type `kVitamin`, sensor circle **0.6 m (19.2 pt) diameter**, sprite `WX_circle_white.png` (bonuses sheet, 100 pt) at scale 0.3 that spins 90° per 0.1 s (900°/s). It starts in `kStateWaiting`.
- **Magnet:** each frame, if the player is within `VITAMIN_GET_DISTANCE 500` pt and alive, the state switches to `kStatePatrol`, i.e. `EnemyAgent.patrol` (Vitamin.mm:15, 67-73; EnemyAgent.mm:58-76). The vitamin then flies straight at the player at `speed 40` m/s = **1280 pt/s** (velocity re-aimed every frame). Once homing, it never returns to waiting.
- **Pickup:** contact with the player → `flag = kDealloc`, `GameManager updateVitaminsCount` → HUD shows `VitaminManager.vitaminsCount` (ContactListener.mm:177-189; GameManager.mm:317-320).
- **No expiry.** Uncollected vitamins stay until the level is released.
- `hitCount 1`, `killScore 80`, `shootRate`, `shootDistance` are set but irrelevant. Bullets do not interact with vitamins.

### Bookkeeping
- `VitaminManager.vitaminsCount` is initialised from `GameSettingsManager.vitaminsCount` (:36-44).
- **QUIRK (important):** `createVitaminAtPosition:` adds the score **when the vitamin is spawned** (:75-85). Pickup only refreshes the HUD, so vitamins are earned on every kill whether or not they are collected. Decide in the rewrite whether pickup should be required. The visible design implies yes.
- The total is saved (`synchronizeVitaminsCount`) only in `releaseVitaminManager` (:87-90, :114-126), which `GameModeManager` calls on level release/restart (GameModeManager.mm:101, 111).
- **QUIRK:** `update` removes at most one dead vitamin per frame (`break`, :107). Harmless.
- Vitamins are spent in `CharacterUpgradeLayer` (section 5). `StoreLayer` sells vitamins via in-app purchase (other doc).

### Unity mapping
- `Vitamin` prefab: trigger `CircleCollider2D`, simple Idle → Homing state, homing speed 1280 pt/s (40 u/s).
- `CurrencyService` with an explicit "credit on pickup" or "credit on drop" option. Persist at level end.

---

## 8. Bonuses - `BonusManager`, `BonusObject`, `UsableBonus` (Managers/BonusManager/)

### `BonusObjectType` (ConfigConstants.h:270-284) and effects on pickup (MainCharacter.mm:421-499)
| # | Enum | UI text (EN) | Effect | Duration |
|---|---|---|---|---|
| 0 | `kWaterShootBonus` | Water Shoot | Shot type = Water, magenta tint | timed (≈13.3 s) |
| 1 | `kExplosionShootBonus` | Circle Shoot | Shot type = Circle (36-ring) | timed |
| 2 | `kWindmillShootBonus` | Windmill Shoot | Shot type = Windmill | timed |
| 3 | `kTornadoShootBonus` | Tornado Shoot | Shot type = Tornado | timed |
| 4 | `kLifeBonusBonus` | +1 Life | `life = min(life+1, 3)` and HUD update | instant |
| 5 | `kExplostionBonus` | Boooooom! | `AgentsManager bigExplosion`: every alive enemy within `BIG_EXPLOSION_DISTANCE 700` pt of the player loses `BIG_EXPLOSION_HIT_CONST 20` hits (AgentsManager.mm:116-126; EnemyAgent.mm:252-258). No score is awarded through this path | instant |
| 6 | `kMakeInvisibleBonus` | **Immortality** (the text says Immortality) | Opacity 50 + `isInvisible`: `hit` is ignored. Enemies are **not** affected (no AI reads it) | timed |
| 7 | `kFastMovementBonus` | Double Speed | Movement ×2 (no effect if the Double Speed upgrade is on) | timed |
| 8 | `kFlagBonus` | - | Capture-the-Flag pickup: +`FLAG_CONST_POINT 100` score, `canGenerateBonus:YES` (game-modes doc) | - |
| 9 | `kPeaceFlagBonus` | - | Peace Walker pickup: +100 score, `gameModeSpecialAction` (game-modes doc) | - |
| 10 | `kRushBonus` | - | Rush station (game-modes doc) | - |

- The pickup text is shown on the HUD via `updateBonusOnLevelOverLayer:` (MainCharacter.mm:497). If the player is firing and a weapon bonus is taken, shooting restarts with the new rate (:492-495).
- Timed-bonus mechanics and their bugs: see section 2, "Bonuses on the player".

### `BonusManager` (BonusManager.mm)
- `generateRandomBonus`: type = `arc4random() % MAXIMUM_BONUS_RAND_CONST(8)`, i.e. uniform over types 0-7, each 12.5 % (:18, :55-62).
- `generateLifeBonus` / `generateBonusWithType:` (:46-70). Position: `SpawnManager generateValidBonusSpawn`, a random angle at a **700-710 pt** radius (`BONUS_SPAWN_RADIUS`, `_2`) from the player, clamped into the arena with a 30 pt margin (SpawnManager.mm:261-300).
  - **QUIRK:** the "too close" check compares the player position with the *offset* vector before adding it (SpawnManager.mm:276), so it is effectively random.
- `generateFlag`, `generateRushStation`, `generateRandomPeaceWalkerBonus` (:72-104) create `Flag`/`RushStation`/`PeaceFlag` (game-modes doc). Rush and Peace objects spawn around the screen centre.
- All bonuses are added to the game layer at **z 10** (RushStation progress timer at z 1000).
- `update` releases `kDealloc` objects and otherwise calls `update` (:106-117). It has the same erase-in-loop skip as `Weapon.updateBullets`.

### When bonuses drop (callers in the game modes; details in the game-modes doc)
- Campaign: on every 20th tick of the 1 s real-time timer, i.e. **every 20 s** (`BONUS_CREATE_LIMIT_CONST 20`), except on boss levels and on the tick equal to the level time (CompaignGameMode.mm:19, 120-129).
- Capture the Flag and Rush: one random bonus every 8 captures (together with `levelUp`) (TakeTheFlagGameMode.mm:128-132; RushGameMode.mm:118-122).
- Peace Walker: a life bonus every 8 flag generations (`LIFE_BONUS_GENERATOR_CONST 8`) (PeceWalkerGameMode.mm:23, 150-155).
- Enemies never drop power-ups. They only drop vitamins.

### `BonusObject` (BonusObject.mm)
A base class derived from `Agent`, so it uses `Agent.createNewBodyAt`: a dynamic **sensor** circle with density 1 (AI/Agent.mm:81-105). Its `initWithType` returns self without setting anything. `releaseAgent` removes the sprite and destroys the body (:25-29).

### `UsableBonus` (UsableBonus.mm)
- Sprite `bonus.png` (a separate file, not in a sheet) tinted by type from `colors[7]`: red, green, blue, yellow, magenta, cyan, (128,128,255) (:16-24, :45-55).
  - **QUIRK (bug):** type 7 (Fast Movement) reads `colors[7]`, past the end of the array, so its colour is undefined.
- Body: sensor circle 1.3 m (41.6 pt) diameter (:48-63).
- Movement: `randomfly` toward a random point in the arena (resolved through `SpawnManager getRandomPointAroundPoint:`) at `speed 4` m/s = **128 pt/s**. It picks a new point when within 10 pt (:87-97, :124-151).
- Lifetime: `CCFadeOut 15 s` (`FADE_OUT_INTERVAL`) starts at spawn (:26, :73-79). The bonus is removed once opacity < 70, i.e. after about **10.9 s** (:101-104), or at the end of the fade.
- **Campaign only - type cycling:** `bonusChangeTick += 0.01` per frame. Each time it reaches 1.0 (**100 frames ≈ 1.67 s** at 60 fps, frame-based and not time-scaled), the type advances 0 → 1 → … → 7 → 0 and the tint changes (:106-122). The player picks whatever type it shows at contact. In other modes the type is fixed at spawn.
- Pickup: ContactListener sets `kDealloc` and calls `getBonusWithType:` (ContactListener.mm:121-163).

### Unity mapping
- `PowerUpDefinition` ScriptableObjects (type, colour, duration, effect). A `PowerUpPickup` prefab with a trigger collider, a wander mover, a fade timer and an optional type-cycle timer (in seconds).
- Player `ActiveBuff` system with one timed slot (or proper stacking rules). Fix the expiry bug.

---

## 9. Input and controls - `InputManager`, `KeyBind`, `HUDLayer` (event source)

### How events flow
`HUDLayer` (Layers/HUDLayer.mm) receives the cocos2d-mac mouse and keyboard events. It builds a `KeyBind{keyCode = first unichar of [event characters], keyQueue = 0}` and calls `InputManager proceedKey:down:` (:103-150).
Mouse events go to `GameManager rotateWithMouse:` → `InputManager.newMouseInputPositions` (screen points) (:39-68).
Each physics frame, `InputManager inputEventListener` (:256-281) does the following, unless input is disabled or the game is "preparing":
1. Recomputes the aim toward `mouseScreen + camera.cameraPosition`. **This happens in every mode.**
2. Moves the player: by keys in modes 0/1, toward the mouse in mode 2.

### Control schemes (`CharacterMoveType`, chosen in Settings → "Control Type"; default `kKeyBoardAndMouse`, Auto Shoot default OFF - GameSettingsManager.mm:81-82)
| # | Enum | Settings label | Move | Aim | Fire |
|---|---|---|---|---|---|
| 0 | `kKeyBoardAndMouse` | "WASD + ARROWS" | **W A S D or arrow keys** (arrows fall through to the WASD cases, InputManager.mm:97-136) | Mouse cursor (360°) | **Left mouse button** held (start on down, stop on up). Ignored while paused or preparing (HUDLayer.mm:70-101) |
| 1 | `kOnlyKeyboard` | "Only Arrows" | **W A S D** | **Arrow keys**: 8 directions, diagonals by combining | Holding any arrow fires. Releasing all arrows stops (:149-159, :211-221) |
| 2 | `kOnlyMouse` | "Only Mouse" | Always moves toward the cursor at full speed | Mouse cursor | Left mouse button held |

Keys that work in every scheme:
| Key | Code (unichar) | Action |
|---|---|---|
| Space | 32 `KEY_SPACE` | Special ability (Slow Motion) (:137-139) |
| Esc | 27 `ESC_KEY` | Pause; Esc again resumes (:140-145). In menu layers Esc = back |
| Arrows | 63232-63235 `KEY_UP/DOWN/LEFT/RIGHT` (NSUpArrowFunctionKey…) | See the table above |
| W/S/A/D | 119/115/97/100 (lower-case only) | Move |

- **Auto Shoot** (Settings toggle, `isAutoFireEnabled`): shooting starts automatically 2 s after spawn and never stops. Mouse buttons and arrow keys no longer start or stop fire. In "Only Arrows" mode it still fires only while an arrow is held, because the direction is (0,0) otherwise (MainCharacter.mm:315-316).
- **QUIRK:** keys are matched by **character**, not by physical key code. Caps Lock or Shift gives `W` = 87, which does nothing. Non-QWERTY layouts (AZERTY) break WASD.
- **QUIRK:** Cmd+key is ignored on key-down but not on key-up (HUDLayer.mm:110-112).
- **QUIRK:** in "Only Arrows" mode the stale mouse position is still used every frame. When no arrow is held, the ship turns to face the last mouse point (initially screen (0,0) + camera, i.e. bottom-left).
- **QUIRK:** Esc while the game is finished calls `resumeGame` (:141-144).
- Only the left button is used (`ccMouseDown`). Right and middle buttons and the scroll wheel are unused. Mouse moves are ignored for aiming in "Only Arrows" mode (HUDLayer.mm:41-50).
- Aim maths: direction from the screen centre to the cursor. This works even with the camera zoom (section 11) because the zoom is centred.

### Rebinding (`KeyBind`)
**There is no key rebinding.** `KeyBind` (Utils/KeyBind.h/.mm) is a plain object holding `keyCode` and `keyQueue`. `keyQueue` is always 0 because `keyCodeCounter` is never incremented. The keys are hard-coded `#define`s (ConfigConstants.h:392-403).

### Joystick / HID
`InputManager.h` imports `DDHidLib` and declares `joySticks`. `joystickListener` (:61-65) would enumerate `DDHidJoystick allJoysticks`, but **it is never called**, and all four delegate callbacks (stick axis, POV, button down/up) are empty (:67-85). **The current game has no gamepad support.**

### Menus
All front-end screens (main menu, mode select, level select, upgrade shop, vitamin store, settings, pause and level-over menus) are **mouse-click only**, plus Esc = back. They need full UI navigation for consoles.

### Player actions that need a gamepad binding (rewrite)
| Action | Current binding | Suggested gamepad |
|---|---|---|
| Move (2D) | WASD / arrows / mouse-follow | Left stick (+ D-pad) |
| Aim (2D, 360°) | Mouse / arrows (8-way) | Right stick (analogue 360°; keep the last aim when centred) |
| Fire (hold) | LMB / hold arrow | Right trigger, **or** fire automatically while the right stick is deflected ("Only Arrows" behaviour) |
| Auto Shoot toggle | Settings | Settings (could be the default on gamepad) |
| Special ability (Slow Motion) | Space | Left trigger / bumper or A |
| Pause / resume | Esc | Start / Options |
| UI navigate / confirm / back | Mouse / Esc | D-pad + stick / A (South) / B (East) |
| Upgrade shop buy/toggle, settings arrows, level grid | Mouse click | UI Submit on a focused item; left/right for selectors |

### Unity mapping
- Input System `PlayerInput` with action maps **Gameplay** (Move, Aim, AimPointer, Fire, Ability, Pause) and **UI**. Use control schemes Keyboard&Mouse, KeyboardOnly and Gamepad instead of the 3 hard-coded modes.
- Use physical key bindings (layout-independent) and add Input System rebinding UI. Steam and the console TRCs expect remappable controls.

---

## 10. Physics - `Physics`, `Figure`, `Bounder`, `ContactListener`, `DestructionListener`

### World (GameCore/Physics/Abstracted/Physics.mm)
- One global `b2World` created lazily through `Figure +sharedPhysicsWorld` (Figure.mm:38-51). It is never destroyed.
- Gravity **(0, 0)** (:28). `SetContinuousPhysics(true)` (:35). Contact and destruction listeners are set (:38-41).
- A debug draw object is created with the shape bit (:44-55), but `DrawDebugData` is never called.
- It is stepped by `GameManager` with the variable `dt` and 8/2 iterations (section 1).
- **Scale:** 32 pt per metre (`ASPECT_RATIO`). Sprite ↔ body positions are divided or multiplied by 32 everywhere.

### `Figure` (GameCore/Objects/Abstracted/Figure.mm)
- The base for every physics object. Fields: `sprite`, `body`, `type` (`GameObjectType`), `flag` (`ObjectEvent` lifecycle: `kDefault` alive, `kDealloc` marked for removal at the owner's next update, `kDying` used by enemies).
- Default `createNewBodyAt:` = dynamic **sensor** box, density 1, friction 0.3 (:58-82). `updatePhysicsBodyPosition:` teleports the body (:118-123). `resetBody` deactivates and destroys the body (:139-143).
- `createNewGroundBodyAt:` (32x32 edge tiles) and `tileMap`, `animation`, `objectMovingFlag`, … are legacy from the earlier "Tanks" game and unused.
- Pattern: collision callbacks only set `flag`. The owning managers destroy bodies outside the step. This is safe; keep the idea (deferred destroy) in Unity.

### Body inventory (no collision filtering anywhere: no categoryBits, maskBits or groupIndex in the codebase)
| Object | `type` | Body | Shape / size | Sensor | Density / friction | Source |
|---|---|---|---|---|---|---|
| Player | `kMainCharacter` | dynamic, damping 6 | circle r 0.65 m (20.8 pt) | no | 1 / 0.2 | MainCharacter.mm:124-146, 165 |
| Player bullet | `kBullet` | dynamic, bullet (CCD) | box ±0.0005 m | no | 0 (→1 kg) / 0.2 | Bullet.mm:48, 105-127 |
| Enemy bullet | `kBullet` (tag 0/2/3) | dynamic, bullet | box ±0.1 m | no (Frunctus, Anakonda and ShootingSerpentus set sensor) | 0 / 0.2 | Bullet.mm:88 |
| Enemy | `kEnemy` | dynamic | circle (size per enemy) | **yes** (Agent default) | 1 | Agent.mm:81-105 |
| Usable bonus | `kBonusObject` | dynamic | circle d 1.3 m | yes | 1 | UsableBonus.mm:48-63 |
| Vitamin | `kVitamin` | dynamic | circle d 0.6 m | yes | 1 | Vitamin.mm:42-56 |
| Rush station / flags | `kRushStation` / `kBonusObject` | - | - | yes | - | game-modes doc |
| Arena wall | `kBounder` | static | box (see Bounder) | no | 1 / 0.8 | Bounder.mm:13-46 |

Physical (non-sensor) interactions that result: player ↔ walls (blocked), bullets ↔ walls, bullets ↔ player (push), bullets ↔ bullets. Everything else only produces contact events.
Unused `GameObjectType` values: `kEnemyBullet`, `kSniperLaser`, `kGameWorldObject`.

### Contact → gameplay rules (`ContactListener::BeginContact`, ContactListener.mm:32-191)
Both orderings (A/B) are handled for each rule.
| Pair | Result | Lines |
|---|---|---|
| Bullet × Bullet (**any** two, player or enemy) | Both destroyed (bullets cancel each other) | :43-49 |
| Bounder × Bullet | Bullet destroyed | :51-58 |
| Bullet (tag ≠ 2,3, i.e. player bullet) × Enemy | Bullet destroyed unless the enemy is `kDying`. `enemy.hit` (hitCount −1, `kDealloc` at ≤0). If the enemy is now `kDealloc` **and** the mode is Campaign: score += `enemy.killScore` | :60-87 |
| Enemy bullet (tag 2 or 3) × Enemy | Nothing (enemy bullets pass through enemies) | :64, :67 |
| Player × Enemy (not `kDying`) | `player.hit` | :89-105 |
| Player × Bullet (**any** bullet) | `player.hit`, bullet destroyed | :107-119 |
| Bonus × Player | Bonus destroyed. Flag → +100 score + `canGenerateBonus:YES`. PeaceFlag → +100 + `gameModeSpecialAction`. Otherwise `player getBonusWithType:` | :121-163 |
| Player × RushStation | `setCharacterIsIn:YES` (`EndContact` → NO) | :165-175, :203-213 |
| Player × Vitamin | Vitamin destroyed, HUD vitamin update | :177-189 |
`EndContact` handles only the rush station. `PreSolve` and `PostSolve` are empty.

- **QUIRK:** the player's own bullets are also `kBullet`, so they would hurt the player on contact. In practice this never happens, because bullets spawn 40 pt ahead (radius 20.8 pt) and are faster than the player.
- **QUIRK:** `enemy.hit` is called even when the enemy is `kDying`. `hit` then rewrites `flag` to kDealloc/kDefault, which can cut a death animation short or award the kill score twice (inferred; see the enemy doc).
- **QUIRK:** kills by BOOM bonus, by big-explosion or by any non-bullet path award no score.
- Score per kill is **only** given in Campaign mode through this listener.

### `DestructionListener`
Both `SayGoodbye` overloads are empty (DestructionListener.mm:22-31). No behaviour.

### Arena bounds - `Bounder` + `GameWorld initWorldBoundaries`
- Arena corners (`CoreSettings.upperLeft/upperRight/bottomLeft/bottomRight`) = the edges of the background sprite `bg.jpg`, centred on the screen centre, inset by `ENEMY_CREATE_BORDER_DELTA 20` pt (GameWorld.mm:63-90).
- Walls (GameWorld.mm:99-167): `bounderDimension = 100` pt. Each wall is a static box with half-extents (100 pt, full arena height or width), centred 100 pt outside the corner line. **The inner face of each wall is exactly on the arena edge.** The walls are 200 pt thick and overlap at the corners. Bounder applies `CC_CONTENT_SCALE_FACTOR` (=1 on Mac) (Bounder.mm:20, 32).
- World size (**measured**): `bg.jpg` is **2176 × 1536 px** (checked in `Mac/Bacteria/Resources/bg.jpg`). Content scale is 1 on Mac, so that is 2176 × 1536 pt; inset by `ENEMY_CREATE_BORDER_DELTA` 20 pt on each side, the playable arena is about **2136 × 1496 pt ≈ 2.1 × 2.3 screens**. `SpawnManager` also hard-codes spawn corners (−555, −375)…(1576, 1124) (ConfigConstants.h:415-425), a 2131 × 1499 pt box: almost the same, but hand-entered, so the rewrite should derive spawn bounds from the arena instead.
- `GAME_WORLD_SIZE 1536` (ConfigConstants.h:23) is unused.

### Unity mapping
- 1 Unity unit = 1 m = 32 pt. Keep all Box2D numbers 1:1 (speeds in u/s).
- Players, bullets, enemies and pickups as **triggers** with layer-based collision (Player, PlayerBullet, Enemy, EnemyBullet, Pickup, Wall). That replaces the "everything is kBullet" ambiguity. Use a physics layer matrix instead of type checks.
- Walls: 4 `BoxCollider2D`s or an `EdgeCollider2D` loop on a static body. Arena size from the level/background asset.
- Decide explicitly whether bullets knock the player back (they did, via mass) and whether bullets cancel each other (they did, for any pair).

---

## 11. Camera - `Camera` (GameCore/Camera/Camera.mm)

- Created at game start (GameManager.mm:121). It is updated each frame from `managePhysics` with the player sprite (GameManager.mm:461, 500-503). It is skipped while the player's opacity is 0 (dead) (:35-36).
- **Follow:** the target is `playerPos − winSize/2` (camera offset that centres the player). Smoothing: `new = lerp(target, previous, 0.2)` = 80 % toward the target **each frame** (:58, :78).
  **QUIRK (frame rate):** the smoothing is per frame. Use a time-based damping in Unity (≈ exp decay with a 0.2 retention per 1/60 s).
- **Clamp:** intended to freeze the camera axis when the player passes the arena edge ± `CHARACTER_CAMERA_STOP_X/Y = 0` (ConfigConstants.h:433-434; :52-90). Because the walls' inner faces are on that edge and the player centre stays ≥20.8 pt inside, the freeze branches are effectively unreachable. **The camera always centres on the player and shows the void beyond the arena edge.**
- **Zoom:** it sets the layer's `CCCamera` `center = (x, y, 300)`, `eye = (x, y, 402)` (:92-93). With cocos2d 2.0's 3D projection (eye at about winHeight/1.1566 ≈ 553 pt for a 640-pt-high view), the extra eye Z of 402 pushes the scene about **0.55-0.58×** smaller. The visible world is therefore about 1.75× the logical screen, centred on the player (inferred from cocos2d 2.0 `CCCamera`/`CCNode transform` semantics; **verify in the Mac build**). The HUD layer is not affected.
- No shake, no dynamic zoom, no scrolling other than the follow. `CCFollow *folow` and `restart` are unused.
- World vs screen: logical screen = 1024 × (1024 / display aspect) (AppDelegate.mm:19, 49-58, `kCCDirectorResize_AutoScale`). The arena is about 2136 × 1496 pt, measured from `bg.jpg` (section 10).
- `cameraPosition` is also used to convert the mouse to world space (InputManager.mm:245-254).

### Unity mapping
- Orthographic `Camera` + `CinemachineCamera` (Position Composer, damping matched to the 0.8/frame follow, no dead zone) following the player. Ortho size ≈ (logical height / zoom) / 2 / 32 u, about 17 u for a 640-pt view at 0.58 (inferred).
- Optionally add Cinemachine Confiner 2D. The original did not confine.

---

## 12. Rendering helper - `CCSpriteBatchNodeBlur` (GameCore/CCSpriteBatchNodeBlur.mm)
- A sprite batch node with a 9-tap linear Gaussian blur fragment shader (`Resources/example_Blur.fsh`; `blurSize = f / textureSize` in one diagonal direction, `substract` = 0) (:16-49, :83-89).
- Three sheets use it: `characters_resources.png` (player + all bullets), `bonuses_spritesheet.png` (vitamins) and `enemy_direction.png` (GameResourceManager.mm:75-105).
- Blur size is 1.0 normally and **2.0 during slow motion** (GameManager.mm:338, 345). `updateWithSlowMotion` switches the blend between `(GL_DST_ALPHA, GL_DST_ALPHA)` (slow motion on; a washed-out additive look) and `(GL_ONE, GL_ONE_MINUS_SRC_ALPHA)` (src=1, dst=771) (:57-60).
- **QUIRK:** `draw` calls `[super draw]` and then draws the quads **again** with the blur shader (:61-78), so every sprite in these sheets is drawn twice. This may be part of the intended "glow" look. Compare against the Mac build.
- Unity: a URP 2D sprite material/shader with a blur/glow amount parameter, animated by the slow-motion state, or a post-process (Bloom + motion-blur style) volume blended in during slow motion.

## 13. `GameWorld` (GameWorld/GameWorld.mm)
- Purpose: creates the level background and arena, then delegates everything to `GameModeManager`. That covers `Run`, `update`, `simulateGameWorldRestart`, `gameWorldSpecialAction`, `gameModeActionLoadNextLevel`, `refreshGameWorld` and `stopGameWorldAfterCharacterDeath` (:169-201).
- `initWithGameLayer:` gives `BonusManager` the layer. It re-parents the three shared batch sheets (player/bullets, vitamins, enemy arrows) to the game layer at **z 1, tag 100** (:36-55).
- `initBackground`: `bg.jpg` at the screen centre, **z −1**. It sets the 4 arena corners in `CoreSettings` (:62-90).
- `initWorldBoundaries`: the 4 `Bounder`s (section 10) (:99-167).
- `releaseGameWorld`: removes the background, the walls, the game mode and the sheets (:203-225).
- Dead: ivars `enemyCreateTimer`, `enemyCreateTimeCounter`, `enemyCreateVar`, `realTimeTickVar`, `weHaveBossLevel`, `isFirstRun`. Defines `ENEMY_CREATE_TIMER_DELTA 10`, `BOUNDER_DELTA 30` (:21-22).
- Unity: an `Arena` prefab (background SpriteRenderer + wall colliders) instantiated by a `LevelBootstrap`. The arena bounds are exposed as a component that spawners can query.

## 14. `CoreSettings` (GameCore/CoreSettings.mm)
A global singleton:
- Arena corners `upperLeft`, `upperRight`, `bottomLeft`, `bottomRight` (set by GameWorld).
- `aspectRatio` = UI scale factor `1024/displayWidth` (or the inverse), used only to lay out menus (AppDelegate.mm:43-45).
- `mainWindow` (for NSAlert sheets).
- Unused: `needGaussianBlurLoad`, `defaultScreenResolutionWidth`.
- Unity: replace with an `ArenaBounds` component and Canvas Scaler.

## 15. `Utils` (Utils/Utils.mm)
- `getRandomNumber:from to:to` returns an inclusive int via `arc4random() % (to-from+1)` (:36-39). `vectorBetweenTwoPoint` = p2 − p1. `vectorWithAngle(rad)` = **(sin, cos)**, so 0 = up and angles are clockwise (:83-86). `timeScale` = scheduler timeScale. `pointInRect`. `calculateVectorLength`.
- `hashString:withSalt:` (HMAC-SHA256) is used by `LevelManager`.
- Unused: `scalePoint` (1024×1024 design), `GetRayCircleIntersect`, `DoRayCircleIntersect` (**QUIRK:** it returns true when there is *no* intersection, :118-128), the `LineIntersection2D` overloads and `annulizeArray`.

## 16. Dead / trivial classes
- `EnemyDirection` / `EnemyDirectionManager` (Characters/): meant to show `enemydirection.png` (scale 0.5, z 1) arrows pointing at off-screen enemies. `updateTarget` is empty and `createNextEnemyDirectionAtPosition:` is never called anywhere. The update loop also has the erase-without-`i--` bug. The empty `enemy_direction` sheet is still added to the layer. **A good feature to actually implement in the rewrite** (off-screen enemy indicators), given the zoomed-out follow camera and the 2×-screen arena.
- `WeaponFactory`: returns nil.
- `CCBlade` (Utils/): MIT-licensed swipe-trail mesh (Ngo Duc Hiep, 2011). It is only `#import`ed in MainCharacter.mm:18 and never instantiated.
- `DestructionListener`: empty.
- `GLES-Render`: Box2D `b2Draw` implementation for debug shapes. It is configured in `Physics` and never drawn. Not needed (Unity has physics gizmos).

---

## 17. `ConfigConstants.h` - constants and enums

### Related to this area (with usage)
| Name | Value | Used for |
|---|---|---|
| `ASPECT_RATIO` | 32.0f | **Points per metre** everywhere (not a screen aspect) |
| `GAME_NEXT_SIMULATION_STEP_INTERVAL` | 0.01f | Per-frame increment for bonus timers and type cycling (frame-based) |
| `GAME_WORLD_SIZE` | 1536.0f | Unused |
| `KEY_UP/DOWN/LEFT/RIGHT` | 63232/63233/63234/63235 | Arrow keys (NS function-key unichars) |
| `KEY_W/S/A/D` | 119/115/97/100 | Lower-case w/s/a/d |
| `KEY_SPACE`, `ESC_KEY` | 32, 27 | Ability, pause/back |
| `BIG_EXPLOSION_HIT_CONST`, `BIG_EXPLOSION_DISTANCE` | 20, 700 | BOOM bonus: −20 hits to enemies within 700 pt |
| `BOUNDER_ESCAPE_DELTA` | 30 | Margin when clamping vitamin/bonus spawns into the arena |
| `BONUS_SPAWN_RADIUS`, `_2` | 700.0f, 710.0f | Bonus spawn ring around the player |
| `UPPER_LEFT_X/Y`, `BOTTOM_LEFT_X/Y`, `UPPER_RIGHT_X/Y`, `BOTTOM_RIGHT_X/Y` | (−555,1124), (−555,−375), (1576,1124), (1576,−375) | Hard-coded arena corners in SpawnManager (Gridium lines). Approximately the real arena |
| `ENEMY_CREATE_BORDER_DELTA` | 20.0f | Inset of the arena from the bg.jpg edges |
| `CHARACTER_BULLET_TAG`, `SHOOTING_COROSIA_BULLET_TAG`, `FRUNCTUS_BULLET_TAG` | 1, 2, 3 | Bullet tags. Tags 2/3 = enemy bullets that pass through enemies. Tag 1 is unused (player bullets have tag 0) |
| `CHARACTER_CAMERA_STOP_X/Y` | 0, 0 | Camera clamp margin (effectively no clamp) |
| `SPECIAL_POWER_TIME_LIMIT` | 10 | Slow-Motion duration (s, real time) |
| `SPECIAL_ABILITIE_TIMER_STEP` | 0.1f | Ability timer tick (s). The refill adds step×20 = 2 % per tick |
| `SPECIAL_ABILITE_PROGRESS_STEP` | 1.0f | HUD bar drain per tick (%) |
| `FLAG_CONST_POINT`, `RUSH_CONS_POINT` | 100, 250 | Flag/peace-flag score; rush score (game-modes doc) |
| `*_IS_BOUGHT_KEY` / `*_IS_BOUGHT_VALUE` | strings | Upgrade purchase flags (section 5) |
| `BOSS_LEVEL_CREATE_CONST` | 10 | Every 10th level is a boss level (the ability is disabled there) |
| `TILE_WIDTH/HEIGHT` | 32 | Only in the unused `Figure.createNewGroundBodyAt` |

Enums:
- `GameObjectType` (:80-95): kMainCharacter 0, kBounder 1, kBullet 2, kEnemyBullet 3 *(unused)*, kEnemy 4, kSniperLaser 5 *(unused)*, kGameWorldObject 6 *(unused)*, kBonusObject 7, kRushStation 8, kVitamin 9.
- `ObjectEvent` (:129-138): kDefault, kMoving, kStaying, kDealloc, kDying, kReset. Only kDefault, kDealloc and kDying are used.
- `BonusObjectType` (:270-284): see section 8.
- `ShootType` (:326-334): kNormalShoot 0, kWaterShoot 1, kWindilShoot 2, kExplosionShoot 3 (= "Circle"), kTornadoShoot 4.
- `SpecialAbilities` (:336-343): kBigExplosion, kFastMovement, kSlowMotion, kInvisible. Only kSlowMotion is used.
- `CharacterMoveType` (:318-324): kKeyBoardAndMouse 0, kOnlyKeyboard 1, kOnlyMouse 2.
- `WeaponType` (:286-296) / `BulletType` (:298-307): unused leftovers.
- `CharacterDecisionFlag` (:309-316), `CharacterFlipDirection` (:120-127), `ObjectMovingFlag` (:140-148; only appears in the `CharacterController` signatures): unused.
- `BulletState` (:150-157), `BULLET_DOWN/RIGHT/LEFT/UP`, `TANK_TOTAL_BULLET_AMOUNT 20`, the `*_ANIMATION_*` defines, `MAX_TANK_ANIMATION_FRAME_COUNT`, `GAME_LAYER_MOVEMENT_SPEED 2`: unused ("Tanks" legacy).

### Other constants (other areas; listed briefly)
- Plist keys `CHARACTER_DATA_KEY` … `OBJECT_SPRITE` (only `ROOT_KEY` is used, by Level.mm).
- Game Center leaderboard IDs `com.AntiBacteria.CompaignMode/CaptureTheFlag/PeaceWalker/Rush`.
- `LayerType`, `GameStates` (kCharacterDyingState unused), `ObjectZLayer` (unused), `GameModes` (unused), `GameModeType` (kCompaignMode, kTakeTheFlagMode, kRushMode, kArmagedonMode, kPeaceWalkerMode).
- `AgentStateType` (enemy AI states).
- `EnemyAgentType` 0-27. The comment on `kArmagedonBacteria` says "//22" but its value is 27.
- `GameDifficulty` (kNormal, kHard, kImpossible), `PauseMenuType` (unused), `Localizations` with `LANGUAGE_COUNT 4` (5 languages listed in the enum).
- Unused legacy: `FishType`, `GameWorldObjectType`, `WorldObjectType`, `WorldObjectState`, `LandskapeType`, `LevelEditorBatchNodes`, `BGAnimationLayerDepth`, `IPAD_*`, `OBSTACLE_START_*`, `WORLD_TILE_OBJECT_COUNT`, `DESTRUCTABLE_OBJECT_COUNT`, `NEXT_TILE_WIDTH_HEIGHT`.
- Spawn radii `ENEMY_CIRCLE_SPAWN_RADIUS(_2)` 500/600, `ENEMY_ARROUND_RANDOM_POINT_RADIUS(2)` 500/520, `FRUNCTUS_RANDOM_AROUND_POINT(_2)` 300/301 (SpawnManager; enemy and game-modes docs).
- About 50 localisation key strings (`_START_GAME` … `_LIFE`). `_YOU_HAVE`, `_CHOOSE_LEVEL_IN_PAUSE`, `_YOUR_TIME_IS` and `_SLOW_MOTION` are unused in code.
- `ConfigConstants.mm` is an empty `@implementation`.

---

## 18. Top quirks to decide on (summary)
1. **Frame-rate-dependent gameplay:** player top speed (impulse per frame + damping), bonus duration (800 frames ≈ 13.3 s), bonus type cycling (100 frames), camera smoothing (0.8 per frame) and the variable physics step. Convert to seconds at 60 fps.
2. **Mixed clocks:** fire rate, Slow Motion (10 s) and recharge (5 s) use real-time NSTimers, while physics and movement use scaled time (0.35 in slow motion). The fire-rate multipliers (×2.4, /2.4) only roughly compensate.
3. **Vitamins are credited on spawn, not on pickup.** Bonus expiry bug: Life/BOOM taken during a weapon bonus makes the weapon bonus permanent. A new bonus inherits the remaining time. Type 7 reads past the colour array.
4. **Collision model:** there are no filters. All bullets share `kBullet` (bullets cancel each other, any bullet hurts the player, non-sensor bullets knock the player back). Enemies are sensors and damage only on contact begin.
5. **Controls:** 3 hard-coded schemes, character-based key matching (breaks with Caps Lock/AZERTY), no rebinding, no gamepad (the DDHidLib stubs are empty), mouse-only menus. The camera is zoomed out by about 0.58 and never clamps (inferred; verify).
