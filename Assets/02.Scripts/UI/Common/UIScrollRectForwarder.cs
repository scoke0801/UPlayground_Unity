using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UPlayGround.UI
{
    /// <summary>
    /// 자기 자신 또는 하위에서 발생한 휠 스크롤을 지정한 ScrollRect로 넘긴다.
    ///
    /// 스크롤 대상 위에 전체 영역 버튼처럼 레이캐스트를 가로채는 요소가 얹혀 있으면,
    /// 스크롤 이벤트는 그 요소의 '조상'으로만 버블링되므로 형제인 ScrollRect에는 닿지 않는다.
    /// 이 컴포넌트를 공통 조상에 붙여 두면 그 경로에서 ScrollRect로 이벤트를 되돌릴 수 있다.
    /// </summary>
    [DisallowMultipleComponent]
    public class UIScrollRectForwarder : MonoBehaviour, IScrollHandler
    {
        [Tooltip("스크롤을 전달할 대상. 비우면 하위에서 처음 찾은 ScrollRect를 사용한다.")]
        [SerializeField] private ScrollRect target;

        public ScrollRect Target
        {
            get => target;
            set => target = value;
        }

        public void OnScroll(PointerEventData eventData)
        {
            ScrollRect scrollRect = ResolveTarget();
            if (scrollRect == null || !scrollRect.isActiveAndEnabled)
                return;

            // 스크롤할 여지가 없으면 넘기지 않아, 상위 스크롤 영역이 있으면 그쪽이 처리하도록 둔다.
            if (!HasScrollableContent(scrollRect))
                return;

            scrollRect.OnScroll(eventData);
        }

        private ScrollRect ResolveTarget()
        {
            if (target == null)
                target = GetComponentInChildren<ScrollRect>(true);

            return target;
        }

        private static bool HasScrollableContent(ScrollRect scrollRect)
        {
            RectTransform content = scrollRect.content;
            RectTransform viewport = scrollRect.viewport;
            if (content == null || viewport == null)
                return false;

            return content.rect.height > viewport.rect.height + 0.5f;
        }
    }
}
