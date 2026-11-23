using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Quest;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    /// <summary>
    /// HUD 퀘스트 정보 UI. 평소에는 숨기고, <see cref="PeekQuestInfo"/>가 불릴 때(길안내 입력) 표시할 퀘스트가 있으면
    /// 잠시 보여 준 뒤 부드럽게 숨긴다. 퀘스트 달성 연출 패널은 이 노출과 별개로 항상 동작한다.
    /// 페이드는 트윈 대신 unscaled 시간으로 직접 진행해 히트스톱·일시정지 중에도 멈추지 않는다.
    /// </summary>
    public class UI_HUD_Quest : UI_Base
    {
        private const string DefaultMainQuestIdPrefix = "quest_main_";
        private const string LegacyMainQuestIdPrefix = "main_";

        [Header("컴포넌트")]
        [SerializeField] private GameObject _questTitlePanel;
        [SerializeField] private GameObject _questDetailPanel;
        [SerializeField] private TextMeshProUGUI _questTitleText;
        [SerializeField] private TextMeshProUGUI _questDescText;
        [SerializeField] private GameObject _questCompletePanel;
        [SerializeField] private CanvasGroup _questCompleteCanvasGroup;
        [SerializeField] private TextMeshProUGUI _questCompleteTitleText;
        [SerializeField] private TextMeshProUGUI _questCompleteNameText;

        [Header("표시 설정")]
        [SerializeField] private string _mainQuestIdPrefix = DefaultMainQuestIdPrefix;
        [SerializeField] private float _questCompleteShowSeconds = 2.5f;

        [Header("길안내 입력 시 노출")]
        [Tooltip("입력 후 퀘스트 정보를 보여 주는 시간(초).")]
        [SerializeField, Min(0.1f)] private float _peekShowSeconds = 6f;
        [Tooltip("나타날 때 걸리는 시간(초).")]
        [SerializeField, Min(0.01f)] private float _peekFadeInSeconds = 0.2f;
        [Tooltip("사라질 때 걸리는 시간(초).")]
        [SerializeField, Min(0.01f)] private float _peekFadeOutSeconds = 0.5f;

        private readonly StringBuilder _descriptionBuilder = new();

        private bool _isSubscribed;
        private bool _isWaitingForDatabaseLoad;
        private bool _hasQuestToDisplay;
        private bool _isPeeking;
        private bool _isFadingOut;
        private float _peekEndTime;
        private Coroutine _questCompleteCoroutine;

        #region UI_Base 생명주기

        protected override void Awake()
        {
            base.Awake();
            CacheQuestContentPanels();
            CacheTextComponents();
            CacheQuestCompleteComponents();

            HideQuestContent();

            if (_questCompletePanel != null)
            {
                _questCompletePanel.SetActive(false);
            }
        }

        protected override void OnShow()
        {
            base.OnShow();
            SubscribeQuestEvents();
            RefreshQuestInfo();
        }

        protected override void OnHide()
        {
            UnsubscribeQuestEvents();
            HideQuestContent();
        }

        protected override void OnDispose()
        {
            UnsubscribeQuestEvents();
        }

        protected override void Update()
        {
            base.Update();
            UpdatePeek();

            if (!_isWaitingForDatabaseLoad)
            {
                return;
            }

            if (UISvc.Quest == null || !UISvc.Quest.IsDBLoaded)
            {
                return;
            }

            _isWaitingForDatabaseLoad = false;
            RefreshQuestInfo();
        }

        #endregion

        private void CacheTextComponents()
        {
            if (_questTitleText != null && _questDescText != null)
            {
                return;
            }

            var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in texts)
            {
                if (_questTitleText == null && text.name == "QuestTitleText")
                {
                    _questTitleText = text;
                }
                else if (_questDescText == null && text.name == "QuestDescText")
                {
                    _questDescText = text;
                }
            }
        }

        private void SubscribeQuestEvents()
        {
            if (_isSubscribed || Svc.Events == null)
            {
                return;
            }

            var ev = Svc.Events;
            ev.Subscribe<QuestEvent, QuestStateEventData>(QuestEvent.QuestAccepted, OnQuestStateChanged);
            ev.Subscribe<QuestEvent, QuestStateEventData>(QuestEvent.QuestCompleted, OnQuestCompleted);
            ev.Subscribe<QuestEvent, QuestStateEventData>(QuestEvent.QuestFailed, OnQuestStateChanged);
            ev.Subscribe<QuestEvent, QuestStateEventData>(QuestEvent.QuestTracked, OnQuestStateChanged);
            ev.Subscribe<QuestEvent, QuestStateEventData>(QuestEvent.QuestUntracked, OnQuestStateChanged);
            ev.Subscribe<QuestEvent, QuestObjectiveEventData>(QuestEvent.QuestObjectiveUpdated, OnQuestObjectiveUpdated);
            _isSubscribed = true;
        }

        private void UnsubscribeQuestEvents()
        {
            if (!_isSubscribed || Svc.Events == null)
            {
                _isSubscribed = false;
                return;
            }

            var ev = Svc.Events;
            ev.Unsubscribe<QuestEvent, QuestStateEventData>(QuestEvent.QuestAccepted, OnQuestStateChanged);
            ev.Unsubscribe<QuestEvent, QuestStateEventData>(QuestEvent.QuestCompleted, OnQuestCompleted);
            ev.Unsubscribe<QuestEvent, QuestStateEventData>(QuestEvent.QuestFailed, OnQuestStateChanged);
            ev.Unsubscribe<QuestEvent, QuestStateEventData>(QuestEvent.QuestTracked, OnQuestStateChanged);
            ev.Unsubscribe<QuestEvent, QuestStateEventData>(QuestEvent.QuestUntracked, OnQuestStateChanged);
            ev.Unsubscribe<QuestEvent, QuestObjectiveEventData>(QuestEvent.QuestObjectiveUpdated, OnQuestObjectiveUpdated);
            _isSubscribed = false;
        }

        private void RefreshQuestInfo()
        {
            CacheTextComponents();

            var questManager = UISvc.Quest;
            if (questManager == null || !questManager.IsDBLoaded)
            {
                _isWaitingForDatabaseLoad = true;
                _hasQuestToDisplay = false;
                HideQuestContent();
                return;
            }

            _isWaitingForDatabaseLoad = false;

            var mainQuest = FindQuestToDisplay(questManager);
            if (mainQuest == null)
            {
                ClearQuestInfo();
                return;
            }

            if (_questTitleText != null)
            {
                _questTitleText.text = mainQuest.QuestSO.questName;
            }

            if (_questDescText != null)
            {
                _questDescText.text = BuildQuestDescription(mainQuest);
            }

            // 내용 패널은 노출 중일 때만 보인다. 여기서는 텍스트만 갱신하고 표시 여부는 건드리지 않는다.
            _hasQuestToDisplay = true;
        }

        /// <summary>
        /// 표시할 퀘스트(추적 중이거나 활성 메인 퀘스트)가 있으면 잠시 보여 준 뒤 부드럽게 숨긴다.
        /// 이미 보이는 중이면 타이머만 갱신한다. 표시할 퀘스트가 없으면 아무것도 노출하지 않고 false를 돌려준다.
        /// </summary>
        public bool PeekQuestInfo()
        {
            RefreshQuestInfo();
            if (!_hasQuestToDisplay)
            {
                return false;
            }

            _peekEndTime = Time.unscaledTime + Mathf.Max(0.1f, _peekShowSeconds);
            if (_isPeeking)
            {
                return true;
            }

            bool wasFadingOut = _isFadingOut;
            _isPeeking = true;
            _isFadingOut = false;
            SetQuestContentActive(true);

            // 완전히 숨은 상태에서 시작하면 0부터 나타나고, 사라지는 중이면 그 자리에서 다시 나타난다.
            if (_canvasGroup != null && !wasFadingOut && _questCompleteCoroutine == null)
            {
                SetCanvasVisible(false);
            }

            return true;
        }

        private void UpdatePeek()
        {
            if (_isPeeking && Time.unscaledTime >= _peekEndTime)
            {
                _isPeeking = false;
                if (_questCompleteCoroutine != null)
                {
                    // 달성 연출이 루트 알파를 쓰는 중이므로 페이드하지 않고 내용만 접는다.
                    SetQuestContentActive(false);
                }
                else
                {
                    _isFadingOut = true;
                }
            }

            if (_canvasGroup == null || _questCompleteCoroutine != null || (!_isPeeking && !_isFadingOut))
            {
                return;
            }

            float target = _isPeeking ? 1f : 0f;
            float duration = _isPeeking ? _peekFadeInSeconds : _peekFadeOutSeconds;
            _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, target, Time.unscaledDeltaTime / duration);

            if (_isFadingOut && _canvasGroup.alpha <= 0f)
            {
                HideQuestContent();
            }
        }

        // 내용 패널을 접고 노출 상태를 끝낸다. 달성 연출이 진행 중이면 루트 알파는 연출에 맡긴다.
        private void HideQuestContent()
        {
            _isPeeking = false;
            _isFadingOut = false;
            SetQuestContentActive(false);
            if (_questCompleteCoroutine == null)
            {
                SetCanvasVisible(false);
            }
        }

        private QuestRuntimeData FindQuestToDisplay(IUIQuestService questManager)
        {
            if (questManager.IsQuestTrackingSuppressed)
            {
                return null;
            }

            var trackedQuest = questManager.GetTrackedQuestRuntime();
            return trackedQuest ?? FindActiveMainQuest(questManager);
        }

        private QuestRuntimeData FindActiveMainQuest(IUIQuestService questManager)
        {
            QuestRuntimeData selectedQuest = null;

            foreach (var quest in questManager.GetActiveQuests())
            {
                if (quest?.QuestSO == null)
                {
                    continue;
                }

                QuestSO questData = quest.QuestSO;
                if (!IsMainQuest(questData))
                {
                    continue;
                }

                if (selectedQuest == null ||
                    string.CompareOrdinal(questData.questId, selectedQuest.QuestSO.questId) < 0)
                {
                    selectedQuest = quest;
                }
            }

            return selectedQuest;
        }

        private bool IsMainQuest(QuestSO quest)
        {
            if (quest == null)
            {
                return false;
            }

            if (quest.questType == QuestType.Main)
            {
                return true;
            }

            string questId = quest.questId;
            if (string.IsNullOrEmpty(questId))
            {
                return false;
            }

            // 기존 세이브와 레거시 에셋은 분류값이 없을 수 있어 ID 접두사를 호환 경로로 유지한다.
            if (!string.IsNullOrEmpty(_mainQuestIdPrefix) &&
                questId.StartsWith(_mainQuestIdPrefix, System.StringComparison.Ordinal))
            {
                return true;
            }

            return questId.StartsWith(DefaultMainQuestIdPrefix, System.StringComparison.Ordinal) ||
                   questId.StartsWith(LegacyMainQuestIdPrefix, System.StringComparison.Ordinal);
        }

        private string BuildQuestDescription(QuestRuntimeData quest)
        {
            _descriptionBuilder.Clear();

            // HUD는 지금 해야 할 목표만 안내한다. 완료된 목표를 남겨두면 현재 목표가 어느 줄인지 읽히지 않는다.
            foreach (var objective in quest.GetActiveObjectives())
            {
                if (objective == null)
                {
                    continue;
                }

                if (_descriptionBuilder.Length > 0)
                {
                    _descriptionBuilder.AppendLine();
                }

                _descriptionBuilder.Append(objective.description);

                if (objective.requiredCount > 1)
                {
                    int currentCount = quest.ObjectiveProgress.TryGetValue(objective.objectiveId, out var count)
                        ? Mathf.Min(count, objective.requiredCount)
                        : 0;
                    _descriptionBuilder.Append(" (");
                    _descriptionBuilder.Append(currentCount);
                    _descriptionBuilder.Append('/');
                    _descriptionBuilder.Append(objective.requiredCount);
                    _descriptionBuilder.Append(')');
                }
            }

            if (_descriptionBuilder.Length == 0)
            {
                _descriptionBuilder.Append(quest.QuestSO.questDescription);
            }

            return _descriptionBuilder.ToString();
        }

        private void ClearQuestInfo()
        {
            if (_questTitleText != null)
            {
                _questTitleText.text = string.Empty;
            }

            if (_questDescText != null)
            {
                _questDescText.text = string.Empty;
            }

            _hasQuestToDisplay = false;
            HideQuestContent();
        }

        private void ShowQuestComplete(QuestStateEventData data)
        {
            CacheQuestCompleteComponents();

            if (_questCompletePanel == null && _questCompleteTitleText == null && _questCompleteNameText == null)
            {
                return;
            }

            if (_questCompleteTitleText != null)
            {
                _questCompleteTitleText.text = "퀘스트 달성";
            }

            if (_questCompleteNameText != null)
            {
                _questCompleteNameText.text = string.IsNullOrEmpty(data?.QuestName) ? data?.QuestId ?? string.Empty : data.QuestName;
            }

            if (_isFadingOut)
            {
                // 사라지던 내용이 루트 알파 복구와 함께 다시 번쩍이지 않게 접는다.
                _isFadingOut = false;
                SetQuestContentActive(false);
            }

            SetCanvasVisible(true);

            if (_questCompleteCoroutine != null)
            {
                StopCoroutine(_questCompleteCoroutine);
            }

            _questCompleteCoroutine = StartCoroutine(ShowQuestCompleteCoroutine());
        }

        private void CacheQuestCompleteComponents()
        {
            if (_questCompletePanel == null)
            {
                var completeTransform = transform.Find("QuestCompletePanel");
                if (completeTransform != null)
                {
                    _questCompletePanel = completeTransform.gameObject;
                }
            }

            if (_questCompleteCanvasGroup == null && _questCompletePanel != null)
            {
                _questCompleteCanvasGroup = _questCompletePanel.GetComponent<CanvasGroup>();
                if (_questCompleteCanvasGroup == null)
                {
                    _questCompleteCanvasGroup = _questCompletePanel.AddComponent<CanvasGroup>();
                }
            }

            var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in texts)
            {
                if (_questCompleteTitleText == null && text.name == "QuestCompleteTitleText")
                {
                    _questCompleteTitleText = text;
                }
                else if (_questCompleteNameText == null && text.name == "QuestCompleteNameText")
                {
                    _questCompleteNameText = text;
                }
            }
        }

        private IEnumerator ShowQuestCompleteCoroutine()
        {
            if (_questCompletePanel != null)
            {
                _questCompletePanel.SetActive(true);
            }

            if (_questCompleteCanvasGroup != null)
            {
                _questCompleteCanvasGroup.alpha = 1f;
            }

            yield return new WaitForSeconds(_questCompleteShowSeconds);

            if (_questCompleteCanvasGroup != null)
            {
                _questCompleteCanvasGroup.alpha = 0f;
            }

            if (_questCompletePanel != null)
            {
                _questCompletePanel.SetActive(false);
            }

            _questCompleteCoroutine = null;
            RefreshQuestInfo();

            // 노출 중이 아니면 루트도 다시 숨긴다(노출 중이면 내용이 계속 보인다).
            if (!_isPeeking)
            {
                SetCanvasVisible(false);
            }
        }

        private void CacheQuestContentPanels()
        {
            if (_questTitlePanel == null)
            {
                _questTitlePanel = transform.Find("Image/QuestTitlePanel")?.gameObject;
            }

            if (_questDetailPanel == null)
            {
                _questDetailPanel = transform.Find("QuestDetailPanel")?.gameObject;
            }
        }

        // 제목·상세 패널만 켜고 끈다. 루트 알파는 노출 페이드와 달성 연출이 각자 관리한다.
        private void SetQuestContentActive(bool active)
        {
            CacheQuestContentPanels();

            if (_questTitlePanel != null)
            {
                _questTitlePanel.SetActive(active);
            }

            if (_questDetailPanel != null)
            {
                _questDetailPanel.SetActive(active);
            }
        }

        private void SetCanvasVisible(bool visible)
        {
            if (_canvasGroup == null)
            {
                return;
            }

            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }

        private void OnQuestStateChanged(QuestStateEventData data)
        {
            RefreshQuestInfo();
        }

        private void OnQuestCompleted(QuestStateEventData data)
        {
            RefreshQuestInfo();
            QuestSO completedQuest = UISvc.Quest?.GetQuestData(data?.QuestId);
            if (completedQuest != null && completedQuest.suppressCompletionPresentation)
                return;

            ShowQuestComplete(data);
        }

        private void OnQuestObjectiveUpdated(QuestObjectiveEventData data)
        {
            RefreshQuestInfo();
        }
    }
}
