#if UNITY_EDITOR
namespace UPlayGround.Tool.Editor.Map
{
    /// <summary>
    /// 기존 타입 호환용 진입점. 실제 구현과 메뉴는 통합 월드 배치 도구가 담당한다.
    /// </summary>
    public static class MapPlacementEditorWindow
    {
        public static void Open()
        {
            GatheringPlacementEditorWindow.OpenActorPlacement();
        }
    }
}
#endif
