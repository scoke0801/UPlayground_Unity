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
        private void OnEnable() { if (CycleRunManager.Instance != null) CycleRunManager.Instance.OnBossDiscovered += Show; if (_group != null) _group.alpha = 0f; }
        private void OnDisable() { if (CycleRunManager.Instance != null) CycleRunManager.Instance.OnBossDiscovered -= Show; }
        private void Show(CycleBossPlacement boss) { if (_title != null) _title.text = $"{(boss.isCentral ? "중앙" : "외곽")} 보스\n{boss.actorId}"; _remaining = _duration; if (_group != null) _group.alpha = 1f; }
        private void Update() { if (_remaining <= 0f) return; _remaining -= Time.unscaledDeltaTime; if (_remaining <= 0f && _group != null) _group.alpha = 0f; }
    }
}
