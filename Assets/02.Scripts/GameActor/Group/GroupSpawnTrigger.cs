using UnityEngine;

namespace UPlayGround.Group
{
    /// <summary>
    /// 플레이어가 트리거에 진입하면 연결된 MonsterGroup을 활성화한다.
    /// BoxCollider(IsTrigger=true)와 함께 배치한다.
    /// </summary>
    public class GroupSpawnTrigger : MonoBehaviour
    {
        [SerializeField] private MonsterGroupController _targetGroup;
        [Tooltip("한 번만 발동할지 여부")]
        [SerializeField] private bool _triggerOnce = true;

        private bool _triggered = false;

        private void OnTriggerEnter(Collider other)
        {
            if (_triggered && _triggerOnce) return;
            if (!other.CompareTag("Player")) return;

            _triggered = true;
            _targetGroup?.Activate();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_targetGroup == null) return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, _targetGroup.transform.position);
        }
#endif
    }
}
