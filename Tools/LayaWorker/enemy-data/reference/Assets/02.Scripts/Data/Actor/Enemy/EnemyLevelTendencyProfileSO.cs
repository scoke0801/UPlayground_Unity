using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Data.Enemy
{
    /// <summary>
    /// 몬스터 레벨 구간별로 Intent 성향 확률과 행동 템포에 곱할 배율을 정의한다.
    /// 같은 BT·같은 공격 세트를 여러 레벨이 공유할 때, 높은 레벨일수록 플레이어의 빈틈을 더 자주 읽고
    /// 더 빨리 다음 행동으로 넘어가게 만드는 데이터 레버다. 행동의 종류(레퍼토리)는 바꾸지 않는다 —
    /// 레퍼토리 성장은 공격 데이터의 requiredLevel이 담당한다.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyLevelTendency", menuName = "UPlayGround/적/Level Tendency")]
    public sealed class EnemyLevelTendencyProfileSO : ScriptableObject
    {
        /// <summary>minLevel 이상인 몬스터에 적용할 배율 묶음.</summary>
        [Serializable]
        public sealed class LevelBand
        {
            [Tooltip("이 레벨 이상부터 적용한다. 레벨이 여러 구간에 걸치면 minLevel이 가장 큰 구간을 쓴다.")]
            [Min(1)] public int minLevel = 1;

            [Header("Intent 성향 배율 (결과는 0~1로 제한)")]
            [Tooltip("공격 의도 점수에 쓰이는 공격성 배율")]
            [Range(0f, 2f)] public float aggressionScale = 1f;
            [Tooltip("플레이어 행동에 반응할 확률 배율")]
            [Range(0f, 2f)] public float reactionScale = 1f;
            [Tooltip("반격 의도 확률 배율")]
            [Range(0f, 2f)] public float counterScale = 1f;
            [Tooltip("플레이어 후딜·경직을 처벌할 확률 배율")]
            [Range(0f, 2f)] public float punishScale = 1f;

            [Header("행동 템포")]
            [Tooltip("행동 사이 기본 대기와 콤보 압박 대기에 곱할 배율. 1보다 작으면 더 빨리 다음 행동을 고른다. "
                     + "헛손질 페널티는 플레이어의 반격 기회이므로 이 배율을 적용하지 않는다.")]
            [Range(0.25f, 2f)] public float actionDelayScale = 1f;
        }

        [Tooltip("레벨 구간 목록. 순서와 무관하게 몬스터 레벨 이하의 minLevel 중 가장 큰 구간을 적용한다.")]
        public List<LevelBand> bands = new();

        /// <summary>레벨에 해당하는 구간을 찾는다. 해당 구간이 없으면 null(배율 1과 같음)을 반환한다.</summary>
        public LevelBand ResolveBand(int level)
        {
            LevelBand best = null;
            for (int i = 0; i < bands.Count; i++)
            {
                LevelBand band = bands[i];
                if (band == null || band.minLevel > level)
                    continue;
                if (best == null || band.minLevel > best.minLevel)
                    best = band;
            }

            return best;
        }
    }
}
