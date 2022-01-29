using BubbleTweaks.Utilities;
using HarmonyLib;
using Kingmaker.UI.MVVM._PCView.Party;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTweaks {
    public static class StatusConditions {
        public static bool Installed = false;
        public static void Install() {
            Installed = true;

        }
        public static void Uninstall() {
            Installed = false;

        }
    }

    [HarmonyPatch(typeof(PartyCharacterPCView))]
    public static class ViewPatches {

        private static readonly ModIcon GutterBG = new("condition_bg", 32, 256);

        private static ModIcon ico(string name) => new("Ico_status_" + name, 42, 42);

        private static readonly ModIcon StatusConfused = ico("confused");
        private static readonly ModIcon StatusStunned = ico("stunned");
        private static readonly ModIcon StatusScared = ico("frightened");
        private static readonly ModIcon StatusParalyzed = ico("paralyzed");
        private static readonly ModIcon StatusCannotMove = ico("stuck");
        private static readonly ModIcon StatusDominated = ico("dominated");

        [HarmonyPatch(nameof(PartyCharacterPCView.Initialize)), HarmonyPostfix]
        public static void Initialize(PartyCharacterPCView __instance) {
            if (!StatusConditions.Installed)
                return;

            var root = __instance.m_PortraitView.transform;

            var bg = new GameObject("condition-gutter", typeof(RectTransform));
            bg.Rect().sizeDelta = new Vector2(21, 0);
            bg.AddTo(root);
            bg.Rect().anchorMin = new Vector2(1.5f, -.5f);
            bg.Rect().anchorMax  = new Vector2(1.5f, .5f);
            bg.Rect().pivot = new Vector2(1, 0.5f);
            bg.AddComponent<Image>().sprite = GutterBG.Sprite;

            var grid = bg.AddComponent<GridLayoutGroup>();
            grid.constraintCount = 1;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.padding.top = 4;
            grid.padding.right = 2;
            grid.childAlignment = TextAnchor.UpperRight;

            grid.cellSize = new(14, 14);
            grid.spacing = new(0, 2);

            var test = new GameObject("test", typeof(RectTransform));
            test.AddComponent<Image>().sprite = StatusConfused.Sprite;
            test.AddTo(bg);

            var test2 = new GameObject("test", typeof(RectTransform));
            test2.AddComponent<Image>().sprite = StatusParalyzed.Sprite;
            test2.AddTo(bg);


        }
    }
}
