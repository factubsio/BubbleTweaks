using UniRx;
using DG.Tweening;
using BubbleTweaks.Utilities;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Kingmaker.UnitLogic;
using UnityEngine.UI.Extensions;
using HarmonyLib;
using System.Reflection.Emit;
using Kingmaker.UI.MVVM._VM.Party;
using Kingmaker.UI.MVVM._PCView.Party;
using Kingmaker.UI;
using Owlcat.Runtime.UI.Controls.Selectable;
using Owlcat.Runtime.UI.Controls.Other;
using System.Reflection;
using Kingmaker.Blueprints.Root;
using Owlcat.Runtime.UI.Controls.Button;

namespace BubbleTweaks {
    public class LuckMeter {

        private static RollChecker checker = new();

        public static void Install() {
            EventBus.Subscribe(checker);
        }

        public static void Uninstall() {
            EventBus.Unsubscribe(checker);
            if (Root != null)
                GameObject.Destroy(Root);
        }

        private static GameObject Root;

        private class Bucket {
            public int Min, Max;

            public int Size => (Max - Min) + 1;

            public string Label => Size > 1 ? $"{Min}-{Max}" : Min.ToString();

            public Action<float> Render;
            public int Count = 0;

            public void Update(int currentMax) {
                if (currentMax == 0)
                    Render(0);
                else
                    Render(Count / (float)currentMax);
            }
        }

        private static List<Bucket> Buckets = new();

        private static Bucket[] IndexedBuckets = new Bucket[21];

        private static int MaxBucket => Buckets.Max(b => b.Count);
        public static void UpdateBuckets() {
            var max = MaxBucket;
            foreach (var bucket in Buckets)
                bucket.Update(max);
        }

        public static void Add(int roll) {
            IndexedBuckets[roll].Count += 1;
            UpdateBuckets();
        }

        private static void MakeBuckets(int count) {
            int nonEdge = count - 2;

            int perBucket = 18 / nonEdge;
            if (perBucket * nonEdge != 18) {
                Main.Log("Illegal bucket setup");
                return;
            }

            Buckets.Add(new Bucket {
                Min = 1,
                Max = 1
            });
            IndexedBuckets[1] = Buckets.Last();

            for (int i = 2; i <= 18; i += perBucket) {
                var bucket = new Bucket {
                    Min = i,
                    Max = i + perBucket - 1,
                };
                Buckets.Add(bucket);
                for (int b = i; b < i + perBucket; b++)
                    IndexedBuckets[b] = bucket;
            }

            Buckets.Add(new Bucket {
                Min = 20,
                Max = 20
            });
            IndexedBuckets[20] = Buckets.Last();
        }

        internal static void InstallUI() {

            if (Root != null)
                return;
            MakeBuckets(5);

            var labelPrefab = Game.Instance.UI.Canvas.transform.Find("HUDLayout/CombatLog_New/TooglePanel/ToogleAll/ToogleAll").gameObject;
            var bgSprite = Game.Instance.UI.Canvas.transform.Find("HUDLayout/CombatLog_New/Panel/Background/Background_Image").GetComponent<Image>().sprite;

            Root = new GameObject("bubble-luckmeter-root", typeof(RectTransform));
            Root.MakeComponent<Image>(img => {
                img.sprite = bgSprite;
            });
            Root.AddTo(Game.Instance.UI.Canvas.transform);

            const int height = 150;
            const int pad = 25;
            const int barMax = height - pad - 10;

            var rect = Root.transform as RectTransform;
            rect.pivot = new Vector2(0, 1);
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = rect.anchorMin;
            rect.anchoredPosition = new Vector2(140, 0);
            rect.sizeDelta = new Vector2(220, height);

            Root.MakeComponent<HorizontalLayoutGroup>(h => {
                h.childControlHeight = false;
                h.childAlignment = TextAnchor.LowerLeft;
                h.padding.bottom = pad;
                h.spacing = 3;
            });


            foreach (var bucket in Buckets) {
                MakeBar(bucket);
            }

            void MakeBar(Bucket bucket) {
                var bar = new GameObject("bubble-luckmeter-bar", typeof(RectTransform));
                Color c;
                if (bucket.Min == 1)
                    c = new Color(1, .2f, .2f, 0.5f);
                else if (bucket.Max == 20)
                    c = new Color(.2f, 1f, .2f, 0.5f);
                else
                    c = new Color(0.4f, 0.4f, 0.4f, 0.5f);

                bar.AddComponent<Image>().color = c;
                bar.AddTo(rect);

                var barRect = bar.transform as RectTransform;
                barRect.sizeDelta = Vector2.zero;

                var label = GameObject.Instantiate(labelPrefab, bar.transform);
                var labelRect = label.transform as RectTransform;
                labelRect.anchorMin = new Vector2(0f, 0);
                labelRect.anchorMax = new Vector2(1f, 0);
                labelRect.anchoredPosition = new Vector2(0, -15);
                labelRect.pivot = new Vector2(0.5f, 1);

                label.GetComponent<TextMeshProUGUI>().color = Color.black;
                label.GetComponent<TextMeshProUGUI>().text = bucket.Label;

                bucket.Render = v => {
                    barRect.DOSizeDelta(new Vector2(0, barMax * v), 0.9f);
                };
            }
        }

