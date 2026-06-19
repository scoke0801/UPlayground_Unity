#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Animation.Editor;
using UPlayGround.CameraSystem;
using UPlayGround.Manager;

namespace UPlayGround.Data.Editor
{
    /// <summary>
    /// 대화 카메라 사전 녹화 도구.
    /// PlayMode에서 프리카메라로 카메라를 직접 몰며 연기 → 녹화 → DialogueCameraRecordingSO로 베이크한다.
    /// 재생은 런타임 DialogueCameraReplayMode가 담당한다(이 도구는 저작 전용).
    /// </summary>
    public class DialogueCameraRecorderWindow : EditorWindow
    {
        private DialogueCameraRecordingSO _recording;
        private Camera _captureCamera;
        private Transform _anchor;
        private CameraSnapshotSpace _space = CameraSnapshotSpace.ActorRelative;
        private float _sampleRate = 30f;

        private float _freeCameraMoveSpeed = 6f;
        private float _freeCameraLookSensitivity = 0.12f;

        private float _smoothingStrength = 0.35f;

        private DialogueCameraRecorder _recorder;
        private List<DialogueCameraRecordingSO.Sample> _pendingSamples;
        private float _pendingSampleRate;
        private CameraSnapshotSpace _pendingSpace;
        private MotionSetAsset _captureMotion;
        private float _captureStartTime;
        private float _captureEndTime = -1f;
        private float _captureCountdown = 1f;
        private bool _autoStopWithMotion = true;
        private bool _isCaptureCountdownActive;
        private double _captureCountdownEndTime;
        private bool _isSynchronizedTake;
        private float _lastMotionDuration;
        private string _captureStatus;

        [MenuItem("UPlayGround/월드/카메라/대화 카메라 녹화", priority = UPlayGround.Tool.Editor.UPlaygroundMenuPriority.WorldCamera)]
        public static void Open()
        {
            GetWindow<DialogueCameraRecorderWindow>("대화 카메라 녹화");
        }

        public static void OpenForMotion(MotionSetAsset motion, Transform suggestedAnchor = null)
        {
            var window = GetWindow<DialogueCameraRecorderWindow>("대화 카메라 녹화");
            window._captureMotion = motion;
            if (suggestedAnchor != null)
                window._anchor = suggestedAnchor;
            window._captureStartTime = 0f;
            window._captureEndTime = -1f;
            window.Show();
            window.Focus();
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            _isCaptureCountdownActive = false;

            // 창이 PlayMode 중 닫히면 런타임 생성한 레코더 GameObject가 씬에 남는다 → 정리.
            if (_recorder != null)
            {
                if (_recorder.IsRecording)
                    _recorder.EndRecording();
                DestroyImmediate(_recorder.gameObject);
                _recorder = null;
            }

            if (_isSynchronizedTake)
                MotionSetEditorWindow.StopCapturePlayback(_captureMotion);
            _isSynchronizedTake = false;
        }

