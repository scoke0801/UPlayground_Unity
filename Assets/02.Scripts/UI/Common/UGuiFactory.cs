using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UPlayGround.UI
{
    /// <summary>
    /// 코드로 uGUI 계층을 조립하는 공용 헬퍼.
    ///
    /// 데이터 개수에 따라 구조가 달라지는 화면(키 설정 목록처럼 액션 수 × 장치 수만큼
    /// 행·칩이 생기는 UI)은 프리팹으로 고정 저작하기 어렵다. 프로젝트에는 이미
    /// <c>UI_System_DevCheatPanel</c>이 같은 방식으로 계층을 코드에서 만들고 있는데, 그 헬퍼가
    /// 해당 클래스 내부에 protected로 갇혀 있어 재사용이 안 됐다. 여기로 꺼내 공용화한다.
    /// (UI_System_DevCheatPanel 자체의 이관은 별개 작업이다.)
    /// </summary>
    public static class UGuiFactory
    {
        public static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            return rect;
        }

        /// <summary>부모를 꽉 채우는 RectTransform.</summary>
        public static RectTransform NewStretched(string name, Transform parent)
        {
            RectTransform rect = NewRect(name, parent);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        public static Image AddImage(GameObject go, Color color, Sprite sprite = null)
        {
            var image = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            return image;
        }

        public static VerticalLayoutGroup AddVLG(
            GameObject go,
            float spacing,
            int padding,
            bool forceExpandHeight = false)
        {
            var layout = go.GetComponent<VerticalLayoutGroup>() ?? go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = forceExpandHeight;
            layout.childAlignment = TextAnchor.UpperLeft;
            return layout;
        }

        public static HorizontalLayoutGroup AddHLG(
            GameObject go,
            float spacing,
            int padding,
            bool forceExpandWidth = false)
        {
            var layout = go.GetComponent<HorizontalLayoutGroup>()
                         ?? go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = forceExpandWidth;
            layout.childForceExpandHeight = true;
            layout.childAlignment = TextAnchor.MiddleLeft;
            return layout;
        }

        public static ContentSizeFitter AddVerticalFitter(GameObject go)
        {
            var fitter = go.GetComponent<ContentSizeFitter>() ?? go.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return fitter;
        }

        public static TextMeshProUGUI MakeText(
            Transform parent,
            string text,
            float size,
            Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Left,
            FontStyles style = FontStyles.Normal)
        {
            RectTransform rect = NewRect("Text", parent);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.fontStyle = style;
            label.richText = false;
            label.raycastTarget = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            return label;
        }

        /// <summary>
        /// 레이아웃 제약을 설정한다. 지정하지 않은 축은 건드리지 않는다(-1 = 미지정).
        /// </summary>
        public static LayoutElement SetSize(
            GameObject go,
            float minW = -1, float minH = -1,
            float prefW = -1, float prefH = -1,
            float flexW = -1, float flexH = -1)
        {
            var element = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            if (minW >= 0) element.minWidth = minW;
            if (minH >= 0) element.minHeight = minH;
            if (prefW >= 0) element.preferredWidth = prefW;
            if (prefH >= 0) element.preferredHeight = prefH;
            if (flexW >= 0) element.flexibleWidth = flexW;
            if (flexH >= 0) element.flexibleHeight = flexH;
            return element;
        }

        /// <summary>
        /// 세로 스크롤 영역을 만들고 항목을 넣을 Content를 돌려준다.
        /// Content에는 VerticalLayoutGroup + ContentSizeFitter가 붙는다.
        /// </summary>
        public static RectTransform MakeVerticalScroll(
            Transform parent,
            out ScrollRect scrollRect,
            float spacing = 0f,
            int padding = 0)
        {
            RectTransform root = NewRect("Scroll", parent);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            scrollRect = root.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;

            RectTransform viewport = NewStretched("Viewport", root);
            viewport.gameObject.AddComponent<RectMask2D>();
            scrollRect.viewport = viewport;

            RectTransform content = NewRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            AddVLG(content.gameObject, spacing, padding);
            AddVerticalFitter(content.gameObject);
            scrollRect.content = content;

            return content;
        }

        /// <summary>배경 + 라벨을 가진 클릭 가능한 버튼.</summary>
        public static Button MakeButton(
            Transform parent,
            string text,
            float fontSize,
            Color background,
            Color textColor,
            out TextMeshProUGUI label)
        {
            RectTransform rect = NewRect("Button_" + text, parent);
            Image image = AddImage(rect.gameObject, background);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            label = MakeText(rect, text, fontSize, textColor, TextAlignmentOptions.Center);
            RectTransform labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8f, 0f);
            labelRect.offsetMax = new Vector2(-8f, 0f);

            return button;
        }

        /// <summary>1px 구분선.</summary>
        public static Image MakeSeparator(Transform parent, Color color, float thickness = 1f)
        {
            RectTransform rect = NewRect("Separator", parent);
            Image image = AddImage(rect.gameObject, color);
            image.raycastTarget = false;
            SetSize(rect.gameObject, minH: thickness, prefH: thickness, flexH: 0f);
            return image;
        }
    }
}
