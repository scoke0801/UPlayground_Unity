using System.Collections.Generic;
using UnityEngine;
using Animancer;
using KinematicCharacterController;

namespace Game.FSM
{
    public enum AttackInputType
    {
        None,
        Light,  
        Heavy   
    }

    [RequireComponent(typeof(KinematicCharacterMotor), typeof(CapsuleCollider))]
    [RequireComponent(typeof(AnimancerComponent), typeof(PlayerCharacterController))]
    public class CharacterBrain : MonoBehaviour
    {
        [Header("References")]
        public AnimancerComponent Animancer;
        public KinematicCharacterMotor Motor;
        public PlayerCharacterController Controller;
        public GameObject HitBox;

        [Header("State Config")]
        public StateSO CurrentState;
        public StateSO DefaultState;
        
        [Header("Animation Data")]
        public CharacterAnimationData AnimData;
        
        private Dictionary<string, object> _blackboard = new Dictionary<string, object>();

        // 프로퍼티는 읽기 전용(private set)으로 둡니다.
        public Vector3 InputDirection { get; private set; }
        public Vector3? OverrideLookDirection { get; private set; }
        public AttackInputType AttackInput { get; private set; }
        public bool IsJumpPressed { get; private set; }
        public bool IsDodgePressed { get; private set; }
        public bool IsSprintPressed { get; private set; }


        public bool IsInteractionPressed { get; private set; }
        
        protected virtual void Awake()
        {
            Animancer = GetComponent<AnimancerComponent>();
            Motor = GetComponent<KinematicCharacterMotor>();
            Controller = GetComponent<PlayerCharacterController>();
            
            if (AnimData != null) 
            {
                AnimData.Initialize();
            }
            
            if(HitBox != null) HitBox.SetActive(false);
        }

        protected virtual void Start()
        {
            ChangeState(DefaultState);
        }

        protected virtual void Update()
        {
            HandleInput();
            
            Vector3 lookDirection = OverrideLookDirection ?? InputDirection;
            Controller.SetInputs(InputDirection, lookDirection, IsJumpPressed, IsSprintPressed);
            
            CheckStateTransitions();
            
            CurrentState?.OnUpdate(this);
        }
        protected virtual void FixedUpdate()
        {
            CurrentState?.OnFixedUpdate(this);
        }
        // 자식이 오버라이드할 함수
        protected virtual void HandleInput() { }

        // 자식 클래스(PlayerBrain, MonsterBrain)에서 값을 세팅하기 위한 메서드들 추가
        protected void SetInputDirection(Vector3 dir) => InputDirection = dir;
        public void SetOverrideLookDirection(Vector3? direction) => OverrideLookDirection = direction;
        protected void SetAttackInput(AttackInputType type) => AttackInput = type;
        protected void SetJumpInput(bool active) => IsJumpPressed = active;
        protected void SetDodgeInput(bool active) => IsDodgePressed = active;
        protected void SetSprintInput(bool active) => IsSprintPressed = active;
        protected void SetInteractionInput(bool active) => IsInteractionPressed = active;

        public void ConsumeInput()
        {
            AttackInput = AttackInputType.None;
            SetInteractionInput(false);
        }

        public void ChangeState(StateSO newState)
        {
            if (newState == null) return;
            if (CurrentState == newState) return;

            CurrentState?.OnExit(this);
            SetOverrideLookDirection(null);
            CurrentState = newState;
            CurrentState.OnEnter(this);
        }
        
        // 상태 전환 관리 로직
        private void CheckStateTransitions()
        {
            if (CurrentState == null) return;

            if (false == CurrentState.Transitions.IsNullOrEmpty())
            {
                TryTransition(CurrentState.Transitions);
            }
        }

        private void TryTransition(StateTransition[] transitions)
        {
            // 우선순위(Priority)가 높은 순서대로 정렬하여 검사합니다.
            // 이는 회피(Dodge)와 같은 중요한 전환이 다른 전환보다 항상 우선하도록 보장합니다.
            // System.Array.Sort(transitions, (t1, t2) => t2.Priority.CompareTo(t1.Priority)); 

            foreach (var transition in transitions)
            {
                // 조건 SO를 실행하여 전이할 지 여부를 판단
                if (transition.Condition.CheckCondition(this))
                {
                    // 조건 충족 시 상태 변경
                    ChangeState(transition.TargetState);
                    
                    // 입력 소모가 필요한 전환이라면 입력 데이터를 초기화합니다.
                    if (transition.ConsumeInputOnTransition)
                    {
                        ConsumeInputData(transition.TargetState);
                    }
                    
                    return; // 전환에 성공했으므로 즉시 종료
                }
            }
        }
        
        // 전환 성공 시 관련 입력 데이터를 초기화하는 헬퍼 메서드
        private void ConsumeInputData(StateSO targetState)
        {
            // 어떤 상태로 전환되었는지에 따라 관련된 입력 데이터를 초기화
            // 1. 점프 상태로 전환되면 점프 입력 상태를 false
            if (targetState is JumpStateSO)
            {
                SetJumpInput(false); 
                return;
            }
    
            // 2. 회피 상태로 전환되면 회피 입력 상태를 false
            if (targetState is DodgeStateSO)
            {
                SetDodgeInput(false);
                return;
            }
    
            ConsumeInput();
        }
        
        public bool IsGrounded()
        {
            return Motor.GroundingStatus.IsStableOnGround;
        }

        public void SetHitBox(bool active)
        {
            if(HitBox != null) HitBox.SetActive(active);
        }

        public void SetData<T>(string key, T value)
        {
            if (_blackboard.ContainsKey(key)) _blackboard[key] = value;
            else _blackboard.Add(key, value);
        }

        public bool HasData(string key) => _blackboard.ContainsKey(key);

        public void ClearData(string key)
        {
            _blackboard.Remove(key);
        }
        public T GetData<T>(string key, T defaultValue = default)
        {
            if (_blackboard.TryGetValue(key, out object value))
            {
                if (value is T tValue) return tValue;
            }
            return defaultValue;
        }
    }
}