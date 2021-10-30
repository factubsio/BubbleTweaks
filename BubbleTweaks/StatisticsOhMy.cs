using HarmonyLib;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI.MVVM._PCView.ServiceWindows.CharacterInfo;
using Kingmaker.UI.MVVM._PCView.ServiceWindows.CharacterInfo.Menu;
using Kingmaker.UI.MVVM._PCView.ServiceWindows.CharacterInfo.Sections.Abilities;
using Kingmaker.UI.MVVM._VM.ServiceWindows.CharacterInfo;
using Kingmaker.UI.MVVM._VM.ServiceWindows.CharacterInfo.Menu;
using Kingmaker.UI.MVVM._VM.ServiceWindows.CharacterInfo.Sections.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.Utility;
using Kingmaker.View.MapObjects.Traps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using BubbleTweaks.Extensions;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using System.Reflection;
using BubbleTweaks.Utilities;
using Kingmaker.EntitySystem.Persistence;
using System.IO;
using System.Collections;
using Kingmaker.UnitLogic.Abilities;

namespace BubbleTweaks {

    public struct CharInfoPageType_EXT {
        public static CharInfoPageType Statistics = (CharInfoPageType)100;
    }
    public struct CharInfoComponentType_EXT {
        public static CharInfoComponentType Statistics = (CharInfoComponentType)100;
    }

    [HarmonyPatch(typeof(CharacterInfoVM))]
    static class CharacterInfoVM_Patches {
        [HarmonyPostfix, HarmonyPatch(MethodType.Constructor)]
        static void _ctor(CharacterInfoVM __instance) {
            __instance.ComponentVMs[CharInfoComponentType_EXT.Statistics] = new();
        }

        [HarmonyPostfix, HarmonyPatch("CreateVM")]
        static void CreateVM(CharInfoComponentType type, CharacterInfoVM __instance, ref CharInfoComponentVM __result) {
            if (type == CharInfoComponentType_EXT.Statistics)
                __result = new CharInfoStatisticsVM(__instance.UnitDescriptor);
        }

    };

    [HarmonyPatch(typeof(CharInfoWindowUtility))]
    static class CharInfoWindowUtility_Patches {
        [HarmonyPatch("GetPageLabel"), HarmonyPostfix]
        static void GetPageLabel(CharInfoPageType page, ref string __result) {
            if (page == CharInfoPageType_EXT.Statistics)
                __result = "Statistics";
        }
    }

    class CharInfoStatisticsVM : CharInfoComponentVM {
        public CharInfoStatisticsVM(IReactiveProperty<UnitDescriptor> unit) : base(unit) {
        }
    }

    public class Once {
        private bool _Run;
        public void Run(Action task) {
            if (_Run) return;
            task();
            _Run = true;
        }
    }

    public class StatDisplay<T> {
        public readonly int Column;
        public readonly string Name;
        public Func<T, string> Get;
        public TextMeshProUGUI Label;
        public bool HasValue => Get != null;

        public StatDisplay(string name, Func<T, string> get, int column) {
            Column = column;
            Name = name;
            Get = get;
        }

        internal int ColumnOr(int col) {
            return Column != -1 ? Column : col;
        }
    }
    public static class BubbleDisplayExtensions {
    }

    class CharInfoStatisticsPCView : CharInfoComponentView<CharInfoStatisticsVM> {
        public override void BindViewImplementation() {
            base.BindViewImplementation();
        }

        private List<StatDisplay<CharacterRecord>> Stats = new();