        private void OnEditorUpdate()
        {
            if (_isCaptureCountdownActive
                && EditorApplication.timeSinceStartup >= _captureCountdownEndTime)
            {
                _isCaptureCountdownActive = false;
                BeginSynchronizedTake();
            }

            if (_isSynchronizedTake
                && _recorder != null
                && _recorder.IsRecording
                && _autoStopWithMotion)
            {
                bool hasState = MotionSetEditorWindow.TryGetCapturePlaybackState(out var state);
                if (!hasState || state.Asset != _captureMotion || !state.IsPlaying)
                    StopSynchronizedTake(false);
            }

            Repaint();
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _recording = (DialogueCameraRecordingSO)EditorGUILayout.ObjectField("녹화 에셋", _recording, typeof(DialogueCameraRecordingSO), false);
                _captureCamera = (Camera)EditorGUILayout.ObjectField("캡처 카메라", _captureCamera, typeof(Camera), true);
                _anchor = (Transform)EditorGUILayout.ObjectField("앵커 Transform", _anchor, typeof(Transform), true);
                _space = (CameraSnapshotSpace)EditorGUILayout.EnumPopup("좌표계", _space);
                _sampleRate = Mathf.Clamp(EditorGUILayout.FloatField("샘플레이트(Hz)", _sampleRate), 1f, 120f);

                if (_space == CameraSnapshotSpace.ActorRelative && _anchor == null)
                    EditorGUILayout.HelpBox("ActorRelative인데 앵커가 비어 있습니다. 앵커 없이 녹화하면 World로 캡처됩니다.", MessageType.Warning);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("새 녹화 에셋 생성", GUILayout.Height(24f)))
                        CreateRecordingAsset();
                }
            }

            EditorGUILayout.Space(6f);
            DrawFreeCameraControls();

            EditorGUILayout.Space(6f);
            DrawSynchronizedCaptureControls();

            EditorGUILayout.Space(6f);
            DrawRecordControls();

            EditorGUILayout.Space(6f);
            DrawSmoothingControls();

            EditorGUILayout.Space(6f);
            DrawPreviewControls();
        }

        private void DrawSynchronizedCaptureControls()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("MotionSet 동기 촬영", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "모션 에디터의 재생 구간과 카메라 녹화를 같은 틱에서 시작합니다. 궁극기 모션/카메라 테이크 반복 촬영용 기반 기능입니다.",
                    MessageType.None);

                _captureMotion = (MotionSetAsset)EditorGUILayout.ObjectField(
                    "MotionSet",
                    _captureMotion,
                    typeof(MotionSetAsset),
                    false);

                float totalDuration = ResolveCaptureMotionDuration();
                using (new EditorGUILayout.HorizontalScope())
                {
                    _captureStartTime = Mathf.Max(0f, EditorGUILayout.FloatField("시작", _captureStartTime));
                    _captureEndTime = EditorGUILayout.FloatField("종료", _captureEndTime);
                }

                if (_captureMotion != null)
                {
                    _captureStartTime = Mathf.Clamp(_captureStartTime, 0f, totalDuration);
                    if (_captureEndTime > 0f)
                        _captureEndTime = Mathf.Clamp(_captureEndTime, _captureStartTime, totalDuration);

                    float effectiveEnd = ResolveCaptureEndTime();
                    EditorGUILayout.LabelField(
                        $"촬영 구간: {_captureStartTime:0.000}s → {effectiveEnd:0.000}s  ·  길이 {Mathf.Max(0f, effectiveEnd - _captureStartTime):0.000}s",
                        EditorStyles.miniLabel);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    _captureCountdown = Mathf.Clamp(
                        EditorGUILayout.FloatField("카운트다운", _captureCountdown),
                        0f,
                        10f);
                    _autoStopWithMotion = EditorGUILayout.ToggleLeft(
                        "모션 종료 시 자동 정지",
                        _autoStopWithMotion,
                        GUILayout.Width(145f));
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(_captureMotion == null))
                    {
                        if (GUILayout.Button("모션 에디터 열기", GUILayout.Height(24f)))
                            MotionSetEditorWindow.OpenForCapture(_captureMotion);
                    }

                    bool isBusy = _isCaptureCountdownActive
                                  || (_recorder != null && _recorder.IsRecording);
                    using (new EditorGUI.DisabledScope(
                               !Application.isPlaying
                               || _captureMotion == null
                               || isBusy))
                    {
                        if (GUILayout.Button("● 동기 촬영 시작", GUILayout.Height(28f)))
                            StartSynchronizedTakeCountdown();
                    }

                    using (new EditorGUI.DisabledScope(
                               !_isCaptureCountdownActive
                               && !_isSynchronizedTake))
                    {
                        if (GUILayout.Button("취소/정지", GUILayout.Width(90f), GUILayout.Height(28f)))
                            CancelOrStopSynchronizedTake();
                    }
                }

                if (_isCaptureCountdownActive)
                {
                    double remaining = Mathf.Max(
                        0f,
                        (float)(_captureCountdownEndTime - EditorApplication.timeSinceStartup));
                    EditorGUILayout.HelpBox($"촬영 시작까지 {remaining:0.0}초", MessageType.Info);
                }
                else if (!string.IsNullOrEmpty(_captureStatus))
                {
                    EditorGUILayout.LabelField(_captureStatus, EditorStyles.miniLabel);
                }
            }
        }

        private void DrawSmoothingControls()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("손떨림 스무딩 (비파괴)", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("녹화 원본(raw)은 보존되며, 강도를 바꿔 몇 번이고 다시 적용해도 누적되지 않습니다.", MessageType.None);

                _smoothingStrength = EditorGUILayout.Slider("강도", _smoothingStrength, 0f, 1f);

                bool hasRaw = _recording != null
                              && ((_recording.rawSamples != null && _recording.rawSamples.Count > 0)
                                  || _recording.SampleCount > 0);

                using (new EditorGUI.DisabledScope(!hasRaw))
                {
                    if (GUILayout.Button("스무딩 적용 / 재생성", GUILayout.Height(24f)))
                        ApplySmoothingToRecording();
                }

                if (_recording != null)
                    EditorGUILayout.LabelField($"현재 에셋 강도: {_recording.smoothingStrength:0.00} · raw {(_recording.rawSamples != null ? _recording.rawSamples.Count : 0)}개 → 재생 {_recording.SampleCount}개");

                if (_recording == null)
                    EditorGUILayout.HelpBox("강도를 적용하려면 녹화 에셋을 지정하세요.", MessageType.Info);
            }
        }

        private void ApplySmoothingToRecording()
        {
            if (_recording == null)
                return;

            Undo.RecordObject(_recording, "Smooth Dialogue Camera Recording");
            _recording.smoothingStrength = _smoothingStrength;
            _recording.RebuildSmoothedSamples(); // 항상 raw에서 재계산 → 비파괴
            EditorUtility.SetDirty(_recording);
            AssetDatabase.SaveAssets();

            Debug.Log($"[DialogueCameraRecorder] 스무딩 적용: {_recording.name} — 강도 {_smoothingStrength:0.00}, raw {(_recording.rawSamples != null ? _recording.rawSamples.Count : 0)} → 재생 {_recording.SampleCount}개");
        }

        private void DrawFreeCameraControls()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("프리카메라(카메라 몰기)", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _freeCameraMoveSpeed = Mathf.Max(0.1f, EditorGUILayout.FloatField("이동 속도", _freeCameraMoveSpeed));
                    _freeCameraLookSensitivity = Mathf.Max(0.01f, EditorGUILayout.FloatField("회전 감도", _freeCameraLookSensitivity));
                }

                bool isPlaying = Application.isPlaying;
                bool hasManager = isPlaying && CameraManager.Instance != null;
                bool isFreeCamera = hasManager && CameraManager.Instance.IsFreeCameraActive;

                using (new EditorGUI.DisabledScope(!hasManager))
                {
                    if (GUILayout.Button(isFreeCamera ? "프리카메라 종료" : "프리카메라 시작", GUILayout.Height(24f)))
                    {
                        if (isFreeCamera)
                            CameraManager.Instance.PopCameraMode();
                        else
                            CameraManager.Instance.PushFreeCamera(_freeCameraMoveSpeed, _freeCameraLookSensitivity);
                    }
                }

                if (isPlaying)
                    EditorGUILayout.HelpBox("우클릭 드래그 회전, WASD 이동, Q/E 하강/상승, Shift 가속, 휠 FOV", MessageType.None);
                else
                    EditorGUILayout.HelpBox("녹화는 PlayMode에서만 동작합니다.", MessageType.Info);
            }
        }

        private void DrawRecordControls()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("녹화", EditorStyles.boldLabel);

                bool isPlaying = Application.isPlaying;
                bool isRecording = _recorder != null && _recorder.IsRecording;

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(
                               !isPlaying
                               || isRecording
                               || _isCaptureCountdownActive))
                    {
                        if (GUILayout.Button("● 녹화 시작", GUILayout.Height(28f)))
                            StartRecording(false);
                    }

                    using (new EditorGUI.DisabledScope(!isRecording))
                    {
                        if (GUILayout.Button("■ 정지", GUILayout.Height(28f)))
                        {
                            if (_isSynchronizedTake)
                                StopSynchronizedTake(true);
                            else
                                StopRecording();
                        }
                    }
                }

                if (isRecording)
                {
                    EditorGUILayout.LabelField($"녹화 중… 샘플 {_recorder.SampleCount}개 / {_recorder.RecordedDuration:0.00}s");
                }
                else if (_pendingSamples != null)
                {
                    float pendingDuration = GetPendingDuration();
                    EditorGUILayout.LabelField($"대기 중인 녹화: 샘플 {_pendingSamples.Count}개 / {pendingDuration:0.00}s");

                    if (_lastMotionDuration > 0f)
                    {
                        float durationError = pendingDuration - _lastMotionDuration;
                        MessageType type = Mathf.Abs(durationError) <= 1f / Mathf.Max(1f, _pendingSampleRate)
                            ? MessageType.Info
                            : MessageType.Warning;
                        EditorGUILayout.HelpBox(
                            $"모션 {_lastMotionDuration:0.000}s · 녹화 {pendingDuration:0.000}s · 차이 {durationError:+0.000;-0.000;0.000}s",
                            type);
                    }

                    using (new EditorGUI.DisabledScope(_recording == null || _pendingSamples.Count == 0))
                    {
                        if (GUILayout.Button("녹화 에셋에 저장(베이크)", GUILayout.Height(24f)))
                            SaveToAsset();
                    }

                    if (_recording == null)
                        EditorGUILayout.HelpBox("저장하려면 상단에서 녹화 에셋을 지정/생성하세요.", MessageType.Info);
                }
            }
        }

        private void DrawPreviewControls()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("미리보기 재생", EditorStyles.boldLabel);

                bool hasManager = Application.isPlaying && CameraManager.Instance != null;
                bool canPlay = hasManager && _recording != null && _recording.SampleCount > 0;
                bool isActive = hasManager && CameraManager.Instance.IsDialogueCameraRecordingActive(_recording);

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(!canPlay))
                    {
                        if (GUILayout.Button("재생", GUILayout.Height(24f)))
                            CameraManager.Instance.PushDialogueCameraRecording(_recording);
                    }

                    using (new EditorGUI.DisabledScope(!isActive))
                    {
                        if (GUILayout.Button("정지", GUILayout.Width(80f), GUILayout.Height(24f)))
                            CameraManager.Instance.StopDialogueCameraRecording(_recording);
                    }
                }
            }
        }

        private Camera ResolveCaptureCamera()
        {
            if (_captureCamera != null)
                return _captureCamera;
            if (Application.isPlaying && CameraManager.Instance != null)
                return CameraManager.Instance.GetMainCamera();
            return Camera.main;
        }

        private bool StartRecording(bool synchronizedTake)
        {
            Camera camera = ResolveCaptureCamera();
            if (camera == null)
            {
                EditorUtility.DisplayDialog("녹화 불가", "캡처 카메라를 찾을 수 없습니다.", "확인");
                return false;
            }

            if (_recorder == null)
            {
                var go = new GameObject("[DialogueCameraRecorder]");
                go.hideFlags = HideFlags.DontSave;
                _recorder = go.AddComponent<DialogueCameraRecorder>();
            }

            _recorder.SampleRate = _sampleRate;
            _recorder.Space = _space;
            _recorder.Anchor = _anchor;
            _recorder.BeginRecording(camera);

            _pendingSamples = null;
            _lastMotionDuration = 0f;
            _isSynchronizedTake = synchronizedTake;
            return true;
        }

        private void StopRecording()
        {
            if (_recorder == null)
                return;

            IReadOnlyList<DialogueCameraRecordingSO.Sample> samples = _recorder.EndRecording();
            _pendingSamples = new List<DialogueCameraRecordingSO.Sample>(samples);
            _pendingSampleRate = _recorder.SampleRate;
            _pendingSpace = _recorder.Space;
            _isSynchronizedTake = false;
        }

        private void StartSynchronizedTakeCountdown()
        {
            if (!ValidateSynchronizedTake(out string error))
            {
                EditorUtility.DisplayDialog("동기 촬영 불가", error, "확인");
                return;
            }

            _captureStatus = string.Empty;
            if (_captureCountdown <= 0f)
            {
                BeginSynchronizedTake();
                return;
            }

            _isCaptureCountdownActive = true;
            _captureCountdownEndTime = EditorApplication.timeSinceStartup + _captureCountdown;
        }

        private void BeginSynchronizedTake()
        {
            if (!ValidateSynchronizedTake(out string error))
            {
                _captureStatus = error;
                return;
            }

            // 샘플러를 먼저 켜서 모션의 시작 포즈(t=start)를 첫 샘플로 확보한다.
            if (!StartRecording(true))
                return;

            float endTime = ResolveCaptureEndTime();
            if (!MotionSetEditorWindow.TryStartCapturePlayback(
                    _captureMotion,
                    _captureStartTime,
                    endTime,
                    out error))
            {
                StopRecording();
                _pendingSamples = null;
                _captureStatus = error;
                EditorUtility.DisplayDialog("모션 재생 실패", error, "확인");
                return;
            }

            _lastMotionDuration = endTime - _captureStartTime;
            _captureStatus = $"동기 촬영 중 · {_captureMotion.name} · {_captureStartTime:0.000}s → {endTime:0.000}s";
        }

        private void CancelOrStopSynchronizedTake()
        {
            if (_isCaptureCountdownActive)
            {
                _isCaptureCountdownActive = false;
                _captureStatus = "동기 촬영 카운트다운 취소";
                return;
            }

            StopSynchronizedTake(true);
        }

        private void StopSynchronizedTake(bool stopMotion)
        {
            if (_recorder != null && _recorder.IsRecording)
                StopRecording();

            if (stopMotion)
                MotionSetEditorWindow.StopCapturePlayback(_captureMotion);

            _captureStatus = _pendingSamples != null
                ? $"동기 촬영 완료 · {_pendingSamples.Count} samples · {GetPendingDuration():0.000}s"
                : "동기 촬영 정지";
        }

        private bool ValidateSynchronizedTake(out string error)
        {
            error = string.Empty;

            if (!Application.isPlaying)
            {
                error = "PlayMode에서만 동기 촬영할 수 있습니다.";
                return false;
            }

            if (_captureMotion == null
                || _captureMotion.motionSet == null
                || !_captureMotion.motionSet.IsValid())
            {
                error = "유효한 MotionSetAsset을 지정하세요.";
                return false;
            }

            float endTime = ResolveCaptureEndTime();
            if (endTime <= _captureStartTime)
            {
                error = "촬영 종료 시간은 시작 시간보다 커야 합니다. 종료 -1은 전체 길이를 사용합니다.";
                return false;
            }

            return true;
        }

        private float ResolveCaptureMotionDuration()
        {
            return _captureMotion != null && _captureMotion.motionSet != null
                ? _captureMotion.motionSet.TotalDuration
                : 0f;
        }

        private float ResolveCaptureEndTime()
        {
            float totalDuration = ResolveCaptureMotionDuration();
            return _captureEndTime > 0f
                ? Mathf.Clamp(_captureEndTime, _captureStartTime, totalDuration)
                : totalDuration;
        }

        private float GetPendingDuration()
        {
            return _pendingSamples != null && _pendingSamples.Count > 1
                ? (_pendingSamples.Count - 1) / Mathf.Max(1f, _pendingSampleRate)
                : 0f;
        }

        private void SaveToAsset()
        {
            if (_recording == null || _pendingSamples == null || _pendingSamples.Count == 0)
                return;

            Undo.RecordObject(_recording, "Bake Dialogue Camera Recording");
            _recording.sampleRate = _pendingSampleRate;
            _recording.space = _pendingSpace;
            // 원본을 raw로 보존하고, samples는 현재 강도로 스무딩해 재생성(비파괴)
            _recording.rawSamples = new List<DialogueCameraRecordingSO.Sample>(_pendingSamples);
            _recording.smoothingStrength = _smoothingStrength;
            _recording.RebuildSmoothedSamples();
            EditorUtility.SetDirty(_recording);
            AssetDatabase.SaveAssets();

            Debug.Log($"[DialogueCameraRecorder] 저장 완료: {_recording.name} — raw {_recording.rawSamples.Count} → 재생 {_recording.SampleCount}개 / {_recording.Duration:0.00}s (스무딩 {_smoothingStrength:0.00})");
        }

        private void CreateRecordingAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "대화 카메라 녹화 에셋 생성", "DCR_New", "asset",
                "녹화 데이터를 저장할 위치를 선택하세요.");
            if (string.IsNullOrEmpty(path))
                return;

            var asset = CreateInstance<DialogueCameraRecordingSO>();
            asset.space = _space;
            asset.sampleRate = _sampleRate;
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            _recording = asset;
            EditorGUIUtility.PingObject(asset);
        }
    }
}
#endif
