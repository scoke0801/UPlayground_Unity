#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Combat;
using UPlayGround.Data;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Editor.Ability;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.Gameplay.Ability;
using UPlayGround.EditorTools;

namespace UPlayGround.Tool.Editor.Combat
{
    /// <summary>
    /// MotionSet 타임라인과 AbilitySet의 공격 Payload를 연결하는 에디터 공용 유틸.
    /// 애니메이션 에디터의 전투 오버레이와 프레임 데이터 테이블이 공유한다.
    ///
    /// 시간 규칙: 모든 반환 시간은 MotionSet 전체 타임라인 기준 절대 시간(초).
    /// 캔슬 윈도우 규칙:
    ///   - 저작(CancelWindowEvent 타임라인 이벤트) 있음: 그 구간 = 캔슬 허용(런타임 PlayerCombat.ResolveCancelMask).
    ///   - 미저작(폴백): 콜리전 비활성 구간 = 캔슬 허용(런타임 IsCancelWindowOpen). 오버레이는 점선으로 구분.
    ///   - Move는 두 경우 모두 마지막 히트 페이즈 이후 리커버리에서만(별도 축).
    /// </summary>
    public static class CombatTimelineUtility
    {
        public struct TimedSpan
        {
            public float Start;
            public float End;
            public int PhaseIndex; // Collision 전용. 그 외 -1
            public string HitboxGroupId; // Collision 전용
            public List<string> AdditionalHitboxGroupIds; // Collision 전용

            public float Duration => Mathf.Max(0f, End - Start);
        }

        /// <summary>MotionSetAsset 하나에 대해 AbilitySet에서 찾은 공격 데이터 묶음.</summary>
        public sealed class ResolvedAttack
        {
            public string SourceName;                  // 예: "약 공격 [2]", "차지 2단계"
            public MotionSetAsset MotionAsset;
            public List<HitPhaseData> HitPhases;
            public PlayerInterruptAction InterruptActions;
            public AbilityAttackInfo AttackInfo;        // nullable
            public ChargeStageData ChargeStage;        // nullable
            public UnityEngine.Object Owner;           // Undo/Dirty 대상 (Ability Payload)

            public HitPhaseData GetHitPhase(int index)
            {
                if (HitPhases == null || HitPhases.Count == 0) return null;
                return HitPhases[Mathf.Clamp(index, 0, HitPhases.Count - 1)];
            }
        }

        // ====================================================================
        //  타임라인 스캔
        // ====================================================================

        /// <summary> 모션별 이벤트 + 글로벌 이벤트에서 TEvent 구간을 절대 시간으로 수집한다. </summary>
        public static List<TimedSpan> CollectSpans<TEvent>(MotionSet set) where TEvent : MotionEventBase
        {
            var result = new List<TimedSpan>();
            if (set == null) return result;

            if (set.globalEvents != null)
            {
                foreach (MotionEventBase evt in set.globalEvents)
                    if (evt is TEvent)
                        result.Add(MakeSpan(evt, 0f));
            }

            float offset = 0f;
            if (set.motions != null)
            {
                foreach (UPlayGround.Animation.Motion motion in set.motions)
                {
                    if (motion?.events != null)
                    {
                        foreach (MotionEventBase evt in motion.events)
                            if (evt is TEvent)
                                result.Add(MakeSpan(evt, offset));
                    }
                    offset += motion?.Duration ?? 0f;
                }
            }

            result.Sort((a, b) => a.Start.CompareTo(b.Start));
            return result;
        }

        /// <summary> Collision 구간 수집 (PhaseIndex 포함). </summary>
        public static List<TimedSpan> CollectCollisionSpans(MotionSet set)
        {
            return CollectSpans<BeginCollisionEvent>(set);
        }

        /// <summary> CancelWindowEvent 구간을 maskOverride와 함께 수집(절대 시간). </summary>
        public struct CancelWindowSpanInfo
        {
            public float Start;
            public float End;
            public PlayerInterruptAction Mask;
        }

