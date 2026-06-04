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
        private bool _isPlayerActionInputSuppressed;
        private bool _allowLookDuringSuppression;
        // ShouldSuppressPlayerActionInput 핫패스에서 문자열 비교 회피용 — InitInputAction 직후 1회 캐시.
        private InputAction _cachedLookAction;
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
            Vector2 hotspot = new Vector2(cursorTexture.width * 0.27f, 0f);
            Cursor.SetCursor(cursorTexture, hotspot, CursorMode.Auto);

            // Actions 초기화
            InitInputAction();

            // Look 액션 참조 캐시 — 입력 콜백마다 actionCache lookup 회피.
            _cachedLookAction = GetAction(InputMapNames.PlayerAction, InputDefine.PlayerAction.Look);

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

        public void ShowCursor(bool isShow, bool isForce = false)
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

        public void SetPlayerActionInputSuppressed(bool suppressed)
        {
            _isPlayerActionInputSuppressed = suppressed;
            if (suppressed)
                _inputBuffer?.Clear();
        }

        public bool IsPlayerActionInputSuppressed => _isPlayerActionInputSuppressed;

        // 모션 툴 프리뷰 등에서 Player 액션 입력은 막되 Look(카메라 회전)만 통과시키고 싶을 때 사용.
        public void SetPlayerActionLookAllowed(bool allowed)
        {
            _allowLookDuringSuppression = allowed;
        }

        public bool IsPlayerActionLookAllowed => _allowLookDuringSuppression;

        #endregion
    }

}
