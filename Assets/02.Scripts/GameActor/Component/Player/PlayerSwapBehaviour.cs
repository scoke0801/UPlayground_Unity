using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.Manager;
using UPlayGround.MovementController;
using UPlayGround.Data.Party;

namespace UPlayGround.Components
{
    /// <summary>
    /// 단일 PlayerActor 하위의 Model 서브루트들을 관리한다.
    /// 교체 시 이전 Model을 비활성화하고 새 Model을 활성화한 뒤
    /// PlayerActor.RefreshForCharacter()를 호출해 컴포넌트들을 일괄 갱신한다.
    /// </summary>
    public class PlayerSwapBehaviour : MonoBehaviour
    {
        private enum CharacterSwitchPurpose
        {
            Gameplay,
            DialoguePresentation
        }

        private PlayerActor _playerActor;

        private readonly List<CharacterModelData> _models = new();
        private CharacterModelData       _activeModel;

        [Header("Model Streaming")]
        [Tooltip("런타임에 로드한 캐릭터 모델을 배치할 셸 하위 루트입니다.")]
        [SerializeField] private Transform _modelRoot;

        [Header("Swap FX")]
        [Tooltip("모델 교체 성공 시 Center 소켓 위치에 재생할 FX 키. 비워두면 재생하지 않는다.")]
        [SerializeField] private string _swapFxKey;
        [SerializeField] private float  _swapFxDuration = 5f;
        [SerializeField] private bool   _destroyPreviousSwapFx = true;
        [SerializeField] private float  _swapFxMinInterval = 0.05f;

        private GameObject _activeSwapFxInstance;
        private float      _lastSwapFxTime = -999f;

        [Header("Evade Afterimage")]
        [Tooltip("Dodge/Dash 회피 성공 시 이동 시작 위치에 현재 캐릭터 외형 잔상을 표시한다.")]
        [SerializeField] private bool _enableEvadeAfterimage = true;
        [Range(0f, 1f)]
        [SerializeField] private float _evadeAfterimageAlpha = 0.55f;
        [Min(0f)]
        [SerializeField] private float _evadeAfterimageHoldDuration = 0.05f;
        [Min(0f)]
        [SerializeField] private float _evadeAfterimageFadeOutDuration = 0.5f;
        [SerializeField] private Color _evadeAfterimageTint = Color.white;

        private GameObject _evadeAfterimageHost;
        private Transform _evadeAfterimageSource;
        private Vector3 _evadeAfterimagePosition;
        private Quaternion _evadeAfterimageRotation;
        private bool _hasEvadeAfterimageStart;

        public CharacterActorType ActiveCharacterType =>
            _activeModel?.characterType ?? CharacterActorType.None;
        public bool IsVisualReady => _activeModel != null;
        public Transform ModelRoot => _modelRoot != null ? _modelRoot : transform;

        private void Awake()
        {
            _playerActor = GetComponent<PlayerActor>();
            CharacterModelData[] embeddedModels =
                GetComponentsInChildren<CharacterModelData>(includeInactive: true);
            for (int i = 0; i < embeddedModels.Length; i++)
                RegisterModel(embeddedModels[i]);

            if (_models.Count == 0)
                UPlayGround.Diagnostics.RuntimeLog.Trace(
                    UPlayGround.Diagnostics.RuntimeLogCategory.Player,
                    $"[PlayerSwapBehaviour] {name}: 스트리밍 모델 준비를 기다립니다.",
                    this);
        }

        /// <summary>
        /// PartyManager.AfterInit에서 호출. 첫 번째 파티 구성으로 초기화한다.
        /// </summary>
        public bool InitializeTo(CharacterActorType type)
        {
            for (int i = 0; i < _models.Count; i++)
                if (_models[i] != null)
                    _models[i].gameObject.SetActive(false);

            CharacterModelData target = GetModelData(type);

            if (target == null)
            {
                Debug.LogError(
                    $"[PlayerSwapBehaviour] CharacterType={type} 모델이 준비되지 않았습니다.",
                    this);
                return false;
            }

            _activeModel = target;
            _activeModel.gameObject.SetActive(true);
            ResetModelInteractionEquipment(_activeModel);
            _playerActor.RefreshForCharacter(_activeModel);
            return true;
        }

