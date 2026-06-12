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
    public class PlayerAttackDataSO : AttackDataSO
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

        [Header("Skill Definitions (2 Slots)")]
        [Tooltip("Ability(Skill1) / Ultimate(Skill2) 2슬롯 기반 스킬 정의. 비어 있으면 skillAttackList[0/1]을 레거시 기본 스킬로 사용한다.")]
        public List<PlayerSkillDefinition> skillDefinitions = new List<PlayerSkillDefinition>();

        [Header("Combo Routes (연계스킬)")]
        [Tooltip("입력 시퀀스 패턴 분기 연계스킬 목록. per-character.\n" +
                 "예) 약약약→강, 대시→점프→스킬1. 통합 윈도우의 '연계 라우트' 탭에서 편집.")]
        public List<ComboRouteEntry> comboRoutes = new List<ComboRouteEntry>();

        [Tooltip("연계 토큰 간 최대 허용 간격(초). 마지막 입력 이후 이 시간을 넘기면 토큰 스트림 폐기.\n" +
                 "짧으면 연계 입력이 까다롭고, 길면 의도치 않은 연계가 발생한다. 캐릭터별로 조정 가능.")]
        [Min(0.05f)]
        public float comboLinkWindow = 1.0f;

        [Tooltip("퍼펙트 가드 반격 공격 데이터. 비어 있으면 강 공격 첫 번째로 대체된다.")]
        public PlayerAttackInfo counterAttack;

        [Tooltip("패리 반격 공격 데이터. 비어 있으면 counterAttack으로 대체된다.")]
        public PlayerAttackInfo parryCounterAttack;

        [Tooltip("교체 등장 공격 데이터. 비어 있으면 약 공격 첫 번째로 대체된다.")]
        public PlayerAttackInfo entryAttack;

        [Tooltip("§5.2 등장 변형 — 타깃 적이 그로기(Stun/Knockdown/브레이크 노출)일 때 발동. 체크해야 활성화(미체크면 기본 entryAttack 사용).\n" +
                 "※ baseInfo는 Unity가 항상 인스턴스화하므로 'baseInfo 유무'로는 미설정을 구분할 수 없어 명시 토글로 옵트인한다.")]
        public bool useEntryAttackVsGroggy = false;
        public PlayerAttackInfo entryAttackVsGroggy;

        [Tooltip("§5.2 등장 변형 — 타깃 적이 공중(Airborne)일 때 발동(런치 추격 등). 체크해야 활성화(미체크면 기본 entryAttack 사용).")]
        public bool useEntryAttackVsAirborne = false;
        public PlayerAttackInfo entryAttackVsAirborne;

        [Tooltip("스왑 회피 성공 시 발동하는 카운터 공격 데이터. 비어 있으면 교체 등장 공격, 약 공격 첫 번째 순으로 대체된다.")]
        public PlayerAttackInfo swapEvadeCounterAttack;

        [Tooltip("Ultimate 게이지가 가득 찬 캐릭터로 교체할 때 발동하는 특수 공격 데이터. 비어 있으면 Ability, 등장 공격 순으로 대체된다.")]
        public PlayerAttackInfo swapSpecialAttack;

        [Tooltip("차지 공격 MotionSet AnimKey.\nMotionSet 내 InfiniteLoop 개수 = chargeStages.Count 와 일치시켜야 한다.")]
        public AnimKey chargeAnimKey = AnimKey.None;

        [Tooltip("차지 단계별 공격 데이터 (인덱스 = 단계).\nAnimKey는 chargeAnimKey 하나로 공유하므로 여기서는 수치만 설정한다.")]
        public List<ChargeStageData> chargeStages = new List<ChargeStageData>();

        [Tooltip("차지 단계 전환 비율 임계값 (0~1).\n요소 수 = chargeStages.Count - 1.\n예) 3단계 → { 0.35, 0.70 }\n비워두면 단계 수에 맞게 균등 분배된다.")]
        public List<float> chargeStageThresholds = new List<float>();

        [Tooltip("차지(홀드) 도중 캔슬 가능한 입력 액션 마스크. None이면 차지 중 캔슬 불가.")]
        public PlayerInterruptAction chargeInterruptActions = PlayerInterruptAction.Dodge;

        [Header("Full Charge VFX")]
        [Tooltip("풀 차지 도달 시 재생할 VFX 키 (GameObjectManager FX Pool)")]
        public string fullChargeVfxKey;
        [Tooltip("VFX 재생 기준 소켓. None이면 루트 위치 사용")]
        public ActorSocketType fullChargeVfxSocket = ActorSocketType.Center;
        [Tooltip("소켓 위치에 추가할 로컬 오프셋")]
        public Vector3 fullChargeVfxOffset;
    }
}
