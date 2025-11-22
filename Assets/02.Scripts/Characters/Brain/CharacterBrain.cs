using System.Collections.Generic;
using UnityEngine;
using Animancer;

namespace Game.FSM
{
    public enum AttackInputType
    {
        None,
        Light,  
        Heavy   
    }

    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    [RequireComponent(typeof(AnimancerComponent), typeof(CharacterAnimationData))]
    public class CharacterBrain : MonoBehaviour
    {
        [Header("References")]
        public AnimancerComponent Animancer;
        public Rigidbody Rb;
        public GameObject HitBox;

        [Header("Physics Settings")]
        public LayerMask GroundLayer;
        public float GroundCheckRadius = 0.2f;

        [Header("State Config")]
        public StateSO CurrentState;
        public StateSO DefaultState;
        
        [Header("Animation Data")]
        public CharacterAnimationData AnimData;
        
        private Dictionary<string, object> _blackboard = new Dictionary<string, object>();

        // 프로퍼티는 읽기 전용(private set)으로 둡니다.
        public Vector3 InputDirection { get; private set; }
        public AttackInputType CurrentInput { get; private set; }
        public bool IsJumpPressed { get; private set; }
        public bool IsDodgePressed { get; private set; }

        // [수정 1] private -> protected virtual로 변경하여 자식이 호출 가능하게 함
        protected virtual void Awake()
        {
            Animancer = GetComponent<AnimancerComponent>();
            Rb = GetComponent<Rigidbody>();
            
            AnimData = GetComponent<CharacterAnimationData>();
            if (AnimData != null) 
            {
                AnimData.Initialize();
            }
            
            if(HitBox != null) HitBox.SetActive(false);

            SetupRigidbody();
        }

        private void SetupRigidbody()
        {
            Rb.constraints = RigidbodyConstraints.FreezeRotation;
            Rb.useGravity = true;
            Rb.interpolation = RigidbodyInterpolation.Interpolate;
            Rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        protected virtual void Start()
        {
            ChangeState(DefaultState);
        }

        protected virtual void Update()
        {
            HandleInput();
            
            // 1. 상태 전환을 먼저 체크합니다. (가장 중요)
            CheckStateTransitions();
            
            // 2. 전환이 일어나지 않았을 때만 현재 상태의 Update 로직을 실행합니다.
            CurrentState?.OnUpdate(this);
        }

        protected virtual void FixedUpdate()
        {
            CurrentState?.OnFixedUpdate(this);
        }

        // 자식이 오버라이드할 함수
        protected virtual void HandleInput() { }

        // [수정 2] 자식 클래스(PlayerBrain, MonsterBrain)에서 값을 세팅하기 위한 메서드들 추가
        protected void SetInputDirection(Vector3 dir) => InputDirection = dir;
        protected void SetAttackInput(AttackInputType type) => CurrentInput = type;
        protected void SetJumpInput(bool active) => IsJumpPressed = active;
        protected void SetDodgeInput(bool active) => IsDodgePressed = active;

        public void ConsumeInput()
        {
            CurrentInput = AttackInputType.None;
        }

        public void ChangeState(StateSO newState)
        {
            if (newState == null) return;
            if (CurrentState == newState) return;

            CurrentState?.OnExit(this);
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
                // 조건 SO를 실행하여 참/거짓을 판별합니다. (데이터 기반 체크)
                if (transition.Condition.CheckCondition(this))
                {
                    // 조건 충족 시 상태 변경
                    ChangeState(transition.TargetState);
                    
                    // 입력 소모가 필요한 전환이라면 입력 데이터를 초기화합니다.
                    if (transition.ConsumeInputOnTransition)
                    {
                        ConsumeInputData(transition.TargetState);
                    }
                    // 상태를 변경했으므로, 사용된 입력이나 데이터는 여기서 소모하는 것이 좋습니다.
                    // 예: 입력 기반 조건(IsDodgePressedCondition)이라면, ChangeState 직후 해당 bool 값을 false로 리셋해야 합니다.
                    // (이 리셋 로직은 조건 SO 내부에서 처리하거나, ChangeState 직후 특정 로직을 호출하도록 설계 가능)
            
                    return; // 전환에 성공했으므로 즉시 종료
                }
            }
        }
        
        // 전환 성공 시 관련 입력 데이터를 초기화하는 헬퍼 메서드
        private void ConsumeInputData(StateSO targetState)
        {
            // 어떤 상태로 전환되었는지에 따라 관련된 입력 데이터를 초기화
    
            // 1. 점프 상태로 전환되면 점프 입력 상태를 false로 만듭니다.
            if (targetState is JumpStateSO)
            {
                SetJumpInput(false); 
            }
    
            // 2. 회피 상태로 전환되면 회피 입력 상태를 false로 만듭니다.
            if (targetState is DodgeStateSO)
            {
                SetDodgeInput(false);
            }
    
            // 3. 공격 상태로 전환되면 공격 입력 상태를 None으로 만듭니다.
            // 이는 CharacterBrain의 ConsumeInput() 메서드를 사용해도 됩니다.
            if (targetState is ComboAttackStateSO)
            {
                ConsumeInput(); // CurrentInput = AttackInputType.None;
            }
        }
        
        public bool IsGrounded()
        {
            return Physics.CheckSphere(transform.position + Vector3.up * 0.1f, GroundCheckRadius, GroundLayer);
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

        public T GetData<T>(string key, T defaultValue = default)
        {
            if (_blackboard.TryGetValue(key, out object value))
            {
                if (value is T tValue) return tValue;
            }
            return defaultValue;
        }
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.1f, GroundCheckRadius);
        }
    }
}