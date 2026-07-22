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
        {
            if (_collisionExecutor == null)
            {
                Debug.LogError($"[CombatActionRunner] {name}에 ICombatCollisionExecutor가 등록되지 않았습니다. Collision 이벤트가 무시됩니다.");
                return;
            }

            _collisionExecutor.ClearHitTargets();
            if (enable)
            {
                _collisionExecutor.SetTargetLayerMask(targetLayer);
                _collisionExecutor.SetHitPhaseIndex(hitPhaseIndex);
                // SetHitboxGroup이 먼저 그룹 목록을 비우므로 순서 유지 필수.
                // SetHitboxGroups는 null/빈 목록을 안전하게 무시(단일 그룹으로 폴백)한다.
                _collisionExecutor.SetHitboxGroup(hitboxGroupId);
                _collisionExecutor.SetHitboxGroups(hitboxGroupIds);
                CurrentAction?.SetHitboxGroup(hitboxGroupId);
            }
            _collisionExecutor.SetEnableCollision(enable);
            if (!enable)
                CurrentAction?.SetHitboxGroup(null);
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

            var definition = new CombatActionDefinition(
                attackData.motionAsset,
                attackData,
                attackData);
            CurrentAction = new CombatActionInstance(_owner, definition);
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
