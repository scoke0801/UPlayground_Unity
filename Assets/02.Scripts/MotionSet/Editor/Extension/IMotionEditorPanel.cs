namespace UPlayGround.Animation.Editor
{
    public interface IMotionEditorPanel
    {
        string Title { get; }
        int Order { get; }
        bool IsAvailable(IMotionEditorContext context);
        void OnGUI(IMotionEditorContext context);
        void OnSceneGUI(IMotionEditorContext context);
        void OnPlaybackStateChanged(
            IMotionEditorContext context,
            MotionPreviewPlaybackState state);
    }

    /// <summary>
    /// 임시 Scene 오브젝트나 EditorApplication 콜백을 소유하는 패널의 선택적 정리 계약.
    /// </summary>
    public interface IMotionEditorPanelLifecycle
    {
        void OnEditorClosed(IMotionEditorContext context);
    }
}
