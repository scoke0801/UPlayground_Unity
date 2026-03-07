using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Data.Combat
{
    /// <summary>
    /// 회복 구슬 트리거 설정 데이터.
    /// 트리거별 드롭 SO, 확률, 쿨다운, 최대 중첩 수를 관리합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "VitalOrbTriggerConfig", menuName = "UPlayGround/Combat/VitalOrb Trigger Config")]
    public class VitalOrbTriggerConfig : ScriptableObject
    {
        public List<VitalOrbTriggerEntry> entries = new();
    }

    [Serializable]
    public class VitalOrbTriggerEntry
    {
        [Tooltip("트리거 식별자 (VitalOrbTrigger Enum과 1:1 대응)")]
        public VitalOrbTrigger trigger;

        public VitalOrbDataSO dropData;

        [Range(0f, 1f)]
        public float probability = 0.1f;

        [Tooltip("동시에 월드에 존재 가능한 최대 오브젝트 수")]
        public int maxStack = 2;

        [Tooltip("동일 트리거 연속 발동 방지 쿨다운 (초)")]
        public float cooldown = 1.0f;
    }

    /// <summary>
    /// 회복 구슬 드롭 트리거 종류.
    /// 전투 코드의 이벤트 발생 지점과 1:1 대응합니다.
    /// </summary>
    public enum VitalOrbTrigger
    {
        FinishAttackHit,    // 처형 공격 히트 (100%)
        KillKillCam,        // 일반 처치 (킬캠 발동) (40%)
        PerfectGuard,       // 퍼펙트 가드 성공 (80%)
        Dodge,              // 구르기 (15%)
        Guard,              // 가드 (10%)
        HeavyAttackHit,     // 강 공격 히트 (12%)
        LightAttackHit,     // 약 공격 히트 (5%)
    }
}
