using BubbleTweaks.Utilities;
using Kingmaker;
using Kingmaker.GameModes;
using Kingmaker.Globalmap;
using Kingmaker.Globalmap.Blueprints;
using Kingmaker.Globalmap.State;
using Kingmaker.Globalmap.View;
using Kingmaker.Kingdom;
using Kingmaker.Kingdom.Settlements;
using Kingmaker.PubSubSystem;
using Kingmaker.UI.Common;
using Owlcat.Runtime.UI.Controls.Button;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UniRx;
using Kingmaker.UI.GlobalMap;
using Kingmaker.UI;
using DG.Tweening;
using HarmonyLib;
using Kingmaker.UI.MVVM._PCView.ServiceWindows.Encyclopedia;
using Kingmaker.UI.MVVM._PCView.ServiceWindows.Journal;

namespace BubbleTweaks {

    public class ArmySelectionHandler : IGlobalMapArmySelectedHandler {
        public GlobalMapArmyState Army;
        public void OnGlobalMapArmySelected(GlobalMapArmyState state) {
            Crusade.TryInitArmyUITweaks();
            Army = state;
        }
    }

    public class GameChangeModeHandler : IGameModeHandler {

        public void OnGameModeStart(GameModeType gameMode) {
            if (gameMode == GameModeType.GlobalMap) {
                Crusade.TryInitJumpToSiege();
                Crusade.TryInitVillageList();
            }
        }

        public void OnGameModeStop(GameModeType gameMode) {
        }
    }

    public class SiegeModeChanged : ISettlementSiegeHandler {
        public void OnSiegeFinished(SettlementState settlement, bool wasDestroyed) {
            if (Crusade.JumpToSiegeButton != null)
                Crusade.JumpToSiegeButton.Interactable = Crusade.AnySettlementsUnderSiege;
        }

        public void OnSiegeStarted(SettlementState settlement) {
            if (Crusade.JumpToSiegeButton != null)
                Crusade.JumpToSiegeButton.Interactable = Crusade.AnySettlementsUnderSiege;
        }
    }

    [HarmonyPatch(typeof(GlobalMapLocalMapManager), "Show")]
    static class GlobalMapLocalMapToggler {
        public static Action OnMapToggled;
        public static void Postfix() {
            OnMapToggled?.Invoke();
        }

    }

    public class Crusade {
        public static void Install() {
            Main.LogHeader("Installing Crusade Tweaks");
            EventBus.Subscribe(armySelectionHandler);
            EventBus.Subscribe(gameModeHandler);
        }

        public static void Uninstall() {
            EventBus.Unsubscribe(armySelectionHandler);
            EventBus.Unsubscribe(gameModeHandler);

            if (VillageListRoot != null)
                GameObject.Destroy(VillageListRoot);
        }

        private static readonly ArmySelectionHandler armySelectionHandler = new();
        private static readonly GameChangeModeHandler gameModeHandler = new();

        private static OwlcatButton disbandButton;

        public static bool AnySettlementsUnderSiege => KingdomState.Instance.SettlementsManager.Settlements.Any(s => s.UnderSiege);
        public static SettlementState FirstSettlementUnderSiege => KingdomState.Instance.SettlementsManager.Settlements.First(s => s.UnderSiege);
        public static OwlcatButton JumpToSiegeButton;

        public static GameObject VillageListRoot;

        public static bool PanelOpen = false;

        private static readonly int Panel_HidePosX = 446;
        private static readonly int Panel_ShowPosX = 7;

