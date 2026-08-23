using System;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Path;
using UPlayGround.Animation;
using UPlayGround.Components;
using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Event;
using UPlayGround.Data.Stat;
using UPlayGround.MovementController;
using UPlayGround.Input;
using UPlayGround.InputDefine;
using UPlayGround.Manager;
using UPlayGround.Combat;
using UPlayGround.State;
using UPlayGround.UI;
using Random = UnityEngine.Random;
using UPlayGround.AI.CombatDecision;
using UPlayGround.Data.Ability;
using UPlayGround.Ability.Core;

namespace UPlayGround
{
    public partial class PlayerActor : GameActor, IDamageable, IPlayerInputSuppressible
    {
        // 매니저 참조 캐싱 — 반복 레지스트리/Instance 조회 방지.
        // 인터페이스 캐시(??=)는 파괴된 매니저(fake-null)를 감지하지 못하지만,
        // 매니저는 DDoL로 세션 내내 유지되고 PlayerActor는 씬 오브젝트로 재생성되므로
        // 인스턴스 필드 캐시는 세션 내 stale 위험이 없다. (CameraMgr는 Unity null 비교로 자가 복구)
        private IInputService _cachedInputManager;
        private IInputService InputMgr => _cachedInputManager ??= Svc.Input;
        private CameraManager _cachedCameraManager;
        private CameraManager CameraMgr => _cachedCameraManager != null ? _cachedCameraManager : (_cachedCameraManager = CameraManager.Instance);
        private IActorObjectService _cachedGameObjectManager;
        private IActorObjectService GameObjectMgr => _cachedGameObjectManager ??= ActorSvc.Objects;
        private IActorCombatService _cachedGameCombatManager;
        private IActorCombatService GameCombatMgr => _cachedGameCombatManager ??= ActorSvc.Combat;


        [SerializeField] private float _interactionRadius;
        [SerializeField] private LayerMask _interactionLayer;

        private float _maxHealth =>
            AbilitySystem?.Attributes.GetCurrent(global::UPlayGround.Data.Stat.Attributes.Vital.MaxHealth) ?? 0f;

        private float _currentHealth
        {
            get => AbilitySystem?.Attributes.GetCurrent(global::UPlayGround.Data.Stat.Attributes.Vital.Health) ?? 0f;
            set => AbilitySystem?.Attributes.SetBase(global::UPlayGround.Data.Stat.Attributes.Vital.Health, value);
        }
        [SerializeField] private bool  _isInvincible  = false;

        // 캐릭터별 Health/Gauge/Cooldown/Effect를 하나의 ASC 저장 스냅샷으로 보관한다.
        private readonly Dictionary<CharacterActorType, AbilitySystemSaveData>
            _characterAbilitySystemMap = new();
        private readonly List<AttributeModifierValue> _equipmentStatBuffer = new();
        private ActiveGameplayEffectHandle _equipmentStatEffectHandle;
        private readonly List<AttributeModifierValue> _skillTreeStatBuffer = new();
        private ActiveGameplayEffectHandle _skillTreeStatEffectHandle;
        private bool _hasInitializedCharacterRuntime;

        // 피격 리액션 Ability 누락을 캐릭터+트리거 단위로 1회만 보고하기 위한 기록.
        private readonly HashSet<string> _warnedReactionTriggers = new();

        [SerializeField] private PlayerEquipment  _equipment;
        [SerializeField] private PlayerCombat     _combat;
        [SerializeField] private PlayerAbilityResourceView _skillGauge;
        private PlayerStaminaRuntime _stamina;
        [SerializeField] private PlayerCombatWeaponStateController _combatWeaponStateController;
        [SerializeField] private FootIKController _footIK;
        [SerializeField] private PlayerBehaviorPredictor _behaviorPredictor;
        private PlayerSwapBehaviour _swapBehaviour;

        [Header("Hit Shake Keys")]
        [Tooltip("일반 피격 쉐이크")]
        [SerializeField] private CameraShakeIdType _shakeKeyHit      = CameraShakeIdType.PlayerHit;
        [Tooltip("Heavy / KnockBack / Airborne 피격 쉐이크")]
        [SerializeField] private CameraShakeIdType _shakeKeyHeavyHit = CameraShakeIdType.PlayerHeavyHit;
        [Tooltip("사망 쉐이크")]
        [SerializeField] private CameraShakeIdType _shakeKeyDeath    = CameraShakeIdType.PlayerDeath;

        [Header("Parry")]
        [Tooltip("패리 성공 시 재생할 VFX 이름")]
        [SerializeField] private string _parryFxName = "ParryFX";

        // 기본 Airborne 수치(7~8)는 피격 경직으로 처리하고, 전용 launch급 공격만 공중 상태로 보낸다.
        private const float MinAirborneStateForce = 10f;

        public event Action<float, float> OnHpChanged;
        public event Action<float, float> OnSkillGaugeChanged;
        public event Action<float, float> OnStaminaChanged;

        protected PlayerMovementController PlayerMovementPlayerController;

        private Camera              _camera;
        private PlayerActorAnimator _playerActorAnimator;

