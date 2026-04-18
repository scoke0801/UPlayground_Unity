using UnityEngine;

/// <summary>
/// 씬에 배치해 미니맵 퀘스트 마커 위치를 등록하는 컴포넌트.
///
/// ■ 사용 방법
///   1. 목표 지점·NPC·트리거 오브젝트에 이 컴포넌트를 추가한다.
///   2. LocationId를 QuestObjectiveData.targetStringId 와 동일하게 설정한다.
///   3. 씬이 로드되면 자동으로 "MinimapMarkerRegistry"에 등록된다.
///
/// ■ ItemDeliver 목표의 NPC 마커
///   LocationId 를 "npc_{npcId}" 형식으로 설정하면 UI_Minimap이 자동으로 연결합니다.
///   예) npcId = 101 → LocationId = "npc_101"
/// </summary>
public class MinimapMarkerRegistrar : MonoBehaviour
{
    [Tooltip("QuestObjectiveData.targetStringId 와 동일한 값으로 설정.\nItemDeliver NPC는 'npc_{npcId}' 형식 사용.")]
    [SerializeField] private string _locationId;

    [Tooltip("미니맵에 표시할 마커 타입")]
    [SerializeField] private MinimapMarkerType _markerType = MinimapMarkerType.QuestTarget;

    public string           LocationId  => _locationId;
    public MinimapMarkerType MarkerType  => _markerType;
    public Vector3          WorldPosition => transform.position;

    private void Awake()     => MinimapMarkerRegistry.Register(this);
    private void OnDestroy() => MinimapMarkerRegistry.Unregister(this);

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = _markerType switch
        {
            MinimapMarkerType.QuestTarget => new Color(1f, 0.9f, 0f,  0.8f),
            MinimapMarkerType.Town        => new Color(0.4f, 1f,  0.4f, 0.8f),
            MinimapMarkerType.Portal      => new Color(0.6f, 0.4f, 1f, 0.8f),
            MinimapMarkerType.Npc         => new Color(0.4f, 0.8f, 1f, 0.8f),
            _                             => new Color(1f,   1f,  1f,  0.8f),
        };

        Gizmos.DrawWireSphere(transform.position, 1f);
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f,
            $"[{_markerType}] {_locationId}");
    }
#endif
}

public enum MinimapMarkerType
{
    QuestTarget, // 퀘스트 목표 지점 ("!" 아이콘, 활성 퀘스트 조건 충족 시에만 표시)
    Custom,      // 커스텀 정적 마커 (config의 customMarker 아이콘)
    Town,        // 마을 입구 / 거점 마커
    Portal,      // 포탈 / 워프 지점 마커
    Npc,         // 고정 NPC 마커 (액터 시스템과 별개로 항상 표시)
}
