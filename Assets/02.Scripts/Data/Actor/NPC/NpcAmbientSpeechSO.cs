using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Quest;

namespace UPlayGround.Data.Actor.NPC
{
    /// <summary>주민이 실제 수행 중인 행동으로 일상 대사의 후보를 제한한다.</summary>
    [Flags]
    public enum NpcSpeechActivity { Idle = 1, Walking = 2, Working = 4, Resting = 8, Bumped = 16, Any = 31 }

    /// <summary>게임 시간 서비스가 확인한 시간대에만 해당 대사를 허용한다.</summary>
    [Flags]
    public enum NpcSpeechPeriod { Dawn = 1, Day = 2, Dusk = 4, Night = 8, Any = 15 }

    /// <summary>주민의 외형별 말투와 일상 말풍선 표시 예산을 보관한다.</summary>
    [CreateAssetMenu(fileName = "NpcAmbientSpeech", menuName = "UPlayGround/액터/NPC 일상 대사")]
    public sealed class NpcAmbientSpeechSO : ScriptableObject
    {
        public const string ResourcePath = "Config/NpcAmbientSpeech";

        [Serializable]
        public sealed class Voice
        {
            public string id;
            [TextArea] public string direction;
            public Line[] lines = Array.Empty<Line>();
        }

        /// <summary>조합하지 않고 저작한 한 발화와 발화가 성립하는 조건이다.</summary>
        [Serializable]
        public sealed class Line
        {
            public string id;
            [Tooltip("장면 근거가 부족한 문구는 ID를 남겨 두고 발화 후보에서 보류한다.")]
            public bool isHeld;
            public string topic;
            [TextArea] public string text;
            public NpcSpeechActivity activities = NpcSpeechActivity.Any;
            public NpcSpeechPeriod periods = NpcSpeechPeriod.Any;

            /// <summary>확인되지 않은 시간대에는 시간 한정 대사를 허용하지 않는다.</summary>
            public bool CanSpeak(NpcSpeechActivity activity, NpcSpeechPeriod period)
                => !isHeld && (activities & activity) != 0 && (periods == NpcSpeechPeriod.Any || (periods & period) != 0);

            /// <summary>발화 중 걷거나 멈추는 것은 허용하되 작업·휴식·시간 조건 변경은 유지하지 않는다.</summary>
            public bool CanFinishSpeaking(NpcSpeechActivity startedActivity, NpcSpeechActivity activity, NpcSpeechPeriod period)
            {
                if (CanSpeak(activity, period)) return true;
                bool isLocomotionChange = startedActivity == NpcSpeechActivity.Idle && activity == NpcSpeechActivity.Walking
                    || startedActivity == NpcSpeechActivity.Walking && activity == NpcSpeechActivity.Idle;
                return isLocomotionChange && CanSpeak(startedActivity, period);
            }
        }

        /// <summary>위에서부터 검사하는 상황별 말투 전환. 조건이 맞으면 잡담으로 폴백하지 않는다.</summary>
        [Serializable]
        public sealed class Rule
        {
            public NpcSpeechActivity activities = NpcSpeechActivity.Idle | NpcSpeechActivity.Walking | NpcSpeechActivity.Resting;
            public string requiredFlag;
            public string blockedFlag;
            public string questId;
            public QuestStatus questStatus;
            public int voice = -1;

            /// <summary>서비스가 준비되지 않은 조건은 거짓으로 간주해 진행 사실을 추측하지 않는다.</summary>
            public bool Matches(NpcSpeechActivity activity, Func<string, bool?> flags, Func<string, QuestStatus?> quests)
                => (activities & activity) != 0
                && (string.IsNullOrEmpty(requiredFlag) || flags?.Invoke(requiredFlag) == true)
                && (string.IsNullOrEmpty(blockedFlag) || flags?.Invoke(blockedFlag) == false)
                && (string.IsNullOrEmpty(questId) || quests?.Invoke(questId) == questStatus);
        }

        [Serializable]
        public struct Binding
        {
            public NpcActorSO npc;
            [Min(0)] public int voice;
            [Tooltip("이 사실이 확인되기 전에는 일상 발화를 보류한다.")]
            public string requiredFlag;
            [Tooltip("비우면 NPC 데이터의 이름을 쓴다. 임시 내부 이름이 남은 데이터의 표시명을 보완한다.")]
            public string speakerName;
            public Rule[] rules;
        }

