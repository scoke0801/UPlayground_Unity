using Game.Input;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround.InputDefine;

namespace UPlayGround.Manager
{
    /// <summary>
    /// 입력 시스템 관리 매니저
    /// </summary>
    public partial class InputManager : BaseManager<InputManager>, IManager
    {
        private int _cursorVisibleStack = 0;
        private InputBuffer _inputBuffer; // InputBuffer 선언
        private bool _isGamepadActive = false;

        public InputLayer CurrentLayer { get; set; } = InputLayer.Level_0;

        #region IManager 구현

        public void Init()
        {
            Debug.Log("[InputManager] 초기화 시작");

            _inputBuffer = new InputBuffer(); // InputBuffer 초기화
            Debug.Log("[InputManager] InputBuffer 초기화 완료");

            Texture2D cursorTexture = Resources.Load<Texture2D>("Cursor/cursor_default");
            ;

            Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.Auto);

            // Actions 초기화
            InitInputAction();

            ShowCursor(false, false);

            RegisterInputEvent(InputMapNames.System, SystemAction.ShowCursor, OnStartedShowCursor, null,
                OnCanceledShowCursor, null, null, InputLayer.Level_Top);
            Debug.Log("[InputManager] 초기화 완료");
        }

        public void AfterInit()
        {
            
        }

        public void Dispose()
        {
            Debug.Log("[InputManager] 정리 시작");

            startCallbackDict.Clear();
            performCallbackDict.Clear();
            cancelCallbackDict.Clear();
            
            Debug.Log("[InputManager] 정리 완료");
        }

        public void OnUpdate()
        {
        }

        public void OnFixedUpdate()
        {
        }

        public void OnLateUpdate()
        {
        }

        public void OnSceneChanged(string sceneType) { }

        #endregion

        #region 유틸리티

        private void ShowCursor(bool isShow, bool isForce = false)
        {
            if (isForce)
            {
                _cursorVisibleStack = isShow ? 1 : 0;
            }
            else
            {
                if (isShow)
                {
                    ++_cursorVisibleStack;
                }
                else
                {
                    _cursorVisibleStack = math.max(0, _cursorVisibleStack - 1);
                }
            }

            RefreshCursorState();

            Debug.Log($"ShowCursor: {Cursor.visible}, stackCount: {_cursorVisibleStack}");
        }

        private void OnStartedShowCursor(InputAction.CallbackContext obj)
        {
            ShowCursor(true, true);
        }

        private void OnCanceledShowCursor(InputAction.CallbackContext obj)
        {
            ShowCursor(false);
        }

        private void RefreshCursorState()
        {
           // Debug.Log($"CursorStack: {_cursorVisibleStack}, gamePadConnected: {_isGamepadActive}");
            bool finalVisibility = _cursorVisibleStack > 0;

            if (finalVisibility)
            {
                if (_isGamepadActive)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;

                //    Debug.Log("[InputManager] Cursor Show");
                }
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        public void SetInputLayer(InputLayer layer)
        {
            if (CurrentLayer == layer)
            {
                return;
            }

            if (layer == InputLayer.None)
            {
                layer = UIManager.Instance.GetTopCanvasLayer().ToInputLayer();
            }
            CurrentLayer = layer;

            InvokeCancelEvents(layer);
        }

        #endregion
    }

}