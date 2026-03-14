using UnityEngine;
using Random = UnityEngine.Random;

namespace UPlayGround.Component
{
    /// <summary>
    /// NPC 행동 설정.
    /// EnemyBrain과 달리 전투 로직 없이 배회·대기 데이터만 담습니다.
    /// </summary>
    public class NpcBrain : MonoBehaviour
    {
        [Header("배회 설정")]
        [Tooltip("스폰 위치 기준 배회 반경")]
        [SerializeField] private float _patrolRadius = 5f;
        [Tooltip("목적지 도착 후 대기 시간 (초)")]
        [SerializeField] private float _patrolWaitTime = 3f;
        [Tooltip("false면 Idle 상태만 유지하고 배회하지 않음")]
        [SerializeField] private bool _enableWander = true;

        private Vector3 _spawnPosition;

        public float PatrolRadius   => _patrolRadius;
        public float PatrolWaitTime => _patrolWaitTime;
        public bool  EnableWander   => _enableWander;

        private void Awake()
        {
            _spawnPosition = transform.position;
        }

        public Vector3 GetRandomWanderPoint()
        {
            Vector2 circle = Random.insideUnitCircle * _patrolRadius;
            return _spawnPosition + new Vector3(circle.x, 0f, circle.y);
        }
    }
}
