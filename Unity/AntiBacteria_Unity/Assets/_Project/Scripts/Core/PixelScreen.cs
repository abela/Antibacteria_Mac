using System;
using UnityEngine;

namespace AntiBacteria.Core
{
    /// <summary>
    /// One pixel grid for the whole screen. The camera shows ViewWidth x ViewHeight game pixels at the reference
    /// zoom, and the world and every UI canvas are scaled by the same whole number, so a game pixel is the same size
    /// everywhere (5x at 1920x1080 with a 384x216 view). On screens that aren't a whole multiple the zoom rounds down
    /// and the player sees a little more of the arena instead of blurry pixels.
    ///
    /// The size comes from ViewSettings (Project Settings > AntiBacteria > View). CameraFollow copies it into the
    /// Pixel Perfect Camera and PixelCanvasScaler uses it for every canvas; both follow changes live.
    /// </summary>
    public static class PixelScreen
    {
        public static int ViewWidth => ViewSettings.Current.ViewWidth;
        public static int ViewHeight => ViewSettings.Current.ViewHeight;

        /// <summary>Goes up every time the view size changes, so cached scales know to refresh.</summary>
        public static int Version { get; private set; }

        public static event Action Changed;

        public static int ScaleFor(int screenWidth, int screenHeight) =>
            Mathf.Max(1, Mathf.Min(screenWidth / ViewWidth, screenHeight / ViewHeight));

        public static int Scale => ScaleFor(Screen.width, Screen.height);

        internal static void NotifyChanged()
        {
            Version++;
            Changed?.Invoke();
        }
    }
}
