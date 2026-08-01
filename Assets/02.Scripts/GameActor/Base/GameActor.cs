using System;
using AYellowpaper.SerializedCollections;
using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Event;
using UPlayGround.Animation;
using UPlayGround.Combat;
using UPlayGround.Components;
using UPlayGround.Manager;
using UPlayGround.MovementController;
using UPlayGround.Gameplay.Tag;
using UPlayGround.Gameplay.Ability;
using UPlayGround.Gameplay.Effect;

namespace UPlayGround
{
    public abstract class GameActor : MonoBehaviour, IWorldActor, IHealthRatioProvider, IMotionEventTargetProvider
    {
        private const string PlayerDefaultTargetLayerName = "Enemy";
        private const string MonsterDefaultTargetLayerName = "Player";

        [HideInInspector, SerializeField] protected ActorType _actorType = ActorType.None;
        [HideInInspector, SerializeField] protected CharacterActorType _characterActorType = CharacterActorType.None;

        [Header("Actor Definition")]
        [SerializeField] private ActorDefinitionSO _definition;

        [Header("Actor Identity")]
        [HideInInspector, SerializeField] private string _actorId = "";

        [SerializeField] protected SerializedDictionary<ActorSocketType, Transform> _socketDict;

        protected ActorMovementController MovementController;
        protected ActorAnimator _animator;
        
        protected ActorColorChanger _colorChanger;
        protected DissolveController _dissolveController;
        protected ActorCameraProximityDither _cameraProximityDither;

        private float _localTimeScale = 1.0f;
        private readonly System.Collections.Generic.Dictionary<ulong, ElementOverride>
            _elementOverrides = new();
        private CombatElement _currentElement;
        private CombatElement? _runtimeBaseElement;

        private readonly struct ElementOverride
        {
            public readonly CombatElement Element;
            public readonly int Priority;

            public ElementOverride(CombatElement element, int priority)
            {
                Element = element;
                Priority = priority;
            }
        }

        /// <summary>런타임 태그 컨테이너. 상태 진입/이탈 시 태그를 추가/제거한다.</summary>
        public GameplayTagContainer Tags { get; private set; }

        /// <summary>액터별 Ability 활성화/쿨다운 런타임.</summary>
        public ActorAbilitySystem Abilities { get; private set; }

        /// <summary>액터별 지속 Effect 런타임.</summary>
        public GameplayEffectController Effects { get; private set; }

        /// <summary>GAS 기반 Ability/Effect/Tag/Attribute 단일 집합 루트.</summary>
        public AbilitySystemComponent AbilitySystem { get; private set; }

        /// <summary>Definition 기본값과 활성 GameplayEffect를 합성한 현재 전투 속성.</summary>
        public CombatElement CurrentElement => _currentElement;
        public bool HasElementOverride => _elementOverrides.Count > 0;

        /// <summary>유리한 속성 공격에 적용되는 Definition 기반 피해 배율.</summary>
        public float ElementalAdvantageMultiplier =>
            _definition != null
                ? Mathf.Max(1f, _definition.elementalAdvantageMultiplier)
                : CombatElementRules.DefaultAdvantageMultiplier;

        public event Action<CombatElement> ElementChanged;
        public event Action ElementOverrideChanged;

        GameObject IMotionEventTargetProvider.MotionEventTarget => gameObject;

        /// <summary>
        /// 액터 개별 타임 스케일 (기본 1.0)
        /// </summary>
        public float LocalTimeScale
        {
            get => _localTimeScale;
            set
            {
                _localTimeScale = float.IsNaN(value) || float.IsInfinity(value)
                    ? 1f
                    : Mathf.Max(0.001f, value);
                if (_animator != null)
                {
                    _animator.Speed = _localTimeScale;
                }
            }
        }

        /// <summary>
        /// 로컬 타임 스케일이 적용된 DeltaTime
        /// </summary>
        public float DeltaTime => Time.deltaTime * _localTimeScale;

