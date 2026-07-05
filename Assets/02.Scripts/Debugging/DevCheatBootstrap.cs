#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround.Manager;
using UPlayGround.UI.DevCheat;

namespace UPlayGround.Debugging
{
    /// <summary>
    /// 개발 치트 패널 진입점. 개발 빌드(및 에디터)에서만 컴파일된다.
    ///
    /// F11 을 코드 생성 <see cref="InputAction"/>(<c>&lt;Keyboard&gt;/f11</c>)으로 매핑하고,
    /// 눌릴 때마다 <c>UIManager</c>의 "DevCheatPanel" 팝업을 토글한다.
    /// 별도 씬 배치 없이 <see cref="RuntimeInitializeOnLoadMethod"/> 로 자동 부트스트랩된다.
    /// </summary>
    public sealed class DevCheatBootstrap : MonoBehaviour
    {
        private const string PanelKey = "DevCheatPanel";

        private static DevCheatBootstrap s_instance;
        private InputAction _toggleAction;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (s_instance != null)
                return;

            var go = new GameObject(nameof(DevCheatBootstrap));
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            s_instance = go.AddComponent<DevCheatBootstrap>();
        }

        private void OnEnable()
        {
            _toggleAction = new InputAction("DevCheatToggle", InputActionType.Button, "<Keyboard>/f11");
            _toggleAction.performed += OnTogglePerformed;
            _toggleAction.Enable();
        }

        private void OnDisable()
        {
            if (_toggleAction == null)
                return;

            _toggleAction.performed -= OnTogglePerformed;
            _toggleAction.Disable();
            _toggleAction.Dispose();
            _toggleAction = null;
        }

        private static void OnTogglePerformed(InputAction.CallbackContext _)
        {
            var mgr = UIManager.Instance;
            if (mgr == null)
                return;

            var panel = mgr.GetUI<UI_DevCheatPanel>();
            if (panel != null && panel.IsVisible)
                panel.Hide();
            else
                mgr.ShowUI(PanelKey);
        }
    }
}
#endif
