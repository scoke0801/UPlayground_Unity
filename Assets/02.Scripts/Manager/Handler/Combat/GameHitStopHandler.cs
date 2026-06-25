using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UPlayGround.Manager.Handler;

namespace UPlayGround.Manager.Combat
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
    ///   새 요청의 scale &lt; 현재 활성 scale → 더 강한 효과이므로 현재 것을 교체
    ///   새 요청의 scale ≥ 현재 활성 scale → 더 약하므로 큐에만 추가 (현재 scale 유지)
    /// </summary>
    public class GameHitStopHandler : GameHandlerBase
    {
        public enum HitStopIntensity
        {
            Light,       // base 0.05s  scale=0.15
            Medium,      // base 0.08s  scale=0.10
            Heavy,       // base 0.12s  scale=0.05
            Critical,    // base 0.15s  scale=0.02
            PlayerDie,   // 1.00s  scale=0.02
            PlayerGuard, // actor-only
        }

        private const float DefaultHitStopDuration = 0.08f;
        private const float DefaultTimeScale = 0.1f;
        private const float MinImpactTimeScale = 0.001f;
        private const float ImpactHoldRatio = 0.6f;
        private const float MinImpactHoldDuration = 0.012f;

        private Volume _volume;
        private GameObject _volumeInstance;
        private bool _isVolumeLoading;

        private float _transitionTime = 0.05f;
        private float _targetWeight = 0f;
        private float _currentWeight = 0f;
        private float _weightVelocity = 0f;
        private float _holdWeightUntilRealtime = -1f;

        // 전역 HitStop: id → 코루틴. 복수 요청이 동시에 살아있을 수 있다.
        private readonly Dictionary<int, Coroutine> _globalCoroutines = new Dictionary<int, Coroutine>();

        private sealed class ActorTimeScaleRequest
        {
            public Coroutine Coroutine;
            public float OriginalLocalTimeScale = 1f;
        }

        // GameActor 단위 LocalTimeScale 조작
        private readonly Dictionary<GameActor, ActorTimeScaleRequest> _actorCoroutines = new Dictionary<GameActor, ActorTimeScaleRequest>();

        public bool IsHitStopping => GameTimeManager.Instance?.IsSlowed ?? false;

        #region GameHandlerBase

        public override void Init()
        {
            _actorCoroutines.Clear();
            _globalCoroutines.Clear();
            LoadVolumeAsync().Forget();
        }

        public override void Dispose()
        {
            Stop();
            StopAllActors();
            GameObjectManager.Instance?.ResetAllActorsTimeScaleIncludingPlayer();

            if (_volumeInstance != null)
            {
                UnityEngine.Object.Destroy(_volumeInstance);
                _volumeInstance = null;
            }
        }

        public override void Update()
        {
            if (_volume == null) return;

            if (_holdWeightUntilRealtime > 0f && Time.realtimeSinceStartup < _holdWeightUntilRealtime)
            {
                _volume.weight = _currentWeight;
                return;
            }

            _holdWeightUntilRealtime = -1f;

            _currentWeight = Mathf.SmoothDamp(
                _currentWeight, _targetWeight,
                ref _weightVelocity, _transitionTime,
                Mathf.Infinity, Time.unscaledDeltaTime);

            _volume.weight = _currentWeight;
        }

        public override void OnSceneChanged(string sceneType)
        {
            Stop();
            StopAllActors();
            GameObjectManager.Instance?.ResetAllActorsTimeScaleIncludingPlayer();
        }

        #endregion

        #region 전역 HitStop — 공개 API

        public void Execute() => Execute(DefaultHitStopDuration, DefaultTimeScale);

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
            duration = Mathf.Max(0f, duration);
            if (duration <= 0f)
                return;

            timeScale = Mathf.Clamp(timeScale, MinImpactTimeScale, 1f);

            if (ShouldReplaceExisting(timeScale))
                StopWeakerThan(timeScale);

            int id = GameTimeManager.Instance.Request(timeScale);
            var co = GameCombatManager.Instance.StartCoroutine(HitStopCoroutine(id, duration, timeScale));
            _globalCoroutines[id] = co;
        }

        /// <summary>
        /// 모든 전역 HitStop 강제 종료.
        /// </summary>
        public void Stop()
        {
            var host = GameCombatManager.Instance;
            foreach (var co in _globalCoroutines.Values)
                if (co != null && host != null) host.StopCoroutine(co);

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
            _targetWeight = 0f;
            _transitionTime = 0f;
            _holdWeightUntilRealtime = -1f;
        }

        public void FlashPostProcess(
            float peakWeight = 1f,
            float holdDuration = 0.08f,
            float fadeOutDuration = 0.24f,
            float minVisibleDuration = 0.12f)
        {
            _currentWeight = Mathf.Clamp01(peakWeight);
            _targetWeight = 0f;
            _weightVelocity = 0f;
            _transitionTime = Mathf.Max(0.01f, fadeOutDuration);
            _holdWeightUntilRealtime = Time.realtimeSinceStartup + Mathf.Max(holdDuration, minVisibleDuration);

            if (_volume != null)
                _volume.weight = _currentWeight;
        }

        #endregion

        #region Actor 단위 LocalTimeScale 조작

        public void ExecuteActorOnly(GameActor actor, float duration, float animSpeed = 0.1f)
        {
            if (actor == null) return;
            
            if (duration <= 0f) return;

            StopActor(actor);

            var request = new ActorTimeScaleRequest
            {
                OriginalLocalTimeScale = ResolveOriginalForCapture(actor),
            };
            request.Coroutine = GameCombatManager.Instance.StartCoroutine(ActorOnlyCoroutine(actor, request, duration, animSpeed));
            _actorCoroutines[actor] = request;
        }

        public void ExecuteLocalImpact(
            GameActor attacker,
            GameActor victim,
            float duration,
            float localTimeScale = 0.1f,
            bool includeAttacker = true,
            float victimTimeScale = -1f)
        {
            if (duration <= 0f) return;

            localTimeScale = Mathf.Clamp(localTimeScale, MinImpactTimeScale, 1f);
            // victimTimeScale < 0 이면 공격자와 동일(기존 대칭 동작). 0 이상이면 피격자를 별도 강도로 멈춘다.
            // 공격자는 약하게(루트모션/카메라가 미세하게 진행) + 피격자는 풀프리즈 같은 비대칭 타격감을 위해 분리한다.
            float resolvedVictimScale = victimTimeScale >= 0f
                ? Mathf.Clamp(victimTimeScale, MinImpactTimeScale, 1f)
                : localTimeScale;
            bool applied = false;

            if (includeAttacker && attacker != null)
            {
                ExecuteActorOnly(attacker, duration, localTimeScale);
                applied = true;
            }

            if (victim != null && victim != attacker)
            {
                ExecuteActorOnly(victim, duration, resolvedVictimScale);
                applied = true;
            }

            if (!applied)
                Execute(duration, localTimeScale);
        }

        public void StopActor(GameActor actor)
        {
            if (actor == null) return;
            if (!_actorCoroutines.TryGetValue(actor, out var request)) return;

            var host = GameCombatManager.Instance;
            if (request.Coroutine != null && host != null) host.StopCoroutine(request.Coroutine);
            _actorCoroutines.Remove(actor);

            actor.LocalTimeScale = request.OriginalLocalTimeScale;
        }

        public void StopAllActors()
        {
            var host = GameCombatManager.Instance;
            foreach (var kvp in _actorCoroutines)
            {
                if (kvp.Value.Coroutine != null && host != null) host.StopCoroutine(kvp.Value.Coroutine);
                if (kvp.Key != null)
                    kvp.Key.LocalTimeScale = kvp.Value.OriginalLocalTimeScale;
            }
            _actorCoroutines.Clear();
        }

        public bool IsActorHitStopping(GameActor actor) =>
            actor != null && _actorCoroutines.ContainsKey(actor);

        /// <summary>
        /// actor가 현재 이 핸들러의 actor-only 히트스톱으로 관리 중이면
        /// 해당 request가 보관한 "진짜 original" LocalTimeScale을 반환한다.
        /// 다른 시스템(DefenseSuccessFeedback 등)이 freeze 중간값을 original로
        /// 오인 캡처하는 것을 막기 위한 교차 조회용.
        /// </summary>
        public bool TryGetActorOriginalScale(GameActor actor, out float original)
        {
            if (actor != null && _actorCoroutines.TryGetValue(actor, out var req))
            {
                original = req.OriginalLocalTimeScale;
                return true;
            }
            original = 1f;
            return false;
        }

        // 캡처 시점에 다른 시스템이 이미 이 actor를 freeze 중이면, 오염된 라이브값이 아니라
        // 그 시스템이 들고 있는 진짜 original을 신뢰한다. 아무도 관리 중이 아니면 라이브값이 곧 진실.
        private float ResolveOriginalForCapture(GameActor actor)
        {
            if (actor == null) return 1f;

            var defense = GameCombatManager.Instance?.DefenseSuccessFeedback;
            if (defense != null && defense.TryGetFrozenOriginalScale(actor, out var trueOriginal))
                return trueOriginal;

            return actor.LocalTimeScale;
        }

        #endregion

        #region 내부

        private bool ShouldReplaceExisting(float newScale)
        {
            float current = GameTimeManager.Instance?.IsSlowed == true
                ? Time.timeScale
                : 1f;
            return newScale < current;
        }

        private void StopWeakerThan(float newScale)
        {
            var toRemove = new List<int>();
            foreach (var id in _globalCoroutines.Keys)
                toRemove.Add(id);

            var host = GameCombatManager.Instance;
            foreach (int id in toRemove)
            {
                if (_globalCoroutines.TryGetValue(id, out var co) && co != null && host != null)
                    host.StopCoroutine(co);
                _globalCoroutines.Remove(id);
                GameTimeManager.Instance?.Release(id);
            }
        }

        private IEnumerator HitStopCoroutine(int id, float duration, float timeScale)
        {
            float holdDuration = GetImpactHoldDuration(duration);
            float recoverDuration = Mathf.Max(0f, duration - holdDuration);

            GameTimeManager.Instance?.UpdateRequestScale(id, Mathf.Clamp(timeScale, MinImpactTimeScale, 1f));

            if (holdDuration > 0f)
                yield return new WaitForSecondsRealtime(holdDuration);

            if (recoverDuration > 0f)
            {
                float elapsed = 0f;
                while (elapsed < recoverDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / recoverDuration);
                    float eased = EaseImpactRecovery(t);
                    GameTimeManager.Instance?.UpdateRequestScale(id, Mathf.Lerp(timeScale, 1f, eased));
                    yield return null;
                }
            }

            _globalCoroutines.Remove(id);
            GameTimeManager.Instance?.Release(id);

            if (_globalCoroutines.Count == 0)
            {
                _targetWeight = 0f;
                _transitionTime = 0.05f;
            }
        }

        private IEnumerator ActorOnlyCoroutine(GameActor actor, ActorTimeScaleRequest request, float duration, float animSpeed)
        {
            if (actor == null) yield break;

            float targetScale = Mathf.Clamp(animSpeed, MinImpactTimeScale, 1f);
            float holdDuration = GetImpactHoldDuration(duration);
            float recoverDuration = Mathf.Max(0f, duration - holdDuration);

            actor.LocalTimeScale = targetScale;

            if (holdDuration > 0f)
                yield return new WaitForSecondsRealtime(holdDuration);

            if (recoverDuration > 0f)
            {
                float elapsed = 0f;
                while (elapsed < recoverDuration)
                {
                    if (actor == null)
                        yield break;

                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / recoverDuration);
                    float eased = EaseImpactRecovery(t);
                    actor.LocalTimeScale = Mathf.Lerp(targetScale, request.OriginalLocalTimeScale, eased);
                    yield return null;
                }
            }

            if (actor != null) actor.LocalTimeScale = request.OriginalLocalTimeScale;
            _actorCoroutines.Remove(actor);
        }

        private static float GetImpactHoldDuration(float duration)
        {
            if (duration <= 0f)
                return 0f;

            return Mathf.Clamp(
                duration * ImpactHoldRatio,
                0f,
                Mathf.Max(0f, duration - MinImpactHoldDuration));
        }

        private static float EaseImpactRecovery(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - Mathf.Pow(1f - t, 3f);
        }

        private async UniTask LoadVolumeAsync()
        {
            if (_volume != null || _isVolumeLoading) return;
            _isVolumeLoading = true;

            try
            {
                GameObject go = await AssetManager.Instance.LoadGlobalAsync<GameObject>(
                    "SlowMoveVolume",
                    nameof(GameHitStopHandler));
                if (go == null) { Debug.LogError("[HitStopHandler] SlowMoveVolume 로드 실패"); return; }

                var hostTransform = GameCombatManager.Instance.transform;
                _volumeInstance = UnityEngine.Object.Instantiate(go, hostTransform.position, Quaternion.identity, hostTransform);
                _volumeInstance.name = "Action_SlowMo_Volume";
                _volume = _volumeInstance.GetComponent<Volume>();

                if (_volume != null) _volume.weight = 0f;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[HitStopHandler] LoadVolume 실패: {e.Message}");
            }
            finally
            {
                _isVolumeLoading = false;
            }
        }

        #endregion
    }
}
