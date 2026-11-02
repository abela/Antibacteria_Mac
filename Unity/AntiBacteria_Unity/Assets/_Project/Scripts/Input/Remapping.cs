using System;
using Rewired;
using UnityEngine;

namespace AntiBacteria.Input
{
    public enum BindDevice { Keyboard, Gamepad }

    /// <summary>
    /// Player-made control bindings through Rewired's InputMapper: read the current binding of an action, listen
    /// for a new key / button, reset to defaults. Only the Gameplay map is remapped (menus keep their fixed keys,
    /// so a bad binding can never lock the player out). Saved with Rewired's UserDataStore (PlayerPrefs).
    /// Esc cancels listening on the keyboard; otherwise it times out.
    /// </summary>
    public static class Remapping
    {
        public const float TimeoutSeconds = 5f;
        private const string Layout = "Default";

        private static InputMapper mapper;
        private static Action<bool> onDone;
        private static bool mapped;
        private static Pending pending;
        private static float listenStart;

        private class Pending
        {
            public BindDevice device;
            public string action;
            public Pole pole;
            public bool axis;
        }

        /// <summary>Waiting for the player to press something (menus ignore input meanwhile).</summary>
        public static bool IsListening => pending != null || (mapper != null && mapper.status != InputMapper.Status.Idle);

        /// <summary>Menus stay deaf until this frame, so the key just bound doesn't also press a menu button.</summary>
        public static int QuietUntilFrame { get; private set; } = -1;

        public static float SecondsLeft => IsListening ? Mathf.Max(0f, TimeoutSeconds - (Time.unscaledTime - listenStart)) : 0f;


        private static Player Player => ReInput.isReady ? ReInput.players.GetPlayer(InputActions.PlayerName) : null;

        public static bool HasGamepad => Player != null && Player.controllers.joystickCount > 0;

        private static ControllerMap GameplayMap(BindDevice device)
        {
            Player p = Player;
            if (p == null) return null;
            if (device == BindDevice.Keyboard)
                return p.controllers.maps.GetMap(ControllerType.Keyboard, 0, InputActions.GameplayCategory, Layout);
            if (p.controllers.joystickCount == 0) return null;
            return p.controllers.maps.GetMap(ControllerType.Joystick, p.controllers.Joysticks[0].id, InputActions.GameplayCategory, Layout);
        }

        private static ActionElementMap Find(ControllerMap map, int actionId, Pole pole, bool axis)
        {
            if (map == null) return null;
            foreach (ActionElementMap aem in map.ElementMapsWithAction(actionId))
            {
                // Axis actions bound to keys or buttons have a side (W = MoveVertical +, S = MoveVertical -).
                if (axis && aem.elementType == ControllerElementType.Button && aem.axisContribution != pole) continue;
                return aem; // the first binding is the one the player edits
            }
            return null;
        }

        /// <summary>The key or button bound to an action (one side of it for axes), or null.</summary>
        public static string BindingName(BindDevice device, string action, Pole pole = Pole.Positive, bool axis = false)
        {
            InputAction a = ReInput.isReady ? ReInput.mapping.GetAction(action) : null;
            if (a == null) return null;
            ActionElementMap aem = Find(GameplayMap(device), a.id, pole, axis);
            return aem != null ? aem.elementIdentifierName : null;
        }

        /// <summary>Wait for a new key / button for an action. done(true) when bound, done(false) when cancelled or timed out.</summary>
        public static void Listen(BindDevice device, string action, Pole pole, bool axis, Action<bool> done)
        {
            Cancel();
            onDone = done;
            mapped = false;
            pending = new Pending { device = device, action = action, pole = pole, axis = axis };
            listenStart = Time.unscaledTime;
            Host.Ensure(); // starts the mapper once every key / button from the menu press is released
        }

        public static void Cancel()
        {
            if (pending != null) { pending = null; Finish(false); return; }
            if (mapper != null && mapper.status != InputMapper.Status.Idle) mapper.Stop();
        }

        public static void ResetDefaults(BindDevice device)
        {
            Player p = Player;
            if (p == null) return;
            p.controllers.maps.LoadDefaultMaps(device == BindDevice.Keyboard ? ControllerType.Keyboard : ControllerType.Joystick);
            Save();
        }

        private static void Save() => ReInput.userDataStore?.Save();

        private static void StartMapper()
        {
            Pending p = pending;
            pending = null;
            InputAction a = ReInput.mapping.GetAction(p.action);
            ControllerMap map = GameplayMap(p.device);
            if (a == null || map == null) { Finish(false); return; }

            if (mapper == null)
            {
                mapper = new InputMapper();
                mapper.InputMappedEvent += _ => mapped = true;
                mapper.StoppedEvent += _ => Finish(mapped);
            }
            InputMapper.Options o = mapper.options;
            o.timeout = Mathf.Max(0.5f, TimeoutSeconds - (Time.unscaledTime - listenStart));
            o.ignoreMouseXAxis = true;
            o.ignoreMouseYAxis = true;
            o.allowAxes = p.device == BindDevice.Gamepad; // triggers can fire / slow down
            o.allowButtonsOnFullAxisAssignment = true;
            o.checkForConflicts = true;
            o.defaultActionWhenConflictFound = InputMapper.ConflictResponse.Replace; // a key does one thing
            // Esc is the way out on the keyboard; Start stays Pause on a pad so the game can always be paused.
            o.isElementAllowedCallback = info =>
                !(info.controllerType == ControllerType.Keyboard && info.keyboardKey == KeyboardKeyCode.Escape);

            mapper.Start(new InputMapper.Context
            {
                actionId = a.id,
                controllerMap = map,
                actionRange = p.axis ? (p.pole == Pole.Positive ? AxisRange.Positive : AxisRange.Negative) : AxisRange.Positive,
                actionElementMapToReplace = Find(map, a.id, p.pole, p.axis),
            });
        }

        private static void Finish(bool ok)
        {
            if (ok) Save();
            QuietUntilFrame = Time.frameCount + 2;
            Action<bool> d = onDone;
            onDone = null;
            d?.Invoke(ok);
        }

        /// <summary>Per-frame work: delayed start, Esc to cancel, timeout before the mapper starts.</summary>
        private class Host : MonoBehaviour
        {
            private static Host instance;

            public static void Ensure()
            {
                if (instance != null) return;
                var go = new GameObject("Remapping") { hideFlags = HideFlags.HideAndDontSave };
                DontDestroyOnLoad(go);
                instance = go.AddComponent<Host>();
            }

            private void Update()
            {
                if (!IsListening || !ReInput.isReady) return;
                if (ReInput.controllers.Keyboard != null && ReInput.controllers.Keyboard.GetKeyDown(KeyboardKeyCode.Escape)) { Cancel(); return; }
                if (pending == null) return;
                if (Time.unscaledTime - listenStart > TimeoutSeconds) { Cancel(); return; }
                // Wait until the button that opened the prompt is up, or it would bind itself.
                if (ReInput.controllers.GetAnyButton()) return;
                StartMapper();
            }
        }
    }
}
