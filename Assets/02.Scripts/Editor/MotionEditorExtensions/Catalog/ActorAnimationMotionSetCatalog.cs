using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.Event;
using UPlayGround.Gameplay.Tag;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Animation.Editor
{
    /// <summary>
    /// 프로젝트 GameplayTag 기반 Actor 모션 데이터를 범용 슬롯 카탈로그로 노출한다.
    /// </summary>
    public sealed class ActorAnimationMotionSetCatalog : IMotionSetCatalog
    {
        private readonly ActorAnimationMotionSet _source;
        private readonly List<MotionSetSlot> _slots = new();
        private readonly List<MotionSetSlot> _assignableSlots = new();
        private readonly Dictionary<string, GameplayTag> _tags =
            new(StringComparer.Ordinal);
        private readonly Dictionary<string, MotionSetAsset> _readOnlyAssets =
            new(StringComparer.Ordinal);

        public ActorAnimationMotionSetCatalog(ActorAnimationMotionSet source)
        {
            _source = source;
            Refresh();
        }

        public UnityEngine.Object SourceAsset => _source;
        public IReadOnlyList<MotionSetSlot> Slots => _slots;
        public IReadOnlyList<MotionSetSlot> AssignableSlots => _assignableSlots;

        public MotionSetAsset Resolve(string slotId)
        {
            if (_readOnlyAssets.TryGetValue(slotId ?? string.Empty, out MotionSetAsset readOnly))
                return readOnly;
            return _source != null && TryGetTag(slotId, out GameplayTag tag)
                ? _source.GetMotionSetAsset(tag)
                : null;
        }

        public bool Assign(string slotId, MotionSetAsset asset)
        {
            if (_source == null || !TryGetTag(slotId, out GameplayTag tag))
                return false;

            SerializedObject serializedSource = new(_source);
            SerializedProperty slotsProperty =
                serializedSource.FindProperty("motionSlots")
                    ?.FindPropertyRelative("_serializedList");
            if (slotsProperty == null)
                return false;

            // ApplyModifiedProperties()가 Undo를 등록하므로 RecordObject는 중복이다.
            int index = FindSlotIndex(slotsProperty, tag.TagName);
            if (index < 0)
            {
                slotsProperty.InsertArrayElementAtIndex(slotsProperty.arraySize);
                index = slotsProperty.arraySize - 1;
            }

            SerializedProperty element = slotsProperty.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("Key")
                .FindPropertyRelative("_tagName").stringValue = tag.TagName;
            element.FindPropertyRelative("Value").objectReferenceValue = asset;
            serializedSource.ApplyModifiedProperties();
            EditorUtility.SetDirty(_source);
            AssetDatabase.SaveAssetIfDirty(_source);
            Refresh();
            return true;
        }

        public MotionSetAsset CreateAndAssign(string slotId, string directory)
        {
            if (!TryGetTag(slotId, out GameplayTag tag))
                return null;

            string assetDirectory = string.IsNullOrWhiteSpace(directory)
                ? Path.GetDirectoryName(AssetDatabase.GetAssetPath(_source))
                : directory;
            if (string.IsNullOrWhiteSpace(assetDirectory))
                assetDirectory = "Assets";
            assetDirectory = assetDirectory.Replace('\\', '/');

            string fileName = $"{_source.name}_{tag.TagName}.asset";
            string path = AssetDatabase.GenerateUniqueAssetPath(
                $"{assetDirectory}/{fileName}");
            MotionSetAsset asset = ScriptableObject.CreateInstance<MotionSetAsset>();
            asset.motionSet = new MotionSet
            {
                motionSetName = Path.GetFileNameWithoutExtension(path),
                motions = new List<Motion>(),
            };
            AssetDatabase.CreateAsset(asset, path);
            if (!Assign(slotId, asset))
            {
                AssetDatabase.DeleteAsset(path);
                return null;
            }

            return asset;
        }

        public void Refresh()
        {
            _slots.Clear();
            _assignableSlots.Clear();
            _tags.Clear();
            _readOnlyAssets.Clear();

            foreach (GameplayTag tag in EnumerateKnownTags())
                _tags[tag.TagName] = tag;

            HashSet<string> assigned = new(StringComparer.Ordinal);
            HashSet<string> inherited = new(StringComparer.Ordinal);
            HashSet<ActorAnimationMotionSet> visited = new();
            for (ActorAnimationMotionSet current = _source;
                 current != null && visited.Add(current);
                 current = current.fallbackMotionSet)
            {
                if (current.motionSlots != null)
                {
                    foreach (KeyValuePair<GameplayTag, MotionSetAsset> pair in current.motionSlots)
                    {
                        if (!pair.Key.IsValid() || !inherited.Add(pair.Key.TagName))
                            continue;

                        if (current == _source)
                            assigned.Add(pair.Key.TagName);
                        _tags[pair.Key.TagName] = pair.Key;
                        _slots.Add(CreateSlot(pair.Key));
                    }
                }

                AddAttackSlots(current);
            }

            foreach (GameplayTag tag in _tags.Values.OrderBy(tag => tag.TagName))
            {
                if (!assigned.Contains(tag.TagName))
                    _assignableSlots.Add(CreateSlot(tag));
            }

            _slots.Sort(CompareSlots);
        }

        private void AddAttackSlots(ActorAnimationMotionSet source)
        {
            AbilitySetSO abilitySet = source?.attackAbilitySet;
            if (abilitySet == null)
                return;

            foreach (GameplayAbilitySO ability in abilitySet.EnumerateAll())
            {
                if (ability?.variants == null)
                    continue;

                for (int variantIndex = 0; variantIndex < ability.variants.Count; variantIndex++)
                {
                    AbilityVariantDefinition variant = ability.variants[variantIndex];
                    if (variant?.executionPayload
                        is not UPlayGroundMotionAbilityPayloadSO payload)
                        continue;

                    MotionReferenceSO motionRef =
                        payload.attackInfo?.baseInfo?.motionRef;
                    MotionSetAsset asset = motionRef?.Resolve(source.attackWeaponType);
                    if (asset == null)
                        continue;

                    // InstanceID는 세션 간 불안정하므로 슬롯 식별자로 쓰지 않는다.
                    string abilityKey = string.IsNullOrWhiteSpace(ability.abilityId)
                        ? ability.name
                        : ability.abilityId;
                    string slotId =
                        $"Attack:{abilityKey}:{variantIndex}:Resolved:{source.attackWeaponType}";
                    if (_readOnlyAssets.ContainsKey(slotId))
                        continue;

                    string abilityName = abilityKey;
                    string variantName = string.IsNullOrWhiteSpace(variant.variantId)
                        ? $"Variant {variantIndex}"
                        : variant.variantId;
                    string displayName = ability.variants.Count > 1
                        ? $"{abilityName} · {variantName}"
                        : abilityName;

                    _readOnlyAssets.Add(slotId, asset);
                    _slots.Add(new MotionSetSlot(slotId, displayName, "공격"));
                }
            }
        }

        private bool TryGetTag(string slotId, out GameplayTag tag)
        {
            string normalized = NormalizeSlotId(slotId);
            if (_tags.TryGetValue(normalized, out tag))
                return true;

            Refresh();
            return _tags.TryGetValue(normalized, out tag);
        }

        private static string NormalizeSlotId(string slotId)
        {
            const string prefix = "Slot:";
            return slotId != null &&
                   slotId.StartsWith(prefix, StringComparison.Ordinal)
                ? slotId.Substring(prefix.Length)
                : slotId;
        }

        private static GameplayTag[] _knownTags;

        /// <summary>
        /// MotionTags 리플렉션 결과는 도메인 리로드 전까지 불변이므로 1회만 수집한다.
        /// Refresh()가 반복 호출되는 경로에 있어 매번 GetFields를 돌리면 안 된다.
        /// </summary>
        private static IEnumerable<GameplayTag> EnumerateKnownTags()
        {
            return _knownTags ??= typeof(MotionTags)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.FieldType == typeof(GameplayTag))
                .Select(field => (GameplayTag)field.GetValue(null))
                .Where(tag => tag.IsValid())
                .OrderBy(tag => tag.TagName, StringComparer.Ordinal)
                .ToArray();
        }

        private static MotionSetSlot CreateSlot(GameplayTag tag)
        {
            string[] parts = tag.TagName.Split('.');
            string group = parts.Length > 1 ? parts[1] : parts[0];
            return new MotionSetSlot(tag.TagName, tag.ToString(), group);
        }

        private static int CompareSlots(MotionSetSlot left, MotionSetSlot right)
        {
            int group = string.CompareOrdinal(left.GroupLabel, right.GroupLabel);
            return group != 0
                ? group
                : string.CompareOrdinal(left.DisplayName, right.DisplayName);
        }

        private static int FindSlotIndex(
            SerializedProperty slotsProperty,
            string tagName)
        {
            for (int i = 0; i < slotsProperty.arraySize; i++)
            {
                string existing = slotsProperty.GetArrayElementAtIndex(i)
                    .FindPropertyRelative("Key")
                    .FindPropertyRelative("_tagName")
                    .stringValue;
                if (string.Equals(existing, tagName, StringComparison.Ordinal))
                    return i;
            }

            return -1;
        }
    }
}
