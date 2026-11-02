using System.Collections.Generic;
using AntiBacteria.Core;
using AntiBacteria.Input;
using AntiBacteria.Rendering;
using AntiBacteria.UI.Menu;
using Rewired;
using UnityEngine;

namespace AntiBacteria.UI.Menus
{
    /// <summary>
    /// Options > Controls: remap the gameplay keys and gamepad buttons (Rewired InputMapper, see Remapping).
    /// Pick the device, then a row and press the new key or button. Reset puts that device back to the defaults.
    /// Mouse aim / fire and the sticks are fixed; menus keep their own keys so they can never be lost.
    /// </summary>
    public static class ControlsMenu
    {
        private struct Bind
        {
            public WordIDs word;
            public string action;
            public Pole pole;
            public bool axis;
            public Bind(WordIDs w, string a, Pole p = Pole.Positive, bool axis = false) { word = w; action = a; pole = p; this.axis = axis; }
        }

        private static readonly Bind[] KeyboardBinds =
        {
            new Bind(WordIDs.MoveUp, InputActions.MoveVertical, Pole.Positive, true),
            new Bind(WordIDs.MoveDown, InputActions.MoveVertical, Pole.Negative, true),
            new Bind(WordIDs.MoveLeft, InputActions.MoveHorizontal, Pole.Negative, true),
            new Bind(WordIDs.MoveRight, InputActions.MoveHorizontal, Pole.Positive, true),
            new Bind(WordIDs.Fire, InputActions.Fire),
            new Bind(WordIDs.SlowMotion, InputActions.SlowMotion),
            new Bind(WordIDs.Pause, InputActions.Pause),
        };

        private static readonly Bind[] GamepadBinds =
        {
            new Bind(WordIDs.Fire, InputActions.Fire),
            new Bind(WordIDs.SlowMotion, InputActions.SlowMotion),
            new Bind(WordIDs.Pause, InputActions.Pause),
        };

        private static BindDevice device = BindDevice.Keyboard;

        /// <param name="opaque">Cover the paused game (opened from the pause menu).</param>
        public static MenuScreen Build(UISkin skin, Transform parent, MenuNavigator nav, bool opaque)
        {
            MenuScreen s = MenuScreen.Create(parent, "Controls");
            Transform t = s.transform;
            if (opaque) UIFactory.Image(t, "Backing", skin.pixel, -400f, -300f, 1120f, 780f, PaletteRole.Background);
            LiveText.Create(skin, t, "Title", 160f, 10f, () => Loc.Get(WordIDs.Controls).ToUpperInvariant(), role: PaletteRole.Accent);

            var keyboardRows = new List<BindingRow>();
            var padRows = new List<BindingRow>();
            BindingRow listeningRow = null;

            void ShowDevice()
            {
                foreach (BindingRow r in keyboardRows) r.gameObject.SetActive(device == BindDevice.Keyboard);
                bool pad = Remapping.HasGamepad;
                foreach (BindingRow r in padRows)
                {
                    r.gameObject.SetActive(device == BindDevice.Gamepad);
                    r.Interactable = pad;
                }
                s.RefreshAll();
            }

            s.Add(MenuOption.Choice(skin, t, "Device", 28f, () => Loc.Get(WordIDs.ControlType),
                () => Loc.Get(device == BindDevice.Keyboard ? WordIDs.Keyboard : WordIDs.Gamepad),
                _ => { device = device == BindDevice.Keyboard ? BindDevice.Gamepad : BindDevice.Keyboard; ShowDevice(); }));

            void AddRows(Bind[] binds, BindDevice dev, List<BindingRow> rows)
            {
                for (int i = 0; i < binds.Length; i++)
                {
                    Bind b = binds[i];
                    BindingRow row = null;
                    row = BindingRow.Create(skin, t, dev + b.action + b.pole, 46f + i * 13f, () => Loc.Get(b.word),
                        () =>
                        {
                            if (listeningRow == row && Remapping.IsListening)
                                return Loc.Get(dev == BindDevice.Keyboard ? WordIDs.PressKey : WordIDs.PressButton) + " " +
                                       Mathf.CeilToInt(Remapping.SecondsLeft);
                            if (dev == BindDevice.Gamepad && !Remapping.HasGamepad) return Loc.Get(WordIDs.NoGamepad);
                            return Remapping.BindingName(dev, b.action, b.pole, b.axis) ?? "-";
                        },
                        () => listeningRow == row && Remapping.IsListening,
                        () =>
                        {
                            listeningRow = row;
                            Remapping.Listen(dev, b.action, b.pole, b.axis, ok =>
                            {
                                listeningRow = null;
                                s.RefreshAll(); // a conflict may have moved another binding
                            });
                            row.Refresh();
                        });
                    rows.Add(row);
                    s.Add(row);
                }
            }
            AddRows(KeyboardBinds, BindDevice.Keyboard, keyboardRows);
            AddRows(GamepadBinds, BindDevice.Gamepad, padRows);

            s.Add(MenuButton.Create(skin, t, "Reset", 160f, 138f, () => Loc.Get(WordIDs.ResetDefaults), () =>
            {
                Remapping.ResetDefaults(device);
                s.RefreshAll();
            }));
            s.Add(MenuButton.Create(skin, t, "Back", 160f, 152f, () => Loc.Get(WordIDs.Back), nav.Back));

            s.OnShown = () =>
            {
                if (Remapping.HasGamepad && GameInput.UsingGamepad) device = BindDevice.Gamepad; // open on the device in hand
                ShowDevice();
            };
            s.OnBack = () => { Remapping.Cancel(); nav.Pop(); };
            ShowDevice();
            return s;
        }
    }
}