        /// <summary>
        /// 지정한 캐릭터 타입으로 교체한다.
        /// </summary>
        public bool SwapTo(
            CharacterActorType type,
            bool preserveAnimation = true,
            bool spawnResidualAttack = true)
            => SwitchTo(
                type,
                preserveAnimation,
                spawnResidualAttack,
                CharacterSwitchPurpose.Gameplay);

        /// <summary>
        /// 대화 연출용으로 외형만 교체한다. 잔류 공격·교체 FX·전투 카메라 상태 보존은 발생시키지 않는다.
        /// </summary>
        public bool ShowForDialogue(CharacterActorType type)
            => SwitchTo(
                type,
                preserveAnimation: false,
                spawnResidualAttack: false,
                CharacterSwitchPurpose.DialoguePresentation);

        private bool SwitchTo(
            CharacterActorType type,
            bool preserveAnimation,
            bool spawnResidualAttack,
            CharacterSwitchPurpose purpose)
        {
            if (_activeModel?.characterType == type)
            {
                if (purpose == CharacterSwitchPurpose.Gameplay)
                    Debug.Log($"[ResidualAttack] Swap skipped: already active. character={type}");
                return false;
            }

            CharacterModelData target = GetModelData(type);
            if (target == null)
            {
                Debug.LogWarning($"[PlayerSwapBehaviour] CharacterType={type} 모델 없음.");
                return false;
            }

            ActorAnimator.MotionPlaybackSnapshot animationSnapshot = preserveAnimation
                ? _playerActor?.Animator?.CaptureMovementPlaybackSnapshot()
                  ?? ActorAnimator.MotionPlaybackSnapshot.Empty
                : ActorAnimator.MotionPlaybackSnapshot.Empty;
            if (purpose == CharacterSwitchPurpose.Gameplay)
            {
                bool wasInCombat = _playerActor?.GetCombat()?.IsInCombat ?? false;
                CameraManager.Instance?.PreserveCombatStateForCharacterSwap(wasInCombat);
            }

            if (spawnResidualAttack)
            {
                TryReturnToResidualRunner(type);
                SwapResidualAttackRunner.CancelRunnersForCharacter(type);
                TrySpawnResidualAttack(_activeModel);
            }

            StopOutgoingModelPlayback(_activeModel);
            ResetModelInteractionEquipment(_activeModel);
            _activeModel?.gameObject.SetActive(false);
            _activeModel = target;
            _activeModel.gameObject.SetActive(true);
            ResetModelInteractionEquipment(_activeModel);
            _playerActor.RefreshForCharacter(_activeModel, animationSnapshot);
            CameraManager.Instance?.RefreshTargetReferences();
            if (purpose == CharacterSwitchPurpose.Gameplay)
                PlaySwapFx();
            return true;
        }

        /// <summary>
        /// 보유한 모든 캐릭터 타입 목록을 반환한다.
        /// </summary>
        public List<CharacterActorType> GetAllCharacterTypes()
        {
            var types = new List<CharacterActorType>(_models.Count);
            for (int i = 0; i < _models.Count; i++)
                if (_models[i] != null)
                    types.Add(_models[i].characterType);
            return types;
        }

        public CharacterModelData GetModelData(CharacterActorType type)
        {
            for (int i = 0; i < _models.Count; i++)
            {
                CharacterModelData model = _models[i];
                if (model != null && model.characterType == type)
                    return model;
            }

            return null;
        }

        /// <summary>로드된 모델을 교체 목록에 등록하고 비활성 대기 상태로 둔다.</summary>
        public bool RegisterModel(CharacterModelData model)
        {
            if (model == null || model.characterType == CharacterActorType.None)
                return false;

            CharacterModelData existing = GetModelData(model.characterType);
            if (existing != null)
                return existing == model;

            _models.Add(model);
            if (_activeModel != model)
                model.gameObject.SetActive(false);
            return true;
        }

        /// <summary>더 이상 상주하지 않는 비활성 모델을 교체 목록에서 해제한다.</summary>
        public bool UnregisterModel(CharacterModelData model)
        {
            if (model == null || model == _activeModel)
                return false;
            return _models.Remove(model);
        }

