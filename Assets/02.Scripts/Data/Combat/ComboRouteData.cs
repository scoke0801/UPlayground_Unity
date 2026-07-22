using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.Data.Combat
{
    /// <summary>
    /// 연계 라우트가 인식하는 입력 토큰.
    /// 직렬화 호환을 위해 enum 정수값을 고정한다(과거 ComboInputType 값 계승).
    /// </summary>
    public enum ComboInputToken
    {
        LightAttack = 0,   // [1] 약공
        HeavyAttack = 1,   // [2] 강공
        Dodge       = 2,   // 회피(대시)
        Skill1      = 3,   // [3] Ability (과거 Skill 계승)
        Jump        = 4,   // 점프(점프 입력 진입 한정)
        Skill2      = 5,   // [4] Ultimate
        Charge      = 6,   // 강공 홀드(차지) 완료
        Dash        = 7,   // 대시
        ElementalImbue = 8, // 캐릭터 속성 부여 공통 어빌리티
    }

    /// <summary>입력 패턴 매칭 방식</summary>
    public enum ComboMatchMode
    {
        /// <summary>스트림 끝 N개가 패턴과 일치(권장). 긴 콤보 끝에서도 짧은 라우트 성립.</summary>
        Suffix,
        /// <summary>스트림 전체가 패턴과 정확히 일치.</summary>
        Exact,
    }

    /// <summary>라우트 성립을 위한 지상/공중 조건</summary>
    public enum RouteGroundCondition
    {
        Any,
        Grounded,
        Airborne,
    }

    /// <summary>
    /// 하나의 연계 라우트(연계스킬) 정의.
    /// 입력 패턴이 일치하고 태그/상태/자원 조건을 통과할 때 대응 공격을 실행한다.
    /// 런타임에서는 AbilitySetSO.comboRoutes를 이 형태로 해석해 사용한다.
    /// </summary>
    [Serializable]
    public class ComboRouteEntry
    {
        [Tooltip("식별용 이름(에디터 표시)")]
        public string routeName = "New Route";

        [Tooltip("플레이어 노출용 스킬명(HUD 키 제시 등). 비우면 routeName으로 폴백.")]
        public string displayName = "";

        /// <summary>HUD 등 플레이어 표시용 이름(displayName 우선, 없으면 routeName).</summary>
        public string DisplayLabel =>
            string.IsNullOrEmpty(displayName) ? routeName : displayName;

        [Header("입력 패턴 (왼→오 순서)")]
        [Tooltip("이 토큰 순서가 입력 스트림 끝(Suffix) 또는 전체(Exact)와 일치해야 한다.")]
        public List<ComboInputToken> inputPattern = new();

        [Tooltip("Suffix: 스트림 끝이 패턴과 일치(권장) / Exact: 스트림 전체가 정확히 일치")]
        public ComboMatchMode matchMode = ComboMatchMode.Suffix;

        [Header("조건 (GameplayTag)")]
        [Tooltip("이 라우트를 쓰려면 Actor가 보유해야 하는 태그 (AND).")]
        public List<GameplayTagId> requiredTagIds = new();

        [Tooltip("이 중 하나라도 보유하면 사용 불가 (블록).")]
        public List<GameplayTagId> blockedTagIds = new();

        [Header("상태/물리 조건")]
        [Tooltip("이 라우트가 성립하려면 플레이어가 지상/공중 중 어느 쪽이어야 하는지")]
        public RouteGroundCondition groundCondition = RouteGroundCondition.Any;

        [Header("자원 소비")]
        [Tooltip("차감할 자원 슬롯(-1=없음). 0=Ability, 1=Ultimate. 부족하면 매칭돼도 미발동.")]
        public int skillGaugeIndex = -1;

        [Header("실행 공격")]
        [Tooltip("라우트 매칭 시 실행할 공격 정보")]
        public AbilityAttackInfo attackInfo = new();

        [Tooltip("같은 길이의 라우트가 경합할 때 우선순위(높을수록 먼저)")]
        public int priority = 0;

        [Header("퍼펙트 타이밍 강화 (선택)")]
        [Tooltip("마무리 입력이 직전 토큰으로부터 이 시간(초) 안에 들어오면 강화 발동. 0이면 강화 비활성.")]
        [Min(0f)] public float perfectWindow = 0f;

        [Tooltip("강화 시 사용할 전용 공격. 모션 참조가 설정되면 기본 공격 대신 이 공격을 실행합니다.")]
        public AbilityAttackInfo enhancedAttackInfo = new();

        [Tooltip("강화 시 데미지 배율(전용 공격 미설정일 때 기본 공격에 곱). 1이면 데미지 보너스 없음.")]
        [Min(0f)] public float enhancedDamageMultiplier = 1.15f;

        [Tooltip("강화 시 강인도/브레이크 피해 배율(전용 공격 미설정일 때 적용). 1이면 보너스 없음.")]
        [Min(0f)] public float enhancedPoiseMultiplier = 1.15f;

        [Tooltip("강화 발동 시 플레이어에게 부여할 태그(연계 보상/후속 분기용). None이면 없음.")]
        public GameplayTagId enhancedGrantTagId = GameplayTagId.None;

        public bool IsEmpty => inputPattern == null || inputPattern.Count == 0;

        /// <summary>퍼펙트 타이밍 강화가 활성화된 라우트인지.</summary>
        public bool HasPerfectWindow => perfectWindow > 0f;

        /// <summary>강화 시 실행할 전용 공격 모션이 설정돼 있는지.</summary>
        public bool HasEnhancedAttack =>
            enhancedAttackInfo?.baseInfo != null
            && enhancedAttackInfo.baseInfo.motionRef != null
            && enhancedAttackInfo.baseInfo.motionRef.HasAnyMotion;

        /// <summary>패턴의 마지막 토큰(없으면 LightAttack 폴백).</summary>
        public ComboInputToken LastToken =>
            (inputPattern != null && inputPattern.Count > 0)
                ? inputPattern[inputPattern.Count - 1]
                : ComboInputToken.LightAttack;

        /// <summary>
        /// 태그 조건을 읽기 전용 태그 계약으로 검사한다.
        /// container == null이면 required가 비어 있을 때만 통과(에디터/무태그 환경 관대 처리).
        /// </summary>
        public bool CheckTagConditions(IGameplayTagReader container)
        {
            if (container == null)
                return requiredTagIds == null || requiredTagIds.Count == 0;

            if (requiredTagIds != null)
            {
                foreach (var id in requiredTagIds)
                {
                    if (id == GameplayTagId.None) continue;
                    if (!container.HasTag(id)) return false;
                }
            }

            if (blockedTagIds != null)
            {
                foreach (var id in blockedTagIds)
                {
                    if (id == GameplayTagId.None) continue;
                    if (container.HasTag(id)) return false;
                }
            }

            return true;
        }
    }
}
