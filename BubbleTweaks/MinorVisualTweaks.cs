using HarmonyLib;
using Kingmaker.UI.MVVM._PCView.ServiceWindows.LocalMap.Markers;
using UniRx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.View;
using Kingmaker.UI.MVVM._VM.ServiceWindows.LocalMap.Markers;
using Kingmaker.UI;
using static Kingmaker.Blueprints.Root.CursorRoot;
using Kingmaker.UI.AbilityTarget;

namespace BubbleTweaks {

    [HarmonyPatch(typeof(CursorController), "SetCursor")]
    static class JumboCursors {
        private static HashSet<CursorType> jumbo = new() {
            CursorType.AttackCursor,
            CursorType.RangeAttackCursor,
            CursorType.StepCursor,
            CursorType.StepVerticalCursor,
            CursorType.MoveCursor,
        };

        private static Dictionary<AttackTextColor, Color> colorForValue = new() {
            { AttackTextColor.Default, Color.white },
            { AttackTextColor.Red, Color.red },
            { AttackTextColor.Blue, Color.blue },
            { AttackTextColor.Yellow, Color.yellow },
        };

        private static AttackTextColor currentAttackTextColor = AttackTextColor.Default;

        public static void Postfix(CursorType cursorType) {
            if (PCCursor.Instance?.m_CanvasScaler == null)
                return;

            if (jumbo.Contains(cursorType))
                PCCursor.Instance.m_CanvasScaler.scaleFactor = BubbleSettings.Instance.CombatCursorScale.GetValue();
            else
                PCCursor.Instance.m_CanvasScaler.scaleFactor = BubbleSettings.Instance.NonCombatCursorScale.GetValue();

            if (currentAttackTextColor != BubbleSettings.Instance.CursorAttackTextColor.GetValue()) {
                currentAttackTextColor = BubbleSettings.Instance.CursorAttackTextColor.GetValue();
                PCCursor.Instance.Text.color = colorForValue[currentAttackTextColor];
            }

        }
    }

    [HarmonyPatch(typeof(LocalMapMarkerPCView), "BindViewImplementation")]
    static class PatchLootVisualColor {

        public static void Postfix(LocalMapMarkerPCView __instance) {
            if (__instance == null)
                return;

            if (__instance.ViewModel.MarkerType == Kingmaker.UI.MVVM._VM.ServiceWindows.LocalMap.Utils.LocalMapMarkType.Loot) {
                __instance.AddDisposable(__instance.ViewModel.IsVisible.Subscribe<bool>(value => {
                    var markerVm = __instance.ViewModel as LocalMapCommonMarkerVM;
                    if (markerVm == null) {
                        return;
                    }
                    Color col = markerVm.m_Marker is UnitLocalMapMarker ? Color.red : Color.green;
                    foreach (var image in __instance.GetComponentsInChildren<Image>()) {
                        image.color = col;
                    }
                }));
            }
        }

    }

    class MinorVisualTweaks {
        public static void Install() {

        }
    }
}
