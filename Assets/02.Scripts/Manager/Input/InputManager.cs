using System.Collections.Generic;
using UPlayGround.Input;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround.InputDefine;
using UPlayGround.Diagnostics;

namespace UPlayGround.Manager
{
    /// <summary>
    /// 입력 시스템 관리 매니저
    /// </summary>
    // IUpdatableManager를 붙여야 GameManager의 틱 목록에 등록된다.
    // IManager.OnUpdate 선언만으로는 호출되지 않는다(조합 grace 만료 처리에 필요).
    public partial class InputManager
        : BaseManager<InputManager>, IManager, IUpdatableManager, ILateUpdatableManager, IInputService
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
        private readonly Dictionary<string, int> _pendingSyntheticPlayerActionReleases = new();
        private readonly List<string> _syntheticReleaseScratch = new();

        public InputLayer CurrentLayer { get; private set; } = InputLayer.Level_0;

        #region IManager 구현

        public void Init()
        {
            RuntimeLog.Trace(
                RuntimeLogCategory.Input | RuntimeLogCategory.System,
                "[InputManager] 초기화 시작");

            _inputBuffer = new InputBuffer(); // InputBuffer 초기화
            RuntimeLog.Trace(
                RuntimeLogCategory.Input | RuntimeLogCategory.System,
                "[InputManager] InputBuffer 초기화 완료");

            Texture2D cursorTexture = Resources.Load<Texture2D>("Cursor/cursor_default");
            ;
            Vector2 hotspot = new Vector2(cursorTexture.width * 0.27f, 0f);
            Cursor.SetCursor(cursorTexture, hotspot, CursorMode.Auto);

            // Actions 초기화 (내부에서 바인딩 프로필 적용 → 조합 카탈로그 구축까지 수행)
            InitInputAction();

            // Look 액션 참조 캐시 — 입력 콜백마다 actionCache lookup 회피.
            _cachedLookAction = GetAction(InputMapNames.PlayerAction, InputDefine.PlayerAction.Look);

            ShowCursor(false, false);

            RegisterInputEvent(InputMapNames.System, SystemAction.ShowCursor, OnStartedShowCursor, null,
                OnCanceledShowCursor, null, null, InputLayer.Level_Top);

            // 활성 디바이스(키보드+마우스 ↔ 게임패드) 감지 시작
            InitDeviceDetection();

            RuntimeLog.Trace(
                RuntimeLogCategory.Input | RuntimeLogCategory.System,
                "[InputManager] 초기화 완료");
        }

        public void AfterInit()
        {
            
        }

        public void Dispose()
        {
            RuntimeLog.Trace(
                RuntimeLogCategory.Input | RuntimeLogCategory.System,
                "[InputManager] 정리 시작");

            // 보류 중인 합성 입력은 그냥 비우면 hold 플래그(차지·스킬 홀드)가 소비자에 남는다.
            // 프레임 조건을 무시하고 cancel을 먼저 발화한 뒤 정리한다.
            ReleaseSyntheticPlayerActions(force: true);
            StopHaptics();

            DisposeDeviceDetection();
            DisposeInputActions();
            OnBindingsChanged = null;
            OnBindingStructureChanged = null;
            OnRebindCaptureChanged = null;

            startCallbackDict.Clear();
            performCallbackDict.Clear();
            cancelCallbackDict.Clear();
            _chordArbiter.ClearCatalog();
            _chordArbiter.Reset();
            _arbiterDispatch.Clear();
            _pendingSyntheticPlayerActionReleases.Clear();
            _syntheticReleaseScratch.Clear();
            _emptyCallbackKeyScratch.Clear();
            _callbackDispatchDepth = 0;
            _hasPendingCallbackCleanup = false;


            RuntimeLog.Trace(
                RuntimeLogCategory.Input | RuntimeLogCategory.System,
                "[InputManager] 정리 완료");
        }

        public void OnUpdate()
        {
            // grace가 만료된 보류 입력을 확정한다.
            TickChordArbiter();
            TickHaptics();
        }

        public void OnFixedUpdate()
        {
        }

        public void OnLateUpdate()
        {
            // 합성 performed 다음 프레임의 모든 Update가 끝난 뒤 release한다.
            // PlayerActor와 GameManager의 Update 실행 순서와 무관하게 Pressed 스냅샷을
            // 최소 한 번 PlayerMovementController에 전달한 뒤 canceled가 발화한다.
            ReleaseSyntheticPlayerActions();
        }

        public void OnSceneChanged(string sceneType) { }

        protected override void OnDestroy()
        {
            // 같은 GameObject의 컴포넌트 파괴 순서는 보장되지 않는다. GameManager가 먼저
            // Dispose하지 못한 경로에서도 InputSystem 전역 구독이 다음 플레이 세션에 남지 않게 한다.
            StopHaptics();
            DisposeDeviceDetection();
            DisposeInputActions();
            base.OnDestroy();
        }

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

            // 스펙 §9.4: 컨텍스트가 바뀌면 대기 중 단일키 후보와 provisional hold를 폐기한다.
            // 외부에는 아래 InvokeCancelEvents의 cancelCallback 1회로만 통지한다.
            _chordArbiter.Reset();

            // 일시정지 UI는 scaled time을 멈출 수 있다. 진입 전에 남은 전투 선입력을
            // 유지하면 UI를 닫은 뒤 공격·회피가 뒤늦게 발동하므로 즉시 폐기한다.
            if (layer > InputLayer.Level_0)
                _inputBuffer?.Clear();

            InvokeCancelEvents(layer);
        }

        /// <summary>플레이어 액션 차단 진입 시 버퍼와 진행 중 홀드 상태를 함께 정리한다.</summary>
        public void SetPlayerActionInputSuppressed(bool suppressed)
        {
            bool enteredSuppression = suppressed && !_isPlayerActionInputSuppressed;
            _isPlayerActionInputSuppressed = suppressed;
            if (suppressed)
            {
                _inputBuffer?.Clear();
                _chordArbiter.Reset();

                if (enteredSuppression)
                {
                    InvokeCancelEvents(
                        InputLayer.Level_1,
                        InputMapNames.PlayerAction);
                }
            }
        }

        public bool IsPlayerActionInputSuppressed => _isPlayerActionInputSuppressed;

        /// <summary>
        /// UI 닫기처럼 같은 물리 입력이 게임플레이 입력으로 새는 전환 구간에서 PlayerAction만 짧게 차단한다.
        /// UI/System 입력은 그대로 통과하며, 기존 전투 버퍼도 함께 비워 닫기 클릭 잔여 입력을 제거한다.
        /// </summary>
        public void SuppressPlayerActionInputBriefly(float seconds = 0.05f, int frameCount = 1)
        {
            _inputBuffer?.Clear();
            _chordArbiter.Reset();
            InvokeCancelEvents(
                InputLayer.Level_1,
                InputMapNames.PlayerAction);

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
