using UnityEngine;

namespace AntiBacteria.Input
{
    public enum PromptFamily { Keyboard, Xbox, PlayStation, Switch }

    /// <summary>
    /// The button prompts at the bottom of every menu ("[A] Select  [B] Back"): which glyphs, and how a Nintendo
    /// controller confirms. Edit in Project Settings > AntiBacteria > Controller Prompts.
    /// </summary>
    [CreateAssetMenu(menuName = "AntiBacteria/Prompt Settings", fileName = "PromptSettings")]
    public class PromptSettings : ScriptableObject
    {
        public const string ResourcePath = "PromptSettings";

        public enum Force { Auto, Keyboard, Xbox, PlayStation, Switch }

        [Tooltip("Show the prompt bar on menu screens.")]
        public bool show = true;
        [Tooltip("Auto follows the device the player touched last. Pick a family to preview its glyphs in the editor.")]
        public Force preview = Force.Auto;
        [Tooltip("Nintendo controllers confirm with A (the right button) and go back with B (the bottom one), " +
                 "as on Switch. Off: same physical buttons as Xbox / PlayStation.")]
        public bool nintendoConfirmOnRight = true;

        [Header("Glyphs (filled in automatically from Art/Sprites/Glyphs)")]
        public Sprite xboxA, xboxB;
        public Sprite psCross, psCircle;
        public Sprite switchA, switchB;
        public Sprite padLeftRight;
        public Sprite keyEnter, keyEsc, keyLeftRight;

        /// <summary>Raised when a value is edited, so open menus redraw their prompts.</summary>
        public static event System.Action Changed;
        private void OnValidate() => Changed?.Invoke();

        private static PromptSettings current;

        public static PromptSettings Current
        {
            get
            {
                if (current != null) return current;
                current = Resources.Load<PromptSettings>(ResourcePath);
                if (current == null)
                {
                    current = CreateInstance<PromptSettings>();
                    current.hideFlags = HideFlags.HideAndDontSave;
                }
                return current;
            }
        }

        public static void Use(PromptSettings settings) => current = settings;
    }
}
