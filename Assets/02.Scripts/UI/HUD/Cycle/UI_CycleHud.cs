using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.Cycle;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    public sealed class UI_CycleHud : MonoBehaviour
    {
        [SerializeField] private TMP_Text _cycleText;
        [SerializeField] private TMP_Text _seedText;
        [SerializeField] private TMP_Text _elapsedText;
        private void OnEnable() { if (CycleRunManager.Instance != null) CycleRunManager.Instance.OnPhaseChanged += OnPhaseChanged; Refresh(); }
        private void OnDisable() { if (CycleRunManager.Instance != null) CycleRunManager.Instance.OnPhaseChanged -= OnPhaseChanged; }
        private void Update() { if (CycleRunManager.Instance?.IsActive == true) RefreshElapsed(CycleRunManager.Instance.Current.elapsedSeconds); }
        private void OnPhaseChanged(CycleRunState _) => Refresh();
        private void Refresh()
        {
            CycleRunState run = CycleRunManager.Instance?.Current;
            bool visible = run != null && run.phase is not (CycleRunPhase.Inactive or CycleRunPhase.Completed);
            if (_cycleText != null) { _cycleText.gameObject.SetActive(visible); _cycleText.text = visible ? $"Cycle {run.cycleIndex}" : string.Empty; }
            if (_seedText != null) { _seedText.gameObject.SetActive(visible); _seedText.text = visible ? $"Seed {run.seed}" : string.Empty; }
            RefreshElapsed(run?.elapsedSeconds ?? 0f);
        }
        private void RefreshElapsed(float seconds)
        {
            if (_elapsedText == null) return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _elapsedText.gameObject.SetActive(CycleRunManager.Instance?.IsActive == true);
            _elapsedText.text = $"{Mathf.FloorToInt(seconds / 60f):00}:{Mathf.FloorToInt(seconds % 60f):00}";
#else
            _elapsedText.gameObject.SetActive(false);
#endif
        }
    }

    public sealed class UI_CycleEncounterBanner : MonoBehaviour
    {
        [SerializeField] private TMP_Text _title;
        [SerializeField] private CanvasGroup _group;
        [Min(0.1f), SerializeField] private float _duration = 3f;
        private float _remaining;
        private void OnEnable() { if (CycleRunManager.Instance != null) CycleRunManager.Instance.OnBossDiscovered += Show; if (_group != null) _group.alpha = 0f; }
        private void OnDisable() { if (CycleRunManager.Instance != null) CycleRunManager.Instance.OnBossDiscovered -= Show; }
        private void Show(CycleBossPlacement boss) { if (_title != null) _title.text = $"{(boss.isCentral ? "중앙" : "외곽")} 보스\n{boss.actorId}"; _remaining = _duration; if (_group != null) _group.alpha = 1f; }
        private void Update() { if (_remaining <= 0f) return; _remaining -= Time.unscaledDeltaTime; if (_remaining <= 0f && _group != null) _group.alpha = 0f; }
    }

    public sealed class UI_BossAssistHud : MonoBehaviour
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
