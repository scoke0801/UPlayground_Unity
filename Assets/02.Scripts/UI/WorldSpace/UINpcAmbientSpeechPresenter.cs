using System.Collections.Generic;
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
        private sealed class Resident
        {
            public NpcActor actor;
            public NpcMovementController movement;
            public Transform head;
            public float nextSpeech;
            public int voice = -1;
            public UINpcSpeechSelector.History history;
        }

        private sealed class Slot
        {
            public UINpcSpeechBubble view;
            public Resident resident;
            public float end;
            public bool isFading;
            public NpcAmbientSpeechSO.Line line;
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

        public UINpcAmbientSpeechPresenter(Transform root, RectTransform canvas, Camera camera)
        {
            _canvas = canvas; _camera = camera;
            _settings = Resources.Load<NpcAmbientSpeechSO>(NpcAmbientSpeechSO.ResourcePath);
            if (_settings == null || _settings.bubblePrefab == null) return;
            _selector = new UINpcSpeechSelector(_settings, Random.Range(0, int.MaxValue));
            for (int i = 0; i < _settings.maxVisible; i++)
            {
                var view = Object.Instantiate(_settings.bubblePrefab, root).GetComponent<UINpcSpeechBubble>();
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
            if (!canSpeak || _camera == null || Time.time < _nextSpeech || _residents.Count == 0) return;
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

        private bool CanSpeak(Resident resident)
        {
            var actor = resident.actor;
            var data = actor != null ? actor.GetData() as NpcActorSO : null;
            string requiredFlag = _settings.GetRequiredFlag(data);
            if (!string.IsNullOrEmpty(requiredFlag)
                && (!Services.TryGet<IGlobalFlagService>(out var flags) || !flags.GetFlag(requiredFlag))) return false;
            return actor != null && actor.isActiveAndEnabled && !actor.HasActorType(ActorType.Combat)
                && !actor.IsInteracting() && !actor.IsDialogueStaged && !actor.IsRoutinePaused
                && resident.movement != null
                && resident.movement.CurrentState is not NpcReactState
                && resident.movement.CurrentState is not NpcBumpState
                && resident.movement.CurrentState is not NpcReturnState
                && resident.movement.Routine?.IsSocializing != true
                && resident.movement.Routine?.NeedsShelter != true;
        }

        private void TickSlot(Slot slot, bool canSpeak)
        {
            if (slot.resident == null) return;
            if (!canSpeak || !CanSpeak(slot.resident) || Time.time >= slot.end
                || !slot.line.CanSpeak(GetActivity(slot.resident), _period)
                || !TryProject(slot.resident, out var position))
            { Release(slot); return; }
            slot.view.Rect.anchoredPosition = position;
            if (!slot.isFading && Time.time >= slot.end - slot.view.ExitSeconds)
            { slot.isFading = true; slot.view.FadeOut(); }
        }

        private bool TryPresent(Slot slot, Resident resident, Vector2 position)
        {
            var data = resident.actor.GetData() as NpcActorSO;
            string speaker = _settings.GetSpeakerName(data);
            if (string.IsNullOrWhiteSpace(speaker)) speaker = resident.actor.Definition?.displayName;
            if (string.IsNullOrWhiteSpace(speaker)) return false;
            int voice = _settings.ResolveVoice(data);
            if (!_settings.HasVoice(voice)) return false;
            var profile = _settings.voices[voice];
            if (resident.voice != voice || resident.history == null)
            {
                resident.voice = voice;
                resident.history = new UINpcSpeechSelector.History(profile.lines.Length);
            }
            if (!_selector.TryChoose(profile, resident.history, GetActivity(resident), _period, Time.time, out var line)) return false;
            slot.resident = resident;
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
            return movement.Motor != null && movement.Motor.Velocity.sqrMagnitude > _settings.walkingSpeed * _settings.walkingSpeed
                ? NpcSpeechActivity.Walking : NpcSpeechActivity.Idle;
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

        private bool HasOverlap(Vector2 position, Vector2 size)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].resident == null) continue;
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
                if (_slots[i].view != null) { _slots[i].view.Clear(); Object.Destroy(_slots[i].view.gameObject); }
            _slots.Clear(); _residents.Clear(); _registry = null;
        }
    }
}