        private void TryBuildUI() {
            Main.Log("Building view");
            var spacer = new GameObject("spacer", typeof(RectTransform));
            spacer.MakeComponent<LayoutElement>(e => {
                e.minWidth = 0;
                e.preferredWidth = 0;
            });

            var linePrefab = new GameObject("line", typeof(RectTransform));
            linePrefab.MakeComponent<Image>(img => {
                img.sprite = AssetLoader.LoadInternal("sprites", "line.png", new Vector2Int(128, 8));
                img.color = new Color(0.2f, 0.2f, 0.2f, 0.25f);
            });
            linePrefab.MakeComponent<LayoutElement>(e => {
                e.preferredHeight = 8;
                e.flexibleWidth = 100000;
            });
            linePrefab.transform.localScale = new Vector3(1, 0.2f, 1);

            var titlePrefab = GameObject.Instantiate(LabelPrefab).GetComponent<TextMeshProUGUI>();
            titlePrefab.alignment = TextAlignmentOptions.Left;
            titlePrefab.fontStyle = FontStyles.Bold;
            //titlePrefab.gameObject.AddComponent<LayoutElement>().minWidth = 200;

            var headingPrefab = GameObject.Instantiate(HeadingPrefab).GetComponent<TextMeshProUGUI>();
            headingPrefab.alignment = TextAlignmentOptions.Left;
            //headingPrefab.fontStyle = FontStyles.Bold;
            headingPrefab.fontSize = titlePrefab.fontSize + 4;
            headingPrefab.fontSizeMin = titlePrefab.fontSizeMin + 4;

            var valuePrefab = GameObject.Instantiate(LabelPrefab).GetComponent<TextMeshProUGUI>();
            valuePrefab.alignment = TextAlignmentOptions.Right;
            //valuePrefab.gameObject.AddComponent<LayoutElement>().minWidth = 40;

            GetComponent<CanvasGroup>().blocksRaycasts = true;

            var content = transform.Find("StandardScrollView/Viewport/Content");

            Stats = BubbleDisplay.GetDisplays<CharacterRecord>().ToList();

            //pad the top a bit
            var topPad = new GameObject("top-pad", typeof(RectTransform));
            topPad.AddComponent<HorizontalLayoutGroup>();
            topPad.AddComponent<LayoutElement>().preferredHeight = 13;
            topPad.AddTo(content);

            var columns = new GameObject("columns", typeof(RectTransform));
            columns.MakeComponent<HorizontalLayoutGroup>(g => {
                g.childControlHeight = false;
                g.childControlWidth = true;
                g.spacing = 40;
            });


            var col0 = new GameObject("col0", typeof(RectTransform));
            col0.MakeComponent<VerticalLayoutGroup>(g => {
                g.spacing = 4;
            });
            col0.MakeComponent<ContentSizeFitter>(f => {
                f.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            });

            var col1 = new GameObject("col1", typeof(RectTransform));
            col1.MakeComponent<VerticalLayoutGroup>(g => {
                g.spacing = 4;
            });
            col1.MakeComponent<ContentSizeFitter>(f => {
                f.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            });


            AddHorizontalSpace(columns.transform, 0);
            col0.AddTo(columns);
            AddHorizontalSpace(columns.transform, 20);
            col1.AddTo(columns);
            AddHorizontalSpace(columns.transform, 0);

            //col0.AddComponent<Image>().color = Color.red;
            //col1.AddComponent<Image>().color = Color.blue;

            //col1.MakeComponent<LayoutElement>(e => {
            //    e.preferredHeight = 900;
            //    e.minWidth = 100;
            //});
            //col0.MakeComponent<LayoutElement>(e => {
            //    e.preferredHeight = 1400;
            //    e.minWidth = 100;
            //});

            //int r = 0;

            Transform[] cols = { col0.transform, col1.transform, content.transform };
            foreach (var stat in Stats) {
                stat.Label = AddRow(stat.Name, stat.Get == null, cols[stat.ColumnOr(0)]);
            }

            columns.AddTo(content);

            static void AddHorizontalSpace(Transform to, int amount) {
                var spacer = new GameObject("spacer", typeof(RectTransform));
                spacer.MakeComponent<LayoutElement>(e => {
                    e.minWidth = 0;
                    e.preferredWidth = 0;
                });
                spacer.AddTo(to);
            }

            static int GetRowHeight(string title, bool heading) {
                if (heading) {
                    if (title.Length == 0)
                        return 14;
                    else
                        return 33;
                } else {
                    return 22;
                }



            }


            TextMeshProUGUI AddRow(string title, bool heading, Transform to) {
                if (heading && title.Length > 0) {
                    AddRow("", true, to);
                }
                var row = new GameObject("row", typeof(RectTransform));
                row.AddComponent<HorizontalLayoutGroup>();


                row.MakeComponent<LayoutElement>(l => {
                    l.preferredHeight = GetRowHeight(title, heading);
                    l.minHeight = GetRowHeight(title, heading);
                });
                row.AddTo(to);
                //if ((r++ % 2) == 0)
                //    row.AddComponent<Image>().color = Color.yellow;
                //else
                //    row.AddComponent<Image>().color = Color.green;

                string titleText = heading ? title.MakeTitle() : ("  " + title);

                GameObject.Instantiate(heading ? headingPrefab : titlePrefab, row.transform).GetComponent<TextMeshProUGUI>().text = titleText;

                if (!heading) {
                    GameObject.Instantiate(linePrefab, row.transform);
                }

                var label = GameObject.Instantiate(valuePrefab, row.transform).GetComponent<TextMeshProUGUI>();
                label.text = heading ? "" : "-";

                return label;

            }
        }