        /// <summary>
        /// 회피 진입 순간의 활성 캐릭터와 위치만 기록한다.
        /// 메시 Bake는 실제 퍼펙트 회피가 성립한 경우에만 수행한다.
        /// </summary>
        public void PrepareEvadeAfterimage()
        {
            CancelEvadeAfterimage();
            if (!_enableEvadeAfterimage || _activeModel == null)
                return;

            Transform source = _activeModel.transform;
            _evadeAfterimageSource = source;
            _evadeAfterimagePosition = source.position;
            _evadeAfterimageRotation = source.rotation;
            _hasEvadeAfterimageStart = true;
        }

        /// <summary>성공 시점의 포즈를 회피 시작 위치에 Bake하고 점진적으로 제거한다.</summary>
        public void RevealEvadeAfterimage()
        {
            if (!_hasEvadeAfterimageStart || _evadeAfterimageSource == null)
                return;

            if (_evadeAfterimageHost == null)
            {
                _evadeAfterimageHost = new GameObject("EvadeAfterimageRunner");
                _evadeAfterimageHost.transform.SetParent(transform, false);
            }

            AfterimageEvent.PlaySingleAt(
                _evadeAfterimageHost,
                _evadeAfterimageSource,
                _evadeAfterimagePosition,
                _evadeAfterimageRotation,
                _evadeAfterimageAlpha,
                _evadeAfterimageHoldDuration,
                _evadeAfterimageFadeOutDuration,
                _evadeAfterimageTint);
            ClearEvadeAfterimageStart();
        }

        /// <summary>Dodge/Dash 회피가 성립하지 않은 시작 위치 기록을 폐기한다.</summary>
        public void CancelEvadeAfterimage()
        {
            ClearEvadeAfterimageStart();
        }

        private void ClearEvadeAfterimageStart()
        {
            _evadeAfterimageSource = null;
            _hasEvadeAfterimageStart = false;
        }

        private void StopOutgoingModelPlayback(CharacterModelData sourceModel)
        {
            if (sourceModel == null) return;

            ActorWeaponTrailController.SuppressAttackTrails(sourceModel.transform);

            var animator = sourceModel.GetComponentInChildren<ActorAnimator>(includeInactive: true);
            animator?.StopMotionSet();
        }

        private static void ResetModelInteractionEquipment(CharacterModelData model)
        {
            if (model == null) return;

            var equipment = model.GetComponentInChildren<PlayerEquipment>(includeInactive: true);
            equipment?.ResetInteractionEquipmentImmediate();
        }

