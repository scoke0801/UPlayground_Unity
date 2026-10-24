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
        [Tooltip("대기 시간의 개체별 무작위 편차 비율")]
        [Range(0f, 1f), SerializeField] private float _patrolWaitVariation = 0.45f;

        [Header("대기 중 주변 살피기")]
        [SerializeField] private bool _enableIdleLook = true;
        [Tooltip("한 방향을 살핀 뒤 다음 방향을 고르기까지의 시간 범위")]
        [SerializeField] private Vector2 _idleLookInterval = new Vector2(3f, 7f);
        [Tooltip("배치 방향을 기준으로 좌우를 살피는 최대 각도")]
        [Range(0f, 90f), SerializeField] private float _idleLookAngle = 40f;
        [Min(0f), SerializeField] private float _idleTurnSpeed = 25f;

        [Header("고정 작업")]
        [Tooltip("배회하지 않을 때 생활 방향으로 돌아온 뒤 재생할 작업 모션")]
        [SerializeField] private UPlayGround.Animation.MotionSetAsset _workMotion;
        [Min(0f), SerializeField] private float _workFacingTolerance = 3f;

        private Vector3 _spawnPosition;
        private Quaternion _homeRotation;
        private Quaternion _idleFacing;
        private Quaternion _idleLookRotation;
        private float _idleLookTimer;

        public float PatrolRadius   => _patrolRadius;
        public float PatrolWaitTime => _patrolWaitTime;
        public bool  EnableWander   => _enableWander;
        public UPlayGround.Animation.MotionSetAsset WorkMotion => _workMotion;
        public Quaternion HomeRotation => _homeRotation;
        public float WorkFacingTolerance => _workFacingTolerance;

        /// <summary>주민들이 같은 프레임에 출발하지 않도록 이번 대기 시간을 선택한다.</summary>
        public float SamplePatrolWaitTime()
        {
            float variation = Mathf.Clamp01(_patrolWaitVariation);
            return Mathf.Max(0f, _patrolWaitTime) * Random.Range(1f - variation, 1f + variation);
        }

        /// <summary>대기 장소의 방향을 기준으로 첫 시선과 행동 시점을 분산한다.</summary>
        public void BeginIdleLook(Quaternion facing)
        {
            _idleFacing = facing;
            ChooseIdleLook();
        }

        /// <summary>기존 NPC 상태 틱에서만 다음 시선을 고른다.</summary>
        public void TickIdleLook(float deltaTime)
        {
            if (!_enableIdleLook || deltaTime <= 0f) return;
            _idleLookTimer -= deltaTime;
            if (_idleLookTimer <= 0f) ChooseIdleLook();
        }

        /// <summary>발이 고정된 대기에서 급회전하지 않도록 회전 속도를 제한한다.</summary>
        public Quaternion RotateIdleLook(Quaternion rotation, float deltaTime)
        {
            if (!_enableIdleLook) return rotation;
            return Quaternion.RotateTowards(rotation, _idleLookRotation,
                Mathf.Max(0f, _idleTurnSpeed) * Mathf.Max(0f, deltaTime));
        }

        private void ChooseIdleLook()
        {
            float angle = Mathf.Clamp(_idleLookAngle, 0f, 90f);
            _idleLookRotation = _idleFacing * Quaternion.Euler(0f, Random.Range(-angle, angle), 0f);
            float minimum = Mathf.Max(0.1f, Mathf.Min(_idleLookInterval.x, _idleLookInterval.y));
            float maximum = Mathf.Max(minimum, Mathf.Max(_idleLookInterval.x, _idleLookInterval.y));
            _idleLookTimer = Random.Range(minimum, maximum);
        }

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
            BeginIdleLook(rotation);
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
