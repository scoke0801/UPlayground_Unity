using UnityEngine;

namespace UPlayGround.UI
{
    /// <summary>
    /// UIRoot가 소유하는 런타임 비주얼 테마 제공자.
    /// 공용 컨트롤은 개별 테마가 없을 때 이 값을 사용한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIVisualThemeProvider : MonoBehaviour
    {
        [SerializeField] private UIVisualThemeSO _theme;

        public static UIVisualThemeSO Current { get; private set; }

        private void Awake()
        {
            if (_theme != null)
                Current = _theme;
        }

        private void OnDestroy()
        {
            if (Current == _theme)
                Current = null;
        }
    }
}
