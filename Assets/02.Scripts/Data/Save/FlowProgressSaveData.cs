using System;
using System.Collections.Generic;

namespace UPlayGround.Data.Save
{
    /// <summary>
    /// FlowGraph 진행 기록. "어떤 진입점/게이트가 이미 발화됐는지"와
    /// "어떤 흐름이 시작·완주됐는지"를 세이브에 남긴다.
    ///
    /// 실행 중인 토큰(코루틴 위치)은 저장하지 않는다. 노드 실행은 대사·컷신·스폰 등
    /// 외부 부수효과를 동반하므로 중간 지점 복원은 안전하게 재현할 수 없다.
    /// 대신 발화/완주 단위로 기록해, 로드 후 1회성 흐름이 다시 재생되지 않게 한다.
    /// </summary>
    [Serializable]
    public class FlowProgressSaveData
    {
        public int dataVersion = 1;

        /// <summary> OncePerSave 정책으로 이미 발화된 진입점·게이트 키 목록. </summary>
        public List<string> firedKeys = new List<string>();

        /// <summary> 진입점별 발화/완주 횟수 기록. </summary>
        public List<FlowEntryProgressSave> entries = new List<FlowEntryProgressSave>();
    }

    /// <summary> 진입점 1개("graphId:nodeId")의 진행 기록. </summary>
    [Serializable]
    public class FlowEntryProgressSave
    {
        public string key;
        public int fireCount;
        public int completeCount;
    }
}
