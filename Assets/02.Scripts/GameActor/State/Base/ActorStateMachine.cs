using System;
using System.Collections.Generic;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>실행 전 컨텍스트 구성이 필요한 캐시 상태를 식별한다.</summary>
    public interface IConfigurableState
    {
    }

    /// <summary>캐시 상태가 전이 직전 실행 컨텍스트를 전달받는 계약.</summary>
    public interface IConfigurableState<TContext> : IConfigurableState
    {
        void Configure(in TContext context);
    }

    /// <summary>컨트롤러별 재사용 상태 인스턴스의 등록과 전환을 소유한다.</summary>
    public sealed class ActorStateMachine
    {
        private readonly ActorMovementController _controller;
        private readonly Dictionary<ActorStateId, GameActorState> _states = new();

        public ActorStateMachine(ActorMovementController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
        }

        public void Register(GameActorState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (_states.TryGetValue(state.StateId, out GameActorState registered))
            {
                if (ReferenceEquals(registered, state))
                    return;

                throw new InvalidOperationException(
                    $"[{_controller.GetType().Name}] 상태 ID가 중복 등록되었습니다: " +
                    $"{state.StateId}, 기존={registered.GetType().Name}, " +
                    $"신규={state.GetType().Name}");
            }

            _states.Add(state.StateId, state);
        }

        public GameActorState Get(ActorStateId stateId)
        {
            if (_states.TryGetValue(stateId, out GameActorState state))
                return state;

            throw new InvalidOperationException(
                $"[{_controller.GetType().Name}] 등록되지 않은 캐시 상태입니다: {stateId}");
        }

        public bool TryTransition(ActorStateId stateId)
            => _controller.TryTransitionToState(GetUnconfigured(stateId));

        public bool TryTransition<TContext>(ActorStateId stateId, in TContext context)
        {
            GameActorState state = Get(stateId);
            IConfigurableState<TContext> configurable =
                GetConfigurableState<TContext>(stateId, state);
            return _controller.TryTransitionToConfiguredState(
                state,
                configurable,
                context);
        }

        public void Transition(ActorStateId stateId)
            => _controller.TransitionToState(GetUnconfigured(stateId));

        public void Transition<TContext>(ActorStateId stateId, in TContext context)
        {
            GameActorState state = Get(stateId);
            IConfigurableState<TContext> configurable =
                GetConfigurableState<TContext>(stateId, state);
            _controller.TransitionToConfiguredState(
                state,
                configurable,
                context);
        }

        private GameActorState GetUnconfigured(ActorStateId stateId)
        {
            GameActorState state = Get(stateId);
            if (state is IConfigurableState)
            {
                throw new InvalidOperationException(
                    $"[{_controller.GetType().Name}] {stateId} 상태는 실행 컨텍스트가 필요합니다.");
            }

            return state;
        }

        private IConfigurableState<TContext> GetConfigurableState<TContext>(
            ActorStateId stateId,
            GameActorState state)
        {
            if (state is not IConfigurableState<TContext> configurable)
            {
                throw new InvalidOperationException(
                    $"[{_controller.GetType().Name}] {stateId} 상태는 {typeof(TContext).Name} 설정 계약을 구현하지 않습니다.");
            }

            return configurable;
        }
    }
}
