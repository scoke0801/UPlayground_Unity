using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// 대화 카메라 사전 녹화용 런타임 샘플러.
    ///
    /// 두 가지 정확성 포인트:
    /// 1) 카메라 스택이 포즈를 적용한 "뒤"에 캡처해야 한다. 카메라는 GameManager.LateUpdate(실행순서 0)
    ///    안에서 CameraManager.OnLateUpdate가 적용하므로, 이 컴포넌트는 [DefaultExecutionOrder] 높은 값으로
    ///    그 이후 LateUpdate에서 캡처한다. (값을 낮추면 한 프레임 이전 포즈를 잡게 됨)
    /// 2) 균일 sampleRate 가정과 맞추기 위해 매 프레임이 아니라 어큐뮬레이터로 고정 간격마다 캡처한다.
    ///
    /// 에디터 녹화 도구(DialogueCameraRecorderWindow)가 PlayMode에서 생성/제어한다.
    /// </summary>
    [DefaultExecutionOrder(20000)]
    public class DialogueCameraRecorder : MonoBehaviour
    {
        public float SampleRate = 30f;
        public CameraSnapshotSpace Space = CameraSnapshotSpace.ActorRelative;
        public Transform Anchor;

        public bool IsRecording { get; private set; }
        public int SampleCount => _samples.Count;
        public float RecordedDuration => SampleRate > 0f && _samples.Count > 1 ? (_samples.Count - 1) / SampleRate : 0f;

        private readonly List<DialogueCameraRecordingSO.Sample> _samples = new();
        private float _accumulator;
        private Camera _camera;

        public void BeginRecording(Camera camera)
        {
            _camera = camera;
            _samples.Clear();
            _accumulator = 0f;
            IsRecording = true;

            if (_camera != null)
                CaptureSample(); // 첫 샘플(t=0) 즉시 캡처
        }

        public IReadOnlyList<DialogueCameraRecordingSO.Sample> EndRecording()
        {
            IsRecording = false;
            return _samples;
        }

        private void LateUpdate()
        {
            if (!IsRecording || _camera == null)
                return;

            float interval = 1f / Mathf.Max(1f, SampleRate);
            _accumulator += Time.unscaledDeltaTime;

            // 프레임이 길면 한 프레임에 여러 간격이 누적될 수 있음 — 타이밍 보존을 위해 모두 캡처
            while (_accumulator >= interval)
            {
                _accumulator -= interval;
                CaptureSample();
            }
        }

        private void CaptureSample()
        {
            Vector3 worldPos = _camera.transform.position;
            Quaternion worldRot = _camera.transform.rotation;

            Vector3 localPos;
            Vector3 localEuler;
            if (Space == CameraSnapshotSpace.ActorRelative && Anchor != null)
            {
                // CameraSnapshotShot.Capture와 동일한 로컬화 수식
                localPos = Anchor.InverseTransformPoint(worldPos);
                localEuler = (Quaternion.Inverse(Anchor.rotation) * worldRot).eulerAngles;
            }
            else
            {
                localPos = worldPos;
                localEuler = worldRot.eulerAngles;
            }

            _samples.Add(new DialogueCameraRecordingSO.Sample
            {
                sampleTime = _samples.Count / Mathf.Max(1f, SampleRate),
                localPosition = localPos,
                localEuler = localEuler,
                fieldOfView = _camera.fieldOfView
            });
        }
    }
}
