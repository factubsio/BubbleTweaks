using HarmonyLib;
using System.Linq;
using JetBrains.Annotations;
using Kingmaker;
using Kingmaker.Blueprints.JsonSystem;
using System;
using BubbleTweaks.Config;
using BubbleTweaks.Utilities;
using UnityModManagerNet;
using UnityEngine;
using Kingmaker.Localization;
using Kingmaker.UI.SettingsUI;
using Kingmaker.Settings;
using Kingmaker.PubSubSystem;
using Kingmaker.Globalmap;
using UnityEngine.UI;
using Owlcat.Runtime.UI.Controls.Button;
using Kingmaker.UI.Common;
using Kingmaker.Globalmap.State;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Kingmaker.Visual.CharacterSystem;
using Kingmaker.Utility;
using Kingmaker.Blueprints;
using Newtonsoft.Json;
using System.IO;
using System.Reflection;
using Kingmaker.UI.MVVM._VM.Party;

namespace BubbleTweaks {

    public enum AttackTextColor {
        Default,
        Yellow,
        Red,
        Blue,
    }

    public class UISettingsEntityDropdownAttackTextColor : UISettingsEntityDropdownEnum<AttackTextColor> { }

    public class BubbleSettings {
        public SettingsEntityFloat TacticalCombatSpeed = new("bubbles.settings.game.tactical.time-scale", 1.0f);
        public SettingsEntityFloat InCombatSpeed = new("bubbles.settings.game.in-combat.time-scale", 1.0f);
        public SettingsEntityFloat OutOfCombatSpeed = new("bubbles.settings.game.out-of-combat.time-scale", 1.0f);
        public SettingsEntityFloat GlobalMapSpeed = new("bubbles.settings.game.global-map.time-scale", 1.0f);

        public SettingsEntityFloat CombatCursorScale = new("bubbles.settings.game.cursor-scale.combat", 1.0f);
        public SettingsEntityFloat NonCombatCursorScale = new("bubbles.settings.game.cursor-scale.non-combat", 1.0f);
        public SettingsEntityEnum<AttackTextColor> CursorAttackTextColor = new("bubbles.settings.game.cursor-text-color", AttackTextColor.Default);

        public SettingsEntityBool PartyViewWith8Slots = new("bubbles.settings.game.ui-party-view-eight", false, false, true);

        public UISettingsEntitySliderFloat TacticalCombatSpeedSlider;
        public UISettingsEntitySliderFloat InCombatSpeedSlider;
        public UISettingsEntitySliderFloat OutOfCombatSpeedSlider;
        public UISettingsEntitySliderFloat GlobalMapSpeedSlider;

        public UISettingsEntitySliderFloat CombatCursorScaleSlider;
        public UISettingsEntitySliderFloat NonCombatCursorScaleSlider;
        public UISettingsEntityDropdownEnum<AttackTextColor> CursorAttackTextColorDropdown;

        public UISettingsEntityBool PartyViewWith8SlotsToggle;

        private BubbleSettings() { }

        public static UISettingsEntityBool MakeToggle(string key, string name, string tooltip) {
            var toggle = ScriptableObject.CreateInstance<UISettingsEntityBool>();
            toggle.m_Description = Helpers.CreateString($"{key}.description", name);
            toggle.m_TooltipDescription = Helpers.CreateString($"{key}.tooltip-description", tooltip);
            toggle.DefaultValue = false;
            toggle.m_ShowVisualConnection = true;
            return toggle;
        }

        public static V MakeEnumDropdown<V, T>(string key, string name, string tooltip) where T: Enum where V : UISettingsEntityDropdownEnum<T> {
            var dropdown = ScriptableObject.CreateInstance<V>();
            Main.Log($"dropdown null: {dropdown == null}");
            dropdown.m_Description = Helpers.CreateString($"{key}.description", name);
            dropdown.m_TooltipDescription = Helpers.CreateString($"{key}.tooltip-description", tooltip);
            dropdown.m_CashedLocalizedValues = new();
            Main.Log("created cashed values?");
            foreach (var v in Enum.GetValues(typeof(T)))
                dropdown.m_CashedLocalizedValues.Add(v.ToString());
            dropdown.m_ShowVisualConnection = true;

            return dropdown;
        }

