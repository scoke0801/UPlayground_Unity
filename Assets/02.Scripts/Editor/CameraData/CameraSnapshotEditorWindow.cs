#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UPlayGround.CameraSystem;
using UPlayGround.Manager;

namespace UPlayGround.Data.Editor
{
    public class CameraSnapshotEditorWindow : EditorWindow
    {
        private CameraSnapshotProfile _profile;
        private Camera _previewCamera;
        private Transform _captureActorAnchor;
        private CameraSnapshotSpace _captureSpace = CameraSnapshotSpace.ActorRelative;
        private int _selectedIndex = -1;
        private Vector2 _shotScroll;
        private Vector2 _inspectorScroll;
        private bool _showProfileSettings = true;
        private bool _isSequencePlaying;
        private double _sequenceStartTime;
        private Vector3 _sequenceStartPosition;
        private Quaternion _sequenceStartRotation;
        private float _sequenceStartFov;
        private float _freeCameraMoveSpeed = 6f;
        private float _freeCameraLookSensitivity = 0.12f;

        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/월드/카메라/카메라 스냅샷 에디터", priority = UPlayGround.Tool.Editor.UPlaygroundMenuPriority.WorldCamera)]
        public static void Open()
        {
            GetWindow<CameraSnapshotEditorWindow>("Camera Snapshot 에디터");
        }

        public static void Open(CameraSnapshotProfile profile)
        {
            var window = GetWindow<CameraSnapshotEditorWindow>("Camera Snapshot 에디터");
            window._profile = profile;
            window._selectedIndex = profile != null && profile.shots != null && profile.shots.Count > 0
                ? 0
                : -1;
            window.Show();
            window.Focus();
        }

        private void OnEnable()
        {
            EditorApplication.update += UpdateEditorPreviewPlayback;
        }

        private void OnDisable()
        {
            EditorApplication.update -= UpdateEditorPreviewPlayback;
        }

        public void CreateGUI()
        {
            UPlayGround.EditorTools.UPlaygroundEditorUX.BuildLegacyWindow(
                rootVisualElement, "카메라 스냅샷",
                "카메라 샷 캡처, 순서 편집, 프로필 검증과 시퀀스 미리보기를 한 흐름에서 제공합니다.",
                "d_Camera Icon", DrawLegacyGUI, "up-camera-snapshot");
        }

        private void DrawLegacyGUI()
        {
            DrawToolbar();
            EditorGUILayout.Space(6f);

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawShotList();
                DrawInspector();
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _profile = (CameraSnapshotProfile)EditorGUILayout.ObjectField("프로필", _profile, typeof(CameraSnapshotProfile), false);
                _previewCamera = (Camera)EditorGUILayout.ObjectField("캡처 카메라", _previewCamera, typeof(Camera), true);
                _captureActorAnchor = (Transform)EditorGUILayout.ObjectField("캡처 기준 Transform", _captureActorAnchor, typeof(Transform), true);
                _captureSpace = (CameraSnapshotSpace)EditorGUILayout.EnumPopup("캡처 좌표계", _captureSpace);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("새 프로필 생성", GUILayout.Height(24f)))
                        CreateProfile();

                    using (new EditorGUI.DisabledScope(_profile == null || ResolveCaptureCamera() == null))
                    {
                        if (GUILayout.Button("현재 카메라 스냅샷 추가", GUILayout.Height(24f)))
                            AddShotFromCamera();
                    }

                    using (new EditorGUI.DisabledScope(_profile == null))
                    {
                        if (GUILayout.Button("저장", GUILayout.Height(24f)))
                            SaveProfile();
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    bool canPlay = _profile != null
                                   && _profile.shots != null
                                   && _profile.shots.Count > 0
                                   && ResolveCaptureCamera() != null;

                    using (new EditorGUI.DisabledScope(!canPlay))
                    {
                        if (GUILayout.Button(_isSequencePlaying ? "처음부터 재생" : "시퀀스 재생", GUILayout.Height(24f)))
                            PlaySequence();
                    }

                    using (new EditorGUI.DisabledScope(!_isSequencePlaying && !Application.isPlaying))
                    {
                        if (GUILayout.Button("정지", GUILayout.Width(80f), GUILayout.Height(24f)))
                            StopSequence();
                    }

                    if (_isSequencePlaying && _profile != null)
                    {
                        float elapsed = (float)(EditorApplication.timeSinceStartup - _sequenceStartTime);
                        EditorGUILayout.LabelField($"{elapsed:0.00}s / {_profile.EffectiveTotalDuration:0.00}s", GUILayout.Width(120f));
                    }
                }

