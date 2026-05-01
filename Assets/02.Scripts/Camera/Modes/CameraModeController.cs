using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// 카메라 모드 등록과 현재 모드 전환을 담당한다.
    /// Dialogue/Free/Cinematic은 후속 단계에서 모드 구현을 추가한다.
    /// </summary>
    public class CameraModeController
    {
        private readonly Dictionary<CameraModeType, ICameraMode> _modes = new Dictionary<CameraModeType, ICameraMode>();
        private readonly Stack<ICameraMode> _modeStack = new Stack<ICameraMode>();
        private readonly CameraRuntimeContext _context;

        public ICameraMode CurrentMode { get; private set; }
        public CameraModeType CurrentModeType => CurrentMode?.ModeType ?? CameraModeType.InGame;

        public CameraModeController(CameraRuntimeContext context)
        {
            _context = context;
        }

        public void Register(ICameraMode mode)
        {
            if (mode == null) return;
            _modes[mode.ModeType] = mode;
        }

        public bool SetMode(CameraModeType modeType, CameraModeEnterParams enterParams = null)
        {
            if (!_modes.TryGetValue(modeType, out ICameraMode nextMode))
            {
                Debug.LogWarning($"[CameraModeController] 등록되지 않은 카메라 모드입니다: {modeType}");
                return false;
            }

            if (CurrentMode == nextMode)
            {
                CurrentMode.OnEnter(_context, enterParams ?? CameraModeEnterParams.Empty);
                return true;
            }

            CurrentMode?.OnExit(_context);
            CurrentMode = nextMode;
            CurrentMode.OnEnter(_context, enterParams ?? CameraModeEnterParams.Empty);
            return true;
        }

        public bool PushMode(CameraModeType modeType, CameraModeEnterParams enterParams = null)
        {
            if (!_modes.TryGetValue(modeType, out ICameraMode nextMode))
            {
                Debug.LogWarning($"[CameraModeController] 등록되지 않은 카메라 모드입니다: {modeType}");
                return false;
            }

            if (CurrentMode == nextMode)
            {
                CurrentMode.OnEnter(_context, enterParams ?? CameraModeEnterParams.Empty);
                return true;
            }

            if (CurrentMode != null)
                _modeStack.Push(CurrentMode);

            CurrentMode?.OnExit(_context);
            CurrentMode = nextMode;
            CurrentMode.OnEnter(_context, enterParams ?? CameraModeEnterParams.Empty);
            return true;
        }

        public bool PopMode(CameraModeEnterParams enterParams = null)
        {
            if (_modeStack.Count == 0)
                return SetMode(CameraModeType.InGame, enterParams);

            ICameraMode previousMode = _modeStack.Pop();
            CurrentMode?.OnExit(_context);
            CurrentMode = previousMode;
            CurrentMode.OnEnter(_context, enterParams ?? CameraModeEnterParams.Empty);
            return true;
        }

        public bool ForceMode(CameraModeType modeType, CameraModeEnterParams enterParams = null)
        {
            _modeStack.Clear();
            return SetMode(modeType, enterParams);
        }

        public void HandleInput(float deltaTime)
        {
            CurrentMode?.HandleInput(_context, deltaTime);
        }

        public CameraRigPose EvaluatePose(float deltaTime, CameraEffectState effectState)
        {
            return CurrentMode != null
                ? CurrentMode.EvaluatePose(_context, deltaTime, effectState)
                : default;
        }
    }
}
