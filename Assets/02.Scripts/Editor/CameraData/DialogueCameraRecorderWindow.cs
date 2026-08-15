#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Animation.Editor;
using UPlayGround.CameraSystem;
using UPlayGround.Dialogue;
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
        private sealed class CaptureTake
        {
            public string Name;
            public List<DialogueCameraRecordingSO.Sample> Samples;
            public float SampleRate;
            public CameraSnapshotSpace Space;
            public float MotionDuration;
            public System.DateTime CapturedAt;
            public int TrimIn;
            public int TrimOut;
            public readonly List<TakeQualityIssue> QualityIssues = new();

            public int EffectiveTrimOut => Samples == null || Samples.Count == 0
                ? -1
                : Mathf.Clamp(TrimOut < 0 ? Samples.Count - 1 : TrimOut, TrimIn, Samples.Count - 1);
            public int TrimmedSampleCount => EffectiveTrimOut >= TrimIn
                ? EffectiveTrimOut - TrimIn + 1
                : 0;
            public float Duration => TrimmedSampleCount > 1
                ? (TrimmedSampleCount - 1) / Mathf.Max(1f, SampleRate)
                : 0f;
        }

        private readonly struct TakeQualityIssue
        {
            public readonly int SampleIndex;
            public readonly bool IsError;
            public readonly string Message;

            public TakeQualityIssue(int sampleIndex, bool isError, string message)
            {
                SampleIndex = sampleIndex;
                IsError = isError;
                Message = message;
            }
        }

        private DialogueCameraRecordingSO _recording;
        private Camera _captureCamera;
        private Transform _anchor;
        private CameraSnapshotSpace _space = CameraSnapshotSpace.ActorRelative;
        private float _sampleRate = 30f;

        private float _freeCameraMoveSpeed = 6f;
        private float _freeCameraLookSensitivity = 0.12f;

        private float _smoothingStrength = 0.35f;
        private bool _usePerChannelSmoothing = true;
        private float _positionSmoothingStrength = 0.35f;
        private float _rotationSmoothingStrength = 0.2f;
        private float _fovSmoothingStrength = 0.35f;
        private bool _useKeyReduction;
        private float _positionReductionTolerance = 0.01f;
        private float _rotationReductionTolerance = 0.5f;
        private float _fovReductionTolerance = 0.1f;

        private DialogueCameraRecorder _recorder;
        private List<DialogueCameraRecordingSO.Sample> _pendingSamples;
        private float _pendingSampleRate;
        private CameraSnapshotSpace _pendingSpace;
        private readonly List<CaptureTake> _takes = new();
        private int _selectedTakeIndex = -1;
        private Vector2 _mainScroll;
        private Vector2 _takeScroll;
        private int _takeSequence = 1;
        private float _takeScrubTime;
        private bool _showRawTakePreview;
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
        private DialogueCameraRecordingSO _takePreviewRecording;
        private DialogueNodeSO _targetDialogueNode;

        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/월드/카메라/대화 카메라 녹화", priority = UPlayGround.Tool.Editor.UPlaygroundMenuPriority.WorldCamera)]
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

            if (_takePreviewRecording != null)
            {
                DestroyImmediate(_takePreviewRecording);
                _takePreviewRecording = null;
            }
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
            DrawTransportBar();
            EditorGUILayout.Space(4f);

            _mainScroll = EditorGUILayout.BeginScrollView(_mainScroll);
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

                DrawReadinessChecklist();
            }

            EditorGUILayout.Space(6f);
            DrawFreeCameraControls();

            EditorGUILayout.Space(6f);
            DrawSynchronizedCaptureControls();

            EditorGUILayout.Space(6f);
            DrawRecordControls();

            EditorGUILayout.Space(6f);
            DrawTakeTimeline();

            EditorGUILayout.Space(6f);
            DrawSmoothingControls();

            EditorGUILayout.Space(6f);
            DrawPreviewControls();

            EditorGUILayout.Space(6f);
            DrawIntegrationControls();
            EditorGUILayout.EndScrollView();
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

        private void DrawTransportBar()
        {
            bool isRecording = _recorder != null && _recorder.IsRecording;
            string state;
            Color stateColor;
            if (_isCaptureCountdownActive)
            {
                state = "COUNTDOWN";
                stateColor = new Color(1f, 0.62f, 0.12f);
            }
            else if (isRecording)
            {
                state = "● REC";
                stateColor = new Color(0.95f, 0.2f, 0.18f);
            }
            else if (_selectedTakeIndex >= 0 && _selectedTakeIndex < _takes.Count)
            {
                state = "TAKE READY";
                stateColor = new Color(0.25f, 0.78f, 0.38f);
            }
            else if (CanStartBasicRecording(out _))
            {
                state = "READY";
                stateColor = new Color(0.28f, 0.62f, 0.95f);
            }
            else
            {
                state = "NOT READY";
                stateColor = new Color(0.48f, 0.48f, 0.48f);
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                Color previous = GUI.color;
                GUI.color = stateColor;
                GUILayout.Label(state, EditorStyles.boldLabel, GUILayout.Width(96f));
                GUI.color = previous;

                float duration = isRecording
                    ? _recorder.RecordedDuration
                    : GetSelectedTake()?.Duration ?? 0f;
                int samples = isRecording
                    ? _recorder.SampleCount
                    : GetSelectedTake()?.Samples?.Count ?? 0;

                GUILayout.Label(FormatTimecode(duration), EditorStyles.boldLabel, GUILayout.Width(82f));
                GUILayout.Label($"{samples} samples", EditorStyles.miniLabel, GUILayout.Width(82f));
                GUILayout.Label($"Take {_takes.Count}", EditorStyles.miniLabel, GUILayout.Width(58f));
                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(!CanStartBasicRecording(out _) || isRecording || _isCaptureCountdownActive))
                {
                    if (GUILayout.Button("● REC", EditorStyles.toolbarButton, GUILayout.Width(58f)))
                        StartRecording(false);
                }

                using (new EditorGUI.DisabledScope(!isRecording))
                {
                    if (GUILayout.Button("■ STOP", EditorStyles.toolbarButton, GUILayout.Width(64f)))
                    {
                        if (_isSynchronizedTake)
                            StopSynchronizedTake(true);
                        else
                            StopRecording();
                    }
                }
            }
        }

        private void DrawReadinessChecklist()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("촬영 준비", EditorStyles.boldLabel);
            DrawChecklistRow(Application.isPlaying, "PlayMode", "PlayMode로 전환하세요.");

            Camera camera = ResolveCaptureCamera();
            DrawChecklistRow(
                camera != null,
                camera != null ? $"캡처 카메라: {camera.name}" : "캡처 카메라",
                "캡처할 카메라를 지정하거나 Main Camera를 준비하세요.");

            bool anchorReady = _space != CameraSnapshotSpace.ActorRelative || _anchor != null;
            DrawChecklistRow(
                anchorReady,
                _space == CameraSnapshotSpace.ActorRelative
                    ? $"ActorRelative 앵커: {(_anchor != null ? _anchor.name : "없음")}"
                    : "World 좌표계",
                "ActorRelative 녹화에는 앵커가 반드시 필요합니다.");

            if (_captureMotion != null)
            {
                float duration = Mathf.Max(0f, ResolveCaptureEndTime() - _captureStartTime);
                bool validMotion = _captureMotion.motionSet != null
                                   && _captureMotion.motionSet.IsValid()
                                   && duration > 0f;
                DrawChecklistRow(
                    validMotion,
                    validMotion
                        ? $"MotionSet: {_captureMotion.name} · {duration:0.000}s · 예상 {Mathf.CeilToInt(duration * _sampleRate) + 1} samples"
                        : "MotionSet 촬영 구간",
                    "유효한 MotionSet과 시작/종료 구간을 지정하세요.");
            }
        }

        private static void DrawChecklistRow(bool valid, string label, string invalidHint)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                Color previous = GUI.color;
                GUI.color = valid
                    ? new Color(0.35f, 0.85f, 0.42f)
                    : new Color(1f, 0.42f, 0.28f);
                GUILayout.Label(valid ? "✓" : "!", EditorStyles.boldLabel, GUILayout.Width(18f));
                GUI.color = previous;
                GUILayout.Label(label, valid ? EditorStyles.miniLabel : EditorStyles.boldLabel);
            }

            if (!valid)
                EditorGUILayout.LabelField($"    {invalidHint}", EditorStyles.wordWrappedMiniLabel);
        }

        private void DrawSmoothingControls()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("손떨림 스무딩 (비파괴)", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("녹화 원본(raw)은 보존되며, 강도를 바꿔 몇 번이고 다시 적용해도 누적되지 않습니다.", MessageType.None);

                _usePerChannelSmoothing = EditorGUILayout.ToggleLeft("채널별 강도 사용", _usePerChannelSmoothing);
                if (_usePerChannelSmoothing)
                {
                    _positionSmoothingStrength = EditorGUILayout.Slider("위치", _positionSmoothingStrength, 0f, 1f);
                    _rotationSmoothingStrength = EditorGUILayout.Slider("회전", _rotationSmoothingStrength, 0f, 1f);
                    _fovSmoothingStrength = EditorGUILayout.Slider("FOV", _fovSmoothingStrength, 0f, 1f);
                }
                else
                {
                    _smoothingStrength = EditorGUILayout.Slider("전체 강도", _smoothingStrength, 0f, 1f);
                }

                bool hasRaw = _recording != null
                              && ((_recording.rawSamples != null && _recording.rawSamples.Count > 0)
                                  || _recording.SampleCount > 0);

                using (new EditorGUI.DisabledScope(!hasRaw))
                {
                    if (GUILayout.Button("스무딩 적용 / 재생성", GUILayout.Height(24f)))
                        ApplySmoothingToRecording();
                }

                if (_recording != null)
                    EditorGUILayout.LabelField(
                        _recording.usePerChannelSmoothing
                            ? $"현재 에셋 P {_recording.positionSmoothingStrength:0.00} / R {_recording.rotationSmoothingStrength:0.00} / FOV {_recording.fovSmoothingStrength:0.00} · raw {(_recording.rawSamples != null ? _recording.rawSamples.Count : 0)} → 재생 {_recording.SampleCount}"
                            : $"현재 에셋 강도 {_recording.smoothingStrength:0.00} · raw {(_recording.rawSamples != null ? _recording.rawSamples.Count : 0)} → 재생 {_recording.SampleCount}");

                if (_recording == null)
                    EditorGUILayout.HelpBox("강도를 적용하려면 녹화 에셋을 지정하세요.", MessageType.Info);

                EditorGUILayout.Space(4f);
                _useKeyReduction = EditorGUILayout.ToggleLeft("오차 기반 키 리덕션", _useKeyReduction);
                if (_useKeyReduction)
                {
                    _positionReductionTolerance = Mathf.Max(
                        0.0001f,
                        EditorGUILayout.FloatField("위치 허용 오차", _positionReductionTolerance));
                    _rotationReductionTolerance = Mathf.Max(
                        0.01f,
                        EditorGUILayout.FloatField("회전 허용 오차(°)", _rotationReductionTolerance));
                    _fovReductionTolerance = Mathf.Max(
                        0.01f,
                        EditorGUILayout.FloatField("FOV 허용 오차", _fovReductionTolerance));
                }
            }
        }

        private void ApplySmoothingToRecording()
        {
            if (_recording == null)
                return;

            Undo.RecordObject(_recording, "Smooth Dialogue Camera Recording");
            _recording.smoothingStrength = _smoothingStrength;
            _recording.usePerChannelSmoothing = _usePerChannelSmoothing;
            _recording.positionSmoothingStrength = _positionSmoothingStrength;
            _recording.rotationSmoothingStrength = _rotationSmoothingStrength;
            _recording.fovSmoothingStrength = _fovSmoothingStrength;
            _recording.useKeyReduction = _useKeyReduction;
            _recording.positionReductionTolerance = _positionReductionTolerance;
            _recording.rotationReductionTolerance = _rotationReductionTolerance;
            _recording.fovReductionTolerance = _fovReductionTolerance;
            _recording.RebuildSmoothedSamples(); // 항상 raw에서 재계산 → 비파괴
            EditorUtility.SetDirty(_recording);
            AssetDatabase.SaveAssets();

            Debug.Log($"[DialogueCameraRecorder] 스무딩 적용: {_recording.name} — raw {(_recording.rawSamples != null ? _recording.rawSamples.Count : 0)} → 재생 {_recording.SampleCount}개");
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
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("테이크", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField($"{_takes.Count}개", EditorStyles.miniLabel, GUILayout.Width(38f));
                }

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

                _takeScroll = EditorGUILayout.BeginScrollView(
                    _takeScroll,
                    EditorStyles.helpBox,
                    GUILayout.MinHeight(80f),
                    GUILayout.MaxHeight(180f));
                if (_takes.Count == 0)
                {
                    EditorGUILayout.LabelField("녹화를 시작하면 여러 테이크를 비교해서 보존할 수 있습니다.", EditorStyles.centeredGreyMiniLabel);
                }
                else
                {
                    for (int i = 0; i < _takes.Count; i++)
                        DrawTakeRow(i);
                }
                EditorGUILayout.EndScrollView();

                CaptureTake selectedTake = GetSelectedTake();
                if (selectedTake == null)
                    return;

                if (selectedTake.MotionDuration > 0f)
                {
                    float durationError = selectedTake.Duration - selectedTake.MotionDuration;
                    MessageType type = Mathf.Abs(durationError) <= 1f / Mathf.Max(1f, selectedTake.SampleRate)
                        ? MessageType.Info
                        : MessageType.Warning;
                    EditorGUILayout.HelpBox(
                        $"모션 {selectedTake.MotionDuration:0.000}s · 녹화 {selectedTake.Duration:0.000}s · 차이 {durationError:+0.000;-0.000;0.000}s",
                        type);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(_recording == null || selectedTake.Samples.Count == 0))
                    {
                        if (GUILayout.Button("선택 Take를 녹화 에셋에 채택", GUILayout.Height(26f)))
                            SaveToAsset();
                    }

                    if (GUILayout.Button("삭제", GUILayout.Width(64f), GUILayout.Height(26f)))
                        DeleteSelectedTake();
                }

                if (_recording == null)
                    EditorGUILayout.HelpBox("채택하려면 상단에서 녹화 에셋을 지정하거나 생성하세요.", MessageType.Info);
            }
        }

        private void DrawTakeRow(int index)
        {
            CaptureTake take = _takes[index];
            bool selected = index == _selectedTakeIndex;
            Rect row = GUILayoutUtility.GetRect(0f, 34f, GUILayout.ExpandWidth(true));
            if (selected)
                EditorGUI.DrawRect(row, new Color(0.18f, 0.38f, 0.58f, 0.5f));
            else if (UnityEngine.Event.current.type == EventType.Repaint && index % 2 == 0)
                EditorGUI.DrawRect(row, new Color(1f, 1f, 1f, 0.025f));

            GUI.Label(
                new Rect(row.x + 8f, row.y + 2f, row.width - 16f, 17f),
                take.Name,
                selected ? EditorStyles.whiteBoldLabel : EditorStyles.boldLabel);

            string detail = $"{take.Duration:0.000}s · {take.Samples.Count} samples · {take.SampleRate:0.#}Hz · {take.Space}";
            if (take.MotionDuration > 0f)
                detail += $" · Δ {take.Duration - take.MotionDuration:+0.000;-0.000;0.000}s";
            GUI.Label(
                new Rect(row.x + 8f, row.y + 17f, row.width - 16f, 15f),
                detail,
                selected ? EditorStyles.whiteMiniLabel : EditorStyles.miniLabel);

            if (UnityEngine.Event.current.type == EventType.MouseDown
                && row.Contains(UnityEngine.Event.current.mousePosition))
            {
                SelectTake(index);
                UnityEngine.Event.current.Use();
                Repaint();
            }
        }

        private void DrawTakeTimeline()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Take 타임라인 / 트림", EditorStyles.boldLabel);
                CaptureTake take = GetSelectedTake();
                if (take == null || take.Samples == null || take.Samples.Count == 0)
                {
                    EditorGUILayout.LabelField("편집할 Take를 선택하세요.", EditorStyles.centeredGreyMiniLabel);
                    return;
                }

                int maxIndex = take.Samples.Count - 1;
                take.TrimIn = Mathf.Clamp(take.TrimIn, 0, maxIndex);
                take.TrimOut = take.TrimOut < 0
                    ? maxIndex
                    : Mathf.Clamp(take.TrimOut, take.TrimIn, maxIndex);

                float fullDuration = maxIndex / Mathf.Max(1f, take.SampleRate);
                float trimInTime = take.TrimIn / Mathf.Max(1f, take.SampleRate);
                float trimOutTime = take.TrimOut / Mathf.Max(1f, take.SampleRate);

                EditorGUI.BeginChangeCheck();
                _takeScrubTime = EditorGUILayout.Slider(
                    "재생 헤드",
                    Mathf.Clamp(_takeScrubTime, 0f, fullDuration),
                    0f,
                    fullDuration);
                if (EditorGUI.EndChangeCheck())
                    ApplyTakeSampleToCamera(take, Mathf.RoundToInt(_takeScrubTime * take.SampleRate));

                Rect timeline = GUILayoutUtility.GetRect(0f, 30f, GUILayout.ExpandWidth(true));
                DrawTimelineBar(timeline, take, fullDuration);

                using (new EditorGUILayout.HorizontalScope())
                {
                    float newIn = EditorGUILayout.FloatField("IN", trimInTime);
                    float newOut = EditorGUILayout.FloatField("OUT", trimOutTime);
                    if (!Mathf.Approximately(newIn, trimInTime))
                        take.TrimIn = Mathf.Clamp(Mathf.RoundToInt(newIn * take.SampleRate), 0, take.TrimOut);
                    if (!Mathf.Approximately(newOut, trimOutTime))
                        take.TrimOut = Mathf.Clamp(Mathf.RoundToInt(newOut * take.SampleRate), take.TrimIn, maxIndex);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("현재 위치 → IN"))
                        take.TrimIn = Mathf.Clamp(Mathf.RoundToInt(_takeScrubTime * take.SampleRate), 0, take.TrimOut);
                    if (GUILayout.Button("현재 위치 → OUT"))
                        take.TrimOut = Mathf.Clamp(Mathf.RoundToInt(_takeScrubTime * take.SampleRate), take.TrimIn, maxIndex);
                    if (GUILayout.Button("트림 초기화", GUILayout.Width(86f)))
                    {
                        take.TrimIn = 0;
                        take.TrimOut = maxIndex;
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("◀ 1 Frame", GUILayout.Width(88f)))
                        StepTakeFrame(take, -1);
                    if (GUILayout.Button("1 Frame ▶", GUILayout.Width(88f)))
                        StepTakeFrame(take, 1);
                    _showRawTakePreview = GUILayout.Toggle(
                        _showRawTakePreview,
                        "Raw 미리보기",
                        EditorStyles.miniButton,
                        GUILayout.Width(96f));
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(
                        $"{take.TrimmedSampleCount}/{take.Samples.Count} samples · {take.Duration:0.000}s",
                        EditorStyles.miniLabel);
                }

                DrawTakeQualitySummary(take);
            }
        }

        private void DrawTimelineBar(Rect rect, CaptureTake take, float fullDuration)
        {
            EditorGUI.DrawRect(rect, new Color(0.10f, 0.10f, 0.12f));
            if (fullDuration <= 0f)
                return;

            float inX = Mathf.Lerp(rect.x, rect.xMax, take.TrimIn / (float)Mathf.Max(1, take.Samples.Count - 1));
            float outX = Mathf.Lerp(rect.x, rect.xMax, take.TrimOut / (float)Mathf.Max(1, take.Samples.Count - 1));
            EditorGUI.DrawRect(
                new Rect(inX, rect.y + 4f, Mathf.Max(2f, outX - inX), rect.height - 8f),
                new Color(0.18f, 0.48f, 0.72f, 0.75f));

            foreach (TakeQualityIssue issue in take.QualityIssues)
            {
                float markerX = Mathf.Lerp(
                    rect.x,
                    rect.xMax,
                    issue.SampleIndex / (float)Mathf.Max(1, take.Samples.Count - 1));
                EditorGUI.DrawRect(
                    new Rect(markerX - 1f, rect.y + 2f, 3f, 7f),
                    issue.IsError
                        ? new Color(0.95f, 0.2f, 0.16f)
                        : new Color(1f, 0.68f, 0.12f));
            }

            float headX = Mathf.Lerp(rect.x, rect.xMax, Mathf.Clamp01(_takeScrubTime / fullDuration));
            EditorGUI.DrawRect(new Rect(headX - 1f, rect.y, 2f, rect.height), new Color(1f, 0.78f, 0.16f));
            GUI.Label(new Rect(rect.x + 5f, rect.y + 6f, 90f, 18f), "IN", EditorStyles.whiteMiniLabel);
            GUI.Label(new Rect(rect.xMax - 28f, rect.y + 6f, 24f, 18f), "OUT", EditorStyles.whiteMiniLabel);
        }

        private void DrawTakeQualitySummary(CaptureTake take)
        {
            int errors = 0;
            int warnings = 0;
            foreach (TakeQualityIssue issue in take.QualityIssues)
            {
                if (issue.IsError) errors++;
                else warnings++;
            }

            if (errors == 0 && warnings == 0)
            {
                EditorGUILayout.HelpBox("품질 분석: 급격한 이동·회전·FOV 변화가 감지되지 않았습니다.", MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox(
                $"품질 분석: 오류 {errors} / 경고 {warnings}. 타임라인 상단의 빨강·주황 마커를 확인하세요.",
                errors > 0 ? MessageType.Error : MessageType.Warning);

            int shown = 0;
            foreach (TakeQualityIssue issue in take.QualityIssues)
            {
                if (shown++ >= 5)
                {
                    EditorGUILayout.LabelField($"외 {take.QualityIssues.Count - 5}개", EditorStyles.miniLabel);
                    break;
                }

                float time = issue.SampleIndex / Mathf.Max(1f, take.SampleRate);
                if (GUILayout.Button(
                        $"{time:0.000}s · {issue.Message}",
                        EditorStyles.miniButton))
                {
                    _takeScrubTime = time;
                    ApplyTakeSampleToCamera(take, issue.SampleIndex);
                }
            }
        }

        private void StepTakeFrame(CaptureTake take, int delta)
        {
            int current = Mathf.RoundToInt(_takeScrubTime * take.SampleRate);
            current = Mathf.Clamp(current + delta, 0, take.Samples.Count - 1);
            _takeScrubTime = current / Mathf.Max(1f, take.SampleRate);
            ApplyTakeSampleToCamera(take, current);
        }

        private void ApplyTakeSampleToCamera(CaptureTake take, int sampleIndex)
        {
            Camera camera = ResolveCaptureCamera();
            if (camera == null || take?.Samples == null || take.Samples.Count == 0)
                return;

            sampleIndex = Mathf.Clamp(sampleIndex, 0, take.Samples.Count - 1);
            IReadOnlyList<DialogueCameraRecordingSO.Sample> previewSamples = take.Samples;
            if (!_showRawTakePreview)
            {
                previewSamples = _usePerChannelSmoothing
                    ? DialogueCameraTrackSmoother.Smooth(
                        take.Samples,
                        _positionSmoothingStrength,
                        _rotationSmoothingStrength,
                        _fovSmoothingStrength)
                    : DialogueCameraTrackSmoother.Smooth(take.Samples, _smoothingStrength);
            }

            DialogueCameraRecordingSO.Sample sample = previewSamples[sampleIndex];
            Vector3 position = sample.localPosition;
            Quaternion rotation = Quaternion.Euler(sample.localEuler);
            if (take.Space == CameraSnapshotSpace.ActorRelative && _anchor != null)
            {
                position = _anchor.TransformPoint(position);
                rotation = _anchor.rotation * rotation;
            }

            camera.transform.SetPositionAndRotation(position, rotation);
            camera.fieldOfView = sample.fieldOfView;
        }

        private void DrawPreviewControls()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("미리보기 재생", EditorStyles.boldLabel);

                bool hasManager = Application.isPlaying && CameraManager.Instance != null;
                bool canPlay = hasManager && _recording != null && _recording.SampleCount > 0;
                bool isSavedActive = hasManager && CameraManager.Instance.IsDialogueCameraRecordingActive(_recording);
                bool isTakeActive = hasManager && CameraManager.Instance.IsDialogueCameraRecordingActive(_takePreviewRecording);
                CaptureTake selectedTake = GetSelectedTake();

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(!hasManager || selectedTake == null))
                    {
                        if (GUILayout.Button("선택 Take 미리보기", GUILayout.Height(24f)))
                            PreviewSelectedTake();
                    }

                    using (new EditorGUI.DisabledScope(!canPlay))
                    {
                        if (GUILayout.Button("저장 에셋 재생", GUILayout.Height(24f)))
                            CameraManager.Instance.PushDialogueCameraRecording(_recording);
                    }

                    using (new EditorGUI.DisabledScope(!isSavedActive && !isTakeActive))
                    {
                        if (GUILayout.Button("정지", GUILayout.Width(80f), GUILayout.Height(24f)))
                        {
                            if (isTakeActive)
                                CameraManager.Instance.StopDialogueCameraRecording(_takePreviewRecording);
                            else
                                CameraManager.Instance.StopDialogueCameraRecording(_recording);
                        }
                    }
                }
            }
        }

        private void DrawIntegrationControls()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("저작 연결", EditorStyles.boldLabel);
                _targetDialogueNode = (DialogueNodeSO)EditorGUILayout.ObjectField(
                    "대화 노드",
                    _targetDialogueNode,
                    typeof(DialogueNodeSO),
                    false);

                if (_targetDialogueNode == null && Selection.activeObject is DialogueNodeSO selectedNode)
                {
                    if (GUILayout.Button($"현재 선택 노드 사용: {selectedNode.name}"))
                        _targetDialogueNode = selectedNode;
                }

                using (new EditorGUI.DisabledScope(_recording == null || _targetDialogueNode == null))
                {
                    if (GUILayout.Button("녹화 에셋을 대화 노드에 연결", GUILayout.Height(25f)))
                    {
                        Undo.RecordObject(_targetDialogueNode, "Assign Dialogue Camera Recording");
                        _targetDialogueNode.cameraRecording = _recording;
                        EditorUtility.SetDirty(_targetDialogueNode);
                        AssetDatabase.SaveAssets();
                        Selection.activeObject = _targetDialogueNode;
                        EditorGUIUtility.PingObject(_targetDialogueNode);
                        ShowNotification(new GUIContent("대화 노드에 연결했습니다."));
                    }
                }

                using (new EditorGUI.DisabledScope(_recording == null))
                {
                    if (GUILayout.Button("녹화 에셋 선택"))
                    {
                        Selection.activeObject = _recording;
                        EditorGUIUtility.PingObject(_recording);
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
            if (!CanStartBasicRecording(out string readinessError))
            {
                EditorUtility.DisplayDialog("녹화 불가", readinessError, "확인");
                return false;
            }

            Camera camera = ResolveCaptureCamera();

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
            AddTake(
                _pendingSamples,
                _pendingSampleRate,
                _pendingSpace,
                _isSynchronizedTake ? _lastMotionDuration : 0f);
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
            int takeCountBeforePlayback = _takes.Count;
            if (!MotionSetEditorWindow.TryStartCapturePlayback(
                    _captureMotion,
                    _captureStartTime,
                    endTime,
                    out error))
            {
                StopRecording();
                if (_takes.Count > takeCountBeforePlayback)
                {
                    _takes.RemoveAt(_takes.Count - 1);
                    SelectTake(_takes.Count - 1);
                }
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

            if (!CanStartBasicRecording(out error))
                return false;

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
            CaptureTake selectedTake = GetSelectedTake();
            if (_recording == null || selectedTake == null || selectedTake.Samples == null || selectedTake.Samples.Count == 0)
                return;

            Undo.RecordObject(_recording, "Bake Dialogue Camera Recording");
            _recording.sampleRate = selectedTake.SampleRate;
            _recording.space = selectedTake.Space;
            // 원본을 raw로 보존하고, samples는 현재 강도로 스무딩해 재생성(비파괴)
            _recording.rawSamples = BuildTrimmedSamples(selectedTake);
            _recording.smoothingStrength = _smoothingStrength;
            _recording.usePerChannelSmoothing = _usePerChannelSmoothing;
            _recording.positionSmoothingStrength = _positionSmoothingStrength;
            _recording.rotationSmoothingStrength = _rotationSmoothingStrength;
            _recording.fovSmoothingStrength = _fovSmoothingStrength;
            _recording.useKeyReduction = _useKeyReduction;
            _recording.positionReductionTolerance = _positionReductionTolerance;
            _recording.rotationReductionTolerance = _rotationReductionTolerance;
            _recording.fovReductionTolerance = _fovReductionTolerance;
            _recording.sourceMotion = _captureMotion;
            _recording.sourceScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            _recording.sourceTakeName = selectedTake.Name;
            _recording.capturedAt = selectedTake.CapturedAt.ToString("O");
            _recording.sourceStartTime = _captureMotion != null ? _captureStartTime : 0f;
            _recording.sourceEndTime = _captureMotion != null ? ResolveCaptureEndTime() : selectedTake.Duration;
            _recording.sourceRawSampleCount = selectedTake.Samples.Count;
            _recording.sourceTrimIn = selectedTake.TrimIn;
            _recording.sourceTrimOut = selectedTake.EffectiveTrimOut;
            _recording.RebuildSmoothedSamples();
            EditorUtility.SetDirty(_recording);
            AssetDatabase.SaveAssets();

            _captureStatus = $"{selectedTake.Name} 채택 완료 → {_recording.name}";
            ShowNotification(new GUIContent($"{selectedTake.Name} 저장 완료"));
            Debug.Log($"[DialogueCameraRecorder] 저장 완료: {_recording.name} ← {selectedTake.Name} — raw {_recording.rawSamples.Count} → 재생 {_recording.SampleCount}개 / {_recording.Duration:0.00}s (스무딩 {_smoothingStrength:0.00})");
        }

        private bool CanStartBasicRecording(out string error)
        {
            if (!Application.isPlaying)
            {
                error = "녹화는 PlayMode에서만 사용할 수 있습니다.";
                return false;
            }

            if (ResolveCaptureCamera() == null)
            {
                error = "캡처 카메라를 지정하거나 Main Camera를 준비하세요.";
                return false;
            }

            if (_space == CameraSnapshotSpace.ActorRelative && _anchor == null)
            {
                error = "ActorRelative 녹화에는 앵커 Transform이 필요합니다. 앵커를 지정하거나 좌표계를 World로 변경하세요.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private void AddTake(
            List<DialogueCameraRecordingSO.Sample> samples,
            float sampleRate,
            CameraSnapshotSpace space,
            float motionDuration)
        {
            if (samples == null || samples.Count == 0)
                return;

            var take = new CaptureTake
            {
                Name = $"Take {_takeSequence++:00}",
                Samples = new List<DialogueCameraRecordingSO.Sample>(samples),
                SampleRate = sampleRate,
                Space = space,
                MotionDuration = motionDuration,
                CapturedAt = System.DateTime.Now,
                TrimIn = 0,
                TrimOut = samples.Count - 1
            };
            AnalyzeTakeQuality(take);
            _takes.Add(take);
            SelectTake(_takes.Count - 1);
            _captureStatus = $"{take.Name} 완료 · {take.Samples.Count} samples · {take.Duration:0.000}s";
        }

        private void SelectTake(int index)
        {
            if (index < 0 || index >= _takes.Count)
            {
                _selectedTakeIndex = -1;
                _pendingSamples = null;
                return;
            }

            _selectedTakeIndex = index;
            CaptureTake take = _takes[index];
            _pendingSamples = take.Samples;
            _pendingSampleRate = take.SampleRate;
            _pendingSpace = take.Space;
            _lastMotionDuration = take.MotionDuration;
        }

        private CaptureTake GetSelectedTake()
        {
            return _selectedTakeIndex >= 0 && _selectedTakeIndex < _takes.Count
                ? _takes[_selectedTakeIndex]
                : null;
        }

        private void DeleteSelectedTake()
        {
            if (_selectedTakeIndex < 0 || _selectedTakeIndex >= _takes.Count)
                return;

            string takeName = _takes[_selectedTakeIndex].Name;
            _takes.RemoveAt(_selectedTakeIndex);
            int next = _takes.Count == 0
                ? -1
                : Mathf.Clamp(_selectedTakeIndex, 0, _takes.Count - 1);
            SelectTake(next);
            _captureStatus = $"{takeName} 삭제";
        }

        private void PreviewSelectedTake()
        {
            CaptureTake take = GetSelectedTake();
            if (take == null || CameraManager.Instance == null)
                return;

            if (_takePreviewRecording == null)
            {
                _takePreviewRecording = CreateInstance<DialogueCameraRecordingSO>();
                _takePreviewRecording.hideFlags = HideFlags.HideAndDontSave;
            }

            _takePreviewRecording.name = $"{take.Name} Preview";
            _takePreviewRecording.space = take.Space;
            _takePreviewRecording.sampleRate = take.SampleRate;
            _takePreviewRecording.rawSamples = BuildTrimmedSamples(take);
            _takePreviewRecording.smoothingStrength = _smoothingStrength;
            _takePreviewRecording.usePerChannelSmoothing = !_showRawTakePreview && _usePerChannelSmoothing;
            _takePreviewRecording.positionSmoothingStrength = _positionSmoothingStrength;
            _takePreviewRecording.rotationSmoothingStrength = _rotationSmoothingStrength;
            _takePreviewRecording.fovSmoothingStrength = _fovSmoothingStrength;
            _takePreviewRecording.useKeyReduction = _useKeyReduction;
            _takePreviewRecording.positionReductionTolerance = _positionReductionTolerance;
            _takePreviewRecording.rotationReductionTolerance = _rotationReductionTolerance;
            _takePreviewRecording.fovReductionTolerance = _fovReductionTolerance;
            if (_showRawTakePreview)
                _takePreviewRecording.smoothingStrength = 0f;
            _takePreviewRecording.entryBlendDuration = _recording != null ? _recording.entryBlendDuration : 0.25f;
            _takePreviewRecording.entryBlendCurve = _recording != null
                ? _recording.entryBlendCurve
                : AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            _takePreviewRecording.useCollision = _recording != null && _recording.useCollision;
            _takePreviewRecording.lockCameraInput = true;
            _takePreviewRecording.restorePreviousModeOnFinish = true;
            _takePreviewRecording.RebuildSmoothedSamples();

            CameraManager.Instance.PushDialogueCameraRecording(_takePreviewRecording);
            _captureStatus = $"{take.Name} 미리보기 재생";
        }

        private static List<DialogueCameraRecordingSO.Sample> BuildTrimmedSamples(CaptureTake take)
        {
            var result = new List<DialogueCameraRecordingSO.Sample>();
            if (take?.Samples == null || take.Samples.Count == 0)
                return result;

            int start = Mathf.Clamp(take.TrimIn, 0, take.Samples.Count - 1);
            int end = Mathf.Clamp(take.EffectiveTrimOut, start, take.Samples.Count - 1);
            float startTime = take.Samples[start].sampleTime;
            for (int i = start; i <= end; i++)
            {
                DialogueCameraRecordingSO.Sample sample = take.Samples[i];
                sample.sampleTime -= startTime;
                result.Add(sample);
            }
            return result;
        }

        private static void AnalyzeTakeQuality(CaptureTake take)
        {
            take.QualityIssues.Clear();
            if (take.Samples == null || take.Samples.Count < 2)
                return;

            float sampleRate = Mathf.Max(1f, take.SampleRate);
            for (int i = 1; i < take.Samples.Count; i++)
            {
                DialogueCameraRecordingSO.Sample previous = take.Samples[i - 1];
                DialogueCameraRecordingSO.Sample current = take.Samples[i];
                float speed = Vector3.Distance(previous.localPosition, current.localPosition) * sampleRate;
                float angularSpeed = Quaternion.Angle(
                    Quaternion.Euler(previous.localEuler),
                    Quaternion.Euler(current.localEuler)) * sampleRate;
                float fovSpeed = Mathf.Abs(current.fieldOfView - previous.fieldOfView) * sampleRate;

                if (speed > 30f)
                    take.QualityIssues.Add(new TakeQualityIssue(i, true, $"위치 속도 급증 {speed:0.0}u/s"));
                else if (speed > 15f)
                    take.QualityIssues.Add(new TakeQualityIssue(i, false, $"빠른 위치 이동 {speed:0.0}u/s"));

                if (angularSpeed > 720f)
                    take.QualityIssues.Add(new TakeQualityIssue(i, true, $"회전 급증 {angularSpeed:0}°/s"));
                else if (angularSpeed > 360f)
                    take.QualityIssues.Add(new TakeQualityIssue(i, false, $"빠른 회전 {angularSpeed:0}°/s"));

                if (fovSpeed > 180f)
                    take.QualityIssues.Add(new TakeQualityIssue(i, true, $"FOV 급변 {fovSpeed:0}°/s"));
                else if (fovSpeed > 90f)
                    take.QualityIssues.Add(new TakeQualityIssue(i, false, $"빠른 FOV 변화 {fovSpeed:0}°/s"));

                if (Vector3.SqrMagnitude(current.localPosition - previous.localPosition) < 0.00000001f
                    && Quaternion.Angle(
                        Quaternion.Euler(previous.localEuler),
                        Quaternion.Euler(current.localEuler)) < 0.001f
                    && Mathf.Abs(current.fieldOfView - previous.fieldOfView) < 0.0001f)
                {
                    if (i > 1 && i % Mathf.Max(2, Mathf.RoundToInt(sampleRate * 0.5f)) == 0)
                        take.QualityIssues.Add(new TakeQualityIssue(i, false, "장시간 동일 포즈"));
                }
            }
        }

        private static string FormatTimecode(float seconds)
        {
            seconds = Mathf.Max(0f, seconds);
            int minutes = Mathf.FloorToInt(seconds / 60f);
            int wholeSeconds = Mathf.FloorToInt(seconds) % 60;
            int frames = Mathf.FloorToInt((seconds - Mathf.Floor(seconds)) * 30f);
            return $"{minutes:00}:{wholeSeconds:00}:{frames:00}";
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
