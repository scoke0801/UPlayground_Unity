using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.EditorTools;

namespace UPlayGround.Tool.Editor.Build
{
    /// <summary>프로필 편집과 출력 폴더별 재빌드 판단을 빌드 실행 전에 보여 준다.</summary>
    public sealed class BuildManagerWindow : EditorWindow
    {
        private const string ProfileFolder = "Assets/10.Datas/Build";
        private const string ProfilePrefKey = "UPlayGround.BuildManager.ProfileGuid";
        private static readonly string[] s_modeLabels = { "전체 (번들 + exe)", "번들만 (대상 폴더 교체)", "exe만 (기존 콘텐츠 사용)" };

        [SerializeField] private BuildManagerProfileSO _profile;
        private SerializedObject _serializedProfile;
        private ReorderableList _scenes;
        private BuildManagerMode _mode;
        private bool _allowStaleContent;
        private bool _excludeTestScenes = true;
        private bool _showGroups;
        private bool _needsStatus = true;
        private double _refreshAfter;
        private BuildAssessment _assessment;
        private List<BuildIssue> _issues = new();
        private string _statusError;
        private string _checkedAt;
        private string _lastResult;
        private Vector2 _scroll;

        [UPlaygroundTool("UPlayGround/빌드/빌드 매니저")]
        public static void Open()
        {
            var window = GetWindow<BuildManagerWindow>("빌드 매니저");
            window.minSize = new Vector2(620f, 600f);
            window.Show();
        }

        private void OnEnable()
        {
            if (_profile == null)
                _profile = AssetDatabase.LoadAssetAtPath<BuildManagerProfileSO>(
                    AssetDatabase.GUIDToAssetPath(EditorPrefs.GetString(ProfilePrefKey, string.Empty)));
            Undo.undoRedoPerformed += OnUndoRedo;
            InvalidateStatus();
        }

        private void OnDisable() => Undo.undoRedoPerformed -= OnUndoRedo;
        private void OnFocus() => InvalidateStatus();
        private void OnProjectChange() => InvalidateStatus();

        private void OnUndoRedo()
        {
            if (_profile != null) AssetDatabase.SaveAssetIfDirty(_profile);
            InvalidateStatus();
            Repaint();
        }

        private void InvalidateStatus()
        {
            _needsStatus = true;
            _refreshAfter = EditorApplication.timeSinceStartup + 0.75;
        }

        private void OnInspectorUpdate()
        {
            if (_profile == null || !_needsStatus || EditorApplication.timeSinceStartup < _refreshAfter
                || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            RefreshStatus();
            Repaint();
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawProfileSelection();
            if (_profile != null)
            {
                using (new EditorGUI.DisabledScope(EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode))
                {
                    DrawProfileSettings();
                    DrawStatus();
                    EditorGUI.BeginChangeCheck();
                    _mode = (BuildManagerMode)GUILayout.SelectionGrid((int)_mode, s_modeLabels, 1);
                    if (_mode == BuildManagerMode.PlayerOnly)
                        _allowStaleContent = EditorGUILayout.ToggleLeft("검사 불일치 콘텐츠 재사용 (고급)", _allowStaleContent);
                    if (EditorGUI.EndChangeCheck()) InvalidateStatus();
                    if (!_needsStatus)
                        foreach (BuildIssue issue in _issues)
                            EditorGUILayout.HelpBox(issue.Message, issue.Severity == BuildIssueSeverity.Error ? MessageType.Error : MessageType.Warning);
                    DrawRunButton();
                }
                DrawGroups();
            }
            if (!string.IsNullOrEmpty(_lastResult)) EditorGUILayout.HelpBox(_lastResult, MessageType.None);
            EditorGUILayout.EndScrollView();
        }

        private void DrawProfileSelection()
        {
            EditorGUI.BeginChangeCheck();
            var selected = (BuildManagerProfileSO)EditorGUILayout.ObjectField("빌드 프로필", _profile, typeof(BuildManagerProfileSO), false);
            if (EditorGUI.EndChangeCheck()) SelectProfile(selected);
            if (_profile != null) return;
            EditorGUILayout.HelpBox("기본 프로필은 Build Settings의 활성 씬에서 /Test/ 씬을 제외해 만듭니다.", MessageType.Info);
            if (GUILayout.Button("기본 프로필 만들기")) SelectProfile(CreateDefaultProfile());
        }

        private void SelectProfile(BuildManagerProfileSO profile)
        {
            _profile = profile;
            _serializedProfile = null;
            _assessment = null;
            _issues.Clear();
            EditorPrefs.SetString(ProfilePrefKey, profile == null ? string.Empty : AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(profile)));
            InvalidateStatus();
        }

