using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// 대화 카메라 사전 녹화(<see cref="DialogueCameraRecordingSO"/>) 재생 모드.
    /// CameraSnapshotSequenceMode와 동일한 포즈/좌표공간/충돌/이펙트 plumbing을 공유하되,
    /// 손으로 키잉한 샷을 절차 보간하는 대신 균일 샘플 궤적을 시간축으로 그대로 보간 재생한다.
    /// </summary>
    public class DialogueCameraReplayMode : ICameraMode
    {
        private DialogueCameraRecordingSO _recording;
        private CameraSnapshotActorReference _anchor;

        private float _elapsed;
        private bool _completed;
        private bool _restoreOnFinish;

        private CameraRigPose _fromPose;
        private bool _hasFromPose;
        private bool _isEntryBlending;
        private float _entryBlendElapsed;

        public CameraModeType ModeType => CameraModeType.DialogueCameraReplay;
        // Dialogue(50)보다 위, CameraSnapshotSequence(100)보다 아래 — 전투 킬캠 등이 우선권을 가짐
        public int Priority => 60;
        public bool AllowsPlayerLookInput => false;
        public bool AllowsZoomInput => false;
        public bool AllowsLockOnInput => false;
        public bool UseCollision => _recording != null && _recording.useCollision;
        public bool IsCompleted => _completed;
        public DialogueCameraRecordingSO ActiveRecording => _recording;

        public void OnEnter(CameraRuntimeContext context, CameraModeEnterParams enterParams)
        {
            _recording = enterParams.DialogueRecording;
            _anchor = enterParams.HasSnapshotActorAnchorOverride
                ? enterParams.SnapshotActorAnchor
                : _recording != null ? _recording.anchor : CameraSnapshotActorReference.ActivePlayer();

            _elapsed = 0f;
            _completed = false;
            _entryBlendElapsed = 0f;
            _isEntryBlending = _recording != null && _recording.entryBlendDuration > 0f;
            // 완료 시 이전 모드 복귀 여부는 enterParams가 결정한다.
            // 대화 중 재생은 false로 들어와 마지막 프레임을 유지(다음 노드가 카메라를 교체).
            _restoreOnFinish = enterParams != null && enterParams.RestorePreviousOnExit;

            context.IsInputLocked = _recording == null || _recording.lockCameraInput;
            if (_recording == null || _recording.releaseLockOnOnEnter)
                context.LockOn?.Release();

            // 진입 직전 실제 카메라 포즈를 from-pose로 캡처 → 첫 샘플로 부드럽게 블렌드
            _hasFromPose = false;
            if (context.MainCamera != null)
            {
                _fromPose = CameraRigPose.FromCamera(
                    context.MainCamera,
                    context.CameraPivot,
                    context.State.CurrentYaw,
                    context.State.CurrentPitch,
                    context.State.TargetDistance);
                _hasFromPose = true;
            }
        }

        public void OnExit(CameraRuntimeContext context)
        {
            context.IsInputLocked = false;
        }

        public void HandleInput(CameraRuntimeContext context, float deltaTime)
        {
        }

        public CameraRigPose EvaluatePose(CameraRuntimeContext context, float deltaTime, CameraEffectState effectState)
        {
            if (_recording == null || _recording.SampleCount == 0 || context.MainCamera == null)
                return CameraRigPose.FromCamera(context.MainCamera, context.CameraPivot,
                    context.State.CurrentYaw, context.State.CurrentPitch, context.State.TargetDistance);

            if (!_hasFromPose)
            {
                _fromPose = BuildPoseFromSample(context, _recording.samples[0], effectState);
                _hasFromPose = true;
            }

            float dt = (_recording.useUnscaledTime ? Time.unscaledDeltaTime : deltaTime)
                       * Mathf.Max(0.01f, _recording.playbackSpeed);

            if (_isEntryBlending)
                return EvaluateEntryBlend(context, effectState, dt);

            if (_completed)
                return BuildPoseFromSample(context, _recording.samples[_recording.SampleCount - 1], effectState);

            _elapsed += Mathf.Max(0f, dt);

            // 종료 판정: 마지막 샘플 도달 또는 샘플이 1개뿐
            if (_recording.SampleCount == 1 || _elapsed >= _recording.Duration)
            {
                _completed = true;
                context.ActiveEnterParams?.OnComplete?.Invoke();
                if (_restoreOnFinish)
                    context.PopCameraMode?.Invoke(CameraModeEnterParams.Empty);
                return BuildPoseFromSample(context, _recording.samples[_recording.SampleCount - 1], effectState);
            }

            // 균일 샘플 간격 가정 → 시간으로부터 인덱스/소수부 복원
            float frame = _elapsed * _recording.sampleRate;
            int i = Mathf.Clamp(Mathf.FloorToInt(frame), 0, _recording.SampleCount - 2);
            float f = Mathf.Clamp01(frame - i);

            // 두 샘플을 먼저 보간한 뒤 포즈를 한 번만 빌드한다.
            // → 앵커 해석/충돌 spherecast/이펙트 합성이 프레임당 1회로 줄어든다.
            //   useCollision=false면 affine 변환이 lerp를 통과해 두 번 빌드 후 lerp와 결과가 동일하고,
            //   true면 끝점 2회 보정 대신 실제 보간 위치 1회 보정이라 오히려 더 정확하다.
            DialogueCameraRecordingSO.Sample blended = InterpolateSample(_recording.samples[i], _recording.samples[i + 1], f);
            return BuildPoseFromSample(context, blended, effectState);
        }

        private static DialogueCameraRecordingSO.Sample InterpolateSample(
            DialogueCameraRecordingSO.Sample a, DialogueCameraRecordingSO.Sample b, float t)
        {
            return new DialogueCameraRecordingSO.Sample
            {
                localPosition = Vector3.Lerp(a.localPosition, b.localPosition, t),
                // euler 직접 lerp는 wrap/짐벌에서 깨지므로 quaternion으로 보간 후 환원
                localEuler = Quaternion.Slerp(Quaternion.Euler(a.localEuler), Quaternion.Euler(b.localEuler), t).eulerAngles,
                fieldOfView = Mathf.Lerp(a.fieldOfView, b.fieldOfView, t)
            };
        }

        private CameraRigPose EvaluateEntryBlend(CameraRuntimeContext context, CameraEffectState effectState, float deltaTime)
        {
            CameraRigPose toPose = BuildPoseFromSample(context, _recording.samples[0], effectState);
            float duration = Mathf.Max(0.01f, _recording.entryBlendDuration);
            _entryBlendElapsed += Mathf.Max(0f, deltaTime);

            float rawT = Mathf.Clamp01(_entryBlendElapsed / duration);
            float t = _recording.entryBlendCurve != null ? Mathf.Clamp01(_recording.entryBlendCurve.Evaluate(rawT)) : rawT;
            CameraRigPose pose = LerpLinearPose(_fromPose, toPose, t);

            if (rawT >= 1f)
            {
                _isEntryBlending = false;
                _elapsed = 0f;
            }

            return pose;
        }

        /// <summary>
        /// 한 샘플을 현재 앵커 기준 월드 포즈로 환원한다.
        /// (CameraSnapshotSequenceMode.BuildPoseFromShot와 동일한 공간/충돌/이펙트 규칙)
        /// </summary>
        private CameraRigPose BuildPoseFromSample(CameraRuntimeContext context, DialogueCameraRecordingSO.Sample sample, CameraEffectState effectState)
        {
            Transform anchor = CameraSnapshotActorReferenceResolver.Resolve(_anchor, context.Target);

            Quaternion localRotation = Quaternion.Euler(sample.localEuler);
            Vector3 position;
            Quaternion rotation;
            if (_recording.space == CameraSnapshotSpace.ActorRelative && anchor != null)
            {
                position = anchor.TransformPoint(sample.localPosition);
                rotation = anchor.rotation * localRotation;
            }
            else
            {
                position = sample.localPosition;
                rotation = localRotation;
            }

            Vector3 pivotPosition = anchor != null ? anchor.position : position;

            if (UseCollision && context.Collision != null)
            {
                Vector3 cameraOffset = position - pivotPosition;
                float desiredDistance = cameraOffset.magnitude;
                if (desiredDistance > 0.001f)
                {
                    Vector3 cameraDirection = cameraOffset / desiredDistance;
                    float resolvedDistance = context.Collision.Evaluate(pivotPosition, cameraDirection, desiredDistance);
                    position = pivotPosition + cameraDirection * resolvedDistance;
                }
            }

            position += effectState.positionDelta;
            rotation = Quaternion.Euler(effectState.pitchDelta, effectState.yawDelta, 0f) * rotation;

            Vector3 euler = rotation.eulerAngles;
            return new CameraRigPose
            {
                PivotPosition = pivotPosition,
                CameraPosition = position,
                CameraRotation = rotation,
                Yaw = euler.y,
                Pitch = euler.x,
                Distance = anchor != null ? Vector3.Distance(pivotPosition, position) + effectState.distanceDelta : 0f,
                FieldOfView = sample.fieldOfView + effectState.fovDelta
            };
        }

        private static CameraRigPose LerpLinearPose(CameraRigPose from, CameraRigPose to, float t)
        {
            return new CameraRigPose
            {
                PivotPosition = Vector3.Lerp(from.PivotPosition, to.PivotPosition, t),
                CameraPosition = Vector3.Lerp(from.CameraPosition, to.CameraPosition, t),
                CameraRotation = Quaternion.Slerp(from.CameraRotation, to.CameraRotation, t),
                Yaw = Mathf.LerpAngle(from.Yaw, to.Yaw, t),
                Pitch = Mathf.LerpAngle(from.Pitch, to.Pitch, t),
                Distance = Mathf.Lerp(from.Distance, to.Distance, t),
                FieldOfView = Mathf.Lerp(from.FieldOfView, to.FieldOfView, t)
            };
        }
    }
}
