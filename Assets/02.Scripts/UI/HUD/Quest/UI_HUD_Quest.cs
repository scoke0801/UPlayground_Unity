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
    /// HUD 퀘스트 정보 UI
    /// </summary>
    public class UI_HUD_Quest : UI_Base
    {
        private const string DefaultMainQuestIdPrefix = "quest_main_";
        private const string LegacyMainQuestIdPrefix = "main_";

        [Header("컴포넌트")]
        [SerializeField] private TextMeshProUGUI _questTitleText;
        [SerializeField] private TextMeshProUGUI _questDescText;
        [SerializeField] private GameObject _questCompletePanel;
        [SerializeField] private CanvasGroup _questCompleteCanvasGroup;
        [SerializeField] private TextMeshProUGUI _questCompleteTitleText;
        [SerializeField] private TextMeshProUGUI _questCompleteNameText;

        [Header("표시 설정")]
        [SerializeField] private string _mainQuestIdPrefix = DefaultMainQuestIdPrefix;
        [SerializeField] private bool _hideWhenNoActiveMainQuest = true;
        [SerializeField] private float _questCompleteShowSeconds = 2.5f;

        private readonly StringBuilder _descriptionBuilder = new();

        private bool _isSubscribed;
        private bool _isWaitingForDatabaseLoad;
        private Coroutine _questCompleteCoroutine;

        #region UI_Base 생명주기

        protected override void Awake()
        {
            base.Awake();
            CacheTextComponents();
            CacheQuestCompleteComponents();

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
        }

        protected override void OnDispose()
        {
            UnsubscribeQuestEvents();
        }

        protected override void Update()
        {
            base.Update();

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
                SetVisible(false);
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

            SetVisible(true);
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

                string questId = quest.QuestSO.questId;
                if (!IsMainQuestId(questId))
                {
                    continue;
                }

                if (selectedQuest == null || string.CompareOrdinal(questId, selectedQuest.QuestSO.questId) < 0)
                {
                    selectedQuest = quest;
                }
            }

            return selectedQuest;
        }

        private bool IsMainQuestId(string questId)
        {
            if (string.IsNullOrEmpty(questId))
            {
                return false;
            }

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

            foreach (var objective in quest.GetVisibleObjectives())
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

            SetVisible(!_hideWhenNoActiveMainQuest);
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

            SetVisible(true);

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
        }

        private void SetVisible(bool visible)
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
            ShowQuestComplete(data);
        }

        private void OnQuestObjectiveUpdated(QuestObjectiveEventData data)
        {
            RefreshQuestInfo();
        }
    }
}
