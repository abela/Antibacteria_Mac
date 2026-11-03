using System.Collections.Generic;
using System.Text;
using AntiBacteria.Core;
using AntiBacteria.Platform;
using AntiBacteria.Rendering;
using AntiBacteria.UI.Menu;
using TMPro;
using UnityEngine;

namespace AntiBacteria.UI.Menus
{
    /// <summary>
    /// Main menu > Leaderboards: the top 20 of each Steam leaderboard in two columns of ten, and the player's own
    /// rank underneath. Left/right switches the board. Each board is downloaded when it is first shown and again
    /// after RefreshSeconds.
    /// </summary>
    public static class LeaderboardMenu
    {
        public const int Count = 20;
        private const int PerColumn = 10;
        private const float RowsY = 22f, RowPitch = 12f;
        private const float RefreshSeconds = 30f;
        private static readonly float[] ColumnX = { 8f, 166f }; // left edge of each column; 146 px wide
        private const float RankW = 18f, ScoreRight = 146f, Gap = 6f;

        private static readonly Board[] Boards = { Board.Campaign, Board.Roguelike, Board.CaptureFlag, Board.Rush, Board.PeaceWalker };

#if UNITY_EDITOR
        /// <summary>Editor only: when set, every board shows this page (names from EditorNames) instead of Steam's. For checking the layout.</summary>
        public static ScorePage EditorPreview;
        public static Dictionary<ulong, string> EditorNames;
#endif

        private class Cached
        {
            public ScorePage page;
            public float time;
            public bool loading;
        }

        private class Row
        {
            public TextMeshProUGUI rank, name, score;
        }

