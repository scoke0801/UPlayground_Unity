using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Animation;

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
        private static readonly List<SwapResidualAttackRunner> ActiveRunners = new();

        private ActorAnimator _animator;
        private ResidualPlayerCombat _combat;
        private GameObject _modelInstance;
        private float _maxLifetime = 1.8f;
        private float _elapsed;
        private bool _isCancelling;

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
            _maxLifetime = Mathf.Max(0.1f, request.MaxLifetime);

            _modelInstance = Instantiate(snapshot.SourceModel.gameObject, snapshot.Position, snapshot.Rotation, transform);
            _modelInstance.name = $"{snapshot.SourceModel.name}_Residual";
            _modelInstance.SetActive(true);

            _combat = gameObject.AddComponent<ResidualPlayerCombat>();
            _combat.Initialize(snapshot, request.AllowHitStop);

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

            _animator.ApplyRootMotion(request.UseRootMotion);
            _animator.OnMotionSetCompleted += OnMotionSetCompleted;
            if (!_animator.RestorePlaybackSnapshot(snapshot.PlaybackSnapshot))
            {
                Debug.LogWarning($"[ResidualAttack] Runner cancelled: restore playback failed. animKey={snapshot.PlaybackSnapshot.Key}");
                Cancel(SwapResidualAttackCancelReason.Cancelled);
            }
            else
            {
                Debug.Log($"[ResidualAttack] Playback restored. animKey={snapshot.PlaybackSnapshot.Key}, lifetime={_maxLifetime}, rootMotion={request.UseRootMotion}");
            }
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            if (_elapsed >= _maxLifetime)
                Cancel(SwapResidualAttackCancelReason.Timeout);
        }

        public void Cancel(SwapResidualAttackCancelReason reason)
        {
            if (_isCancelling) return;
            _isCancelling = true;

            Debug.Log($"[ResidualAttack] Runner cancel. reason={reason}, elapsed={_elapsed:0.000}, object={name}");

            if (_combat != null)
                _combat.SetEnableCollision(false);

            if (_animator != null)
            {
                _animator.OnMotionSetCompleted -= OnMotionSetCompleted;
                _animator.StopMotionSet();
            }

            ActiveRunners.Remove(this);
            Destroy(gameObject);
        }

        private void OnMotionSetCompleted() => Cancel(SwapResidualAttackCancelReason.Completed);

        private void OnDestroy()
        {
            ActiveRunners.Remove(this);
            if (_animator != null)
                _animator.OnMotionSetCompleted -= OnMotionSetCompleted;
        }
    }
}
