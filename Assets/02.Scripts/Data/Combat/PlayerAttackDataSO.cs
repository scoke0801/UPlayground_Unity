using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Combat
{
    /// <summary>
    /// 플레이어 공격 데이터 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerAttackData", menuName = "UPlayGround/Player/Attack Data")]
    public class PlayerAttackDataSO : ScriptableObject
    {
        [Header("Attack Pool")]
        [Tooltip("약 공격 리스트")]
        public List<PlayerAttackInfo> liteComboAttackList  = new List<PlayerAttackInfo>();
        
        [Tooltip("강 공격 리스트")]
        public List<PlayerAttackInfo> heavyComboAttackList  = new List<PlayerAttackInfo>();
        
        [Tooltip("점프 공격 리스트")]
        public List<PlayerAttackInfo> jumpAttackList  = new List<PlayerAttackInfo>();
        
        [Tooltip("대쉬 공격 리스트")]
        public List<PlayerAttackInfo> dashAttackList  = new List<PlayerAttackInfo>();
        
        [Tooltip("스킬 공격 리스트")]
        public List<PlayerAttackInfo> skillAttackList  = new List<PlayerAttackInfo>();

        [Tooltip("차지 공격 MotionSet AnimKey.\nMotionSet 내 InfiniteLoop 개수 = chargeStages.Count 와 일치시켜야 한다.")]
        public AnimKey chargeAnimKey = AnimKey.None;

        [Tooltip("차지 단계별 공격 데이터 (인덱스 = 단계).\nAnimKey는 chargeAnimKey 하나로 공유하므로 여기서는 수치만 설정한다.")]
        public List<ChargeStageData> chargeStages = new List<ChargeStageData>();

        [Tooltip("차지 단계 전환 비율 임계값 (0~1).\n요소 수 = chargeStages.Count - 1.\n예) 3단계 → { 0.35, 0.70 }\n비워두면 단계 수에 맞게 균등 분배된다.")]
        public List<float> chargeStageThresholds = new List<float>();

        [Header("Combo Sequences")]
        [Tooltip(
            "입력 패턴 기반 콤보 시퀀스.\n" +
            "패턴과 태그 조건이 일치하면 일반 liteCombo / heavyCombo보다 우선 실행된다.\n" +
            "같은 길이의 시퀀스가 여럿이면 priority가 높은 것을 먼저 체크한다.")]
        public List<ComboSequenceEntry> comboSequences = new();

        [Header("Full Charge VFX")]
        [Tooltip("풀 차지 도달 시 재생할 VFX 키 (GameObjectManager FX Pool)")]
        public string fullChargeVfxKey;
        [Tooltip("VFX 재생 기준 소켓. None이면 루트 위치 사용")]
        public ActorSocketType fullChargeVfxSocket = ActorSocketType.Center;
        [Tooltip("소켓 위치에 추가할 로컬 오프셋")]
        public Vector3 fullChargeVfxOffset;
    }
}