#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UPlayGround.Animation;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.Event;
using UPlayGround.MovementController;
using MotionData = UPlayGround.Animation.Motion;

namespace UPlayGround.Editor
{
    /// <summary>
    /// 배치 베이크 샘플러의 기준값을 실제 PlayMode ActorAnimator 오도미터로 캡처한다.
    /// </summary>
    [InitializeOnLoad]
    internal static class MotionWarpPlayModeReferenceCapture
    {
        private const float SampleRate = 120f;
        private const string PendingKey = "UPlayGround.MotionWarpBake.Capture.Pending";
        private const string AssetGuidKey = "UPlayGround.MotionWarpBake.Capture.AssetGuid";
        private const string PrefabPathKey = "UPlayGround.MotionWarpBake.Capture.PrefabPath";
        private const string AnimatorPathKey = "UPlayGround.MotionWarpBake.Capture.AnimatorPath";
        private const string WarpGlobalStartKey = "UPlayGround.MotionWarpBake.Capture.GlobalStart";
        private const string WarpStartKey = "UPlayGround.MotionWarpBake.Capture.Start";
        private const string WarpEndKey = "UPlayGround.MotionWarpBake.Capture.End";
        private const string ResultKey = "UPlayGround.MotionWarpBake.Capture.Result";

        [Serializable]
        private struct CaptureResult
        {
            public bool Success;
            public string Error;
            public Vector3 LocalTotal;
            public float PathLength;
            public float Yaw;
            public Vector3[] CumulativeLocalPositions;
            public float[] CumulativePathLengths;
            public float[] CumulativeYaw;
        }

