using UnityEngine;
using UPlayGround;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Quest;
using UPlayGround.Manager;

/// <summary>
/// 플레이어가 트리거 영역에 진입하면 지정한 퀘스트를 수락한다.
/// Collider(Is Trigger)와 함께 씬에 배치해서 사용한다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class QuestTriggerZone : MonoBehaviour
{
    [Header("퀘스트")]
    [SerializeField] private QuestIdType _questId = QuestIdType.None;

    [Header("위치 목표")]
    [Tooltip("비워두지 않으면 진입 시 ReachLocation 목표도 함께 갱신한다.")]
    [SerializeField] private string _locationId;

    [Header("트리거 설정")]
    [SerializeField] private bool _triggerOnce = true;
    [SerializeField] private bool _disableColliderAfterTrigger = false;

    private bool _triggered;
    private Collider _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        if (_collider != null)
            _collider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered && _triggerOnce) return;

        var actor = other.GetComponent<GameActor>();
        if (actor == null || !actor.HasActorType(ActorType.Player)) return;

        var questManager = QuestManager.Instance;
        if (questManager == null) return;

        bool handled = false;

        if (_questId != QuestIdType.None)
            handled |= questManager.AcceptQuest(_questId);

        if (!string.IsNullOrEmpty(_locationId))
        {
            questManager.NotifyLocationReached(_locationId);
            handled = true;
        }

        if (!handled) return;

        // DB 로드 전에는 보류 큐로만 처리됨 — 재진입으로 재시도 가능하도록 트리거 상태를 잠그지 않는다.
        if (!questManager.IsDBLoaded) return;

        _triggered = true;
        if (_disableColliderAfterTrigger && _collider != null)
            _collider.enabled = false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        var triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
            triggerCollider.isTrigger = true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.35f, 0.8f, 1f, 0.8f);
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
#endif
}
