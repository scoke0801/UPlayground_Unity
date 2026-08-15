#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UPlayGround.UI.DevCheat
{
    /// <summary>UI_System_DevCheatPanel — 런타임 UGUI 생성 헬퍼(탭 콘텐츠를 코드로 구성).</summary>
    public partial class UI_System_DevCheatPanel
    {
        protected static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        protected static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        protected static Image AddImage(GameObject go, Color color)
        {
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        protected static VerticalLayoutGroup AddVLG(GameObject go, float spacing, int pad, bool forceExpandHeight = false)
        {
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = new RectOffset(pad, pad, pad, pad);
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = forceExpandHeight;
            v.childControlWidth = true;
            v.childControlHeight = true;
            return v;
        }

        protected static HorizontalLayoutGroup AddHLG(GameObject go, float spacing, int pad, bool forceExpandWidth = false)
        {
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.padding = new RectOffset(pad, pad, pad, pad);
            h.childForceExpandWidth = forceExpandWidth;
            h.childForceExpandHeight = true;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childAlignment = TextAnchor.MiddleLeft;
            return h;
        }

        protected static LayoutElement SetSize(GameObject go,
            float minW = -1, float prefW = -1, float flexW = -1,
            float minH = -1, float prefH = -1, float flexH = -1)
        {
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.minWidth = minW; le.preferredWidth = prefW; le.flexibleWidth = flexW;
            le.minHeight = minH; le.preferredHeight = prefH; le.flexibleHeight = flexH;
            return le;
        }

        protected static TextMeshProUGUI MakeText(Transform parent, string text, int size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
        {
            var rt = NewRect("Text", parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            return t;
        }

        protected static Button MakeButton(Transform parent, string label, Color bg, Action onClick, int fontSize = 18)
        {
            var rt = NewRect("Btn_" + label, parent);
            var img = AddImage(rt.gameObject, bg);
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;

            var t = MakeText(rt, label, fontSize, TextMain, TextAlignmentOptions.Center);
            Stretch((RectTransform)t.transform);

            if (onClick != null)
                btn.onClick.AddListener(() => onClick());
            return btn;
        }

        protected static Toggle MakeToggle(Transform parent, string label, bool isOn, Action<bool> onChanged)
        {
            var rt = NewRect("Toggle_" + label, parent);
            AddImage(rt.gameObject, RowBg);
            var h = AddHLG(rt.gameObject, 10, 10);
            h.childForceExpandWidth = false;

            var toggle = rt.gameObject.AddComponent<Toggle>();

            var box = NewRect("Box", rt);
            SetSize(box.gameObject, minW: 28, prefW: 28, minH: 28, prefH: 28);
            var boxImg = AddImage(box.gameObject, new Color(0.06f, 0.08f, 0.10f, 1f));

            var check = NewRect("Check", box);
            Stretch(check);
            check.offsetMin = new Vector2(5, 5);
            check.offsetMax = new Vector2(-5, -5);
            var checkImg = AddImage(check.gameObject, Accent);

            var t = MakeText(rt, label, 18, TextMain);
            SetSize(t.gameObject, flexW: 1);

            toggle.targetGraphic = boxImg;
            toggle.graphic = checkImg;
            toggle.isOn = isOn;
            checkImg.enabled = isOn;
            if (onChanged != null)
                toggle.onValueChanged.AddListener(v => onChanged(v));
            return toggle;
        }

        protected static TMP_InputField MakeInput(Transform parent, string placeholder, Action<string> onChanged,
            TMP_InputField.ContentType contentType = TMP_InputField.ContentType.Standard)
        {
            var rt = NewRect("Input", parent);
            var bg = AddImage(rt.gameObject, new Color(0.06f, 0.08f, 0.10f, 1f));
            SetSize(rt.gameObject, minH: 40, prefH: 40);

            var input = rt.gameObject.AddComponent<TMP_InputField>();
            input.targetGraphic = bg;
            input.contentType = contentType;

            var viewport = NewRect("TextArea", rt);
            Stretch(viewport);
            viewport.offsetMin = new Vector2(10, 4);
            viewport.offsetMax = new Vector2(-10, -4);
            viewport.gameObject.AddComponent<RectMask2D>();

            var ph = MakeText(viewport, placeholder, 18, TextSub);
            Stretch((RectTransform)ph.transform);

            var textComp = MakeText(viewport, string.Empty, 18, TextMain);
            Stretch((RectTransform)textComp.transform);

            input.textViewport = viewport;
            input.textComponent = textComp;
            input.placeholder = ph;

            if (onChanged != null)
                input.onValueChanged.AddListener(v => onChanged(v));
            return input;
        }

        /// <summary> 세로 스크롤 리스트를 만든다. 반환은 컨텐츠(RectTransform, VLG 부착). </summary>
        protected static RectTransform MakeScroll(Transform parent, out ScrollRect scrollRect)
        {
            var rootRt = NewRect("Scroll", parent);
            AddImage(rootRt.gameObject, new Color(0.06f, 0.08f, 0.11f, 0.6f));
            scrollRect = rootRt.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;

            var viewport = NewRect("Viewport", rootRt);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            AddImage(viewport.gameObject, new Color(1f, 1f, 1f, 0.001f));

            var content = NewRect("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = new Vector2(0, 0);
            content.offsetMax = new Vector2(0, 0);
            var vlg = AddVLG(content.gameObject, 4, 4);
            vlg.childForceExpandHeight = false;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            return content;
        }

        protected static void ClearChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
                Destroy(t.GetChild(i).gameObject);
        }
    }
}
#endif
