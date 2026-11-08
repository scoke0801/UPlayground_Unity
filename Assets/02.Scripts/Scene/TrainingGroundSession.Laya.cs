#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UPlayGround.Data.Actor;
using UnityEngine;

namespace UPlayGround.Training
{
    public sealed partial class TrainingGroundSession
    {
        /// <summary>기존 배치·그룹·실패 정리 경로를 사용해 정의로 지정한 상대 한 마리만 생성한다.</summary>
        public bool TryStartSingleDefinition(ActorDefinitionSO definition, float distance, out string message)
        {
            message = "기존 대련을 재시작한 뒤 실험을 시작하세요.";
            if (HasEncounter || _practiceCombat != null) return false;
            return TrySpawnDefinitions(new[] { definition }, new[] { new Vector2(0, distance) }, out message);
        }
    }
}
#endif
