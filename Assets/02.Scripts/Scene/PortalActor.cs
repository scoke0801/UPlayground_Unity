using System.Collections;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;

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
            {
                motor.SetPositionAndRotation(_destinationPoint.position, _destinationPoint.rotation);
            }
            else
            {
                actor.transform.SetPositionAndRotation(_destinationPoint.position, _destinationPoint.rotation);
            }

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
    }
}