        /// <summary>LocalTimeScale이 누적 반영된 액터 고유 시각.</summary>
        public float ActorTime { get; private set; }

        public virtual ActorAnimator Animator => _animator;
        public BaseMoveAnimType MoveAnimType { get; set; } = BaseMoveAnimType.Run;

        public ActorType ActorType => _actorType;
        public CharacterActorType CharacterType => _characterActorType;

        MonsterActorGrade IWorldActor.Grade =>
            this is MonsterActor monster ? monster.Grade : MonsterActorGrade.Normal;
        Transform IWorldActor.Transform => transform;
        bool IWorldActor.IsAlive => this is IDamageable damageable && damageable.IsAlive();
        float IHealthRatioProvider.HealthRatio =>
            this is IDamageable damageable ? damageable.GetHealthPercent() : 0f;

        void IWorldActor.LockOn()
        {
            if (this is IDamageable damageable)
                damageable.LockOn();
        }

        void IWorldActor.UnLockOn()
        {
            if (this is IDamageable damageable)
                damageable.UnLockOn();
        }

        /// <summary>런타임 고유 ID. 스폰 시 ActorDefinitionSO에서 주입되거나 Inspector에서 직접 설정.</summary>
        public string ActorId => _actorId;

        /// <summary>이 액터를 정의하는 ScriptableObject. 런타임 스폰 시 주입됨.</summary>
        public ActorDefinitionSO Definition => _definition;

        /// <summary>전투 액션 실행 런타임. CombatActionRunner.Awake에서 자신을 등록한다(같은 GameObject).</summary>
        public CombatActionRunner ActionRunner { get; private set; }

        /// <summary>CombatActionRunner가 init 시 호출해 placement 의존 없이 참조를 캐시한다.</summary>
        public void RegisterActionRunner(CombatActionRunner runner) => ActionRunner = runner;

        /// <summary>
        /// 플래그 조합 체크. 예: actor.HasActorType(ActorType.Talkable)
        /// </summary>
        public bool HasActorType(ActorType flag) => (_actorType & flag) != 0;

        public LayerMask GetAttackTargetLayerMask()
        {
            if (_definition != null && _definition.targetLayerMask.value != 0)
                return _definition.targetLayerMask;

            if (HasActorType(ActorType.Player))
                return LayerMask.GetMask(PlayerDefaultTargetLayerName);

            if (HasActorType(ActorType.Monster))
                return LayerMask.GetMask(MonsterDefaultTargetLayerName);

            return 0;
        }

        /// <summary>
        /// ActorDefinitionSO를 주입한다. ActorSpawnManager가 스폰 직후 호출.
        /// 서브클래스에서 오버라이드해 스탯 등 추가 적용 가능.
        /// </summary>
        public virtual void SetDefinition(ActorDefinitionSO definition)
        {
            _definition = definition;
            ApplyBaseDefinition();
            RefreshCurrentElement();
        }

        public ActorMovementController ActorController => MovementController;

        /// <summary>피격 연출 컴포넌트를 자동 부착해야 하는 액터인지 여부.</summary>
        protected virtual bool RequiresCombatVisuals => true;
        
        protected virtual void Awake()
        {
            AbilitySystem = gameObject.GetOrAddComponent<AbilitySystemComponent>();
            AbilitySystem.EnsureInitialized();
            Tags = AbilitySystem.ProjectTags;
            Effects = AbilitySystem.ProjectEffects;
            Abilities = AbilitySystem.ProjectAbilities;
            ApplyBaseDefinition();
            MovementController = GetComponent<ActorMovementController>();
            _animator = GetComponentInChildren<ActorAnimator>();
            if (_animator != null)
                _animator.Init(this);
            
            if (RequiresCombatVisuals)
            {
                _colorChanger = gameObject.GetOrAddComponent<ActorColorChanger>();
                _dissolveController = gameObject.GetOrAddComponent<DissolveController>();
                _cameraProximityDither = gameObject.GetOrAddComponent<ActorCameraProximityDither>();
            }
            RefreshCurrentElement();
            
            // 매니저에 등록
            ActorSvc.Objects?.RegisterActor(this);
        }

