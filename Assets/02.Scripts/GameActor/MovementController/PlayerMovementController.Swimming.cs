using UnityEngine;
using UPlayGround.Components;
using UPlayGround.State;

namespace UPlayGround.MovementController
{
    /// <summary>수영 설정과 입수 감지. 발이 수면 아래로 충분히 잠기면 수영 상태로 전환한다.</summary>
    public partial class PlayerMovementController
    {
        [Header("Swimming - 진입/이탈")]
        [Min(0f), Tooltip("발 아래 수심이 이 값 이상이면 수영을 시작한다(m). 걷기 모션이 어색해지는 가슴 깊이 기준.")]
        public float SwimEnterDepth = 1.2f;
        [Min(0f), Tooltip("수영 중 수심이 이 값보다 얕아지고 디딜 곳이 있으면 수영을 끝낸다(m). 진입보다 낮아야 경계에서 깜박이지 않는다.")]
        public float SwimExitDepth = 0.85f;
        [Min(0f), Tooltip("수영 중 발이 수면 아래에 잠기는 깊이(m). 어깨가 수면에 오는 높이.")]
        public float SwimFloatDepth = 1.35f;
        [Min(0f), Tooltip("수영 시작 직후 이탈 판정을 막는 시간(초).")]
        public float SwimMinStayTime = 0.4f;
        [Min(0f), Tooltip("수영을 끝낸 직후 재진입을 막는 시간(초).")]
        public float SwimReenterCooldown = 0.5f;

        [Header("Swimming - 이동")]
        [Min(0f)] public float SwimSpeed = 3.2f;
        [Min(0f)] public float SwimFastSpeed = 5f;
        [Min(0f), Tooltip("이동 입력을 이만큼 유지하면 빠른 수영으로 전환한다(초).")]
        public float SwimFastStartDelay = 1.2f;
        [Min(0f), Tooltip("수평 속도 수렴 속도. 낮을수록 물의 저항이 크게 느껴진다.")]
        public float SwimMoveSharpness = 3.5f;
        [Min(0f)] public float SwimRotationSharpness = 5f;

        [Header("Swimming - 부력")]
        [Min(0f), Tooltip("목표 높이까지 거리에 비례한 수직 속도 계수.")]
        public float SwimVerticalSharpness = 6f;
        [Min(0f)] public float SwimMaxVerticalSpeed = 6f;
        [Min(0f), Tooltip("입수 충격을 흡수하는 수직 속도 보간 속도.")]
        public float SwimVerticalDamping = 10f;
        [Min(0f), Tooltip("제자리 수영 시 위아래 출렁임 폭(m).")]
        public float SwimBobAmplitude = 0.025f;
        [Min(0f)] public float SwimBobFrequency = 1.6f;

        [Header("Swimming - 잠수")]
        [Min(0f), Tooltip("발 위치에서 머리 끝까지의 높이(m). 수심이 이보다 깊으면 머리가 잠긴 것으로 본다.")]
        public float SwimHeadHeight = 1.7f;
        [Min(0f), Tooltip("잠수 중 카메라 방향으로 헤엄치는 속도(m/s).")]
        public float SwimDiveMoveSpeed = 3f;
        [Min(0f), Tooltip("상승(점프)·하강(웅크리기/대시) 버튼을 누르고 있을 때의 수직 속도(m/s). 카메라 각도와 무관한 고정 속도다.")]
        public float SwimButtonVerticalSpeed = 2.5f;
        [Range(-30f, 60f), Tooltip("수중에서 '수평'으로 취급할 카메라 피치(도, 아래가 양수). 궤도 카메라는 평소에도 캐릭터를 내려다보므로 이 각도가 수평 이동이다. 이보다 높이면 하강, 낮추면 상승한다.")]
        public float SwimNeutralCameraPitch = 18f;
        [Range(0f, 15f), Tooltip("기준 피치 주변에서 수직 이동을 만들지 않는 여유 각도(도). 수평으로 헤엄칠 때 깊이가 흘러가지 않게 한다.")]
        public float SwimPitchDeadZone = 6f;
        [Range(0.5f, 3f), Tooltip("기준 피치에서 벗어난 각도를 이동 각도로 증폭하는 배율. 카메라 피치 범위가 좁아도 충분히 가파르게 오르내릴 수 있게 한다.")]
        public float SwimPitchGain = 1.6f;
        [Range(5f, 80f), Tooltip("잠수 시작에 필요한 실제 카메라 하향각(도). 수평 이동 여유 각도 안에서는 잠수하지 않는다. 옆 이동량과 전진 입력 크기는 시작 각도에 영향을 주지 않는다.")]
        public float SwimDiveStartPitch = 18f;
        [Min(0f), Tooltip("잠수 조건을 이 시간 동안 유지해야 시작된다. 카메라를 잠깐 내린 것만으로 가라앉지 않게 한다(초).")]
        public float SwimDiveStartHoldTime = 0.25f;
        [Min(0f), Tooltip("하강각이 이 값 이상이면 머리를 아래로 한 자세를 쓴다(도).")]
        public float SwimSteepDescentAngle = 35f;
        [Min(0f), Tooltip("수면 아래로 내려갈 수 있는 최대 깊이(m).")]
        public float SwimMaxDiveDepth = 10f;
        [Min(0f), Tooltip("수영 진입 직후 이 시간 동안은 머리가 잠겨도 소모·익사를 적용하지 않는다(낙하 입수 충격 보호, 초).")]
        public float SwimSubmergeGraceTime = 1f;