        public static void TryInitVillageList() {
            try {
                if (VillageListRoot != null && (VillageListRoot.transform == null || VillageListRoot.transform.parent == null)) {
                    Main.Log("Detected invalid VillageListRoot, destroying completely");
                    GameObject.Destroy(VillageListRoot);
                    entryPrefab = null;
                    scrollContents = null;
                    VillageListRoot = null;
                }

                if (VillageListRoot == null) {
                    var scrollPrefab = Game.Instance.UI.GlobalMapCanvas.transform.Find("ServiceWindowsConfig/EncyclopediaView/EncyclopediaNavigationView/BodyGroup/StandardScrollView").gameObject;
                    var localMapPrefab = Game.Instance.UI.GlobalMapCanvas.transform.Find("LocalMap").gameObject;

                    VillageListRoot = GameObject.Instantiate(localMapPrefab, Game.Instance.UI.GlobalMapCanvas.transform);
                    var rootTransform = VillageListRoot.transform as RectTransform;
                    //Shift it down enough to not obscure the regular little-map "expand" button
                    rootTransform.anchoredPosition = new Vector2(rootTransform.anchoredPosition.x, -116);
                    rootTransform.SetSiblingIndex(localMapPrefab.transform.GetSiblingIndex() + 1);

                    var mapManager = localMapPrefab.GetComponent<GlobalMapLocalMapManager>();
                    Main.Log("got map manager");

                    var scroller = GameObject.Instantiate(scrollPrefab, rootTransform);
                    scrollContents = scroller.transform.Find("Viewport/Content");
                    var scrollRect = scroller.transform as RectTransform;
                    var elem = scroller.AddComponent<LayoutElement>();
                    scrollRect.anchoredPosition = new Vector2(16, 0);
                    scrollRect.anchorMin = new Vector2(0, 0);
                    scrollRect.anchorMax = new Vector2(0.95f, 1); //the scrollbar wants to go off the side of the panel for some reason

                    var buttonPrefab = GlobalMapUI.Instance.transform.Find("GlobalMapToolbarView/SkipDayButton").gameObject;

                    entryPrefab = new GameObject("village_entry", typeof(RectTransform));
                    var button = GameObject.Instantiate(buttonPrefab, entryPrefab.transform);
                    button.SetActive(true);
                    button.name = "Button";
                    button.GetComponentInChildren<Image>().color = new Color(.7f, .7f, .7f, 0.8f);
                    var rect = button.transform as RectTransform;
                    //The button also wants to go off the side of the panel (I think it's because the panel is narrower than the graphics :sad:)
                    rect.anchoredPosition3D = new Vector3(-16, 0, 0);
                    rect.pivot = new Vector2(1.0f, 0.5f);
                    var textObject = rect.Find("Text").gameObject;
                    var statsLabel = GameObject.Instantiate(textObject, button.transform);
                    statsLabel.name = "Stats";
                    statsLabel.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineRight;
                    textObject.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;
                    button.GetComponent<LayoutElement>().preferredWidth = 400; //:owlcat_sad:
                    button.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

                    var sizer = entryPrefab.AddComponent<LayoutElement>();
                    sizer.preferredHeight = 36;

                    GameObject.Destroy(VillageListRoot.GetComponent<GlobalMapLocalMapManager>());
                    GameObject.Destroy(rootTransform.Find("Map").gameObject);

                    var openButton = rootTransform.Find("SwitchButton/Background/ArrowUp").gameObject;
                    var closeButton = rootTransform.Find("SwitchButton/Background/ArrowDown").gameObject;


                    var switchButton = rootTransform.Find("SwitchButton").GetComponent<OwlcatMultiButton>();
                    switchButton.OnLeftClick.RemoveAllListeners();

                    void TogglePanel(bool state) {
                        PanelOpen = state;

                        openButton.SetActive(!PanelOpen);
                        closeButton.SetActive(PanelOpen);

                        rootTransform.DOAnchorPosX(PanelOpen ? (float)Panel_ShowPosX : (float)Panel_HidePosX, 0.4f).SetUpdate<Tweener>(true);
                        UISoundController.Instance.Play(PanelOpen ? UISoundType.MapOpen : UISoundType.MapClose);

                    }

                    GlobalMapLocalMapToggler.OnMapToggled = () => {
                        if (mapManager.PinFlag && PanelOpen) {
                            TogglePanel(false);
                        }
                    };

                    switchButton.OnLeftClick.AddListener(() => {
                        TogglePanel(!PanelOpen);

                        if (PanelOpen && mapManager.PinFlag) {
                            mapManager.OnToggleClick();
                        }
                    });


#if DEBUG
                    TogglePanel(true);
#endif
                }
                BuildVillageList();
            } catch (Exception ex) {
                Main.Error(ex, "building ui");
            }
        }

        private static void BuildVillageList() {
            for (int i = 0; i < scrollContents.childCount; i++) {
                GameObject.Destroy(scrollContents.GetChild(i).gameObject);
            }

            foreach (var settlment in KingdomState.Instance.SettlementsManager.Settlements) {
                var entry = GameObject.Instantiate(entryPrefab, scrollContents);
                var label = entry.transform.Find("Button/Text").GetComponent<TextMeshProUGUI>();
                var stats = entry.transform.Find("Button/Stats").GetComponent<TextMeshProUGUI>();
                label.text = settlment.Name;
                stats.text = $"{settlment.Buildings.Count(b => b.IsFinished && !b.IsSpecialSlot)}+{settlment.Buildings.Count(b => !b.IsFinished)}, {settlment.SlotsLeft}";
                var button = entry.transform.Find("Button").GetComponent<OwlcatButton>();
                button.OnLeftClick.AddListener(() => {
                    Game.Instance.UI.GetCameraRig().ScrollTo(settlment.MarkerManager.m_Marker.transform.position);
                });
            }
        }

