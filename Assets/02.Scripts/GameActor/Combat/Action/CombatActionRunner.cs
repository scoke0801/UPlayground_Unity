using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.Combat
{
    public class CombatActionRunner : MonoBehaviour
    {
        private GameActor _owner;
        private ICombatCollisionExecutor _collisionExecutor;

        public CombatActionInstance CurrentAction { get; private set; }
        public bool HasAction => CurrentAction != null;
        public bool HasCollisionExecutor => _collisionExecutor != null;
        public int CurrentPhaseIndex => CurrentAction != null ? CurrentAction.CurrentPhaseIndex : 0;
        public bool IsCollisionActive => CurrentAction != null && CurrentAction.IsCollisionActive;
        public string ActiveHitboxGroupId => CurrentAction?.ActiveHitboxGroupId;

        private void Awake()
        {
            _owner = GetComponent<GameActor>();
            _owner?.RegisterActionRunner(this);
        }

        /// <summary>PlayerCombat/EnemyCombat이 init 시 자신을 충돌 실행체로 등록한다.</summary>
        public void SetCollisionExecutor(ICombatCollisionExecutor executor) => _collisionExecutor = executor;

        /// <summary>
        /// BeginCollisionEvent 진입점. 기존 MotionEvent_Collision의 actor 분기와 동작이 동일하다:
        /// ClearHitTargets는 항상, enable 시에만 target layer / hit phase 설정, 마지막에 collision 토글.
        /// </summary>
        public void HandleCollisionEvent(
            bool enable,
            int hitPhaseIndex,
            string hitboxGroupId,
            LayerMask targetLayer)
            => HandleCollisionEvent(enable, hitPhaseIndex, hitboxGroupId, null, targetLayer);

        public void HandleCollisionEvent(
            bool enable,
            int hitPhaseIndex,
            string hitboxGroupId,
            IReadOnlyList<string> hitboxGroupIds,
            LayerMask targetLayer)
            => HandleCollisionEvent(
                enable,
                CollisionRequest.Attached(hitPhaseIndex, targetLayer, hitboxGroupId, hitboxGroupIds));

        /// <summary>
        /// BeginCollisionEvent 진입점. 판정 소스(부착형 그룹 / 명시적 Shape)를 요청 하나로 전달한다.
        /// enable일 때 원자적 요청을 실행체에 전달해 HitPhase·대상 초기화와 윈도우 시작을 한 번에 처리한다.
        /// </summary>
        public void HandleCollisionEvent(bool enable, in CollisionRequest request)
        {
            if (_collisionExecutor == null)
            {
                Debug.LogError($"[CombatActionRunner] {name}에 ICombatCollisionExecutor가 등록되지 않았습니다. Collision 이벤트가 무시됩니다.");
                return;
            }

            if (enable)
            {
                // Explicit Shape는 부착형 그룹을 사용하지 않으므로 액션 상태에 그룹을 기록하지 않는다.
                CurrentAction?.SetHitboxGroup(request.IsExplicit ? null : request.PrimaryHitboxGroupId);
                _collisionExecutor.BeginCollision(request);
            }
            else
            {
                // 기존 계약 보존: Collision 종료 이벤트도 적중 대상 캐시를 비운다.
                _collisionExecutor.ClearHitTargets();
                _collisionExecutor.EndCollision();
                CurrentAction?.SetHitboxGroup(null);
            }
        }

        /// <summary>
        /// DisableCollisionEvent 진입점. 기존 동작 보존: enable 복구 시에만 ClearHitTargets, phase/layer는 건드리지 않음.
        /// </summary>
        public void HandleCollisionToggle(bool enable)
        {
            if (_collisionExecutor == null)
            {
                Debug.LogError($"[CombatActionRunner] {name}에 ICombatCollisionExecutor가 등록되지 않았습니다. DisableCollision 이벤트가 무시됩니다.");
                return;
            }

            if (enable)
                _collisionExecutor.ClearHitTargets();
            _collisionExecutor.SetEnableCollision(enable);
        }

        public void StartAction(AttackData attackData)
        {
            if (attackData == null)
                return;

            CurrentAction = new CombatActionInstance(_owner);
            HandleTimelineEvent(CombatTimelineEventType.ActionStarted, attackData.hitPhaseIndex);
        }

        public void CancelCurrentAction()
        {
            if (CurrentAction == null)
                return;

            CurrentAction.ApplyTimelineEvent(new CombatTimelineEvent(
                CombatTimelineEventType.EndCollision,
                CurrentAction.CurrentPhaseIndex,
                Time.time));
            CurrentAction = null;
        }

        public void HandleTimelineEvent(CombatTimelineEventType type, int hitPhaseIndex = 0)
        {
            if (CurrentAction == null)
                return;

            CurrentAction.ApplyTimelineEvent(new CombatTimelineEvent(type, hitPhaseIndex, Time.time));

            if (type == CombatTimelineEventType.ActionEnded)
                CurrentAction = null;
        }
    }
}
