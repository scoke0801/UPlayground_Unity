using UnityEngine;
using Random = UnityEngine.Random;

namespace UPlayGround.Components
{
    /// <summary>
    /// NPC 행동 설정.
    /// EnemyAIController과 달리 전투 로직 없이 배회·대기 데이터만 담습니다.
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

        [Header("고정 작업")]
        [Tooltip("배회하지 않을 때 생활 방향으로 돌아온 뒤 재생할 작업 모션")]
        [SerializeField] private UPlayGround.Animation.MotionSetAsset _workMotion;
        [Min(0f), SerializeField] private float _workFacingTolerance = 3f;

        private Vector3 _spawnPosition;
        private Quaternion _homeRotation;

        public float PatrolRadius   => _patrolRadius;
        public float PatrolWaitTime => _patrolWaitTime;
        public bool  EnableWander   => _enableWander;
        public UPlayGround.Animation.MotionSetAsset WorkMotion => _workMotion;
        public Quaternion HomeRotation => _homeRotation;
        public float WorkFacingTolerance => _workFacingTolerance;

        private void Awake()
        {
            _spawnPosition = transform.position;
            _homeRotation = transform.rotation;
        }

        /// <summary>저장된 생활 배치의 배회 중심과 작업 방향을 함께 복원한다.</summary>
        public void SetHomePose(Vector3 position, Quaternion rotation)
        {
            _spawnPosition = position;
            _homeRotation = rotation;
        }

        /// <summary>생활 배치 복원 시 그 자리에서 수행할 작업을 바꾼다.</summary>
        public void SetWorkMotion(UPlayGround.Animation.MotionSetAsset motion) => _workMotion = motion;

        /// <summary>지역 스토리에서 기존 NPC를 고정 배치로 재사용할 때 호출한다.</summary>
        public void ConfigureStationary(Vector3 position)
        {
            _enableWander = false;
            _spawnPosition = position;
        }

        public Vector3 GetRandomWanderPoint()
        {
            Vector2 circle = Random.insideUnitCircle * _patrolRadius;
            return _spawnPosition + new Vector3(circle.x, 0f, circle.y);
        }
    }
}
