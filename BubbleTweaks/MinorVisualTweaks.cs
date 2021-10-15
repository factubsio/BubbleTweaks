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

namespace BubbleTweaks {

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
