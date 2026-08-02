using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UPlayGround.Combat;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// 충돌 판정 활성화 이벤트.
    /// hitPhaseIndex로 현재 히트가 몇 번째 구간인지 Combat에 알린다.
    /// </summary>
    [Serializable]
    [MovedFrom(true, sourceAssembly: "Assembly-CSharp")]
    [MotionEventDescriptor("Collision", "Combat", 0, "공격 판정을 켜고 HitPhase를 Combat에 전달합니다.", "hitbox", "attack", "damage", "타격", "피격", "콜리전")]
    public class BeginCollisionEvent : MotionEventBase
    {
        [MotionEventLabel("히트 페이즈 인덱스")]
        [Tooltip("AttackInfoBase.hitPhases의 인덱스. 멀티 히트 시 구간마다 다른 값을 설정한다.")]
        public int hitPhaseIndex = 0;

        // AttachedHitboxGroup = 0. 신규 필드를 직렬화하지 않은 기존 에셋이 기본값으로 종전 경로를 유지한다.
        [MotionEventLabel("판정 소스")]
        [Tooltip("판정 소스. 무기·신체 부착형 HitBox 그룹을 쓸지, 이벤트가 직접 소유한 범위를 쓸지 결정한다.")]
        public CollisionSourceType collisionSource = CollisionSourceType.AttachedHitboxGroup;

        [MotionEventLabel("HitBox 그룹 ID")]
        [Tooltip("CombatHitbox.groupId. 비어 있으면 HitPhaseData 또는 Default 그룹을 사용한다.")]
        [MotionEventShowIf(nameof(collisionSource), (int)CollisionSourceType.AttachedHitboxGroup)]
        public string hitboxGroupId;

        [MotionEventLabel("추가 HitBox 그룹")]
        [Tooltip("같은 CollisionEvent에서 함께 활성화할 추가 CombatHitbox.groupId 목록.")]
        [MotionEventShowIf(nameof(collisionSource), (int)CollisionSourceType.AttachedHitboxGroup)]
        public List<string> additionalHitboxGroupIds = new();

        [MotionEventLabel("명시적 판정 범위")]
        [Tooltip("이벤트가 직접 소유하는 명시적 판정 범위. 폭발·충격파·광역 지면 타격에 사용한다.")]
        [MotionEventShowIf(nameof(collisionSource), (int)CollisionSourceType.ExplicitShape)]
        public ExplicitCollisionShapeData explicitShape = new();

        public override string GetDisplayName() => "Collision";

        // OnceOnBegin은 Execute 안에서 즉시 Shape를 샘플링하므로 이번 프레임의 최종 본 포즈가 필요하다.
        // 부착형/Window는 기존 Update 시작 + LateUpdate 검출 타이밍을 유지한다.
        public override bool RequiresPostEvaluation =>
            base.RequiresPostEvaluation
            || collisionSource == CollisionSourceType.ExplicitShape
               && explicitShape?.evaluation == CollisionEvaluationType.OnceOnBegin;

        public override string GetShortLabel()
        {
            if (collisionSource == CollisionSourceType.ExplicitShape)
            {
                string shapeLabel = explicitShape != null ? explicitShape.Describe() : "Shape 미설정";
                return $"Collision [P{hitPhaseIndex} / {shapeLabel}]";
            }

            string groupLabel = string.IsNullOrWhiteSpace(hitboxGroupId) ? "Phase Default" : hitboxGroupId;
            if (additionalHitboxGroupIds != null && additionalHitboxGroupIds.Count > 0)
                groupLabel += $"+{additionalHitboxGroupIds.Count}";
            return $"Collision [P{hitPhaseIndex} / {groupLabel}]";
        }

        /// <summary>런타임 실행에 사용할 요청을 만든다. 에디터 검증 도구도 같은 경로를 사용한다.</summary>
        public CollisionRequest BuildRequest(LayerMask targetLayerMask)
            => collisionSource == CollisionSourceType.ExplicitShape
                ? CollisionRequest.Explicit(hitPhaseIndex, targetLayerMask, explicitShape)
                : CollisionRequest.Attached(hitPhaseIndex, targetLayerMask, hitboxGroupId, additionalHitboxGroupIds);

        public override void Execute(GameObject target)
        {
            if (HandleMotionEventCombatTarget(target, true))
                return;

            GameActor actor = target.GetComponent<GameActor>();
            HandleActorCombat(actor, true);
        }

        public override void OnCompleteEvent(GameObject target)
        {
            if (HandleMotionEventCombatTarget(target, false))
                return;

            GameActor actor = target.GetComponent<GameActor>();
            HandleActorCombat(actor, false);
        }

        private bool HandleMotionEventCombatTarget(GameObject target, bool isCollisionEnable)
        {
            var combatTarget = target.GetComponent<IMotionEventCombatTarget>()
                               ?? target.GetComponentInParent<IMotionEventCombatTarget>()
                               ?? target.GetComponentInChildren<IMotionEventCombatTarget>();
            if (combatTarget == null) return false;

            Debug.Log($"[ResidualAttack] MotionEvent Collision route. target={target.name}, enable={isCollisionEnable}, phase={hitPhaseIndex}, source={collisionSource}, handler={combatTarget.GetType().Name}");
            combatTarget.ClearHitTargets();
            if (isCollisionEnable)
            {
                combatTarget.SetHitPhaseIndex(hitPhaseIndex);
                // 잔류 공격은 자신의 targetLayerMask를 이미 보유하므로 요청의 LayerMask는 사용하지 않는다.
                combatTarget.BeginCollision(BuildRequest(default));
            }
            else
            {
                combatTarget.EndCollision();
            }
            return true;
        }

        // P3 2차: PlayerCombat/EnemyCombat을 직접 호출하지 않고 CombatActionRunner에 위임한다.
        // runner가 등록된 ICombatCollisionExecutor(=각 Combat)에 동일한 순서로 전달한다.
        private void HandleActorCombat(GameActor actor, bool isCollisionEnable)
        {
            if (actor == null) return;
            if (!actor.HasActorType(ActorType.Player) && !actor.HasActorType(ActorType.Monster))
                return;

            CombatActionRunner runner = actor.ActionRunner;
            if (runner == null || !runner.HasCollisionExecutor)
            {
                // P3 3차 이후 충돌 윈도우는 runner instance가 단일 소유한다. runner/executor가 없으면
                // 그 자체가 설정 오류이며, 우회 경로(legacy SetEnableCollision)도 같은 runner로 forward되어
                // 결국 판정이 동작하지 않는다. 따라서 가짜 안전망을 두지 않고 설정 오류로 보고한다.
                Debug.LogError($"[Collision] {actor.name}의 CombatActionRunner/ICombatCollisionExecutor가 준비되지 않아 충돌 이벤트가 무시됩니다.");
                return;
            }

            // 정규화(Trim/중복 제거/primary 결합)는 실제 활성화 시점(각 Combat)에서
            // HitboxGroupIds.Normalize로 일괄 처리한다. 여기서는 원본 목록을 그대로 전달한다.
            runner.HandleCollisionEvent(
                isCollisionEnable,
                BuildRequest(actor.GetAttackTargetLayerMask()));
        }
    }
}