                DrawFreeCameraControls();
                DrawProfileSettings();
            }
        }

        private void DrawProfileSettings()
        {
            if (_profile == null) return;

            _showProfileSettings = EditorGUILayout.Foldout(_showProfileSettings, "프로필 설정", true);
            if (!_showProfileSettings)
                return;

            EditorGUI.BeginChangeCheck();
            Undo.RecordObject(_profile, "Edit Camera Snapshot Profile");

            _profile.sequenceName = EditorGUILayout.TextField("시퀀스 이름", _profile.sequenceName);
            _profile.useUnscaledTime = EditorGUILayout.Toggle("Unscaled Time", _profile.useUnscaledTime);
            _profile.restorePreviousModeOnFinish = EditorGUILayout.Toggle("종료 시 이전 모드 복귀", _profile.restorePreviousModeOnFinish);
            _profile.lockCameraInput = EditorGUILayout.Toggle("카메라 입력 잠금", _profile.lockCameraInput);
            _profile.releaseLockOnOnEnter = EditorGUILayout.Toggle("진입 시 락온 해제", _profile.releaseLockOnOnEnter);
            _profile.applyFirstShotImmediately = EditorGUILayout.Toggle("첫 샷 즉시 적용", _profile.applyFirstShotImmediately);
            _profile.useCollision = EditorGUILayout.Toggle("충돌 보정 사용", _profile.useCollision);
            _profile.entryBlendDuration = Mathf.Max(0f, EditorGUILayout.FloatField("진입 블렌드 시간", _profile.entryBlendDuration));
            _profile.entryBlendCurve = EditorGUILayout.CurveField("진입 블렌드 커브", _profile.entryBlendCurve);
            _profile.playbackSpeed = Mathf.Max(0.01f, EditorGUILayout.FloatField("재생 속도", _profile.playbackSpeed));
            _profile.priority = EditorGUILayout.IntField("우선순위", _profile.priority);
            _profile.interruptPolicy = (CameraSnapshotInterruptPolicy)EditorGUILayout.EnumPopup("인터럽트 정책", _profile.interruptPolicy);
            DrawActorReferenceField("액터 기준", ref _profile.actorAnchor);
            DrawActorReferenceField("LookAt 기준", ref _profile.lookAtTarget);

            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(_profile);
        }

        private static void DrawActorReferenceField(string label, ref CameraSnapshotActorReference reference)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                reference.enabled = EditorGUILayout.Toggle("사용", reference.enabled);
                reference.useActivePlayerWhenEmpty = EditorGUILayout.Toggle("비어 있으면 활성 플레이어", reference.useActivePlayerWhenEmpty);
                reference.actorIdType = (UPlayGround.Data.Actor.ActorIdType)EditorGUILayout.EnumPopup("Actor ID", reference.actorIdType);
                using (new EditorGUI.DisabledScope(reference.actorIdType != UPlayGround.Data.Actor.ActorIdType.None))
                {
                    reference.actorId = EditorGUILayout.TextField("Actor ID 문자열", reference.actorId);
                }
                reference.socketType = (UPlayGround.Data.EnumType.ActorSocketType)EditorGUILayout.EnumPopup("Socket", reference.socketType);
            }
        }

        private void DrawFreeCameraControls()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                _freeCameraMoveSpeed = Mathf.Max(0.1f, EditorGUILayout.FloatField("프리카메라 속도", _freeCameraMoveSpeed));
                _freeCameraLookSensitivity = Mathf.Max(0.01f, EditorGUILayout.FloatField("회전 감도", _freeCameraLookSensitivity));

                bool isPlaying = Application.isPlaying;
                bool isFreeCamera = isPlaying
                                    && CameraManager.Instance != null
                                    && CameraManager.Instance.IsFreeCameraActive;

                using (new EditorGUI.DisabledScope(!isPlaying || CameraManager.Instance == null))
                {
                    if (GUILayout.Button(isFreeCamera ? "프리카메라 종료" : "프리카메라 시작", GUILayout.Width(130f), GUILayout.Height(24f)))
                    {
                        if (isFreeCamera)
                            CameraManager.Instance.PopCameraMode();
                        else
                            CameraManager.Instance.PushFreeCamera(_freeCameraMoveSpeed, _freeCameraLookSensitivity);
                    }
                }
            }

            if (Application.isPlaying)
                EditorGUILayout.HelpBox("프리카메라: 우클릭 드래그 회전, WASD 이동, Q/E 하강/상승, Shift 가속, Ctrl 감속, 휠 FOV", MessageType.None);
            else
                EditorGUILayout.HelpBox("프리카메라는 PlayMode에서 Game View 녹화용으로 동작합니다.", MessageType.Info);
        }

        private void DrawShotList()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(320f)))
            {
                EditorGUILayout.LabelField("샷 목록", EditorStyles.boldLabel);

                if (_profile == null)
                {
                    EditorGUILayout.HelpBox("CameraSnapshotProfile을 선택하거나 새로 생성하세요.", MessageType.Info);
                    return;
                }

                _shotScroll = EditorGUILayout.BeginScrollView(_shotScroll, EditorStyles.helpBox);
                for (int i = 0; i < _profile.shots.Count; i++)
                {
                    CameraSnapshotShot shot = _profile.shots[i];
                    string label = $"#{i + 1:00}  {shot.shotName}   {shot.duration:0.##}초";

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Toggle(_selectedIndex == i, label, "Button", GUILayout.Height(28f)))
                            _selectedIndex = i;

                        if (GUILayout.Button("▲", GUILayout.Width(28f)) && i > 0)
                        {
                            Undo.RecordObject(_profile, "Move Camera Snapshot Shot");
                            (_profile.shots[i - 1], _profile.shots[i]) = (_profile.shots[i], _profile.shots[i - 1]);
                            _selectedIndex = i - 1;
                            EditorUtility.SetDirty(_profile);
                        }

                        if (GUILayout.Button("▼", GUILayout.Width(28f)) && i < _profile.shots.Count - 1)
                        {
                            Undo.RecordObject(_profile, "Move Camera Snapshot Shot");
                            (_profile.shots[i + 1], _profile.shots[i]) = (_profile.shots[i], _profile.shots[i + 1]);
                            _selectedIndex = i + 1;
                            EditorUtility.SetDirty(_profile);
                        }
                    }
                }
                EditorGUILayout.EndScrollView();

                EditorGUILayout.LabelField($"총 길이: {_profile.TotalDuration:0.##}초");
            }
        }

        private void DrawInspector()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.LabelField("샷 편집", EditorStyles.boldLabel);

                if (_profile == null || _selectedIndex < 0 || _selectedIndex >= _profile.shots.Count)
                {
                    EditorGUILayout.HelpBox("편집할 샷을 선택하세요.", MessageType.Info);
                    return;
                }

                CameraSnapshotShot shot = _profile.shots[_selectedIndex];
                _inspectorScroll = EditorGUILayout.BeginScrollView(_inspectorScroll, EditorStyles.helpBox);

                Undo.RecordObject(_profile, "Edit Camera Snapshot Shot");
                shot.shotName = EditorGUILayout.TextField("이름", shot.shotName);
                shot.space = (CameraSnapshotSpace)EditorGUILayout.EnumPopup("좌표계", shot.space);
                shot.position = EditorGUILayout.Vector3Field("위치", shot.position);
                shot.rotationEuler = EditorGUILayout.Vector3Field("회전", shot.rotationEuler);
                shot.fieldOfView = EditorGUILayout.Slider("FOV", shot.fieldOfView, 1f, 179f);
                shot.duration = Mathf.Max(0.01f, EditorGUILayout.FloatField("지속 시간", shot.duration));
                shot.blendCurve = EditorGUILayout.CurveField("블렌드 커브", shot.blendCurve);
                shot.moveType = (CameraSnapshotMoveType)EditorGUILayout.EnumPopup("이동 방식", shot.moveType);
                using (new EditorGUI.DisabledScope(shot.moveType != CameraSnapshotMoveType.OrbitAroundAnchor))
                {
                    shot.orbitDirection = (CameraSnapshotOrbitDirection)EditorGUILayout.EnumPopup("공전 방향", shot.orbitDirection);
                    shot.keepLookAtTargetDuringBlend = EditorGUILayout.Toggle("보간 중 중심 바라보기", shot.keepLookAtTargetDuringBlend);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(ResolveCaptureCamera() == null))
                    {
                        if (GUILayout.Button("현재 카메라로 덮어쓰기"))
                        {
                            shot.Capture(ResolveCaptureCamera(), _captureActorAnchor, _captureSpace);
                            EditorUtility.SetDirty(_profile);
                        }

                        if (GUILayout.Button("카메라를 이 위치로 이동"))
                            MoveCameraToShot(shot);
                    }

                    if (GUILayout.Button("삭제", GUILayout.Width(80f)))
                    {
                        _profile.shots.RemoveAt(_selectedIndex);
                        _selectedIndex = Mathf.Clamp(_selectedIndex - 1, -1, _profile.shots.Count - 1);
                        EditorUtility.SetDirty(_profile);
                    }
                }

                if (GUI.changed)
                    EditorUtility.SetDirty(_profile);

                EditorGUILayout.EndScrollView();
            }
        }

        private void CreateProfile()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Camera Snapshot Profile 생성",
                "CameraSnapshotProfile",
                "asset",
                "저장 위치를 선택하세요.",
                "Assets/10.Datas");

            if (string.IsNullOrEmpty(path)) return;

            var profile = CreateInstance<CameraSnapshotProfile>();
            profile.sequenceName = System.IO.Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();
            _profile = profile;
            Selection.activeObject = profile;
        }

        private void AddShotFromCamera()
        {
            Undo.RecordObject(_profile, "Add Camera Snapshot Shot");
            var shot = new CameraSnapshotShot
            {
                shotName = $"Shot {_profile.shots.Count + 1}",
                duration = 1f
            };
            shot.Capture(ResolveCaptureCamera(), _captureActorAnchor, _captureSpace);
            _profile.shots.Add(shot);
            _selectedIndex = _profile.shots.Count - 1;
            EditorUtility.SetDirty(_profile);
        }

        private void MoveCameraToShot(CameraSnapshotShot shot)
        {
            if (shot == null) return;

            Transform actorAnchor = ResolveEditorActorAnchor();
            shot.ResolveWorldPose(actorAnchor, out Vector3 position, out Quaternion rotation);

            if (Application.isPlaying && CameraManager.Instance != null)
            {
                PreviewShotInCameraManager(shot);
                SceneView.RepaintAll();
                return;
            }

            Camera camera = ResolveCaptureCamera();
            if (camera == null) return;

            Undo.RecordObject(camera.transform, "Move Camera To Snapshot");
            Undo.RecordObject(camera, "Set Camera Snapshot FOV");
            camera.transform.SetPositionAndRotation(position, rotation);
            camera.fieldOfView = shot.fieldOfView;

            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                AlignSceneViewToCamera(sceneView, camera);
            }
        }

        private Camera ResolveCaptureCamera()
        {
            if (_previewCamera != null)
                return _previewCamera;

            if (Camera.main != null)
                return Camera.main;

            return SceneView.lastActiveSceneView != null
                ? SceneView.lastActiveSceneView.camera
                : null;
        }

        private void PreviewShotInCameraManager(CameraSnapshotShot shot)
        {
            var runtimeProfile = CreateInstance<CameraSnapshotProfile>();
            runtimeProfile.hideFlags = HideFlags.HideAndDontSave;
            runtimeProfile.sequenceName = "Camera Snapshot Preview";
            runtimeProfile.useUnscaledTime = true;
            runtimeProfile.restorePreviousModeOnFinish = false;
            runtimeProfile.lockCameraInput = true;
            runtimeProfile.releaseLockOnOnEnter = false;
            runtimeProfile.applyFirstShotImmediately = true;
            runtimeProfile.actorAnchor = _profile != null ? _profile.actorAnchor : CameraSnapshotActorReference.ActivePlayer();
            runtimeProfile.lookAtTarget = _profile != null
                ? _profile.lookAtTarget
                : CameraSnapshotActorReference.None();
            runtimeProfile.shots.Add(new CameraSnapshotShot
            {
                shotName = shot.shotName,
                space = shot.space,
                position = shot.position,
                rotationEuler = shot.rotationEuler,
                fieldOfView = shot.fieldOfView,
                duration = 9999f,
                blendCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f)
            });

            CameraManager.Instance.PushCameraSnapshotSequence(runtimeProfile);
        }

        private void PlaySequence()
        {
            if (_profile == null || _profile.shots == null || _profile.shots.Count == 0)
                return;

            if (Application.isPlaying && CameraManager.Instance != null)
            {
                _isSequencePlaying = true;
                _sequenceStartTime = EditorApplication.timeSinceStartup;
                CameraManager.Instance.PushCameraSnapshotSequence(_profile);
                return;
            }

            Camera camera = ResolveCaptureCamera();
            if (camera == null) return;

            _sequenceStartPosition = camera.transform.position;
            _sequenceStartRotation = camera.transform.rotation;
            _sequenceStartFov = camera.fieldOfView;
            _sequenceStartTime = EditorApplication.timeSinceStartup;
            _isSequencePlaying = true;
            UpdateEditorPreviewPlayback();
        }

        private void StopSequence()
        {
            _isSequencePlaying = false;

            if (Application.isPlaying
                && CameraManager.Instance != null
                && CameraManager.Instance.CurrentCameraMode == UPlayGround.CameraSystem.CameraModeType.CameraSnapshotSequence)
            {
                CameraManager.Instance.PopCameraMode();
            }

            Repaint();
        }

        private void UpdateEditorPreviewPlayback()
        {
            if (!_isSequencePlaying || _profile == null)
                return;

            if (Application.isPlaying)
            {
                if (EditorApplication.timeSinceStartup - _sequenceStartTime >= _profile.EffectiveTotalDuration)
                    _isSequencePlaying = false;

                Repaint();
                return;
            }

            Camera camera = ResolveCaptureCamera();
            if (camera == null || _profile.shots == null || _profile.shots.Count == 0)
            {
                _isSequencePlaying = false;
                return;
            }

            float elapsed = (float)(EditorApplication.timeSinceStartup - _sequenceStartTime) * Mathf.Max(0.01f, _profile.playbackSpeed);
            if (!TryEvaluateEditorPose(elapsed, out Vector3 position, out Quaternion rotation, out float fov))
            {
                CameraSnapshotShot lastShot = _profile.shots[_profile.shots.Count - 1];
                Transform actorAnchor = ResolveEditorActorAnchor();
                lastShot.ResolveWorldPose(actorAnchor, out position, out rotation);
                fov = lastShot.fieldOfView;
                _isSequencePlaying = false;
            }

            camera.transform.SetPositionAndRotation(position, rotation);
            camera.fieldOfView = fov;

            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
                AlignSceneViewToCamera(sceneView, camera);

            Repaint();
        }

        private bool TryEvaluateEditorPose(float elapsed, out Vector3 position, out Quaternion rotation, out float fov)
        {
            position = default;
            rotation = Quaternion.identity;
            fov = 60f;

            float accumulated = 0f;
            for (int i = 0; i < _profile.shots.Count; i++)
            {
                CameraSnapshotShot shot = _profile.shots[i];
                float duration = Mathf.Max(0.01f, shot.duration);
                float nextAccumulated = accumulated + duration;
                if (elapsed >= nextAccumulated)
                {
                    accumulated = nextAccumulated;
                    continue;
                }

                ResolveEditorFromPose(i, out Vector3 fromPosition, out Quaternion fromRotation, out float fromFov);
                Transform actorAnchor = ResolveEditorActorAnchor();
                shot.ResolveWorldPose(actorAnchor, out Vector3 toPosition, out Quaternion toRotation);

                float rawT = Mathf.Clamp01((elapsed - accumulated) / duration);
                if (_profile.applyFirstShotImmediately && i == 0)
                    rawT = 1f;

                float t = shot.blendCurve != null
                    ? Mathf.Clamp01(shot.blendCurve.Evaluate(rawT))
                    : rawT;

                position = Vector3.Lerp(fromPosition, toPosition, t);
                rotation = Quaternion.Slerp(fromRotation, toRotation, t);
                fov = Mathf.Lerp(fromFov, shot.fieldOfView, t);
                _selectedIndex = i;
                return true;
            }

            return false;
        }

        private void ResolveEditorFromPose(int shotIndex, out Vector3 position, out Quaternion rotation, out float fov)
        {
            if (shotIndex <= 0)
            {
                if (_profile.applyFirstShotImmediately && _profile.shots.Count > 0)
                {
                    CameraSnapshotShot firstShot = _profile.shots[0];
                    firstShot.ResolveWorldPose(ResolveEditorActorAnchor(), out position, out rotation);
                    fov = firstShot.fieldOfView;
                    return;
                }

                position = _sequenceStartPosition;
                rotation = _sequenceStartRotation;
                fov = _sequenceStartFov;
                return;
            }

            CameraSnapshotShot previousShot = _profile.shots[shotIndex - 1];
            previousShot.ResolveWorldPose(ResolveEditorActorAnchor(), out position, out rotation);
            fov = previousShot.fieldOfView;
        }

        private Transform ResolveEditorActorAnchor()
        {
            if (_captureActorAnchor != null)
                return _captureActorAnchor;

            return _profile != null
                ? CameraSnapshotActorReferenceResolver.Resolve(_profile.actorAnchor)
                : null;
        }

        private static void AlignSceneViewToCamera(SceneView sceneView, Camera camera)
        {
            if (sceneView == null || camera == null) return;

            sceneView.LookAtDirect(camera.transform.position, camera.transform.rotation, sceneView.size);
            sceneView.cameraSettings.fieldOfView = camera.fieldOfView;
            sceneView.Repaint();
        }

        private void SaveProfile()
        {
            EditorUtility.SetDirty(_profile);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
