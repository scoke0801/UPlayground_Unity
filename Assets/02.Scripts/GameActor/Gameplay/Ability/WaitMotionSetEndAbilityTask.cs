using System;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Animation;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Event;
using UPlayGround.Data.Projectile;
using UPlayGround.Manager;

namespace UPlayGround.Gameplay.Ability
{
    [CreateAssetMenu(
        fileName = "AbilityTask_WaitMotionSetEnd",
        menuName = "UPlayGround/Ability/Task/Wait MotionSet End")]
    public sealed class WaitMotionSetEndAbilityTask : AbilityTaskDefinitionSO
    {
        public override AbilityTaskInstance CreateRuntime(AbilityTaskContext context) =>
            new WaitMotionSetEndAbilityTaskInstance(context);
    }

    /// <summary>
    /// 액터 상태가 시작한 MotionSet 종료를 Ability Task 수명주기에 연결한다.
    /// 부모 Ability 취소 시 Motion 이벤트 구독을 즉시 정리한다.
    /// </summary>
    public sealed class WaitMotionSetEndAbilityTaskInstance : AbilityTaskInstance
    {
        private ActorAnimator _animator;

        public WaitMotionSetEndAbilityTaskInstance(AbilityTaskContext context) : base(context) { }

        protected override void OnActivate()
        {
            if (!AbilitySystemComponent.TryResolve(Context.Owner.Handle, out var component))
            {
                Fail("AbilitySystemComponent를 찾을 수 없습니다.");
                return;
            }

            GameActor owner = component.GetComponent<GameActor>();
            _animator = owner != null ? owner.Animator : null;
            if (_animator == null)
            {
                Fail("ActorAnimator를 찾을 수 없습니다.");
                return;
            }
            _animator.OnMotionSetEnded += OnMotionSetEnded;
        }

        private void OnMotionSetEnded(MotionSet _, bool completed)
        {
            if (completed) Succeed("MotionCompleted");
            else Fail("MotionInterrupted");
        }

        protected override void OnEnd()
        {
            if (_animator != null)
                _animator.OnMotionSetEnded -= OnMotionSetEnded;
            _animator = null;
        }
    }

    [CreateAssetMenu(
        fileName = "AbilityTask_WaitMotionEvent",
        menuName = "UPlayGround/Ability/Task/Wait Motion Event")]
    public sealed class WaitMotionEventAbilityTask : AbilityTaskDefinitionSO
    {
        [Tooltip("비우면 다음 MotionEvent를 기다립니다. 타입 이름과 정확히 일치시킵니다.")]
        public string eventTypeName;

        public override AbilityTaskInstance CreateRuntime(AbilityTaskContext context) =>
            new WaitMotionEventAbilityTaskInstance(context, eventTypeName);
    }

    public sealed class WaitMotionEventAbilityTaskInstance : AbilityTaskInstance
    {
        private readonly string _eventTypeName;
        private MotionEventExecutor _executor;

        public WaitMotionEventAbilityTaskInstance(
            AbilityTaskContext context,
            string eventTypeName) : base(context) =>
            _eventTypeName = eventTypeName?.Trim() ?? string.Empty;

        protected override void OnActivate()
        {
            if (!AbilitySystemComponent.TryResolve(
                    Context.Owner.Handle,
                    out AbilitySystemComponent component))
            {
                Fail("AbilitySystemComponent를 찾을 수 없습니다.");
                return;
            }
            _executor = component.GetComponentInChildren<MotionEventExecutor>();
            if (_executor == null)
            {
                Fail("MotionEventExecutor를 찾을 수 없습니다.");
                return;
            }
            _executor.EventExecuted += OnEventExecuted;
        }

        protected override void OnEnd()
        {
            if (_executor != null)
                _executor.EventExecuted -= OnEventExecuted;
            _executor = null;
        }

        private void OnEventExecuted(MotionEventBase motionEvent)
        {
            if (motionEvent == null
                || (!string.IsNullOrEmpty(_eventTypeName)
                    && !string.Equals(
                        motionEvent.GetType().Name,
                        _eventTypeName,
                        StringComparison.Ordinal)))
                return;
            Succeed(motionEvent.GetType().Name);
        }
    }

    [CreateAssetMenu(
        fileName = "AbilityTask_ApplyEffect",
        menuName = "UPlayGround/Ability/Task/Apply Effect")]
    public sealed class ApplyEffectAbilityTask : AbilityTaskDefinitionSO
    {
        public GameplayEffectSO effect;

