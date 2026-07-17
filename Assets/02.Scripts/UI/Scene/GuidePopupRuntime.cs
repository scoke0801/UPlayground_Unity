using UPlayGround.Data.Path;
using UPlayGround.Data.UI;
using UPlayGround.Manager;

namespace UPlayGround.UI.Guide
{
    /// <summary>
    /// 런타임에서 가이드 팝업을 표준 방식으로 여는 헬퍼.
    /// UI_GuidePopup이 입력 차단과 게임 일시정지를 담당한다.
    /// </summary>
    public static class GuidePopupRuntime
    {
        public static UI_GuidePopup Open(GuidePopupDataSO data, int startPageIndex = 0)
        {
            if (data == null || UISvc.UI == null)
                return null;

            var go = UISvc.UI.ShowUI(UIKeyType.GuidePopup, CanvasLayer.Popup);
            var popup = go != null ? go.GetComponent<UI_GuidePopup>() : null;
            popup?.Setup(data, startPageIndex);
            return popup;
        }

        public static bool IsOpen()
        {
            var popup = UISvc.UI?.GetUI<UI_GuidePopup>(UIKeyType.GuidePopup);
            return popup != null && popup.IsVisible;
        }

        public static void Close()
        {
            UISvc.UI?.HideUI(UIKeyType.GuidePopup);
        }
    }
}
