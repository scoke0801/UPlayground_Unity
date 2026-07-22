using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.Components
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
    public sealed class SwapResidualAttackRunner : MonoBehaviour, IResidualMotionWarpTarget
    {
        private static readonly List<SwapResidualAttackRunner> AllRunners = new();
        private static readonly List<SwapResidualAttackRunner> ActiveRunners = new();

        private const float DefaultWarpFallbackMaxSpeed = 22f;

        private ActorAnimator _animator;
        private ResidualPlayerCombat _combat;
        private MotionWarpController _motionWarp;
        private System.Action _endMotionWarpAction;
        private DissolveController _dissolveController;
        private GameObject _modelInstance;
        private float _maxLifetime = 1.8f;
        private float _minVisibleLifetime = 0.45f;
        private float _fadeOutDuration = 0.55f;
        private bool _useRootMotion;
        private float _rootMotionMaxDistance;
        private LayerMask _rootMotionBlocker;
        private float _warpFallbackMaxSpeed = DefaultWarpFallbackMaxSpeed;
        private float _rootMotionDistance;
        private float _elapsed;
        private bool _isCancelling;
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
                Debug.LogWarning($"[ResidualAttack] Runner spawn aborted. character={request.Snapshot.CharacterType}, motion={request.Snapshot.PlaybackSnapshot.DisplayKey}");
                return null;
            }

            ActiveRunners.Add(runner);
            Debug.Log($"[ResidualAttack] Runner spawned. character={request.Snapshot.CharacterType}, motion={request.Snapshot.PlaybackSnapshot.DisplayKey}, activeCount={ActiveRunners.Count}, position={request.Snapshot.Position}");
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
            _warpFallbackMaxSpeed = DefaultWarpFallbackMaxSpeed;

            _modelInstance = Instantiate(snapshot.SourceModel.gameObject, snapshot.Position, snapshot.Rotation, transform);
            _modelInstance.name = $"{snapshot.SourceModel.name}_Residual";
            _modelInstance.SetActive(true);
            PrepareResidualVisuals();
            _dissolveController = gameObject.GetOrAddComponent<DissolveController>();
            _dissolveController.SetDissolveColor(request.DissolveColor);
            if (request.DissolveNoiseMask != null)
                _dissolveController.SetDissolveNoise(request.DissolveNoiseMask, request.DissolveNoiseStrength, request.DissolveNoiseScrollRotate);
            _dissolveController.RefreshRenderers();
            _dissolveController.WarmupDissolveMaterials();

            _combat = gameObject.AddComponent<ResidualPlayerCombat>();
            _combat.Initialize(
                snapshot,
                request.AllowHitStop,
                request.FeedbackMinInterval,
                request.HitStopDuration,
                request.HitStopTimeScale,
                request.ShowCharacterOnDamageFloater);
            _motionWarp = gameObject.GetOrAddComponent<MotionWarpController>();
            // 매 프레임 메서드 그룹 → delegate 변환(GC 할당) 방지용 캐시
            _endMotionWarpAction = _motionWarp.EndMotionWarp;
            ResolveFallbackWarpTarget(MotionWarpController.DefaultTargetKey, useSnapshot: false);

            _animator = _modelInstance.GetComponentInChildren<ActorAnimator>(true);
            if (_animator == null)
            {
                Debug.LogWarning($"[ResidualAttack] Runner cancelled: ActorAnimator missing. model={_modelInstance.name}");
                Cancel(SwapResidualAttackCancelReason.Cancelled);
                return;
            }

            _animator.Init(snapshot.OwnerPlayer);
            bool hasMotion = snapshot.PlaybackSnapshot.SourceAsset != null
                ? _animator.HasMotion(snapshot.PlaybackSnapshot.SourceAsset)
                : _animator.HasMotion(snapshot.PlaybackSnapshot.Slot, true);
            Debug.Log($"[ResidualAttack] Residual animator initialized. animator={_animator.name}, owner={snapshot.OwnerPlayer.name}, hasMotion={hasMotion}");

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

            // 수동 루트모션/잔류 워프(ApplyRootMotionDelta)가 DeltaPosition을 읽으려면 applyRootMotion=true 필요.
            // ActorAnimator가 OnAnimatorMove를 구현하므로 Unity가 자동 적용하지 않고 델타만 노출한다.
            _animator.ApplyRootMotion(true);
            _animator.OnMotionSetCompleted += OnMotionSetCompleted;
            if (!_animator.RestorePlaybackSnapshot(snapshot.PlaybackSnapshot))
            {
                Debug.LogWarning($"[ResidualAttack] Runner cancelled: restore playback failed. motion={snapshot.PlaybackSnapshot.DisplayKey}");
                Cancel(SwapResidualAttackCancelReason.Cancelled);
            }
            else
            {
                ActorWeaponTrailController.StartAttackTrails(_modelInstance.transform);
                Debug.Log($"[ResidualAttack] Playback restored. motion={snapshot.PlaybackSnapshot.DisplayKey}, lifetime={_maxLifetime}, rootMotion={request.UseRootMotion}");
            }
        }

        private void Update()
        {
            if (_isCancelling)
                return;

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
            if (_animator == null)
                return;

            Vector3 delta = _animator.DeltaPosition;
            bool isWarping = _motionWarp != null && _motionWarp.IsMotionWarping;
            bool hasWarpTarget = _motionWarp != null && _motionWarp.HasTarget;
            if (!_useRootMotion && !isWarping)
            {
                if (hasWarpTarget)
                    TrackWarpTargetRotation(Time.deltaTime);
                return;
            }

            delta.y = 0f;
            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
                return;
            if (!isWarping && delta.sqrMagnitude <= 0.000001f)
                return;

            if (isWarping)
            {
                Vector3 velocity = delta / deltaTime;
                float fallbackMaxDistance = _rootMotionMaxDistance > 0f ? _rootMotionMaxDistance : 7f;
                velocity = _motionWarp.EvaluateVelocity(
                    velocity,
                    transform.position,
                    true,
                    _motionWarp.WarpRemainingTime,
                    _motionWarp.WarpDuration,
                    0f,
                    fallbackMaxDistance,
                    _warpFallbackMaxSpeed,
                    deltaTime,
                    _endMotionWarpAction);
                velocity = _motionWarp.ClampApproachVelocity(velocity, transform.position, deltaTime);
                delta = velocity * deltaTime;

                Quaternion currentRotation = transform.rotation;
                if (_motionWarp.TryEvaluateRotation(
                        currentRotation,
                        transform.position,
                        true,
                        _motionWarp.WarpRemainingTime,
                        _motionWarp.WarpDuration,
                        0f,
                        fallbackMaxDistance,
                        _warpFallbackMaxSpeed,
                        out Quaternion warpedRotation))
                {
                    transform.rotation = warpedRotation;
                }
            }
            else if (hasWarpTarget)
            {
                Vector3 velocity = delta / deltaTime;
                velocity = _motionWarp.ClampApproachVelocity(velocity, transform.position, deltaTime);
                delta = velocity * deltaTime;
                TrackWarpTargetRotation(deltaTime);
            }

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

        public WarpResolverContext BuildWarpResolverContext()
        {
            return _combat != null ? _combat.BuildWarpResolverContext() : default;
        }

        public void SetResidualMotionWarpTarget(string key, Transform target, bool useSnapshot)
        {
            EnsureMotionWarp();
            _motionWarp.SetTarget(key, target, useSnapshot);
        }

        public void BeginResidualMotionWarp(MotionWarpWindowSettings settings, string key)
        {
            EnsureMotionWarp();
            string useKey = string.IsNullOrEmpty(key) ? MotionWarpController.DefaultTargetKey : key;
            if (!_motionWarp.GetTarget(useKey).IsValid)
                ResolveFallbackWarpTarget(useKey, settings.targetPolicy == MotionWarpTargetPolicy.Snapshot);

            _motionWarp.BeginWarpWindow(settings, useKey);
            _motionWarp.BeginMotionWarp(settings.duration);
            if (settings.overrideDistance && settings.maxSpeed > 0f)
                _warpFallbackMaxSpeed = settings.maxSpeed;
        }

        public void EndResidualMotionWarp()
        {
            if (_motionWarp == null) return;

            _motionWarp.EndWarpWindow();
            _motionWarp.EndMotionWarp();
        }

        private void EnsureMotionWarp()
        {
            if (_motionWarp == null)
                _motionWarp = gameObject.GetOrAddComponent<MotionWarpController>();
        }

        private void ResolveFallbackWarpTarget(string key, bool useSnapshot)
        {
            WarpResolverContext context = BuildWarpResolverContext();
            if (context.origin == null)
                return;

            Transform resolved = HybridResolver.Instance.Resolve(in context);
            if (resolved != null)
                _motionWarp.SetTarget(key, resolved, useSnapshot);
        }

        private void TrackWarpTargetRotation(float deltaTime)
        {
            if (_motionWarp == null || !_motionWarp.HasTarget || deltaTime <= 0f)
                return;

            Vector3 dir = _motionWarp.TargetPosition - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude <= 0.0001f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(dir.normalized);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                1f - Mathf.Exp(-15f * deltaTime));
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

            if (forceImmediate || _fadeOutDuration <= 0f || _dissolveController == null)
            {
                Destroy(gameObject);
                return;
            }

            _dissolveController.StartDissolve(_fadeOutDuration, destroyOnComplete: true);
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
