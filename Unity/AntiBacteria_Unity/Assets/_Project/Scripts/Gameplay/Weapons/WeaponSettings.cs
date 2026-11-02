using System;
using System.Collections.Generic;
using UnityEngine;

namespace AntiBacteria.Gameplay.Weapons
{
    /// <summary>The player's shot types (Docs/DesignReference/01, section 6).</summary>
    public enum ShotKind { Normal, Water, Windmill, Circle, Tornado }

    /// <summary>
    /// One shot type: how often it fires and where the bullets of one volley go.
    /// Rates are game time, so they slow down with slow motion (2013 mixed real-time timers and frame counts;
    /// fix for bugs 20 and 21: rates come from this data only).
    /// </summary>
    [Serializable]
    public class ShotPattern
    {
        public enum Layout
        {
            /// <summary>N bullets fanned around the aim; N = the volley size, which grows with the level.</summary>
            Volley,
            /// <summary>Bullets at fixed angles around the aim (Windmill: a "+" of double shots).</summary>
            Fixed,
            /// <summary>A full circle of bullets, starting at the aim.</summary>
            Ring,
            /// <summary>Bullets around a direction that turns every volley and ignores the aim (Tornado).</summary>
            Spin,
        }

        public ShotKind kind;
        public WordIDs nameId;
        [Tooltip("Seconds between volleys (game time: slower in slow motion).")]
        [Min(0.01f)] public float intervalSeconds = 0.1f;
        public Layout layout;
        [Tooltip("Volley: degrees between the bullets of one volley.")]
        public float volleyStepDegrees = 4f;
        [Tooltip("Fixed: bullet angles relative to the aim, in degrees.")]
        public float[] fixedAngles = new float[0];
        [Tooltip("Ring: bullets in the circle.")]
        [Min(1)] public int ringCount = 36;
        [Tooltip("Spin: bullet angles around the turning direction, in degrees.")]
        public float[] spinOffsets = { -10f, 0f, 10f };
        [Tooltip("Spin: degrees the direction turns after each volley.")]
        public float spinStepDegrees = 30f;
        [Tooltip("Add the small random spread and barrel alternation from Shooting Feel.")]
        public bool feelSpread = true;
        [Tooltip("Muzzle flash, recoil and camera kick on each volley.")]
        public bool muzzle = true;

        /// <summary>World angles (degrees) of the next volley. spinAngle is the Tornado's turning direction.</summary>
        public void Angles(float aimDegrees, int volley, ref float spinAngle, List<float> result)
        {
            result.Clear();
            switch (layout)
            {
                case Layout.Volley:
                {
                    int n = Mathf.Max(1, volley);
                    float first = -(n - 1) * volleyStepDegrees / 2f;
                    for (int i = 0; i < n; i++) result.Add(aimDegrees + first + i * volleyStepDegrees);
                    break;
                }
                case Layout.Fixed:
                    foreach (float a in fixedAngles) result.Add(aimDegrees + a);
                    break;
                case Layout.Ring:
                {
                    int n = Mathf.Max(1, ringCount);
                    for (int i = 0; i < n; i++) result.Add(aimDegrees + i * 360f / n);
                    break;
                }
                case Layout.Spin:
                    foreach (float a in spinOffsets) result.Add(spinAngle + a);
                    spinAngle = Mathf.Repeat(spinAngle + spinStepDegrees, 360f);
                    break;
            }
        }
    }

    /// <summary>
    /// Shot types and the bullets-per-volley progression. Edit in Project Settings > AntiBacteria > Weapons.
    /// Lives in Resources so builds read the same values.
    /// </summary>
    [CreateAssetMenu(menuName = "AntiBacteria/Weapon Settings", fileName = "WeaponSettings")]
    public class WeaponSettings : ScriptableObject
    {
        public const string ResourcePath = "WeaponSettings";

        [Tooltip("The five shot types. Normal is the default; the others come from bonuses and the upgrade shop.")]
        public ShotPattern[] patterns = Defaults();

        [Header("Bullets per volley (Normal and Water)")]
        [Tooltip("Campaign level where each bullet is earned. 2013: 1 bullet from level 1, 2 from 11, 3 from 21, 4 from 31.")]
        public int[] volleyFromLevel = { 1, 11, 21, 31 };
        [Tooltip("Never more than this many (2013 bug 20: its 'cap at 4' didn't cap).")]
        [Min(1)] public int maxVolley = 4;

        private static WeaponSettings current;

        public static WeaponSettings Current
        {
            get
            {
                if (current != null) return current;
                current = Resources.Load<WeaponSettings>(ResourcePath);
                if (current == null)
                {
                    current = CreateInstance<WeaponSettings>();
                    current.hideFlags = HideFlags.HideAndDontSave;
                }
                return current;
            }
        }

        public static void Use(WeaponSettings settings) => current = settings;

        public ShotPattern Get(ShotKind kind)
        {
            foreach (ShotPattern p in patterns) if (p != null && p.kind == kind) return p;
            return patterns.Length > 0 ? patterns[0] : null;
        }

        public int VolleyForLevel(int level)
        {
            int n = 0;
            foreach (int from in volleyFromLevel) if (level >= from) n++;
            return Mathf.Clamp(n, 1, maxVolley);
        }

        /// <summary>
        /// 2013 values (MainCharacter.mm), with clean rates: Water and Tornado asked for 0.02 s but ran at about one volley
        /// per frame; 0.03 s keeps them a hose without depending on the frame rate.
        /// </summary>
        public static ShotPattern[] Defaults() => new[]
        {
            new ShotPattern { kind = ShotKind.Normal, nameId = WordIDs.NormalShoot, intervalSeconds = 0.1f, layout = ShotPattern.Layout.Volley },
            new ShotPattern { kind = ShotKind.Water, nameId = WordIDs.WaterShoot, intervalSeconds = 0.03f, layout = ShotPattern.Layout.Volley },
            new ShotPattern
            {
                kind = ShotKind.Windmill, nameId = WordIDs.WindmillShoot, intervalSeconds = 0.1f, layout = ShotPattern.Layout.Fixed,
                fixedAngles = new[] { -5f, 5f, 85f, 95f, 175f, 185f, 265f, 275f }, feelSpread = false,
            },
            new ShotPattern
            {
                kind = ShotKind.Circle, nameId = WordIDs.CircleShoot, intervalSeconds = 0.2f, layout = ShotPattern.Layout.Ring,
                ringCount = 36, feelSpread = false, muzzle = false,
            },
            new ShotPattern
            {
                kind = ShotKind.Tornado, nameId = WordIDs.TornadoShoot, intervalSeconds = 0.03f, layout = ShotPattern.Layout.Spin,
                spinOffsets = new[] { -10f, 0f, 10f }, spinStepDegrees = 30f, feelSpread = false, muzzle = false,
            },
        };
    }
}