        internal static void Show() {
            InstallUI();
        }
    }

    public class RollChecker :
        IAttackHandler, IGlobalRulebookHandler<RuleSavingThrow> {

        void IAttackHandler.HandleAttackHitRoll(RuleAttackRoll rollAttackHit) {
            if (rollAttackHit.Initiator.IsInCompanionRoster()) {
                LuckMeter.Add(rollAttackHit.D20.Result);
            }
        }

        void IRulebookHandler<RuleSavingThrow>.OnEventAboutToTrigger(RuleSavingThrow evt) { }

        void IRulebookHandler<RuleSavingThrow>.OnEventDidTrigger(RuleSavingThrow evt) {
            if (evt.Initiator.IsInCompanionRoster()) {
                LuckMeter.Add(evt.D20.Result);
            }
        }
    }

    [HarmonyPatch(typeof(PartyPCView))]
    static class PartyPCView_Patches {
        private static Sprite hudBackground8;
        private static int leadingEdge;
        private static float itemWidth;
        private static float firstX;
        private static float scale;

        [HarmonyPatch("Initialize"), HarmonyPrefix]
        static void Initialize(PartyPCView __instance) {
            if (PartyVM_Patches.SupportedSlots == 6)
                return;

            try {


                Main.Log("INSTALLING BUBBLE GROUP PANEL");

                if (hudBackground8 == null) {
                    hudBackground8 = AssetLoader.LoadInternal("sprites", "UI_HudBackgroundCharacter_8.png", new Vector2Int(1746, 298));
                }

                __instance.transform.Find("Background").GetComponent<Image>().sprite = hudBackground8;

                scale = 6 / (float)8;
                itemWidth = 94.5f;
                __instance.m_Shift = itemWidth;

                var currentViews = __instance.GetComponentsInChildren<PartyCharacterPCView>(true);
                List<GameObject> toTweak = new(currentViews.Select(view => view.gameObject));
                firstX = toTweak[0].transform.localPosition.x - 9.5f;

                UpdateCharacterBindings(__instance);

            } catch (Exception e) {
                Main.Error(e, "party view initialize");
            }
        }

        [HarmonyPatch(nameof(PartyPCView.UpdateCharacterBindings)), HarmonyPostfix]
        static void UpdateCharacterBindings(PartyPCView __instance) {
            if (PartyVM_Patches.SupportedSlots == 6) return;

            var currentViews = __instance.GetComponentsInChildren<PartyCharacterPCView>(true);
            List<GameObject> toTweak = new(currentViews.Select(view => view.gameObject));
            firstX = toTweak[0].transform.localPosition.x - 9.5f;

            for (int i = 0; i < toTweak.Count; i++) {
                GameObject view = toTweak[i];

                TweakPCView(__instance, i, view);
            }
        }

