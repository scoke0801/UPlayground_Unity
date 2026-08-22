using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.Dialogue
{
    /// <summary>대화 제스처의 정서 분류. 라인 감정에 맞는 제스처만 뽑을 때 쓴다.</summary>
    public enum DialogueMotionCategory
    {
        Neutral,
        Agree,
        Deny,
        Think,
        Explain,
        Excited,
        Sad,
        Angry,
    }

    /// <summary>카탈로그 항목 하나 — 저작용 ID와 실제 모션 슬롯 태그의 짝.</summary>
    [Serializable]
    public sealed class DialogueMotionEntry
    {
        [Tooltip("대사 노드에서 이 제스처를 지정할 때 쓰는 ID. 태그와 달리 사람이 읽고 쓰는 값이다.")]
        public string motionId = string.Empty;

        [Tooltip("액터 MotionSet이 해석할 모션 슬롯 태그.")]
        public GameplayTag motionTag;

        [Tooltip("이 제스처의 정서 분류. 카테고리 랜덤 추출의 기준이 된다.")]
        public DialogueMotionCategory category = DialogueMotionCategory.Neutral;

        [Tooltip("랜덤 추출 가중치. 0이면 뽑히지 않는다.")]
        [Min(0f)] public float randomWeight = 1f;

        [Tooltip("끄면 랜덤 풀에서 제외되고 ID 지정으로만 재생된다. 시퀀스 조각처럼 단독 재생이 어색한 모션에 쓴다.")]
        public bool includeInRandomPool = true;

        [Tooltip("원본 클립 등 저작 메모. 런타임 동작에 영향을 주지 않는다.")]
        public string note = string.Empty;

        public bool IsValid()
            => !string.IsNullOrWhiteSpace(motionId) && motionTag.IsValid();
    }

    /// <summary>
    /// 대화 연출에 쓰는 제스처 모션의 단일 소스.
    /// 대사 노드는 ID로 제스처를 지정하고, 지정이 없으면 카테고리 랜덤 풀에서 뽑는다.
    /// 태그를 코드 상수로 두지 않는 이유는 제스처가 애니메이션 에셋을 따라 늘어나는 콘텐츠 데이터이기 때문이다 —
    /// 태그 등록은 GameplayTagRegistry가, 실제 모션 해석은 액터 MotionSet이 담당한다.
    /// </summary>
    [CreateAssetMenu(
        menuName = "UPlayGround/대화/Motion Catalog",
        fileName = "DialogueMotionCatalog")]
    public sealed class DialogueMotionCatalogSO : ScriptableObject
    {
        [Tooltip("지정도 랜덤도 성립하지 않을 때 재생할 기본 대화 모션 슬롯.")]
        [SerializeField] private GameplayTag _defaultMotionTag;

        [Tooltip("대화 제스처 목록.")]
        [SerializeField] private List<DialogueMotionEntry> _entries = new();

        private Dictionary<string, DialogueMotionEntry> _byId;
        private readonly List<DialogueMotionEntry> _pickBuffer = new();

        /// <summary>지정·랜덤이 모두 실패했을 때 쓰는 기본 슬롯.</summary>
        public GameplayTag DefaultMotionTag => _defaultMotionTag;

        public IReadOnlyList<DialogueMotionEntry> Entries => _entries;

        /// <summary>저작 ID로 제스처 슬롯을 찾는다. 없으면 false.</summary>
        public bool TryGetMotionTag(string motionId, out GameplayTag motionTag)
        {
            motionTag = default;
            if (string.IsNullOrWhiteSpace(motionId))
                return false;

            EnsureLookup();
            if (!_byId.TryGetValue(motionId.Trim(), out DialogueMotionEntry entry))
                return false;

            motionTag = entry.motionTag;
            return entry.motionTag.IsValid();
        }

        /// <summary>
        /// 카테고리 랜덤 풀에서 하나를 뽑는다.
        /// <paramref name="exclude"/>와 같은 슬롯은 후보가 둘 이상일 때만 제외해 같은 제스처가 연속되지 않게 한다.
        /// 해당 카테고리에 후보가 없으면 Neutral로, 그것도 없으면 기본 슬롯으로 내려간다.
        /// </summary>
        public GameplayTag PickRandom(DialogueMotionCategory category, GameplayTag exclude)
        {
            if (TryPickFrom(category, exclude, out GameplayTag picked))
                return picked;

            if (category != DialogueMotionCategory.Neutral
                && TryPickFrom(DialogueMotionCategory.Neutral, exclude, out picked))
                return picked;

            return _defaultMotionTag;
        }

        private bool TryPickFrom(
            DialogueMotionCategory category,
            GameplayTag exclude,
            out GameplayTag picked)
        {
            picked = default;

            _pickBuffer.Clear();
            float totalWeight = 0f;
            for (int i = 0; i < _entries.Count; i++)
            {
                DialogueMotionEntry entry = _entries[i];
                if (entry == null
                    || !entry.includeInRandomPool
                    || entry.randomWeight <= 0f
                    || entry.category != category
                    || !entry.motionTag.IsValid())
                    continue;

                _pickBuffer.Add(entry);
                totalWeight += entry.randomWeight;
            }

            if (_pickBuffer.Count == 0)
                return false;

            // 후보가 하나뿐이면 직전과 같더라도 그대로 쓴다 — 제외를 우선하면 재생할 모션이 사라진다.
            if (_pickBuffer.Count > 1 && exclude.IsValid())
            {
                for (int i = _pickBuffer.Count - 1; i >= 0; i--)
                {
                    if (_pickBuffer[i].motionTag != exclude)
                        continue;

                    totalWeight -= _pickBuffer[i].randomWeight;
                    _pickBuffer.RemoveAt(i);
                }
            }

            if (_pickBuffer.Count == 0 || totalWeight <= 0f)
                return false;

            float roll = UnityEngine.Random.value * totalWeight;
            for (int i = 0; i < _pickBuffer.Count; i++)
            {
                roll -= _pickBuffer[i].randomWeight;
                if (roll > 0f && i < _pickBuffer.Count - 1)
                    continue;

                picked = _pickBuffer[i].motionTag;
                return true;
            }

            return false;
        }

        private void EnsureLookup()
        {
            if (_byId != null)
                return;

            _byId = new Dictionary<string, DialogueMotionEntry>(
                _entries.Count,
                StringComparer.Ordinal);

            for (int i = 0; i < _entries.Count; i++)
            {
                DialogueMotionEntry entry = _entries[i];
                if (entry?.IsValid() != true)
                    continue;

                _byId[entry.motionId.Trim()] = entry;
            }
        }

#if UNITY_EDITOR
        private void OnValidate() => _byId = null;
#endif
    }
}
