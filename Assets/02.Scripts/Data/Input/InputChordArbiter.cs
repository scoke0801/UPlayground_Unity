using System;
using System.Collections.Generic;

namespace UPlayGround.InputDefine
{
    /// <summary>
    /// 중재기가 외부로 내보내는 입력 단계.
    /// </summary>
    public enum InputArbiterPhase
    {
        Started,
        Performed,
        Canceled,
    }

    /// <summary>
    /// 중재 결과로 확정된 입력 1건.
    /// <see cref="PhysicalTime"/>은 grace 지연과 무관하게 항상 원래 물리 입력 시각이다.
    /// </summary>
    public readonly struct InputArbiterEvent<TContext>
    {
        public readonly string MapName;
        public readonly string ActionName;
        public readonly InputArbiterPhase Phase;
        public readonly float PhysicalTime;
        public readonly TContext Context;

        /// <summary>조합 성립으로 인해 중재기가 생성한 보정 이벤트인지 여부.</summary>
        public readonly bool IsSynthetic;

        public InputArbiterEvent(
            string mapName,
            string actionName,
            InputArbiterPhase phase,
            float physicalTime,
            TContext context,
            bool isSynthetic = false)
        {
            MapName = mapName;
            ActionName = actionName;
            Phase = phase;
            PhysicalTime = physicalTime;
            Context = context;
            IsSynthetic = isSynthetic;
        }

        public override string ToString() =>
            $"{MapName}/{ActionName}:{Phase}@{PhysicalTime:F3}{(IsSynthetic ? "(synthetic)" : string.Empty)}";
    }

    /// <summary>
    /// 조합키(Modifier + Trigger)와 단일키가 같은 물리 컨트롤을 공유할 때의 런타임 중재기.
    ///
    /// 스펙 §9의 판정 규칙을 그대로 구현한다.
    /// - Modifier가 이미 눌린 상태에서 Trigger가 들어오면 구성 단일 액션을 즉시 억제한다.
    /// - 조합 후보 컨트롤의 단일 액션은 <see cref="GraceSeconds"/> 동안 provisional로 보류한다.
    ///   grace 안에 조합이 성립하면 보류분을 폐기하고, 성립하지 않으면 도착 순서 그대로 확정한다.
    /// - 조합이 성립하면 이미 확정된 Modifier 단일 Hold 액션에는 보정 Canceled를 1회 내보낸다.
    ///
    /// Unity Input System 타입을 참조하지 않는 순수 로직이라 EditMode에서 단독 검증할 수 있다.
    /// 컨텍스트 페이로드는 제네릭으로 그대로 실어 나른다(런타임은 InputAction.CallbackContext).
    /// </summary>
    public sealed class InputChordArbiter<TContext>
    {
        public const float DefaultGraceSeconds = 0.12f;

        private readonly struct ActionKey : IEquatable<ActionKey>
        {
            public readonly string Map;
            public readonly string Action;

            public ActionKey(string map, string action)
            {
                Map = map;
                Action = action;
            }

            public bool Equals(ActionKey other) =>
                string.Equals(Map, other.Map, StringComparison.Ordinal)
                && string.Equals(Action, other.Action, StringComparison.Ordinal);

            public override bool Equals(object obj) => obj is ActionKey other && Equals(other);

            public override int GetHashCode() => HashCode.Combine(Map, Action);

            public override string ToString() => $"{Map}/{Action}";
        }

        private readonly struct ChordEntry
        {
            public readonly ActionKey Owner;
            public readonly string ModifierPath;
            public readonly string TriggerPath;

            public ChordEntry(ActionKey owner, string modifierPath, string triggerPath)
            {
                Owner = owner;
                ModifierPath = modifierPath;
                TriggerPath = triggerPath;
            }
        }

        private sealed class PendingEntry
        {
            public ActionKey Key;
            public string ControlPath;
            public float Deadline;
            public readonly List<InputArbiterEvent<TContext>> Queue = new(4);
        }

        private readonly List<ChordEntry> _chords = new();
        private readonly Dictionary<ActionKey, PendingEntry> _pending = new();
        private readonly Dictionary<ActionKey, string> _active = new();
        private readonly HashSet<ActionKey> _suppressed = new();
        private readonly List<ActionKey> _scratchKeys = new();

        /// <summary>두 번째 키를 기다리는 최대 시간(초).</summary>
        public float GraceSeconds { get; set; } = DefaultGraceSeconds;

        /// <summary>
        /// 상대 컨트롤 경로(예: "leftshoulder")가 현재 물리적으로 눌려 있는지 조회한다.
        /// 미지정이면 "눌리지 않음"으로 취급한다.
        /// </summary>
        public Func<string, bool> IsControlPressed { get; set; }

        /// <summary>등록된 조합 후보 수. 진단·테스트용.</summary>
        public int ChordCount => _chords.Count;

