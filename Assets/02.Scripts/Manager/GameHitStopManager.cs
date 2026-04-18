using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UPlayGround.Manager.Handler
{
    /// <summary>
    /// HitStop(타격 정지) 효과 관리.
    ///
    /// timeScale 요청 모델:
    ///   Execute()는 GameTimeManager에 id 기반 요청을 등록한다.
    ///   duration 종료 후 해당 id만 Release — 더 강한 다른 효과가 살아있으면
    ///   timeScale은 그 효과의 값을 유지한다.
    ///
    ///   강도 비교:
    ///   새 요청의 scale < 현재 활성 scale → 더 강한 효과이므로 현재 것을 교체
    ///   새 요청의 scale ≥ 현재 활성 scale → 더 약하므로 큐에만 추가 (현재 scale 유지)
    /// </summary>
    public class GameHitStopManager : BaseManager<GameHitStopManager>, IManager
    {
        public enum HitStopIntensity
        {
            Light,       // 0.05s  scale=0.15
            Medium,      // 0.08s  scale=0.10
            Heavy,       // 0.12s  scale=0.05
            Critical,    // 0.15s  scale=0.02
            PlayerDie,   // 1.00s  scale=0.02
            PlayerGuard, // actor-only
        }

        [Header("HitStop Settings")]
        [SerializeField] private float _defaultHitStopDuration = 0.08f;
        [SerializeField] private float _defaultTimeScale       = 0.1f;

        private AsyncOperationHandle<GameObject> _volumeHandle;
        private Volume     _volume;
        private GameObject _volumeInstance;

        private float _transitionTime  = 0.05f;
        private float _targetWeight    = 0f;
        private float _currentWeight   = 0f;
        private float _weightVelocity  = 0f;

        // 전역 HitStop: id → 코루틴. 복수 요청이 동시에 살아있을 수 있다.
        private readonly Dictionary<int, Coroutine> _globalCoroutines = new Dictionary<int, Coroutine>();

        // GameActor 단위 Animator 속도 조작
        private readonly Dictionary<GameActor, Coroutine> _actorCoroutines = new Dictionary<GameActor, Coroutine>();

        public bool IsHitStopping => GameTimeManager.Instance?.IsSlowed ?? false;

        #region IManager

        public void Init()
        {
            _actorCoroutines.Clear();
            _globalCoroutines.Clear();
            LoadVolume();
        }

        public void AfterInit() { }

        public void Dispose()
        {
            Stop();
            StopAllActors();

            if (_volumeInstance != null)
            {
                Destroy(_volumeInstance);
                _volumeInstance = null;
            }
            if (_volumeHandle.IsValid())
                Addressables.Release(_volumeHandle);
        }

        public void OnUpdate()
        {
            if (_volume == null) return;

            _currentWeight = Mathf.SmoothDamp(
                _currentWeight, _targetWeight,
                ref _weightVelocity, _transitionTime,
                Mathf.Infinity, Time.unscaledDeltaTime);

            _volume.weight = _currentWeight;
        }

        public void OnFixedUpdate() { }
        public void OnLateUpdate()  { }

        public void OnSceneChanged(string sceneType)
        {
            Stop();
            StopAllActors();
        }

        #endregion

        #region 전역 HitStop — 공개 API

        public void Execute() => Execute(_defaultHitStopDuration, _defaultTimeScale);

        public void Execute(HitStopIntensity intensity)
        {
            switch (intensity)
            {
                case HitStopIntensity.Light:      Execute(0.05f, 0.15f); break;
                case HitStopIntensity.Medium:     Execute(0.08f, 0.10f); break;
                case HitStopIntensity.Heavy:      Execute(0.12f, 0.05f); break;
                case HitStopIntensity.Critical:   Execute(0.15f, 0.02f); break;
                case HitStopIntensity.PlayerDie:  Execute(1.00f, 0.02f); break;
                case HitStopIntensity.PlayerGuard:
                    // timeScale은 건드리지 않고 Actor-only 슬로우만 적용
                    _targetWeight  = 0f;
                    _currentWeight = 1f;
                    _transitionTime = 3f;
                    GameObjectManager.Instance?.SetGlobalTimeScaleExceptPlayer(0.05f, 3f);
                    break;
            }
        }

        /// <summary>
        /// 커스텀 파라미터 HitStop.
        /// 더 강한 효과(scale 더 낮음)가 이미 있으면 해당 효과를 교체하고
        /// 현재 효과보다 약하면 큐에만 추가한다(현재 scale은 유지됨).
        /// </summary>
        public void Execute(float duration, float timeScale = 0.1f)
        {
            // 현재 활성 scale보다 강한(더 낮은) 요청이면 기존 것을 중단해서 교체
            if (ShouldReplaceExisting(timeScale))
                StopWeakerThan(timeScale);

            int id  = GameTimeManager.Instance.Request(timeScale);
            var co  = StartCoroutine(HitStopCoroutine(id, duration));
            _globalCoroutines[id] = co;
        }

        /// <summary>
        /// 모든 전역 HitStop 강제 종료.
        /// </summary>
        public void Stop()
        {
            foreach (var co in _globalCoroutines.Values)
                if (co != null) StopCoroutine(co);

            _globalCoroutines.Clear();
            GameTimeManager.Instance?.ReleaseAll();
        }

        /// <summary>
        /// ApplyHitFeedback에서 호출하던 ResetActorTimeScale 대체.
        /// 더 이상 timeScale을 강제 리셋하지 않는다 — 요청 큐가 알아서 관리.
        /// Volume 페이드만 처리한다.
        /// </summary>
        public void ResetActorTimeScale()
        {
            // 요청 큐 모델에서는 각 요청이 duration 종료 후 스스로 Release한다.
            // Volume 시각 효과와 PlayerGuard로 걸린 액터 타임스케일을 즉시 복원한다.
            _targetWeight   = 0f;
            _transitionTime = 0f;
            GameObjectManager.Instance?.ResetTimeScale();
        }

        #endregion

        #region Actor 단위 Animator 속도 조작

        public void ExecuteActorOnly(GameActor actor, float duration, float animSpeed = 0.1f)
        {
            if (actor == null) return;
            StopActor(actor);
            _actorCoroutines[actor] = StartCoroutine(ActorOnlyCoroutine(actor, duration, animSpeed));
        }

        public void StopActor(GameActor actor)
        {
            if (actor == null) return;
            if (!_actorCoroutines.TryGetValue(actor, out var co)) return;

            if (co != null) StopCoroutine(co);
            _actorCoroutines.Remove(actor);

            var anim = actor.Animator?.GetAnimator;
            if (anim != null) anim.speed = 1f;
        }

        public void StopAllActors()
        {
            foreach (var kvp in _actorCoroutines)
            {
                if (kvp.Value != null) StopCoroutine(kvp.Value);
                if (kvp.Key != null)
                {
                    var anim = kvp.Key.Animator?.GetAnimator;
                    if (anim != null) anim.speed = 1f;
                }
            }
            _actorCoroutines.Clear();
        }

        public bool IsActorHitStopping(GameActor actor) =>
            actor != null && _actorCoroutines.ContainsKey(actor);

        #endregion

        #region 내부

        /// <summary>
        /// 새 요청의 scale이 현재 활성 scale보다 낮으면(더 강하면) true.
        /// </summary>
        private bool ShouldReplaceExisting(float newScale)
        {
            float current = GameTimeManager.Instance?.IsSlowed == true
                ? Time.timeScale
                : 1f;
            return newScale < current;
        }

        /// <summary>
        /// 등록된 요청 중 newScale보다 약한(scale이 높은) 것들을 모두 중단한다.
        /// 더 강한 효과가 들어왔을 때 약한 요청의 잔여 시간을 정리하기 위함.
        /// </summary>
        private void StopWeakerThan(float newScale)
        {
            var toRemove = new List<int>();
            foreach (var id in _globalCoroutines.Keys)
                toRemove.Add(id);

            foreach (int id in toRemove)
            {
                if (_globalCoroutines.TryGetValue(id, out var co) && co != null)
                    StopCoroutine(co);
                _globalCoroutines.Remove(id);
                GameTimeManager.Instance?.Release(id);
            }
        }

        private IEnumerator HitStopCoroutine(int id, float duration)
        {
            yield return new WaitForSecondsRealtime(duration);

            _globalCoroutines.Remove(id);
            GameTimeManager.Instance?.Release(id);

            // 남은 요청이 없을 때만 Volume 페이드 아웃
            if (_globalCoroutines.Count == 0)
            {
                _targetWeight   = 0f;
                _transitionTime = 0.05f;
            }
        }

        private IEnumerator ActorOnlyCoroutine(GameActor actor, float duration, float animSpeed)
        {
            if (actor == null) yield break;

            var anim = actor.Animator?.GetAnimator;
            if (anim == null) { _actorCoroutines.Remove(actor); yield break; }

            float original = anim.speed;
            anim.speed = animSpeed;

            yield return new WaitForSecondsRealtime(duration);

            if (actor != null && anim != null) anim.speed = original;
            _actorCoroutines.Remove(actor);
        }

        private async void LoadVolume()
        {
            if (_volumeHandle.IsValid() || _volume != null) return;

            _volumeHandle = Addressables.LoadAssetAsync<GameObject>("SlowMoveVolume");

            try
            {
                GameObject go = await _volumeHandle.Task;
                if (go == null) { Debug.LogError("[HitStopManager] SlowMoveVolume 로드 실패"); return; }

                _volumeInstance      = Instantiate(go, transform.position, Quaternion.identity, transform);
                _volumeInstance.name = "Action_SlowMo_Volume";
                _volume              = _volumeInstance.GetComponent<Volume>();

                if (_volume != null) _volume.weight = 0f;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[HitStopManager] LoadVolume 실패: {e.Message}");
                if (_volumeHandle.IsValid()) Addressables.Release(_volumeHandle);
            }
        }

        #endregion
    }
}
