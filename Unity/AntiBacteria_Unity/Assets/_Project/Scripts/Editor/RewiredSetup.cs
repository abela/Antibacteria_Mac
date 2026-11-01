using System;
using System.Collections.Generic;
using System.Linq;
using AntiBacteria.Input;
using Rewired;
using Rewired.Data;
using Rewired.Data.Mapping;
using UnityEditor;
using UnityEngine;

namespace AntiBacteria.EditorTools
{
    /// <summary>
    /// Fills a fresh Rewired Input Manager with AntiBacteria's actions and default bindings
    /// (keyboard + mouse and any gamepad through Rewired's Dual Analog Gamepad template), then saves
    /// it as Assets/_Project/Prefabs/RewiredInputManager.prefab. Run once on a NEW Input Manager:
    /// GameObject > Create Other > Rewired > Input Manager, then AntiBacteria > Input > Configure.
    /// After that, edit bindings in Rewired's own editor window.
    /// </summary>
    public static class RewiredSetup
    {
        public const string PrefabPath = "Assets/_Project/Prefabs/RewiredInputManager.prefab";

        // Rewired "Dual Analog Gamepad" template (works for Xbox, PlayStation, Switch Pro, Steam Deck...)
        private static readonly Guid GamepadTemplate = new Guid("83b427e4-086f-47f3-bb06-be266abd1ca5");
        private const int LeftStickX = 0, LeftStickY = 1, RightStickX = 2, RightStickY = 3;
        private const int ActionBottomRow1 = 4, ActionBottomRow2 = 5;          // A / Cross,  B / Circle
        private const int LeftShoulder1 = 10, LeftShoulder2 = 11;              // LB, LT
        private const int RightShoulder1 = 12, RightShoulder2 = 13;            // RB, RT
        private const int Center2 = 15;                                        // Start / Options
        private const int DPadUp = 19, DPadRight = 20, DPadDown = 21, DPadLeft = 22;

        // Rewired mouse element ids
        private const int MouseLeftButton = 3, MouseRightButton = 4;

        [MenuItem("AntiBacteria/Input/Configure Rewired Input Manager")]
        public static void ConfigureFromMenu()
        {
            var im = UnityEngine.Object.FindFirstObjectByType<InputManager>();
            if (im == null)
            {
                Debug.LogError("No Rewired Input Manager in the open scene. Create one with GameObject > Create Other > Rewired > Input Manager first.");
                return;
            }
            Debug.Log(Configure(im));
        }

