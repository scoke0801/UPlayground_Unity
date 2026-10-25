using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UPlayGround.Combat;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;

namespace UPlayGround.Animation.Editor
{
    public static partial class CombatMotionAnalysis
    {
        /// <summary>현재 resolver의 이벤트 범위를 사용한다. 이벤트 Execute는 호출하지 않는다.</summary>
        public static void CollectTiming(MotionSet set, AbilityAttackInfo attack, string usage, CombatMotionMeasurement result)
        {
            var events = new List<MotionEventBase>();
            MotionTimelineResolver.CollectEventsInRange(set, float.MinValue, float.MaxValue, events);
            result.hasSinglePassTiming = set.sections.Count == 1 &&
                (set.sections[0].endPolicy == MotionSectionEndPolicy.Stop || set.sections[0].endPolicy == MotionSectionEndPolicy.Continue) &&
                string.IsNullOrEmpty(set.sections[0].defaultNextId) && !events.Any(e => e is IMotionTimelineControlEvent) &&
                set.timeStretch?.enabled != true && (set.curves == null || set.curves.Count == 0);
            foreach (var section in set.sections)
            {
                MotionTimelineResolver.TryGetSection(set, section.id, out var range);
                result.windows.Add(new MotionTimingWindow { kind = "Section", label = section.displayName,
                    start = range.startTime, end = range.endTime, source = $"{section.endPolicy} → {section.defaultNextId}" });
            }
            foreach (var evt in events)
            {
                if (!MotionTimelineResolver.TryGetEventGlobalRange(set, evt, out float start, out float end)) continue;
                var window = new MotionTimingWindow { kind = "Event", label = evt.GetDisplayName(), start = start, end = end,
                    source = evt.GetType().FullName };
                if (evt is BeginCollisionEvent hit) ConfigureHit(window, hit, attack);
                if (evt is CancelWindowEvent cancel) { window.kind = "AuthoredCancel"; window.actions = ((int)cancel.maskOverride).ToString(); }
                if (evt is ComboWindowEvent) window.kind = "Combo";
                if (evt.GetType().Name.Contains("Telegraph")) window.kind = "Telegraph";
                result.windows.Add(window);
                if (evt is DisableCollisionEvent)
                {
                    result.hasSinglePassTiming = false;
                    result.limitations.Add("DisableCollision의 OFF/ON 복구가 있는 타임라인: 원본 이벤트만 표시하며 최종 판정·캔슬 구간은 축약하지 않는다.");
                }
            }
            var hits = result.windows.Where(w => w.kind == "Hit").OrderBy(w => w.start).ToList();
            for (int i = 1; i < hits.Count; i++) hits[i].gapFromPrevious = hits[i].start - hits[i - 1].end;
            if (!result.hasSinglePassTiming)
            {
                result.limitations.Add("분기/루프/시간 보정 또는 복합 판정이 있어 beforeFirstHit/afterLastHit는 미산출(-1). 궤적은 저작 타임라인 1회 순회다.");
                return;
            }
            if (hits.Count == 0) return;
            result.beforeFirstHit = hits.Min(w => w.start);
            result.afterLastHit = Mathf.Max(0, set.TotalDuration - hits.Max(w => w.end));
            result.limitations.Add("afterLastHit는 마지막 판정 뒤 남은 애니메이션 길이이며 조작 불가능 시간이 아니다.");
            bool grounded = usage.StartsWith("LightCombo", StringComparison.Ordinal) || usage.StartsWith("HeavyCombo", StringComparison.Ordinal);
            if (grounded) CollectGroundedCancels(set, attack, result);
            else result.limitations.Add("이 사용처는 PlayerAttackState 외 상태다. 저작 Cancel 이벤트만 표시하며 행동별 실행 가능 시간은 보장하지 않는다.");
        }

        private static void ConfigureHit(MotionTimingWindow window, BeginCollisionEvent hit, AbilityAttackInfo attack)
        {
            window.kind = "Hit";
            window.hitPhaseIndex = hit.hitPhaseIndex;
            if (hit.collisionSource != CollisionSourceType.AttachedHitboxGroup)
            {
                window.source += " / ExplicitShape 공간 측정 미지원";
                return;
            }
            if (hit.hitPhaseIndex < 0 || hit.hitPhaseIndex >= attack.baseInfo.hitPhases.Count)
                throw new InvalidOperationException($"유효하지 않은 HitPhase: {hit.hitPhaseIndex}");
            string primary = string.IsNullOrWhiteSpace(hit.hitboxGroupId)
                ? attack.baseInfo.hitPhases[hit.hitPhaseIndex].hitboxGroupId : hit.hitboxGroupId;
            if (string.IsNullOrWhiteSpace(primary)) primary = CombatHitbox.DefaultGroupId;
            window.groups = HitboxGroupIds.Normalize(primary, hit.additionalHitboxGroupIds) ?? new List<string> { primary.Trim() };
        }

