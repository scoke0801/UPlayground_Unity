using System;
using UnityEngine;

namespace UPlayGround.UI
{
    /// <summary>
    /// 모든 화면 위에 뜨는 전역 FPS 표시의 켜짐 상태와 화면 인스턴스를 관리한다.
    /// 켜짐 상태는 static이라 씬 표식이 UIManager 초기화보다 먼저 켜도 유지되고,
    /// 인스턴스가 만들어지면 그 시점의 상태를 따른다. 씬이 바뀌어도 상태는 유지된다.
    /// </summary>
    public sealed class UIFrameRateOverlay : IDisposable
    {
        /// <summary>전역 FPS 표시가 켜져 있는지 반환한다.</summary>
        public static bool IsVisible { get; private set; }

        private static event Action VisibilityChanged;

        private GameObject _instance;

        /// <summary>전역 FPS 표시를 켜거나 끈다.</summary>
        public static void SetVisible(bool visible)
        {
            if (IsVisible == visible)
                return;

            IsVisible = visible;
            VisibilityChanged?.Invoke();
        }

        /// <summary>전역 FPS 표시의 켜짐 상태를 뒤집는다.</summary>
        public static void ToggleVisible() => SetVisible(!IsVisible);

        // 도메인 리로드를 끈 Play 진입에서도 이전 세션의 켜짐 상태와 구독이 남지 않게 한다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPlaySessionState()
        {
            IsVisible = false;
            VisibilityChanged = null;
        }

        /// <summary>
        /// 표시 프리팹을 parent 아래에 만들고 켜짐 상태를 따르게 한다.
        /// parent는 캔버스가 아닌 Transform이어야 프리팹 자체 캔버스의 정렬 순서로 모든 레이어 위에 그려진다.
        /// </summary>
        public UIFrameRateOverlay(GameObject prefab, Transform parent)
        {
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            _instance = UnityEngine.Object.Instantiate(prefab, parent, false);
            _instance.name = prefab.name;
            ApplyVisibility();
            VisibilityChanged += ApplyVisibility;
        }

        /// <summary>상태 구독을 끊고 화면 인스턴스를 파괴한다.</summary>
        public void Dispose()
        {
            VisibilityChanged -= ApplyVisibility;
            if (_instance == null)
                return;

            // EditMode 검증에서는 지연 파괴가 금지되므로 즉시 파괴한다.
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(_instance);
            else
                UnityEngine.Object.DestroyImmediate(_instance);
            _instance = null;
        }

        // 꺼진 동안 오브젝트를 비활성화해 측정 Update와 캔버스 리빌드 비용을 남기지 않는다.
        // 진단 표시는 즉시 반응해야 하므로 열고 닫기 트윈을 넣지 않는다.
        private void ApplyVisibility()
        {
            if (_instance != null)
                _instance.SetActive(IsVisible);
        }
    }
}
