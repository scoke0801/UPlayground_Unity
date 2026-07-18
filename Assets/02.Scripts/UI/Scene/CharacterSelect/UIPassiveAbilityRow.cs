using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.Ability;

namespace UPlayGround.UI
{
    public sealed class UIPassiveAbilityRow : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _title;
        [SerializeField] private TextMeshProUGUI _description;
        [SerializeField] private TextMeshProUGUI _trigger;

        public void Bind(PassiveAbilitySO passive)
        {
            if (passive == null)
            {
                gameObject.SetActive(false);
                return;
            }

            AbilityPresentationDefinition presentation = passive.presentation;
            if (_icon != null)
            {
                _icon.sprite = presentation?.icon;
                _icon.enabled = presentation?.icon != null;
            }
            if (_title != null)
                _title.text = presentation?.displayName ?? passive.name;
            if (_description != null)
                _description.text = passive.CharacterSelectDescription;
            if (_trigger != null)
            {
                _trigger.text = passive.activationType switch
                {
                    PassiveActivationType.PerfectDodge => "퍼펙트 회피",
                    PassiveActivationType.PerfectGuard => "퍼펙트 가드",
                    _ => "상시",
                };
            }
            gameObject.SetActive(true);
        }

        public void Clear() => gameObject.SetActive(false);

        public static UIPassiveAbilityRow CreateRuntime(Transform parent, int index)
        {
            var root = new GameObject(
                $"PassiveRow_Runtime_{index + 1}",
                typeof(RectTransform),
                typeof(Image),
                typeof(LayoutElement),
                typeof(HorizontalLayoutGroup),
                typeof(UIPassiveAbilityRow));
            root.transform.SetParent(parent, false);
            root.GetComponent<Image>().color =
                new Color(0.12f, 0.15f, 0.20f, 1f);
            root.GetComponent<LayoutElement>().preferredHeight = 92f;
            var horizontal = root.GetComponent<HorizontalLayoutGroup>();
            horizontal.padding = new RectOffset(8, 8, 8, 8);
            horizontal.spacing = 10f;
            horizontal.childAlignment = TextAnchor.MiddleLeft;
            horizontal.childControlWidth = true;
            horizontal.childControlHeight = true;
            horizontal.childForceExpandWidth = false;
            horizontal.childForceExpandHeight = false;

            var iconObject = new GameObject(
                "Icon",
                typeof(RectTransform),
                typeof(Image),
                typeof(LayoutElement));
            iconObject.transform.SetParent(root.transform, false);
            var iconLayout = iconObject.GetComponent<LayoutElement>();
            iconLayout.preferredWidth = 52f;
            iconLayout.preferredHeight = 52f;
            var icon = iconObject.GetComponent<Image>();
            icon.preserveAspect = true;
            icon.enabled = false;

            var textRoot = new GameObject(
                "Texts",
                typeof(RectTransform),
                typeof(LayoutElement),
                typeof(VerticalLayoutGroup));
            textRoot.transform.SetParent(root.transform, false);
            textRoot.GetComponent<LayoutElement>().flexibleWidth = 1f;
            var vertical = textRoot.GetComponent<VerticalLayoutGroup>();
            vertical.spacing = 2f;
            vertical.childControlWidth = true;
            vertical.childControlHeight = true;
            vertical.childForceExpandWidth = true;
            vertical.childForceExpandHeight = false;

            TextMeshProUGUI title = CreateText(
                textRoot.transform, "Title", 19f, Color.white);
            TextMeshProUGUI description = CreateText(
                textRoot.transform,
                "Description",
                15f,
                new Color(0.62f, 0.68f, 0.74f, 1f));
            TextMeshProUGUI trigger = CreateText(
                textRoot.transform,
                "Trigger",
                14f,
                new Color(0.35f, 0.80f, 0.90f, 1f));

            var row = root.GetComponent<UIPassiveAbilityRow>();
            row._icon = icon;
            row._title = title;
            row._description = description;
            row._trigger = trigger;
            root.SetActive(false);
            return row;
        }

        private static TextMeshProUGUI CreateText(
            Transform parent,
            string objectName,
            float fontSize,
            Color color)
        {
            var textObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAlignmentOptions.Left;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }
    }
}