        /// <summary>확정 대기 중인 액션 수. 진단·테스트용.</summary>
        public int PendingCount => _pending.Count;

        #region 카탈로그

        public void ClearCatalog()
        {
            _chords.Clear();
        }

        /// <summary>
        /// 조합 후보를 등록한다. 경로는 내부에서 상대 경로로 정규화한다.
        /// </summary>
        public void RegisterChord(
            string mapName,
            string actionName,
            string modifierPath,
            string triggerPath)
        {
            string modifier = NormalizePath(modifierPath);
            string trigger = NormalizePath(triggerPath);
            if (string.IsNullOrEmpty(modifier) || string.IsNullOrEmpty(trigger))
                return;

            var owner = new ActionKey(mapName, actionName);
            foreach (ChordEntry existing in _chords)
            {
                if (existing.Owner.Equals(owner)
                    && existing.ModifierPath == modifier
                    && existing.TriggerPath == trigger)
                {
                    return;
                }
            }

            _chords.Add(new ChordEntry(owner, modifier, trigger));
        }

        public bool IsChordAction(string mapName, string actionName)
        {
            var key = new ActionKey(mapName, actionName);
            foreach (ChordEntry chord in _chords)
            {
                if (chord.Owner.Equals(key))
                    return true;
            }

            return false;
        }

        #endregion

        #region 입력 중재

        /// <summary>
        /// 물리 입력 1건을 중재기에 넣는다. 즉시 확정된 이벤트만 <paramref name="dispatch"/>에 추가된다.
        /// 보류된 입력은 <see cref="Tick"/>에서 확정되거나 조합 성립 시 폐기된다.
        /// </summary>
        public void Submit(
            string mapName,
            string actionName,
            InputArbiterPhase phase,
            string controlPath,
            float time,
            TContext context,
            List<InputArbiterEvent<TContext>> dispatch)
        {
            var key = new ActionKey(mapName, actionName);
            string path = NormalizePath(controlPath);
            var evt = new InputArbiterEvent<TContext>(mapName, actionName, phase, time, context);

            // 조합 액션 본인은 지연시키지 않는다. 성립 시점에 구성 단일 액션을 정리한다.
            if (IsChordAction(mapName, actionName))
            {
                if (phase != InputArbiterPhase.Canceled)
                    ResolveChordWin(key, time, context, dispatch);

                DispatchEvent(evt, path, dispatch);
                return;
            }

            // 조합에 밀려 억제된 액션은 실제 release가 올 때까지 계속 무시한다.
            if (_suppressed.Contains(key))
            {
                if (phase == InputArbiterPhase.Canceled)
                    _suppressed.Remove(key);
                return;
            }

            // 이미 보류 중이면 같은 큐에 이어 붙여 도착 순서를 보존한다.
            if (_pending.TryGetValue(key, out PendingEntry pending))
            {
                pending.Queue.Add(evt);
                return;
            }

            if (phase == InputArbiterPhase.Started)
            {
                // Modifier가 이미 눌려 있으면 조합이 확실하므로 대기 없이 억제한다.
                if (HasActiveChordFor(key, path))
                {
                    _suppressed.Add(key);
                    return;
                }

                // 조합 후보 컨트롤이면 grace 동안 provisional로 보류한다.
                if (GraceSeconds > 0f && IsChordParticipant(key, path))
                {
                    var entry = new PendingEntry
                    {
                        Key = key,
                        ControlPath = path,
                        Deadline = time + GraceSeconds,
                    };
                    entry.Queue.Add(evt);
                    _pending[key] = entry;
                    return;
                }
            }

            DispatchEvent(evt, path, dispatch);
        }

        /// <summary>
        /// grace가 만료된 보류 입력을 확정한다. 매 프레임 1회 호출한다.
        /// </summary>
        public void Tick(float time, List<InputArbiterEvent<TContext>> dispatch)
        {
            if (_pending.Count == 0)
                return;

            _scratchKeys.Clear();
            foreach (KeyValuePair<ActionKey, PendingEntry> pair in _pending)
            {
                if (pair.Value.Deadline <= time)
                    _scratchKeys.Add(pair.Key);
            }

            for (int i = 0; i < _scratchKeys.Count; i++)
            {
                if (!_pending.TryGetValue(_scratchKeys[i], out PendingEntry entry))
                    continue;

                _pending.Remove(_scratchKeys[i]);
                for (int e = 0; e < entry.Queue.Count; e++)
                    DispatchEvent(entry.Queue[e], entry.ControlPath, dispatch);
            }

            _scratchKeys.Clear();
        }

        /// <summary>
        /// 입력 컨텍스트 변경(UI 진입 등) 시 호출한다.
        /// 대기 중 단일키 후보와 provisional hold를 폐기하고 조합 상태를 초기화한다.
        /// </summary>
        public void Reset()
        {
            _pending.Clear();
            _active.Clear();
            _suppressed.Clear();
            _scratchKeys.Clear();
        }

