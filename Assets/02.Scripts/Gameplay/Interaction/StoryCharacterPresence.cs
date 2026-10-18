using System;
using System.Collections;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;

namespace UPlayGround.Gameplay.Interaction
{
    /// <summary>사건 현장의 비전투 인물을 저장된 주인공·동의·현재 조작 인물에 맞춰 표시한다.</summary>
    public sealed class StoryCharacterPresence : MonoBehaviour
    {
        [Serializable]
        public sealed class Participant
        {
            public GameObject actor;
            public CharacterActorType character;
            public CharacterActorType requiredProtagonist;
            public string completedFlag;
            public string requiredFlag;
        }

        [SerializeField] private Participant[] _participants = Array.Empty<Participant>();
        private IPartyService _party;
        private IGlobalFlagService _flags;
        private PartyManager _partyEvents;
        private Coroutine _binding;

        private void Awake() => HideAll();
        private void OnEnable() => _binding = StartCoroutine(Bind());

        private IEnumerator Bind()
        {
            while (!Services.TryGet(out _party) || !Services.TryGet(out _flags)) yield return null;
            // 이 컴포넌트는 Assembly-CSharp의 씬 통합 계층이다. 등록된 서비스 인스턴스만 사용한다.
            _partyEvents = _party as PartyManager;
            if (_partyEvents != null)
            {
                _partyEvents.OnSwapStarted += OnSwapStarted;
                _partyEvents.OnSwapCompleted += OnSwapCompleted;
                _partyEvents.OnRosterChanged += Refresh;
            }
            _flags.OnFlagChanged += OnFlagChanged;
            _flags.OnFlagsReloaded += Refresh;
            yield return null;
            Refresh();
            _binding = null;
        }

        private void OnSwapStarted(PlayerActor previous, PlayerActor next) => HideAll();
        private void OnSwapCompleted(PlayerActor actor) => Refresh();
        private void OnFlagChanged(string key, bool value) => Refresh();

        private void Refresh()
        {
            if (_party == null || _flags == null) return;
            foreach (var item in _participants)
            {
                bool visible = (item.requiredProtagonist == CharacterActorType.None
                    || item.requiredProtagonist == _party.StoryProtagonistType)
                    && (string.IsNullOrEmpty(item.requiredFlag) || _flags.GetFlag(item.requiredFlag))
                    && item.character != _party.ActiveCharacterType
                    && (string.IsNullOrEmpty(item.completedFlag) || !_flags.GetFlag(item.completedFlag));
                if (item.actor != null && item.actor.activeSelf != visible) item.actor.SetActive(visible);
            }
        }

        private void HideAll()
        {
            foreach (var item in _participants)
                if (item.actor != null) item.actor.SetActive(false);
        }

        private void OnDisable()
        {
            if (_binding != null) StopCoroutine(_binding);
            _binding = null;
            if (_flags != null)
            {
                _flags.OnFlagChanged -= OnFlagChanged;
                _flags.OnFlagsReloaded -= Refresh;
            }
            if (_partyEvents != null)
            {
                _partyEvents.OnSwapStarted -= OnSwapStarted;
                _partyEvents.OnSwapCompleted -= OnSwapCompleted;
                _partyEvents.OnRosterChanged -= Refresh;
            }
            _flags = null;
            _party = null;
            _partyEvents = null;
            HideAll();
        }
    }
}