        private Vector2        _currentMoveInput;
        private InputCondition _jumpInputCondition;
        private InputCondition _crouchInputCondition;
        private InputCondition _dodgeInputCondition;
        private InputCondition _dashInputCondition;
        private InputCondition _attackInputCondition;
        private InputCondition _heavyInputCondition;
        private InputCondition _equipInputCondition;

        // 교체 어시스트
        private bool _swapAssistQueued = false;
        // 어시스트 패리(§4.3): 패리 윈도우 우선. 창 내 피격 시 패리 처리, 비소비 만료 시 즉시공격으로 폴백.
        private bool  _assistParryFallbackPending = false;
        private float _assistParryFallbackTime    = -999f;

        // 스왑 회피 카운터. 1차 구현은 등장 공격 데이터를 재사용한다.
        private bool         _swapEvadeQueued = false;
        private MonsterActor _swapEvadeTarget;
        private float        _swapEvadeInvincibleEndTime = -999f;
        private float        _swapEvadeCounterInputEndTime = -999f;

        // 경직 내성(Stagger Protection): 리액션 회복 직후 짧은 창 동안 약한 리액션(Light/Hit)을 무시한다.
        // 데미지는 그대로 적용되고 통제권만 보호 → 다인전 Hit→Idle(찰나)→Hit 재스턴 루프를 차단한다.
        private float        _staggerImmuneEndTime = -999f;
        private bool         _staggerImmunityTagGranted;
        private AbilityExecutionHandle _triggeredReactionHandle;
        private ActorStateId? _triggeredReactionState;
        // 회복 직후 부여 길이(초). 0.25~0.35 권장. 큰 한 방(Heavy/넉백 등)은 이 창에도 통과한다.
        public const float   StaggerImmunityDuration = 0.3f;

        // 등장 공격 (교체 직후 범위 내 적 존재 시 발동)
        private bool         _entryAttackQueued = false;
        private MonsterActor _entryAttackTarget;
        // PlayerAttackState 가 OnEnter에서 1회 소비하여 ExecuteEntryAttack 라우팅을 트리거.
        private bool         _isEntryAttackPending = false;
        private bool         _isSwapEvadeCounterAttackPending = false;
        private bool         _isSwapSpecialAttackPending = false;
        private bool         _isInputSuppressed = false;

        // 차지 공격 입력 추적
        private bool  _chargeAttackHeld;
        private float _chargeHoldTime;
        private const float ChargeThreshold = 0.3f; // 이 시간 이상 홀드 시 차지로 전환
        private InputCondition _interactionInputCondition;
        private InputCondition _guardInputCondition;

        // 직접 버튼이 연결된 스킬 슬롯(Ability, Ultimate, 공통 속성 부여).
        private List<InputCondition> _skillInputCondition = new List<InputCondition>
        {
            InputCondition.None, InputCondition.None, InputCondition.None,
        };
        private readonly List<bool> _skillInputHeld = new()
        {
            false, false, false,
        };
        private readonly List<InputCondition> _skillInputSnapshot = new()
        {
            InputCondition.None, InputCondition.None, InputCondition.None,
        };

        public override ActorAnimator      Animator              => _playerActorAnimator;
        public PlayerMovementController    PlayerController       => PlayerMovementPlayerController;
        public float                       InteractionRadius      => _interactionRadius;
        public LayerMask                   InteractionLayer       => _interactionLayer;
        public bool                        IsEquippedRightWeapon => _equipment.IsMainWeaponEquipped;
        public bool                        IsEquippedLeftWeapon  => _equipment.IsSubWeaponEquipped;
        public bool                        IsInCombat            => _combat?.IsInCombat ?? false;
        public float                       MaxHealth             => _maxHealth;
        public float                       CurrentHealth         => _currentHealth;
        public PlayerAbilityResourceView            SkillGauge            => _skillGauge;
        public PlayerStaminaRuntime                  Stamina               => _stamina;
        public FootIKController            FootIK                => _footIK;
        public bool                        IsInputSuppressed     =>
            _isInputSuppressed || _swapBehaviour?.IsVisualReady != true;
        public bool                        IsInvincible          => _isInvincible;
        public bool                        IsSwapEvadeInvincible => ActorTime <= _swapEvadeInvincibleEndTime;
        public bool                        IsSwapEvadeCounterAvailable => ActorTime <= _swapEvadeCounterInputEndTime;
        public bool                        IsStaggerImmune       => ActorTime <= _staggerImmuneEndTime;

        /// <summary>
        /// 현재 활성 플레이어가 Drink 모션을 시작할 수 있는지 확인한다.
        /// 소모품 사용 가능 여부와는 별개이며, 비전투 Idle 상태이고 Drink MotionSet이 있을 때만 true다.
        /// </summary>
        public bool CanStartConsumableUse()
        {
            return IsAlive()
                && !IsInCombat
                && PlayerMovementPlayerController?.CurrentState is PlayerIdleState
                && Animator?.HasMotion(UPlayGround.Data.Actor.Animation.MotionTags.Drink, true) == true;
        }

        /// <summary>
        /// Idle 상태에서 소모품 사용 전용 상태로 전환한다.
        /// </summary>
        public bool TryStartConsumableUse()
        {
            return CanStartConsumableUse()
                && PlayerMovementPlayerController.TryTransitionToState(
                    new PlayerDrinkState(PlayerMovementPlayerController));
        }
    }
}