        public static UISettingsEntitySliderFloat MakeSliderFloat(string key, string name, string tooltip, float min, float max, float step) {
            var slider = ScriptableObject.CreateInstance<UISettingsEntitySliderFloat>();
            slider.m_Description = Helpers.CreateString($"{key}.description", name);
            slider.m_TooltipDescription = Helpers.CreateString($"{key}.tooltip-description", tooltip);
            slider.m_MinValue = min;
            slider.m_MaxValue = max;
            slider.m_Step = step;
            slider.m_ShowValueText = true;
            slider.m_DecimalPlaces = 1;
            slider.m_ShowVisualConnection = true;

            return slider;
        }

        public static UISettingsGroup MakeSettingsGroup(string key, string name, params UISettingsEntityBase[] settings) {
            UISettingsGroup group = ScriptableObject.CreateInstance<UISettingsGroup>();
            group.name = key;
            group.Title = Helpers.CreateString(key, name);

            group.SettingsList = settings;

            return group;
        }

        private bool Initialized = false;

        public void Initialize() {
            if (Initialized) return;
            Initialized = true;

            TacticalCombatSpeedSlider = MakeSliderFloat("settings.game.tactical.time-scale", "Increase tactical combat animation speed", "Speeds up the animation speed of the all characters in tactical battle mode.", 1, 10, 0.1f);
            TacticalCombatSpeedSlider.LinkSetting(TacticalCombatSpeed);

            InCombatSpeedSlider = MakeSliderFloat("settings.game.in-combat.time-scale", "Increase animation speed when in combat", "Speeds up the animation speed of the all characters when engaged in combat.", 1, 10, 0.1f);
            InCombatSpeedSlider.LinkSetting(InCombatSpeed);

            OutOfCombatSpeedSlider = MakeSliderFloat("settings.game.out-of-combat.time-scale", "Increase animation speed when not in combat", "Speeds up the animation speed of the all characters when not engaged in combat.", 1, 10, 0.1f);
            OutOfCombatSpeedSlider.LinkSetting(OutOfCombatSpeed);

            GlobalMapSpeedSlider = MakeSliderFloat("settings.game.global-map.time-scale", "Increase animation speed when on the global map", "Speeds up the animation speed of all tokens on the global map.", 1, 10, 0.1f);
            GlobalMapSpeedSlider.LinkSetting(GlobalMapSpeed);
            (GlobalMapSpeed as IReadOnlySettingEntity<float>).OnValueChanged += (_) => {
                SpeedTweaks.UpdateSpeed();
            };

            try {

                CombatCursorScaleSlider = MakeSliderFloat("settings.game.cursor-scale.combat", "Scale the cursor for combat actions (e.g. attack, step, ranged attack)", "Scale the cursor for combat actions (e.g. attack, step, ranged attack)", 0.25f, 5.0f, 0.25f);
                CombatCursorScaleSlider.LinkSetting(CombatCursorScale);
            } catch (Exception ex) {
                Main.Error(ex, "making combat cursor scale");
            }

            try {
                NonCombatCursorScaleSlider = MakeSliderFloat("settings.game.cursor-scale.non-combat", "Scale the cursor for non-combat actions", "Scale the cursor for non-combat actions", 0.25f, 5.0f, 0.25f);
                NonCombatCursorScaleSlider.LinkSetting(NonCombatCursorScale);

            } catch (Exception ex) {
                Main.Error(ex, "making non combat cursor scale ");
            }
            try {
                CursorAttackTextColorDropdown = MakeEnumDropdown<UISettingsEntityDropdownAttackTextColor, AttackTextColor>("settings.game.cursor-text-color", "Color for the attack-count on the cursor", "Color for the attack-count text on the curosr");
                CursorAttackTextColorDropdown.LinkSetting(CursorAttackTextColor);
            } catch (Exception ex) {
                Main.Error(ex, "making combat text color");
            }

            try {
                PartyViewWith8SlotsToggle = MakeToggle("settings.game.ui-party-view-replacement-eight", "Expanded party view (eight slots)", "Replace the Owlcat party view at the bottom of the screen with a custom version that can show eight characters without scrolling\n<size=150%><b>Requires a full game restart</b></size>");
                PartyViewWith8SlotsToggle.LinkSetting(PartyViewWith8Slots);
            } catch (Exception ex) {
                Main.Error(ex, "making enhanced party view toggle");
            }
        }

        private static readonly BubbleSettings instance = new();
        public static BubbleSettings Instance { get { return instance; } }
    }


