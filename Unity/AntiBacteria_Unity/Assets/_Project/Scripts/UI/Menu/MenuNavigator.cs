using AntiBacteria.Audio;
using System.Collections.Generic;
using AntiBacteria.Core;
using AntiBacteria.Input;
using UnityEngine;

namespace AntiBacteria.UI.Menu
{
    /// <summary>
    /// Drives a stack of menu screens with keyboard, gamepad (Rewired UI actions) and mouse.
    /// Up/down moves, left/right changes values (with key repeat), Confirm submits, Back / Esc / B goes back.
    /// Works while the game is paused (unscaled time).
    /// </summary>
    public class MenuNavigator : MonoBehaviour
    {
        private const float RepeatDelay = 0.35f;
        private const float RepeatRate = 0.09f;

        private readonly List<MenuScreen> stack = new List<MenuScreen>();
        private int openedFrame = -1;
        private int heldV, heldH;
        private float timerV, timerH;
        private Vector2 lastMouse;

        public MenuScreen Top => stack.Count > 0 ? stack[stack.Count - 1] : null;
        public bool IsOpen => stack.Count > 0;
        /// <summary>Screens open, the current one included.</summary>
        public int Depth => stack.Count;

        private void OnEnable()
        {
            Loc.Changed += RefreshTop;
            GameSettings.Changed += RefreshTop;
            lastMouse = GameInput.MouseScreenPosition;
        }

        private void OnDisable()
        {
            Loc.Changed -= RefreshTop;
            GameSettings.Changed -= RefreshTop;
        }

        /// <summary>Close everything and open this screen.</summary>
        public void Show(MenuScreen screen)
        {
            Clear();
            Push(screen);
        }

        public void Push(MenuScreen screen)
        {
            if (Top != null) Top.gameObject.SetActive(false);
            stack.Add(screen);
            Open(screen);
        }

        public void Pop()
        {
            if (stack.Count == 0) return;
            Top.gameObject.SetActive(false);
            stack.RemoveAt(stack.Count - 1);
            if (Top != null) Open(Top);
        }

        public void Clear()
        {
            foreach (MenuScreen s in stack) if (s != null) s.gameObject.SetActive(false);
            stack.Clear();
        }

        public void Back()
        {
            MenuScreen top = Top;
            if (top == null || !top.CanGoBack) return;
            Sound.Play(Sfx.UiBack);
            if (top.OnBack != null) top.OnBack();
            else if (stack.Count > 1) Pop();
        }

        private void Open(MenuScreen screen)
        {
            screen.gameObject.SetActive(true);
            screen.Shown();
            openedFrame = Time.frameCount; // the key that opened it must not also act on it
            heldV = heldH = 0;
        }

        private void RefreshTop()
        {
            if (Top != null) Top.RefreshAll();
        }

        private void Update()
        {
            MenuScreen top = Top;
            if (top == null || Time.frameCount == openedFrame) return;
            // Controls screen listening for a new key: that key is not a menu press (nor the frame after).
            if (Remapping.IsListening || Time.frameCount <= Remapping.QuietUntilFrame) return;

            // Keyboard / gamepad
            Vector2 nav = GameInput.UINavigation;
            int v = nav.y > 0.5f ? 1 : nav.y < -0.5f ? -1 : 0;
            int h = nav.x > 0.5f ? 1 : nav.x < -0.5f ? -1 : 0;
            if (Repeat(v, ref heldV, ref timerV))
            {
                MenuItem before = top.SelectedItem;
                top.Move(-v); // up = previous row
                if (top.SelectedItem != before) Sound.Play(Sfx.UiMove);
            }
            if (Repeat(h, ref heldH, ref timerH) && top.SelectedItem != null && top.SelectedItem.Step(h)) Sound.Play(Sfx.UiMove);

            if (GameInput.UISubmitPressed)
            {
                if (top.SelectedItem != null && top.SelectedItem.Interactable) Sound.Play(Sfx.UiConfirm);
                top.SelectedItem?.Submit();
                return;
            }
            if (GameInput.UICancelPressed || GameInput.PausePressed)
            {
                Back();
                return;
            }

            // Mouse: hover selects, click activates.
            Vector2 mouse = GameInput.MouseScreenPosition;
            bool moved = (mouse - lastMouse).sqrMagnitude > 0.25f;
            lastMouse = mouse;
            bool click = GameInput.MouseLeftPressed;
            if (!moved && !click) return;
            MenuItem item = top.ItemAt(mouse);
            if (item == null || !item.Interactable) return;
            if (item != top.SelectedItem) Sound.Play(Sfx.UiMove);
            top.Select(item);
            item.Hover(mouse);
            if (click) { Sound.Play(Sfx.UiConfirm); item.Click(mouse); }
        }

        private static bool Repeat(int dir, ref int held, ref float timer)
        {
            if (dir == 0) { held = 0; return false; }
            if (dir != held)
            {
                held = dir;
                timer = RepeatDelay;
                return true;
            }
            timer -= Time.unscaledDeltaTime;
            if (timer > 0f) return false;
            timer = RepeatRate;
            return true;
        }
    }
}
