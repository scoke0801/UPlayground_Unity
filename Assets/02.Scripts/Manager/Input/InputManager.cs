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
        private int _playerActionSuppressedUntilFrame = -1;
        private float _playerActionSuppressedUntilTime = -1f;
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

            // 활성 디바이스(키보드+마우스 ↔ 게임패드) 감지 시작
            InitDeviceDetection();

            Debug.Log("[InputManager] 초기화 완료");
        }

        public void AfterInit()
        {
            
        }

        public void Dispose()
        {
            Debug.Log("[InputManager] 정리 시작");

            DisposeDeviceDetection();

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
            // force가 아닌 push로 처리해야 UI가 올린 커서 스택을 덮어쓰지 않는다.
            // (force로 두면 Alt를 눌렀다 떼는 순간 스택이 0으로 떨어져 열린 UI 위에서 커서가 사라짐)
            ShowCursor(true);
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

        /// <summary>
        /// 현재 열려 있는 "입력 차단 모달"들을 기준으로 입력 레이어를 재계산한다.
        /// 모달 Show(올림)·Hide(복원) 양쪽에서 호출해 두 기준을 대칭으로 맞춘다.
        /// 차단 모달이 하나도 없으면 Level_0(게임플레이)으로 내려간다.
        /// 파생값 재계산이라 누적 상태가 없어 순서·재진입에 관계없이 항상 정합하다.
        /// </summary>
        public void RefreshInputLayer()
        {
            if (UIManager.Instance == null)
            {
                return;
            }

            InputLayer layer = UIManager.Instance.GetTopBlockingInputLayer();
            if (CurrentLayer == layer)
            {
                return;
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

        /// <summary>
        /// UI 닫기처럼 같은 물리 입력이 게임플레이 입력으로 새는 전환 구간에서 PlayerAction만 짧게 차단한다.
        /// UI/System 입력은 그대로 통과하며, 기존 전투 버퍼도 함께 비워 닫기 클릭 잔여 입력을 제거한다.
        /// </summary>
        public void SuppressPlayerActionInputBriefly(float seconds = 0.05f, int frameCount = 1)
        {
            _inputBuffer?.Clear();

            int untilFrame = Time.frameCount + Mathf.Max(0, frameCount);
            if (untilFrame > _playerActionSuppressedUntilFrame)
                _playerActionSuppressedUntilFrame = untilFrame;

            float untilTime = Time.unscaledTime + Mathf.Max(0f, seconds);
            if (untilTime > _playerActionSuppressedUntilTime)
                _playerActionSuppressedUntilTime = untilTime;
        }

        // 모션 툴 프리뷰 등에서 Player 액션 입력은 막되 Look(카메라 회전)만 통과시키고 싶을 때 사용.
        public void SetPlayerActionLookAllowed(bool allowed)
        {
            _allowLookDuringSuppression = allowed;
        }

        public bool IsPlayerActionLookAllowed => _allowLookDuringSuppression;

        #endregion
    }

}
