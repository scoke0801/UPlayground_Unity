namespace UPlayGround.Data.Quest
{
    /// <summary>
    /// 목표 마커가 플레이어에게 알려야 할 "가서 무엇을 하는가".
    /// 같은 <see cref="QuestObjectiveType"/>이라도 실제 행동이 다른 목표가 있어서
    /// (예: 서사 이벤트 목표가 단서 조사일 수도, 전투 조우일 수도 있다)
    /// 아이콘 성격만 목표마다 따로 저작한다. 진행 판정에는 관여하지 않는다.
    /// </summary>
    public enum QuestMarkerIntent
    {
        /// <summary>목표 타입의 기본 아이콘을 쓴다.</summary>
        Auto = 0,

        /// <summary>대화·전달처럼 사람을 만나는 목표.</summary>
        Talk = 1,

        /// <summary>전투가 벌어지는 목표.</summary>
        Combat = 2,

        /// <summary>단서 조사·지점 탐색처럼 살펴보는 목표.</summary>
        Explore = 3,
    }
}
