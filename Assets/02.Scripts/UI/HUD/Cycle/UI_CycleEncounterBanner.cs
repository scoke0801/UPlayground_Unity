using TMPro;
using UnityEngine;
using UPlayGround.Data.Cycle;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    // 파일명과 클래스명이 일치해야 프리팹/씬에 정식 MonoScript가 연결된다 (UI_CycleHud.cs에서 분리).
    public sealed class UI_CycleEncounterBanner : MonoBehaviour
    {
        [SerializeField] private TMP_Text _title;
        [SerializeField] private CanvasGroup _group;
        [Min(0.1f), SerializeField] private float _duration = 3f;
        private float _remaining;
        private void OnEnable() { if (UISvc.Cycle != null) UISvc.Cycle.OnBossDiscovered += Show; if (_group != null) _group.alpha = 0f; }
        private void OnDisable() { if (UISvc.Cycle != null) UISvc.Cycle.OnBossDiscovered -= Show; }
        private void Show(CycleBossPlacement boss)
        {
            string name = !string.IsNullOrWhiteSpace(boss?.displayName) ? boss.displayName : "미확인 상대";
            if (_title != null) _title.text = $"{name}\n대결 상대를 찾았습니다";
            _remaining = _duration;
            if (_group != null) _group.alpha = 1f;
        }
        private void Update() { if (_remaining <= 0f) return; _remaining -= Time.unscaledDeltaTime; if (_remaining <= 0f && _group != null) _group.alpha = 0f; }
    }
}
