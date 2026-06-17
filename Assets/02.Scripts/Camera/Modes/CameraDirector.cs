using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// 카메라 모드 등록과 현재 모드 전환을 담당한다.
    /// Dialogue/Free/Cinematic은 후속 단계에서 모드 구현을 추가한다.
    /// </summary>
    public class CameraDirector
    {
        private readonly Dictionary<CameraModeType, ICameraBehavior> _modes = new Dictionary<CameraModeType, ICameraBehavior>();
        private readonly Stack<ICameraBehavior> _modeStack = new Stack<ICameraBehavior>();
        private readonly CameraContext _context;

        public ICameraBehavior CurrentMode { get; private set; }
        public CameraModeType CurrentModeType => CurrentMode?.ModeType ?? CameraModeType.InGame;

        public CameraDirector(CameraContext context)
        {
            _context = context;
        }

        public void Register(ICameraBehavior mode)
        {
            if (mode == null) return;
            _modes[mode.ModeType] = mode;
        }

        public bool IsRegistered(CameraModeType modeType) => _modes.ContainsKey(modeType);

        public bool SetMode(CameraModeType modeType, CameraModeEnterParams enterParams = null)
        {
            if (!_modes.TryGetValue(modeType, out ICameraBehavior nextMode))
            {
                Debug.LogWarning($"[CameraDirector] 등록되지 않은 카메라 모드입니다: {modeType}");
                return false;
            }

            if (CurrentMode == nextMode)
            {
                _context.ActiveEnterParams = enterParams ?? CameraModeEnterParams.Empty;
                CurrentMode.OnEnter(_context, _context.ActiveEnterParams);
                return true;
            }

            CurrentMode?.OnExit(_context);
            CurrentMode = nextMode;
            _context.ActiveEnterParams = enterParams ?? CameraModeEnterParams.Empty;
            CurrentMode.OnEnter(_context, _context.ActiveEnterParams);
            return true;
        }

        public bool PushMode(CameraModeType modeType, CameraModeEnterParams enterParams = null)
        {
            if (!_modes.TryGetValue(modeType, out ICameraBehavior nextMode))
            {
                Debug.LogWarning($"[CameraDirector] 등록되지 않은 카메라 모드입니다: {modeType}");
                return false;
            }

            if (CurrentMode == nextMode)
            {
                _context.ActiveEnterParams = enterParams ?? CameraModeEnterParams.Empty;
                CurrentMode.OnEnter(_context, _context.ActiveEnterParams);
                return true;
            }

            if (CurrentMode != null)
                _modeStack.Push(CurrentMode);

            CurrentMode?.OnExit(_context);
            CurrentMode = nextMode;
            _context.ActiveEnterParams = enterParams ?? CameraModeEnterParams.Empty;
            CurrentMode.OnEnter(_context, _context.ActiveEnterParams);
            return true;
        }

        public bool PopMode(CameraModeEnterParams enterParams = null)
        {
            if (_modeStack.Count == 0)
                return SetMode(CameraModeType.InGame, enterParams);

            ICameraBehavior previousMode = _modeStack.Pop();
            CurrentMode?.OnExit(_context);
            CurrentMode = previousMode;
            _context.ActiveEnterParams = enterParams ?? CameraModeEnterParams.Empty;
            CurrentMode.OnEnter(_context, _context.ActiveEnterParams);
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

        public CameraPose EvaluatePose(float deltaTime, CameraEffectState effectState)
        {
            return CurrentMode != null
                ? CurrentMode.EvaluatePose(_context, deltaTime, effectState)
                : default;
        }
    }
}