        public static List<CancelWindowSpanInfo> CollectCancelWindowSpans(MotionSet set)
        {
            var result = new List<CancelWindowSpanInfo>();
            if (set == null) return result;

            if (set.globalEvents != null)
                foreach (MotionEventBase evt in set.globalEvents)
                    if (evt is CancelWindowEvent cw) result.Add(MakeCancelSpan(cw, 0f));

            float offset = 0f;
            if (set.motions != null)
            {
                foreach (UPlayGround.Animation.Motion motion in set.motions)
                {
                    if (motion?.events != null)
                        foreach (MotionEventBase evt in motion.events)
                            if (evt is CancelWindowEvent cw) result.Add(MakeCancelSpan(cw, offset));
                    offset += motion?.Duration ?? 0f;
                }
            }

            result.Sort((a, b) => a.Start.CompareTo(b.Start));
            return result;
        }

        static CancelWindowSpanInfo MakeCancelSpan(CancelWindowEvent cw, float offset) => new CancelWindowSpanInfo
        {
            Start = offset + cw.startTime,
            End = offset + Mathf.Max(cw.startTime, cw.endTime),
            Mask = cw.maskOverride,
        };

        static TimedSpan MakeSpan(MotionEventBase evt, float offset)
        {
            return new TimedSpan
            {
                Start = offset + evt.startTime,
                End = offset + Mathf.Max(evt.startTime, evt.endTime),
                PhaseIndex = evt is BeginCollisionEvent col ? Mathf.Max(0, col.hitPhaseIndex) : -1,
                HitboxGroupId = evt is BeginCollisionEvent collision ? collision.hitboxGroupId : null,
                AdditionalHitboxGroupIds = evt is BeginCollisionEvent collisionWithGroups
                    ? HitboxGroupIds.Normalize(null, collisionWithGroups.additionalHitboxGroupIds)
                    : null,
            };
        }

        /// <summary> 전체 길이에서 spans를 뺀 보집합(= 콜리전 비활성 구간). </summary>
        public static List<TimedSpan> ComputeComplementSpans(List<TimedSpan> spans, float totalDuration, float minWidth = 0.01f)
        {
            var result = new List<TimedSpan>();
            if (totalDuration <= 0f) return result;

            float cursor = 0f;
            if (spans != null)
            {
                foreach (TimedSpan span in spans.OrderBy(s => s.Start))
                {
                    if (span.Start - cursor >= minWidth)
                        result.Add(new TimedSpan { Start = cursor, End = span.Start, PhaseIndex = -1 });
                    cursor = Mathf.Max(cursor, span.End);
                }
            }

            if (totalDuration - cursor >= minWidth)
                result.Add(new TimedSpan { Start = cursor, End = totalDuration, PhaseIndex = -1 });
            return result;
        }

        /// <summary> 선딜(0→첫 판정)/액티브 합/후딜(마지막 판정→끝) 계산. 판정이 없으면 전부 0. </summary>
        public static void ComputeFrameMetrics(
            List<TimedSpan> collisions, float totalDuration,
            out float startup, out float activeTotal, out float recovery)
        {
            startup = 0f;
            activeTotal = 0f;
            recovery = 0f;
            if (collisions == null || collisions.Count == 0 || totalDuration <= 0f) return;

            startup = Mathf.Max(0f, collisions.Min(s => s.Start));
            recovery = Mathf.Max(0f, totalDuration - collisions.Max(s => s.End));
            foreach (TimedSpan span in collisions)
                activeTotal += span.Duration;
        }

        // ====================================================================
        //  AbilitySet 역조회 (MotionSetAsset → 공격 데이터)
        // ====================================================================