        private readonly Once MakeUI = new();


        private GameObject LabelPrefab => Game.Instance.UI.Canvas.transform.Find("ServiceWindowsPCView/CharacterInfoPCView/CharacterScreen/LevelClassScores/RaceGenderAlighment/Alignment/Alignment").gameObject;
        private GameObject HeadingPrefab => Game.Instance.UI.Canvas.transform.Find("ServiceWindowsPCView/CharacterInfoPCView/CharacterScreen/NamePortrait/CharName/CharacterName").gameObject;

        private void UpdateCharacter() {
            var record = GlobalRecord.Instance.ForCharacter(ViewModel.Unit.Value);
            foreach (var stat in Stats.Where(s => s.HasValue))
                stat.Label.text = stat.Get(record) ?? "-";
        }

        public override void RefreshView() {
            base.RefreshView();

            MakeUI.Run(TryBuildUI);

            Main.Safely(UpdateCharacter);
        }


    }

    [HarmonyPatch(typeof(CharacterInfoPCView))]
    static class CharacterInfoPCView_Patches {
        [HarmonyPatch("BindViewImplementation"), HarmonyPrefix]
        static void BindViewImplementation(CharacterInfoPCView __instance) {
            try {
                if (!__instance.m_ComponentViews.ContainsKey(CharInfoComponentType_EXT.Statistics)) {
                    Main.Log($"abilities parent: { __instance.m_AbilitiesView.transform.parent.name}");
                    var prefab = __instance.m_AbilitiesView.gameObject;
                    Main.Log("got prefab");

                    var statsGameObject = GameObject.Instantiate(prefab, prefab.transform.parent);
                    Main.Log("made stats view");
                    var statsView = statsGameObject.AddComponent<CharInfoStatisticsPCView>();
                    Main.Log("added component");

                    statsGameObject.GetComponent<CanvasGroup>().alpha = 1.0f;

                    GameObject.DestroyImmediate(statsGameObject.GetComponent<CharInfoAbilitiesPCView>());
                    Main.Log("destroy old component");
                    var toRemove = statsGameObject.GetComponentsInChildren<CharInfoFeatureGroupPCView>();
                    foreach (var r in toRemove)
                        GameObject.DestroyImmediate(r.gameObject);
                    Main.Log("and subobjects");

                    __instance.m_ComponentViews[CharInfoComponentType_EXT.Statistics] = statsView;
                }
            } catch (Exception ex) {
                Main.Error(ex, "injecting statistics view");
            }

        }
    }


    [HarmonyPatch]
    static class SaveHooker {

        [HarmonyPatch(typeof(ZipSaver))]
        [HarmonyPatch("SaveJson"), HarmonyPostfix]
        static void Zip_Saver(string name, ZipSaver __instance) {
            DoSave(name, __instance);
        }
        
        [HarmonyPatch(typeof(FolderSaver))]
        [HarmonyPatch("SaveJson"), HarmonyPostfix]
        static void Folder_Saver(string name, FolderSaver __instance) {
            DoSave(name, __instance);
        }

        static void DoSave(string name, ISaver saver) {
            if (name != "header")
                return;

            Main.Safely(() => {
                var serializer = new JsonSerializer();
                serializer.Formatting = Formatting.Indented;
                var writer = new StringWriter();
                serializer.Serialize(writer, GlobalRecord.Instance);
                writer.Flush();
                saver.SaveJson(LoadHooker.FileName, writer.ToString());
                Main.Log("save complete...");
            });
        }
    }

    [HarmonyPatch(typeof(Game))]
    static class LoadHooker {
        public const string FileName = "header.json.bubble_statistics";

        [HarmonyPatch("LoadGame"), HarmonyPostfix]
        static void LoadGame(SaveInfo saveInfo) {
            Main.Log("loading...");
            LoadingProcess.Instance.StartLoadingProcess(LoadRoutine(saveInfo), null, LoadingProcessTag.None);
        }


