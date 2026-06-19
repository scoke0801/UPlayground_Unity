using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Data.Party
{
    /// <summary>
    /// 레벨업에 필요한 경험치(EXP) 곡선.
    /// 여러 캐릭터가 한 곡선을 공유할 수 있으며, 필요 시 캐릭터별로 분리한다.
    /// PartyMemberGrowthSO.levelCurve로 참조된다.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelCurve_", menuName = "UPlayGround/파티/Level Curve")]
    public class LevelCurveSO : ScriptableObject
    {
        [Header("공식 기반 (explicitTable이 비어 있을 때 사용)")]
        [Tooltip("required(L) = round(baseExp * pow(L, exponent))")]
        [Min(1)] public int baseExp = 100;

        [Min(1f)] public float exponent = 1.5f;

        [Header("명시 테이블 (공식과 택1)")]
        [Tooltip("인덱스 i = 레벨 (i+1) → (i+2) 로 가는 데 필요한 경험치. 비어 있으면 공식 사용.")]
        public List<int> explicitTable = new();

        /// <summary>
        /// 레벨 L 에서 L+1 로 가는 데 필요한 경험치.
        /// 명시 테이블이 있으면 우선 사용하고, 범위를 벗어나면 마지막 값으로 클램프한다.
        /// </summary>
        public long GetRequiredExp(int level)
        {
            int clamped = Mathf.Max(1, level);

            if (explicitTable != null && explicitTable.Count > 0)
            {
                int index = Mathf.Clamp(clamped - 1, 0, explicitTable.Count - 1);
                return Mathf.Max(1, explicitTable[index]);
            }

            double required = baseExp * System.Math.Pow(clamped, exponent);
            return (long)System.Math.Max(1.0, System.Math.Round(required, System.MidpointRounding.AwayFromZero));
        }
    }
}
