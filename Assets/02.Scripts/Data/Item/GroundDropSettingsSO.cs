using System;
using UnityEngine;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Item
{
    /// <summary>희귀도별 바닥 드롭의 월드 연출 강도.</summary>
    [Serializable]
    public struct GroundDropRarityStyle
    {
        public ItemRarity rarity;
        [Tooltip("빛기둥 높이(m). 0이면 기둥을 그리지 않는다.")] [Min(0f)] public float beamHeight;
        [Tooltip("바닥 링 크기 배율.")] [Min(0.1f)] public float ringScale;
        [Tooltip("링·빛기둥 불투명도.")] [Range(0f, 1f)] public float glowAlpha;
    }

    /// <summary>바닥 드롭의 표시·수동 획득·안전 배치 기준.</summary>
    [CreateAssetMenu(menuName = "UPlayGround/아이템/Ground Drop Settings")]
    public sealed class GroundDropSettingsSO : ScriptableObject
    {
        public GameObject dropPrefab;
        public GameObject labelPrefab;
        [Min(0.1f)] public float pickupDistance = 4f;
        [Min(0.1f)] public float labelDistance = 15f;
        [Min(0.01f)] public float groundProbeHeight = 3f;
        [Min(0.01f)] public float groundProbeDepth = 12f;
        [Min(0.01f)] public float surfaceOffset = 0.08f;
        [Min(0.01f)] public float scatterRadius = 0.6f;
        [Tooltip("아이템 중심 사이의 목표 간격(m). 좁은 지형에서는 안전한 착지를 우선한다.")]
        [Min(0.01f)] public float minimumDropSpacing = 0.55f;
        [Tooltip("분산 착지점과 원천 바닥 사이에 허용할 높이 차이(m).")]
        [Min(0f)] public float maximumScatterHeightDifference = 0.6f;
        [Tooltip("분산 착지점의 최대 바닥 경사(도).")]
        [Range(0f, 89f)] public float maximumScatterSlope = 45f;
        [Min(0.01f)] public float labelHeight = 0.55f;
        [Min(0f)] public float labelFadeDuration = 0.12f;
        public string pickupFxKey = "LootArrival";
        [Min(0.05f)] public float pickupFxLifetime = 1.2f;
        public LayerMask groundMask;
        public LayerMask visibilityMask;

        [Header("이름표")]
        [Tooltip("이름표 UI 크기 배율. 월드 아이템보다 이름표가 튀지 않게 줄인다.")]
        [Range(0.4f, 1.5f)] public float labelScale = 0.8f;
        [Tooltip("이름표 아래 연결선 두께(px).")]
        [Min(1f)] public float labelTailWidth = 2f;
        [Tooltip("연결선 시작 불투명도. 아이템 쪽으로 갈수록 흐려진다.")]
        [Range(0f, 1f)] public float labelTailOpacity = 0.32f;

        [Header("월드 표현")]
        [Tooltip("월드 모델과 아이콘 메달의 긴 변 크기(m).")]
        [Min(0.1f)] public float tokenSize = 0.55f;
        [Tooltip("바닥에서 메달 중심까지의 높이(m).")]
        [Min(0.05f)] public float tokenHoverHeight = 0.5f;
        [Min(0f)] public float tokenBobAmplitude = 0.07f;
        [Min(0.1f)] public float tokenBobSpeed = 2.2f;
        [Tooltip("초당 회전각(도).")]
        public float tokenSpinSpeed = 70f;

        [Header("등장 연출")]
        [Tooltip("드롭 원천 위치에서 튀어나올 때 시작 높이(m).")]
        [Min(0f)] public float launchHeight = 0.9f;
        [Min(0.05f)] public float tossDuration = 0.45f;
        [Min(0f)] public float tossArcHeight = 0.8f;
        [Tooltip("착지 직후 링이 퍼지고 빛기둥이 솟는 시간.")]
        [Min(0.05f)] public float landingPulseDuration = 0.4f;
        [Tooltip("착지 뒤 이름표가 뜨기 전 대기 시간.")]
        [Min(0f)] public float labelRevealDelay = 0.1f;

        public GroundDropRarityStyle[] rarityStyles =
        {
            new GroundDropRarityStyle { rarity = ItemRarity.COMMON, beamHeight = 0f, ringScale = 0.8f, glowAlpha = 0.45f },
            new GroundDropRarityStyle { rarity = ItemRarity.UNCOMMON, beamHeight = 1.2f, ringScale = 0.9f, glowAlpha = 0.5f },
            new GroundDropRarityStyle { rarity = ItemRarity.RARE, beamHeight = 2.2f, ringScale = 1f, glowAlpha = 0.6f },
            new GroundDropRarityStyle { rarity = ItemRarity.UNIQUE, beamHeight = 3.2f, ringScale = 1.15f, glowAlpha = 0.7f },
            new GroundDropRarityStyle { rarity = ItemRarity.LEGENDARY, beamHeight = 4.5f, ringScale = 1.3f, glowAlpha = 0.8f },
        };

        /// <summary>희귀도에 맞는 연출 스타일을 찾고, 없으면 가장 약한 기본값을 돌려준다.</summary>
        public GroundDropRarityStyle GetRarityStyle(ItemRarity rarity)
        {
            if (rarityStyles != null)
                for (int i = 0; i < rarityStyles.Length; i++)
                    if (rarityStyles[i].rarity == rarity) return rarityStyles[i];
            return new GroundDropRarityStyle { rarity = rarity, beamHeight = 0f, ringScale = 0.8f, glowAlpha = 0.4f };
        }
    }
}
