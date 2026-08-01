using System;
using System.Collections.Generic;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>캐시 상태가 전이 직전 실행 컨텍스트를 전달받는 계약.</summary>
    public interface IConfigurableState<TContext>
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

            _states[state.StateId] = state;
        }

        public GameActorState Get(ActorStateId stateId)
        {
            if (_states.TryGetValue(stateId, out GameActorState state))
                return state;

            throw new InvalidOperationException(
                $"[{_controller.GetType().Name}] 등록되지 않은 캐시 상태입니다: {stateId}");
        }

        public bool TryTransition(ActorStateId stateId)
            => _controller.TryTransitionToState(Get(stateId));

        public bool TryTransition<TContext>(ActorStateId stateId, in TContext context)
        {
            GameActorState state = Configure(stateId, context);
            return _controller.TryTransitionToState(state);
        }

        public void Transition(ActorStateId stateId)
            => _controller.TransitionToState(Get(stateId));

        public void Transition<TContext>(ActorStateId stateId, in TContext context)
            => _controller.TransitionToState(Configure(stateId, context));

        private GameActorState Configure<TContext>(ActorStateId stateId, in TContext context)
        {
            GameActorState state = Get(stateId);
            if (state is not IConfigurableState<TContext> configurable)
            {
                throw new InvalidOperationException(
                    $"[{_controller.GetType().Name}] {stateId} 상태는 {typeof(TContext).Name} 설정 계약을 구현하지 않습니다.");
            }

            configurable.Configure(context);
            return state;
        }
    }
}
