using Animancer;
using UnityEngine;

namespace UPlayGround.Animation.Editor
{
    /// <summary>
    /// 애니메이션 에디터가 프리뷰 대상에 요구하는 최소 계약.
    /// </summary>
    public interface IMotionPreviewSubject
    {
        GameObject Root { get; }
        AnimancerComponent Animancer { get; }
        AvatarMask GetLayerMask(int layerIndex);
        IMotionSetCatalog Catalog { get; }
        void Refresh();
    }

    public interface IMotionPreviewRootMotion
    {
        Vector3 DeltaPosition { get; }
        Quaternion DeltaRotation { get; }

        /// <summary>
        /// 누적 수평 루트 변위와 누적 수평 경로 길이. 구간 총량 계측용이다.
        /// DeltaPosition 을 프레임마다 더하는 방식은 한 프레임에 루트모션 평가가 여러 번 일어나면
        /// 마지막 한 번만 세어 항상 과소 계측되므로, 총량을 재는 쪽은 이 누적값의 차이를 써야 한다.
        /// </summary>
        bool TryGetRootMotionOdometer(out Vector3 worldTotal, out float pathLength);
        void SetSimulationSuspended(bool suspended);
        void Teleport(Vector3 position, Quaternion rotation);
    }

    public interface IMotionPreviewInputLock
    {
        void SetInputSuppressed(bool suppressed, bool allowCameraLook);
        void ClearBufferedInput();
    }

    /// <summary>
    /// 런타임 애니메이션 시스템과 같은 Animancer 그래프를 공유하는 대상의
    /// 외부 프리뷰 소유권을 재생 구간 동안만 획득한다.
    /// </summary>
    public interface IMotionPreviewPlaybackOwnership
    {
        void AcquirePreviewOwnership();
        void ReleasePreviewOwnership();
    }

    public interface IMotionPreviewVariants
    {
        System.Collections.Generic.IReadOnlyList<MotionPreviewAxis> Axes { get; }
        string GetSelected(string axisId);
        bool Select(string axisId, string optionId);
    }

    public interface IMotionPreviewStatusOverlay
    {
        string GetSceneStatusText();
    }

    /// <summary>
    /// 프리뷰 데이터에서 생성된 대상의 AI·물리 상태를 안전하게 정지하고 복구한다.
    /// 씬에 이미 존재하는 수동 대상에는 <c>spawned=false</c>가 전달된다.
    /// </summary>
    public interface IMotionPreviewSubjectSession
    {
        void OnPreviewLoaded(bool spawned);
        void OnPreviewReleased();
    }
}
