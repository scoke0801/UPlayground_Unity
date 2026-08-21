namespace UPlayGround.Data.Quest
{
    /// <summary>
    /// 퀘스트 목표가 가리키는 지점의 마커 위치 ID를 해석한다.
    /// 미니맵·전체 지도·월드 마커가 같은 규칙을 보게 하려고 한 곳에 둔다.
    /// </summary>
    public static class QuestObjectiveMarker
    {
        /// <summary>
        /// 목표를 표시할 마커 위치 ID. 마커를 만들지 않는 목표는 null을 돌려준다.
        /// 저작에서 <see cref="QuestObjectiveData.markerLocationId"/>를 지정하면 타입 기본 규칙보다 우선한다.
        /// </summary>
        public static string ResolveLocationId(QuestObjectiveData objective)
        {
            if (objective == null)
                return null;

            if (!string.IsNullOrWhiteSpace(objective.markerLocationId))
                return objective.markerLocationId;

            return objective.type switch
            {
                QuestObjectiveType.ReachLocation => objective.targetStringId,
                QuestObjectiveType.ItemDeliver   => $"npc_{objective.npcId}",
                _                                => null,
            };
        }
    }
}
