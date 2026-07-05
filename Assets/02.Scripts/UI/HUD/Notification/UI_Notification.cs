using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Path;
using UPlayGround.Data.Party;
using UPlayGround.Data.Quest;
using UPlayGround.Manager;

namespace UPlayGround.UI.HUD.Notification
{
    /// <summary>
    /// 게임 진행 중 시스템 메시지를 잠깐 표시하는 HUD Notification.
    /// 커서와 InputLayer를 건드리지 않도록 HUD 레이어 전용으로 사용한다.
    /// </summary>
    public class UI_Notification : UI_Base
    {
        private static readonly Color SystemAccent = new Color(0.35f, 0.75f, 0.95f, 1f);
        private static readonly Color QuestAccent = new Color(1f, 0.68f, 0.22f, 1f);
        private static readonly Color PartyAccent = new Color(0.55f, 0.9f, 0.55f, 1f);

        [SerializeField] private UI_NotificationEntry _entryPrefab;
        [SerializeField] private Transform _content;
        [SerializeField] private int _maxVisibleEntries = 4;
        [SerializeField] private bool _listenQuestCompleted = true;
        [SerializeField] private bool _listenPartyUnlocked = true;

        private readonly List<UI_NotificationEntry> _entries = new();
        private IDisposable _questCompletedSubscription;
        private PartyManager _subscribedPartyManager;

        protected override bool RequiresCursorVisible => false;
        protected override bool BlocksLowerInput => false;

        protected override void Awake()
        {
            base.Awake();
            _layer = CanvasLayer.HUD;
            _canCloseWithEsc = false;
            SetInteractable(false);
            CacheContent();
        }

        protected override void OnShow()
        {
            base.OnShow();
            SetInteractable(false);
            SubscribeEvents();
        }

        protected override void OnHide()
        {
            UnsubscribeEvents();
            base.OnHide();
        }

        protected override void OnDispose()
        {
            UnsubscribeEvents();
            base.OnDispose();
        }

        public static UI_Notification ShowSystemMessage(string title, string message, Sprite icon = null)
        {
            var ui = EnsureVisible();
            ui?.ShowMessage(title, message, icon, SystemAccent);
            return ui;
        }

        public static UI_Notification ShowQuestCompleted(string questName)
        {
            var ui = EnsureVisible();
            ui?.ShowMessage("퀘스트 달성", string.IsNullOrWhiteSpace(questName) ? "퀘스트를 완료했습니다." : questName, null, QuestAccent);
            return ui;
        }

        public static UI_Notification ShowPartyMemberUnlocked(CharacterActorType type)
        {
            var ui = EnsureVisible();
            if (ui == null)
                return null;

            ui.ResolvePartyMember(type, out string name, out Sprite icon);
            ui.ShowMessage("파티원 합류", $"{name}이(가) 파티에 합류했습니다.", icon, PartyAccent);
            return ui;
        }

        public void ShowMessage(string title, string message, Sprite icon = null, Color? accentColor = null)
        {
            if (_entryPrefab == null)
            {
                Debug.LogWarning("[UI_Notification] Entry 프리팹이 연결되지 않았습니다.");
                return;
            }

            CacheContent();
            Transform parent = _content != null ? _content : transform;
            var entry = Instantiate(_entryPrefab, parent);
            entry.gameObject.SetActive(true);
            entry.Init(title, message, icon, accentColor ?? SystemAccent);

            _entries.Add(entry);
            TrimEntries();
        }

        private static UI_Notification EnsureVisible()
        {
            if (UIManager.Instance == null)
                return null;

            var obj = UIManager.Instance.ShowUI(UIKeyType.Notification, CanvasLayer.HUD);
            return obj != null ? obj.GetComponent<UI_Notification>() : null;
        }

        private void SubscribeEvents()
        {
            if (_listenQuestCompleted && _questCompletedSubscription == null && EventManager.Instance != null)
            {
                _questCompletedSubscription = EventManager.Instance.Subscribe<QuestEvent, QuestStateEventData>(
                    QuestEvent.QuestCompleted,
                    OnQuestCompleted);
            }

            if (_listenPartyUnlocked && _subscribedPartyManager == null && PartyManager.Instance != null)
            {
                _subscribedPartyManager = PartyManager.Instance;
                _subscribedPartyManager.OnCharacterUnlocked += OnCharacterUnlocked;
            }
        }

        private void UnsubscribeEvents()
        {
            _questCompletedSubscription?.Dispose();
            _questCompletedSubscription = null;

            if (_subscribedPartyManager != null)
            {
                _subscribedPartyManager.OnCharacterUnlocked -= OnCharacterUnlocked;
                _subscribedPartyManager = null;
            }
        }

        private void OnQuestCompleted(QuestStateEventData data)
        {
            string questName = string.IsNullOrWhiteSpace(data?.QuestName) ? data?.QuestId : data.QuestName;
            ShowMessage("퀘스트 달성", string.IsNullOrWhiteSpace(questName) ? "퀘스트를 완료했습니다." : questName, null, QuestAccent);
        }

        private void OnCharacterUnlocked(CharacterActorType type)
        {
            ResolvePartyMember(type, out string name, out Sprite icon);
            ShowMessage("파티원 합류", $"{name}이(가) 파티에 합류했습니다.", icon, PartyAccent);
        }

        private void ResolvePartyMember(CharacterActorType type, out string name, out Sprite icon)
        {
            PartyMemberDataSO memberData = PartyManager.Instance?.PartyMemberDataSO;
            name = memberData != null ? memberData.GetName(type) : string.Empty;
            if (string.IsNullOrWhiteSpace(name))
                name = type.ToString();

            icon = memberData != null ? memberData.GetHeadSprite(type) : null;
        }

        private void CacheContent()
        {
            if (_content != null)
                return;

            var content = transform.Find("Content");
            if (content != null)
                _content = content;
        }

        private void TrimEntries()
        {
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                if (_entries[i] == null)
                    _entries.RemoveAt(i);
            }

            int max = Mathf.Max(1, _maxVisibleEntries);
            while (_entries.Count > max)
            {
                var oldest = _entries[0];
                _entries.RemoveAt(0);
                oldest?.ForceClose();
            }
        }
    }
}
