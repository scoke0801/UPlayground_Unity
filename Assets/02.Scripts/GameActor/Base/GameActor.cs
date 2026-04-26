using System;
using AYellowpaper.SerializedCollections;
using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;
using UPlayGround.Animation;
using UPlayGround.Component;
using UPlayGround.Manager;
using UPlayGround.MovementController;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround
{
    public abstract class GameActor : MonoBehaviour
    {
        [SerializeField] protected ActorType _actorType = ActorType.None;
        [SerializeField] protected CharacterActorType _characterActorType = CharacterActorType.None;

        [Header("Actor Identity")]
        [SerializeField] private string _actorId = "";
        private ActorDefinitionSO _definition;

        [SerializeField] protected SerializedDictionary<ActorSocketType, Transform> _socketDict;

        protected ActorMovementController MovementController;
        protected ActorAnimator _animator;
        
        protected ActorColorChanger _colorChanger;
        protected DissolveController _dissolveController;

        private float _localTimeScale = 1.0f;

        /// <summary>런타임 태그 컨테이너. 상태 진입/이탈 시 태그를 추가/제거한다.</summary>
        public GameplayTagContainer Tags { get; private set; }

        /// <summary>
        /// 액터 개별 타임 스케일 (기본 1.0)
        /// </summary>
        public float LocalTimeScale
        {
            get => _localTimeScale;
            set
            {
                _localTimeScale = value;
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

        public virtual ActorAnimator Animator => _animator;
        public BaseMoveAnimType MoveAnimType { get; set; } = BaseMoveAnimType.Run;

        public ActorType ActorType => _actorType;
        public CharacterActorType CharacterType => _characterActorType;

        /// <summary>런타임 고유 ID. 스폰 시 ActorDefinitionSO에서 주입되거나 Inspector에서 직접 설정.</summary>
        public string ActorId => _actorId;

        /// <summary>이 액터를 정의하는 ScriptableObject. 런타임 스폰 시 주입됨.</summary>
        public ActorDefinitionSO Definition => _definition;

        /// <summary>
        /// 플래그 조합 체크. 예: actor.HasActorType(ActorType.Talkable)
        /// </summary>
        public bool HasActorType(ActorType flag) => (_actorType & flag) != 0;

        /// <summary>
        /// ActorDefinitionSO를 주입한다. ActorSpawnManager가 스폰 직후 호출.
        /// 서브클래스에서 오버라이드해 스탯 등 추가 적용 가능.
        /// </summary>
        public virtual void SetDefinition(ActorDefinitionSO definition)
        {
            _definition = definition;
            if (definition != null && !string.IsNullOrEmpty(definition.actorId))
                _actorId = definition.actorId;
        }

        public ActorMovementController ActorController => MovementController;
        
        protected virtual void Awake()
        {
            Tags = gameObject.GetOrAddComponent<GameplayTagContainer>();
            MovementController = GetComponent<ActorMovementController>();
            _animator = GetComponentInChildren<ActorAnimator>();
            if (_animator != null)
                _animator.Init(this);
            
            _colorChanger = gameObject.GetOrAddComponent<ActorColorChanger>();
            _dissolveController = gameObject.GetOrAddComponent<DissolveController>();
            
            // 매니저에 등록
            GameObjectManager.Instance?.RegisterActor(this);
        }

        
        protected virtual void OnDestroy()
        {
            // 매니저에서 제거
            GameObjectManager.Instance?.UnregisterActor(this);
        }
        
        protected virtual void Start()
        {
            // SpawnActor를 거치지 않고 생성된 Actor(스킬 소환 등)를 추적 목록에 등록.
            // AfterInit 스캔 또는 SpawnActor에서 이미 등록된 경우 무시된다.
            if (!string.IsNullOrEmpty(_actorId))
                ActorSpawnManager.Instance?.RegisterActor(this);
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