#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Tool.Editor.Animation
{
    /// <summary>
    /// 선택한 폴더 하위의 FBX/모델 에셋에서 루트 모션 클립만 찾아 Root Transform 임포트 옵션을 일괄 변경한다.
    /// </summary>
    public sealed class RootMotionImportSettingsBatchTool : EditorWindow
    {
        private const string DefaultFolder = "Assets";
        private const string MenuPath = "UPlayGround/유틸/Root Motion 임포트 설정 일괄 변경";
        private const string AssetMenuPath = "Assets/UPlayGround/Root Motion Import Settings Batch";

        private string _targetFolder = DefaultFolder;
        private bool _onlyRootMotionClip = true;
        private bool _applyMirror = false;
        private bool _mirror = false;
        private Vector2 _scroll;
        private string _statusMessage = "";
        private readonly List<ScanRow> _rows = new();

        private RootTransformPreset _preset = RootTransformPreset.ImageOptions;
        private RootTransformSettings _settings = RootTransformSettings.CreateImageOptions();

        private GUIStyle _headerStyle;

        private enum RootTransformPreset
        {
            ImageOptions,
            InPlace
        }

        private sealed class ScanRow
        {
            public string ModelPath;
            public string ClipName;
            public bool HasRootMotion;
            public bool WillChange;
        }

        private struct RootTransformSettings
        {
            public bool LockRootRotation;
            public bool KeepOriginalOrientation;
            public float RotationOffset;

            public bool LockRootHeightY;
            public bool KeepOriginalPositionY;
            public bool HeightFromFeet;
            public float HeightOffset;

            public bool LockRootPositionXZ;
            public bool KeepOriginalPositionXZ;

            public static RootTransformSettings CreateImageOptions()
            {
                return new RootTransformSettings
                {
                    LockRootRotation = false,
                    KeepOriginalOrientation = false,
                    RotationOffset = 0f,
                    LockRootHeightY = false,
                    KeepOriginalPositionY = true,
                    HeightFromFeet = false,
                    HeightOffset = 0f,
                    LockRootPositionXZ = false,
                    KeepOriginalPositionXZ = false
                };
            }

            public static RootTransformSettings CreateInPlace()
            {
                return new RootTransformSettings
                {
                    LockRootRotation = true,
                    KeepOriginalOrientation = false,
                    RotationOffset = 0f,
                    LockRootHeightY = true,
                    KeepOriginalPositionY = false,
                    HeightFromFeet = false,
                    HeightOffset = 0f,
                    LockRootPositionXZ = true,
                    KeepOriginalPositionXZ = false
                };
            }
        }

        [UPlayGround.EditorTools.UPlaygroundTool(MenuPath)]
        public static void Open()
        {
            var window = GetWindow<RootMotionImportSettingsBatchTool>("Root Motion Import Batch");
            window.minSize = new Vector2(780f, 520f);
            window.ApplySelectionFolderIfPossible();
            window.Show();
        }

        [MenuItem(AssetMenuPath)]
        private static void OpenFromAssetMenu() => Open();

        [MenuItem(AssetMenuPath, true)]
        private static bool ValidateOpenFromAssetMenu()
        {
            return Selection.assetGUIDs
                .Select(AssetDatabase.GUIDToAssetPath)
                .Any(AssetDatabase.IsValidFolder);
        }

        private void OnGUI()
        {
            InitStyles();

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Root Motion Import Settings Batch", _headerStyle);
            EditorGUILayout.LabelField(
                "선택 폴더 하위의 모델 에셋을 재귀 검색하고, 루트 모션이 있는 AnimationClip의 Root Transform 옵션을 일괄 변경합니다.",
                EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(6f);

            DrawTargetFolder();
            EditorGUILayout.Space(4f);
            DrawSettings();
            EditorGUILayout.Space(6f);
            DrawActionButtons();
            EditorGUILayout.Space(6f);
            DrawResultTable();

            if (!string.IsNullOrEmpty(_statusMessage))
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.HelpBox(_statusMessage, MessageType.Info);
            }
        }

        private void InitStyles()
        {
            _headerStyle ??= new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleLeft
            };
        }

        private void DrawTargetFolder()
        {
            using var box = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);
            EditorGUILayout.LabelField("대상", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                _targetFolder = EditorGUILayout.TextField("검색 폴더", _targetFolder);

                if (GUILayout.Button("선택 폴더", GUILayout.Width(80f)))
                    ApplySelectionFolderIfPossible();

                if (GUILayout.Button("...", GUILayout.Width(28f)))
                {
                    string picked = EditorUtility.OpenFolderPanel("검색 폴더 선택", ToAbsoluteFolderPath(_targetFolder), "");
                    if (!string.IsNullOrEmpty(picked))
                        _targetFolder = ToProjectRelativePath(picked);
                }
            }

            _onlyRootMotionClip = EditorGUILayout.Toggle(
                new GUIContent("루트 모션 클립만 변경", "AnimationClip.hasRootCurves 또는 hasMotionCurves가 true인 클립만 변경합니다."),
                _onlyRootMotionClip);
        }

        private void DrawSettings()
        {
            using var box = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Root Transform 설정", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _preset = (RootTransformPreset)EditorGUILayout.EnumPopup("프리셋", _preset);
            if (EditorGUI.EndChangeCheck())
            {
                _settings = _preset == RootTransformPreset.ImageOptions
                    ? RootTransformSettings.CreateImageOptions()
                    : RootTransformSettings.CreateInPlace();
            }

            EditorGUILayout.Space(3f);
            EditorGUILayout.LabelField("Root Transform Rotation", EditorStyles.boldLabel);
            _settings.LockRootRotation = EditorGUILayout.Toggle("Bake Into Pose", _settings.LockRootRotation);
            _settings.KeepOriginalOrientation = EditorGUILayout.Toggle(
                new GUIContent("Based Upon: Original", "꺼져 있으면 Body Orientation으로 설정됩니다."),
                _settings.KeepOriginalOrientation);
            _settings.RotationOffset = EditorGUILayout.FloatField("Offset", _settings.RotationOffset);

            EditorGUILayout.Space(3f);
            EditorGUILayout.LabelField("Root Transform Position (Y)", EditorStyles.boldLabel);
            _settings.LockRootHeightY = EditorGUILayout.Toggle("Bake Into Pose", _settings.LockRootHeightY);
            _settings.KeepOriginalPositionY = EditorGUILayout.Toggle(
                new GUIContent("Based Upon: Original", "꺼져 있으면 Center of Mass/Feet 설정을 사용합니다."),
                _settings.KeepOriginalPositionY);
            using (new EditorGUI.DisabledScope(_settings.KeepOriginalPositionY))
                _settings.HeightFromFeet = EditorGUILayout.Toggle("Based Upon: Feet", _settings.HeightFromFeet);
            _settings.HeightOffset = EditorGUILayout.FloatField("Offset", _settings.HeightOffset);

            EditorGUILayout.Space(3f);
            EditorGUILayout.LabelField("Root Transform Position (XZ)", EditorStyles.boldLabel);
            _settings.LockRootPositionXZ = EditorGUILayout.Toggle("Bake Into Pose", _settings.LockRootPositionXZ);
            _settings.KeepOriginalPositionXZ = EditorGUILayout.Toggle(
                new GUIContent("Based Upon: Original", "꺼져 있으면 Center of Mass로 설정됩니다."),
                _settings.KeepOriginalPositionXZ);

            EditorGUILayout.Space(3f);
            _applyMirror = EditorGUILayout.Toggle("Mirror도 변경", _applyMirror);
            using (new EditorGUI.DisabledScope(!_applyMirror))
                _mirror = EditorGUILayout.Toggle("Mirror", _mirror);
        }

        private void DrawActionButtons()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("스캔", GUILayout.Height(28f)))
                    Scan();

                using (new EditorGUI.DisabledScope(_rows.All(row => !row.WillChange)))
                {
                    if (GUILayout.Button("일괄 적용", GUILayout.Height(28f)))
                        Apply();
                }
            }
        }

        private void DrawResultTable()
        {
            if (_rows.Count == 0)
                return;

            EditorGUILayout.LabelField($"검색 결과: {_rows.Count}개 클립", EditorStyles.boldLabel);

            using var scroll = new EditorGUILayout.ScrollViewScope(_scroll, EditorStyles.helpBox);
            _scroll = scroll.scrollPosition;

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("적용", EditorStyles.boldLabel, GUILayout.Width(40f));
                GUILayout.Label("루트 모션", EditorStyles.boldLabel, GUILayout.Width(70f));
                GUILayout.Label("클립", EditorStyles.boldLabel, GUILayout.Width(180f));
                GUILayout.Label("모델 경로", EditorStyles.boldLabel);
            }

            foreach (var row in _rows)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.Toggle(row.WillChange, GUILayout.Width(40f));
                    EditorGUILayout.Toggle(row.HasRootMotion, GUILayout.Width(70f));
                    EditorGUILayout.LabelField(row.ClipName, GUILayout.Width(180f));
                    EditorGUILayout.LabelField(row.ModelPath);
                }
            }
        }

        private void Scan()
        {
            _rows.Clear();

            if (!ValidateTargetFolder())
                return;

            foreach (string modelPath in FindModelPaths(_targetFolder))
            {
                foreach (var clipInfo in GetClipInfos(modelPath))
                {
                    bool willChange = !_onlyRootMotionClip || clipInfo.hasRootMotion;
                    _rows.Add(new ScanRow
                    {
                        ModelPath = modelPath,
                        ClipName = clipInfo.name,
                        HasRootMotion = clipInfo.hasRootMotion,
                        WillChange = willChange
                    });
                }
            }

            int modelCount = _rows.Select(row => row.ModelPath).Distinct().Count();
            int changeCount = _rows.Count(row => row.WillChange);
            _statusMessage = $"{modelCount}개 모델에서 {_rows.Count}개 클립을 찾았습니다. 적용 대상: {changeCount}개.";
        }

        private void Apply()
        {
            if (!ValidateTargetFolder())
                return;

            int changedModelCount = 0;
            int changedClipCount = 0;

            try
            {
                AssetDatabase.StartAssetEditing();

                foreach (string modelPath in FindModelPaths(_targetFolder))
                {
                    var importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
                    if (importer == null)
                        continue;

                    var rootMotionClipNames = GetClipInfos(modelPath)
                        .Where(info => info.hasRootMotion)
                        .Select(info => info.name)
                        .ToHashSet();

                    ModelImporterClipAnimation[] clipAnimations = importer.clipAnimations;
                    if (clipAnimations == null || clipAnimations.Length == 0)
                        clipAnimations = importer.defaultClipAnimations;

                    bool changed = false;
                    for (int i = 0; i < clipAnimations.Length; i++)
                    {
                        var clip = clipAnimations[i];
                        bool hasRootMotion = rootMotionClipNames.Contains(clip.name) || rootMotionClipNames.Contains(clip.takeName);
                        if (_onlyRootMotionClip && !hasRootMotion)
                            continue;

                        ApplySettings(ref clip);
                        clipAnimations[i] = clip;
                        changed = true;
                        changedClipCount++;
                    }

                    if (!changed)
                        continue;

                    importer.clipAnimations = clipAnimations;
                    importer.SaveAndReimport();
                    changedModelCount++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }

            Scan();
            _statusMessage = $"{changedModelCount}개 모델 / {changedClipCount}개 클립의 Root Transform 임포트 설정을 변경했습니다.";
        }

        private void ApplySettings(ref ModelImporterClipAnimation clip)
        {
            clip.lockRootRotation = _settings.LockRootRotation;
            clip.keepOriginalOrientation = _settings.KeepOriginalOrientation;
            clip.rotationOffset = _settings.RotationOffset;

            clip.lockRootHeightY = _settings.LockRootHeightY;
            clip.keepOriginalPositionY = _settings.KeepOriginalPositionY;
            clip.heightFromFeet = _settings.HeightFromFeet;
            clip.heightOffset = _settings.HeightOffset;

            clip.lockRootPositionXZ = _settings.LockRootPositionXZ;
            clip.keepOriginalPositionXZ = _settings.KeepOriginalPositionXZ;

            if (_applyMirror)
                clip.mirror = _mirror;
        }

        private bool ValidateTargetFolder()
        {
            if (string.IsNullOrWhiteSpace(_targetFolder) || !AssetDatabase.IsValidFolder(_targetFolder))
            {
                _statusMessage = "유효한 Assets 하위 폴더를 지정해야 합니다.";
                return false;
            }

            return true;
        }

        private static IEnumerable<string> FindModelPaths(string folder)
        {
            return AssetDatabase.FindAssets("", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => AssetImporter.GetAtPath(path) is ModelImporter)
                .Distinct()
                .OrderBy(path => path);
        }

        private static IEnumerable<(string name, bool hasRootMotion)> GetClipInfos(string modelPath)
        {
            return AssetDatabase.LoadAllAssetsAtPath(modelPath)
                .OfType<AnimationClip>()
                .Where(clip => clip != null && !clip.name.StartsWith("__preview__"))
                .Select(clip => (clip.name, clip.hasRootCurves || clip.hasMotionCurves))
                .OrderBy(info => info.name);
        }

        private void ApplySelectionFolderIfPossible()
        {
            string selectedFolder = Selection.assetGUIDs
                .Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(AssetDatabase.IsValidFolder);

            if (!string.IsNullOrEmpty(selectedFolder))
                _targetFolder = selectedFolder;
        }

        private static string ToAbsoluteFolderPath(string projectRelativePath)
        {
            if (string.IsNullOrWhiteSpace(projectRelativePath))
                return Application.dataPath;

            if (Path.IsPathRooted(projectRelativePath))
                return projectRelativePath;

            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            return string.IsNullOrEmpty(projectRoot)
                ? Application.dataPath
                : Path.GetFullPath(Path.Combine(projectRoot, projectRelativePath));
        }

        private static string ToProjectRelativePath(string absolutePath)
        {
            absolutePath = absolutePath.Replace('\\', '/');
            string dataPath = Application.dataPath.Replace('\\', '/');
            if (absolutePath.StartsWith(dataPath))
                return "Assets" + absolutePath[dataPath.Length..];

            return absolutePath;
        }
    }
}
#endif
