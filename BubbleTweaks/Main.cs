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
using Kingmaker.UI.Overtip;
using Kingmaker.UI._ConsoleUI.Overtips;
using System.Reflection.Emit;
using Kingmaker.UnitLogic.ActivatableAbilities;
using System.Diagnostics;

namespace BubbleTweaks {

    public enum AttackTextColor {
        Default,
        Yellow,
        Red,
        Blue,
    }

    public class UISettingsEntityDropdownAttackTextColor : UISettingsEntityDropdownEnum<AttackTextColor> { }

    public class BubbleSettings {
        public SettingsEntityFloat PauseFadeStrength = new("bubbles.settings.game.pause.fade-strength", 1.0f);
        public SettingsEntityFloat TacticalCombatSpeed = new("bubbles.settings.game.tactical.time-scale", 1.0f);
        public SettingsEntityFloat InCombatSpeed = new("bubbles.settings.game.in-combat.time-scale", 1.0f);
        public SettingsEntityFloat OutOfCombatSpeed = new("bubbles.settings.game.out-of-combat.time-scale", 1.0f);
        public SettingsEntityFloat GlobalMapSpeed = new("bubbles.settings.game.global-map.time-scale", 1.0f);

        public SettingsEntityFloat CombatCursorScale = new("bubbles.settings.game.cursor-scale.combat", 1.0f);
        public SettingsEntityFloat NonCombatCursorScale = new("bubbles.settings.game.cursor-scale.non-combat", 1.0f);
        public SettingsEntityEnum<AttackTextColor> CursorAttackTextColor = new("bubbles.settings.game.cursor-text-color", AttackTextColor.Default);

        public SettingsEntityBool PartyViewWith8Slots = new("bubbles.settings.game.ui-party-view-eight", false, false, true);

        public UISettingsEntitySliderFloat PauseFadeStrengthSlider;
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

            PauseFadeStrengthSlider = MakeSliderFloat("settings.game.pause.fade-strength", "Fade strength during pause", "Reduces the strength of the fade effect when the game is paused", 0, 1, 0.1f);
            PauseFadeStrengthSlider.LinkSetting(PauseFadeStrength);

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

            //try {
            //    PartyViewWith8SlotsToggle = MakeToggle("settings.game.ui-party-view-replacement-eight", "Expanded party view (eight slots)", "Replace the Owlcat party view at the bottom of the screen with a custom version that can show eight characters without scrolling\n<size=150%><b>Requires a full game restart</b></size>");
            //    PartyViewWith8SlotsToggle.LinkSetting(PartyViewWith8Slots);
            //} catch (Exception ex) {
            //    Main.Error(ex, "making enhanced party view toggle");
            //}
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
                    BubbleSettings.Instance.PauseFadeStrengthSlider ));

            //if (BubbleSettings.Instance.PartyViewWith8Slots.GetValue()) {
            //    PartyVM_Patches.Repatch();
            //}
        }
    }

    public static class Blueprinting {
        public static void Test(BlueprintsCache __instance) {
            var reader = new BinaryReader(__instance.m_PackFile);
            byte[] typeGuid = new byte[16];
            Dictionary<string, int> blueprintCountsByType = new();
            GuidClassBinder binder = (GuidClassBinder)Json.Serializer.Binder;
            int loaded = 0;
            int total = 0;

            Stopwatch timer = new();
            timer.Start();
            foreach (var (guid, cacheEntry) in __instance.m_LoadedBlueprints) {
                //reader.BaseStream.Seek(cacheEntry.Offset, SeekOrigin.Begin);
                //reader.Read(typeGuid, 0, typeGuid.Length);

                //var typeGuidString = new Guid(typeGuid).ToString("N");

                //if (binder.m_GuidToTypeCache.TryGetValue(typeGuidString, out var type)) {
                //    if (blueprintCountsByType.TryGetValue(type.FullName, out var count))
                //        blueprintCountsByType[type.FullName] = count;
                //    else
                //        blueprintCountsByType[type.FullName] = 1;

                //    loaded++;
                //} else {
                //    Main.Error("could not find type for guid");
                //}
                //total++;
                if (cacheEntry.Blueprint != null) {
                    loaded++;
                }
                total++;
            }
            timer.Stop();
            Main.Log($"Scanned types for {loaded}/{total} blueprints in {timer.ElapsedMilliseconds}ms, unique types: {blueprintCountsByType.Count}");
            //foreach (var k in blueprintCountsByType.Keys) {
            //    Main.Log(k);
            //}
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
            try {
                return DoLoad(modEntry);
            } catch (Exception e) {
                Main.Error(e, "bad");
            }

            return false;
        }

        private static bool DoLoad(UnityModManager.ModEntry modEntry) {
            harmony = new Harmony(modEntry.Info.Id);
#if DEBUG
            modEntry.OnUnload = OnUnload;
#endif
            modEntry.OnUpdate = OnUpdate;
            ModSettings.ModEntry = modEntry;

#if DEBUG
            ModSettings.ModEntry.Logger.Log("HELLO (dev) ???");
#else
            ModSettings.ModEntry.Logger.Log("HELLO (REL) ???");
#endif

            ModSettings.LoadAllSettings();
            Enabled = true;
            ModPath = modEntry.Path;

            harmony.PatchAll();
            SpeedTweaks.Install();
            Crusade.Install();
            StatisticsOhMy.Install();
            MinorVisualTweaks.Install();

            EventBus.Subscribe(AoeAreaIndicators.Instance);
            EventBus.Subscribe(DoorMarkerHandler.Instance);
            //StatusConditions.Install();
            //LuckMeter.Install();


            return true;
        }

        static bool Shifting => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        static void OnUpdate(UnityModManager.ModEntry modEntry, float delta) {

#if DEBUG
            if (Input.GetKeyDown(KeyCode.LeftAlt)) {
                // MinorVisualTweaks.PrintDoors();
                //ActionWheel.Toggle();
            } else if (Input.GetKeyUp(KeyCode.LeftAlt)) {
                //ActionWheel.Hide();
            } else if (Input.GetKeyDown(KeyCode.F) && Shifting) {
                modEntry.GetType().GetMethod("Reload", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(modEntry, new object[] { });
            } else if (Input.GetKeyDown(KeyCode.R) && Shifting) {
                // Blueprinting.Test(ResourcesLibrary.BlueprintsCache);
                //PartyVM_Patches.Repatch();
                //LuckMeter.Show();
            }
#endif
        }

#if DEBUG
        static bool OnUnload(UnityModManager.ModEntry modEntry) {
            Main.Log("WARNING: UNLOADING");
            Main.Log("WARNING: UNLOADING");

            //SpeedTweaks.Uninstall();
            //Crusade.Uninstall();
            //StatisticsOhMy.Uninstall();
            //LuckMeter.Uninstall();
            //StatusConditions.Uninstall();
            //MinorVisualTweaks.Uninstall();
            //Resources.Uninstall();
            //ActionWheel.Uninstall();
            //EventBus.Unsubscribe(AoeAreaIndicators.Instance);
            EventBus.Unsubscribe(DoorMarkerHandler.Instance);
            harmony.UnpatchAll();

            return true;

        }
#endif

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
        public static void Error(Exception e, string message = null) {
            if (message != null)
                Log(message);

            Log(e.ToString());
            if (message != null)
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
