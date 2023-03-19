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
using Kingmaker;
using DungeonArchitect;
using Kingmaker.UI.MVVM._VM.ServiceWindows.LocalMap.Utils;
using Kingmaker.View.MapObjects;
using BubbleTweaks.Utilities;
using Kingmaker.Localization;
using Kingmaker.UI.MVVM._PCView.ServiceWindows.LocalMap;
using Owlcat.Runtime.UI.Utility;
using Kingmaker.PubSubSystem;
using Kingmaker.Blueprints;
using Kingmaker.Dungeon;

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

            if (__instance.ViewModel.MarkerType == LocalMapMarkType.Loot) {
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

    [HarmonyPatch(typeof(PauseToggle))]
    static class PauseToggle_Patches {
        public static float GetPauseStrength() => BubbleSettings.Instance.PauseFadeStrength.GetValue();

        [HarmonyPatch(nameof(PauseToggle.PlayPause)), HarmonyTranspiler]
        static IEnumerable<CodeInstruction> PlayPause(IEnumerable<CodeInstruction> instructions) {
            return PatchPauseFunction(instructions);
        }

        [HarmonyPatch(nameof(PauseToggle.Initialize)), HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Initialize(IEnumerable<CodeInstruction> instructions) {
            return PatchPauseFunction(instructions);
        }

        [HarmonyPatch(nameof(PauseToggle.OnAreaDidLoad)), HarmonyTranspiler]
        static IEnumerable<CodeInstruction> OnAreaDidLoad(IEnumerable<CodeInstruction> instructions) {
            return PatchPauseFunction(instructions);
        }

        private static IEnumerable<CodeInstruction> PatchPauseFunction(IEnumerable<CodeInstruction> instructions) {
            var input = instructions.ToList();
            int load_const = input.FindIndex(ins => ins.LoadsConstant(1.0));
            if (load_const == -1) {
                Main.Log("ERROR: cannot find ldc 1");
                return instructions;
            } else {
                Main.LogDebug("Found ldc 1 @ " + load_const);
            }

            var old = input[load_const];
            var replacement = CodeInstruction.Call(typeof(PauseToggle_Patches), nameof(PauseToggle_Patches.GetPauseStrength));
            replacement.MoveLabelsFrom(old);

            input[load_const] = replacement;

            return input;
        }
    }

    public class DoorMarker : ILocalMapMarker {
        private InteractionDoor door;
        public DoorMarker(InteractionDoor door) {
            this.door = door;
        }
        public string GetDescription() {
            return "Door";
        }

        public LocalMapMarkType GetMarkerType() {
            return LocalMapMarkType.Poi;
        }

        public Vector3 GetPosition() {
            return door.transform.position;
        }

        public bool IsVisible() {
            return true;
        }
    }

    [HarmonyPatch(typeof(LocalMapBaseView))]
    static class LocalMapBaseView_Patches {

        private static Sprite doorSprite = null;
        private static Sprite anchorSprite = null;


        [HarmonyPatch(nameof(LocalMapBaseView.AddLocalMapMarker))]
        [HarmonyPrefix]
        public static bool AddLocalMapMarker(LocalMapMarkerVM localMapMarkerVM, LocalMapBaseView __instance) {
            if (localMapMarkerVM.MarkerType >= LocalMapMarkType_EXT.Door) {
                try {
                    LocalMapMarkerSet localMapMarkerSet = __instance.m_MarkerSets.FirstOrDefault((LocalMapMarkerSet s) => s.Type == LocalMapMarkType.Loot);
                    if (localMapMarkerSet == null) {
                        return false;
                    }
                    LocalMapMarkerPCView item = WidgetFactory.GetWidget<LocalMapMarkerPCView>(localMapMarkerSet.View, true, false);
                    item.transform.SetParent(localMapMarkerSet.Container, false);
                    item.Initialize(__instance.m_Image.rectTransform.sizeDelta, delegate {
                        WidgetFactory.DisposeWidget<LocalMapMarkerPCView>(item);
                    });
                    item.Bind(localMapMarkerVM);
                    doorSprite ??= AssetLoader.LoadInternal("icons", "door_map_marker.png", new Vector2Int(128, 128));
                    anchorSprite ??= AssetLoader.LoadInternal("icons", "anchor_map_marker.png", new Vector2Int(128, 128));

                    item.transform.Find("Mark").GetComponent<Image>().sprite = localMapMarkerVM.MarkerType switch {
                        LocalMapMarkType_EXT.Door => doorSprite,
                        LocalMapMarkType_EXT.SaveAnchor => anchorSprite,
                        _ => null,
                    };
                } catch (Exception ex) {
                    Main.Error(ex);
                }

                return false;
            }
            return true;
        }
    }

    public static class LocalMapMarkType_EXT {
        public const LocalMapMarkType Door = (LocalMapMarkType)100;
        public const LocalMapMarkType SaveAnchor = (LocalMapMarkType)101;
    }
    class DoorMarkerHandler : IAreaActivationHandler {
        private static DoorMarkerHandler _Instance = null;
        public static DoorMarkerHandler Instance => _Instance ??= new();

        private static SharedStringAsset doorDescription;
        private static SharedStringAsset anchorDescription;

        private static readonly BlueprintGuid AutoSaveGuid = BlueprintGuid.Parse("92be2851e8c54cc4a623094592d38d47");

        public void OnAreaActivated() {
            if (!DungeonController.IsDungeonArea)
                return;


            doorDescription ??= new() {
                String = Helpers.CreateString("door.mapmarker", "Door"),
                name = "bubble.door.mapmarker",
                hideFlags = HideFlags.DontSave
            };
            anchorDescription ??= new() {
                String = Helpers.CreateString("anchor.mapmarker", "Save Anchor"),
                name = "bubble.anchor.mapmarker",
                hideFlags = HideFlags.DontSave
            };

            foreach (var entityData in Game.Instance.State.MapObjects) {

                foreach (var part in entityData.Interactions) {
                    if (part is InteractionDoorPart doorPart && doorPart.Enabled && doorPart.Owner.IsVisibleForPlayer) {
                        AddMapMarkerPart(part.Owner, LocalMapMarkType_EXT.Door, doorDescription);
                    } else if (part is InteractionSkillCheckPart skillCheckPart) {
                        if (skillCheckPart.Settings?.CheckPassedActions?.deserializedGuid == AutoSaveGuid) {
                            AddMapMarkerPart(part.Owner, LocalMapMarkType_EXT.SaveAnchor, anchorDescription);
                        }
                    }

                }
            }
        }

        private static void AddMapMarkerPart(MapObjectEntityData owner, LocalMapMarkType type, SharedStringAsset description) {
            LocalMapMarkerPart localMapMarkerPart = owner.Ensure<LocalMapMarkerPart>();
            localMapMarkerPart.IsRuntimeCreated = true;
            localMapMarkerPart.Settings.Type = type;
            localMapMarkerPart.Settings.Description = description;
            localMapMarkerPart.Settings.DescriptionUnit = null;
            localMapMarkerPart.SetHidden(false);
        }
    }

    class MinorVisualTweaks {
        public static void Install() {

        }

        internal static void PrintDoors() {




        }
    }
}