        public static MenuScreen Build(UISkin skin, Transform parent, MenuNavigator nav)
        {
            MenuScreen s = MenuScreen.Create(parent, "Leaderboards");
            Transform t = s.transform;
            var cache = new Dictionary<Board, Cached>();
            int index = 0;

            var rows = new Row[Count];
            for (int i = 0; i < Count; i++)
            {
                float x = ColumnX[i / PerColumn], y = RowsY + (i % PerColumn) * RowPitch;
                rows[i] = new Row
                {
                    rank = UIFactory.Text(skin, t, "Rank" + i, x + RankW, y, HorizontalAlignmentOptions.Right, 1, PaletteRole.Foreground, true),
                    name = UIFactory.Text(skin, t, "Name" + i, x + RankW + 4f, y, HorizontalAlignmentOptions.Left),
                    score = UIFactory.Text(skin, t, "Score" + i, x + ScoreRight, y, HorizontalAlignmentOptions.Right),
                };
            }
            UnityEngine.UI.Image divider = UIFactory.Image(t, "Divider", skin.pixel, 159f, RowsY + 2f, 1f, PerColumn * RowPitch - 4f, PaletteRole.Foreground, true);
            TextMeshProUGUI status = UIFactory.Text(skin, t, "Status", 160f, RowsY + 4f * RowPitch, HorizontalAlignmentOptions.Center, 1, PaletteRole.Foreground, true);
            TextMeshProUGUI mine = UIFactory.Text(skin, t, "Mine", 160f, RowsY + PerColumn * RowPitch, HorizontalAlignmentOptions.Center, 1, PaletteRole.Accent);

            void Redraw()
            {
                if (!s) return;
                cache.TryGetValue(Boards[index], out Cached c);
                ScorePage page = c?.page;
#if UNITY_EDITOR
                if (EditorPreview != null) { page = EditorPreview; c = null; }
#endif
                string message =
                    page == null && !PlatformServices.LeaderboardsAvailable ? Loc.Get(WordIDs.LeaderboardOffline) :
                    page == null && (c == null || c.loading) ? Loc.Get(WordIDs.LeaderboardLoading) :
                    page == null ? Loc.Get(WordIDs.LeaderboardFailed) :
                    page.top.Count == 0 ? Loc.Get(WordIDs.LeaderboardEmpty) : "";
                status.text = message;
                divider.enabled = message.Length == 0;

                for (int i = 0; i < Count; i++)
                {
                    Row r = rows[i];
                    bool has = message.Length == 0 && i < page.top.Count;
                    if (!has) { r.rank.text = r.name.text = r.score.text = ""; continue; }
                    ScoreRow e = page.top[i];
                    r.rank.text = e.rank.ToString();
                    r.score.text = e.score.ToString();
                    float room = ScoreRight - RankW - 4f - UIFactory.Width(r.score, r.score.text) - Gap;
                    r.name.text = Fit(r.name, Clean(skin.font, Name(e.user)), room);
                    PaletteRole role = e.isMe ? PaletteRole.Accent : PaletteRole.Foreground;
                    r.name.GetComponent<PaletteGraphic>().Set(role, false);
                    r.score.GetComponent<PaletteGraphic>().Set(role, false);
                }

                mine.text = page == null ? "" :
                    page.me is ScoreRow me ? Loc.Get(WordIDs.LeaderboardYou) + "  #" + me.rank + "  " + me.score :
                    Loc.Get(WordIDs.LeaderboardUnranked);
            }

            void Fetch(bool force)
            {
                Board b = Boards[index];
                if (!PlatformServices.LeaderboardsAvailable) { Redraw(); return; }
                if (!cache.TryGetValue(b, out Cached c)) cache[b] = c = new Cached();
                bool fresh = c.page != null && Time.unscaledTime - c.time < RefreshSeconds;
                if (c.loading || (fresh && !force)) { Redraw(); return; }
                c.loading = true;
                Redraw();
                PlatformServices.FetchTop(b, Count, page =>
                {
                    c.loading = false;
                    if (page != null || c.page == null) c.page = page; // a failed refresh keeps the last good list
                    c.time = Time.unscaledTime;
                    if (s && b == Boards[index]) Redraw();
                });
            }

            // Steam sends player names after the scores: redraw as they arrive.
            System.Action names = null;
            names = () =>
            {
                if (!s) { PlatformServices.NamesChanged -= names; return; }
                if (s.gameObject.activeInHierarchy) Redraw();
            };
            PlatformServices.NamesChanged += names;

            // The header is the board picker: "LEADERBOARDS   < Campaign >".
            s.Add(MenuOption.Choice(skin, t, "Board", 4f, () => Loc.Get(WordIDs.Leaderboards).ToUpperInvariant(),
                () => BoardName(Boards[index]), dir =>
                {
                    index = (index + dir + Boards.Length) % Boards.Length;
                    Fetch(false);
                }));
            s.Add(MenuButton.Create(skin, t, "Back", 160f, 154f, () => Loc.Get(WordIDs.Back), nav.Back));
            s.OnShown = () => Fetch(false);
            return s;
        }

        private static string BoardName(Board b) => Loc.Get((WordIDs)System.Enum.Parse(typeof(WordIDs), b.ToString()));

        private static string Name(ulong user)
        {
#if UNITY_EDITOR
            if (EditorPreview != null && EditorNames != null && EditorNames.TryGetValue(user, out string preview)) return preview;
#endif
            string n = PlatformServices.PlayerName(user);
            return string.IsNullOrEmpty(n) || n == "[unknown]" ? "..." : n;
        }

        /// <summary>Letters the pixel font doesn't have become '?' (Steam names can be in any script).</summary>
        private static string Clean(TMP_FontAsset font, string s)
        {
            var sb = new StringBuilder(s.Length);
            foreach (char ch in s) sb.Append(ch == ' ' || font.HasCharacter(ch, true) ? ch : '?');
            return sb.ToString().Trim();
        }

        private static string Fit(TextMeshProUGUI t, string s, float max)
        {
            if (UIFactory.Width(t, s) <= max) return s;
            while (s.Length > 1 && UIFactory.Width(t, s + "..") > max) s = s.Substring(0, s.Length - 1);
            return s.TrimEnd() + "..";
        }
    }
}
