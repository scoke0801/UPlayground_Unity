using System.Collections.Generic;
using System;
using Random = UnityEngine.Random;
using UPlayGround.Components;
using UPlayGround.Data.Quest;
using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Actor.NPC;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;
using UPlayGround.InputDefine;
using UPlayGround.MovementController;
using UPlayGround.State;

namespace UPlayGround.UI
{
    /// <summary>월드 HUD의 기존 틱에서 주변 주민의 일상 발화와 제한된 말풍선 풀을 관리한다.</summary>
    public sealed class UINpcAmbientSpeechPresenter
    {
        private enum SpeechKind { Ambient, Reaction }
        private sealed class Resident
        {
            public NpcActor actor;
            public NpcMovementController movement;
            public Transform head;
            public float nextSpeech;
            public uint bumpSequence;
            public float nextReaction;
            public readonly Dictionary<int, UINpcSpeechSelector.History> histories = new();
        }

        private sealed class Slot
        {
            public UINpcSpeechBubble view;
            public Resident resident;
            public float end;
            public bool isFading;
            public NpcAmbientSpeechSO.Line line;
            public bool isReaction;
            public int voice;
            public NpcSpeechActivity activity;
            public float visibilityLostAt = -1f;
        }

        private readonly List<Resident> _residents = new();
        private readonly List<Slot> _slots = new();
        private readonly RaycastHit[] _hits = new RaycastHit[32];
        private readonly RectTransform _canvas;
        private readonly NpcAmbientSpeechSO _settings;
        private IUIActorRegistryService _registry;
        private Camera _camera;
        private float _nextDecision;
        private float _nextSpeech;
        private int _nextResident;
        private readonly UINpcSpeechSelector _selector;
        private NpcSpeechPeriod _period;
        private readonly Func<string, bool?> _readFlag = ReadFlag;
        private readonly Func<string, QuestStatus?> _readQuest = ReadQuest;

        public UINpcAmbientSpeechPresenter(Transform root, RectTransform canvas, Camera camera)
        {
            _canvas = canvas; _camera = camera;
            _settings = Resources.Load<NpcAmbientSpeechSO>(NpcAmbientSpeechSO.ResourcePath);
            if (_settings == null || _settings.bubblePrefab == null) return;
            _selector = new UINpcSpeechSelector(_settings, Random.Range(0, int.MaxValue));
            for (int i = 0; i < _settings.maxVisible; i++)
            {
                var view = UnityEngine.Object.Instantiate(_settings.bubblePrefab, root).GetComponent<UINpcSpeechBubble>();
                view.Clear();
                _slots.Add(new Slot { view = view });
            }
        }

        /// <summary>레지스트리가 준비된 뒤 구독하며 NPC별 Update나 월드 검색을 만들지 않는다.</summary>
        public void ManagedLateTick()
        {
            if (_settings == null || _slots.Count == 0) return;
            if (_registry == null && Services.TryGet<IUIActorRegistryService>(out var registry))
            {
                _registry = registry;
                _registry.OnActorRegistered += Register;
                _registry.OnActorUnregistered += Unregister;
                for (int i = 0; i < registry.AllActors.Count; i++) Register(registry.AllActors[i]);
            }
            if (_registry == null) return;
            bool canSpeak = CanPresent();
            _period = Services.TryGet<IGameTimeService>(out var clock)
                ? (NpcSpeechPeriod)(1 << (int)clock.CurrentDayPeriod) : 0;
            for (int i = 0; i < _slots.Count; i++) TickSlot(_slots[i], canSpeak);
            if (Time.time < _nextDecision) return;
            _nextDecision = Time.time + _settings.decisionInterval;
            // 카메라 교체·씬 전환은 저빈도 판단에서만 확인한다.
            if (Services.TryGet<ICameraViewService>(out var cameras)) _camera = cameras.GetMainCamera();
            if (!canSpeak || _camera == null || _residents.Count == 0) return;
            TryPresentReactions();
            if (Time.time < _nextSpeech) return;
            Slot available = null;
            for (int i = 0; i < _slots.Count; i++)
                if (_slots[i].resident == null) { available = _slots[i]; break; }
            if (available == null) return;
            for (int count = 0; count < _residents.Count; count++)
            {
                _nextResident %= _residents.Count;
                var resident = _residents[_nextResident++];
                if (Time.time < resident.nextSpeech || !CanSpeak(resident) || IsSpeaking(resident)) continue;
                if (!TryProject(resident, out var position) || HasOverlap(position, available.view.Rect.sizeDelta)) continue;
                if (TryPresent(available, resident, position)) break;
            }
        }