        public override AbilityTaskInstance CreateRuntime(AbilityTaskContext context) =>
            new ApplyEffectAbilityTaskInstance(context, effect, false);
    }

    [CreateAssetMenu(
        fileName = "AbilityTask_RemoveEffect",
        menuName = "UPlayGround/Ability/Task/Remove Effect")]
    public sealed class RemoveEffectAbilityTask : AbilityTaskDefinitionSO
    {
        public GameplayEffectSO effect;

        public override AbilityTaskInstance CreateRuntime(AbilityTaskContext context) =>
            new ApplyEffectAbilityTaskInstance(context, effect, true);
    }

    public sealed class ApplyEffectAbilityTaskInstance : AbilityTaskInstance
    {
        private readonly GameplayEffectSO _effect;
        private readonly bool _remove;

        public ApplyEffectAbilityTaskInstance(
            AbilityTaskContext context,
            GameplayEffectSO effect,
            bool remove) : base(context)
        {
            _effect = effect;
            _remove = remove;
        }

        protected override void OnActivate()
        {
            if (_effect == null
                || !AbilitySystemComponent.TryResolve(
                    Context.Owner.Handle,
                    out AbilitySystemComponent component))
            {
                Fail("Effect 또는 AbilitySystemComponent가 없습니다.");
                return;
            }
            GameActor actor = component.GetComponent<GameActor>();
            if (actor?.Effects == null)
            {
                Fail("GameplayEffectController가 없습니다.");
                return;
            }
            if (_remove)
            {
                actor.Effects.RemoveEffectsByDefinition(_effect);
            }
            else
            {
                if (!actor.Effects.TryApplyEffect(_effect, actor, out _))
                {
                    Fail($"Effect 적용에 실패했습니다: {_effect.effectId}");
                    return;
                }
            }
            Succeed(_effect.effectId);
        }
    }

    [CreateAssetMenu(
        fileName = "AbilityTask_SpawnProjectile",
        menuName = "UPlayGround/Ability/Task/Spawn Projectile")]
    public sealed class SpawnProjectileAbilityTask : AbilityTaskDefinitionSO
    {
        public ProjectileDefinitionSO definition;
        public Vector3 localOffset = new(0f, 1f, 0f);
        public LayerMask hitLayers = -1;

        public override AbilityTaskInstance CreateRuntime(AbilityTaskContext context) =>
            new SpawnProjectileAbilityTaskInstance(
                context,
                definition,
                localOffset,
                hitLayers);
    }

    public sealed class SpawnProjectileAbilityTaskInstance : AbilityTaskInstance
    {
        private readonly ProjectileDefinitionSO _definition;
        private readonly Vector3 _localOffset;
        private readonly LayerMask _hitLayers;

        public SpawnProjectileAbilityTaskInstance(
            AbilityTaskContext context,
            ProjectileDefinitionSO definition,
            Vector3 localOffset,
            LayerMask hitLayers) : base(context)
        {
            _definition = definition;
            _localOffset = localOffset;
            _hitLayers = hitLayers;
        }

        protected override void OnActivate()
        {
            if (_definition == null
                || Svc.Projectile == null
                || !AbilitySystemComponent.TryResolve(
                    Context.Owner.Handle,
                    out AbilitySystemComponent component))
            {
                Fail("Projectile 정의, 서비스 또는 AbilitySystemComponent가 없습니다.");
                return;
            }
            GameActor actor = component.GetComponent<GameActor>();
            if (actor == null)
            {
                Fail("GameActor를 찾을 수 없습니다.");
                return;
            }
            Vector3 direction = actor.transform.forward;
            Vector3 targetPosition = default;
            Transform target = null;
            bool hasTarget = actor.Abilities.TryGetPrimaryTargetReservation(
                out AbilityTargetReservation reservation);
            if (hasTarget)
            {
                direction = reservation.Direction;
                targetPosition = reservation.Position;
                target = reservation.Target != null
                    ? reservation.Target.transform
                    : null;
            }
            Svc.Projectile.Spawn(new ProjectileSpawnRequest
            {
                definition = _definition,
                owner = actor.gameObject,
                origin = actor.transform.TransformPoint(_localOffset),
                logicalOrigin = actor.transform.position,
                direction = direction,
                hasTargetPosition = hasTarget,
                targetPosition = targetPosition,
                targetTransform = target,
                hitLayers = _hitLayers,
                damageScale = 1f,
            });
            Succeed(_definition.name);
        }
    }
}