        public static string Configure(InputManager im)
        {
            UserData ud = im.userData;
            if (ud.GetActionNames().Contains(InputActions.MoveHorizontal))
                return "RewiredSetup: this Input Manager is already configured - edit it in Rewired's editor instead.";

            // ---- categories
            InputCategory gameplayCat = ud.GetActionCategory(0);
            Set(gameplayCat, "name", InputActions.GameplayCategory); Set(gameplayCat, "descriptiveName", "Gameplay");
            ud.AddActionCategory();
            InputCategory uiCat = ud.GetActionCategory(ud.GetActionCategoryIds().Length - 1);
            Set(uiCat, "name", InputActions.UICategory); Set(uiCat, "descriptiveName", "UI");

            InputMapCategory gameplayMap = ud.GetMapCategory(0);
            Set(gameplayMap, "name", InputActions.GameplayCategory); Set(gameplayMap, "descriptiveName", "Gameplay");
            ud.AddMapCategory();
            InputMapCategory uiMap = ud.GetMapCategory(ud.GetMapCategoryIds().Length - 1);
            Set(uiMap, "name", InputActions.UICategory); Set(uiMap, "descriptiveName", "UI");

            // ---- actions
            int moveH = AddAction(ud, gameplayCat.id, InputActions.MoveHorizontal, "Move", InputActionType.Axis, "Move right", "Move left");
            int moveV = AddAction(ud, gameplayCat.id, InputActions.MoveVertical, "Move", InputActionType.Axis, "Move up", "Move down");
            int aimH = AddAction(ud, gameplayCat.id, InputActions.AimHorizontal, "Aim", InputActionType.Axis, "Aim right", "Aim left");
            int aimV = AddAction(ud, gameplayCat.id, InputActions.AimVertical, "Aim", InputActionType.Axis, "Aim up", "Aim down");
            int fire = AddAction(ud, gameplayCat.id, InputActions.Fire, "Fire", InputActionType.Button);
            int slow = AddAction(ud, gameplayCat.id, InputActions.SlowMotion, "Slow motion", InputActionType.Button);
            int pause = AddAction(ud, gameplayCat.id, InputActions.Pause, "Pause", InputActionType.Button);

            int uiH = AddAction(ud, uiCat.id, InputActions.UIHorizontal, "UI horizontal", InputActionType.Axis, "Right", "Left");
            int uiV = AddAction(ud, uiCat.id, InputActions.UIVertical, "UI vertical", InputActionType.Axis, "Up", "Down");
            int uiSubmit = AddAction(ud, uiCat.id, InputActions.UISubmit, "Confirm", InputActionType.Button);
            int uiCancel = AddAction(ud, uiCat.id, InputActions.UICancel, "Back", InputActionType.Button);

            // ---- keyboard
            ControllerMap_Editor kbGame = KeyboardMap(ud, gameplayMap.id);
            Key(kbGame, moveH, KeyboardKeyCode.D); Key(kbGame, moveH, KeyboardKeyCode.A, Pole.Negative);
            Key(kbGame, moveV, KeyboardKeyCode.W); Key(kbGame, moveV, KeyboardKeyCode.S, Pole.Negative);
            Key(kbGame, moveH, KeyboardKeyCode.RightArrow); Key(kbGame, moveH, KeyboardKeyCode.LeftArrow, Pole.Negative);
            Key(kbGame, moveV, KeyboardKeyCode.UpArrow); Key(kbGame, moveV, KeyboardKeyCode.DownArrow, Pole.Negative);
            Key(kbGame, slow, KeyboardKeyCode.Space);
            Key(kbGame, pause, KeyboardKeyCode.Escape);
            Key(kbGame, pause, KeyboardKeyCode.P);

            ControllerMap_Editor kbUi = KeyboardMap(ud, uiMap.id);
            Key(kbUi, uiH, KeyboardKeyCode.RightArrow); Key(kbUi, uiH, KeyboardKeyCode.LeftArrow, Pole.Negative);
            Key(kbUi, uiV, KeyboardKeyCode.UpArrow); Key(kbUi, uiV, KeyboardKeyCode.DownArrow, Pole.Negative);
            Key(kbUi, uiH, KeyboardKeyCode.D); Key(kbUi, uiH, KeyboardKeyCode.A, Pole.Negative);
            Key(kbUi, uiV, KeyboardKeyCode.W); Key(kbUi, uiV, KeyboardKeyCode.S, Pole.Negative);
            Key(kbUi, uiSubmit, KeyboardKeyCode.Return); Key(kbUi, uiSubmit, KeyboardKeyCode.KeypadEnter); Key(kbUi, uiSubmit, KeyboardKeyCode.Space);
            Key(kbUi, uiCancel, KeyboardKeyCode.Escape); Key(kbUi, uiCancel, KeyboardKeyCode.Backspace);

            // ---- mouse (aiming uses the cursor position directly, see GameInput.MouseScreenPosition)
            ControllerMap_Editor mouseGame = MouseMap(ud, gameplayMap.id);
            Element(mouseGame, fire, MouseLeftButton, ControllerElementType.Button);
            Element(mouseGame, slow, MouseRightButton, ControllerElementType.Button);

            // ---- gamepad (template)
            ControllerMap_Editor padGame = JoystickMap(ud, gameplayMap.id);
            Element(padGame, moveH, LeftStickX, ControllerElementType.Axis);
            Element(padGame, moveV, LeftStickY, ControllerElementType.Axis);
            Element(padGame, moveH, DPadRight, ControllerElementType.Button);
            Element(padGame, moveH, DPadLeft, ControllerElementType.Button, Pole.Negative);
            Element(padGame, moveV, DPadUp, ControllerElementType.Button);
            Element(padGame, moveV, DPadDown, ControllerElementType.Button, Pole.Negative);
            Element(padGame, aimH, RightStickX, ControllerElementType.Axis);
            Element(padGame, aimV, RightStickY, ControllerElementType.Axis);
            Element(padGame, fire, RightShoulder2, ControllerElementType.Axis, Pole.Positive, AxisRange.Positive);
            Element(padGame, fire, RightShoulder1, ControllerElementType.Button);
            Element(padGame, slow, LeftShoulder2, ControllerElementType.Axis, Pole.Positive, AxisRange.Positive);
            Element(padGame, slow, LeftShoulder1, ControllerElementType.Button);
            Element(padGame, pause, Center2, ControllerElementType.Button);

            ControllerMap_Editor padUi = JoystickMap(ud, uiMap.id);
            Element(padUi, uiH, LeftStickX, ControllerElementType.Axis);
            Element(padUi, uiV, LeftStickY, ControllerElementType.Axis);
            Element(padUi, uiH, DPadRight, ControllerElementType.Button);
            Element(padUi, uiH, DPadLeft, ControllerElementType.Button, Pole.Negative);
            Element(padUi, uiV, DPadUp, ControllerElementType.Button);
            Element(padUi, uiV, DPadDown, ControllerElementType.Button, Pole.Negative);
            Element(padUi, uiSubmit, ActionBottomRow1, ControllerElementType.Button);
            Element(padUi, uiCancel, ActionBottomRow2, ControllerElementType.Button);

            // ---- the single local player
            string[] names = ud.GetPlayerNames();
            int playerIndex = Array.IndexOf(names, InputActions.PlayerName);
            if (playerIndex < 0)
            {
                ud.AddPlayer();
                playerIndex = ud.GetPlayerIds().Length - 1;
            }
            Player_Editor player = ud.GetPlayer(playerIndex);
            Set(player, "name", InputActions.PlayerName);
            Set(player, "descriptiveName", "Player");
            Set(player, "startPlaying", true);
            Set(player, "assignKeyboardOnStart", true);
            Set(player, "assignMouseOnStart", true);
            Set(player, "defaultKeyboardMaps", Mappings(gameplayMap.id, uiMap.id));
            Set(player, "defaultMouseMaps", Mappings(gameplayMap.id));
            Set(player, "defaultJoystickMaps", Mappings(gameplayMap.id, uiMap.id));

            EditorUtility.SetDirty(im);

            // ---- save as prefab
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs")) AssetDatabase.CreateFolder("Assets/_Project", "Prefabs");
            PrefabUtility.SaveAsPrefabAssetAndConnect(im.gameObject, PrefabPath, InteractionMode.AutomatedAction);

            ActionElementMap firstKey = kbGame.ActionElementMaps.First();
            return "RewiredSetup: " + ud.GetActionIds().Length + " actions, keyboard/mouse/gamepad maps for Gameplay + UI, player '" +
                   player.name + "'. Saved " + PrefabPath + ". (check: key " + firstKey.keyboardKeyCode + " -> element id " + firstKey.elementIdentifierId + ")";
        }