        private bool CanPresent()
        {
            if (Time.timeScale <= 0f || _registry.Player == null || !_registry.Player.isActiveAndEnabled) return false;
            if (!_registry.Player.IsAlive() || _registry.Player.GetCombat()?.IsInCombat == true) return false;
            if (Services.TryGet<IDialogueService>(out var dialogue) && dialogue.IsDialogueActive) return false;
            if (Services.TryGet<IInputService>(out var input) && input.CurrentLayer != InputLayer.Level_0) return false;
            if (Services.TryGet<IUISceneService>(out var scene) && !scene.ShowExplorationHud) return false;
            return true;
        }

        private bool CanSpeak(Resident resident, SpeechKind kind = SpeechKind.Ambient)
        {
            bool isReaction = kind == SpeechKind.Reaction;
            var actor = resident.actor;
            return actor != null && actor.isActiveAndEnabled && !actor.HasActorType(ActorType.Combat)
                && !actor.IsInteracting() && !actor.IsDialogueStaged
                && (!actor.IsRoutinePaused || isReaction && resident.movement?.CurrentState is NpcBumpState)
                && resident.movement != null
                && resident.movement.CurrentState is not NpcReactState
                && (isReaction || resident.movement.CurrentState is not NpcBumpState)
                && resident.movement.CurrentState is not NpcReturnState
                && resident.movement.Routine?.IsSocializing != true
                && resident.movement.Routine?.NeedsShelter != true;
        }

        private void TickSlot(Slot slot, bool canSpeak)
        {
            if (slot.resident == null) return;
            if (!canSpeak || !CanSpeak(slot.resident, slot.isReaction ? SpeechKind.Reaction : SpeechKind.Ambient) || Time.time >= slot.end)
            { Release(slot); return; }
            NpcSpeechActivity activity = slot.isReaction ? NpcSpeechActivity.Bumped : GetActivity(slot.resident);
            bool isLocomotionChange = slot.activity == NpcSpeechActivity.Idle && activity == NpcSpeechActivity.Walking
                || slot.activity == NpcSpeechActivity.Walking && activity == NpcSpeechActivity.Idle;
            NpcSpeechActivity routingActivity = isLocomotionChange ? slot.activity : activity;
            if (!slot.line.CanFinishSpeaking(slot.activity, activity, _period)
                || _settings.ResolveVoice(slot.resident.actor.GetData() as NpcActorSO, routingActivity, _readFlag, _readQuest) != slot.voice)
            { Release(slot); return; }
            if (!TryProject(slot.resident, out var position))
            {
                if (slot.visibilityLostAt < 0f) slot.visibilityLostAt = Time.time;
                slot.view.SetVisible(false);
                if (Time.time - slot.visibilityLostAt >= _settings.visibilityGrace) Release(slot);
                return;
            }
            if (slot.visibilityLostAt >= 0f)
            {
                if (!slot.isFading) slot.end += Time.time - slot.visibilityLostAt;
                slot.visibilityLostAt = -1f;
            }
            slot.view.SetVisible(true);
            slot.view.Rect.anchoredPosition = position;
            if (!slot.isFading && Time.time >= slot.end - slot.view.ExitSeconds)
            { slot.isFading = true; slot.view.FadeOut(); }
        }

