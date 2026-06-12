#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
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

        [MenuItem("UPlayGround/월드/카메라/대화 카메라 녹화", priority = UPlayGround.Tool.Editor.UPlaygroundMenuPriority.WorldCamera)]
        public static void Open()
        {
            GetWindow<DialogueCameraRecorderWindow>("대화 카메라 녹화");
        }

        private void OnEnable()
        {
            EditorApplication.update += Repaint;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Repaint;

            // 창이 PlayMode 중 닫히면 런타임 생성한 레코더 GameObject가 씬에 남는다 → 정리.
            if (_recorder != null)
            {
                DestroyImmediate(_recorder.gameObject);
                _recorder = null;
            }
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
            DrawRecordControls();

            EditorGUILayout.Space(6f);
            DrawSmoothingControls();

            EditorGUILayout.Space(6f);
            DrawPreviewControls();
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
                    using (new EditorGUI.DisabledScope(!isPlaying || isRecording))
                    {
                        if (GUILayout.Button("● 녹화 시작", GUILayout.Height(28f)))
                            StartRecording();
                    }

                    using (new EditorGUI.DisabledScope(!isRecording))
                    {
                        if (GUILayout.Button("■ 정지", GUILayout.Height(28f)))
                            StopRecording();
                    }
                }

                if (isRecording)
                {
                    EditorGUILayout.LabelField($"녹화 중… 샘플 {_recorder.SampleCount}개 / {_recorder.RecordedDuration:0.00}s");
                }
                else if (_pendingSamples != null)
                {
                    EditorGUILayout.LabelField($"대기 중인 녹화: 샘플 {_pendingSamples.Count}개 / {(_pendingSamples.Count > 1 ? (_pendingSamples.Count - 1) / Mathf.Max(1f, _pendingSampleRate) : 0f):0.00}s");

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

        private void StartRecording()
        {
            Camera camera = ResolveCaptureCamera();
            if (camera == null)
            {
                EditorUtility.DisplayDialog("녹화 불가", "캡처 카메라를 찾을 수 없습니다.", "확인");
                return;
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
        }

        private void StopRecording()
        {
            if (_recorder == null)
                return;

            IReadOnlyList<DialogueCameraRecordingSO.Sample> samples = _recorder.EndRecording();
            _pendingSamples = new List<DialogueCameraRecordingSO.Sample>(samples);
            _pendingSampleRate = _recorder.SampleRate;
            _pendingSpace = _recorder.Space;
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
