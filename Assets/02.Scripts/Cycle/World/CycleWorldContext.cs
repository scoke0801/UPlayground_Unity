using UnityEngine;
using UPlayGround.Data.Cycle;
using UPlayGround.Manager;

namespace UPlayGround.Cycle
{
    /// <summary>씬의 사이클 월드 설정을 런타임 매니저에 연결하는 저작 컴포넌트.</summary>
    public sealed class CycleWorldContext : MonoBehaviour
    {
        [SerializeField] private CycleWorldConfigSO _config;
        [SerializeField] private CycleConfigSO _runConfig;
        [SerializeField] private RemainsActor _remainsPrefab;
        public CycleWorldConfigSO Config => _config;

        private void Start()
        {
            // 저장 데이터가 먼저 복원된 경우에도 씬에 연결된 실제 설정 에셋으로 런타임 참조를 되살린다.
            if (_runConfig != null) CycleRunManager.Instance?.SetConfig(_runConfig, allowActiveRestore: true);
            CycleRemainsManager.Instance?.Configure(_remainsPrefab);
            CycleRunManager.Instance?.ConfigureWorldContext(this);
        }
    }
}
