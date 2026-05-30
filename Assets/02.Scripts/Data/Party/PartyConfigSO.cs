using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Party
{
    /// <summary>
    /// 파티 구성 정보를 정의하는 ScriptableObject.
    /// Resources/Data/PartyConfig.asset 에 배치해 PartyManager가 로드한다.
    /// </summary>
    [CreateAssetMenu(fileName = "PartyConfig", menuName = "UPlayGround/Party/Party Config")]
    public class PartyConfigSO : ScriptableObject
    {
        [Header("Roster")]
        [Tooltip("게임 시작 시 보유한 캐릭터 전체 목록(=초기 Roster). 처치 보상으로 추가될 캐릭터는 런타임에 합류한다.")]
        public List<CharacterActorType> partyOrder = new();

        [Header("Battle Order")]
        [Tooltip("출전(BattleOrder) 슬롯 상한. 신규 합류 시 이 수보다 적게 차있으면 자동 편입된다.")]
        [Min(1)]
        public int maxBattleSize = 4;

        [Tooltip("게임 시작 시 출전 슬롯에 배치할 캐릭터. 비어있으면 partyOrder의 앞 maxBattleSize 명을 사용.")]
        public List<CharacterActorType> defaultBattleOrder = new();

        [Tooltip("게임 시작 시 조작할 캐릭터의 BattleOrder 인덱스 (0부터 시작)")]
        [Min(0)]
        public int startActiveIndex = 0;

        [Header("Swap")]
        [Tooltip("다른 출전 파티원으로 교체한 뒤, 교체되어 나간 캐릭터에게 적용할 개별 스왑 쿨타임.")]
        [Min(0f)]
        public float swapCooldown = 3f;

        [Header("Residual Attack")]
        [Tooltip("공격 중 교체 시 퇴장 캐릭터 모델 복제본이 남은 공격 타임라인을 실행한다.")]
        public bool enableResidualAttackOnSwap = true;

        [Tooltip("잔류 공격 오브젝트 최대 생존 시간. 이벤트 누락/무한 루프 방지용.")]
        [Min(0.1f)]
        public float residualAttackMaxLifetime = 1.8f;

        [Tooltip("향후 페이드/디졸브 정리용 지속 시간. 1차 구현에서는 즉시 제거 폴백을 사용한다.")]
        [Min(0f)]
        public float residualAttackFadeOutDuration = 0.25f;

        [Tooltip("잔류 공격 히트 시 히트스톱 피드백을 허용한다.")]
        public bool residualAttackAllowHitStop = true;

        [Tooltip("잔류 모델 루트모션 이동 허용. KCC를 사용하지 않으므로 기본값은 false 권장.")]
        public bool residualAttackUseRootMotion = false;

        [Tooltip("잔류 루트모션 이동 허용 시 최대 누적 이동 거리.")]
        [Min(0f)]
        public float residualAttackRootMotionMaxDistance = 2.5f;

        [Tooltip("잔류 루트모션 이동 중 막히는 레이어. 0이면 충돌 보정을 하지 않는다.")]
        public LayerMask residualAttackRootMotionBlocker = 0;

        [Tooltip("동시에 유지할 수 있는 잔류 공격 모델 수.")]
        [Min(1)]
        public int residualAttackMaxCount = 1;

        [Tooltip("같은 캐릭터 타입 잔류 러너가 남아 있으면 그 위치로 복귀한다.")]
        public bool residualAttackReturnToSameCharacterRunner = true;

        [Tooltip("같은 캐릭터 복귀 위치로 인정할 최대 잔류 러너 나이.")]
        [Min(0f)]
        public float residualAttackReturnPositionMaxAge = 1.8f;

        [Tooltip("잔류 공격 히트스톱 최소 재발동 간격.")]
        [Min(0f)]
        public float residualAttackFeedbackMinInterval = 0.08f;

        [Tooltip("잔류 공격 히트스톱 지속 시간.")]
        [Min(0f)]
        public float residualAttackHitStopDuration = 0.04f;

        [Tooltip("잔류 공격 히트스톱 타임스케일.")]
        [Range(0.01f, 1f)]
        public float residualAttackHitStopTimeScale = 0.2f;

        [Tooltip("잔류 공격 데미지 플로터에 퇴장 캐릭터 타입을 함께 표시한다.")]
        public bool residualAttackShowCharacterOnDamageFloater = false;

        [Tooltip("교체 후 다시 돌아왔을 때 캐릭터별 일반 콤보 진행도를 복원한다.")]
        public bool preserveComboStatePerCharacter = true;

        [Tooltip("저장된 콤보 진행도를 이어갈 수 있는 최대 시간. 0 이하면 시간 제한 없이 저장값을 사용한다.")]
        [Min(0f)]
        public float comboStateMaxCarryTime = 1.8f;

        [Header("Growth")]
        [Tooltip("캐릭터별 레벨 성장 데이터. 누락된 캐릭터는 기본 스탯 기준으로 전투력을 계산한다.")]
        public List<PartyMemberGrowthSO> growthData = new();

        [Header("Entry Attack Defaults")]
        [Tooltip("CharacterModelData.entryAttackRange 가 0 이하일 때 사용할 기본 검출 반경.")]
        [Min(0f)]
        public float defaultEntryAttackRange = 6f;

        [Tooltip("등장 공격의 적 검출 레이어. 락온/공격 레이어와 동일 권장.")]
        public LayerMask entryAttackTargetLayer = ~0;

        [Tooltip("LOS 검사 시 시야를 가로막는 레이어 (지형 등). requireLineOfSight=true 인 캐릭터에만 사용.")]
        public LayerMask entryAttackLineOfSightBlocker = 0;
        
        [Header("파티원 이미지")]
        public PartyMemberDataSO partyMemberData;
    }
}
