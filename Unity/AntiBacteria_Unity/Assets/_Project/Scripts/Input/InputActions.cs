namespace AntiBacteria.Input
{
    /// <summary>
    /// Rewired action names. The Rewired Input Manager is configured from these by
    /// AntiBacteria > Input > Configure Rewired Input Manager (RewiredSetup.cs), so code and data match.
    /// </summary>
    public static class InputActions
    {
        public const string PlayerName = "Player0";

        // Gameplay category / map category
        public const string GameplayCategory = "Gameplay";
        public const string MoveHorizontal = "MoveHorizontal";
        public const string MoveVertical = "MoveVertical";
        public const string AimHorizontal = "AimHorizontal";
        public const string AimVertical = "AimVertical";
        public const string Fire = "Fire";
        public const string SlowMotion = "SlowMotion";
        public const string Pause = "Pause";

        // UI category / map category - names expected by Rewired's UI input module
        public const string UICategory = "UI";
        public const string UIHorizontal = "UIHorizontal";
        public const string UIVertical = "UIVertical";
        public const string UISubmit = "UISubmit";
        public const string UICancel = "UICancel";
    }
}
