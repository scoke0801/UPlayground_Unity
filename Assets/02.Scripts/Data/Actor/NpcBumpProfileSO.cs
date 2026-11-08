using System;
using UnityEngine;

namespace UPlayGround.Data.Actor
{
    /// <summary>플레이어에게 부딪힌 NPC의 몸 부위. 접촉점의 로컬 위치로 판정한다.</summary>
    public enum NpcBumpPart
    {
        Chest,
        Back,
        LeftShoulder,
        RightShoulder,
        Hip,
    }

    /// <summary>충돌 속도와 NPC 상태로 정해지는 반응 강도. 값이 클수록 크게 밀린다.</summary>
    public enum NpcBumpTier
    {
        None = 0,
        Lean = 1,
        Stagger = 2,
        Shove = 3,
    }

    /// <summary>플레이어에게 부딪혔을 때 NPC가 보이는 반응의 수치 묶음(속도 구간·밀림 거리·부위별 가중).</summary>
    [CreateAssetMenu(fileName = "NpcBump_", menuName = "UPlayGround/액터/NPC 충돌 반응")]
    public sealed class NpcBumpProfileSO : ScriptableObject
    {
        /// <summary>부위별 상체 반응 가중. 같은 충돌이라도 어깨를 맞으면 더 크게 돌아간다.</summary>
        [Serializable]
        public struct PartResponse
        {
            public NpcBumpPart part;
            [Tooltip("접촉 방향의 반대로 상체가 기우는 각도 배율.")]
            [Min(0f)] public float leanWeight;
            [Tooltip("접촉한 쪽 어깨가 뒤로 밀리며 몸통이 비틀어지는 각도 배율.")]
            [Min(0f)] public float twistWeight;
        }

        [Header("속도 구간 (m/s) — 플레이어 수평 속도")]
        [Tooltip("이 속도 미만이면 반응하지 않는다. 걷다가 스치는 정도는 무시한다.")]
        [Min(0f)] public float minimumSpeed = 1.5f;

        [Tooltip("이 속도 이상이면 휘청(Stagger)한다.")]
        [Min(0f)] public float staggerSpeed = 4f;

        [Tooltip("이 속도 이상이면 크게 밀려난다(Shove). 대시 수준.")]
        [Min(0f)] public float shoveSpeed = 7f;

        [Header("밀림")]
        [Tooltip("Stagger 단계에서 밀려나는 거리(m).")]
        [Min(0f)] public float staggerDistance = 0.35f;

        [Tooltip("Shove 단계에서 밀려나는 거리(m).")]
        [Min(0f)] public float shoveDistance = 0.8f;

        [Tooltip("밀림 속도의 지수 감쇠 계수. 밀림 거리 = 초기 속도 / 감쇠이므로 값이 클수록 짧고 빠르게 멈춘다.")]
        [Min(0.5f)] public float pushDrag = 8f;

        [Tooltip("밀려난 뒤 자세를 회복하기까지의 시간(초).")]
        [Min(0.1f)] public float recoverSeconds = 0.9f;

        [Tooltip("상체가 최대로 기울기까지 걸리는 시간(초). 짧을수록 타격감이 강하다.")]
        [Min(0.02f)] public float attackSeconds = 0.12f;

        [Tooltip("같은 NPC가 다시 반응하기까지의 최소 시간(초). 연속으로 부딪혀도 한 번만 반응한다.")]
        [Min(0f)] public float cooldownSeconds = 2f;

        [Header("상체 반응 (도)")]
        [Tooltip("가중 1일 때 Lean 단계의 상체 기울기. 높은 단계는 배율이 올라간다.")]
        [Min(0f)] public float baseLeanDegrees = 8f;

        [Tooltip("단계별 상체 반응 배율. 인덱스는 Lean(0), Stagger(1), Shove(2).")]
        public float[] tierAngleScale = { 1f, 1.8f, 2.6f };

        [Header("부위 판정 (NPC 로컬)")]
        [Tooltip("이 높이 비율(0=발, 1=머리) 아래에 닿으면 골반으로 본다.")]
        [Range(0.05f, 0.7f)] public float hipHeightRatio = 0.5f;

        [Tooltip("몸 중심에서 이 거리(m) 이상 옆에 닿으면 어깨로 본다.")]
        [Min(0f)] public float shoulderOffset = 0.22f;

        [Header("부위별 가중")]
        public PartResponse[] partResponses =
        {
            new PartResponse { part = NpcBumpPart.Chest, leanWeight = 1f, twistWeight = 0.3f },
            new PartResponse { part = NpcBumpPart.Back, leanWeight = 1f, twistWeight = 0.3f },
            new PartResponse { part = NpcBumpPart.LeftShoulder, leanWeight = 0.8f, twistWeight = 1.4f },
            new PartResponse { part = NpcBumpPart.RightShoulder, leanWeight = 0.8f, twistWeight = 1.4f },
            new PartResponse { part = NpcBumpPart.Hip, leanWeight = 0.6f, twistWeight = 0.5f },
        };

        /// <summary>부위의 가중을 찾는다. 항목이 없으면 가중 1인 기본값을 돌려준다.</summary>
        public PartResponse GetPartResponse(NpcBumpPart part)
        {
            for (int i = 0; i < partResponses.Length; i++)
                if (partResponses[i].part == part) return partResponses[i];
            return new PartResponse { part = part, leanWeight = 1f, twistWeight = 1f };
        }

        /// <summary>단계별 상체 반응 배율. 배열이 모자라면 마지막 값을 쓴다.</summary>
        public float GetTierAngleScale(NpcBumpTier tier)
        {
            if (tier == NpcBumpTier.None || tierAngleScale == null || tierAngleScale.Length == 0) return 0f;
            int index = Mathf.Clamp((int)tier - 1, 0, tierAngleScale.Length - 1);
            return tierAngleScale[index];
        }

        private void OnValidate()
        {
            staggerSpeed = Mathf.Max(staggerSpeed, minimumSpeed);
            shoveSpeed = Mathf.Max(shoveSpeed, staggerSpeed);
            shoveDistance = Mathf.Max(shoveDistance, staggerDistance);
            attackSeconds = Mathf.Min(attackSeconds, recoverSeconds * 0.5f);
        }
    }
}
