using UnityEngine;

namespace UPlayGround.Manager
{
    /// <summary>궁극기 얼굴 패널의 실시간 표현값. 전투 시계에는 관여하지 않는다.</summary>
    public readonly struct FaceStripRequest
    {
        public readonly Sprite Face;
        public readonly Color Accent;
        public readonly float InDuration, HoldDuration, OutDuration;
        public readonly float FaceDelay, BreathScale, FlashDuration;

        public FaceStripRequest(Sprite face, Color accent, float inDuration, float holdDuration,
            float outDuration, float faceDelay, float breathScale, float flashDuration)
        {
            Face = face;
            Accent = accent;
            InDuration = Mathf.Max(0f, inDuration);
            HoldDuration = Mathf.Max(0f, holdDuration);
            OutDuration = Mathf.Max(0f, outDuration);
            FaceDelay = Mathf.Clamp(faceDelay, 0f, InDuration);
            BreathScale = Mathf.Clamp(breathScale, 0f, 0.02f);
            FlashDuration = Mathf.Clamp(flashDuration, 0f, HoldDuration);
        }
    }

    /// <summary>표시 전용 패널을 교체하고 발급한 소유 토큰으로만 정리한다.</summary>
    public interface IFaceStripPresenter : IGameService
    {
        long Show(FaceStripRequest request);
        void Hide(long token);
    }
}
