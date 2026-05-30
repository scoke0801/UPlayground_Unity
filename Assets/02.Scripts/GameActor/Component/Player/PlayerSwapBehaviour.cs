using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;
using UPlayGround.MovementController;

namespace UPlayGround.Component
{
    /// <summary>
    /// 단일 PlayerActor 하위의 Model 서브루트들을 관리한다.
    /// 교체 시 이전 Model을 비활성화하고 새 Model을 활성화한 뒤
    /// PlayerActor.RefreshForCharacter()를 호출해 컴포넌트들을 일괄 갱신한다.
    /// </summary>
    public class PlayerSwapBehaviour : MonoBehaviour
    {
        private PlayerActor _playerActor;

        private List<CharacterModelData> _models = new();
        private CharacterModelData       _activeModel;

        [Header("Swap FX")]
        [Tooltip("모델 교체 성공 시 Center 소켓 위치에 재생할 FX 키. 비워두면 재생하지 않는다.")]
        [SerializeField] private string _swapFxKey;
        [SerializeField] private float  _swapFxDuration = 5f;
        [SerializeField] private bool   _destroyPreviousSwapFx = true;
        [SerializeField] private float  _swapFxMinInterval = 0.05f;

        private GameObject _activeSwapFxInstance;
        private float      _lastSwapFxTime = -999f;

        public CharacterActorType ActiveCharacterType =>
            _activeModel?.characterType ?? CharacterActorType.None;

        private void Awake()
        {
            _playerActor = GetComponent<PlayerActor>();
            _models.AddRange(GetComponentsInChildren<CharacterModelData>(includeInactive: true));

            if (_models.Count == 0)
                Debug.LogWarning($"[PlayerSwapBehaviour] {name}: CharacterModelData를 찾을 수 없습니다.");
        }

        /// <summary>
        /// PartyManager.AfterInit에서 호출. 첫 번째 파티 구성으로 초기화한다.
        /// </summary>
        public void InitializeTo(CharacterActorType type)
        {
            foreach (var m in _models)
                m.gameObject.SetActive(false);

            var target = _models.Find(m => m.characterType == type);
            if (target == null && _models.Count > 0)
            {
                Debug.LogWarning($"[PlayerSwapBehaviour] CharacterType={type} 모델 없음. 첫 번째로 대체.");
                target = _models[0];
            }

            if (target == null) return;

            _activeModel = target;
            _activeModel.gameObject.SetActive(true);
            _playerActor.RefreshForCharacter(_activeModel);
        }

        /// <summary>
        /// 지정한 캐릭터 타입으로 교체한다.
        /// </summary>
        public bool SwapTo(CharacterActorType type)
        {
            if (_activeModel?.characterType == type)
            {
                Debug.Log($"[ResidualAttack] Swap skipped: already active. character={type}");
                return false;
            }

            var target = _models.Find(m => m.characterType == type);
            if (target == null)
            {
                Debug.LogWarning($"[PlayerSwapBehaviour] CharacterType={type} 모델 없음.");
                return false;
            }

            ActorAnimator.MotionPlaybackSnapshot animationSnapshot =
                _playerActor?.Animator?.CaptureMovementPlaybackSnapshot()
                ?? ActorAnimator.MotionPlaybackSnapshot.Empty;

            TryReturnToResidualRunner(type);
            SwapResidualAttackRunner.CancelRunnersForCharacter(type);
            TrySpawnResidualAttack(_activeModel);

            _activeModel?.gameObject.SetActive(false);
            _activeModel = target;
            _activeModel.gameObject.SetActive(true);
            _playerActor.RefreshForCharacter(_activeModel, animationSnapshot);
            PlaySwapFx();
            return true;
        }

        /// <summary>
        /// 보유한 모든 캐릭터 타입 목록을 반환한다.
        /// </summary>
        public List<CharacterActorType> GetAllCharacterTypes()
        {
            var types = new List<CharacterActorType>(_models.Count);
            foreach (var m in _models)
                types.Add(m.characterType);
            return types;
        }

        public CharacterModelData GetModelData(CharacterActorType type)
            => _models.Find(m => m.characterType == type);

        private void TrySpawnResidualAttack(CharacterModelData sourceModel)
        {
            var partyManager = PartyManager.Instance;
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
                allowHitStop,
                useRootMotion,
                rootMotionMaxDistance,
                rootMotionBlocker,
                feedbackMinInterval,
                hitStopDuration,
                hitStopTimeScale,
                showCharacterOnDamageFloater);
            Debug.Log($"[ResidualAttack] Spawn request. sourceCharacter={sourceModel.characterType}, animKey={snapshot.PlaybackSnapshot.Key}, lifetime={maxLifetime}, minVisible={minVisibleLifetime}, fade={fadeOutDuration}, hitStop={allowHitStop}, rootMotion={useRootMotion}, maxCount={maxCount}");
            SwapResidualAttackRunner.Spawn(request, maxCount);
        }

        private void TryReturnToResidualRunner(CharacterActorType targetType)
        {
            var partyManager = PartyManager.Instance;
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

            _activeSwapFxInstance = GameObjectManager.Instance?.ShowFX(
                _swapFxKey,
                position,
                owner.rotation,
                null,
                _swapFxDuration);
            _lastSwapFxTime = Time.time;
        }
    }
}
