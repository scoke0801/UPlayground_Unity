using System;
using UnityEngine;

namespace UPlayGround.Data.Codex
{
    [Serializable]
    public struct MonsterCodexBonus
    {
        [Min(0f)] public float maxExpBonus;
        [Min(0f)] public float maxDamageDealtBonus;
        [Min(0f)] public float maxDamageTakenReduce;
    }

    /// <summary>몬스터 한 종의 도감 진행 목표와 최대 보정을 정의한다.</summary>
    [CreateAssetMenu(
        fileName = "MonsterCodexEntry_",
        menuName = "UPlayGround/도감/Monster Codex Entry")]
    public sealed class MonsterCodexEntrySO : ScriptableObject
    {
        [Header("식별")]
        [Tooltip("ActorDatabase의 몬스터 actorId와 일치해야 한다.")]
        public string actorId = "";
        public bool includeInCodex = true;

        [Header("표시")]
        public Sprite portrait;
        [Tooltip("비워두면 ActorDefinitionSO의 표시명을 사용한다.")]
        public string displayNameOverride = "";
        [TextArea(2, 4)]
        [Tooltip("비워두면 ActorDefinitionSO의 설명을 사용한다.")]
        public string descriptionOverride = "";

        [Header("진행")]
        [Min(1)] public int fullRecordKillCount = 10;
        public MonsterCodexBonus bonus;

        public float GetRecordRatio(long killCount) =>
            MonsterCodexCalculator.GetRecordRatio(killCount, fullRecordKillCount);

#if UNITY_EDITOR
        private void OnValidate()
        {
            fullRecordKillCount = Mathf.Max(1, fullRecordKillCount);
        }
#endif
    }
}
