#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Combat;
using UPlayGround.Components;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.EditorTools;

namespace UPlayGround.Tool.Editor.Combat
{
    /// <summary>
    /// CombatHitbox.groupId와 공격 데이터/모션이벤트의 hitboxGroupId를 함께 변경(rename)하는 에디터 유틸.
    /// 런타임 그룹 결정 우선순위는 BeginCollisionEvent → HitPhaseData → Default 이므로
    /// Ability Payload(HitPhaseData)와 MotionSet 에셋(BeginCollisionEvent)을 모두 같은 필드명(hitboxGroupId)으로 다룬다.
    /// 타입 하드코딩 대신 SerializedProperty 전체 순회로 중첩 위치(콤보/차지/잔상/라우트 등)를 빠짐없이 잡는다.
    /// </summary>
    public static class CombatHitboxGroupSyncUtility
    {
        public const string HitboxGroupFieldName = "hitboxGroupId";
        public const string HitboxComponentGroupFieldName = "_groupId";
        public const string AdditionalHitboxGroupsFieldName = "additionalHitboxGroupIds";

        /// <summary>한 그룹 ID가 HitBox / 공격데이터 / 모션이벤트 각각에서 몇 번 쓰이는지 집계.</summary>
        public sealed class GroupUsage
        {
            public string GroupId;
            public int HitboxCount;
            public int DataPhaseCount;   // Ability Payload 내부 hitboxGroupId 출현 수
            public int EventCount;       // MotionSet 등 비-Payload 에셋의 hitboxGroupId 출현 수
            public string NewGroupId;    // UI 편집용 매핑 대상

            public int TotalCount => HitboxCount + DataPhaseCount + EventCount;
        }

        /// <summary>
        /// 무기(또는 그 하위/캐릭터 루트)에서 캐릭터 컨텍스트를 찾는다.
        ///  • CharacterModelData (attackData 보유)
        ///  • PlayerActorAnimator.PlayerMotionSet (무기 타입별 MotionSet 보유)
        /// 무기를 선택하면 부모(Model)에서, 캐릭터 루트를 선택하면 자식(Model)에서 찾도록 양쪽을 본다.
        /// </summary>
        public static bool TryResolveContext(
            GameObject node,
            out CharacterModelData model,
            out PlayerActorAnimationMotionSet container)
        {
            model = null;
            container = null;
            if (node == null)
                return false;

            model = node.GetComponentInParent<CharacterModelData>(true)
                    ?? node.GetComponentInChildren<CharacterModelData>(true);
            PlayerActorAnimator animator = node.GetComponentInParent<PlayerActorAnimator>(true)
                                           ?? node.GetComponentInChildren<PlayerActorAnimator>(true);
            container = animator != null ? animator.PlayerMotionSet : null;
            return model != null || container != null;
        }

        public static int CollectAttackData(CharacterModelData model, List<UnityEngine.Object> into)
        {
            if (model == null || model.abilitySet == null)
                return 0;

            int added = 0;
            foreach (AbilityAttackEditorUtility.Entry entry in AbilityAttackEditorUtility.Collect(model.abilitySet))
                if (AddUnique(into, entry.Payload))
                    added++;
            return added;
        }

        /// <summary>
        /// 지정한 무기 타입의 MotionSet만 수집한다(+ fallback 체인). 컨테이너의 모든 무기 타입을
        /// 무차별 수집하지 않아 과수집을 막는다. hitboxGroupId(충돌 이벤트)가 있는 에셋만 담는다.
        /// </summary>
        public static int CollectMotionSetsForWeapon(
            PlayerActorAnimationMotionSet container,
            WeaponType weaponType,
            List<UnityEngine.Object> into)
        {
            if (container == null)
                return 0;
            var visited = new HashSet<ActorAnimationMotionSet>();
            return CollectMotionSetAssets(container.GetActorAnimationMotionSet(weaponType), into, visited);
        }

        /// <summary>컨테이너가 보유한 무기 타입 목록(드롭다운용).</summary>
        public static List<WeaponType> GetWeaponTypes(PlayerActorAnimationMotionSet container)
        {
            var list = new List<WeaponType>();
            if (container?.motionSets != null)
                foreach (WeaponType type in container.motionSets.Keys)
                    list.Add(type);
            return list;
        }

        private static int CollectMotionSetAssets(
            ActorAnimationMotionSet set,
            List<UnityEngine.Object> into,
            HashSet<ActorAnimationMotionSet> visited)
        {
            if (set == null || !visited.Add(set)) // fallback 체인 순환 방지
                return 0;

            int added = 0;
            if (set.motionSlots != null)
                foreach (MotionSetAsset asset in set.motionSlots.Values)
                    // hitboxGroupId(충돌 이벤트)가 실제로 있는 MotionSet만 수집한다.
                    // Idle/Walk/Run 같은 비전투 모션이 목록을 도배하지 않도록.
                    if (asset != null && HasHitboxGroup(asset) && AddUnique(into, asset))
                        added++;

            added += CollectMotionSetAssets(set.fallbackMotionSet, into, visited);
            return added;
        }

        /// <summary>에셋 내부에 hitboxGroupId 필드가 하나라도 있으면 true(충돌 이벤트 보유 여부).</summary>
        public static bool HasHitboxGroup(UnityEngine.Object asset)
        {
            if (asset == null)
                return false;
            var so = new SerializedObject(asset);
            SerializedProperty it = so.GetIterator();
            bool enter = true;
            while (it.Next(enter))
            {
                enter = true;
                if (it.propertyType == SerializedPropertyType.String && it.name == HitboxGroupFieldName)
                    return true;
                if (it.isArray && it.name == AdditionalHitboxGroupsFieldName)
                    return true;
            }
            return false;
        }