        private static int AddAction(UserData ud, int categoryId, string name, string descriptive, InputActionType type,
                                     string positive = null, string negative = null)
        {
            ud.AddAction(categoryId);
            int[] ids = ud.GetActionIds();
            InputAction a = ud.GetActionById(ids[ids.Length - 1]);
            Set(a, "name", name);
            Set(a, "descriptiveName", descriptive);
            Set(a, "type", type);
            if (positive != null) Set(a, "positiveDescriptiveName", positive);
            if (negative != null) Set(a, "negativeDescriptiveName", negative);
            Set(a, "userAssignable", true);
            return a.id;
        }

        private static ControllerMap_Editor KeyboardMap(UserData ud, int mapCategoryId)
        {
            ud.CreateKeyboardMap(mapCategoryId, 0);
            return ud.GetKeyboardMap(mapCategoryId, 0);
        }

        private static ControllerMap_Editor MouseMap(UserData ud, int mapCategoryId)
        {
            ud.CreateMouseMap(mapCategoryId, 0);
            return ud.GetMouseMap(mapCategoryId, 0);
        }

        private static ControllerMap_Editor JoystickMap(UserData ud, int mapCategoryId)
        {
            ud.CreateJoystickMap(mapCategoryId, GamepadTemplate, 0);
            return ud.GetJoystickMap(mapCategoryId, GamepadTemplate, 0);
        }

