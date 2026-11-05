using Rewired;
using UnityEngine;

namespace AntiBacteria.Input
{
    /// <summary>
    /// The only place gameplay code reads controls from. Wraps Rewired's Player0 so the rest of the
    /// game never deals with controllers, keys or Rewired directly.
    /// </summary>
    public static class GameInput
    {
        /// <summary>Stick input below this is ignored for aiming (keeps the last aim direction).</summary>
        public const float AimDeadZone = 0.35f;

        private static Player cachedPlayer;

        private static Player Player
        {
            get
            {
                if (!ReInput.isReady) return null;
                if (cachedPlayer == null) cachedPlayer = ReInput.players.GetPlayer(InputActions.PlayerName);
                return cachedPlayer;
            }
        }

        public static bool IsReady => Player != null;

        /// <summary>Movement, length 0..1 (keyboard diagonals are normalised).</summary>
        public static Vector2 Move
        {
            get
            {
                Player p = Player;
                if (p == null) return Vector2.zero;
#if UNITY_EDITOR
                if (EditorMoveOverride is Vector2 m) return Vector2.ClampMagnitude(m, 1f);
#endif
                var v = new Vector2(p.GetAxis(InputActions.MoveHorizontal), p.GetAxis(InputActions.MoveVertical));
                return Vector2.ClampMagnitude(v, 1f);
            }
        }

        /// <summary>Right-stick aim, raw (0 when centred). Mouse aim uses MouseScreenPosition instead.</summary>
        public static Vector2 AimStick
        {
            get
            {
                Player p = Player;
                if (p == null) return Vector2.zero;
                return new Vector2(p.GetAxis(InputActions.AimHorizontal), p.GetAxis(InputActions.AimVertical));
            }
        }

        public static bool FireHeld =>
#if UNITY_EDITOR
            EditorFireOverride ||
#endif
            (Player != null && Player.GetButton(InputActions.Fire));

        /// <summary>Confirm, Back, Fire, Pause or the mouse held (hold to skip the intro).</summary>
        public static bool AnyButtonHeld =>
            (Player != null && (Player.GetButton(InputActions.UISubmit) || Player.GetButton(InputActions.UICancel) ||
                                Player.GetButton(InputActions.Fire) || Player.GetButton(InputActions.Pause))) ||
            (ReInput.isReady && ReInput.controllers.Mouse != null && ReInput.controllers.Mouse.GetButton(0));
        public static bool SlowMotionPressed => Player != null && Player.GetButtonDown(InputActions.SlowMotion);
        public static bool PausePressed => Player != null && Player.GetButtonDown(InputActions.Pause);

        /// <summary>True when the last device the player touched was a gamepad (aim with the stick, hide the cursor).</summary>
        public static bool UsingGamepad
        {
            get
            {
                Player p = Player;
                if (p == null) return false;
                Controller c = p.controllers.GetLastActiveController();
                return c != null && c.type == ControllerType.Joystick;
            }
        }

        /// <summary>Menu navigation from arrows / WASD / left stick / d-pad, each axis -1..1.</summary>
        public static Vector2 UINavigation
        {
            get
            {
                Player p = Player;
                if (p == null) return Vector2.zero;
                return new Vector2(p.GetAxis(InputActions.UIHorizontal), p.GetAxis(InputActions.UIVertical));
            }
        }

        /// <summary>
        /// Confirm / Back. On a Nintendo controller they swap (ControllerPrompts.NintendoLayout): A, the right button,
        /// confirms and B, the bottom one, goes back, as Switch players expect.
        /// </summary>
        public static bool UISubmitPressed =>
            Player != null && Player.GetButtonDown(ControllerPrompts.NintendoLayout ? InputActions.UICancel : InputActions.UISubmit);
        public static bool UICancelPressed =>
            Player != null && Player.GetButtonDown(ControllerPrompts.NintendoLayout ? InputActions.UISubmit : InputActions.UICancel);

        /// <summary>The device used last (keyboard, mouse or a joystick), for the button prompts.</summary>
        public static Controller LastActiveController => Player?.controllers.GetLastActiveController();

        public static bool MouseLeftHeld =>
            ReInput.isReady && ReInput.controllers.Mouse != null && ReInput.controllers.Mouse.GetButton(0);

        public static bool MouseLeftPressed =>
            ReInput.isReady && ReInput.controllers.Mouse != null && ReInput.controllers.Mouse.GetButtonDown(0);

#if UNITY_EDITOR
        /// <summary>Editor only: pretend the mouse is here (screen pixels). Used by the screenshot director.</summary>
        public static Vector2? EditorMouseOverride;

        /// <summary>Editor only: replaces the movement input. Used by the screenshot director's auto-dodge.</summary>
        public static Vector2? EditorMoveOverride;

        /// <summary>Editor only: fire is held (balance bot).</summary>
        public static bool EditorFireOverride;
#endif

        /// <summary>Mouse position in screen pixels.</summary>
        public static Vector2 MouseScreenPosition
        {
            get
            {
#if UNITY_EDITOR
                if (EditorMouseOverride is Vector2 o) return o;
#endif
                return ReInput.isReady && ReInput.controllers.Mouse != null ? ReInput.controllers.Mouse.screenPosition : Vector2.zero;
            }
        }

        /// <summary>Called when scenes reload so a stale Player reference is never used.</summary>
        public static void Reset() => cachedPlayer = null;
    }
}