        private void EnsureSerializedProfile()
        {
            if (_serializedProfile != null && _serializedProfile.targetObject == _profile) return;
            _serializedProfile = new SerializedObject(_profile);
            _scenes = new ReorderableList(_serializedProfile, _serializedProfile.FindProperty("scenes"), true, true, true, true);
            _scenes.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "빌드 대상 씬 (첫 항목이 시작 씬)");
            _scenes.drawElementCallback = (rect, index, active, focused) =>
            {
                rect.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.PropertyField(rect, _scenes.serializedProperty.GetArrayElementAtIndex(index), new GUIContent(index.ToString()));
            };
        }

        private void DrawProfileSettings()
        {
            EnsureSerializedProfile();
            _serializedProfile.Update();
            EditorGUILayout.LabelField("빌드 설정", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_serializedProfile.FindProperty("profileName"), new GUIContent("프로필 이름"));
            var output = _serializedProfile.FindProperty("outputDirectory");
            string resolved;
            try { resolved = _profile.GetOutputDirectory(); }
            catch { resolved = output.stringValue; }
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                string edited = EditorGUILayout.TextField("빌드 폴더", string.IsNullOrWhiteSpace(output.stringValue) ? resolved : output.stringValue);
                if (EditorGUI.EndChangeCheck()) output.stringValue = edited;
                if (GUILayout.Button("폴더 선택", GUILayout.Width(80f)))
                {
                    string chosen = EditorUtility.OpenFolderPanel("빌드 폴더 선택", Directory.Exists(resolved) ? resolved : Application.dataPath, string.Empty);
                    if (!string.IsNullOrEmpty(chosen)) output.stringValue = chosen;
                }
            }
            EditorGUILayout.PropertyField(_serializedProfile.FindProperty("executableName"), new GUIContent("exe 이름 (확장자 제외)"));
            EditorGUILayout.PropertyField(_serializedProfile.FindProperty("buildTarget"), new GUIContent("대상 플랫폼"));
            EditorGUILayout.PropertyField(_serializedProfile.FindProperty("developmentBuild"), new GUIContent("Development 빌드"));
            _scenes.DoLayoutList();
            _excludeTestScenes = EditorGUILayout.ToggleLeft("Build Settings 가져올 때 /Test/ 씬 제외", _excludeTestScenes);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Build Settings 활성 씬 가져오기"))
                {
                    _scenes.serializedProperty.ClearArray();
                    foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
                        if (scene.enabled && (!_excludeTestScenes || !scene.path.Contains("/Test/")))
                            AddScene(scene.path);
                }
                if (GUILayout.Button("열린 씬 추가"))
                    for (int i = 0; i < SceneManager.sceneCount; i++) AddScene(SceneManager.GetSceneAt(i).path);
            }
            if (_serializedProfile.ApplyModifiedProperties())
            {
                AssetDatabase.SaveAssetIfDirty(_profile);
                InvalidateStatus();
            }
            EditorGUILayout.HelpBox("프로필에 자동 저장됩니다. 씬을 드래그해 순서를 바꾸세요. Ctrl+Z로 되돌릴 수 있습니다.", MessageType.None);
            try { EditorGUILayout.SelectableLabel("실행 파일: " + BuildManagerFiles.GetExecutablePath(_profile.GetOutputDirectory(), _profile.executableName), EditorStyles.wordWrappedLabel, GUILayout.Height(34f)); }
            catch (Exception exception) { EditorGUILayout.HelpBox(exception.Message, MessageType.Error); }
        }

        private void AddScene(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            if (scene == null) return;
            SerializedProperty list = _scenes.serializedProperty;
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == scene) return;
            int index = list.arraySize++;
            list.GetArrayElementAtIndex(index).objectReferenceValue = scene;
        }

        private void RefreshStatus()
        {
            _needsStatus = false;
            _statusError = null;
            try
            {
                BuildPreflightInput input = BuildManagerRunner.CreateInput(_profile, _mode, _allowStaleContent);
                _assessment = BuildPreflight.Assess(input);
                _issues = BuildPreflight.Evaluate(input);
                _checkedAt = DateTime.Now.ToString("HH:mm:ss");
            }
            catch (Exception exception)
            {
                _assessment = null;
                _issues.Clear();
                _statusError = "상태 확인 실패: " + exception.Message;
            }
        }

        private void DrawStatus()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("선택한 빌드 폴더 상태", EditorStyles.boldLabel);
            if (GUILayout.Button("상태 새로고침 / 사전 점검")) RefreshStatus();
            if (_needsStatus) EditorGUILayout.HelpBox("설정이 변경되어 상태를 다시 확인합니다.", MessageType.Info);
            if (!string.IsNullOrEmpty(_statusError)) EditorGUILayout.HelpBox(_statusError, MessageType.Error);
            if (_assessment == null || _needsStatus) return;
            EditorGUILayout.LabelField("exe", _assessment.PlayerNeedsBuild ? "다시 빌드 필요" : "기록과 일치");
            EditorGUILayout.LabelField("번들", _assessment.ContentNeedsBuild ? "빌드·반영 필요" : "기록과 일치");
            string recommendation = _assessment.RecommendedMode.HasValue ? s_modeLabels[(int)_assessment.RecommendedMode.Value] : "추가 빌드 필요 없음 (기록 기준)";
            EditorGUILayout.HelpBox("권장: " + recommendation + "\n확인 시각: " + _checkedAt, MessageType.Info);
            foreach (string reason in _assessment.Reasons) EditorGUILayout.HelpBox(reason, MessageType.None);
            if (_assessment.RecommendedMode.HasValue && GUILayout.Button("권장 모드 선택"))
            {
                _mode = _assessment.RecommendedMode.Value;
                _allowStaleContent = false;
                RefreshStatus();
            }
            EditorGUILayout.HelpBox("저장된 설정·컴파일 결과·에셋 의존성·카탈로그 기준입니다. 실행 성공이나 실제 번들 파일 전체 무결성을 보장하지 않습니다.", MessageType.None);
        }

        private void DrawGroups()
        {
            _showGroups = EditorGUILayout.Foldout(_showGroups, "Addressables 그룹 (읽기 전용)");
            if (!_showGroups) return;
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;
            foreach (var group in settings.groups)
            {
                var schema = group != null ? group.GetSchema<BundledAssetGroupSchema>() : null;
                if (schema == null) continue;
                EditorGUILayout.LabelField(group.Name + " · 항목 " + group.entries.Count + " · " + schema.BundleMode);
            }
        }

        private void DrawRunButton()
        {
            using (new EditorGUI.DisabledScope(_needsStatus || _assessment == null || BuildPreflight.HasError(_issues)))
            {
                if (!GUILayout.Button("선택한 폴더에 " + s_modeLabels[(int)_mode] + " 실행", GUILayout.Height(32f))) return;
            }
            if (!EditorUtility.DisplayDialog("빌드 실행", s_modeLabels[(int)_mode] + "\n" + _profile.GetOutputDirectory() + "\n기존 파일을 갱신합니다.", "실행", "취소")) return;
            BuildManagerResult result = BuildManagerRunner.Run(_profile, _mode, _allowStaleContent);
            _lastResult = (result.Success ? "성공: " : "실패: ") + result.Summary;
            InvalidateStatus();
        }

        private static BuildManagerProfileSO CreateDefaultProfile()
        {
            Directory.CreateDirectory(ProfileFolder);
            var profile = CreateInstance<BuildManagerProfileSO>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
                if (scene.enabled && !scene.path.Contains("/Test/"))
                    profile.scenes.Add(AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path));
            AssetDatabase.CreateAsset(profile, AssetDatabase.GenerateUniqueAssetPath(ProfileFolder + "/BuildProfile_Release.asset"));
            AssetDatabase.SaveAssetIfDirty(profile);
            return profile;
        }
    }
}