        private static GameObject entryPrefab;
        private static Transform scrollContents;

        public static void TryInitJumpToSiege() {
            if (GlobalMapUI.Instance != null) {
                var button = GlobalMapUI.Instance.transform.Find("GlobalMapToolbarView/SkipDayButton");

                for (int i = 0; i < button.parent.childCount; i++) {
                    if (button.parent.GetChild(i).name.StartsWith("BUBBLE")) {
                        GameObject.Destroy(button.parent.GetChild(i).gameObject);
                    }
                }

                var buttonNew = GameObject.Instantiate(button.gameObject, button.parent);
                buttonNew.name = "BUBBLE_JUMP_TO_SIEGE";

                buttonNew.GetComponentInChildren<TextMeshProUGUI>().text = "Jump to Siege";
                buttonNew.SetActive(true);

                buttonNew.transform.localPosition -= new Vector3(0, 50, 0);

                JumpToSiegeButton = buttonNew.GetComponentInChildren<OwlcatButton>();
                JumpToSiegeButton.Interactable = AnySettlementsUnderSiege;


                JumpToSiegeButton.m_OnSingleLeftClick = new Button.ButtonClickedEvent();
                JumpToSiegeButton.m_OnSingleLeftClick.AddListener(() => {
                    if (!AnySettlementsUnderSiege) {
                        Main.Log("No settlements under siege?");
                        return;
                    }
                    Game.Instance.UI.GetCameraRig().ScrollTo(FirstSettlementUnderSiege.MarkerManager.m_Marker.transform.position);
                });
            }
        }

        public static void TryInitArmyUITweaks() {
            if (GlobalMapUI.Instance != null && GlobalMapUI.Instance.transform.Find("ArmyHUDPCView/Background/BUBBLEBUT0") == null) {
                Main.Log("Initializing Crusade UI tweaks");
                var button = GlobalMapUI.Instance.transform.Find("ArmyHUDPCView/Background/InfoBlock");

                for (int i = 0; i < button.parent.childCount; i++) {
                    if (button.parent.GetChild(i).name.StartsWith("BUBBLE")) {
                        GameObject.Destroy(button.parent.GetChild(i).gameObject);
                    }
                }

                var buttonNew = GameObject.Instantiate(button.gameObject, button.parent);
                var innerButton = buttonNew.transform.Find("SettingsButton");

                disbandButton = innerButton.GetComponent<OwlcatButton>();
                var layer = disbandButton.m_CommonLayer[0];
                var tex = innerButton.GetComponent<Image>();
                var baseSprite = AssetLoader.LoadInternal("icons", "disband_army.png", new Vector2Int(128, 128));
                var pressedSprite = AssetLoader.LoadInternal("icons", "disband_army_click.png", new Vector2Int(128, 128));
                var highlightedSprite = AssetLoader.LoadInternal("icons", "disband_army_hover.png", new Vector2Int(128, 128));
                tex.sprite = baseSprite;
                layer.m_SpriteState.highlightedSprite = highlightedSprite;
                layer.m_SpriteState.pressedSprite = pressedSprite;
                layer.m_SpriteState.selectedSprite = baseSprite;

                disbandButton.m_OnSingleLeftClick = new Button.ButtonClickedEvent();
                disbandButton.m_OnSingleLeftClick.AddListener(() => {
                    if (armySelectionHandler.Army == null)
                        return;
                    if (armySelectionHandler.Army.Data.m_LeaderGuid != null && armySelectionHandler.Army.Data.m_LeaderGuid.Length > 0) {
                        Main.Log("Trying to disband an army with a general");
                        UIUtility.ShowMessageBox("You cannot disband an army that has a General", Kingmaker.UI.MessageModalBase.ModalType.Message, (buttonType) => {
                        });
                        return;
                    }
                    UIUtility.ShowMessageBox("Are you sure you want to disband this army?", Kingmaker.UI.MessageModalBase.ModalType.Dialog, (buttonType) => {
                        if (buttonType == Kingmaker.UI.MessageModalBase.ButtonType.Yes) {
                            Main.Log($"Disposing of army: {armySelectionHandler.Army.Data.ArmyName.ArmyName}");
                            armySelectionHandler.Army.Data.RemoveAllSquads();
                            Game.Instance.Player.GlobalMap.LastActivated.DestroyArmy(armySelectionHandler.Army);
                        }
                    }, null, 0, "Disband", "Cancel");
                });

                var frame = buttonNew.transform as RectTransform;
                frame.name = "BUBBLEBUT0";
                frame.localPosition += new Vector3(0, frame.rect.height + 4, 0);
            }
        }
    }
}
