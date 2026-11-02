# Steam setup

The game talks to stores only through `PlatformServices` (`Scripts/Platform`). With no store running, every call does nothing. Settings live in **Project Settings → AntiBacteria → Platform (Steam)**.

## 1. Turn Steam on
1. Package Manager → **+** → *Add package from git URL*:
   `https://github.com/rlabrecque/Steamworks.NET.git?path=/com.rlabrecque.steamworks.net#2025.164.1`
   The `AntiBacteria.Platform` assembly detects the package, defines `STEAMWORKS_NET`, and compiles `SteamBackend`. No other change is needed.
2. On the settings page, click **Write steam_appid.txt**. It defaults to 480 (Valve's test app). With Steam running, Play in the editor should log `Steam: signed in as …`.
3. When the real App ID exists, set it on the page, write the file again, and turn on **Restart through Steam** for release builds.

## 2. Steamworks partner site
**Stats & Achievements:** create one achievement per API name below. The names can be changed on the settings page.

| API name | Unlocked by |
|---|---|
| ACH_BOSS_10 / 20 / 30 / 40 | Beating the boss of level 10 / 20 / 30 / 40 |
| ACH_FINISH | Beating level 50 |
| ACH_HARD | Clearing any level on Hard |
| ACH_IMPOSSIBLE | Clearing any level on Impossible |
| ACH_FIRST_UPGRADE | Buying an upgrade |
| ACH_ALL_UPGRADES | Owning every upgrade |
| ACH_VITAMINS | Holding 1000 vitamins (threshold on the page) |
| ACH_FLAGS | 25 flags in one Capture the Flag run |
| ACH_ZONES | 10 zones in one Rush run |
| ACH_PEACE | 10 flags in one Peace Walker run |

**Leaderboards** (created by the game on first use, sorted high to low, keep best):

| Leaderboard | Score submitted |
|---|---|
| Campaign | Sum of the best scores of every level on every difficulty |
| Capture the Flag | Run score |
| Rush | Run score |
| Peace Walker | Run score |

**Steam Cloud:** give the app a byte quota, for example 1 MB and 10 files. The game writes `save.json` through ISteamRemoteStorage. When the game starts, the newer of the cloud and local copies wins. Don't also set up Auto-Cloud for the same file.

## 3. Behaviour
- The Steam overlay (Shift+Tab) pauses a running level.
- Consoles: add a backend that implements `IPlatformBackend` (Platform/PlatformServices.cs) and create it in `PlatformServices.Init` under that platform's define.
