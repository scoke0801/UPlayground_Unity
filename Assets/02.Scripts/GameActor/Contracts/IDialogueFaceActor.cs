namespace UPlayGround
{
    /// <summary>대화 참여자의 모델별 얼굴 보정을 기존 관리 틱에서 호출한다.</summary>
    public interface IDialogueFaceActor
    {
        void RestoreDialogueFace();
        void SetDialogueFaceIntent(bool suppressLook, bool suppressBlink);
        void ApplyDialogueFace(bool paused);
    }
}