        private bool TryPresent(Slot slot, Resident resident, Vector2 position, SpeechKind kind = SpeechKind.Ambient)
        {
            bool isReaction = kind == SpeechKind.Reaction;
            var data = resident.actor.GetData() as NpcActorSO;
            string speaker = _settings.GetSpeakerName(data);
            if (string.IsNullOrWhiteSpace(speaker)) speaker = resident.actor.Definition?.displayName;
            if (string.IsNullOrWhiteSpace(speaker)) return false;
            NpcSpeechActivity activity = isReaction ? NpcSpeechActivity.Bumped : GetActivity(resident);
            int voice = _settings.ResolveVoice(data, activity, _readFlag, _readQuest);
            if (!_settings.HasVoice(voice)) return false;
            var profile = _settings.voices[voice];
            if (!resident.histories.TryGetValue(voice, out var history))
            {
                history = new UINpcSpeechSelector.History(profile.lines.Length);
                resident.histories.Add(voice, history);
            }
            if (!_selector.TryChoose(profile, history, activity, _period, Time.time, out var line)) return false;
            slot.resident = resident;
            slot.isReaction = isReaction;
            slot.voice = voice;
            slot.activity = activity;
            slot.visibilityLostAt = -1f;
            slot.isFading = false;
            float duration = Mathf.Clamp(line.text.Length / _settings.charactersPerSecond, _settings.readingSeconds.x, _settings.readingSeconds.y);
            slot.line = line;
            slot.end = Time.time + duration;
            resident.nextSpeech = slot.end + Random.Range(_settings.interval.x, _settings.interval.y);
            _nextSpeech = Time.time + _settings.globalGap;
            slot.view.Rect.anchoredPosition = position;
            slot.view.Present(speaker, line.text);
            return true;
        }

        private NpcSpeechActivity GetActivity(Resident resident)
        {
            var movement = resident.movement;
            var routine = movement.Routine;
            if (routine != null && routine.IsWorking)
                return routine.IsResting ? NpcSpeechActivity.Resting : NpcSpeechActivity.Working;
            if (movement.CurrentState is NpcIdleState idle && idle.IsWorking) return NpcSpeechActivity.Working;
            return movement.Motor != null && movement.Motor.Velocity.sqrMagnitude > _settings.walkingSpeed * _settings.walkingSpeed
                ? NpcSpeechActivity.Walking : NpcSpeechActivity.Idle;
        }

        private static bool? ReadFlag(string key)
            => Services.TryGet<IGlobalFlagService>(out var flags) ? flags.GetFlag(key) : null;

        private static QuestStatus? ReadQuest(string key)
            => Services.TryGet<IQuestFlowService>(out var quests) ? quests.GetQuestStatus(key) : null;

        private void TryPresentReactions()
        {
            for (int i = 0; i < _residents.Count; i++)
            {
                var resident = _residents[i];
                // base.Awake의 등록보다 파생 Awake의 반응기 연결이 늦을 수 있다.
                var bump = resident.actor != null ? resident.actor.BumpReceiver : null;
                if (bump == null || bump.BumpSequence == resident.bumpSequence) continue;
                resident.bumpSequence = bump.BumpSequence;
                if (Time.time - bump.LastBumpTime > _settings.reactionWindow || Time.time < resident.nextReaction
                    || !CanSpeak(resident, SpeechKind.Reaction) || !TryProject(resident, out var position)) continue;
                Slot available = null;
                for (int j = 0; j < _slots.Count; j++)
                    if (_slots[j].resident == resident) { available = _slots[j]; break; }
                if (available == null)
                    for (int j = 0; j < _slots.Count; j++)
                        if (_slots[j].resident == null) { available = _slots[j]; break; }
                if (available == null || HasOverlap(position, available.view.Rect.sizeDelta, available)) continue;
                if (TryPresent(available, resident, position, SpeechKind.Reaction))
                    resident.nextReaction = Time.time + _settings.reactionCooldown;
            }
        }

