# Steam setup

The game talks to stores only through `PlatformServices` (`Scripts/Platform`). With no store running, every call does nothing. Settings live in **Project Settings → AntiBacteria → Platform (Steam)**.

## 1. Turn Steam on
1. Package Manager → **+** → *Add package from git URL*:
   `https://github.com/rlabrecque/Steamworks.NET.git?path=/com.rlabrecque.steamworks.net#2025.165.0`
   The `AntiBacteria.Platform` assembly detects the package, defines `STEAMWORKS_NET`, and compiles `SteamBackend`. No other change is needed.
2. The App ID is **5554670** (set on the settings page, written to `steam_appid.txt` in the project root). With Steam running, Play in the editor logs `Steam: signed in as …, app 5554670, cloud on/on`.
3. **Restart through Steam** is on. It only runs in release player builds (not the editor or development builds), so a release build started outside Steam relaunches through Steam.
4. At start-up the game calls `RequestUserStats`, so the newest published achievement list is loaded before anything unlocks.

## 2. Steamworks partner site
All of the below is set up and **published** (App Admin → Publish) for app 5554670. Any later change on the partner site needs another Prepare → Publish before players (and the editor) see it.

**Achievements** (20, edited at `partner.steamgames.com/apps/achievementsbeta/5554670`, icons auto-generated in the game's palette). The API names can be changed on the settings page.

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
| ACH_ROGUE_10 / 25 | Clearing room 10 / 25 in Roguelike |
| ACH_ROGUE_50 | Clearing all 50 rooms in Roguelike |
| ACH_ROGUE_IMPOSSIBLE | Clearing room 20 in Roguelike on Impossible (room on the page) |
| ACH_BUDDY | Taking the Germ Buddy perk |
| ACH_LEGENDARY | Taking a Legendary perk |
| ACH_FOUR_LINES | Reaching 4 bullet lines (Extra Bullet ×3) |

**Leaderboards** (all five already exist on the partner site; the game would also create them on first use. Sorted high to low, keep best):

| Leaderboard | Score submitted |
|---|---|
| Campaign | Sum of the best scores of every level on every difficulty |
| Capture the Flag | Run score |
| Rush | Run score |
| Peace Walker | Run score |
| Roguelike | Run score |

**In-game leaderboards:** Main menu → Leaderboards (shown only when Steam is running). Left/right picks the board; each shows the global top 20 in two columns of ten, the player's own line in the accent colour, and "You #rank score" underneath even when they're outside the top 20. A board is downloaded when first shown and again after 30 s. Names arrive from Steam a moment after the scores (shown as "..." until then); letters the pixel font lacks show as "?". Code: `UI/Menus/LeaderboardMenu.cs`, `PlatformServices.FetchTop`. For checking the layout with sample data in the editor, set `LeaderboardMenu.EditorPreview` / `EditorNames` (editor-only).

**Steam Cloud:** quota is 10 MB / 20 files, with Mac, Windows and Linux enabled. The game writes `save.json` through ISteamRemoteStorage (API cloud, so it works the same on all three). When the game starts, the newer of the cloud and local copies wins. Don't also set up Auto-Cloud for the same file.
- If `SteamRemoteStorage.GetQuota` returns false / 0 bytes right after a cloud change is published, the Steam client hasn't picked up the new settings yet: quit and reopen Steam. Then quit and reopen Unity too: the editor keeps its first connection to Steam, and after a Steam restart that old connection reports cloud off and 0 achievements.
- The local save is always written first, so a cloud failure never loses progress.

**Store page:** under *Supported Features*, tick **Steam Achievements**, **Steam Cloud** and **Steam Leaderboards**.

**Testing:** *AntiBacteria → Progress Tool → Reset My Steam Achievements…* clears your own unlocks (needs Play mode with Steam running).

## 3. Behaviour
- The Steam overlay (Shift+Tab) pauses a running level.
- Consoles: add a backend that implements `IPlatformBackend` (Platform/PlatformServices.cs) and create it in `PlatformServices.Init` under that platform's define.
