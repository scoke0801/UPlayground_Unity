using System;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data.Cycle;

namespace UPlayGround.Editor.P09Builder
{
    /// <summary>
    /// P09 몬스터를 Cycle 보스 풀과 BossAssist 데이터에 연결하기 위한 빌드 설정.
    /// 파티 캐릭터 해금(recruitableAs)과는 별개의 경로다.
    /// </summary>
    [Serializable]
    public sealed class CycleBuildSettings
    {
        public bool isCycleBoss;
        public CycleWorldConfigSO worldConfig;
        public bool registerAsOuterBoss = true;
        public bool registerAsCentralBoss;

        public bool createOrUpdateBossAssist;
        public BossAssistDatabaseSO assistDatabase;
        public string assistId = string.Empty;
        public BossAssistRole role = BossAssistRole.Damage;
        public Sprite icon;
        public GameObject assistPrefab;
        public MotionSetAsset motionSet;
        public float cooldownSeconds = 45f;
        public float maxExecutionSeconds = 5f;
        public AssistPlacementPolicy placementPolicy = AssistPlacementPolicy.NearPlayer;
        public Vector3 placementOffset = new(1.5f, 0f, 1.5f);
        public bool requiresTarget = true;
        public bool recruitableFromCentralBoss;
        public float healAmount;
    }
}
