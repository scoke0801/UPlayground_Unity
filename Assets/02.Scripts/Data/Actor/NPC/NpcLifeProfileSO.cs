using UnityEngine;

namespace UPlayGround.Data.Actor.NPC
{
    /// <summary>주민 역할의 생활 판단 주기와 교류·관찰 범위를 정의한다.</summary>
    [CreateAssetMenu(menuName = "UPlayGround/NPC/Life Profile")]
    public sealed class NpcLifeProfileSO : ScriptableObject
    {
        public string roleId;
        [Min(0.1f)] public float decisionInterval = 0.5f;
        [Min(0f)] public float initialSpread = 3f;
        public bool canSocialize;
        [Min(0.1f)] public float socialSearchInterval = 2f;
        [Min(0.1f)] public float socialRadius = 8f;
        [Min(0.1f)] public float socialArrivalTimeout = 15f;
        [Min(0.1f)] public float socialDuration = 4f;
        [Min(0.1f)] public float socialCooldown = 45f;
        [Min(0f)] public float noticeRadius = 10f;
        [Min(0f)] public float noticeDelay = 0.6f;
        [Min(0.1f)] public float noticeSeconds = 2f;
        [Min(0.1f)] public float noticeCooldown = 20f;
        [Range(0f, 1f)] public float noticeProbability = 0.5f;
    }
}