        private static ActionElementMap NewElement(ControllerMap_Editor map)
        {
            map.AddActionElementMap();
            return map.ActionElementMaps.Last();
        }

        // Rewired's keyboard element identifier ids, read from Rewired at runtime
        // (ReInput.controllers.Keyboard.GetElementIdentifierByKeyCode). Setting only keyboardKeyCode in
        // editor data leaves the element id at -1, which Rewired treats as unbound.
        private static readonly Dictionary<KeyboardKeyCode, int> KeyElementIds = new Dictionary<KeyboardKeyCode, int>
        {
            { KeyboardKeyCode.A, 1 }, { KeyboardKeyCode.D, 4 }, { KeyboardKeyCode.P, 16 },
            { KeyboardKeyCode.S, 19 }, { KeyboardKeyCode.W, 23 },
            { KeyboardKeyCode.KeypadEnter, 52 }, { KeyboardKeyCode.Space, 54 }, { KeyboardKeyCode.Backspace, 55 },
            { KeyboardKeyCode.Return, 58 }, { KeyboardKeyCode.Escape, 60 },
            { KeyboardKeyCode.UpArrow, 89 }, { KeyboardKeyCode.DownArrow, 90 },
            { KeyboardKeyCode.RightArrow, 91 }, { KeyboardKeyCode.LeftArrow, 92 },
        };

        private static void Key(ControllerMap_Editor map, int actionId, KeyboardKeyCode key, Pole pole = Pole.Positive)
        {
            int elementId;
            if (!KeyElementIds.TryGetValue(key, out elementId))
                throw new ArgumentException("RewiredSetup: add the Rewired element id for key " + key + " to KeyElementIds.");
            ActionElementMap e = NewElement(map);
            Set(e, "actionId", actionId);
            Set(e, "elementType", ControllerElementType.Button);
            Set(e, "elementIdentifierId", elementId);
            Set(e, "keyboardKeyCode", key);
            Set(e, "axisContribution", pole);
        }

        private static void Element(ControllerMap_Editor map, int actionId, int elementId, ControllerElementType type,
                                    Pole pole = Pole.Positive, AxisRange range = AxisRange.Full)
        {
            ActionElementMap e = NewElement(map);
            Set(e, "actionId", actionId);
            Set(e, "elementType", type);
            Set(e, "elementIdentifierId", elementId);
            Set(e, "axisRange", range);
            Set(e, "axisContribution", pole);
        }

        private static List<Player_Editor.Mapping> Mappings(params int[] mapCategoryIds)
        {
            return mapCategoryIds.Select(id => new Player_Editor.Mapping(true, id, 0)).ToList();
        }

        /// <summary>Rewired's data properties have internal setters (its own editor assigns them);
        /// call the setter through reflection so any side effects still run.</summary>
        private static void Set(object target, string property, object value)
        {
            var prop = target.GetType().GetProperty(property,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            var setter = prop != null ? prop.GetSetMethod(true) : null;
            if (setter != null) { setter.Invoke(target, new[] { value }); return; }
            var field = target.GetType().GetField("_" + property,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field == null) throw new MissingMemberException(target.GetType().Name, property);
            field.SetValue(target, value);
        }
    }
}
