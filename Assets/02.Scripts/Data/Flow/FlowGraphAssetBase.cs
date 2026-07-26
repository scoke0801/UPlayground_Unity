using UnityEngine;

namespace UPlayGround.Data.Flow
{
    /// <summary>
    /// FlowGraph 에셋의 데이터 계층 추상 베이스.
    ///
    /// UPlayGround.Data는 UPlayGround.FlowGraph를 참조하지 않으므로(의존 방향은 FlowGraph → Data),
    /// 데이터 에셋(MapRegionInfoSO 등)이 FlowGraph 에셋을 직접 필드로 들 수 없다.
    /// 이 베이스를 Data에 두고 FlowGraphSO가 상속해, 인스펙터 참조는 데이터 쪽에서 갖고
    /// 실행 해석은 FlowGraph 모듈이 담당하게 한다.
    /// 구현체는 UPlayGround.FlowGraph.FlowGraphSO 하나다.
    /// </summary>
    public abstract class FlowGraphAssetBase : ScriptableObject
    {
        /// <summary>FlowGraphManager 등록·조회용 식별자.</summary>
        public abstract string GraphId { get; }
    }
}
