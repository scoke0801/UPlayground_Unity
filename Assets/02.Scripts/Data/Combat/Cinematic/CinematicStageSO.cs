using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace UPlayGround.Data.Cinematic
{
    public enum CinematicStageTier
    {
        None,
        CameraOnly,
        CasterClone,
        BothClones
    }

    public enum CinematicStageFallback
    {
        Abort,
        DemoteToCameraOnly,
        CasterCloneOnly
    }

    public enum CinematicTargetRepresentation
    {
        Clone,
        Silhouette,
        DummyRig,
        VfxOnly,
        None
    }

    public enum CinematicStageTransitionType
    {
        None,
        WhiteFlash,
        Fade,
        Dissolve,
        Wipe
    }

    public enum CinematicStageExitReason
    {
        Completed,
        Interrupted,
        OwnerLost,
        SceneChanged,
        WatchdogTimeout,
        Disabled,
        Failed,
        Replaced
    }

    public enum UltimateTargetSize
    {
        Small,
        Medium,
        Large,
        Giant
    }

    [Serializable]
    public struct UltimateTargetSizeAnchors
    {
        public Vector3 small;
        public Vector3 medium;
        public Vector3 large;
        public Vector3 giant;

        public Vector3 GetOffset(UltimateTargetSize size)
        {
            return size switch
            {
                UltimateTargetSize.Small => small,
                UltimateTargetSize.Large => large,
                UltimateTargetSize.Giant => giant,
                _ => medium
            };
        }
    }

    [Serializable]
    public sealed class CinematicStageSettings
    {
        public bool enabled;
        public CinematicStageSO stage;
    }

    [CreateAssetMenu(
        fileName = "CinematicStage",
        menuName = "UPlayGround/전투/Cinematic Stage")]
    public sealed class CinematicStageSO : ScriptableObject
    {
        [Header("등급")]
        public CinematicStageTier tier = CinematicStageTier.CasterClone;
        public CinematicStageFallback fallback = CinematicStageFallback.DemoteToCameraOnly;

        [Header("무대")]
        [Tooltip("부팅 단계에서 Additive로 미리 로드해 둔 씬 이름입니다. 발동 중에는 씬을 로드하지 않습니다.")]
        public string stageSceneName;
        public GameObject stagePrefab;
        public Vector3 anchorOffset = new(0f, 400f, 0f);
        public bool alignStageYawToTarget = true;

        [Header("타깃 표현")]
        public CinematicTargetRepresentation targetMode =
            CinematicTargetRepresentation.Silhouette;
        public GameObject silhouettePrefab;
        public UltimateTargetSizeAnchors sizeAnchors;
        [Min(0.1f)] public float smallHeight = 1.2f;
        [Min(0.1f)] public float largeHeight = 3.5f;
        [Min(0.1f)] public float giantHeight = 7f;

        [Header("렌더/조명")]
        public LayerMask stageCullingMask;
        public VolumeProfile stageVolumeProfile;
        public bool hideSourceRenderers = true;

        [Header("전환")]
        public CinematicStageTransitionType enterTransition =
            CinematicStageTransitionType.WhiteFlash;
        [Min(0f)] public float enterTransitionDuration = 0.12f;
        public CinematicStageTransitionType exitTransition =
            CinematicStageTransitionType.Dissolve;
        [Min(0f)] public float exitTransitionDuration = 0.2f;

        [Header("안전장치")]
        [Min(1f)] public float maxStageSeconds = 30f;

        public UltimateTargetSize ClassifyTarget(float height)
        {
            if (height < smallHeight)
                return UltimateTargetSize.Small;
            if (height >= giantHeight)
                return UltimateTargetSize.Giant;
            if (height >= largeHeight)
                return UltimateTargetSize.Large;
            return UltimateTargetSize.Medium;
        }

        private void OnValidate()
        {
            smallHeight = Mathf.Max(0.1f, smallHeight);
            largeHeight = Mathf.Max(smallHeight, largeHeight);
            giantHeight = Mathf.Max(largeHeight, giantHeight);
            enterTransitionDuration = Mathf.Max(0f, enterTransitionDuration);
            exitTransitionDuration = Mathf.Max(0f, exitTransitionDuration);
            maxStageSeconds = Mathf.Max(1f, maxStageSeconds);
        }
    }
}
