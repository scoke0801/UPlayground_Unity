using System;
using System.Collections;
using System.Collections.Generic;
using Animancer;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Component;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// 현재 포즈의 모델 복제본을 남기고 알파 페이드로 정리하는 잔상 이벤트.
    /// </summary>
    [Serializable]
    public class AfterimageEvent : MotionEventBase
    {
        [Tooltip("복제할 자식 오브젝트 이름. 비워두면 활성 CharacterModelData 모델을 사용한다.")]
        public string targetObjectName;

        [Tooltip("잔상이 유지되는 동안의 기본 알파.")]
        [Range(0f, 1f)]
        public float alpha = 0.45f;

        [Tooltip("잔상 생성 간격(A초). 0 이하면 한 번에 생성한다.")]
        [Min(0f)]
        public float spawnInterval = 0.05f;

        [Tooltip("이 이벤트 구간 안에서 생성할 최대 잔상 수(B개).")]
        [Min(1)]
        public int spawnCount = 1;

        [Tooltip("각 잔상이 이 시간(C초)만큼 유지된 뒤 페이드아웃한다.")]
        [Min(0f)]
        public float holdDuration = 0.2f;

        [Tooltip("잔상 색상 틴트. 알파도 최종 알파에 곱해진다.")]
        public Color tintColor = Color.white;

        [Tooltip("유지 시간이 끝난 뒤 잔상이 사라지는 시간. 0 이하면 즉시 제거한다.")]
        [Min(0f)]
        public float fadeOutDuration = 0.35f;

        [Tooltip("생성 위치 보정. 복제 대상의 로컬 축 기준으로 적용된다.")]
        public Vector3 offset;

        [Tooltip("생성 회전 보정.")]
        public Vector3 rotationOffset;

        private AfterimageEventRunner _runner;

        public override string GetDisplayName() => "Afterimage";

        public override string GetShortLabel()
        {
            string targetLabel = string.IsNullOrEmpty(targetObjectName) ? "Model" : targetObjectName;
            return $"Afterimage: {targetLabel} x{Mathf.Max(1, spawnCount)}";
        }

        public override void Execute(GameObject target)
        {
            if (target == null)
                return;

            Transform source = ResolveSource(target);
            if (source == null)
            {
                Debug.LogWarning($"[AfterimageEvent] 복제할 대상을 찾을 수 없습니다. target={target.name}, objectName={targetObjectName}");
                return;
            }

            _runner = target.GetOrAddComponent<AfterimageEventRunner>();
            _runner.Play(new AfterimageEventSettings
            {
                source = source,
                alpha = alpha,
                spawnInterval = spawnInterval,
                spawnCount = spawnCount,
                holdDuration = holdDuration,
                fadeOutDuration = fadeOutDuration,
                tintColor = tintColor,
                offset = offset,
                rotationOffset = rotationOffset
            });
        }

        public override void OnCompleteEvent(GameObject target)
        {
            _runner?.StopSpawning();
            _runner = null;
        }

        private Transform ResolveSource(GameObject target)
        {
            if (!string.IsNullOrEmpty(targetObjectName))
                return FindTransformByName(target.transform, targetObjectName);

            var modelData = target.GetComponentInChildren<CharacterModelData>(includeInactive: false);
            if (modelData != null)
                return modelData.transform;

            var actorAnimator = target.GetComponentInChildren<ActorAnimator>(includeInactive: false);
            if (actorAnimator != null)
                return actorAnimator.transform;

            return target.transform;
        }

        private static Transform FindTransformByName(Transform parent, string objectName)
        {
            if (parent == null || string.IsNullOrEmpty(objectName))
                return null;

            foreach (var child in parent.GetComponentsInChildren<Transform>(includeInactive: false))
            {
                if (child.name == objectName)
                    return child;
            }

            return null;
        }

        private static void PrepareVisualOnlyInstance(GameObject instance)
        {
            foreach (var executor in instance.GetComponentsInChildren<MotionEventExecutor>(true))
                GameObject.Destroy(executor);

            foreach (var actorAnimator in instance.GetComponentsInChildren<ActorAnimator>(true))
                GameObject.Destroy(actorAnimator);

            foreach (var animancer in instance.GetComponentsInChildren<AnimancerComponent>(true))
                GameObject.Destroy(animancer);

            foreach (var animator in instance.GetComponentsInChildren<Animator>(true))
                animator.enabled = false;

            foreach (var particleSystem in instance.GetComponentsInChildren<ParticleSystem>(true))
                particleSystem.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);

            foreach (var trailRenderer in instance.GetComponentsInChildren<TrailRenderer>(true))
                trailRenderer.enabled = false;

            foreach (var lineRenderer in instance.GetComponentsInChildren<LineRenderer>(true))
                lineRenderer.enabled = false;

            foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;

            foreach (var rigidbody in instance.GetComponentsInChildren<Rigidbody>(true))
                GameObject.Destroy(rigidbody);

            foreach (var behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour is AlphaFadeController)
                    continue;

                behaviour.enabled = false;
            }
        }

        private struct AfterimageEventSettings
        {
            public Transform source;
            public float alpha;
            public float spawnInterval;
            public int spawnCount;
            public float holdDuration;
            public float fadeOutDuration;
            public Color tintColor;
            public Vector3 offset;
            public Vector3 rotationOffset;
        }

        private sealed class AfterimageEventRunner : MonoBehaviour
        {
            private Coroutine _spawnRoutine;

            public void Play(AfterimageEventSettings settings)
            {
                StopSpawning();
                _spawnRoutine = StartCoroutine(SpawnRoutine(settings));
            }

            public void StopSpawning()
            {
                if (_spawnRoutine == null)
                    return;

                StopCoroutine(_spawnRoutine);
                _spawnRoutine = null;
            }

            private IEnumerator SpawnRoutine(AfterimageEventSettings settings)
            {
                int count = Mathf.Max(1, settings.spawnCount);
                float interval = Mathf.Max(0f, settings.spawnInterval);

                for (int i = 0; i < count; i++)
                {
                    if (settings.source == null)
                        break;

                    SpawnAfterimage(settings);

                    if (i >= count - 1)
                        break;

                    if (interval <= 0f)
                        continue;

                    float elapsed = 0f;
                    while (elapsed < interval)
                    {
                        elapsed += Time.deltaTime;
                        yield return null;
                    }
                }

                _spawnRoutine = null;
            }

            private void SpawnAfterimage(AfterimageEventSettings settings)
            {
                Vector3 position = settings.source.position + settings.source.TransformDirection(settings.offset);
                Quaternion rotation = settings.source.rotation * Quaternion.Euler(settings.rotationOffset);
                var instance = Instantiate(settings.source.gameObject, position, rotation);
                instance.name = $"{settings.source.name}_Afterimage";
                instance.SetActive(true);

                PrepareVisualOnlyInstance(instance);

                var alphaFadeController = instance.GetOrAddComponent<AlphaFadeController>();
                alphaFadeController.RefreshRenderers();
                alphaFadeController.WarmupAlphaMaterials(settings.alpha, settings.tintColor);

                StartCoroutine(FadeAfterHoldRoutine(instance, alphaFadeController, settings.holdDuration, settings.fadeOutDuration));
            }

            private IEnumerator FadeAfterHoldRoutine(GameObject instance, AlphaFadeController alphaFadeController, float holdDuration, float fadeOutDuration)
            {
                float elapsed = 0f;
                float safeHoldDuration = Mathf.Max(0f, holdDuration);
                while (elapsed < safeHoldDuration)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                if (instance == null)
                    yield break;

                if (alphaFadeController == null)
                {
                    Destroy(instance);
                    yield break;
                }

                alphaFadeController.StartFadeOut(fadeOutDuration, destroyOnComplete: true);
            }

            private void OnDestroy()
            {
                StopSpawning();
                StopAllCoroutines();
            }
        }
    }
}
