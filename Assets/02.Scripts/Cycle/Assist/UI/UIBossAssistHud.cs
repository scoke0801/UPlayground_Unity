using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.Cycle;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    // 파일명과 클래스명이 일치해야 프리팹/씬에 정식 MonoScript가 연결된다 (UICycleHud.cs에서 분리).
    public sealed class UIBossAssistHud : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private Image _cooldownFill;
        [SerializeField] private TMP_Text _cooldownText;
        [SerializeField] private CanvasGroup _group;
        private void Update()
        {
            BossAssistManager manager = BossAssistManager.Instance;
            if (manager == null) return;
            BossAssistDefinitionSO definition = manager.EquippedDefinition;
            if (_icon != null)
            {
                _icon.enabled = definition != null && definition.icon != null;
                _icon.sprite = definition != null ? definition.icon : null;
            }
            (float remaining, float duration) = manager.SampleCooldown();
            if (_cooldownFill != null) _cooldownFill.fillAmount = duration > 0f ? remaining / duration : 0f;
            if (_cooldownText != null) _cooldownText.text = remaining > 0f ? Mathf.CeilToInt(remaining).ToString() : string.Empty;
            if (_group != null) _group.alpha = definition == null ? 0.25f : manager.IsExecuting || remaining > 0f ? 0.55f : 1f;
        }
    }
}
