using System.Collections;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;
using UPlayGround.State;

namespace UPlayGround
{
    public enum PortalType
    {
        SceneTransition,  // 씬 전환
        InMapTeleport,    // 맵 내 위치 이동
    }

    /// <summary>
    /// 플레이어가 트리거 영역에 진입하면 씬 전환 또는 맵 내 텔레포트를 수행하는 포탈.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PortalActor : MonoBehaviour
    {
        [SerializeField] private PortalType _portalType = PortalType.SceneTransition;

        [Header("씬 전환 설정")]
        [Tooltip("전환할 씬 이름. SceneName 상수와 일치해야 한다. (PortalType.SceneTransition 전용)")]
        [SerializeField] private string _targetSceneName;

        [Header("맵 내 텔레포트 설정")]
        [Tooltip("이동할 목적지 Transform. (PortalType.InMapTeleport 전용)")]
        [SerializeField] private Transform _destinationPoint;

        [Header("맵 클릭 텔레포트 설정")]
        [Tooltip("맵 UI에서 포탈 아이콘 클릭 시 플레이어가 도착할 위치. 미설정 시 포탈 자체 위치 사용.")]
        [SerializeField] private Transform _mapArrivalPoint;

        [Tooltip("false로 설정하면 플레이어가 진입해도 포탈이 동작하지 않는다.")]
        [SerializeField] private bool _isActive = true;

        [Tooltip("중앙 보스 처치 후 활성화되고, 진입 시 사이클 정산을 먼저 요청하는 탈출 포탈.")]
        [SerializeField] private bool _isCycleExitPortal;

        [Header("맵 UI 동기화")]
        [Tooltip("이 포탈을 맵(RegionInfo)에 동기화할지 여부. 숨김 포탈은 false.")]
        [SerializeField] private bool _showOnMap = true;

        [Tooltip("파스트트래블 시 대상 씬에서 도착할 SceneArrivalPoint.Id. 비우면 씬 기본 스폰.")]
        [SerializeField] private string _targetArrivalId;

        [Tooltip("맵에 표시할 이름. 비우면 오브젝트 이름 사용.")]
        [SerializeField] private string _mapLabel;

        private bool _isActivating;

        // ── 맵 UI 동기화용 읽기 전용 접근자 ──────────────────────
        public PortalType Type            => _portalType;
        public string     TargetSceneName => _targetSceneName;
        public string     TargetArrivalId => _targetArrivalId;
        public bool       ShowOnMap       => _showOnMap;
        public string     MapLabel        => string.IsNullOrEmpty(_mapLabel) ? gameObject.name : _mapLabel;
        public bool       IsCycleExitPortal => _isCycleExitPortal;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isActive || _isActivating) return;

            var actor = other.GetComponent<GameActor>();
            if (actor == null || !actor.HasActorType(ActorType.Player)) return;

            // 정산이 실패하면 씬 전환도 시작하지 않는다. 중복 트리거는 매니저 단계 검사에서 거부된다.
            if (_isCycleExitPortal && !(ActorSvc.CycleExit?.RequestExit() ?? false))
                return;

            switch (_portalType)
            {
                case PortalType.SceneTransition:
                    ActivateSceneTransition();
                    break;
                case PortalType.InMapTeleport:
                    ActivateInMapTeleport(actor);
                    break;
            }
        }

        private void ActivateSceneTransition()
        {
            if (string.IsNullOrEmpty(_targetSceneName)) return;

            _isActivating = true;
            // 걸어서 통과할 때도 맵 파스트트래블과 동일한 도착 지점을 사용해 일관성을 맞춘다.
            ActorSvc.SceneTransition?.LoadScene(_targetSceneName, _targetArrivalId);
        }

        private void ActivateInMapTeleport(GameActor actor)
        {
            if (_destinationPoint == null) return;

            _isActivating = true;

            var motor = actor.ActorController?.Motor;
            if (motor != null)
                motor.SetPositionAndRotation(_destinationPoint.position, _destinationPoint.rotation);
            else
                actor.transform.SetPositionAndRotation(_destinationPoint.position, _destinationPoint.rotation);

            // 이전 애니메이션 이어짐 방지: 상태를 Idle로 강제 전환
            actor.ActorController?.TransitionToState(new PlayerIdleState(actor.ActorController));

            // 카메라 튀는 현상 방지: SmoothDamp 속도를 초기화하고 목적지로 즉시 스냅
            CameraManager.Instance?.SnapToTarget(_destinationPoint.position);

            StartCoroutine(ResetActivatingAfterDelay());
        }

        private IEnumerator ResetActivatingAfterDelay()
        {
            yield return new WaitForSeconds(0.5f);
            _isActivating = false;
        }

        /// <summary>
        /// 플레이어가 이 포탈에서 스폰될 때 사용할 위치/회전을 반환한다.
        /// _mapArrivalPoint가 설정되어 있으면 그 위치를, 없으면 포탈 자체 위치를 반환한다.
        /// </summary>
        public (Vector3 position, Quaternion rotation) GetArrivalPoint()
        {
            if (_mapArrivalPoint != null)
                return (_mapArrivalPoint.position, _mapArrivalPoint.rotation);
            return (transform.position, transform.rotation);
        }

        /// <summary>
        /// 외부(이벤트, 스토리 트리거 등)에서 포탈 활성 상태를 제어한다.
        /// </summary>
        public void SetPortalActive(bool active)
        {
            _isActive = active;
        }

        /// <summary>
        /// 맵 UI에서 포탈 아이콘 클릭 시 _mapArrivalPoint(없으면 포탈 자체 위치)로 플레이어를 이동한다.
        /// </summary>
        public void TeleportPlayerHere(GameActor actor)
        {
            _isActivating = true;

            Vector3    targetPos = _mapArrivalPoint != null ? _mapArrivalPoint.position : transform.position;
            Quaternion targetRot = _mapArrivalPoint != null ? _mapArrivalPoint.rotation : transform.rotation;

            var motor = actor.ActorController?.Motor;
            if (motor != null)
                motor.SetPositionAndRotation(targetPos, targetRot);
            else
                actor.transform.SetPositionAndRotation(targetPos, targetRot);

            actor.ActorController?.TransitionToState(new PlayerIdleState(actor.ActorController));
            CameraManager.Instance?.SnapToTarget(targetPos);

            StartCoroutine(ResetActivatingAfterDelay());
        }
    }
}
