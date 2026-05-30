using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Component
{
    public enum SwapResidualAttackCancelReason
    {
        Completed,
        Timeout,
        Replaced,
        Cancelled,
    }

    /// <summary>
    /// 스왑으로 퇴장한 캐릭터 모델 복제본의 남은 공격 MotionSet과 충돌 이벤트를 실행한다.
    /// </summary>
    public sealed class SwapResidualAttackRunner : MonoBehaviour
    {
        private static readonly List<SwapResidualAttackRunner> AllRunners = new();
        private static readonly List<SwapResidualAttackRunner> ActiveRunners = new();

        private ActorAnimator _animator;
        private ResidualPlayerCombat _combat;
        private DissolveController _sourceDissolveController;
        private GameObject _modelInstance;
        private float _maxLifetime = 1.8f;
        private float _minVisibleLifetime = 0.45f;
        private float _fadeOutDuration;
        private bool _useRootMotion;
        private float _rootMotionMaxDistance;
        private LayerMask _rootMotionBlocker;
        private float _rootMotionDistance;
        private float _elapsed;
        private bool _isCancelling;
        private bool _isFinishing;
        private bool _hasDeferredCancel;
        private SwapResidualAttackCancelReason _deferredCancelReason;

        public CharacterActorType OwnerType { get; private set; }
        public float Elapsed => _elapsed;

        public static void CancelRunnersForCharacter(CharacterActorType characterType)
        {
            if (characterType == CharacterActorType.None)
                return;

            for (int i = ActiveRunners.Count - 1; i >= 0; i--)
            {
                var runner = ActiveRunners[i];
                if (runner != null && runner.OwnerType == characterType)
                    runner.Cancel(SwapResidualAttackCancelReason.Replaced);
            }
        }

        public static void CancelAll(SwapResidualAttackCancelReason reason = SwapResidualAttackCancelReason.Cancelled)
        {
            for (int i = AllRunners.Count - 1; i >= 0; i--)
                AllRunners[i]?.Cancel(reason, forceImmediate: true);
        }

        public static bool TryConsumeRunnerPosition(
            CharacterActorType characterType,
            float maxAge,
            out Vector3 position,
            out Quaternion rotation)
        {
            position = default;
            rotation = default;

            if (characterType == CharacterActorType.None)
                return false;

            for (int i = ActiveRunners.Count - 1; i >= 0; i--)
            {
                var runner = ActiveRunners[i];
                if (runner == null || runner.OwnerType != characterType)
                    continue;

                if (maxAge > 0f && runner.Elapsed > maxAge)
                    continue;

                position = runner.transform.position;
                rotation = runner.transform.rotation;
                runner.Cancel(SwapResidualAttackCancelReason.Replaced);
                return true;
            }

            return false;
        }

        public static SwapResidualAttackRunner Spawn(SwapResidualAttackRequest request, int maxCount)
        {
            if (request.Snapshot.SourceModel == null || request.Snapshot.OwnerPlayer == null)
            {
                Debug.LogWarning($"[ResidualAttack] Runner spawn failed: snapshot reference missing. sourceModel={request.Snapshot.SourceModel != null}, owner={request.Snapshot.OwnerPlayer != null}");
                return null;
            }

            int safeMaxCount = Mathf.Max(1, maxCount);
            while (ActiveRunners.Count >= safeMaxCount)
            {
                Debug.Log($"[ResidualAttack] Runner replaced. activeCount={ActiveRunners.Count}, maxCount={safeMaxCount}");
                ActiveRunners[0].Cancel(SwapResidualAttackCancelReason.Replaced);
            }

            var root = new GameObject($"ResidualAttack_{request.Snapshot.CharacterType}");
            root.transform.SetPositionAndRotation(request.Snapshot.Position, request.Snapshot.Rotation);

            var runner = root.AddComponent<SwapResidualAttackRunner>();
            AllRunners.Add(runner);
            runner.Initialize(request);
            if (runner._isCancelling)
            {
                Debug.LogWarning($"[ResidualAttack] Runner spawn aborted. character={request.Snapshot.CharacterType}, animKey={request.Snapshot.PlaybackSnapshot.Key}");
                return null;
            }

            ActiveRunners.Add(runner);
            Debug.Log($"[ResidualAttack] Runner spawned. character={request.Snapshot.CharacterType}, animKey={request.Snapshot.PlaybackSnapshot.Key}, activeCount={ActiveRunners.Count}, position={request.Snapshot.Position}");
            return runner;
        }

        public void Initialize(SwapResidualAttackRequest request)
        {
            var snapshot = request.Snapshot;
            OwnerType = snapshot.CharacterType;
            _maxLifetime = Mathf.Max(0.1f, request.MaxLifetime);
            _minVisibleLifetime = Mathf.Min(_maxLifetime, Mathf.Max(0f, request.MinVisibleLifetime));
            _fadeOutDuration = Mathf.Max(0f, request.FadeOutDuration);
            _useRootMotion = request.UseRootMotion;
            _rootMotionMaxDistance = Mathf.Max(0f, request.RootMotionMaxDistance);
            _rootMotionBlocker = request.RootMotionBlocker;
            _sourceDissolveController = snapshot.SourceModel.GetComponentInParent<DissolveController>();

            _modelInstance = Instantiate(snapshot.SourceModel.gameObject, snapshot.Position, snapshot.Rotation, transform);
            _modelInstance.name = $"{snapshot.SourceModel.name}_Residual";
            _modelInstance.SetActive(true);
            PrepareResidualVisuals();

            _combat = gameObject.AddComponent<ResidualPlayerCombat>();
            _combat.Initialize(
                snapshot,
                request.AllowHitStop,
                request.FeedbackMinInterval,
                request.HitStopDuration,
                request.HitStopTimeScale,
                request.ShowCharacterOnDamageFloater);

            _animator = _modelInstance.GetComponentInChildren<ActorAnimator>(true);
            if (_animator == null)
            {
                Debug.LogWarning($"[ResidualAttack] Runner cancelled: ActorAnimator missing. model={_modelInstance.name}");
                Cancel(SwapResidualAttackCancelReason.Cancelled);
                return;
            }

            _animator.Init(snapshot.OwnerPlayer);
            Debug.Log($"[ResidualAttack] Residual animator initialized. animator={_animator.name}, owner={snapshot.OwnerPlayer.name}, hasMotion={_animator.HasMotion(snapshot.PlaybackSnapshot.Key, true)}");

            var executor = _animator.GetComponent<MotionEventExecutor>();
            if (executor != null)
            {
                executor.SetTargetObject(gameObject);
                Debug.Log($"[ResidualAttack] MotionEventExecutor routed to runner root. animator={_animator.name}, target={gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"[ResidualAttack] MotionEventExecutor missing on animator. animator={_animator.name}");
            }

            // 수동 루트모션(ApplyRootMotionDelta)이 DeltaPosition을 읽으려면 applyRootMotion=true 필요.
            // ActorAnimator가 OnAnimatorMove를 구현하므로 Unity가 자동 적용하지 않고 델타만 노출한다.
            _animator.ApplyRootMotion(_useRootMotion);
            _animator.OnMotionSetCompleted += OnMotionSetCompleted;
            if (!_animator.RestorePlaybackSnapshot(snapshot.PlaybackSnapshot))
            {
                Debug.LogWarning($"[ResidualAttack] Runner cancelled: restore playback failed. animKey={snapshot.PlaybackSnapshot.Key}");
                Cancel(SwapResidualAttackCancelReason.Cancelled);
            }
            else
            {
                ActorWeaponTrailController.StartAttackTrails(_modelInstance.transform);
                Debug.Log($"[ResidualAttack] Playback restored. animKey={snapshot.PlaybackSnapshot.Key}, lifetime={_maxLifetime}, rootMotion={request.UseRootMotion}");
            }
        }

        private void Update()
        {
            ApplyRootMotionDelta();

            _elapsed += Time.deltaTime;
            if (_hasDeferredCancel && _elapsed >= _minVisibleLifetime)
            {
                _hasDeferredCancel = false;
                Cancel(_deferredCancelReason);
                return;
            }

            if (_elapsed >= _maxLifetime)
                Cancel(SwapResidualAttackCancelReason.Timeout);
        }

        private void ApplyRootMotionDelta()
        {
            if (!_useRootMotion || _animator == null)
                return;

            Vector3 delta = _animator.DeltaPosition;
            delta.y = 0f;
            if (delta.sqrMagnitude <= 0.000001f)
                return;

            if (_rootMotionMaxDistance > 0f)
            {
                float remaining = _rootMotionMaxDistance - _rootMotionDistance;
                if (remaining <= 0f)
                    return;

                float magnitude = delta.magnitude;
                if (magnitude > remaining)
                    delta = delta.normalized * remaining;
            }

            Vector3 start = transform.position;
            Vector3 desired = start + delta;
            if (_rootMotionBlocker.value != 0 &&
                Physics.SphereCast(start + Vector3.up * 0.5f, 0.25f, delta.normalized, out var hit, delta.magnitude, _rootMotionBlocker, QueryTriggerInteraction.Ignore))
            {
                float safeDistance = Mathf.Max(0f, hit.distance - 0.05f);
                desired = start + delta.normalized * safeDistance;
            }

            Vector3 applied = desired - start;
            transform.position = desired;
            _rootMotionDistance += applied.magnitude;
        }

        public void Cancel(SwapResidualAttackCancelReason reason) => Cancel(reason, forceImmediate: false);

        private void Cancel(SwapResidualAttackCancelReason reason, bool forceImmediate)
        {
            if (_isCancelling && !forceImmediate) return;

            if (!forceImmediate
                && reason == SwapResidualAttackCancelReason.Completed
                && _elapsed < _minVisibleLifetime)
            {
                _hasDeferredCancel = true;
                _deferredCancelReason = reason;
                Debug.Log($"[ResidualAttack] Runner completion deferred. elapsed={_elapsed:0.000}, minVisible={_minVisibleLifetime:0.000}, object={name}");
                return;
            }

            _isCancelling = true;

            Debug.Log($"[ResidualAttack] Runner cancel. reason={reason}, elapsed={_elapsed:0.000}, minVisible={_minVisibleLifetime:0.000}, object={name}");

            if (_combat != null)
                _combat.SetEnableCollision(false);

            if (_modelInstance != null)
                ActorWeaponTrailController.StopAttackTrails(_modelInstance.transform);

            if (_animator != null)
            {
                _animator.OnMotionSetCompleted -= OnMotionSetCompleted;
                _animator.StopMotionSet();
            }

            ActiveRunners.Remove(this);

            bool canFade = (reason is SwapResidualAttackCancelReason.Completed or SwapResidualAttackCancelReason.Timeout)
                           && !forceImmediate
                           && _fadeOutDuration > 0f;
            if (canFade)
                FinishWithDissolve();
            else
                Destroy(gameObject);
        }

        private void PrepareResidualVisuals()
        {
            if (_modelInstance == null) return;

            var renderers = _modelInstance.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;

                renderer.enabled = true;
                renderer.SetPropertyBlock(null);
            }
        }

        private void FinishWithDissolve()
        {
            if (_isFinishing) return;
            _isFinishing = true;

            var dissolve = gameObject.GetComponent<DissolveController>()
                           ?? gameObject.AddComponent<DissolveController>();
            
            dissolve.RefreshRenderers();
            dissolve.StartDissolve(_fadeOutDuration);
        }

        private void OnMotionSetCompleted() => Cancel(SwapResidualAttackCancelReason.Completed);

        private void OnDestroy()
        {
            AllRunners.Remove(this);
            ActiveRunners.Remove(this);
            if (_animator != null)
                _animator.OnMotionSetCompleted -= OnMotionSetCompleted;
        }
    }
}