        #endregion

        #region 내부

        private void DispatchEvent(
            InputArbiterEvent<TContext> evt,
            string controlPath,
            List<InputArbiterEvent<TContext>> dispatch)
        {
            var key = new ActionKey(evt.MapName, evt.ActionName);
            switch (evt.Phase)
            {
                case InputArbiterPhase.Started:
                    _active[key] = controlPath;
                    break;
                case InputArbiterPhase.Canceled:
                    _active.Remove(key);
                    break;
            }

            dispatch?.Add(evt);
        }

        /// <summary>
        /// 조합이 성립했을 때 구성 단일 액션을 정리한다.
        /// 보류 중이면 조용히 폐기하고, 이미 확정된 Hold 액션이면 보정 Canceled를 1회 내보낸다.
        /// </summary>
        private void ResolveChordWin(
            ActionKey chordKey,
            float time,
            TContext context,
            List<InputArbiterEvent<TContext>> dispatch)
        {
            _scratchKeys.Clear();

            foreach (KeyValuePair<ActionKey, PendingEntry> pair in _pending)
            {
                if (pair.Key.Equals(chordKey))
                    continue;
                if (!IsChordParticipantPath(chordKey, pair.Value.ControlPath))
                    continue;

                _scratchKeys.Add(pair.Key);
            }

            for (int i = 0; i < _scratchKeys.Count; i++)
            {
                _pending.Remove(_scratchKeys[i]);
                _suppressed.Add(_scratchKeys[i]);
            }

            _scratchKeys.Clear();

            foreach (KeyValuePair<ActionKey, string> pair in _active)
            {
                if (pair.Key.Equals(chordKey))
                    continue;
                if (!IsChordParticipantPath(chordKey, pair.Value))
                    continue;

                _scratchKeys.Add(pair.Key);
            }

            for (int i = 0; i < _scratchKeys.Count; i++)
            {
                ActionKey key = _scratchKeys[i];
                _active.Remove(key);
                _suppressed.Add(key);
                dispatch?.Add(new InputArbiterEvent<TContext>(
                    key.Map,
                    key.Action,
                    InputArbiterPhase.Canceled,
                    time,
                    context,
                    isSynthetic: true));
            }

            _scratchKeys.Clear();
        }

        /// <summary>
        /// 같은 맵의 다른 액션이 소유한 조합 중, 이 컨트롤을 Trigger로 쓰면서
        /// Modifier가 실제로 눌려 있는 것이 있는지 검사한다.
        /// </summary>
        private bool HasActiveChordFor(ActionKey key, string controlPath)
        {
            if (string.IsNullOrEmpty(controlPath))
                return false;

            foreach (ChordEntry chord in _chords)
            {
                if (chord.Owner.Equals(key))
                    continue;
                if (!string.Equals(chord.Owner.Map, key.Map, StringComparison.Ordinal))
                    continue;
                if (chord.TriggerPath != controlPath)
                    continue;

                if (IsControlPressed != null && IsControlPressed(chord.ModifierPath))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 같은 맵의 다른 액션이 소유한 조합에서 이 컨트롤이 Modifier 또는 Trigger로 쓰이는지 검사한다.
        /// </summary>
        private bool IsChordParticipant(ActionKey key, string controlPath)
        {
            if (string.IsNullOrEmpty(controlPath))
                return false;

            foreach (ChordEntry chord in _chords)
            {
                if (chord.Owner.Equals(key))
                    continue;
                if (!string.Equals(chord.Owner.Map, key.Map, StringComparison.Ordinal))
                    continue;

                if (chord.ModifierPath == controlPath || chord.TriggerPath == controlPath)
                    return true;
            }

            return false;
        }

        /// <summary>지정한 조합 액션의 구성 컨트롤인지 검사한다.</summary>
        private bool IsChordParticipantPath(ActionKey chordKey, string controlPath)
        {
            if (string.IsNullOrEmpty(controlPath))
                return false;

            foreach (ChordEntry chord in _chords)
            {
                if (!chord.Owner.Equals(chordKey))
                    continue;

                if (chord.ModifierPath == controlPath || chord.TriggerPath == controlPath)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// "&lt;Gamepad&gt;/leftShoulder" 같은 바인딩 경로와 "/Gamepad/leftShoulder" 같은
        /// 컨트롤 경로를 동일한 상대 경로로 정규화한다.
        /// </summary>
        public static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            string trimmed = path.Trim();
            int layoutEnd = trimmed.IndexOf(">/", StringComparison.Ordinal);
            if (layoutEnd >= 0)
                return trimmed.Substring(layoutEnd + 2).Trim('/').ToLowerInvariant();

            string normalized = trimmed.Trim('/').ToLowerInvariant();
            int slash = normalized.IndexOf('/');
            return slash >= 0 ? normalized.Substring(slash + 1) : normalized;
        }

        #endregion
    }
}