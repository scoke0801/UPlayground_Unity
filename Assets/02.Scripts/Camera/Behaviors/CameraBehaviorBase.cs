using System.Collections.Generic;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// Modifier 파이프라인을 호스팅하는 카메라 Behavior 베이스.
    /// EvaluatePose 시 등록된 Modifier들을 Priority 오름차순으로 실행해 포즈를 누적 산출한다.
    ///
    /// 자체 포즈 계산이 필요한 Behavior(대화/스냅샷 등)는 EvaluatePose를 오버라이드해
    /// 파이프라인을 우회할 수 있다.
    /// </summary>
    public abstract class CameraBehaviorBase : ICameraBehavior
    {
        private readonly List<ICameraModifier> _modifiers = new List<ICameraModifier>();
        private bool _sorted = true;

        public abstract CameraModeType ModeType { get; }
        public virtual int Priority => 0;
        public virtual bool AllowsPlayerLookInput => true;
        public virtual bool AllowsZoomInput => true;
        public virtual bool AllowsLockOnInput => true;
        public virtual bool UseCollision => true;
        public virtual bool RequiresPrimaryTarget => true;

        /// <summary>
        /// Modifier 등록. 생성자에서 호출해 인스턴스를 주입한다.
        /// 인스턴스를 보유하므로 각 Modifier의 프레임 간 보간 상태가 유지된다.
        /// </summary>
        protected void AddModifier(ICameraModifier modifier)
        {
            if (modifier == null) return;
            _modifiers.Add(modifier);
            _sorted = false;
        }

        private void EnsureSorted()
        {
            if (_sorted) return;
            _modifiers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            _sorted = true;
        }

        public virtual void OnEnter(CameraContext context, CameraModeEnterParams enterParams)
        {
            for (int i = 0; i < _modifiers.Count; i++)
                if (_modifiers[i] is ICameraModifierLifecycle lifecycle)
                    lifecycle.OnEnter(context, enterParams);
        }

        public virtual void OnExit(CameraContext context)
        {
            for (int i = 0; i < _modifiers.Count; i++)
                if (_modifiers[i] is ICameraModifierLifecycle lifecycle)
                    lifecycle.OnExit(context);
        }

        public virtual void HandleInput(CameraContext context, float deltaTime) { }

        public virtual CameraPose EvaluatePose(CameraContext context, float deltaTime, CameraEffectState effectState)
        {
            EnsureSorted();

            CameraFrame frame = new CameraFrame
            {
                Context = context,
                State = context?.State,
                Effects = effectState,
                DeltaTime = deltaTime,
                Pose = BuildInitialPose(context),
            };

            for (int i = 0; i < _modifiers.Count; i++)
                _modifiers[i].Apply(ref frame);

            return frame.Pose;
        }

        /// <summary>
        /// 파이프라인 진입 전 초기 포즈. 누적 상태(yaw/pitch/distance)와 현재 FOV로 시드한다.
        /// 위치/회전은 Follow/Collision Modifier가 채운다.
        /// </summary>
        protected virtual CameraPose BuildInitialPose(CameraContext context)
        {
            CameraState state = context?.State;
            return new CameraPose
            {
                Yaw = state?.CurrentYaw ?? 0f,
                Pitch = state?.CurrentPitch ?? 0f,
                Distance = state?.TargetDistance ?? 0f,
                FieldOfView = context != null && context.MainCamera != null ? context.MainCamera.fieldOfView : 60f,
            };
        }
    }
}
