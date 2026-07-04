using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 마우스 호버 대상 UI를 EventSystem 선택 상태로 동기화한다.
/// 키보드/게임패드 포커스 하이라이트와 마우스 호버를 같은 선택 흐름으로 맞출 때 사용한다.
/// </summary>
[RequireComponent(typeof(Selectable))]
public class UISelectOnPointerEnter : MonoBehaviour, IPointerEnterHandler
{
    private Selectable _selectable;

    private void Awake()
    {
        _selectable = GetComponent<Selectable>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (EventSystem.current == null)
            return;

        if (_selectable == null)
            _selectable = GetComponent<Selectable>();

        if (_selectable == null || !_selectable.IsInteractable())
            return;

        EventSystem.current.SetSelectedGameObject(gameObject);
    }
}
