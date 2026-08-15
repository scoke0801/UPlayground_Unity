using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Dialogue;

namespace UPlayGround.UI
{
    /// <summary>
    /// 이전 대화내역(Backlog) 스크롤 패널. 최신 항목이 아래에 쌓입니다.
    /// 열려 있는 동안 대화 재생을 정지하고, 닫을 때 이전 정지 상태를 복원합니다.
    /// </summary>
    public class UI_Popup_DialogueBacklog : UI_Base
    {
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Transform entryContainer;
        [SerializeField] private UIDialogueBacklogEntry entryPrefab;
        [SerializeField] private GameObject emptyMessage;
        [SerializeField] private Button closeButton;

        private readonly List<UIDialogueBacklogEntry> _entries = new();

        // 이력이 열려 있는 동안 진행 입력을 삼켜야 하므로 입력을 독점한다.
        protected override bool BlocksLowerInput => true;

        // 이력을 열기 직전의 정지 상태. 닫을 때 이 값으로 되돌린다.
        private bool _pauseStateBeforeOpen;

        protected override void Awake()
        {
            base.Awake();
            closeButton?.onClick.AddListener(Hide);
        }

        protected override void OnShow()
        {
            var dialogue = UISvc.Dialogue;
            if (dialogue != null)
            {
                _pauseStateBeforeOpen = dialogue.IsPaused;
                dialogue.SetPaused(true);
                dialogue.OnHistoryChanged += Rebuild;
            }

            // Rebuild가 IsVisible 상태에서 스크롤을 하단으로 맞춘다.
            Rebuild();
        }

        protected override void OnHide()
        {
            var dialogue = UISvc.Dialogue;
            if (dialogue != null)
            {
                dialogue.OnHistoryChanged -= Rebuild;
                dialogue.SetPaused(_pauseStateBeforeOpen);
            }
        }

        // ── 목록 구성 ────────────────────────────────────────────────────

        private void Rebuild()
        {
            var dialogue = UISvc.Dialogue;
            IReadOnlyList<DialogueLogEntry> history = dialogue?.History;
            int count = history?.Count ?? 0;

            if (entryPrefab == null || entryContainer == null)
                return;

            // 항목 수를 맞춘 뒤 재사용한다(이력 상한이 작아 풀링까지는 불필요).
            while (_entries.Count < count)
                _entries.Add(Instantiate(entryPrefab, entryContainer));

            for (int i = 0; i < _entries.Count; i++)
            {
                bool used = i < count;
                _entries[i].gameObject.SetActive(used);

                if (used)
                    _entries[i].Setup(history[i]);
            }

            if (emptyMessage != null)
                emptyMessage.SetActive(count == 0);

            // 열려 있는 동안 새 대사가 기록되면(정지 해제 상태) 최신 항목이 보이도록 따라 내려간다.
            if (IsVisible)
                ScrollToBottom();
        }

        private void ScrollToBottom()
        {
            if (scrollRect == null)
                return;

            // 항목 높이는 중첩 레이아웃 그룹이 계산하므로, 재빌드 전에는 content 높이가 0이라 위치가 어긋난다.
            if (scrollRect.content != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);

            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}
