using TMPro;
using UnityEngine;
using UPlayGround.Data.Cycle;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    public sealed class UICycleHud : MonoBehaviour
    {
        [SerializeField] private TMP_Text _cycleText;
        [SerializeField] private TMP_Text _seedText;
        [SerializeField] private TMP_Text _elapsedText;
        private void OnEnable() { if (UISvc.Cycle != null) UISvc.Cycle.OnPhaseChanged += OnPhaseChanged; Refresh(); }
        private void OnDisable() { if (UISvc.Cycle != null) UISvc.Cycle.OnPhaseChanged -= OnPhaseChanged; }
        private void Update() { if (UISvc.Cycle?.IsActive == true) RefreshElapsed(UISvc.Cycle.Current.elapsedSeconds); }
        private void OnPhaseChanged(CycleRunState _) => Refresh();
        private void Refresh()
        {
            CycleRunState run = UISvc.Cycle?.Current;
            bool visible = run != null && run.phase is not (CycleRunPhase.Inactive or CycleRunPhase.Completed);
            if (_cycleText != null) { _cycleText.gameObject.SetActive(visible); _cycleText.text = visible ? $"Cycle {run.cycleIndex}" : string.Empty; }
            if (_seedText != null) { _seedText.gameObject.SetActive(visible); _seedText.text = visible ? $"Seed {run.seed}" : string.Empty; }
            RefreshElapsed(run?.elapsedSeconds ?? 0f);
        }
        private void RefreshElapsed(float seconds)
        {
            if (_elapsedText == null) return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _elapsedText.gameObject.SetActive(UISvc.Cycle?.IsActive == true);
            _elapsedText.text = $"{Mathf.FloorToInt(seconds / 60f):00}:{Mathf.FloorToInt(seconds % 60f):00}";
#else
            _elapsedText.gameObject.SetActive(false);
#endif
        }
    }

}