        [Header("Swimming - 물가 올라가기")]
        [Min(0f), Tooltip("점프로 올라설 수 있는 수면 위 턱의 최대 높이(m).")]
        public float SwimLedgeMaxHeight = 0.9f;
        [Min(0f), Tooltip("턱을 찾는 전방 거리(m).")]
        public float SwimLedgeProbeDistance = 0.9f;
        [Min(0f), Tooltip("턱 위로 올라갈 때 확보할 여유 높이(m).")]
        public float SwimLedgeClearance = 0.4f;
        [Min(0f)] public float SwimLedgeForwardSpeed = 3.5f;

        private const float SwimProbeInterval = 0.05f;
        private float _nextWaterProbeTime;
        private float _swimEntryBlockedUntil;

        public bool IsSwimming => CurrentState is PlayerSwimState;

        /// <summary>낙구를 넘은 동안에는 상류 수면의 부력으로 다시 붙잡히지 않는다.</summary>
        public bool IsInWaterfallDrop => Motor != null
            && WaterfallCurrent.TryResolve(Motor.TransientPosition, out _, out _, out bool isFalling) && isFalling;

        /// <summary>수영 입력에 더할 하류 물살. 일반 수면에서는 영벡터다.</summary>
        public Vector3 SwimCurrentVelocity => Motor != null
            && WaterfallCurrent.TryResolve(Motor.TransientPosition, out _, out Vector3 velocity, out _)
                ? velocity : Vector3.zero;

        /// <summary>공중 상태의 중력을 유지하면서 폭포 물살을 따라 낙하하게 한다.</summary>
        public void ApplyWaterfallFallVelocity(ref Vector3 velocity)
        {
            if (Motor != null && WaterfallCurrent.TryResolve(Motor.TransientPosition, out var current, out _, out bool isFalling)
                && isFalling) current.ConstrainFallVelocity(ref velocity);
        }

        /// <summary>발 위치 기준 수면 높이와 수심을 구한다. 등록된 수면 위가 아니면 false.</summary>
        public bool TryGetWaterDepth(out float depth, out float surfaceY)
        {
            depth = 0f;
            surfaceY = 0f;
            if (Motor == null) return false;
            Vector3 feet = Motor.TransientPosition;
            if (!FootstepWaterSurface.TryGetSurfaceHeight(feet, out surfaceY)) return false;
            depth = surfaceY - feet.y;
            return true;
        }

        private float SwimCameraPitch
        {
            get
            {
                Vector3 cameraForward = _cameraRotation * Vector3.forward;
                return Mathf.Asin(Mathf.Clamp(-cameraForward.y, -1f, 1f)) * Mathf.Rad2Deg;
            }
        }

        /// <summary>수중 하강이 가능한 시선으로 전진하는지. 대각선이나 약한 입력에도 같은 카메라 각도를 쓴다.</summary>
        public bool HasSwimDiveIntent => PlayerSwimRules.IsDiveIntent(
            _inputState.MoveInput, SwimCameraPitch,
            Mathf.Max(SwimDiveStartPitch, SwimNeutralCameraPitch + SwimPitchDeadZone));

        /// <summary>
        /// 수중 3D 이동 방향. 카메라 요(좌우)는 그대로 쓰고, 피치는 기준 피치에서 벗어난 만큼만 증폭해 기울인다.
        /// 아래를 보고 앞으로 가면 내려가고 위를 보면 올라오며, 뒤로 당기면 반대가 된다.
        /// </summary>
        public Vector3 SwimMoveVector
        {
            get
            {
                Vector2 stick = _inputState.MoveInput;
                float pitch = PlayerSwimRules.ResolveSwimPitch(SwimCameraPitch, SwimNeutralCameraPitch, SwimPitchDeadZone, SwimPitchGain);
                Quaternion yaw = Quaternion.Euler(0f, _cameraRotation.eulerAngles.y, 0f);
                Vector3 local = Quaternion.Euler(pitch, 0f, 0f) * new Vector3(stick.x, 0f, stick.y);
                return Vector3.ClampMagnitude(yaw * local, 1f);
            }
        }

        /// <summary>상승·하강 버튼 축. 상승(점프)이 +1, 하강(웅크리기·대시)이 -1이고 동시에 누르면 0이다.</summary>
        public float SwimVerticalAxis =>
            (_inputState.SwimAscendHeld ? 1f : 0f) - (_inputState.SwimDescendHeld ? 1f : 0f);

        /// <summary>지정 시간 동안 수영 자동 진입을 막는다. 물가 올라가기처럼 일부러 물을 벗어난 직후 쓴다.</summary>
        public void BlockSwimEntry(float seconds) =>
            _swimEntryBlockedUntil = Mathf.Max(_swimEntryBlockedUntil, Time.time + seconds);

        /// <summary>수영 가능한 상태에서 충분히 잠겼으면 수영으로 전환한다.</summary>
        private void UpdateWaterImmersion()
        {
            if (Motor == null || !Motor.enabled || CurrentState == null) return;
            if (Time.time < _nextWaterProbeTime) return;
            _nextWaterProbeTime = Time.time + SwimProbeInterval;

            if (Time.time < _swimEntryBlockedUntil || !PlayerSwimRules.CanEnterFrom(CurrentState.StateId)) return;
            if (IsInWaterfallDrop) return;
            if (!TryGetWaterDepth(out float depth, out _) || !PlayerSwimRules.ShouldEnter(depth, SwimEnterDepth)) return;
            TryTransitionToState(ActorStateId.Swim);
        }
    }
}