        public Voice[] voices = Array.Empty<Voice>();
        public Binding[] bindings = Array.Empty<Binding>();
        [Min(0)] public int defaultVoice;
        public int workingVoice = -1;
        public int merchantVoice = -1;
        public int bumpedVoice = -1;
        [Min(0.1f)] public float reactionWindow = 1.5f;
        [Min(0.1f)] public float reactionCooldown = 8f;
        [Min(0f)] public float visibilityGrace = 0.4f;
        public GameObject bubblePrefab;
        public Vector2 interval = new(22f, 48f);
        public Vector2 initialDelay = new(4f, 18f);
        [Min(1)] public int maxVisible = 2;
        [Min(0.1f)] public float globalGap = 5f;
        [Min(0.1f)] public float decisionInterval = 0.4f;
        [Min(1f)] public float maxDistance = 16f;
        public Vector2 readingSeconds = new(4f, 8f);
        [Min(1f)] public float charactersPerSecond = 8f;
        [Min(0f)] public float headClearance = 0.25f;
        public Vector3 fallbackOffset = new(0f, 2.1f, 0f);
        [Min(0f)] public float overlapPadding = 20f;
        [Min(1f)] public float lineCooldown = 600f;
        [Min(1f)] public float topicCooldown = 75f;
        [Min(1)] public int globalRecentLines = 48;
        [Min(0.01f)] public float walkingSpeed = 0.2f;
        public LayerMask occlusionMask = ~0;
        private Dictionary<NpcActorSO, Binding> _bindingLookup;

        /// <summary>명시된 NPC 데이터 연결을 우선하고 미지정 주민에는 기본 말투를 쓴다.</summary>
        public int ResolveVoice(NpcActorSO npc)
        {
            EnsureBindings();
            if (npc != null && _bindingLookup.TryGetValue(npc, out var binding)) return binding.voice;
            return defaultVoice;
        }

        /// <summary>개별 진행 조건을 먼저 적용하고 실제 작업·상점 역할 뒤에 일상 말투를 사용한다.</summary>
        public int ResolveVoice(NpcActorSO npc, NpcSpeechActivity activity,
            Func<string, bool?> flags, Func<string, QuestStatus?> quests)
        {
            EnsureBindings();
            if (npc != null && _bindingLookup.TryGetValue(npc, out var binding) && binding.rules != null)
                for (int i = 0; i < binding.rules.Length; i++)
                    if (binding.rules[i] != null && binding.rules[i].Matches(activity, flags, quests))
                        return binding.rules[i].voice;
            if (activity == NpcSpeechActivity.Bumped) return bumpedVoice;
            string required = GetRequiredFlag(npc);
            if (!string.IsNullOrEmpty(required) && flags?.Invoke(required) != true) return -1;
            if (activity == NpcSpeechActivity.Working) return workingVoice;
            if (activity == NpcSpeechActivity.Idle && npc != null && npc.merchantCatalog != null) return merchantVoice;
            return ResolveVoice(npc);
        }

        /// <summary>빈 프로필이나 손상된 인덱스로 말풍선이 열리지 않게 한다.</summary>
        public bool HasVoice(int index) => index >= 0 && index < voices.Length
            && voices[index] != null && voices[index].lines != null && voices[index].lines.Length > 0;

        /// <summary>서사 중인 주민의 평온한 잡담을 차단할 저장 사실을 찾는다.</summary>
        public string GetRequiredFlag(NpcActorSO npc)
        {
            EnsureBindings();
            if (npc != null && _bindingLookup.TryGetValue(npc, out var binding)) return binding.requiredFlag;
            return null;
        }

        /// <summary>저작된 표시명이 있으면 사용하고 기본값은 NPC 데이터 이름을 유지한다.</summary>
        public string GetSpeakerName(NpcActorSO npc)
        {
            EnsureBindings();
            if (npc != null && _bindingLookup.TryGetValue(npc, out var binding)
                && !string.IsNullOrWhiteSpace(binding.speakerName)) return binding.speakerName;
            return npc != null ? npc.actorName : null;
        }

        /// <summary>저작 원본을 동기화한 뒤 런타임 조회 캐시를 갱신한다.</summary>
        public void InvalidateBindings() => _bindingLookup = null;
        private void OnEnable() => InvalidateBindings();
        private void OnValidate() => InvalidateBindings();

        private void EnsureBindings()
        {
            if (_bindingLookup != null) return;
            _bindingLookup = new Dictionary<NpcActorSO, Binding>();
            for (int i = 0; i < bindings.Length; i++)
                if (bindings[i].npc != null) _bindingLookup[bindings[i].npc] = bindings[i];
        }
    }
}
