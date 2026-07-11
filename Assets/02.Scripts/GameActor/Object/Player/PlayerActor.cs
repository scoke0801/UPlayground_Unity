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
using UPlayGround.Manager.Handler;
using UPlayGround.Manager.Combat;
using UPlayGround.Combat;
using UPlayGround.State;
using UPlayGround.UI;
using Random = UnityEngine.Random;
using UPlayGround.AI.CombatDecision;

namespace UPlayGround
{
    public partial class PlayerActor : GameActor, IDamageable
    {
        // 매니저 참조 캐싱 — 반복 Instance 조회(락 경합) 방지, 파괴 시 fake-null로 재조회
        private InputManager _cachedInputManager;
        private InputManager InputMgr => _cachedInputManager != null ? _cachedInputManager : (_cachedInputManager = InputManager.Instance);
        private CameraManager _cachedCameraManager;
        private CameraManager CameraMgr => _cachedCameraManager != null ? _cachedCameraManager : (_cachedCameraManager = CameraManager.Instance);
        private GameObjectManager _cachedGameObjectManager;
        private GameObjectManager GameObjectMgr => _cachedGameObjectManager != null ? _cachedGameObjectManager : (_cachedGameObjectManager = GameObjectManager.Instance);
        private GameCombatManager _cachedGameCombatManager;
        private GameCombatManager GameCombatMgr => _cachedGameCombatManager != null ? _cachedGameCombatManager : (_cachedGameCombatManager = GameCombatManager.Instance);


        [SerializeField] private float _interactionRadius;
        [SerializeField] private LayerMask _interactionLayer;

        [SerializeField] private float _maxHealth     = 100f;
        [SerializeField] private float _currentHealth = 100f;
        [SerializeField] private bool  _isInvincible  = false;

        // 교체 시 캐릭터별 체력·스킬 게이지 저장소
        private readonly Dictionary<CharacterActorType, float> _characterHealthMap = new();
        private readonly Dictionary<CharacterActorType, float> _characterSkillMap  = new();
        private readonly Dictionary<CharacterActorType, float[]> _characterSkillCooldownMap = new();
        private readonly object _equipmentStatSource = new();
        private readonly List<StatModifier> _equipmentStatBuffer = new();
        private bool _hasInitializedCharacterRuntime;

        [SerializeField] private PlayerEquipment  _equipment;
        [SerializeField] private PlayerCombat     _combat;
        [SerializeField] private PlayerSkillGauge _skillGauge;
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

        // 직접 버튼이 연결된 스킬 슬롯만 보유 (Skill_1, Skill_2). 그 외 스킬은 연계로 발동.
        private List<InputCondition> _skillInputCondition = new List<InputCondition>
        {
            InputCondition.None, InputCondition.None,
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
        public PlayerSkillGauge            SkillGauge            => _skillGauge;
        public FootIKController            FootIK                => _footIK;
        public bool                        IsInputSuppressed     => _isInputSuppressed;
        public bool                        IsInvincible          => _isInvincible;
        public bool                        IsSwapEvadeInvincible => Time.time <= _swapEvadeInvincibleEndTime;
        public bool                        IsSwapEvadeCounterAvailable => Time.time <= _swapEvadeCounterInputEndTime;
        public bool                        IsStaggerImmune       => Time.time <= _staggerImmuneEndTime;
    }
}
