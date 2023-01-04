using HarmonyLib;
using Kingmaker.Controllers.MapObjects;
using System;

namespace BubbleTweaks {
    internal class InteractionHighlightToggle {
        [HarmonyPatch(typeof(InteractionHighlightController))]
        static class InteractionHighlightController_Patch {
            [HarmonyPatch(nameof(InteractionHighlightController.HighlightOn)), HarmonyPrefix]
            static bool HighlightOn(InteractionHighlightController __instance) {
                try {
                    if (__instance.m_Inactive || !BubbleSettings.Instance.InteractionHighlight.GetValue())
                        return true;

                    if (__instance.m_IsHighlighting) {
                        __instance.m_IsHighlighting = false;
                        __instance.HighlightOff();
                        return false;
                    }

                    return true;
                }
                catch (Exception e) {
                    Main.Error(e, "Failed while processing interaction highlight toggle on.");
                }
                return true;
            }

            [HarmonyPatch(nameof(InteractionHighlightController.HighlightOff)), HarmonyPrefix]
            static bool HighlightOff(InteractionHighlightController __instance) {
                try {
                    if (__instance.m_Inactive || !BubbleSettings.Instance.InteractionHighlight.GetValue())
                        return true;

                    if (__instance.m_IsHighlighting) {
                        return false;
                    }

                    // Set this to true so the normal logic will process to disable higlights
                    __instance.m_IsHighlighting = true;
                    return true;
                } catch (Exception e) {
                    Main.Error(e, "Failed while processing interaction highlight toggle off.");
                }
                return true;
            }
        }
    }
}
