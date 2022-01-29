using BubbleTweaks.Config;
using BubbleTweaks.Utilities;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.GameModes;
using Kingmaker.UI.MVVM._PCView.InGame;
using Kingmaker.UI.MVVM._VM.Tooltip.Templates;
using Kingmaker.UI.MVVM._VM.Tooltip.Utils;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.View;
using Owlcat.Runtime.Core.Utils;
using Owlcat.Runtime.UI.Controls.Button;
using Owlcat.Runtime.UI.Controls.SelectableState;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTweaks {
    public class ActionWheel {
        private static readonly TrashCan Trash = new TrashCan();
        private static GameObject Root;
        private static ActionWheelComponent Wheel;

        private static string[] Abilities = new string[12] {
            "47808d23c67033d4bbab86a1070fd62f",
            "1c1ebf5370939a9418da93176cc44cd9",
            "6e81a6679a0889a429dec9cedcf3729c",
            "0d657aa811b310e4bbd8586e60156a2d",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
        };

        private static bool Installed = false;

        public static void Install() {
            Installed = true;

        }

        public static void Uninstall() {
            Installed = false;
            Trash.Dispose();
        }

        public static bool Active => Installed && Game.Instance?.Player?.GameId != null;

        internal static void Toggle() {
            if (!Active) return;

            if (Wheel?.Showing ?? false) {
                Hide();
            } else {
                Show();
            }
        }


        internal static void Show() {
            if (!Active) return;

            if (Root == null) {
                Root = new GameObject("bubble-actionwheel-root", typeof(RectTransform));
                Trash.Add(Root);
                Root.AddComponent<CanvasGroup>();
                Wheel = Root.AddComponent<ActionWheelComponent>();

                var dynamicRoot = Game.Instance.RootUiContext.m_UIView.GetComponent<InGamePCView>().m_DynamicPartPCView.transform;

                var canvas = dynamicRoot.Find("DynamicCanvas").gameObject;

                var maskSprite = AssetLoader.LoadInternal("icons", "UI_CircleMask256.png", new Vector2Int(256, 256));
                var borderSprite = AssetLoader.LoadInternal("icons", "circle_border.png", new Vector2Int(108, 108));

                float radius = 100;

                var back = new GameObject("bubble-pie", typeof(RectTransform));
                var bg = back.AddComponent<Image>();
                bg.sprite = maskSprite;
                bg.color = new Color(0, 0, 0, 0.15f);
                back.Rect().sizeDelta = new(2 * radius, 2 * radius);
                back.AddTo(Root);

                var pie = new GameObject("bubble-pie", typeof(RectTransform));

                Wheel.BG = pie.AddComponent<Image>();
                Wheel.BG.sprite = maskSprite;
                Wheel.BG.fillMethod = Image.FillMethod.Radial360;
                Wheel.BG.fillAmount = 1 / 12.0f;
                Wheel.BG.type = Image.Type.Filled;
                Wheel.BGEnabledColor = new Color(1, 1, 0, 0.3f);
                Wheel.BGDisabledColor = new Color(1, 0, 0, 0.3f);
                Wheel.BGBaseRotation = 15 + 30 * 6;
                pie.AddTo(Root);
                pie.Rect().localRotation = Quaternion.Euler(0, 0, 15 + 30 * 6);
                pie.Rect().sizeDelta = new(radius * 2, radius * 2);
                pie.SetActive(false);

                var iconSize = new Vector2(32, 32);
                var spriteColorBlock = new ColorBlock {
                    normalColor = new Color(.9f, .9f, .9f),
                    highlightedColor = Color.white,
                    pressedColor = new Color(.3f, .3f, .3f),
                    selectedColor = Color.white,
                    disabledColor = Color.gray,
                    colorMultiplier = 1,
                    fadeDuration = 0.2f
                };

                var borderColorBlock = new ColorBlock {
                    normalColor = Color.white,
                    highlightedColor = Color.yellow,
                    pressedColor = Color.white,
                    selectedColor = Color.white,
                    disabledColor = Color.gray,
                    colorMultiplier = 1,
                    fadeDuration = 0.2f
                };

                float casterAngle = 18.0f;

                float[] portraitAngles = { 0, casterAngle, -casterAngle, casterAngle * 2, -casterAngle * 2, casterAngle * 3, };

                var portraitRoot = new GameObject("bubble-portraits", typeof(RectTransform));
                portraitRoot.AddTo(Root);

                for (int i = 0; i < 6; i++) {
                    var button = new GameObject($"bubble-actionwheel-button-{i}", typeof(RectTransform));

                    var maskLayer = new GameObject("mask", typeof(RectTransform));
                    maskLayer.AddComponent<Image>().sprite = maskSprite;
                    maskLayer.AddComponent<Mask>().showMaskGraphic = false;
                    maskLayer.AddTo(button);
                    maskLayer.Rect().sizeDelta = iconSize;

                    var borderLayer = new GameObject("border", typeof(RectTransform));
                    borderLayer.AddComponent<Image>().sprite = borderSprite;
                    borderLayer.AddTo(button);
                    borderLayer.Rect().sizeDelta = new Vector2(34, 34);

                    var iconLayer = new GameObject("icon", typeof(RectTransform));
                    iconLayer.AddTo(maskLayer);
                    iconLayer.Rect().sizeDelta = iconSize;
                    var img = iconLayer.AddComponent<Image>();
                    var b = button.AddComponent<OwlcatButton>();
                    //img.color = new Color(0, 1, 1, 0.6f);
                    img.sprite = Game.Instance.Player.PartyAndPets[i].Portrait.SmallPortrait;

                    b.AddLayerToMainPart(new Owlcat.Runtime.UI.Controls.SelectableState.OwlcatSelectableLayerPart {
                        Transition = OwlcatTransition.ColorTint,
                        Colors = borderColorBlock,
                        Image = borderLayer.GetComponent<Image>(),
                    });
                    b.AddLayerToMainPart(new Owlcat.Runtime.UI.Controls.SelectableState.OwlcatSelectableLayerPart {
                        Transition = OwlcatTransition.ColorTint,
                        Colors = spriteColorBlock,
                        Image = img,
                    });

                    //Wheel.Buttons[i] = b;

                    button.SetActive(false);

                    var group = button.AddComponent<CanvasGroup>();
                    group.alpha = 0;
                    Wheel.Caster[i] = new(b, img);

                    var name = Game.Instance.Player.PartyAndPets[i].CharacterName;

                    var showTween = DOTween.To(() => group.alpha, v => group.alpha = v, 1, 2.3f).SetUpdate(true).SetAutoKill(false).Pause();
                    Wheel.Caster[i].Show = BubbleTweener.Make(showTween, 1.0f, BubbleTweener.Float(0, 1, 0.3f, () => group.alpha));

                    var hideTween = DOTween.To(() => group.alpha, v => group.alpha = v, 0, 2.3f).SetUpdate(true).SetAutoKill(false).Pause();
                    Wheel.Caster[i].Hide = BubbleTweener.Make(hideTween, 0.0f, BubbleTweener.Float(1, 0, 0.3f, () => 1 - group.alpha));
                    hideTween.OnComplete(() => button.SetActive(false));


                    float angle = (portraitAngles[i] - (casterAngle * .5f)) * Mathf.Deg2Rad;
                    var x = Mathf.Sin(angle);
                    var y = Mathf.Cos(angle);
                    button.AddTo(portraitRoot);
                    button.Rect().localPosition = new Vector3(x * (radius + 42), y * (radius + 42), 0);
                    button.Rect().sizeDelta = iconSize;
                    button.Rect().localScale = new Vector3(1.3f, 1.3f, 1.3f);

                }

                for (int i = 0; i < 12; i++) {
                    var button = new GameObject($"bubble-actionwheel-button-{i}", typeof(RectTransform));

                    var maskLayer = new GameObject("mask", typeof(RectTransform));
                    maskLayer.AddComponent<Image>().sprite = maskSprite;
                    maskLayer.AddComponent<Mask>();
                    maskLayer.AddTo(button);
                    maskLayer.Rect().sizeDelta = iconSize;

                    var borderLayer = new GameObject("border", typeof(RectTransform));
                    borderLayer.AddComponent<Image>().sprite = borderSprite;
                    borderLayer.AddTo(button);
                    borderLayer.Rect().sizeDelta = new Vector2(34, 34);

                    var iconLayer = new GameObject("icon", typeof(RectTransform));
                    iconLayer.AddTo(maskLayer);
                    iconLayer.Rect().sizeDelta = iconSize;
                    var img = iconLayer.AddComponent<Image>();
                    var b = button.AddComponent<OwlcatButton>();

                    if (Abilities[i] != null) {
                        var ability = Resources.GetBlueprint<BlueprintAbility>(Abilities[i]);
                        //b.SetTooltip(new TooltipTemplateAbility(ability));
                        img.sprite = ability.Icon;
                        img.color = new Color(0.8f, 0.8f, 0.8f);
                    } else {
                        img.color = new Color(0, 0, 0, 0.6f);
                    }

                    b.AddLayerToMainPart(new Owlcat.Runtime.UI.Controls.SelectableState.OwlcatSelectableLayerPart {
                        Transition = OwlcatTransition.ColorTint,
                        Colors = borderColorBlock,
                        Image = borderLayer.GetComponent<Image>(),
                    });
                    b.AddLayerToMainPart(new Owlcat.Runtime.UI.Controls.SelectableState.OwlcatSelectableLayerPart {
                        Transition = OwlcatTransition.ColorTint,
                        Colors = spriteColorBlock,
                        Image = img,
                    });
                    b.Interactable = Abilities[i] != null;

                    Wheel.Buttons[i] = b;
                    int index = i;
                    b.OnSingleLeftClick.AddListener(() => {
                        int canCast = index + 1;
                        float offset = 0;
                        if (canCast % 2 == 1) {
                            offset = casterAngle * .5f;
                        }

                        float rotate = -30 * index - offset;
                        if (Wheel.Caster[0].Button.gameObject.activeSelf) {
                            portraitRoot.transform.DOLocalRotateQuaternion(Quaternion.Euler(0, 0, rotate), 0.1f).SetUpdate(true);
                        } else {
                            portraitRoot.transform.localRotation = Quaternion.Euler(0, 0, rotate);
                        }


                        for (int c = 0; c < 6; c++) {
                            var unit = Game.Instance.Player.PartyAndPets[c];
                            bool free = !unit.Commands.HasUnfinished();
                            if (!free && unit.Commands.Attack != null)
                                free = true;
                            var caster = Wheel.Caster[c];
                            var obj = caster.Button.gameObject;
                            if (c < canCast) {
                                obj.SetActive(true);
                                caster.Hide.Pause();
                                caster.Show.Play();
                                caster.Portrait.color = free ? Color.white : Color.red;
                            } else {
                                if (obj.activeSelf) {
                                    caster.Show.Pause();
                                    caster.Hide.Play();
                                }
                            }
                            caster.Button.transform.localRotation = Quaternion.Euler(0, 0, -rotate);
                        }
                    });

                    float angle = i * 30 * Mathf.Deg2Rad;
                    var x = Mathf.Sin(angle);
                    var y = Mathf.Cos(angle);
                    button.AddTo(Root);
                    button.Rect().localPosition = new Vector3(x * radius, y * radius, 0);
                    button.Rect().sizeDelta = iconSize;
                    button.Rect().localScale = new(1.3f, 1.3f, 1.3f);

                }


                Wheel.RootCanvas = canvas.GetComponent<Canvas>();
                Root.AddTo(canvas);
                Root.Rect().localScale = Vector3.zero;
            }

            Wheel.Unit = Kingmaker.Cheats.Utilities.GetUnitUnderMouse();
            if (Wheel.Unit == null) {
                Root.SetActive(false);
                return;
            }

            Wheel.UpdatePosition();
            Wheel.DoShow();
        }

        internal static void Hide() {
            if (!Active) return;

            Wheel?.DoHide();
        }
    }

    public class ActionWheelComponent : MonoBehaviour {

        public bool Showing = false;

        public Vector3 EntityPosition {
            get {
                if (!this.m_UnitScanned) {
                    UnitEntityData unit = this.Unit;
                    if ((unit?.View) != null) {
                        this.m_Bone = this.Unit.View.transform.FindChildRecursive("UI_Overtip_Bone");
                        this.m_UnitScanned = true;
                    }
                }
                if (this.m_Bone != null) {
                    return this.m_Bone.position;
                }
                if (this.Unit != null) {
                    UnitEntityView view = this.Unit.View;
                    Vector2 vector = (view != null) ? view.CameraOrientedBoundsSize : Vector2.zero;
                    return new Vector3(this.Unit.Position.x, this.Unit.Position.y + vector.y + 0f, this.Unit.Position.z);
                }
                return default;
            }
        }

        public Vector3 CanvasPosition {
            get {
                Vector3 result;
                try {
                    Vector3 entityPosition = EntityPosition;
                    Camera camera = Game.Instance.UI.GetCameraRig().Camera;
                    if (camera == null) {
                        result = Vector3.zero;
                    } else {
                        result = camera.WorldToViewportPoint(entityPosition);
                    }
                } finally { }
                return result;
            }
        }

        void Update() {
            var currentPos = RootCanvas.ScreenToCanvasPosition(Input.mousePosition);

            var distance = Vector2.Distance(currentPos, transform.localPosition);
            int segment;
            if (distance > 116 || distance < 30) {
                segment = -1;
            } else {
                var dir = (currentPos - transform.localPosition).normalized;
                var up = new Vector3(0, 1, 0);
                var angle = -Vector2.SignedAngle(up, dir) + 15;
                if (angle < 0)
                    angle += 360;
                segment = (int)(angle / 30);
            }

            for (int i = 0; i < Buttons.Length; i++) {
                if (i == segment && Buttons[i].Interactable) {
                    Buttons[i].IsHighlighted = true;
                    bool mouseDown = Input.GetMouseButton(0);
                    if (Buttons[i].CurrentState == OwlcatSelectionState.Pressed && !mouseDown) {
                        Buttons[i].OnSingleLeftClick.Invoke();
                    }
                    Buttons[i].IsPressed = mouseDown;
                } else {
                    Buttons[i].IsHighlighted = false;
                    Buttons[i].IsPressed = false;
                }
            }

            BG.transform.localRotation = Quaternion.Euler(0, 0, BGBaseRotation - 30 * segment);
            if (segment == -1) {
                BG.gameObject.SetActive(false);
            } else {
                if (Buttons[segment].Interactable)
                    BG.color = BGEnabledColor;
                else
                    BG.color = BGDisabledColor;
                BG.gameObject.SetActive(true);
            }

        }

        public void DoShow() {
            Showing = true;
            gameObject.SetActive(true);
            Hide.Pause();
            Show.Play();
        }


        public void DoHide() {
            Showing = false;
            canvas.blocksRaycasts = false;

            foreach (var c in Caster)
                c.Button.gameObject.SetActive(false);

            if (!gameObject.activeSelf)
                return;

            Show.Pause();
            Hide.Play();

        }

        void OnDisable() {
            transform.localScale = Vector3.zero;
        }

        const float ANIM = 0.12f;
        const float ANIM_TH = 0.01f;

        void Awake() {
            canvas = GetComponent<CanvasGroup>();
            canvas.blocksRaycasts = false;


            var showTween = transform.DOScale(Vector3.one, ANIM);
            showTween.Pause();
            showTween.SetAutoKill(false);
            showTween.SetUpdate(true);
            showTween.OnComplete(() => {
                canvas.blocksRaycasts = true;
            });


            Show = BubbleTweener.Make(showTween, Vector3.one, BubbleTweener.Vec3(Vector3.zero, Vector3.one, ANIM, () => transform.localScale.x));

            var hideTween = transform.DOScale(Vector3.zero, ANIM);
            hideTween.SetUpdate(true);
            hideTween.Pause();
            hideTween.SetAutoKill(false);
            hideTween.OnComplete(() => {
                gameObject.SetActive(false);
            });

            Hide = BubbleTweener.Make(hideTween, Vector3.zero, BubbleTweener.Vec3(Vector3.one, Vector3.zero, ANIM, () => 1.0f - transform.localScale.x));


        }

        public void UpdatePosition() {
            var rect = transform as RectTransform;
            //rect.anchorMin = rect.anchorMax = Input.mousePosition;
            var screenPoint = Input.mousePosition;
            try {
                rect.localPosition = RootCanvas.ScreenToCanvasPosition(Input.mousePosition);
            
            } catch (Exception e) {

                Main.Error(e, "");
            };
        }

        public UnitEntityData Unit;
        public Transform m_Bone;
        private bool m_UnitScanned;
        private IPlayable Hide;
        private IPlayable Show;
        private CanvasGroup canvas;
        public Canvas RootCanvas;
        public OwlcatButton[] Buttons = new OwlcatButton[12];
        internal Image BG;
        internal float BGBaseRotation;
        internal Color BGEnabledColor;
        internal Color BGDisabledColor;
        internal CasterButton[] Caster = new CasterButton[6];
    }

    public interface IPlayable {
        void Play();
        void Pause();
    }

    public class MultiPlayer : IPlayable, IEnumerable<IPlayable> {
        private List<IPlayable> ToPlay = new();
        public void Add(IPlayable tween) {
            ToPlay.Add(tween);
        }

        public IEnumerator<IPlayable> GetEnumerator() {
            return ((IEnumerable<IPlayable>)ToPlay).GetEnumerator();
        }

        public void Pause() {
            foreach (var playable in ToPlay)
                playable.Pause();
        }
        public void Play() {
            foreach (var playable in ToPlay)
                playable.Play();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return ((IEnumerable)ToPlay).GetEnumerator();
        }
    }

    public class BubbleTweener : IPlayable {
        private Tweener Handle;
        private object End;
        private Func<(object, float)> Update;

        public static BubbleTweener Make<T>(Tweener handle, T end, Func<(T, float)> update) {
            return new(handle, end, () => update());
        }

        private BubbleTweener(Tweener handle, object end, Func<(object, float)> update) {
            Handle = handle;
            End = end;
            Update = update;
        }

        public void Play() {
            var (current, duration) = Update();
            Handle.Pause();
            Handle.ChangeValues(current, End, duration);
            if (duration < 0.01f) {
                Handle.Complete();
            } else {
                Handle.Rewind();
                Handle.Play();
            }
        }

        public void Pause() {
            Handle.Pause();
        }

        public static Func<(Vector3, float)> Vec3(Vector3 from, Vector3 to, float duration, Func<float> normalisedNow) {
            return () => {
                float here = normalisedNow();
                return (Vector3.Lerp(from, to, here), (1 - here) * duration);
            };
        }
        public static Func<(float, float)> Float(float from, float to, float duration, Func<float> normalisedNow) {
            return () => {
                float here = normalisedNow();

                return (Mathf.Lerp(from, to, here), (1 - here) * duration);
            };
        }
    }

    public class TrashCan : IDisposable {
        private List<IDisposable> Contents = new();

        public void Add(IDisposable item) => Contents.Add(item);
        public void Add(GameObject item) => Contents.Add(new DisposableGameObject(item));

        public void Dispose() {
            foreach (var item in Contents)
                item.Dispose();
        }
    }

    public class DisposableGameObject : IDisposable {
        private readonly GameObject Object;

        public DisposableGameObject(GameObject @object) {
            Object = @object;
        }

        public void Dispose() {
            GameObject.Destroy(Object);
        }
    }

    public static class CanvasPositioningExtensions {
        public static Vector3 WorldToCanvasPosition(this Canvas canvas, Vector3 worldPosition, Camera camera = null) {
            if (camera == null) {
                camera = Camera.main;
            }
            var viewportPosition = camera.WorldToViewportPoint(worldPosition);
            return canvas.ViewportToCanvasPosition(viewportPosition);
        }

        public static Vector3 ScreenToCanvasPosition(this Canvas canvas, Vector3 screenPosition) {
            var viewportPosition = new Vector3(screenPosition.x / Screen.width,
                                               screenPosition.y / Screen.height,
                                               0);
            return canvas.ViewportToCanvasPosition(viewportPosition);
        }

        public static Vector3 ViewportToCanvasPosition(this Canvas canvas, Vector3 viewportPosition) {
            var centerBasedViewPortPosition = viewportPosition - new Vector3(0.5f, 0.5f, 0);
            var canvasRect = canvas.GetComponent<RectTransform>();
            var scale = canvasRect.sizeDelta;
            return Vector3.Scale(centerBasedViewPortPosition, scale);
        }
    }

    internal class CasterButton {
        public OwlcatButton Button;
        public Image Portrait;
        internal BubbleTweener Show, Hide;

        public CasterButton(OwlcatButton button, Image portrait) {
            Button = button;
            Portrait = portrait;
        }
    }
}
