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
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.Gameplay.Ability;

namespace UPlayGround.Tool.Editor.Combat
{
    /// <summary>
    /// MotionSet 타임라인과 AttackDataSO를 연결하는 에디터 공용 유틸.
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

        /// <summary> AnimKey 하나에 대해 AttackDataSO에서 찾은 공격 데이터 묶음. </summary>
        public sealed class ResolvedAttack
        {
            public string SourceName;                  // 예: "약 공격 [2]", "차지 2단계"
            public AnimKey AnimKey;
            public List<HitPhaseData> HitPhases;
            public PlayerInterruptAction InterruptActions;
            public PlayerAttackInfo PlayerInfo;        // nullable
            public EnemyAttackInfo EnemyInfo;          // nullable
            public ChargeStageData ChargeStage;        // nullable
            public UnityEngine.Object Owner;           // Undo/Dirty 대상 (AttackDataSO)

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
        //  AttackDataSO 역조회 (AnimKey → 공격 데이터)
        // ====================================================================

        /// <summary>
        /// AttackDataSO 안에서 해당 AnimKey를 쓰는 공격 데이터를 모두 찾는다.
        /// 차지 공격은 단계별로 1개씩 반환한다.
        /// </summary>
        public static List<ResolvedAttack> ResolveAttacks(AttackDataSO data, AnimKey key)
        {
            var result = new List<ResolvedAttack>();
            if (data == null || key == AnimKey.None) return result;

            if (data is EnemyAttackDataSO enemy)
                ResolveEnemyAttacks(enemy, key, result);

            return result;
        }

        public static List<ResolvedAttack> ResolveAttacks(AbilitySetSO data, AnimKey key)
        {
            var result = new List<ResolvedAttack>();
            if (data == null || key == AnimKey.None)
                return result;

            PlayerCombatAbilityDataView view = PlayerCombatAbilityDataView.Build(data);
            if (view == null)
                return result;

            AddPlayerList(result, data, view.liteComboAttackList, "약 공격", key);
            AddPlayerList(result, data, view.heavyComboAttackList, "강 공격", key);
            AddPlayerList(result, data, view.jumpAttackList, "점프 공격", key);
            AddPlayerList(result, data, view.dashAttackList, "대시 공격", key);
            AddPlayerList(result, data, view.skillAttackList, "스킬", key);
            AddPlayerInfo(result, data, view.counterAttack, "카운터", key);
            AddPlayerInfo(result, data, view.parryCounterAttack, "패리 카운터", key);
            AddPlayerInfo(result, data, view.entryAttack, "교체 등장", key);
            AddPlayerInfo(result, data, view.entryAttackVsGroggy, "교체 등장 (그로기)", key);
            AddPlayerInfo(result, data, view.entryAttackVsAirborne, "교체 등장 (공중)", key);
            AddPlayerInfo(result, data, view.swapEvadeCounterAttack, "스왑 회피 카운터", key);
            AddPlayerInfo(result, data, view.swapSpecialAttack, "스왑 특수", key);

            return result;
        }

        static void AddPlayerList(List<ResolvedAttack> result, AbilitySetSO data,
            List<PlayerAttackInfo> list, string listName, AnimKey key)
        {
            if (list == null) return;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i]?.baseInfo == null || list[i].baseInfo.animKey != key) continue;
                AddPlayerInfo(result, data, list[i], $"{listName} [{i}]", key);
            }
        }

        static void AddPlayerInfo(List<ResolvedAttack> result, AbilitySetSO data,
            PlayerAttackInfo info, string sourceName, AnimKey key)
        {
            if (info?.baseInfo == null) return;
            // 리스트 직접 매칭이 아닌 단일 슬롯(카운터 등)은 animKey 일치 시에만 추가
            if (info.baseInfo.animKey != key) return;
            // 동일 인스턴스 중복 방지 (skillDefinitions가 리스트 항목을 공유하는 경우)
            foreach (ResolvedAttack existing in result)
                if (ReferenceEquals(existing.PlayerInfo, info)) return;

            result.Add(new ResolvedAttack
            {
                SourceName = sourceName,
                AnimKey = key,
                HitPhases = info.baseInfo.hitPhases,
                InterruptActions = info.interruptActions,
                PlayerInfo = info,
                Owner = data,
            });
        }

        static void ResolveEnemyAttacks(EnemyAttackDataSO data, AnimKey key, List<ResolvedAttack> result)
        {
            if (data.skills == null) return;
            for (int i = 0; i < data.skills.Count; i++)
            {
                EnemyAttackInfo skill = data.skills[i];
                if (skill?.baseInfo == null || skill.baseInfo.animKey != key) continue;
                result.Add(new ResolvedAttack
                {
                    SourceName = $"스킬 [{i}] {skill.attackCategory}",
                    AnimKey = key,
                    HitPhases = skill.baseInfo.hitPhases,
                    InterruptActions = PlayerInterruptAction.None,
                    EnemyInfo = skill,
                    Owner = data,
                });
            }
        }

        // ====================================================================
        //  MotionSet ↔ AttackDataSO 자동 연결
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
        /// ActorDefinitionSO 전체를 스캔해 이 MotionSet(또는 fallback 체인)을 쓰는 몬스터의 attackData를 찾는다.
        /// 플레이어 세트는 ActorDefinitionSO에 연결되지 않으므로 찾지 못한다(수동 지정 + 캐시 사용).
        /// </summary>
        public static EnemyAttackDataSO FindEnemyAttackDataForMotionSet(
            ActorAnimationMotionSet motionSet, out ActorDefinitionSO owner)
        {
            owner = null;
            if (motionSet == null) return null;

            foreach (string guid in AssetDatabase.FindAssets("t:ActorDefinitionSO"))
            {
                var actor = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (actor == null || actor.attackData == null || actor.prefab == null) continue;

                var animator = actor.prefab.GetComponentInChildren<ActorAnimator>(true);
                if (animator == null || animator.MotionSet == null) continue;

                foreach (ActorAnimationMotionSet set in EnumerateMotionSets(animator.MotionSet, true))
                {
                    if (set != motionSet) continue;
                    owner = actor;
                    return actor.attackData;
                }
            }
            return null;
        }

        // ── MotionSet GUID → AttackDataSO GUID 수동 매핑 캐시 (플레이어 세트용) ──
        const string PAIR_PREFS_PREFIX = "UPlayground.CombatTimeline.AttackDataFor.";

        public static void SaveAttackDataPairing(UnityEngine.Object motionSetAsset, AttackDataSO attackData)
        {
            string setGuid = GetAssetGuid(motionSetAsset);
            if (string.IsNullOrEmpty(setGuid)) return;

            string dataGuid = GetAssetGuid(attackData);
            if (string.IsNullOrEmpty(dataGuid))
                EditorPrefs.DeleteKey(PAIR_PREFS_PREFIX + setGuid);
            else
                EditorPrefs.SetString(PAIR_PREFS_PREFIX + setGuid, dataGuid);
        }

        public static AttackDataSO LoadAttackDataPairing(UnityEngine.Object motionSetAsset)
        {
            string setGuid = GetAssetGuid(motionSetAsset);
            if (string.IsNullOrEmpty(setGuid)) return null;

            string dataGuid = EditorPrefs.GetString(PAIR_PREFS_PREFIX + setGuid, string.Empty);
            if (string.IsNullOrEmpty(dataGuid)) return null;
            return AssetDatabase.LoadAssetAtPath<AttackDataSO>(AssetDatabase.GUIDToAssetPath(dataGuid));
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