        /// <summary>
        /// AbilitySet 안에서 해당 MotionSetAsset을 쓰는 공격 데이터를 모두 찾는다.
        /// 차지 공격은 단계별로 1개씩 반환한다.
        /// </summary>
        /// <param name="motionOwner">
        /// 이 공격을 실행하는 액터의 MotionSet. 지정하면 키 해석을 그 액터(및 fallback 체인)로
        /// 한정한다. null이면 프로젝트 전체에서 찾으므로, 여러 액터가 같은 모션 에셋을 공유할 때
        /// 실제로 그 액터가 쓰지 않는 공격까지 매칭될 수 있다.
        /// </param>
        public static List<ResolvedAttack> ResolveAttacks(
            AbilitySetSO data,
            MotionSetAsset motionAsset,
            ActorAnimationMotionSet motionOwner = null)
        {
            var result = new List<ResolvedAttack>();
            if (data == null || motionAsset == null)
                return result;

            // 키마다 프로젝트를 훑지 않도록 이 호출 한 번 분량의 인덱스만 만든다.
            AbilityMotionIndex index =
                motionOwner != null ? null : new AbilityMotionIndex();

            PlayerCombatAbilityDataView view = PlayerCombatAbilityDataView.Build(data);
            if (view != null)
            {
                AddPlayerList(result, data, view.liteComboAttackList, "약 공격", motionAsset, motionOwner, index);
                AddPlayerList(result, data, view.heavyComboAttackList, "강 공격", motionAsset, motionOwner, index);
                AddPlayerList(result, data, view.jumpAttackList, "점프 공격", motionAsset, motionOwner, index);
                AddPlayerList(result, data, view.dashAttackList, "대시 공격", motionAsset, motionOwner, index);
                AddPlayerList(result, data, view.skillAttackList, "스킬", motionAsset, motionOwner, index);
                AddPlayerInfo(result, data, view.counterAttack, "카운터", motionAsset, motionOwner, index);
                AddPlayerInfo(result, data, view.parryCounterAttack, "패리 카운터", motionAsset, motionOwner, index);
                AddPlayerInfo(result, data, view.entryAttack, "교체 등장", motionAsset, motionOwner, index);
                AddPlayerInfo(result, data, view.entryAttackVsGroggy, "교체 등장 (그로기)", motionAsset, motionOwner, index);
                AddPlayerInfo(result, data, view.entryAttackVsAirborne, "교체 등장 (공중)", motionAsset, motionOwner, index);
                AddPlayerInfo(result, data, view.swapEvadeCounterAttack, "스왑 회피 카운터", motionAsset, motionOwner, index);
                AddPlayerInfo(result, data, view.swapSpecialAttack, "스왑 특수", motionAsset, motionOwner, index);
            }

            var entries = AbilityAttackEditorUtility.Collect(data, true);
            for (int i = 0; i < entries.Count; i++)
            {
                AbilityAttackInfo info = entries[i].AttackInfo;
                if (!MatchesMotion(info.baseInfo.motionKey, motionAsset, motionOwner, index)
                    || result.Exists(x => ReferenceEquals(x.AttackInfo, info)))
                    continue;
                result.Add(new ResolvedAttack
                {
                    SourceName = entries[i].Ability.name,
                    MotionAsset = motionAsset,
                    HitPhases = info.baseInfo.hitPhases,
                    InterruptActions = info.interruptActions,
                    AttackInfo = info,
                    Owner = entries[i].Payload,
                });
            }

            return result;
        }

