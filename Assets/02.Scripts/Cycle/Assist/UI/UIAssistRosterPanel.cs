using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Cycle;
using UPlayGround.Data.Cycle;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    /// <summary>
    /// 파티(동료) 화면의 어시스트(사이클 보스 영입 동료) 관리 섹션.
    /// - 로스터(최대 4) 표시, 클릭으로 장착 교체
    /// - 로스터 가득 찬 상태의 신규 영입(PendingRecruitAssistId)에 대해
    ///   기존 어시스트 방출 후 수락 / 신규 포기 결정을 처리한다.
    ///   이 결정이 남아 있으면 사이클 정산이 차단되므로 반드시 여기서 소비돼야 한다.
    /// </summary>
    public class UIAssistRosterPanel : MonoBehaviour, IUIRefreshable
    {
        [Header("로스터")]
        [SerializeField] private Transform           _content;
        [SerializeField] private UIAssistRosterEntry _entryPrefab;
        [SerializeField] private TextMeshProUGUI     _rosterCountText; // N / 최대
        [SerializeField] private GameObject          _emptyRoot;       // 영입한 어시스트가 없을 때

        [Header("교체 대기 (로스터 가득 + 신규 영입)")]
        [SerializeField] private GameObject      _pendingRoot;
        [SerializeField] private Image           _pendingIcon;
        [SerializeField] private TextMeshProUGUI _pendingText;
        [SerializeField] private Button          _discardPendingButton; // 신규 영입 포기

        private const int MaxRosterSize = 4;

        private readonly List<UIAssistRosterEntry> _entries = new();

        private void Awake()
        {
            if (_discardPendingButton != null)
                _discardPendingButton.onClick.AddListener(OnDiscardPendingClicked);
        }

        private void OnEnable()
        {
            var manager = BossAssistManager.Instance;
            if (manager != null)
            {
                manager.OnRecruitmentResolved += OnRecruitmentResolved;
                manager.OnAssistCompleted += OnAssistCompleted;
            }
            Refresh();
        }

        private void OnDisable()
        {
            var manager = BossAssistManager.Instance;
            if (manager != null)
            {
                manager.OnRecruitmentResolved -= OnRecruitmentResolved;
                manager.OnAssistCompleted -= OnAssistCompleted;
            }
        }

        private void OnRecruitmentResolved(BossRecruitmentResult _) => Refresh();
        private void OnAssistCompleted(string _) => Refresh();

        public void Refresh()
        {
            var manager = BossAssistManager.Instance;
            AssistRosterService roster = manager?.Roster;
            if (roster == null)
            {
                SetEmptyVisible(true);
                if (_pendingRoot != null) _pendingRoot.SetActive(false);
                return;
            }

            bool replaceMode = !string.IsNullOrEmpty(roster.PendingRecruitAssistId);
            RebuildEntries(manager, roster, replaceMode);
            RefreshPendingBlock(manager, roster, replaceMode);

            if (_rosterCountText != null)
                _rosterCountText.text = $"{roster.Roster.Count} / {MaxRosterSize}";
            SetEmptyVisible(roster.Roster.Count == 0 && !replaceMode);
        }

        private void RebuildEntries(BossAssistManager manager, AssistRosterService roster, bool replaceMode)
        {
            foreach (var entry in _entries)
            {
                if (entry == null) continue;
                entry.OnClicked -= OnEntryClicked;
                Destroy(entry.gameObject);
            }
            _entries.Clear();

            if (_entryPrefab == null || _content == null) return;

            foreach (string assistId in roster.Roster)
            {
                var entry = Instantiate(_entryPrefab, _content);
                if (entry == null) continue;

                entry.Bind(
                    assistId,
                    manager.FindDefinition(assistId),
                    string.Equals(assistId, roster.EquippedAssistId, System.StringComparison.Ordinal),
                    manager.GetCooldownRemaining(assistId),
                    replaceMode);
                entry.OnClicked += OnEntryClicked;
                _entries.Add(entry);
            }
        }

        private void RefreshPendingBlock(BossAssistManager manager, AssistRosterService roster, bool replaceMode)
        {
            if (_pendingRoot != null) _pendingRoot.SetActive(replaceMode);
            if (!replaceMode) return;

            BossAssistDefinitionSO pending = manager.FindDefinition(roster.PendingRecruitAssistId);
            if (_pendingIcon != null)
            {
                _pendingIcon.sprite  = pending != null ? pending.icon : null;
                _pendingIcon.enabled = _pendingIcon.sprite != null;
            }
            if (_pendingText != null)
            {
                string name = pending != null && !string.IsNullOrWhiteSpace(pending.displayName)
                    ? pending.displayName
                    : "미확인 지원";
                string role = pending != null ? UIAssistRosterEntry.RoleLabel(pending.role) : "?";
                _pendingText.text = $"신규 영입 대기: {name} ({role})\n방출할 어시스트를 선택하거나 영입을 포기하세요.";
            }
        }

        // 일반 모드: 장착 교체. 교체 대기 모드: 선택한 어시스트를 방출하고 신규를 수락.
        private void OnEntryClicked(string assistId)
        {
            var roster = BossAssistManager.Instance?.Roster;
            if (roster == null) return;

            if (!string.IsNullOrEmpty(roster.PendingRecruitAssistId))
                roster.ResolvePending(assistId, acceptNew: true);
            else
                roster.Equip(assistId);

            Refresh();
        }

        private void OnDiscardPendingClicked()
        {
            var roster = BossAssistManager.Instance?.Roster;
            if (roster == null || string.IsNullOrEmpty(roster.PendingRecruitAssistId)) return;

            roster.ResolvePending(null, acceptNew: false);
            Refresh();
        }

        private void SetEmptyVisible(bool visible)
        {
            if (_emptyRoot != null) _emptyRoot.SetActive(visible);
        }
    }
}