        private static bool AddUnique(List<UnityEngine.Object> into, UnityEngine.Object asset)
        {
            if (asset == null || into.Contains(asset))
                return false;
            into.Add(asset);
            return true;
        }

        /// <summary>HitBox 루트와 대상 에셋들에서 그룹 ID 사용 현황을 수집한다.</summary>
        public static List<GroupUsage> Collect(GameObject hitboxRoot, IReadOnlyList<UnityEngine.Object> assets)
        {
            var map = new Dictionary<string, GroupUsage>();

            GroupUsage Get(string id)
            {
                id ??= string.Empty;
                if (!map.TryGetValue(id, out GroupUsage usage))
                {
                    usage = new GroupUsage { GroupId = id, NewGroupId = id };
                    map[id] = usage;
                }
                return usage;
            }

            if (hitboxRoot != null)
            {
                foreach (CombatHitbox hitbox in hitboxRoot.GetComponentsInChildren<CombatHitbox>(true))
                {
                    var so = new SerializedObject(hitbox);
                    SerializedProperty groupProp = so.FindProperty(HitboxComponentGroupFieldName);
                    Get(groupProp != null ? groupProp.stringValue : string.Empty).HitboxCount++;
                }
            }

            if (assets != null)
            {
                foreach (UnityEngine.Object asset in assets)
                {
                    if (asset == null)
                        continue;
                    bool isData = asset is UPlayGroundMotionAbilityPayloadSO;
                    var so = new SerializedObject(asset);
                    SerializedProperty it = so.GetIterator();
                    bool enter = true;
                    while (it.Next(enter))
                    {
                        enter = true;
                        if (it.propertyType == SerializedPropertyType.String && it.name == HitboxGroupFieldName)
                        {
                            GroupUsage usage = Get(it.stringValue);
                            if (isData) usage.DataPhaseCount++;
                            else usage.EventCount++;
                        }
                        else if (!isData && it.isArray && it.name == AdditionalHitboxGroupsFieldName)
                        {
                            for (int i = 0; i < it.arraySize; i++)
                            {
                                SerializedProperty item = it.GetArrayElementAtIndex(i);
                                if (item.propertyType != SerializedPropertyType.String)
                                    continue;

                                Get(item.stringValue).EventCount++;
                            }
                        }
                    }
                }
            }

            var list = new List<GroupUsage>(map.Values);
            list.Sort((a, b) => string.CompareOrdinal(a.GroupId, b.GroupId));
            return list;
        }

        /// <summary>
        /// 단일 패스 remap. map의 키는 '원래' 그룹 ID이므로 A→B, B→C가 함께 있어도 연쇄(cascade)되지 않는다.
        /// 저장은 호출자 책임(프리팹 SaveAsPrefabAsset 등). 변경된 HitBox 개수를 반환.
        /// </summary>
        public static int RemapInHitboxes(GameObject root, IReadOnlyDictionary<string, string> map)
        {
            if (root == null || map == null)
                return 0;

            int changed = 0;
            foreach (CombatHitbox hitbox in root.GetComponentsInChildren<CombatHitbox>(true))
            {
                var so = new SerializedObject(hitbox);
                SerializedProperty groupProp = so.FindProperty(HitboxComponentGroupFieldName);
                if (groupProp == null)
                    continue;
                if (map.TryGetValue(groupProp.stringValue ?? string.Empty, out string to) && to != groupProp.stringValue)
                {
                    groupProp.stringValue = to;
                    so.ApplyModifiedProperties();
                    changed++;
                }
            }
            return changed;
        }

        /// <summary>
        /// 에셋(Ability Payload / MotionSet 등) 내부의 모든 hitboxGroupId를 단일 패스로 remap한다.
        /// 변경된 출현 수를 반환하며 0보다 크면 에셋을 dirty로 표시한다.
        /// </summary>
        public static int RemapInAsset(UnityEngine.Object asset, IReadOnlyDictionary<string, string> map)
        {
            if (asset == null || map == null)
                return 0;

            var so = new SerializedObject(asset);
            int changed = 0;
            SerializedProperty it = so.GetIterator();
            bool enter = true;
            while (it.Next(enter))
            {
                enter = true;
                if (it.propertyType != SerializedPropertyType.String || it.name != HitboxGroupFieldName)
                {
                    if (it.isArray && it.name == AdditionalHitboxGroupsFieldName)
                    {
                        for (int i = 0; i < it.arraySize; i++)
                        {
                            SerializedProperty item = it.GetArrayElementAtIndex(i);
                            if (item.propertyType != SerializedPropertyType.String)
                                continue;

                            if (map.TryGetValue(item.stringValue ?? string.Empty, out string additionalTo)
                                && additionalTo != item.stringValue)
                            {
                                item.stringValue = additionalTo;
                                changed++;
                            }
                        }
                    }

                    continue;
                }

                if (map.TryGetValue(it.stringValue ?? string.Empty, out string to) && to != it.stringValue)
                {
                    it.stringValue = to;
                    changed++;
                }
            }

            if (changed > 0)
            {
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(asset);
            }
            return changed;
        }
    }
}
#endif
