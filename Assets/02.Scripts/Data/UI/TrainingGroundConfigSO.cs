using System;
using UnityEngine;
using UPlayGround.Data.Actor;

namespace UPlayGround.Data.UI
{
    /// <summary>훈련장의 이동 목적지와 선택 가능한 상대·배치를 정의한다.</summary>
    [CreateAssetMenu(menuName = "UPlayGround/Training/Ground Config")]
    public sealed class TrainingGroundConfigSO : ScriptableObject
    {
        public string sceneName;
        public string fallbackReturnScene;
        public ActorDefinitionSO[] bosses;
        public string[] weaponLabels;
        public TrainingTier[] tiers;
        public Vector2[] formation;
        public float bossDistance = 12;
        public float singleDistance = 5;
        public Vector2 distanceRange = new(1, 30);
        public Vector2 lateralRange = new(-20, 20);
    }

    /// <summary>같은 난이도에서 선택할 수 있는 무기별 상대 목록.</summary>
    [Serializable]
    public sealed class TrainingTier
    {
        public string label;
        public ActorDefinitionSO[] members;
    }
}
