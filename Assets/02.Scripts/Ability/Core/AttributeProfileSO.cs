using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Ability.Core
{
    [Serializable]
    public sealed class AttributeProfileEntry
    {
        [SerializeField] private string _attributeId;
        [SerializeField] private float _baseValue;

        public AttributeId AttributeId => new(_attributeId);
        public float BaseValue => _baseValue;

        public AttributeProfileEntry(AttributeId attributeId, float baseValue)
        {
            _attributeId = attributeId.Value;
            _baseValue = baseValue;
        }
    }

    /// <summary>
    /// 액터·캐릭터별 Attribute 기본값 모음.
    /// 저장과 참조에는 프로젝트 enum이 아닌 안정 문자열 Attribute ID만 사용한다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "AttributeProfile_",
        menuName = "UPlayGround/Ability/Attribute Profile")]
    public sealed class AttributeProfileSO : ScriptableObject
    {
        [SerializeField] private string _profileId;
        [SerializeField] private List<AttributeProfileEntry> _entries = new();

        public string ProfileId => _profileId?.Trim() ?? string.Empty;
        public IReadOnlyList<AttributeProfileEntry> Entries => _entries;

        public bool TryGetBaseValue(AttributeId attributeId, out float value)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                AttributeProfileEntry entry = _entries[i];
                if (entry != null && entry.AttributeId == attributeId)
                {
                    value = entry.BaseValue;
                    return true;
                }
            }

            value = 0f;
            return false;
        }

        public bool TryCopyBaseValues(
            IDictionary<AttributeId, float> destination,
            out string error)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            destination.Clear();
            for (int i = 0; i < _entries.Count; i++)
            {
                AttributeProfileEntry entry = _entries[i];
                if (entry == null || !entry.AttributeId.IsValid)
                {
                    error = $"{name}: {i}번 Attribute ID가 비어 있습니다.";
                    destination.Clear();
                    return false;
                }
                if (destination.ContainsKey(entry.AttributeId))
                {
                    error = $"{name}: Attribute ID가 중복됩니다: {entry.AttributeId}";
                    destination.Clear();
                    return false;
                }
                if (float.IsNaN(entry.BaseValue) || float.IsInfinity(entry.BaseValue))
                {
                    error = $"{name}: {entry.AttributeId} 값이 유한수가 아닙니다.";
                    destination.Clear();
                    return false;
                }
                destination.Add(entry.AttributeId, entry.BaseValue);
            }

            error = string.Empty;
            return true;
        }

#if UNITY_EDITOR
        public void EditorReplace(
            string profileId,
            IEnumerable<AttributeProfileEntry> entries)
        {
            _profileId = profileId?.Trim() ?? string.Empty;
            _entries.Clear();
            if (entries != null)
                _entries.AddRange(entries);
            _entries.Sort((left, right) =>
                left.AttributeId.CompareTo(right.AttributeId));
        }

        public bool EditorSetBaseValue(AttributeId attributeId, float value)
        {
            if (!attributeId.IsValid
                || float.IsNaN(value)
                || float.IsInfinity(value))
                return false;

            for (int i = 0; i < _entries.Count; i++)
            {
                AttributeProfileEntry entry = _entries[i];
                if (entry == null || entry.AttributeId != attributeId)
                    continue;
                _entries[i] = new AttributeProfileEntry(attributeId, value);
                return true;
            }

            _entries.Add(new AttributeProfileEntry(attributeId, value));
            _entries.Sort((left, right) =>
                left.AttributeId.CompareTo(right.AttributeId));
            return true;
        }
#endif
    }
}
