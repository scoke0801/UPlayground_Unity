using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;

namespace UPlayGround
{
    /// <summary>
    /// 플레이어가 트리거 영역에 진입하면 지정된 씬으로 전환하는 포탈.
    /// Loading 씬을 경유하는 SceneManager.LoadScene()을 사용한다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PortalActor : MonoBehaviour
    {
        [Tooltip("전환할 씬 이름. SceneName 상수와 일치해야 한다.")]
        [SerializeField] private string _targetSceneName;

        [Tooltip("false로 설정하면 플레이어가 진입해도 씬 전환이 일어나지 않는다.")]
        [SerializeField] private bool _isActive = true;

        private bool _isActivating;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isActive || _isActivating) return;
            if (string.IsNullOrEmpty(_targetSceneName)) return;

            var actor = other.GetComponent<GameActor>();
            if (actor == null || !actor.HasActorType(ActorType.Player)) return;

            _isActivating = true;
            SceneManager.Instance.LoadScene(_targetSceneName);
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
