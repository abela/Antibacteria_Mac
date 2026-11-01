namespace AntiBacteria.Core
{
    /// <summary>
    /// Converts the 2013 game's units (cocos2d points, Box2D metres) into Unity units.
    /// Docs/DesignReference documents every original value in points or metres; convert them here
    /// so the original numbers stay visible in the code and the scale can be tuned in one place.
    /// </summary>
    public static class Legacy
    {
        /// <summary>Pixels per Unity unit (matches the sprite import and the Pixel Perfect Camera).</summary>
        public const float PixelsPerUnit = 16f;

        /// <summary>
        /// Screen pixels per original point. The pixel-art sprites are larger relative to the screen
        /// than the 2013 sprites, so world distances and speeds are scaled to keep the arena about
        /// 1.35 x 1.7 screens (Docs/ArtDirection/StyleGuide.md). Tune feel here.
        /// </summary>
        public const float PixelsPerPoint = 0.2f;

        /// <summary>Box2D in the original: 32 points per metre (Physics.h PTM_RATIO).</summary>
        public const float PointsPerMetre = 32f;

        /// <summary>The original counted many timers as 0.01 per rendered frame at 60 fps:
        /// one "unit" of those counters is 1/0.6 seconds.</summary>
        public const float SecondsPerFrameUnit = 1f / 0.6f;

        public static float Units(float points) => points * PixelsPerPoint / PixelsPerUnit;

        public static float UnitsFromMetres(float metres) => Units(metres * PointsPerMetre);
    }
}