        static void AddPlayerList(List<ResolvedAttack> result, AbilitySetSO data,
            List<AbilityAttackInfo> list, string listName, MotionSetAsset motionAsset,
            ActorAnimationMotionSet motionOwner, AbilityMotionIndex index)
        {
            if (list == null) return;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i]?.baseInfo == null
                    || !MatchesMotion(list[i].baseInfo.motionKey, motionAsset, motionOwner, index))
                    continue;
                AddPlayerInfo(result, data, list[i], $"{listName} [{i}]", motionAsset, motionOwner, index);
            }
        }

        static void AddPlayerInfo(List<ResolvedAttack> result, AbilitySetSO data,
            AbilityAttackInfo info, string sourceName, MotionSetAsset motionAsset,
            ActorAnimationMotionSet motionOwner, AbilityMotionIndex index)
        {
            if (info?.baseInfo == null) return;
            // 리스트 직접 매칭이 아닌 단일 슬롯(카운터 등)도 액터 매핑으로 비교한다.
            if (!MatchesMotion(info.baseInfo.motionKey, motionAsset, motionOwner, index)) return;
            // 동일 인스턴스 중복 방지 (skillDefinitions가 리스트 항목을 공유하는 경우)
            foreach (ResolvedAttack existing in result)
                if (ReferenceEquals(existing.AttackInfo, info)) return;

            result.Add(new ResolvedAttack
            {
                SourceName = sourceName,
                MotionAsset = motionAsset,
                HitPhases = info.baseInfo.hitPhases,
                InterruptActions = info.interruptActions,
                AttackInfo = info,
                Owner = data,
            });
        }

        static bool MatchesMotion(
            AbilityMotionKey motionKey,
            MotionSetAsset motionAsset,
            ActorAnimationMotionSet motionOwner,
            AbilityMotionIndex index)
        {
            if (!motionKey.IsValid || motionAsset == null)
                return false;
            // 액터를 알면 그 액터의 해석만 본다. 같은 키가 무기·액터마다 다른 모션을 가리키므로
            // 전역 매칭은 그 액터가 실제로 쓰지 않는 공격까지 끌어온다.
            if (motionOwner != null)
                return motionOwner.GetAbilityMotionAsset(motionKey) == motionAsset;
            return index != null && index.Matches(motionKey, motionAsset);
        }

        // ====================================================================
        //  MotionSet ↔ AbilitySet 자동 연결
        // ====================================================================

        /// <summary> root와 fallback 체인을 순회한다 (최대 8단계, 순환 방지). </summary>
        public static IEnumerable<ActorAnimationMotionSet> EnumerateMotionSets(ActorAnimationMotionSet root, bool includeFallback)
        {
            var visited = new HashSet<ActorAnimationMotionSet>();
            ActorAnimationMotionSet current = root;
            int depth = 0;
            while (current != null && visited.Add(current) && depth++ < 8)
            {
                yield return current;
                if (!includeFallback) yield break;
                current = current.fallbackMotionSet;
            }
        }

        /// <summary>
        /// ActorDefinitionSO 전체를 스캔해 이 MotionSet(또는 fallback 체인)을 쓰는 몬스터의 AbilitySet을 찾는다.
        /// 플레이어 세트는 ActorDefinitionSO에 연결되지 않으므로 찾지 못한다(수동 지정 + 캐시 사용).
        /// </summary>
        public static AbilitySetSO FindAbilitySetForMotionSet(
            ActorAnimationMotionSet motionSet,
            out ActorDefinitionSO owner,
            out bool ambiguous,
            out string candidateSummary)
        {
            owner = null;
            ambiguous = false;
            candidateSummary = string.Empty;
            if (motionSet == null) return null;

            var candidates = new Dictionary<AbilitySetSO, List<ActorDefinitionSO>>();

            foreach (string guid in AssetDatabase.FindAssets("t:ActorDefinitionSO"))
            {
                var actor = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (actor == null || actor.EffectiveAbilitySet == null || actor.prefab == null) continue;

                var animator = actor.prefab.GetComponentInChildren<ActorAnimator>(true);
                if (animator == null || animator.MotionSet == null) continue;

                foreach (ActorAnimationMotionSet set in EnumerateMotionSets(animator.MotionSet, true))
                {
                    if (set != motionSet) continue;

                    if (!candidates.TryGetValue(
                            actor.EffectiveAbilitySet,
                            out List<ActorDefinitionSO> owners))
                    {
                        owners = new List<ActorDefinitionSO>();
                        candidates.Add(actor.EffectiveAbilitySet, owners);
                    }
                    owners.Add(actor);
                    break;
                }
            }

            if (candidates.Count == 0)
                return null;

            var descriptions = new List<string>();
            foreach (KeyValuePair<AbilitySetSO, List<ActorDefinitionSO>> candidate in candidates)
            {
                string actorNames = string.Join(
                    ", ",
                    candidate.Value.Select(candidateOwner => candidateOwner.name));
                descriptions.Add($"{candidate.Key.name} ({actorNames})");
            }
            descriptions.Sort(StringComparer.Ordinal);
            candidateSummary = string.Join("; ", descriptions);

            if (candidates.Count > 1)
            {
                ambiguous = true;
                return null;
            }

            KeyValuePair<AbilitySetSO, List<ActorDefinitionSO>> unique = candidates.First();
            owner = unique.Value[0];
            return unique.Key;
        }

        // ── MotionSet GUID → AbilitySet GUID 수동 매핑 캐시 (플레이어 세트용) ──
        const string PAIR_PREFS_PREFIX = "UPlayground.CombatTimeline.AbilitySetFor.";

        public static void SaveAttackDataPairing(UnityEngine.Object motionSetAsset, AbilitySetSO attackData)
        {
            if (motionSetAsset is ActorAnimationMotionSet actorMotionSet)
            {
                if (actorMotionSet.attackAbilitySet != attackData)
                {
                    Undo.RecordObject(actorMotionSet, "Connect Attack Ability Set");
                    actorMotionSet.attackAbilitySet = attackData;
                    EditorUtility.SetDirty(actorMotionSet);
                    AssetDatabase.SaveAssetIfDirty(actorMotionSet);
                }
            }

            string setGuid = GetAssetGuid(motionSetAsset);
            if (string.IsNullOrEmpty(setGuid)) return;

            string dataGuid = GetAssetGuid(attackData);
            if (string.IsNullOrEmpty(dataGuid))
                EditorPrefs.DeleteKey(PAIR_PREFS_PREFIX + setGuid);
            else
                EditorPrefs.SetString(PAIR_PREFS_PREFIX + setGuid, dataGuid);
        }

        public static AbilitySetSO LoadAttackDataPairing(UnityEngine.Object motionSetAsset)
        {
            if (motionSetAsset is ActorAnimationMotionSet actorMotionSet
                && actorMotionSet.attackAbilitySet != null)
                return actorMotionSet.attackAbilitySet;

            string setGuid = GetAssetGuid(motionSetAsset);
            if (string.IsNullOrEmpty(setGuid)) return null;

            string dataGuid = EditorPrefs.GetString(PAIR_PREFS_PREFIX + setGuid, string.Empty);
            if (string.IsNullOrEmpty(dataGuid)) return null;
            return AssetDatabase.LoadAssetAtPath<AbilitySetSO>(AssetDatabase.GUIDToAssetPath(dataGuid));
        }

        static string GetAssetGuid(UnityEngine.Object obj)
        {
            if (obj == null) return null;
            string path = AssetDatabase.GetAssetPath(obj);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.AssetPathToGUID(path);
        }

        // ====================================================================
        //  표시 포맷
        // ====================================================================

        public static string FormatInterruptMask(PlayerInterruptAction mask)
        {
            if (mask == PlayerInterruptAction.None) return "없음";

            var names = new List<string>();
            if ((mask & PlayerInterruptAction.Dodge) != 0) names.Add("회피");
            if ((mask & PlayerInterruptAction.Jump) != 0) names.Add("점프");
            if ((mask & PlayerInterruptAction.Dash) != 0) names.Add("대시");
            if ((mask & PlayerInterruptAction.Guard) != 0) names.Add("가드");
            if ((mask & PlayerInterruptAction.LightAttack) != 0) names.Add("약공");
            if ((mask & PlayerInterruptAction.HeavyAttack) != 0) names.Add("강공");
            if ((mask & PlayerInterruptAction.Skill) != 0) names.Add("스킬");
            if ((mask & PlayerInterruptAction.Move) != 0) names.Add("이동");
            return string.Join("·", names);
        }
    }
}
#endif