        static MotionWarpPlayModeReferenceCapture()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        /// <summary>출처가 남은 기존 프로필 하나를 골라 현재 포맷의 기준값으로 다시 측정한다.</summary>
        public static void Begin()
        {
            if (SessionState.GetBool(PendingKey, false))
                throw new InvalidOperationException("MotionWarp PlayMode 기준 캡처가 이미 진행 중입니다.");

            if (!TryResolveCaptureTarget(
                    out MotionSetAsset asset,
                    out MotionEvent_MotionWarp warp,
                    out float globalStart,
                    out string prefabPath,
                    out string animatorPath))
            {
                throw new InvalidOperationException(
                    "PlayMode 출처가 기록된 MotionWarp 기준 프로필 또는 소유 플레이어 프리팹을 찾지 못했습니다.");
            }

            SessionState.SetBool(PendingKey, true);
            SessionState.SetString(
                AssetGuidKey,
                AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset)));
            SessionState.SetString(PrefabPathKey, prefabPath);
            SessionState.SetString(AnimatorPathKey, animatorPath ?? string.Empty);
            SessionState.SetFloat(WarpGlobalStartKey, globalStart);
            SessionState.SetFloat(WarpStartKey, warp.startTime);
            SessionState.SetFloat(WarpEndKey, warp.endTime);
            SessionState.EraseString(ResultKey);
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(PendingKey, false))
                return;

            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                CaptureResult result;
                try
                {
                    result = CaptureInPlayMode();
                }
                catch (Exception exception)
                {
                    result = new CaptureResult
                    {
                        Success = false,
                        Error = exception.ToString(),
                    };
                }

                SessionState.SetString(ResultKey, JsonUtility.ToJson(result));
                EditorApplication.ExitPlaymode();
                return;
            }

            if (state == PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += CompleteInEditMode;
        }

        private static CaptureResult CaptureInPlayMode()
        {
            MotionSetAsset asset = LoadTargetAsset();
            MotionEvent_MotionWarp warp = ResolveTargetWarp(asset);
            string prefabPath = SessionState.GetString(PrefabPathKey, string.Empty);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                throw new InvalidOperationException($"기준 프리팹을 불러오지 못했습니다: {prefabPath}");

            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            instance.name = $"{prefab.name}_MotionWarpCapture";
            try
            {
                string animatorPath = SessionState.GetString(AnimatorPathKey, string.Empty);
                Transform animatorTransform = string.IsNullOrEmpty(animatorPath)
                    ? instance.transform
                    : instance.transform.Find(animatorPath);
                Animator animator = animatorTransform != null
                    ? animatorTransform.GetComponent<Animator>()
                    : null;
                ActorAnimator actorAnimator = animatorTransform != null
                    ? animatorTransform.GetComponent<ActorAnimator>()
                    : null;
                if (animator == null || actorAnimator == null)
                    throw new InvalidOperationException(
                        $"기준 프리팹에서 Animator/ActorAnimator를 찾지 못했습니다: {prefabPath}/{animatorPath}");

                for (Transform cursor = animator.transform;
                     cursor != null;
                     cursor = cursor.parent)
                    cursor.gameObject.SetActive(true);
                actorAnimator.enabled = true;
                animator.applyRootMotion = true;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                float globalStart = SessionState.GetFloat(WarpGlobalStartKey, 0f);
                return SampleRuntimeOdometer(
                    asset.motionSet,
                    warp,
                    globalStart,
                    instance.transform,
                    animator,
                    actorAnimator);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static CaptureResult SampleRuntimeOdometer(
            MotionSet set,
            MotionEvent_MotionWarp warp,
            float globalStart,
            Transform actorRoot,
            Animator animator,
            ActorAnimator actorAnimator)
        {
            float globalEnd = Mathf.Min(
                set.TotalDuration,
                globalStart + Mathf.Max(0f, warp.endTime - warp.startTime));
            float duration = globalEnd - globalStart;
            int trajectorySampleCount = MotionEvent_MotionWarp.TrajectorySampleCount;
            var cumulativeLocalPositions = new Vector3[trajectorySampleCount];
            var cumulativePathLengths = new float[trajectorySampleCount];
            var cumulativeYaw = new float[trajectorySampleCount];
            if (duration <= 0.0001f)
            {
                return new CaptureResult
                {
                    Success = false,
                    Error = "PlayMode 기준 윈도우 길이가 0입니다.",
                };
            }

            PlayableGraph graph = PlayableGraph.Create("MotionWarpPlayModeReference");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(
                graph,
                "RootMotion",
                animator);
            output.SetWeight(1f);

            AnimationClipPlayable activePlayable = default;
            int activeMotionIndex = -1;
            float activeMotionEnd = globalStart;
            Vector3 previousWorldOdometer = actorAnimator.RootMotionWorldOdometer;
            float previousPathOdometer = actorAnimator.RootMotionPathOdometer;
            Vector3 initialPosition = actorRoot.position;
            Quaternion initialRotation = actorRoot.rotation;
            Quaternion inverseInitialRotation = Quaternion.Inverse(initialRotation);
            Vector3 initialUp = initialRotation * Vector3.up;
            Vector3 measuredLocal = Vector3.zero;
            float measuredPath = 0f;
            float measuredYaw = 0f;
            float currentTime = globalStart;
            int nextTrajectorySample = 1;
            try
            {
                graph.Play();
                if (!WarpRootMotionBatchBakeWindow.ConfigurePlayable(
                        set,
                        graph,
                        output,
                        globalStart,
                        ref activePlayable,
                        out activeMotionIndex,
                        out activeMotionEnd))
                    throw new InvalidOperationException(
                        "PlayMode 기준 윈도우의 시작 모션을 해석하지 못했습니다.");
                previousWorldOdometer = actorAnimator.RootMotionWorldOdometer;
                previousPathOdometer = actorAnimator.RootMotionPathOdometer;

                while (currentTime < globalEnd - 0.000001f)
                {
                    if (currentTime >= activeMotionEnd - 0.000001f)
                    {
                        if (!WarpRootMotionBatchBakeWindow.ConfigurePlayable(
                                set,
                                graph,
                                output,
                                currentTime,
                                ref activePlayable,
                                out activeMotionIndex,
                                out activeMotionEnd))
                            break;
                        previousWorldOdometer = actorAnimator.RootMotionWorldOdometer;
                        previousPathOdometer = actorAnimator.RootMotionPathOdometer;
                    }

                    float trajectoryTime = globalStart
                        + duration * (nextTrajectorySample
                                      / (float)(trajectorySampleCount - 1));
                    float nextTime = Mathf.Min(
                        globalEnd,
                        Mathf.Min(
                            currentTime + 1f / SampleRate,
                            Mathf.Min(trajectoryTime, activeMotionEnd)));
                    float step = nextTime - currentTime;
                    if (step <= 0.000001f)
                        break;
                    graph.Evaluate(step);
                    Vector3 worldOdometer = actorAnimator.RootMotionWorldOdometer;
                    float pathOdometer = actorAnimator.RootMotionPathOdometer;
                    Vector3 horizontal = worldOdometer - previousWorldOdometer;
                    float pathDelta = Mathf.Max(0f, pathOdometer - previousPathOdometer);
                    Vector3 frameDelta = animator.deltaPosition;
                    Quaternion frameRotation = animator.deltaRotation;
                    if (pathDelta <= 0f)
                    {
                        // 수동 PlayableGraph 평가는 같은 스텝 안에서 OnAnimatorMove 콜백이
                        // 실행되지 않을 수 있다. 그때도 Animator의 원본 델타는 유효하다.
                        horizontal = new Vector3(frameDelta.x, 0f, frameDelta.z);
                        pathDelta = horizontal.magnitude;
                    }
                    if (pathDelta > 0f)
                    {
                        measuredLocal += inverseInitialRotation * horizontal;
                        measuredPath += pathDelta;
                    }

                    Quaternion beforeRotation = actorRoot.rotation;
                    Quaternion afterRotation =
                        (beforeRotation * frameRotation).normalized;
                    measuredYaw += WarpRootMotionBatchBakeWindow.ResolveYawDelta(
                        beforeRotation,
                        afterRotation,
                        initialUp);
                    actorRoot.SetPositionAndRotation(
                        actorRoot.position + frameDelta,
                        afterRotation);
                    previousWorldOdometer = worldOdometer;
                    previousPathOdometer = pathOdometer;
                    currentTime = nextTime;
                    if (nextTrajectorySample < trajectorySampleCount
                        && currentTime >= trajectoryTime - 0.000001f)
                    {
                        cumulativeLocalPositions[nextTrajectorySample] = measuredLocal;
                        cumulativePathLengths[nextTrajectorySample] = measuredPath;
                        cumulativeYaw[nextTrajectorySample] = measuredYaw;
                        nextTrajectorySample++;
                    }
                }

                while (nextTrajectorySample < trajectorySampleCount)
                {
                    cumulativeLocalPositions[nextTrajectorySample] = measuredLocal;
                    cumulativePathLengths[nextTrajectorySample] = measuredPath;
                    cumulativeYaw[nextTrajectorySample] = measuredYaw;
                    nextTrajectorySample++;
                }
            }
            finally
            {
                actorRoot.SetPositionAndRotation(initialPosition, initialRotation);
                if (graph.IsValid())
                    graph.Destroy();
            }

            return new CaptureResult
            {
                Success = measuredPath > 0.0001f,
                Error = measuredPath > 0.0001f
                    ? string.Empty
                    : "PlayMode 오도미터가 유효한 루트 경로를 만들지 못했습니다.",
                LocalTotal = measuredLocal,
                PathLength = measuredPath,
                Yaw = measuredYaw,
                CumulativeLocalPositions = cumulativeLocalPositions,
                CumulativePathLengths = cumulativePathLengths,
                CumulativeYaw = cumulativeYaw,
            };
        }

        private static void CompleteInEditMode()
        {
            int exitCode = 1;
            try
            {
                string json = SessionState.GetString(ResultKey, string.Empty);
                CaptureResult result = string.IsNullOrEmpty(json)
                    ? new CaptureResult
                    {
                        Success = false,
                        Error = "PlayMode 캡처 결과가 없습니다.",
                    }
                    : JsonUtility.FromJson<CaptureResult>(json);
                if (!result.Success)
                    throw new InvalidOperationException(result.Error);

                MotionSetAsset asset = LoadTargetAsset();
                MotionEvent_MotionWarp warp = ResolveTargetWarp(asset);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    SessionState.GetString(PrefabPathKey, string.Empty));
                string animatorPath = SessionState.GetString(AnimatorPathKey, string.Empty);
                Transform animatorTransform = prefab != null && string.IsNullOrEmpty(animatorPath)
                    ? prefab.transform
                    : prefab?.transform.Find(animatorPath);
                Animator animator = animatorTransform != null
                    ? animatorTransform.GetComponent<Animator>()
                    : null;
                if (animator == null)
                    throw new InvalidOperationException("기준 프리팹의 Animator를 다시 찾지 못했습니다.");

                Undo.RecordObject(asset, "MotionWarp PlayMode 기준 캡처");
                warp.RecordBakedProfile(
                    animator.avatar,
                    animator.transform.lossyScale,
                    WarpRootMotionBatchBakeWindow.BuildSourceFingerprint(
                        asset,
                        warp,
                        animator.avatar,
                        animator.transform.lossyScale),
                    result.CumulativeLocalPositions,
                    result.CumulativePathLengths,
                    result.CumulativeYaw,
                    fromPlayMode: true);
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssetIfDirty(asset);
                Debug.Log(
                    $"[MotionWarp] PlayMode 기준 캡처 완료: {AssetDatabase.GetAssetPath(asset)} "
                    + $"path={result.PathLength:F4}, local={result.LocalTotal}");
                exitCode = 0;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                ClearSession();
                if (Application.isBatchMode)
                    EditorApplication.Exit(exitCode);
            }
        }

        private static bool TryResolveCaptureTarget(
            out MotionSetAsset asset,
            out MotionEvent_MotionWarp warp,
            out float globalStart,
            out string prefabPath,
            out string animatorPath)
        {
            asset = null;
            warp = null;
            globalStart = 0f;
            prefabPath = null;
            animatorPath = null;

            foreach (string guid in AssetDatabase.FindAssets(
                         "t:MotionSetAsset",
                         new[] { "Assets/10.Datas" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                MotionSetAsset candidate = AssetDatabase.LoadAssetAtPath<MotionSetAsset>(path);
                if (candidate?.motionSet == null)
                    continue;
                foreach (MotionEvent_MotionWarp candidateWarp in
                         CollectWarpEvents(candidate.motionSet))
                {
                    if (!TryGetReferenceRig(
                            candidateWarp,
                            out Avatar referenceAvatar,
                            out Vector3 referenceScale)
                        || !candidate.motionSet.TryGetEventGlobalStart(
                            candidateWarp,
                            out float candidateGlobalStart))
                        continue;

                    if (!TryFindPlayerOwner(
                            referenceAvatar,
                            referenceScale,
                            out prefabPath,
                            out animatorPath))
                        continue;
                    asset = candidate;
                    warp = candidateWarp;
                    globalStart = candidateGlobalStart;
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetReferenceRig(
            MotionEvent_MotionWarp warp,
            out Avatar avatar,
            out Vector3 animatorScale)
        {
            avatar = null;
            animatorScale = Vector3.one;
            if (warp?.bakedProfiles != null)
            {
                foreach (MotionWarpRootMotionBakeProfile profile in
                         warp.bakedProfiles)
                {
                    if (profile == null
                        || !profile.HasPlayModeReference
                        || profile.avatar == null)
                        continue;
                    avatar = profile.avatar;
                    animatorScale = profile.animatorScale;
                    return true;
                }
            }

            if (warp == null
                || !warp.bakedValid
                || !warp.bakedFromPlayMode
                || warp.bakedFormatVersion <= 0
                || warp.bakedPathLen <= 0.0001f
                || warp.bakedAvatar == null)
                return false;
            avatar = warp.bakedAvatar;
            animatorScale = warp.bakedAnimatorScale;
            return true;
        }

        private static bool TryFindPlayerOwner(
            Avatar avatar,
            Vector3 animatorScale,
            out string prefabPath,
            out string animatorPath)
        {
            prefabPath = null;
            animatorPath = null;
            string[] guids = AssetDatabase.FindAssets(
                "t:Prefab",
                new[] { "Assets/03.Prefabs/Actor/Player" });
            foreach (string path in guids
                         .Select(AssetDatabase.GUIDToAssetPath)
                         .OrderBy(GetPlayerOwnerPriority)
                         .ThenBy(value => value, StringComparer.OrdinalIgnoreCase))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Animator animator = prefab != null
                    ? prefab.GetComponentsInChildren<Animator>(true)
                        .FirstOrDefault(candidate =>
                            candidate.avatar == avatar
                            && (candidate.transform.lossyScale - animatorScale)
                            .sqrMagnitude <= 0.0001f
                            && candidate.GetComponent<ActorAnimator>() != null)
                    : null;
                if (animator == null)
                    continue;
                prefabPath = path;
                animatorPath = AnimationUtility.CalculateTransformPath(
                    animator.transform,
                    prefab.transform);
                return true;
            }

            return false;
        }

        private static int GetPlayerOwnerPriority(string path)
            => path.IndexOf(
                    "/Actor/Player/Models/",
                    StringComparison.OrdinalIgnoreCase) >= 0
                ? 0
                : 1;

        private static MotionSetAsset LoadTargetAsset()
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(
                SessionState.GetString(AssetGuidKey, string.Empty));
            MotionSetAsset asset = AssetDatabase.LoadAssetAtPath<MotionSetAsset>(assetPath);
            return asset != null
                ? asset
                : throw new InvalidOperationException("캡처 대상 MotionSetAsset을 다시 찾지 못했습니다.");
        }

        private static MotionEvent_MotionWarp ResolveTargetWarp(MotionSetAsset asset)
        {
            float globalStart = SessionState.GetFloat(WarpGlobalStartKey, 0f);
            float start = SessionState.GetFloat(WarpStartKey, 0f);
            float end = SessionState.GetFloat(WarpEndKey, 0f);
            MotionEvent_MotionWarp warp = CollectWarpEvents(asset.motionSet)
                .FirstOrDefault(candidate =>
                    Mathf.Approximately(candidate.startTime, start)
                    && Mathf.Approximately(candidate.endTime, end)
                    && asset.motionSet.TryGetEventGlobalStart(candidate, out float candidateGlobalStart)
                    && Mathf.Approximately(candidateGlobalStart, globalStart));
            return warp ?? throw new InvalidOperationException(
                "캡처 대상 MotionWarp 이벤트를 다시 찾지 못했습니다.");
        }

        private static IEnumerable<MotionEvent_MotionWarp> CollectWarpEvents(MotionSet set)
        {
            if (set?.globalEvents != null)
                foreach (MotionEventBase motionEvent in set.globalEvents)
                    if (motionEvent is MotionEvent_MotionWarp warp)
                        yield return warp;
            if (set?.motions == null)
                yield break;
            foreach (MotionData motion in set.motions)
            {
                if (motion?.events == null)
                    continue;
                foreach (MotionEventBase motionEvent in motion.events)
                    if (motionEvent is MotionEvent_MotionWarp warp)
                        yield return warp;
            }
        }

        private static void ClearSession()
        {
            SessionState.EraseBool(PendingKey);
            SessionState.EraseString(AssetGuidKey);
            SessionState.EraseString(PrefabPathKey);
            SessionState.EraseString(AnimatorPathKey);
            SessionState.EraseFloat(WarpGlobalStartKey);
            SessionState.EraseFloat(WarpStartKey);
            SessionState.EraseFloat(WarpEndKey);
            SessionState.EraseString(ResultKey);
        }
    }
}
#endif