        private static void TweakPCView(PartyPCView __instance, int i, GameObject view) {
            var viewRect = view.transform as RectTransform;

            if (viewRect.localScale.x <= (scale + 0.01f)) return;

            var pos = viewRect.localPosition;
            pos.x = firstX + (i * itemWidth);
            viewRect.localPosition = pos;
            viewRect.localScale = new Vector3(scale, scale, 1);

            var portraitRect = view.transform.Find("Portrait") as RectTransform;
            const float recaleFactor = 1.25f;
            portraitRect.localScale = new Vector3(recaleFactor, recaleFactor, 1);

            var frameRect = view.transform.Find("Frame") as RectTransform;
            frameRect.pivot = new Vector2(.5f, 1);
            frameRect.anchoredPosition = new Vector2(0, 23);
            frameRect.sizeDelta = new Vector2(0, 47);

            var healthBarRect = view.transform.Find("Health") as RectTransform;
            healthBarRect.pivot = new Vector2(0, 1);
            healthBarRect.anchoredPosition = new Vector2(0, -2);
            healthBarRect.anchorMin = new Vector2(0, 1);
            healthBarRect.anchorMax = new Vector2(0, 1);
            healthBarRect.localScale = new Vector2(recaleFactor, recaleFactor);

            var hitpointRect = view.transform.Find("HitPoint") as RectTransform;
            var hpPos = hitpointRect.anchoredPosition;
            hpPos.y -= 20;
            hitpointRect.anchoredPosition = hpPos;

            view.transform.Find("PartBuffView").gameObject.SetActive(false);

            (view.transform.Find("Frame/Selected/Mark") as RectTransform).anchoredPosition = new Vector2(0, 94);

            var buffRect = view.transform.Find("BuffMain") as RectTransform;

            buffRect.sizeDelta = new Vector2(-8, 24);
            buffRect.pivot = new Vector2(0, 0);
            buffRect.anchorMin = new Vector2(0, 1);
            buffRect.anchorMax = new Vector2(1, 1);
            buffRect.anchoredPosition = new Vector2(4, -4);
            buffRect.Edit<GridLayoutGroupWorkaround>(g => {
                g.constraint = GridLayoutGroup.Constraint.FixedRowCount;
                g.padding.top = 2;
            });
            buffRect.gameObject.AddComponent<Image>().color = new Color(.05f, .05f, .05f);

            var buffHover = buffRect.Find("BuffTriggerNotification").GetComponent<OwlcatSelectable>();

            __instance.AddDisposable(buffHover.OnHoverAsObservable().Subscribe<bool>(selected => {
                viewRect.SetAsLastSibling();
            }));

            buffRect.Find("BuffTriggerNotification/BuffAdditional/").localScale = new Vector2(1.25f, 1.25f);
        }
    }

    //[HarmonyPatch(typeof(UnitBuffPartPCView))]
    //static class UnitBuffPartPCView_Patches {

    //    private static HashSet<Guid> Dreamwork = new() {
    //        // Teamwork buffs
    //        Guid.Parse("44569e9e95364bf42b1071382a8a89da"),
    //        Guid.Parse("a6298b0f87fc7694086cd8eac9d6a2aa"),
    //        Guid.Parse("cc26546e4f73fe142b606b4759b4eb18"),
    //        Guid.Parse("e5079510480031146992dafde835c3b8"),
    //        Guid.Parse("3de0359d9480cb549ab6cf1eac51f9dc"),
    //        Guid.Parse("2f5768f642de59f40acd5211a627a237"),
    //        Guid.Parse("965ea9716b87f4b46a6a8f50523393bd"),
    //        Guid.Parse("693964e674883e74b8d0005dbf4a4e6b"),
    //        Guid.Parse("731a11dcc952e744f8a88768e07a0542"),
    //        Guid.Parse("953c3dbda63dcdb4aad6c54c1a4590d0"),
    //        Guid.Parse("9c179de4894c295499822714878f3590"),
    //        Guid.Parse("c7223802e54e8524c8b1e5c71df22f7b"),

    //        // Toggles (power attack, rapid shot)
    //        Guid.Parse("0f310c1e709e15e4fa693db15a4baeb4"),
    //        Guid.Parse("f958ef62eea5050418fb92dfa944c631"),
    //        Guid.Parse("8af258b1dd322874ba6047b0c24660c7"),
    //        Guid.Parse("bf3b19ed9c919464aa2a741271718542"),

    //    };


    //    [HarmonyPatch("DrawBuffs"), HarmonyPostfix]
    //    static void DrawBuffs(UnitBuffPartPCView __instance) {
    //        try {
    //            if (PartyVM_Patches.SupportedSlots == 6)
    //                return;

    //            if (__instance.ViewModel.Buffs.Count <= 6)
    //                return;

    //            var main = __instance.m_MainContainer.transform;
    //            var overflow = __instance.m_AdditionalContainer.transform;

    //            int[] badButShown = new int[5];
    //            int[] goodButHidden = new int[5];
    //            int nextShown = 0;
    //            int nextHidden = 0;

    //            for (int i = 0; i < __instance.m_BuffList.Count; i++) {
    //                var buff = __instance.m_BuffList[i].ViewModel.Buff;
    //                if (nextShown < 5 && Dreamwork.Contains(buff.Blueprint.AssetGuid.m_Guid)) {
    //                    badButShown[nextShown++] = i;
    //                } else if (nextHidden < 5 && __instance.m_BuffList[i].transform.parent == overflow) {
    //                    goodButHidden[nextHidden++] = i;
    //                }
    //            }

    //            if (nextHidden == 0 || nextShown == 0)
    //                return;

    //            Vector3 overflowScale = __instance.m_BuffList[goodButHidden[0]].transform.localScale;
    //            Vector3 mainScale = __instance.m_BuffList[badButShown[0]].transform.localScale;


