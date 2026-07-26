using System.Collections.Generic;
using System.Text;
using TMPro;
using UPlayGround.UI.InputPrompt;
using UnityEngine;
using UnityEngine.UI;

namespace UPlayGround.UI
{
    /// <summary>
    /// 바인딩 하나를 키캡 칩 묶음으로 그린다.
    ///
    /// <c>InputGlyphResolver</c>가 돌려주는 <see cref="GlyphPart"/> 목록을 그대로 받아
    /// 스프라이트가 있으면 아이콘 칩(Ⓐ, 마우스 등), 없으면 텍스트 칩(W, Space, LB)으로 그린다.
    /// 복합 바인딩은 파트가 2개 이상 오므로 사이에 "+"를 넣는다(LB + RB).
    ///
    /// 글리프 데이터가 없어도 텍스트 칩으로 정상 동작한다. 목업의 W A S D · Space · Shift ·
    /// LB · RT는 모두 사각 박스 안의 텍스트라, 스프라이트 없이도 의도한 모양이 나온다.
    /// </summary>
    public sealed class UIKeyCapStrip : MonoBehaviour
    {
        private const float CapHeight = 26f;
        private const float CapMinWidth = 26f;
        private const float CapPaddingX = 8f;

        private static readonly Color CapBackground = new(0.16f, 0.19f, 0.25f, 1f);
        private static readonly Color CapBorder = new(0.35f, 0.42f, 0.53f, 1f);
        private static readonly Color CapText = new(0.90f, 0.94f, 1f, 1f);
        private static readonly Color PlusText = new(0.55f, 0.62f, 0.72f, 1f);
        private static readonly Color EmptyText = new(0.42f, 0.47f, 0.55f, 1f);

        private HorizontalLayoutGroup _layout;
        private string _contentKey;

        private void EnsureLayout()
        {
            if (_layout != null)
                return;

            _layout = UGuiFactory.AddHLG(gameObject, spacing: 4f, padding: 0);
            _layout.childAlignment = TextAnchor.MiddleCenter;
            _layout.childForceExpandWidth = false;
            _layout.childForceExpandHeight = false;
        }

        public void Clear()
        {
            _contentKey = null;
            ClearChildren();
        }

        private void ClearChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }
        }

        /// <summary>바인딩이 없을 때의 표시("-").</summary>
        public void SetEmpty()
        {
            const string key = "E";
            if (_contentKey == key)
                return;

            EnsureLayout();
            _contentKey = key;
            ClearChildren();
            TextMeshProUGUI dash = UGuiFactory.MakeText(
                transform, "-", 16f, EmptyText, TextAlignmentOptions.Center);
            UGuiFactory.SetSize(dash.gameObject, minW: CapMinWidth, prefW: CapMinWidth,
                minH: CapHeight, prefH: CapHeight, flexH: 0f);
        }

        public void SetParts(IReadOnlyList<GlyphPart> parts)
        {
            if (parts == null || parts.Count == 0)
            {
                SetEmpty();
                return;
            }

            string key = BuildPartsKey(parts);
            if (_contentKey == key)
                return;

            EnsureLayout();
            _contentKey = key;
            ClearChildren();

            for (int i = 0; i < parts.Count; i++)
            {
                if (i > 0)
                    AddPlus();

                AddCap(parts[i]);
            }
        }

        /// <summary>글리프 해석에 실패했을 때 사람이 읽는 문자열을 그대로 칩으로 만든다.</summary>
        public void SetText(string display)
        {
            if (string.IsNullOrWhiteSpace(display) || display == "미지정")
            {
                SetEmpty();
                return;
            }

            string key = "T:" + display;
            if (_contentKey == key)
                return;

            EnsureLayout();
            _contentKey = key;
            ClearChildren();

            // "LB + East" 같은 문자열은 파트로 쪼개 조합키처럼 보이게 한다.
            string[] tokens = display.Split('+');
            for (int i = 0; i < tokens.Length; i++)
            {
                string token = tokens[i].Trim();
                if (token.Length == 0)
                    continue;

                if (i > 0)
                    AddPlus();

                AddCap(GlyphPart.TextOnly(token));
            }
        }

        private static string BuildPartsKey(IReadOnlyList<GlyphPart> parts)
        {
            var builder = new StringBuilder(parts.Count * 16);
            builder.Append("P:");
            for (int i = 0; i < parts.Count; i++)
            {
                GlyphPart part = parts[i];
                builder.Append(part.Sprite != null ? part.Sprite.GetInstanceID() : 0);
                builder.Append(':');
                builder.Append(part.Text);
                builder.Append('|');
            }

            return builder.ToString();
        }

        private void AddPlus()
        {
            TextMeshProUGUI plus = UGuiFactory.MakeText(
                transform, "+", 14f, PlusText, TextAlignmentOptions.Center);
            UGuiFactory.SetSize(plus.gameObject, minW: 10f, prefW: 10f,
                minH: CapHeight, prefH: CapHeight, flexH: 0f);
        }

        private void AddCap(GlyphPart part)
        {
            RectTransform cap = UGuiFactory.NewRect("Cap", transform);

            if (part.HasSprite)
            {
                // 아이콘은 칩 배경 없이 스프라이트만 둔다. 목업의 Ⓐ·마우스 표현과 같다.
                Image icon = UGuiFactory.AddImage(cap.gameObject, Color.white, part.Sprite);
                icon.type = Image.Type.Simple;
                icon.preserveAspect = true;
                UGuiFactory.SetSize(cap.gameObject,
                    minW: CapHeight, prefW: CapHeight,
                    minH: CapHeight, prefH: CapHeight, flexH: 0f);
                return;
            }

            UGuiFactory.AddImage(cap.gameObject, CapBackground);
            AddBorder(cap);

            string text = string.IsNullOrWhiteSpace(part.Text) ? "?" : part.Text;
            TextMeshProUGUI label = UGuiFactory.MakeText(
                cap, text, 14f, CapText, TextAlignmentOptions.Center);
            var labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(CapPaddingX * 0.5f, 0f);
            labelRect.offsetMax = new Vector2(-CapPaddingX * 0.5f, 0f);

            // 글자 수에 비례해 폭을 잡는다. 스프라이트 없이도 W와 Space가 같은 높이로 정렬된다.
            float width = Mathf.Max(CapMinWidth, text.Length * 9f + CapPaddingX * 2f);
            UGuiFactory.SetSize(cap.gameObject,
                minW: width, prefW: width,
                minH: CapHeight, prefH: CapHeight, flexH: 0f);
        }

        // 스프라이트 에셋 없이 테두리를 만든다. 얇은 Image 4장이면 충분하다.
        private static void AddBorder(RectTransform cap)
        {
            AddEdge(cap, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f));
            AddEdge(cap, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f));
            AddEdge(cap, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 0f));
            AddEdge(cap, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0f));
        }

        private static void AddEdge(
            RectTransform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 sizeDelta)
        {
            RectTransform edge = UGuiFactory.NewRect("Edge", parent);
            edge.anchorMin = anchorMin;
            edge.anchorMax = anchorMax;
            edge.anchoredPosition = Vector2.zero;
            edge.sizeDelta = sizeDelta;
            Image image = UGuiFactory.AddImage(edge.gameObject, CapBorder);
            image.raycastTarget = false;
        }
    }
}
