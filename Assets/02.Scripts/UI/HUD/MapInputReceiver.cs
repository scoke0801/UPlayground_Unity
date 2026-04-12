using UnityEngine;
using UnityEngine.EventSystems;

namespace UPlayGround.UI
{
    /// <summary>
    /// UI_Map의 드래그·스크롤 입력을 중계하는 경량 컴포넌트.
    /// MapViewport 오브젝트에 부착하세요 (Image 컴포넌트 필요 — raycastTarget = true).
    /// </summary>
    [RequireComponent(typeof(UnityEngine.UI.Graphic))]
    public class MapInputReceiver : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IScrollHandler
    {
        public event System.Action<PointerEventData> OnBeginDragEvent;
        public event System.Action<PointerEventData> OnDragEvent;
        public event System.Action<PointerEventData> OnScrollEvent;
        
        void IBeginDragHandler.OnBeginDrag(PointerEventData e) => OnBeginDragEvent?.Invoke(e);
        void IDragHandler.OnDrag(PointerEventData e)           => OnDragEvent?.Invoke(e);
        void IScrollHandler.OnScroll(PointerEventData e)       => OnScrollEvent?.Invoke(e);
    }
}
