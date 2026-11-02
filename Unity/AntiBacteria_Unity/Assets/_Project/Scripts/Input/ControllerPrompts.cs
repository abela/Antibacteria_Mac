using Rewired;

namespace AntiBacteria.Input
{
    /// <summary>
    /// Which button glyphs to show: the family of the device the player touched last (keyboard and mouse count as
    /// keyboard), recognised from the controller's name; console builds always use their own family.
    /// Also decides whether a Nintendo controller confirms with its right button (A), as players there expect.
    /// </summary>
    public static class ControllerPrompts
    {
        private static int cachedFrame = -1;
        private static PromptFamily cached;

        /// <summary>The family of the last device used.</summary>
        public static PromptFamily Detected
        {
            get
            {
                int frame = UnityEngine.Time.frameCount;
                if (frame == cachedFrame) return cached;
                cachedFrame = frame;
                cached = Detect(GameInput.LastActiveController);
                return cached;
            }
        }

        /// <summary>The glyphs to draw: Detected, or the family picked in PromptSettings.preview.</summary>
        public static PromptFamily Shown
        {
            get
            {
                PromptSettings.Force f = PromptSettings.Current.preview;
                return f == PromptSettings.Force.Auto ? Detected : (PromptFamily)((int)f - 1);
            }
        }

        /// <summary>Confirm / Back are on the right / bottom buttons (Nintendo controller, setting on).</summary>
        public static bool NintendoLayout => Detected == PromptFamily.Switch && PromptSettings.Current.nintendoConfirmOnRight;

        private static PromptFamily Detect(Controller c)
        {
            if (c == null || c.type != ControllerType.Joystick) return PromptFamily.Keyboard;
#if UNITY_PS4 || UNITY_PS5
            return PromptFamily.PlayStation;
#elif UNITY_SWITCH
            return PromptFamily.Switch;
#elif UNITY_GAMECORE || UNITY_XBOXONE
            return PromptFamily.Xbox;
#else
            string n = ((c.hardwareName ?? "") + " " + (c.name ?? "")).ToLowerInvariant();
            if (n.Contains("dualsense") || n.Contains("dualshock") || n.Contains("playstation") || n.Contains("ps4") ||
                n.Contains("ps5") || n.Contains("sony") || n.Contains("wireless controller"))
                return PromptFamily.PlayStation;
            if (n.Contains("nintendo") || n.Contains("switch") || n.Contains("pro controller") || n.Contains("joy-con") || n.Contains("joycon"))
                return PromptFamily.Switch;
            return PromptFamily.Xbox; // XInput and unknown pads use the Xbox layout
#endif
        }
    }
}