        private void ApplyBaseDefinition()
        {
            if (_definition == null) return;

            if (!string.IsNullOrEmpty(_definition.actorId))
                _actorId = _definition.actorId;

            _actorType = _definition.actorType;
            _characterActorType = _definition.characterType;
        }

        protected virtual void Update()
        {
            ActorTime += DeltaTime;
        }

        public void AddElementOverride(
            ulong sourceId,
            CombatElement element,
            int priority = 0)
        {
            if (sourceId == 0 || element == CombatElement.None)
                return;

            _elementOverrides[sourceId] = new ElementOverride(element, priority);
            RefreshCurrentElement();
            ElementOverrideChanged?.Invoke();
        }

        public void RemoveElementOverride(ulong sourceId)
        {
            if (sourceId == 0 || !_elementOverrides.Remove(sourceId))
                return;
            RefreshCurrentElement();
            ElementOverrideChanged?.Invoke();
        }

        public void ResolveDefinitionElement(int newGameSeed)
        {
            _runtimeBaseElement = _definition != null
                ? _definition.ResolveCombatElement(newGameSeed)
                : CombatElement.None;
            RefreshCurrentElement();
        }

        protected void SetCharacterBaseElement(CombatElement element)
        {
            _runtimeBaseElement = element;
            RefreshCurrentElement();
        }

        private void RefreshCurrentElement()
        {
            CombatElement resolved = _runtimeBaseElement
                ?? (_definition != null
                    ? _definition.combatElement
                    : CombatElement.None);
            int bestPriority = int.MinValue;
            ulong bestSourceId = 0;

            foreach (var pair in _elementOverrides)
            {
                ElementOverride candidate = pair.Value;
                if (candidate.Priority < bestPriority
                    || candidate.Priority == bestPriority
                    && pair.Key <= bestSourceId)
                {
                    continue;
                }

                resolved = candidate.Element;
                bestPriority = candidate.Priority;
                bestSourceId = pair.Key;
            }

            if (_currentElement == resolved)
                return;
            _currentElement = resolved;
            ElementChanged?.Invoke(resolved);
        }

        
        protected virtual void OnDestroy()
        {
            // 매니저에서 제거
            if (Services.TryGet<IActorObjectService>(out var objects))
                objects.UnregisterActor(this);
        }
        
        protected virtual void Start()
        {
            // SpawnActor를 거치지 않고 생성된 Actor(스킬 소환 등)를 추적 목록에 등록.
            // AfterInit 스캔 또는 SpawnActor에서 이미 등록된 경우 무시된다.
            if (!string.IsNullOrEmpty(_actorId))
                ActorSvc.SpawnTracking?.RegisterActor(this);
        }

        /// <summary>
        /// Grab 공격으로 피격자를 잡고 있는 동안, 공격자가 모션 해제를 알릴 때 발화.
        /// 피격자의 GrabbedState가 구독하여 즉시 탈출한다.
        /// </summary>
        public event Action OnForcedMotionReleased;

        /// <summary>
        /// 공격자가 모션 종료 시점에 호출. MotionEvent 또는 AttackState.OnExit에서 호출한다.
        /// </summary>
        public void FireForcedMotionReleased()
        {
            OnForcedMotionReleased?.Invoke();
            OnForcedMotionReleased = null;
        }

        public bool HasSocket(ActorSocketType socketType)
        {
            return _socketDict.ContainsKey(socketType);
        }
        
        public Transform GetSocket(ActorSocketType socketType)
        {
            if (_socketDict.TryGetValue(socketType, out var result))
            {
                return result;
            }

            return null;
        }

        public bool TryGetSocket(ActorSocketType socketType, out Transform socket)
        {
            if (_socketDict.TryGetValue(socketType, out socket))
            {
                return true;
            }
            
            return false;
        }
    }
}
