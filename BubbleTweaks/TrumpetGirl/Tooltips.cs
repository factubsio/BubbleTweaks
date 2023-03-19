using HarmonyLib;
using Kingmaker.UI.MVVM._VM.Tooltip.Bricks;
using Kingmaker.UI.MVVM._VM.Tooltip.Templates;
using Owlcat.Runtime.UI.Tooltips;
using System.Collections.Generic;
using System.Linq;

namespace BubbleTweaks.TrumpetGirl {
#if DEBUG
    [HarmonyPatch]
    class Tooltips {
        [HarmonyPatch(typeof(TooltipTemplateAbility), nameof(TooltipTemplateAbility.GetBody))]
        [HarmonyPostfix]
        public static void AbilityTooltip(TooltipTemplateAbility __instance, ref IEnumerable<ITooltipBrick> __result) {
            var list = __result.ToList();

            var guid = __instance.BlueprintAbility?.AssetGuidThreadSafe;
            list.Insert(0, new TooltipBrickText($"<color=grey>guid: {guid}</color>", TooltipTextType.Small | TooltipTextType.Italic));

            __result = list;
        }

        [HarmonyPatch(typeof(TooltipTemplateActivatableAbility), nameof(TooltipTemplateActivatableAbility.GetBody))]
        [HarmonyPostfix]
        public static void ActivatableAbilityTooltip(TooltipTemplateActivatableAbility __instance, ref IEnumerable<ITooltipBrick> __result) {
            var list = __result.ToList();

            var guid = __instance.BlueprintActivatableAbility?.AssetGuidThreadSafe;
            var guid2 = __instance.BlueprintActivatableAbility?.m_Buff?.Guid.ToString();
            list.Insert(0, new TooltipBrickText($"<color=grey>guid: {guid}\nbuff: {guid2}</color>", TooltipTextType.Small | TooltipTextType.Italic));

            __result = list;
        }

        [HarmonyPatch(typeof(TooltipTemplateItem), nameof(TooltipTemplateItem.GetBody))]
        [HarmonyPostfix]
        public static void ItemTooltip(TooltipTemplateItem __instance, ref IEnumerable<ITooltipBrick> __result) {
            var list = __result.ToList();

            var guid = __instance.m_BlueprintItem?.AssetGuidThreadSafe ?? __instance.m_Item?.Blueprint?.AssetGuidThreadSafe;
            list.Insert(0, new TooltipBrickText($"<color=grey>guid: {guid}</color>", TooltipTextType.Small | TooltipTextType.Italic));

            __result = list;
        }

        [HarmonyPatch(typeof(TooltipTemplateBuff), nameof(TooltipTemplateBuff.GetBody))]
        [HarmonyPostfix]
        public static void BuffTooltip(TooltipTemplateBuff __instance, ref IEnumerable<ITooltipBrick> __result) {
            var list = __result.ToList();

            var guid = __instance.Buff?.Blueprint?.AssetGuidThreadSafe;
            list.Insert(0, new TooltipBrickText($"<color=grey>guid: {guid}</color>", TooltipTextType.Small | TooltipTextType.Italic));

            __result = list;
        }
    }
#endif
}