        private void TrySpawnResidualAttack(CharacterModelData sourceModel)
        {
            var partyManager = Svc.Party;
            if (partyManager != null && !partyManager.EnableResidualAttackOnSwap)
            {
                Debug.LogWarning("[ResidualAttack] Spawn skipped: PartyManager disabled residual attack.");
                return;
            }

            if (_playerActor == null || sourceModel == null)
            {
                Debug.LogWarning($"[ResidualAttack] Spawn skipped: actor/model missing. actor={_playerActor != null}, sourceModel={sourceModel != null}");
                return;
            }

            var combat = _playerActor.GetCombat();
            if (combat == null || !combat.TryCreateResidualAttackSnapshot(sourceModel, out var snapshot))
            {
                Debug.Log($"[ResidualAttack] Spawn skipped: snapshot unavailable. combat={combat != null}, sourceCharacter={sourceModel.characterType}");
                return;
            }

            float maxLifetime = partyManager != null ? partyManager.ResidualAttackMaxLifetime : 2.4f;
            float minVisibleLifetime = partyManager != null ? partyManager.ResidualAttackMinVisibleLifetime : 0.45f;
            float fadeOutDuration = partyManager != null ? partyManager.ResidualAttackFadeOutDuration : 0.55f;
            Color dissolveColor = partyManager != null ? partyManager.ResidualAttackDissolveColor : Color.white;
            Texture dissolveNoiseMask = partyManager != null ? partyManager.ResidualAttackDissolveNoiseMask : null;
            float dissolveNoiseStrength = partyManager != null ? partyManager.ResidualAttackDissolveNoiseStrength : 0.1f;
            Vector4 dissolveNoiseScrollRotate = partyManager != null ? partyManager.ResidualAttackDissolveNoiseScrollRotate : Vector4.zero;
            bool allowHitStop = partyManager == null || partyManager.ResidualAttackAllowHitStop;
            bool useRootMotion = partyManager != null && partyManager.ResidualAttackUseRootMotion;
            float rootMotionMaxDistance = partyManager != null ? partyManager.ResidualAttackRootMotionMaxDistance : 2.5f;
            LayerMask rootMotionBlocker = partyManager != null ? partyManager.ResidualAttackRootMotionBlocker : 0;
            float feedbackMinInterval = partyManager != null ? partyManager.ResidualAttackFeedbackMinInterval : 0.08f;
            float hitStopDuration = partyManager != null ? partyManager.ResidualAttackHitStopDuration : 0.04f;
            float hitStopTimeScale = partyManager != null ? partyManager.ResidualAttackHitStopTimeScale : 0.2f;
            bool showCharacterOnDamageFloater = partyManager != null && partyManager.ResidualAttackShowCharacterOnDamageFloater;
            int maxCount = partyManager != null ? partyManager.ResidualAttackMaxCount : 1;

            var request = new SwapResidualAttackRequest(
                snapshot,
                maxLifetime,
                minVisibleLifetime,
                fadeOutDuration,
                dissolveColor,
                dissolveNoiseMask,
                dissolveNoiseStrength,
                dissolveNoiseScrollRotate,
                allowHitStop,
                useRootMotion,
                rootMotionMaxDistance,
                rootMotionBlocker,
                feedbackMinInterval,
                hitStopDuration,
                hitStopTimeScale,
                showCharacterOnDamageFloater);
            Debug.Log($"[ResidualAttack] Spawn request. sourceCharacter={sourceModel.characterType}, motion={snapshot.PlaybackSnapshot.DisplayKey}, lifetime={maxLifetime}, minVisible={minVisibleLifetime}, fade={fadeOutDuration}, hitStop={allowHitStop}, rootMotion={useRootMotion}, maxCount={maxCount}");
            SwapResidualAttackRunner.Spawn(request, maxCount);
        }

        private void TryReturnToResidualRunner(CharacterActorType targetType)
        {
            var partyManager = Svc.Party;
            if (partyManager != null && !partyManager.ResidualAttackReturnToSameCharacterRunner)
                return;

            float maxAge = partyManager != null ? partyManager.ResidualAttackReturnPositionMaxAge : 1.8f;
            if (!SwapResidualAttackRunner.TryConsumeRunnerPosition(targetType, maxAge, out var position, out var rotation))
                return;

            var motor = _playerActor?.ActorController?.Motor;
            if (motor != null)
                motor.SetPositionAndRotation(position, rotation);
            else if (_playerActor != null)
                _playerActor.transform.SetPositionAndRotation(position, rotation);

            CameraManager.Instance?.SnapToTarget(position);
            Debug.Log($"[ResidualAttack] Returned to residual runner position. character={targetType}, position={position}");
        }

        private void PlaySwapFx()
        {
            if (string.IsNullOrWhiteSpace(_swapFxKey)) return;
            if (Time.time - _lastSwapFxTime < _swapFxMinInterval) return;

            Transform owner = _playerActor != null ? _playerActor.transform : transform;
            Vector3 position = owner.position;
            if (_activeModel != null
                && _activeModel.SocketDict != null
                && _activeModel.SocketDict.TryGetValue(ActorSocketType.Center, out Transform centerSocket)
                && centerSocket != null)
            {
                position = centerSocket.position;
            }

            if (_destroyPreviousSwapFx && _activeSwapFxInstance != null)
            {
                Destroy(_activeSwapFxInstance);
                _activeSwapFxInstance = null;
            }

            _activeSwapFxInstance = ActorSvc.Objects?.ShowFX(
                _swapFxKey,
                position,
                owner.rotation,
                null,
                _swapFxDuration);
            _lastSwapFxTime = Time.time;
        }
    }
}
