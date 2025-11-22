using UnityEngine;
using UnityEngine.AI;

namespace Game.FSM
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class MonsterBrain : CharacterBrain
    {
        [Header("AI Settings")]
        public Transform Target;       
        public float DetectRange = 10f;
        public float AttackRange = 1.5f;
        public float AttackCooldown = 2.0f;

        private NavMeshAgent _agent;
        private float _lastAttackTime;

        // [해결] 이제 부모의 Awake가 protected이므로 에러 없음
        protected override void Awake()
        {
            base.Awake(); 
            _agent = GetComponent<NavMeshAgent>();
            _agent.updatePosition = false; 
            _agent.updateRotation = false;
        }

        protected override void HandleInput()
        {
            if (Target == null) 
            {
                // [해결] 부모에 SetInputDirection 메서드가 추가되어 에러 없음
                SetInputDirection(Vector3.zero);
                return;
            }

            float distance = Vector3.Distance(transform.position, Target.position);

            if (distance < DetectRange && distance > AttackRange)
            {
                _agent.SetDestination(Target.position);
                Vector3 direction = (_agent.nextPosition - transform.position).normalized;
                
                SetInputDirection(direction); 
                _agent.nextPosition = transform.position; 
            }
            else
            {
                SetInputDirection(Vector3.zero);
            }

            if (distance <= AttackRange)
            {
                if (Time.time - _lastAttackTime > AttackCooldown)
                {
                    // [해결] 부모에 SetAttackInput 메서드가 추가되어 에러 없음
                    SetAttackInput(AttackInputType.Light);
                    _lastAttackTime = Time.time;
                }
            }
        }
    }
}