        private static void CollectGroundedCancels(MotionSet set, AbilityAttackInfo attack, CombatMotionMeasurement result)
        {
            var authored = result.windows.Where(w => w.kind == "AuthoredCancel").ToArray();
            var hits = result.windows.Where(w => w.kind == "Hit").ToArray();
            var combos = result.windows.Where(w => w.kind == "Combo").ToArray();
            var boundaries = new SortedSet<float> { 0, set.TotalDuration };
            foreach (var window in result.windows)
            {
                boundaries.Add(Mathf.Clamp(window.start, 0, set.TotalDuration));
                boundaries.Add(Mathf.Clamp(window.end, 0, set.TotalDuration));
            }
            foreach (var hit in hits) boundaries.Add(Mathf.Clamp(hit.end + attack.moveCancelDelayAfterLastHit, 0, set.TotalDuration));
            float[] times = boundaries.ToArray();
            for (int i = 0; i + 1 < times.Length; i++)
            {
                float start = times[i], end = times[i + 1], time = (start + end) * 0.5f;
                bool colliding = hits.Any(h => Contains(h, time));
                var active = authored.Where(w => Contains(w, time)).ToArray();
                var mask = ResolveCancelMask(attack.interruptActions, colliding, active.Select(w => (PlayerInterruptAction)int.Parse(w.actions)));
                // 기본 약/강은 공용 인터럽트가 아닌 ComboWindow 경로가 소유한다.
                mask &= PlayerInterruptAction.Dodge | PlayerInterruptAction.Jump | PlayerInterruptAction.Dash |
                    PlayerInterruptAction.Guard | PlayerInterruptAction.Skill;
                if (!hits.Any(h => h.start <= time)) mask &= ~PlayerInterruptAction.Guard;
                var previous = hits.Where(h => h.end <= time).OrderBy(h => h.end).LastOrDefault();
                bool canMove = (attack.interruptActions & PlayerInterruptAction.Move) != 0 && !colliding &&
                    !combos.Any(w => Contains(w, time)) && previous != null &&
                    previous.hitPhaseIndex >= attack.baseInfo.hitPhases.Count - 1 &&
                    time >= previous.end + Mathf.Max(0, attack.moveCancelDelayAfterLastHit);
                if (canMove) mask |= PlayerInterruptAction.Move;
                if (mask == PlayerInterruptAction.None) continue;
                result.windows.Add(new MotionTimingWindow { kind = "Cancel", start = start, end = end,
                    actions = mask.ToString(), label = mask.ToString(), source = active.Length > 0 ? "저작 마스크 교집합 + 상태 게이트" : "콜리전 OFF 폴백 + 상태 게이트" });
            }
            var cancels = result.windows.Where(w => w.kind == "Cancel").ToList();
            for (int i = cancels.Count - 1; i > 0; i--)
            {
                var previous = cancels[i - 1]; var current = cancels[i];
                if (Mathf.Abs(previous.end - current.start) > 0.000001f || previous.actions != current.actions || previous.source != current.source) continue;
                previous.end = current.end;
                result.windows.Remove(current);
            }
            result.limitations.Add("행동별 캔슬은 PlayerAttackState의 시간/마스크 게이트 추론. 입력·접지·자원·전환 승인 조건은 별도이며 콤보 대기 시 이동/스킬보다 콤보가 우선한다.");
        }

        /// <summary>PlayerCombat.ResolveCancelMask의 활성 이벤트 합집합·폴백 계약.</summary>
        public static PlayerInterruptAction ResolveCancelMask(PlayerInterruptAction global, bool colliding, IEnumerable<PlayerInterruptAction> activeMasks)
        {
            var masks = activeMasks.ToArray();
            if (masks.Length == 0) return colliding ? PlayerInterruptAction.None : global;
            PlayerInterruptAction result = PlayerInterruptAction.None;
            foreach (var mask in masks) result |= mask == PlayerInterruptAction.None ? global : global & mask;
            return result;
        }

        private static bool Contains(MotionTimingWindow window, float time) => time >= window.start && time < window.end;
    }
}