        private bool TryProject(Resident resident, out Vector2 position)
        {
            position = default;
            if (_camera == null || resident.actor == null || _registry.Player == null) return false;
            Vector3 world = resident.head != null
                ? resident.head.position + Vector3.up * _settings.headClearance
                : resident.actor.transform.position + _settings.fallbackOffset;
            if ((world - _registry.Player.transform.position).sqrMagnitude > _settings.maxDistance * _settings.maxDistance) return false;
            Vector3 screen = _camera.WorldToScreenPoint(world);
            if (screen.z <= _camera.nearClipPlane) return false;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas, screen, null, out position)) return false;
            // 가장자리로 강제 고정하면 누가 말하는지 잃으므로 화면 밖 발화는 숨긴다.
            Vector2 size = _slots[0].view.Rect.sizeDelta;
            Rect bounds = _canvas.rect;
            if (position.x - size.x * 0.5f < bounds.xMin || position.x + size.x * 0.5f > bounds.xMax
                || position.y < bounds.yMin || position.y + size.y > bounds.yMax) return false;
            Vector3 origin = _camera.transform.position;
            Vector3 direction = world - origin;
            int count = Physics.RaycastNonAlloc(origin, direction.normalized, _hits, direction.magnitude,
                _settings.occlusionMask, QueryTriggerInteraction.Ignore);
            if (count == _hits.Length) return false;
            for (int i = 0; i < count; i++)
            {
                Transform hit = _hits[i].transform;
                if (hit.IsChildOf(resident.actor.transform) || hit.IsChildOf(_registry.Player.transform)) continue;
                return false;
            }
            return true;
        }

        private bool HasOverlap(Vector2 position, Vector2 size, Slot ignored = null)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i] == ignored || _slots[i].resident == null) continue;
                Vector2 delta = position - _slots[i].view.Rect.anchoredPosition;
                if (Mathf.Abs(delta.x) < size.x + _settings.overlapPadding
                    && Mathf.Abs(delta.y) < size.y + _settings.overlapPadding) return true;
            }
            return false;
        }

        private bool IsSpeaking(Resident resident)
        {
            for (int i = 0; i < _slots.Count; i++) if (_slots[i].resident == resident) return true;
            return false;
        }

        private void Register(GameActor actor)
        {
            if (actor is not NpcActor npc) return;
            for (int i = 0; i < _residents.Count; i++) if (_residents[i].actor == npc) return;
            _residents.Add(new Resident { actor = npc, movement = npc.GetComponent<NpcMovementController>(),
                bumpSequence = npc.BumpReceiver != null ? npc.BumpReceiver.BumpSequence : 0,
                head = npc.GetSocket(ActorSocketType.UI_HpBar),
                nextSpeech = Time.time + Random.Range(_settings.initialDelay.x, _settings.initialDelay.y) });
        }

        private void Unregister(GameActor actor)
        {
            for (int i = _residents.Count - 1; i >= 0; i--)
            {
                if (_residents[i].actor != actor) continue;
                for (int j = 0; j < _slots.Count; j++) if (_slots[j].resident == _residents[i]) Release(_slots[j]);
                _residents.RemoveAt(i);
            }
        }

        private void Release(Slot slot)
        { if (slot.view != null) slot.view.Clear(); slot.resident = null; slot.isFading = false; }

        /// <summary>HUD를 숨겼다가 복귀해도 중단된 트윈과 표시가 남지 않게 한다.</summary>
        public void HideAll()
        {
            for (int i = 0; i < _slots.Count; i++) Release(_slots[i]);
        }

        /// <summary>HUD 파괴·재초기화 시 이벤트와 풀을 해제한다.</summary>
        public void Dispose()
        {
            if (_registry != null)
            { _registry.OnActorRegistered -= Register; _registry.OnActorUnregistered -= Unregister; }
            for (int i = 0; i < _slots.Count; i++)
                if (_slots[i].view != null) { _slots[i].view.Clear(); UnityEngine.Object.Destroy(_slots[i].view.gameObject); }
            _slots.Clear(); _residents.Clear(); _registry = null;
        }
    }
}
