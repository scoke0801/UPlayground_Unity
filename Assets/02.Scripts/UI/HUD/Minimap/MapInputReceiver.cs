using UnityEngine;
using UnityEngine.EventSystems;

namespace UPlayGround.UI
{
    /// <summary>
    /// UI_Scene_Map의 드래그·스크롤 입력을 중계하는 경량 컴포넌트.
    /// MapViewport 오브젝트에 부착하세요 (Image 컴포넌트 필요 — raycastTarget = true).
    /// </summary>
    [RequireComponent(typeof(UnityEngine.UI.Graphic))]
    public class MapInputReceiver : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IScrollHandler, IPointerClickHandler
    {
        public event System.Action<PointerEventData> OnBeginDragEvent;
        public event System.Action<PointerEventData> OnDragEvent;
        public event System.Action<PointerEventData> OnScrollEvent;
        /// <summary>
        /// 주 입력(마우스 좌클릭 또는 가상 커서 Submit) 시 발행.
        /// 지도처럼 좌표를 직접 지시하는 화면에서 마우스와 패드가 같은 동작을 공유한다.
        /// 드래그로 끝난 입력은 발행하지 않는다.
        /// </summary>
        public event System.Action<PointerEventData> OnPrimaryClickEvent;
        /// <summary>우클릭 (PointerEventData.InputButton.Right) 시 발행. 드래그로 끝난 입력은 발행하지 않는다.</summary>
        public event System.Action<PointerEventData> OnRightClickEvent;

        void IBeginDragHandler.OnBeginDrag(PointerEventData e) => OnBeginDragEvent?.Invoke(e);
        void IDragHandler.OnDrag(PointerEventData e)           => OnDragEvent?.Invoke(e);
        void IScrollHandler.OnScroll(PointerEventData e)       => OnScrollEvent?.Invoke(e);

        void IPointerClickHandler.OnPointerClick(PointerEventData e)
        {
            // EventSystem은 누른 대상과 드래그 대상이 다를 때만 클릭을 취소한다.
            // 이 컴포넌트는 둘을 한 오브젝트에서 받으므로, 패닝을 끝내며 뗀 버튼이 클릭으로 들어온다.
            if (e.dragging)
                return;

            if (e.button == PointerEventData.InputButton.Left)
                OnPrimaryClickEvent?.Invoke(e);
            else if (e.button == PointerEventData.InputButton.Right)
                OnRightClickEvent?.Invoke(e);
        }
    }
}
