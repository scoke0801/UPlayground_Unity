using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UPlayGround.Animation.Editor
{
    /// <summary>
    /// Play Mode 프리뷰 중 Game 뷰 위의 마우스 휠(줌)과 우클릭 드래그(회전)를 프리뷰 시점 조작으로 넘긴다.
    /// </summary>
    /// <remarks>
    /// Input System 기본 설정(PointersAndKeyboardsRespectGameViewFocus)은 Game 뷰에 포커스가 없으면
    /// 마우스 입력을 게임에 전달하지 않는다. 툴 창을 조작하는 동안 Game 뷰는 포커스를 잃으므로
    /// 게임 입력 맵만으로는 프리뷰 시점을 돌릴 수 없다. 그래서 Game 뷰 패널의 에디터 이벤트를 직접 받아
    /// 포커스와 무관하게 동작시킨다. 게임이 커서를 잠그고 마우스를 직접 읽는 동안에는 이중 입력이 되므로 양보한다.
    /// </remarks>
    internal sealed class MotionPreviewGameViewInput : IDisposable
    {
        // 에디터 휠 이벤트는 Windows 기본 설정에서 노치당 3줄 단위로 들어온다.
        private const float WheelDeltaPerNotch = 3f;
        private const int OrbitButton = 1;
        private const double RehookIntervalSeconds = 1d;

        private static readonly Type GameViewType =
            typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");

        private readonly Func<bool> _canControl;
        private readonly Action<Vector2, float> _applyViewInput;
        private readonly Dictionary<VisualElement, EditorWindow> _hookedPanels = new();
        private readonly List<VisualElement> _staleRoots = new();
        private double _nextRehookTime;
        private bool _isOrbiting;

        public MotionPreviewGameViewInput(
            Func<bool> canControl,
            Action<Vector2, float> applyViewInput)
        {
            _canControl = canControl;
            _applyViewInput = applyViewInput;
        }

        /// <summary>조작 가능할 때만 Game 뷰에 훅을 유지하고, 도킹 변경으로 패널이 바뀌면 다시 연결한다.</summary>
        public void Tick()
        {
            if (!_canControl())
            {
                UnhookAll();
                return;
            }

            double now = EditorApplication.timeSinceStartup;
            if (now < _nextRehookTime)
                return;

            _nextRehookTime = now + RehookIntervalSeconds;
            RefreshHooks();
        }

        public void Dispose()
        {
            UnhookAll();
        }

        private void RefreshHooks()
        {
            if (GameViewType == null)
                return;

            RemoveStaleHooks();
            foreach (UnityEngine.Object found in Resources.FindObjectsOfTypeAll(GameViewType))
            {
                if (found is not EditorWindow gameView)
                    continue;

                // 같은 도킹 영역의 비활성 탭은 패널에 붙어 있지 않다. 탭이 보이면 다음 갱신에서 연결된다.
                VisualElement root = gameView.rootVisualElement.panel?.visualTree;
                if (root == null || _hookedPanels.ContainsKey(root))
                    continue;

                Hook(root);
                _hookedPanels.Add(root, gameView);
            }
        }

        private void RemoveStaleHooks()
        {
            _staleRoots.Clear();
            foreach (KeyValuePair<VisualElement, EditorWindow> pair in _hookedPanels)
            {
                EditorWindow gameView = pair.Value;
                if (gameView == null || gameView.rootVisualElement.panel?.visualTree != pair.Key)
                    _staleRoots.Add(pair.Key);
            }

            foreach (VisualElement root in _staleRoots)
            {
                Unhook(root);
                _hookedPanels.Remove(root);
            }
            _staleRoots.Clear();
        }

        private void UnhookAll()
        {
            _isOrbiting = false;
            _nextRehookTime = 0d;
            if (_hookedPanels.Count == 0)
                return;

            foreach (VisualElement root in _hookedPanels.Keys)
                Unhook(root);
            _hookedPanels.Clear();
        }

        private void Hook(VisualElement root)
        {
            root.RegisterCallback<WheelEvent>(OnWheel, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
        }

        private void Unhook(VisualElement root)
        {
            root.UnregisterCallback<WheelEvent>(OnWheel, TrickleDown.TrickleDown);
            root.UnregisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            root.UnregisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            root.UnregisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
        }

        private void OnWheel(WheelEvent evt)
        {
            if (!CanHandleHoveredGameView())
                return;

            // 에디터 휠은 아래로 굴릴 때 +y다. 시점 계약은 +가 줌인이므로 부호를 뒤집는다.
            _applyViewInput(Vector2.zero, -evt.delta.y / WheelDeltaPerNotch);
            // Game 뷰가 같은 휠로 화면 배율을 바꾸지 않게 소비한다.
            evt.StopPropagation();
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button == OrbitButton && CanHandleHoveredGameView())
                _isOrbiting = true;
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_isOrbiting)
                return;

            // 창 밖에서 버튼을 떼 PointerUp을 놓쳐도 버튼 상태로 드래그 종료를 판정한다.
            bool isOrbitButtonHeld = (evt.pressedButtons & (1 << OrbitButton)) != 0;
            if (!isOrbitButtonHeld || !_canControl())
            {
                _isOrbiting = false;
                return;
            }

            if (IsGameReadingMouse())
                return;

            // 패널 좌표는 포인트·아래쪽 +y다. 장치 마우스 델타(픽셀·위쪽 +y)와 단위를 맞춘다.
            Vector2 delta = evt.deltaPosition * EditorGUIUtility.pixelsPerPoint;
            _applyViewInput(new Vector2(delta.x, -delta.y), 0f);
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (evt.button == OrbitButton)
                _isOrbiting = false;
        }

        private bool CanHandleHoveredGameView()
        {
            EditorWindow hovered = EditorWindow.mouseOverWindow;
            return hovered != null
                   && GameViewType.IsInstanceOfType(hovered)
                   && _canControl()
                   && !IsGameReadingMouse();
        }

        /// <summary>Game 뷰가 포커스를 갖고 커서를 잠근 상태면 게임이 마우스를 직접 읽어 시점을 돌린다.</summary>
        private static bool IsGameReadingMouse()
        {
            EditorWindow focused = EditorWindow.focusedWindow;
            return focused != null
                   && GameViewType.IsInstanceOfType(focused)
                   && UnityEngine.Cursor.lockState == CursorLockMode.Locked;
        }
    }
}