        private static IEnumerator LoadRoutine(SaveInfo saveInfo) {
            Main.Safely(() => {
                var serializer = new JsonSerializer();
                var raw = saveInfo.Saver.ReadJson(FileName);
                if (raw != null) {
                    var rawReader = new StringReader(raw);
                    var jsonReader = new JsonTextReader(rawReader);
                    GlobalRecord.Instance = serializer.Deserialize<GlobalRecord>(jsonReader);
                } else {
                    GlobalRecord.Instance = new GlobalRecord();
                }
            });
            yield return null;
        }
    }

    [HarmonyPatch(typeof(CharInfoMenuSelectorPCView))]
    static class CharInfoMenuSelectorPCView_Patches {
        [HarmonyPatch("Initialize"), HarmonyPrefix]
        static void Initialize(CharInfoMenuSelectorPCView __instance) {
            if (__instance.m_MenuEntities.Empty()) {
                var prefab = __instance.GetComponentInChildren<CharInfoMenuEntityPCView>().gameObject;
                GameObject.Instantiate(prefab, __instance.gameObject.transform);
            }
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class BubbleDisplayable : Attribute { }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class BubbleTitle : Attribute {
        public readonly string Name;
        public readonly int Column;

        public BubbleTitle(string name, int column = -1) {
            Name = name;
            Column = column;
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class BubbleDisplay : Attribute {
        public readonly string Name;
        public readonly int Order;
        public readonly int Column;


        public BubbleDisplay(int order, string name, int column = -1) {
            Order = order;
            Name = name;
            Column = column;
        }
        public static IEnumerable<(MemberInfo, BubbleDisplay, BubbleTitle)> GetDisplayedMembers(Type type) {
            return GetDisplayedMembersRaw(type).OrderBy(m => m.Item2.Order).ThenBy(m => m.Item1.MetadataToken);
        }

        public static IEnumerable<(MemberInfo, BubbleDisplay, BubbleTitle)> GetDisplayedMembersRaw(Type type) {
            foreach (MemberInfo member in type.GetMembers()) {
                var display = member.GetCustomAttribute<BubbleDisplay>();
                if (display == null) continue;
                var title = member.GetCustomAttribute<BubbleTitle>();
                yield return (member, display, title);
            }
        }

        public static IEnumerable<StatDisplay<T>> GetDisplays<T>() {
            foreach (var (member, display, title) in GetDisplayedMembers(typeof(T))) {
                if (title != null) {
                    yield return new StatDisplay<T>(title.Name, null, title.Column);
                }

                if (member is FieldInfo field) {
                    if (field.FieldType.GetCustomAttribute<BubbleDisplayable>() != null) {
                        yield return new StatDisplay<T>(display.Name, null, display.Column);
                        foreach (var (subMember, subDisplay, _) in GetDisplayedMembers(field.FieldType)) {
                            if (subMember is FieldInfo subField)
                                yield return new StatDisplay<T>(subDisplay.Name, obj => subField.GetValue(field.GetValue(obj))?.ToString(), subDisplay.ColumnOr(display.Column));
                            else if (subMember is PropertyInfo subProp)
                                yield return new StatDisplay<T>(subDisplay.Name, obj => subProp.GetValue(field.GetValue(obj))?.ToString(), subDisplay.ColumnOr(display.Column));
                        }
                    }
                    else
                        yield return new StatDisplay<T>(display.Name, obj => field.GetValue(obj)?.ToString(), display.Column);

                } else if (member is PropertyInfo prop) {

                    if (prop.PropertyType.GetCustomAttribute<BubbleDisplayable>() != null) {
                        yield return new StatDisplay<T>(display.Name, null, display.Column);
                        foreach (var (subMember, subDisplay, _) in GetDisplayedMembers(prop.PropertyType)) {
                            if (subMember is FieldInfo subField)
                                yield return new StatDisplay<T>(subDisplay.Name, obj => subField.GetValue(prop.GetValue(obj))?.ToString(), subDisplay.ColumnOr(display.Column));
                            else if (subMember is PropertyInfo subProp)
                                yield return new StatDisplay<T>(subDisplay.Name, obj => subProp.GetValue(prop.GetValue(obj))?.ToString(), subDisplay.ColumnOr(display.Column));
                        }
                    }
                    else
                        yield return new StatDisplay<T>(display.Name, obj => prop.GetValue(obj)?.ToString(), display.Column);
                }
            }
        }

        private int ColumnOr(int column) {
            return Column != -1 ? Column : column;
        }
    }

    [BubbleDisplayable]
    public struct SavesRecord {
        [BubbleDisplay(0, "Critical passes")]
        public int PassedCrit;

        [BubbleDisplay(0, "Passes")]
        public int Passed;

        [BubbleDisplay(0, "Failures")]
        public int Failed;

        [BubbleDisplay(0, "Critical failures")]
        public int FailedCrit;
    }

    public class CharacterRecord {

        public int DamageDoneTotalWhilePresent;
        [BubbleTitle("Damage")]
        [BubbleDisplay(0, "Damage done")]
        public int DamageDone;

        private static string GetPercent(int top, int bottom) {
            if (bottom == 0) {
                return "0%";
            }
            float proportion = top / (float)bottom;
            int percent = (int)(proportion * 100);
            return $"{percent}%";
        }

        [BubbleDisplay(0, "Party damage done (active)")]
        [JsonIgnore]
        public string DamageDoneWhilePresentPercent => GetPercent(DamageDone, DamageDoneTotalWhilePresent);
        [BubbleDisplay(0, "Party damage done (overall)")]
        [JsonIgnore]
        public string DamageDonePercent => GetPercent(DamageDone, GlobalRecord.Instance.TotalDamage);

        [BubbleDisplay(1, "Friendly fire")]
        public int FriendlyDamageDealt;

        public int DamageTakenTotalWhilePresent;
        [BubbleDisplay(1, "Damage taken")]
        public int DamageTaken;

        [BubbleDisplay(1, "Biggest single hit")]
        public int BiggestHit;
        [BubbleDisplay(1, "  Against")]
        public string BiggestHitTarget;

        [BubbleTitle("Attacks")]
        [BubbleDisplay(2, "Attacks rolled")]
        public int AttacksTotal;

        [BubbleDisplay(2, "Hits")]
        public int AttacksHit;
        [BubbleDisplay(2, "Misses")]
        public int AttacksMissed;
        [BubbleDisplay(2, "Critical Hits")]
        public int AttacksCrit;
        [BubbleDisplay(2, "Critical Misses")]
        public int AttacksCriticallyMissed;

        public int AbilityDamageTaken;


        [BubbleTitle("Skill checks")]
        [BubbleDisplay(3, "Skill checks passes")]
        public int SkillChecksPassed;
        [BubbleDisplay(3, "Skill checks fails")]
        public int SkillChecksFailed;

        public SavesRecord[] Saves = new SavesRecord[Enum.GetValues(typeof(SavingThrowType)).Length];


        [JsonIgnore]
        public SavesRecord SavesTotal {
            get {
                SavesRecord total = new();
                foreach (var record in Saves) {
                    total.Failed += record.Failed;
                    total.FailedCrit += record.FailedCrit;
                    total.Passed += record.Passed;
                    total.PassedCrit += record.PassedCrit;
                }
                return total;
            }
        }

        public int SpellsResistedByMe;
        public int SpellsResistedBThem;

        [BubbleDisplay(3, "Traps triggered")]
        public int TrapsTriggered;

        public int TimesKilled;
        public int TimesDowned;

        public int MealsCooked;
        public int PotionsCreated;
        public int ScrollsCreated;
  
        public int TotalCorruption;
        public int MaxCorruption;

        public int HealingDoneTotalWhilePresent;

        [BubbleDisplay(4, "Healing done")]
        public int HealingDone;

        [BubbleDisplay(4, "Party healing done (active)")]
        [JsonIgnore]
        public string HealingDoneWhilePresentPercent => GetPercent(HealingDone, HealingDoneTotalWhilePresent);
        [BubbleDisplay(4, "Party healing done (overall)")]
        [JsonIgnore]
        public string HealingDonePercent => GetPercent(HealingDone, GlobalRecord.Instance.TotalHealing);

        [JsonIgnore]
        [BubbleDisplay(5, "Favourite weapon")]
        public string FavouriteWeapon => WeaponsUsed.OrderByDescending(kv => kv.Value).FirstOrDefault().Key;
        [JsonIgnore]
        [BubbleDisplay(5, "Favourite spell")]
        public string FavouriteSpell => SpellsCast.OrderByDescending(kv => kv.Value).FirstOrDefault().Key;

        public Dictionary<string, int> SpellsCast = new();
        public Dictionary<string, int> WeaponsUsed = new();

        [BubbleDisplay(0, "Fortitude saves", 1)]
        public SavesRecord FortSaves => Saves[(int)SavingThrowType.Fortitude];
        [BubbleDisplay(1, "Reflex saves", 1)]
        public SavesRecord ReflexSaves => Saves[(int)SavingThrowType.Reflex];
        [BubbleDisplay(2, "Will saves", 1)]
        public SavesRecord WillSaves => Saves[(int)SavingThrowType.Will];
        [BubbleDisplay(3, "Other saves", 1)]
        public SavesRecord OtherSaves => Saves[(int)SavingThrowType.Unknown];
    }

    static class CountingDictionaryExtensions {
        public static void Increment<T>(this Dictionary<T, int> dict, T key, int delta = 1) {
            if (dict.TryGetValue(key, out var val))
                dict[key] = val + delta;
            else
                dict[key] = delta;
        }
        public static void Decrement<T>(this Dictionary<T, int> dict, T key, int delta = -1) {
            dict.Increment(key, delta);
        }

        public delegate void ModifyStructInPlace<T>(ref T val);

        public static void ModifyStruct<T>(this T[] array, int index, ModifyStructInPlace<T> act) where T : struct {
            act(ref array[index]);
        }

        public static void GetStruct<K, V>(this Dictionary<K, V> dict, K key, Func<V, V> act) where V : struct {
            if (!dict.TryGetValue(key, out var val))
                val = default;
            dict[key] = act(val);
        }
        public static void GetObj<K, V>(this Dictionary<K, V> dict, K key, Action<V> act) where V : class {
            if (!dict.TryGetValue(key, out var val)) {
                val = default;
                dict[key] = val;
            }
            act(val);
        }
    }

    public class GlobalRecord {
        public Dictionary<string, CharacterRecord> PerCharacter = new();

        public CharacterRecord ForCharacter(UnitEntityData unit) {
            var key = unit.UniqueId;
            if (!PerCharacter.TryGetValue(key, out var record)) {
                record = new();
                PerCharacter[key] = record;
            }
            return record;
        }

        public int version = 1;

        public int TotalHealing;
        public int TotalDamage;
        public int TotalDamageTaken;

        public static GlobalRecord Instance = new();
    }

    class StatiscticsListener : IAttackHandler, IDamageHandler, IAttributeDamageHandler, IRollSkillCheckHandler, ITrapActivationHandler, IGlobalRulebookHandler<RuleSavingThrow>, IAbilityExecutionProcessHandler, IHealingHandler {

        static CharacterRecord For(UnitEntityData unit) => GlobalRecord.Instance.ForCharacter(unit);

        static void WhenFriend(UnitEntityData unit, Action<CharacterRecord> act) {
            if (unit != null && unit.IsInCompanionRoster()) {
                act(GlobalRecord.Instance.ForCharacter(unit));
            }
        }

        static void CurrentParty(Action<CharacterRecord> act) {
            foreach (var c in Game.Instance.Player.PartyAndPets) {
                act(GlobalRecord.Instance.ForCharacter(c));
            }

        }

        void IAttackHandler.HandleAttackHitRoll(RuleAttackRoll rollAttackHit) {
            WhenFriend(rollAttackHit.Initiator, record => {
                record.AttacksTotal++;

                record.WeaponsUsed.Increment(rollAttackHit.Weapon.Name);

                if (rollAttackHit.IsHit) {
                    record.AttacksHit++;
                    if (rollAttackHit.IsCriticalConfirmed)
                        record.AttacksCrit++;
                } else {
                    record.AttacksMissed++;
                    if (rollAttackHit.D20.Result == 1) {
                        record.AttacksCriticallyMissed++;
                    }
                }
            });
            throw new NotImplementedException();
        }

        void IAttributeDamageHandler.HandleAttributeDamage(UnitEntityData unit, StatType attribute, int oldDamage, int newDamage, bool drain) {
            WhenFriend(unit, record => {
                if (newDamage > oldDamage)
                    record.AbilityDamageTaken += newDamage - oldDamage;
            });
        }

        void IDamageHandler.HandleDamageDealt(RuleDealDamage dealDamage) {
            WhenFriend(dealDamage.Initiator, record => {
                record.DamageDone += dealDamage.Result;
                if (dealDamage.Result > record.BiggestHit) {
                    record.BiggestHit = dealDamage.Result;
                    record.BiggestHitTarget = dealDamage.Target.CharacterName;
                }

                if (dealDamage.Target.IsInCompanionRoster())
                    record.FriendlyDamageDealt += dealDamage.Result;

                GlobalRecord.Instance.TotalDamage += dealDamage.Result;
                CurrentParty(record => record.DamageDoneTotalWhilePresent += dealDamage.Result);
            });
            WhenFriend(dealDamage.Target, record => {
                record.DamageTaken += dealDamage.Result;
                GlobalRecord.Instance.TotalDamageTaken += dealDamage.Result;
                CurrentParty(record => record.DamageTakenTotalWhilePresent += dealDamage.Result);
            });
        }

        void IRollSkillCheckHandler.HandlePartySkillCheckRolled(RulePartySkillCheck check) {
            WhenFriend(check.Initiator, record => {
                if (check.Success)
                    record.SkillChecksPassed++;
                else
                    record.SkillChecksFailed++;
            });
        }

        void ITrapActivationHandler.HandleTrapActivation(UnitEntityData unit, TrapObjectView trap) {
            WhenFriend(unit, record => record.TrapsTriggered++);
        }

        void IRollSkillCheckHandler.HandleUnitSkillCheckRolled(RuleSkillCheck check) {
            WhenFriend(check.Initiator, record => {
                if (check.Success)
                    record.SkillChecksPassed++;
                else
                    record.SkillChecksFailed++;
            });
        }

        void IRulebookHandler<RuleSavingThrow>.OnEventAboutToTrigger(RuleSavingThrow evt) {
        }

        void IRulebookHandler<RuleSavingThrow>.OnEventDidTrigger(RuleSavingThrow evt) {
            WhenFriend(evt.Initiator, record => {
                record.Saves.ModifyStruct((int)evt.Type, (ref SavesRecord save) => {
                    if (evt.IsPassed) {
                        save.Passed++;
                        if (!evt.AutoPass && evt.D20.Result == 20)
                            save.PassedCrit++;
                    } else {
                        save.Failed++;
                        if (evt.D20.Result == 1)
                            save.FailedCrit++;
                    }
                });

            });
        }

        public void HandleExecutionProcessStart(AbilityExecutionContext context) {
        }

        public void HandleExecutionProcessEnd(AbilityExecutionContext context) {
            WhenFriend(context.Caster, record => {
                record.SpellsCast.Increment(context.AbilityBlueprint.Name);
            });
        }

        void IHealingHandler.HandleHealing(RuleHealDamage healDamage) {
            if (!healDamage.Target.IsInCompanionRoster())
                return;

            WhenFriend(healDamage.Initiator, record => {
                record.HealingDone += healDamage.Value;
                GlobalRecord.Instance.TotalHealing += healDamage.Value;
                CurrentParty(r => {
                    r.HealingDoneTotalWhilePresent += healDamage.Value;
                });
            });
        }
    }

    class StatisticsOhMy {
        internal static void Install() {
            if (!CharInfoWindowUtility.PagesOrderPC[UnitType.MainCharacter].Contains(CharInfoPageType_EXT.Statistics)) {
                Main.Log("Injecting enums");

                CharInfoWindowUtility.PagesOrderPC[UnitType.MainCharacter].Add(CharInfoPageType_EXT.Statistics);
                CharInfoWindowUtility.PagesOrderPC[UnitType.Companion].Add(CharInfoPageType_EXT.Statistics);
                CharInfoWindowUtility.PagesOrderPC[UnitType.Pet].Add(CharInfoPageType_EXT.Statistics);


                CharInfoWindowUtility.PagesContent[CharInfoPageType_EXT.Statistics] = new CharInfoPage {
                    ComponentsForAll = new List<CharInfoComponentType>
                        {
                        CharInfoComponentType.NameAndPortrait,
                        CharInfoComponentType.LevelClassScores,
                        CharInfoComponentType.AttackMain,
                        CharInfoComponentType.DefenceMain,
                        CharInfoComponentType_EXT.Statistics,
                    }
                };
            } else {
                Game.ResetUI();
            }

            EventBus.Subscribe(listener);
        }


        internal static void Uninstall() {
            EventBus.Unsubscribe(listener);
        }
        private static StatiscticsListener listener = new();
    }
}
