using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Animation;

namespace UPlayGround.Data
{
    /// <summary>
    /// 대화 카메라 사전 녹화 트랙. 균일 <see cref="sampleRate"/>로 캡처한 카메라 포즈 샘플을 담는다.
    /// 재생은 DialogueCameraReplayMode가 담당한다.
    ///
    /// CameraSnapshotProfile과의 차이: 손으로 키잉한 소수 샷이 아니라 카메라를 직접 몰아 캡처한
    /// 연속 궤적이다. 둘은 같은 ICameraBehavior/CameraPose 재생 경로를 공유하는 형제 시스템이며,
    /// 좌표 공간/앵커 자산(CameraSnapshotSpace, CameraSnapshotActorReference)도 재사용한다.
    /// </summary>
    [CreateAssetMenu(fileName = "DCR_", menuName = "UPlayGround/카메라/Dialogue Recording")]
    public class DialogueCameraRecordingSO : ScriptableObject
    {
        /// <summary>
        /// 단일 캡처 프레임. space 기준으로 해석된다(ActorRelative면 앵커 로컬, World면 월드).
        /// 회전은 Quaternion 직렬화/짐벌 혼동을 피하기 위해 euler로 저장한다(기존 스냅샷 자산과 일관).
        /// 병렬 배열 대신 구조체 배열을 쓰는 이유: 트림·편집 시 위치/회전/FOV가 desync되지 않게 하기 위함.
        /// </summary>
        [Serializable]
        public struct Sample
        {
            [Tooltip("비균일 키 리덕션용 원본 시간. 0만 저장된 구버전 트랙은 sampleRate 균일 시간축을 사용한다.")]
            public float sampleTime;
            public Vector3 localPosition;
            public Vector3 localEuler;
            public float fieldOfView;
        }

        public string recordingName;

        [Header("제작 메타데이터")]
        public MotionSetAsset sourceMotion;
        public string sourceScene;
        public string sourceTakeName;
        public string capturedAt;
        public float sourceStartTime;
        public float sourceEndTime;
        public int sourceRawSampleCount;
        public int sourceTrimIn;
        public int sourceTrimOut;
        [TextArea(2, 4)] public string authoringNotes;

        [Header("좌표 기준")]
        [Tooltip("ActorRelative 권장 — 앵커(화자/플레이어) 기준 로컬이라 다른 NPC·장소에서 재사용 가능. World는 녹화한 장소에 용접됨.")]
        public CameraSnapshotSpace space = CameraSnapshotSpace.ActorRelative;
        public CameraSnapshotActorReference anchor = CameraSnapshotActorReference.ActivePlayer();

        [Header("샘플")]
        [Tooltip("초당 샘플 수(Hz). 재생 시간축은 (샘플수-1)/sampleRate로 복원된다.")]
        [Min(1f)] public float sampleRate = 30f;

        [Tooltip("재생에 사용되는 트랙. smoothingStrength>0이면 rawSamples를 스무딩한 결과다.")]
        public List<Sample> samples = new List<Sample>();

        [Header("스무딩 (비파괴)")]
        [Tooltip("녹화 원본. 손떨림 제거는 항상 이 raw에서 다시 계산하므로 재스무딩이 누적되지 않는다. 직접 편집 금지.")]
        public List<Sample> rawSamples = new List<Sample>();

        [Tooltip("손떨림 스무딩 강도(0=원본 그대로). 값을 바꾸면 rawSamples에서 samples를 재생성한다.")]
        [Range(0f, 1f)] public float smoothingStrength = 0f;
        [Tooltip("위치/회전/FOV 스무딩 강도를 개별 적용한다. 꺼져 있으면 smoothingStrength 하나를 사용한다.")]
        public bool usePerChannelSmoothing = false;
        [Range(0f, 1f)] public float positionSmoothingStrength = 0.35f;
        [Range(0f, 1f)] public float rotationSmoothingStrength = 0.2f;
        [Range(0f, 1f)] public float fovSmoothingStrength = 0.35f;

        [Header("키 리덕션")]
        public bool useKeyReduction = false;
        [Min(0.0001f)] public float positionReductionTolerance = 0.01f;
        [Min(0.01f)] public float rotationReductionTolerance = 0.5f;
        [Min(0.01f)] public float fovReductionTolerance = 0.1f;

        [Header("재생")]
        public bool useUnscaledTime = true;
        [Min(0.01f)] public float playbackSpeed = 1f;
        public bool useCollision = false;
        public bool lockCameraInput = true;
        public bool releaseLockOnOnEnter = true;
        public bool restorePreviousModeOnFinish = true;

        [Header("진입 블렌드")]
        [Tooltip("진입 직전 카메라 포즈 → 첫 샘플로의 블렌드 시간(초). 0이면 즉시 컷.")]
        [Min(0f)] public float entryBlendDuration = 0.25f;
        public AnimationCurve entryBlendCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public int SampleCount => samples?.Count ?? 0;

        /// <summary>녹화 전체 길이(초). 샘플이 2개 미만이면 0.</summary>
        public bool HasSampleTime => SampleCount > 1 && samples[SampleCount - 1].sampleTime > 0f;
        public float Duration => SampleCount > 1
            ? HasSampleTime ? samples[SampleCount - 1].sampleTime : (SampleCount - 1) / sampleRate
            : 0f;

        /// <summary>
        /// rawSamples에서 현재 smoothingStrength로 samples를 재생성한다(비파괴, 항상 raw 기준).
        /// 구버전 에셋(rawSamples 비어 있음) 호환: 기존 samples를 raw로 승격한다.
        /// </summary>
        public void RebuildSmoothedSamples()
        {
            if ((rawSamples == null || rawSamples.Count == 0) && samples != null && samples.Count > 0)
                rawSamples = new List<Sample>(samples); // legacy 승격

            List<Sample> smoothed = usePerChannelSmoothing
                ? DialogueCameraTrackSmoother.Smooth(
                    rawSamples,
                    positionSmoothingStrength,
                    rotationSmoothingStrength,
                    fovSmoothingStrength)
                : DialogueCameraTrackSmoother.Smooth(rawSamples, smoothingStrength);
            samples = useKeyReduction
                ? DialogueCameraTrackReducer.Reduce(
                    smoothed,
                    sampleRate,
                    positionReductionTolerance,
                    rotationReductionTolerance,
                    fovReductionTolerance)
                : smoothed;
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(recordingName))
                recordingName = name;

            sampleRate = Mathf.Max(1f, sampleRate);
            playbackSpeed = Mathf.Max(0.01f, playbackSpeed);
            entryBlendDuration = Mathf.Max(0f, entryBlendDuration);
            positionSmoothingStrength = Mathf.Clamp01(positionSmoothingStrength);
            rotationSmoothingStrength = Mathf.Clamp01(rotationSmoothingStrength);
            fovSmoothingStrength = Mathf.Clamp01(fovSmoothingStrength);
            positionReductionTolerance = Mathf.Max(0.0001f, positionReductionTolerance);
            rotationReductionTolerance = Mathf.Max(0.01f, rotationReductionTolerance);
            fovReductionTolerance = Mathf.Max(0.01f, fovReductionTolerance);
        }
    }
}