    [HarmonyPatch(typeof(UISettingsManager), "Initialize")]
    public static class SettingsInjector {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "harmony patch")]
        static void Postfix() {
            if (Game.Instance.UISettingsManager.m_GameSettingsList.Any(group => group.name?.StartsWith("bubble") ?? false)) {
                return;
            }

            BubbleSettings.Instance.Initialize();

            Game.Instance.UISettingsManager.m_GameSettingsList.Add(
                BubbleSettings.MakeSettingsGroup("bubble.speed-tweaks", "Bubble speed tweaks",
                    BubbleSettings.Instance.GlobalMapSpeedSlider,
                    BubbleSettings.Instance.InCombatSpeedSlider,
                    BubbleSettings.Instance.OutOfCombatSpeedSlider,
                    BubbleSettings.Instance.TacticalCombatSpeedSlider));

            Game.Instance.UISettingsManager.m_GameSettingsList.Add(
                BubbleSettings.MakeSettingsGroup("bubble.cursor-tweaks", "Bubble cursor tweaks",
                    BubbleSettings.Instance.CombatCursorScaleSlider,
                    BubbleSettings.Instance.NonCombatCursorScaleSlider,
                    BubbleSettings.Instance.CursorAttackTextColorDropdown));

            Game.Instance.UISettingsManager.m_GameSettingsList.Add(
                BubbleSettings.MakeSettingsGroup("bubble.ui-tweaks", "Bubble UI tweaks",
                    BubbleSettings.Instance.PartyViewWith8SlotsToggle));

            if (BubbleSettings.Instance.PartyViewWith8Slots.GetValue()) {
                PartyVM_Patches.Repatch();
            }
        }
    }

#if DEBUG
    [EnableReloading]
#endif
    public static class Main {
        public static Harmony harmony;
        public static bool Enabled;
        internal static string ModPath;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "harmony method")]
        static bool Load(UnityModManager.ModEntry modEntry) {
            harmony = new Harmony(modEntry.Info.Id);
#if DEBUG
            modEntry.OnUnload = OnUnload;
#endif
            modEntry.OnUpdate = OnUpdate;
            ModSettings.ModEntry = modEntry;

            ModSettings.ModEntry.Logger.Log("HELLO???");

            ModSettings.LoadAllSettings();
            Enabled = true;
            ModPath = modEntry.Path;

            //BundleManger.AddBundle("tutorialcanvas");
            Main.Log("Loaded bundle");

#if DEBUG
            harmony.PatchAll();
            PostPatchInitializer.Initialize();
            //SpeedTweaks.Install();
            //Crusade.Install();
            //StatisticsOhMy.Install();
            LuckMeter.Install();
#else
            harmony.PatchAll();
            PostPatchInitializer.Initialize();
            SpeedTweaks.Install();
            Crusade.Install();
            StatisticsOhMy.Install();
            LuckMeter.Install();
#endif


            return true;
        }
        static bool Shifting => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        static void OnUpdate(UnityModManager.ModEntry modEntry, float delta) {

#if DEBUG
            if (Input.GetKeyDown(KeyCode.I) && Shifting) {
            } else if (Input.GetKeyDown(KeyCode.B) && Shifting) {
                modEntry.GetType().GetMethod("Reload", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(modEntry, new object[] { });
            } else if (Input.GetKeyDown(KeyCode.R) && Shifting) {
                Main.Log("HELLO");
                LuckMeter.Show();
            }
#endif
        }

        static bool OnUnload(UnityModManager.ModEntry modEntry) {
            Main.Log("WARNING: UNLOADING");
            Main.Log("WARNING: UNLOADING");

            harmony.UnpatchAll();
            //SpeedTweaks.Uninstall();
            //Crusade.Uninstall();
            //StatisticsOhMy.Uninstall();
            LuckMeter.UnInstall();

            return true;

        }

        internal static void LogPatch(string v, object coupDeGraceAbility) {
            throw new NotImplementedException();
        }

        public static void Log(string msg) {
            ModSettings.ModEntry.Logger.Log(msg);
        }
        [System.Diagnostics.Conditional("DEBUG")]
        public static void LogDebug(string msg) {
            ModSettings.ModEntry.Logger.Log(msg);
        }
        public static void LogPatch(string action, [NotNull] IScriptableObjectWithAssetId bp) {
            Log($"{action}: {bp.AssetGuid} - {bp.name}");
        }
        public static void LogHeader(string msg) {
            Log($"--{msg.ToUpper()}--");
        }
        public static void Error(Exception e, string message) {
            Log(message);
            Log(e.ToString());
            PFLog.Mods.Error(message);
        }
        public static void Error(string message) {
            Log(message);
            PFLog.Mods.Error(message);
        }

        public static void Safely(Action act) {
            try {
                act();
            } catch(Exception ex) {
                Main.Error(ex, "trying to safely invoke action");
            }
        }
    }
}