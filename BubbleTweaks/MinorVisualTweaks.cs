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
using Kingmaker.UI._ConsoleUI.Overtips;
using System.Reflection;

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
    [HarmonyPatch(typeof(OvertipViewPartBase))]
    static class OvertipViewPartBase_Patches {

        private static readonly MethodInfo EntityOvertipVM_get_IsTurnBased = typeof(EntityOvertipVM).GetProperty("IsTurnBased", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).GetMethod;

        private static bool FakeCheckIsTurnBased(OvertipViewPartBase view) {
            //if we're really not in turn based, we're not in turn based
            if (!view.ViewModel.IsTurnBased)
                return false;

            //if we are in turn based but we're one of the special parts, pretend we're not in turn based
            if (view is OvertipViewPartName || view is OvertipViewPartShortHitPoints || view is OvertipViewPartStandartHitPoints)
                return false;

            //but for everything else we are in turn based
            return true;
        }


        [HarmonyPatch(nameof(OvertipViewPartBase.NeedShow)), HarmonyTranspiler]
        static IEnumerable<CodeInstruction> NeedShow(IEnumerable<CodeInstruction> instructions) {
#if false
		/* 0x001C26CA 02           */ IL_000A: ldarg.0
		/* 0x001C26CB 280237000A   */ IL_000B: call      instance !0 class [Owlcat.Runtime.UI]Owlcat.Runtime.UI.MVVM.ViewBase`1<class Kingmaker.UI._ConsoleUI.Overtips.EntityOvertipVM>::get_ViewModel()
		/* 0x001C26D0 6FC46E0006   */ IL_0010: callvirt  instance bool Kingmaker.UI._ConsoleUI.Overtips.EntityOvertipVM::get_IsTurnBased()
		/* 0x001C26D5 2C02         */ IL_0015: brfalse.s IL_0019

        replace the call get_ViewModel with a call to our static method 

		/* 0x001C26CA 02           */ IL_000A: ldarg.0
		/* 0x001C26CB 280237000A   */ IL_000B: call      FakeCheckIsTurnBased
		/* 0x001C26D0 6FC46E0006   */ IL_0010: callvirt  instance bool Kingmaker.UI._ConsoleUI.Overtips.EntityOvertipVM::get_IsTurnBased()
		/* 0x001C26D5 2C02         */ IL_0015: brfalse.s IL_0019

        delete the callvirt to get_IsTurnBased

		/* 0x001C26CA 02           */ IL_000A: ldarg.0
		/* 0x001C26CB 280237000A   */ IL_000B: call      FakeCheckIsTurnBased
		/* 0x001C26D5 2C02         */ IL_0015: brfalse.s IL_0019
#endif
            var input = instructions.ToList();

            //find the callvirt get_IsTurnBased
            int call_IsTurnBased = input.FindIndex(ins => ins.Calls(EntityOvertipVM_get_IsTurnBased));

            //replace the preceding call
            input[call_IsTurnBased - 1] = CodeInstruction.Call(typeof(OvertipViewPartBase_Patches), "FakeCheckIsTurnBased", new Type[] { typeof(OvertipViewPartBase) });

            //remove the callvirt
            input.RemoveAt(call_IsTurnBased);

            return input;
        }
    }

    [HarmonyPatch(typeof(UnitOvertipView))]
    static class UnitOvertipView_Patches {

        private static readonly MethodInfo PartName_NeedsShow = typeof(OvertipViewPartBase).GetMethod("NeedShow", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

        public static bool CheckNeedsShow(UnitOvertipView view) {
            return view.m_OvertipViewPartName.NeedShow() || view.m_OvertipStandartHitPoints.NeedShow() || view.m_OvertipShortHitPoints.NeedShow();
        }

        [HarmonyPatch(nameof(UnitOvertipView.NeedName)), HarmonyTranspiler]
        static IEnumerable<CodeInstruction> NeedName(IEnumerable<CodeInstruction> instructions) {
            var input = instructions.ToList();
            int insertAt = input.FindIndex(ins => ins.Calls(PartName_NeedsShow));
            input[insertAt] = CodeInstruction.Call(typeof(UnitOvertipView_Patches), "CheckNeedsShow", new Type[] { typeof(UnitOvertipView) }); //replace the callvirt with our own method
            input.RemoveAt(insertAt - 1); //delete the ldfld
            return input;
        }
    }




    class MinorVisualTweaks {
        public static void Install() {

        }
    }
}