    //            while (nextHidden > 0 && nextShown > 0) {
    //                nextHidden--;
    //                nextShown--;

    //                __instance.m_BuffList[badButShown[nextShown]].transform.SetParent(overflow);
    //                __instance.m_BuffList[badButShown[nextShown]].transform.localScale = overflowScale;
    //                __instance.m_BuffList[goodButHidden[nextHidden]].transform.SetParent(main);
    //                __instance.m_BuffList[goodButHidden[nextHidden]].transform.localScale = mainScale;
    //            }

    //            while (main.childCount > 6) {
    //                var toSwap = main.transform.GetChild(6);
    //                toSwap.transform.SetParent(overflow);
    //                toSwap.localScale = overflowScale;
    //            }
    //        } catch (Exception ex) {
    //            Main.Error(ex, "buffling");
    //        }
    //    }
    //}

    //[HarmonyPatch(typeof(PartyVM))]
    public static class PartyVM_Patches {

        public static int SupportedSlots = 6;

        private static int WantedSlots = 6;

        //[HarmonyTranspiler]
        //[HarmonyPatch("set_StartIndex")]
        static IEnumerable<CodeInstruction> UpdateStartValue(IEnumerable<CodeInstruction> instructions) {
            return ConvertConstants(instructions, 8);
        }

        //[HarmonyTranspiler]
        //[HarmonyPatch(MethodType.Constructor)]
        static IEnumerable<CodeInstruction> _ctor(IEnumerable<CodeInstruction> instructions) {
            return ConvertConstants(instructions, 8);
        }

        private static OpCode[] LdConstants = {
            OpCodes.Ldc_I4_0,
            OpCodes.Ldc_I4_1,
            OpCodes.Ldc_I4_2,
            OpCodes.Ldc_I4_3,
            OpCodes.Ldc_I4_4,
            OpCodes.Ldc_I4_5,
            OpCodes.Ldc_I4_6,
            OpCodes.Ldc_I4_7,
            OpCodes.Ldc_I4_8,
        };

        private static IEnumerable<CodeInstruction> ConvertConstants(IEnumerable<CodeInstruction> instructions, int to) {
            Func<CodeInstruction> makeReplacement;
            if (to <= 8)
                makeReplacement = () => new CodeInstruction(LdConstants[to]);
            else
                makeReplacement = () => new CodeInstruction(OpCodes.Ldc_I4_S, to);

            foreach (var ins in instructions) {
                if (ins.opcode == OpCodes.Ldc_I4_6)
                    yield return makeReplacement();
                else
                    yield return ins;
            }
        }


        private static readonly BubblePatch Patch_ctor = BubblePatch.FirstConstructor(typeof(PartyVM), typeof(PartyVM_Patches));
        private static readonly BubblePatch Patch_set_StartIndex = BubblePatch.Method(typeof(PartyVM), typeof(PartyVM_Patches), "UpdateStartValue");

        public static void Repatch() {
            //if (BubbleSettings.Instance.PartyViewWith8Slots.GetValue())
            //    WantedSlots = 8;
            //else
                WantedSlots = 6;

            if (SupportedSlots == WantedSlots)
                return;

            Patch_ctor.Revert();
            Patch_set_StartIndex.Revert();

            if (WantedSlots == 6)
                return;

            SupportedSlots = 8;

            Patch_ctor.Apply();
            Patch_set_StartIndex.Apply();
        }
    }

    public class BubblePatch {
        public MethodBase Original;
        public HarmonyMethod Patch;
        public bool IsPatched = false;

        public BubblePatch(MethodBase target, Type patcher, string name) {
            Patch = new HarmonyMethod(patcher, name);
            Original = target;
        }

        public static BubblePatch FirstConstructor(Type target, Type patcher) {
            return new BubblePatch(target.GetConstructors().First(), patcher, "_ctor");
        }
        public static BubblePatch Setter(Type target, Type patcher, string propertyName) {
            return new BubblePatch(target.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SetMethod, patcher, $"set_{propertyName}");
        }
        public static BubblePatch Getter(Type target, Type patcher, string propertyName) {
            return new BubblePatch(target.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetMethod, patcher, $"get_{propertyName}");
        }
        public static BubblePatch Method(Type target, Type patcher, string name) {
            return new BubblePatch(target.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance), patcher, name);
        }

        public void Revert() {
            if (!IsPatched)
                return;

            Main.harmony.Unpatch(Original, Patch.method);
            IsPatched = false;
        }
        public void Apply() {
            if (IsPatched)
                throw new Exception("Trying to apply a patch that is already applied");
            Main.harmony.Patch(Original, null, null, Patch, null);
            IsPatched = true;
        }
    }

}
