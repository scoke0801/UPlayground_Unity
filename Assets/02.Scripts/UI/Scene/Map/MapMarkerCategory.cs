/// <summary>
/// 맵 범례/필터에서 표시 토글할 수 있는 마커 카테고리.
/// 현재 아이콘 시스템이 구분 가능한 그룹 단위(플레이어/퀘스트목표/적/NPC/정적마커/유저마커)로 정의한다.
/// (적 일반·강함, 상인·퀘스트NPC 등 세분화는 마커 데이터가 더 풍부해지면 확장)
/// </summary>
public enum MapMarkerCategory
{
    Player       = 0,  // 플레이어
    QuestTarget  = 1,  // 퀘스트 목표
    Enemy        = 2,  // 적
    Npc          = 3,  // NPC / 상인 / 채집 등 액터
    StaticMarker = 4,  // 포탈 / 거점 / 던전 입구 등 정적 마커
    UserMarker   = 5,  // 유저 마커
}
