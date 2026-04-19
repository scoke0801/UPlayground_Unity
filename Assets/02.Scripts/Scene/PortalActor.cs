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

        private bool _isActivating;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isActive || _isActivating) return;

            var actor = other.GetComponent<GameActor>();
            if (actor == null || !actor.HasActorType(ActorType.Player)) return;

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
            SceneManager.Instance.LoadScene(_targetSceneName);
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
