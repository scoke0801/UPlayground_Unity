using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Contracts.Ability;
using UPlayGround.Data.Ability;

namespace UPlayGround.UI
{
    /// <summary>HUD의 버프·디버프 아이콘 한 칸을 표시한다.</summary>
    public sealed class UIGameplayEffectIcon : MonoBehaviour
    {
        private static readonly Color BeneficialColor =
            new Color32(0x42, 0xE3, 0x9A, 0xFF);
        private static readonly Color HarmfulColor =
            new Color32(0xFF, 0x52, 0x63, 0xFF);
        private static readonly Color NeutralColor =
            new Color32(0xE6, 0xC1, 0x5A, 0xFF);

        [SerializeField] private Image _border;
        [SerializeField] private Image _icon;
        [SerializeField] private Image _timeShade;
        [SerializeField] private TextMeshProUGUI _fallbackText;
        [SerializeField] private GameObject _stackBadge;
        [SerializeField] private TextMeshProUGUI _stackText;
        [SerializeField] private TextMeshProUGUI _remainingText;
        [SerializeField] private TextMeshProUGUI _polarityText;

        public ulong RuntimeId { get; private set; }

        public void Bind(
            in GameplayEffectViewState state,
            Sprite fallbackIcon)
        {
            RuntimeId = state.RuntimeId;
            gameObject.name = $"Effect_{state.EffectId}";
            gameObject.SetActive(true);

            if (_border != null)
                _border.color = GetPolarityColor(state.Polarity);

            Sprite icon = state.Icon != null ? state.Icon : fallbackIcon;
            if (_icon != null)
            {
                _icon.sprite = icon;
                _icon.enabled = icon != null;
            }

            if (_fallbackText != null)
            {
                bool useFallbackText = icon == null;
                _fallbackText.gameObject.SetActive(useFallbackText);
                _fallbackText.text = useFallbackText
                    ? GetFallbackLetter(state.DisplayName, state.EffectId)
                    : string.Empty;
            }

            if (_polarityText != null)
            {
                _polarityText.text = state.Polarity switch
                {
                    GameplayEffectPolarity.Beneficial => "+",
                    GameplayEffectPolarity.Harmful => "−",
                    _ => "·",
                };
            }

            Refresh(state);
        }

        public void Refresh(in GameplayEffectViewState state)
        {
            bool showStack = state.ShowStackCount && state.StackCount > 1;
            if (_stackBadge != null)
                _stackBadge.SetActive(showStack);
            if (_stackText != null)
                _stackText.text = showStack ? state.StackCount.ToString() : string.Empty;

            bool hasDuration = !state.IsInfinite && state.DurationSeconds > 0f;
            if (_timeShade != null)
            {
                _timeShade.gameObject.SetActive(hasDuration);
                _timeShade.fillAmount = hasDuration
                    ? 1f - Mathf.Clamp01(state.RemainingSeconds / state.DurationSeconds)
                    : 0f;
            }

            bool showRemaining = hasDuration
                                 && state.ShowRemainingTime
                                 && state.RemainingSeconds <= 60f;
            if (_remainingText != null)
            {
                _remainingText.gameObject.SetActive(showRemaining);
                _remainingText.text = showRemaining
                    ? FormatRemaining(state.RemainingSeconds)
                    : string.Empty;
            }
        }

        public void Release()
        {
            RuntimeId = 0;
            gameObject.SetActive(false);
        }

        private static Color GetPolarityColor(GameplayEffectPolarity polarity) =>
            polarity switch
            {
                GameplayEffectPolarity.Beneficial => BeneficialColor,
                GameplayEffectPolarity.Harmful => HarmfulColor,
                _ => NeutralColor,
            };

        private static string FormatRemaining(float seconds)
        {
            float clamped = Mathf.Max(0f, seconds);
            return clamped < 10f
                ? clamped.ToString("0.0")
                : Mathf.CeilToInt(clamped).ToString();
        }

        private static string GetFallbackLetter(string displayName, string effectId)
        {
            string source = !string.IsNullOrWhiteSpace(displayName)
                ? displayName.Trim()
                : effectId?.Trim();
            return string.IsNullOrEmpty(source)
                ? "?"
                : source.Substring(0, 1).ToUpperInvariant();
        }
    }
}
