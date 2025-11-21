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

    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider), typeof(AnimancerComponent))]
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