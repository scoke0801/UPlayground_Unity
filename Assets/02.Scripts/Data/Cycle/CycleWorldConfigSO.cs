using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Data.Cycle
{
    [CreateAssetMenu(fileName = "CycleWorldConfig", menuName = "UPlayGround/사이클/월드 설정")]
    public sealed class CycleWorldConfigSO : ScriptableObject
    {
        public string mapId;
        public List<string> outerBossActorIds = new();
        public List<string> centralBossActorIds = new();
        [Min(1)] public int outerBossCount = 3;
        [Min(1)] public int maxSameSectorBossCount = 1;
        [Min(1)] public int baseMonsterLevel = 1;

        public bool Validate(out string error)
        {
            if (string.IsNullOrWhiteSpace(mapId)) { error = "mapId가 비어 있습니다."; return false; }
            if (outerBossActorIds == null || outerBossActorIds.Count == 0) { error = "외곽 보스 풀이 비어 있습니다."; return false; }
            if (centralBossActorIds == null || centralBossActorIds.Count == 0) { error = "중앙 보스 풀이 비어 있습니다."; return false; }
            if (outerBossCount <= 0 || maxSameSectorBossCount <= 0) { error = "보스 수와 섹터 제한은 1 이상이어야 합니다."; return false; }
            error = null;
            return true;
        }
    }
}
