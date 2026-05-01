using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Animation.Editor
{
    public class AnimationBindingRemapTestWindow : EditorWindow
    {
        private const string DefaultOutputFolder = "Assets/07.Animations/Remapped";

        [Serializable]
        private class RemapEntry
        {
            public bool enabled = true;
            public string sourcePath;
            public string targetPath;
        }

        private Transform _armatureRoot;
        private DefaultAsset _outputFolder;
        private string _outputFolderPath = DefaultOutputFolder;
        private bool _includeArmatureName = true;
        private bool _onlyUnmappedExtraBones = true;
        private Vector2 _clipScroll;
        private Vector2 _bindingScroll;

        private readonly List<AnimationClip> _clips = new();
        private readonly List<RemapEntry> _entries = new();
        private readonly List<string> _targetPaths = new();
        private readonly Dictionary<string, List<string>> _targetPathsByLeafName = new();

        [MenuItem("UPlayGround/Util/Animation Binding Remap Test")]
        private static void Open()
        {
            GetWindow<AnimationBindingRemapTestWindow>("Binding Remap Test");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox(
                "Animancer Pro의 Remap Animation Bindings와 같은 방식으로 AnimationClip의 curve binding path를 복제 클립에 재매핑하는 검증용 도구입니다.",
                MessageType.Info);

            DrawTargetSection();
            DrawClipSection();
            DrawMappingSection();
            DrawActionSection();
        }

        private void DrawTargetSection()
        {
            EditorGUILayout.LabelField("대상 본 구조", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _armatureRoot = (Transform)EditorGUILayout.ObjectField("Armature Root", _armatureRoot, typeof(Transform), true);
            _includeArmatureName = EditorGUILayout.ToggleLeft("Animator 기준 경로에 Armature 이름 포함", _includeArmatureName);
            _onlyUnmappedExtraBones = EditorGUILayout.ToggleLeft("path가 있는 Transform binding만 수집", _onlyUnmappedExtraBones);
            if (EditorGUI.EndChangeCheck())
            {
                CollectTargetPaths();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("선택 오브젝트를 Armature로 사용", GUILayout.Height(24f)))
                {
                    if (Selection.activeTransform != null)
                    {
                        _armatureRoot = Selection.activeTransform;
                        CollectTargetPaths();
                    }
                }

                if (GUILayout.Button("대상 본 경로 갱신", GUILayout.Height(24f)))
                {
                    CollectTargetPaths();
                }
            }

            EditorGUILayout.LabelField("수집된 대상 경로", _targetPaths.Count.ToString());
            EditorGUILayout.LabelField("수집된 대상 이름", _targetPathsByLeafName.Count.ToString());
            EditorGUILayout.Space(8f);
        }

        private void DrawClipSection()
        {
            EditorGUILayout.LabelField("소스 AnimationClip", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Project 선택 클립 추가", GUILayout.Height(24f)))
                {
                    AddSelectedClips();
                }

                if (GUILayout.Button("목록 비우기", GUILayout.Height(24f)))
                {
                    _clips.Clear();
                    _entries.Clear();
                }
            }

            _clipScroll = EditorGUILayout.BeginScrollView(_clipScroll, GUILayout.MinHeight(70f), GUILayout.MaxHeight(120f));
            for (int i = 0; i < _clips.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    _clips[i] = (AnimationClip)EditorGUILayout.ObjectField(_clips[i], typeof(AnimationClip), false);
                    if (GUILayout.Button("X", GUILayout.Width(24f)))
                    {
                        _clips.RemoveAt(i);
                        i--;
                    }
                }
            }
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("클립 Binding 수집 / 매핑표 생성", GUILayout.Height(26f)))
            {
                CollectSourceBindings();
                AutoMapByLeafName();
            }

            EditorGUILayout.Space(8f);
        }

        private void DrawMappingSection()
        {
            EditorGUILayout.LabelField("매핑표", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("이름 기준 자동 매핑", GUILayout.Height(24f)))
                {
                    AutoMapByLeafName();
                }

                if (GUILayout.Button("매핑된 항목만 활성화", GUILayout.Height(24f)))
                {
                    foreach (var entry in _entries)
                    {
                        entry.enabled = !string.IsNullOrEmpty(entry.targetPath);
                    }
                }
            }

            _bindingScroll = EditorGUILayout.BeginScrollView(_bindingScroll, GUILayout.MinHeight(220f));
            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    entry.enabled = EditorGUILayout.ToggleLeft(entry.sourcePath, entry.enabled);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Target", GUILayout.Width(48f));
                        entry.targetPath = EditorGUILayout.TextField(entry.targetPath);
                        if (GUILayout.Button("...", GUILayout.Width(28f)))
                        {
                            ShowTargetPathMenu(entry);
                        }
                    }
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(8f);
        }

        private void DrawActionSection()
        {
            EditorGUILayout.LabelField("출력", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _outputFolder = (DefaultAsset)EditorGUILayout.ObjectField("Output Folder", _outputFolder, typeof(DefaultAsset), false);
            if (EditorGUI.EndChangeCheck())
            {
                var path = AssetDatabase.GetAssetPath(_outputFolder);
                if (AssetDatabase.IsValidFolder(path))
                {
                    _outputFolderPath = path;
                }
            }

            _outputFolderPath = EditorGUILayout.TextField("Output Path", _outputFolderPath);

            using (new EditorGUI.DisabledScope(_clips.Count == 0 || _entries.Count == 0))
            {
                if (GUILayout.Button("Remap 클립 생성", GUILayout.Height(32f)))
                {
                    RemapClips();
                }
            }
        }

        private void CollectTargetPaths()
        {
            _targetPaths.Clear();
            _targetPathsByLeafName.Clear();

            if (_armatureRoot == null)
            {
                return;
            }

            foreach (var target in _armatureRoot.GetComponentsInChildren<Transform>(true))
            {
                var path = GetTargetPath(target);
                _targetPaths.Add(path);

                var leafName = NormalizeName(target.name);
                if (!_targetPathsByLeafName.TryGetValue(leafName, out var paths))
                {
                    paths = new List<string>();
                    _targetPathsByLeafName.Add(leafName, paths);
                }

                paths.Add(path);
            }

            _targetPaths.Sort(StringComparer.Ordinal);
        }

        private void AddSelectedClips()
        {
            foreach (var clip in Selection.objects.OfType<AnimationClip>())
            {
                if (!_clips.Contains(clip))
                {
                    _clips.Add(clip);
                }
            }
        }

        private void CollectSourceBindings()
        {
            var sourcePaths = new SortedSet<string>(StringComparer.Ordinal);

            foreach (var clip in _clips)
            {
                if (clip == null)
                {
                    continue;
                }

                foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                {
                    TryAddSourcePath(sourcePaths, binding);
                }

                foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                {
                    TryAddSourcePath(sourcePaths, binding);
                }
            }

            _entries.Clear();
            foreach (var sourcePath in sourcePaths)
            {
                _entries.Add(new RemapEntry
                {
                    sourcePath = sourcePath,
                    targetPath = string.Empty,
                });
            }
        }

        private void TryAddSourcePath(SortedSet<string> sourcePaths, EditorCurveBinding binding)
        {
            if (_onlyUnmappedExtraBones && string.IsNullOrEmpty(binding.path))
            {
                return;
            }

            if (!string.IsNullOrEmpty(binding.path))
            {
                sourcePaths.Add(binding.path);
            }
        }

        private void AutoMapByLeafName()
        {
            if (_targetPaths.Count == 0)
            {
                CollectTargetPaths();
            }

            foreach (var entry in _entries)
            {
                if (TryFindBestTargetPath(entry.sourcePath, out var targetPath))
                {
                    entry.targetPath = targetPath;
                    continue;
                }

                entry.targetPath = string.Empty;
            }
        }

        private bool TryFindBestTargetPath(string sourcePath, out string targetPath)
        {
            targetPath = string.Empty;

            var leafName = NormalizeName(GetLeafName(sourcePath));
            if (!_targetPathsByLeafName.TryGetValue(leafName, out var candidates) || candidates.Count == 0)
            {
                return false;
            }

            if (candidates.Count == 1)
            {
                targetPath = candidates[0];
                return true;
            }

            targetPath = candidates
                .OrderByDescending(candidate => GetCommonSuffixScore(sourcePath, candidate))
                .ThenBy(candidate => candidate.Length)
                .FirstOrDefault();

            return !string.IsNullOrEmpty(targetPath);
        }

        private static int GetCommonSuffixScore(string sourcePath, string targetPath)
        {
            var sourceSegments = SplitPath(sourcePath);
            var targetSegments = SplitPath(targetPath);
            var score = 0;

            var sourceIndex = sourceSegments.Length - 1;
            var targetIndex = targetSegments.Length - 1;

            while (sourceIndex >= 0 && targetIndex >= 0)
            {
                if (NormalizeName(sourceSegments[sourceIndex]) != NormalizeName(targetSegments[targetIndex]))
                {
                    break;
                }

                score++;
                sourceIndex--;
                targetIndex--;
            }

            return score;
        }

        private static string GetLeafName(string path)
        {
            var segments = SplitPath(path);
            return segments.Length == 0 ? string.Empty : segments[^1];
        }

        private static string[] SplitPath(string path)
        {
            return string.IsNullOrEmpty(path)
                ? Array.Empty<string>()
                : path.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private void ShowTargetPathMenu(RemapEntry entry)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("<Clear>"), string.IsNullOrEmpty(entry.targetPath), () => entry.targetPath = string.Empty);
            menu.AddSeparator(string.Empty);

            foreach (var path in _targetPaths)
            {
                var selected = path == entry.targetPath;
                menu.AddItem(new GUIContent(path), selected, () => entry.targetPath = path);
            }

            menu.ShowAsContext();
        }

        private void RemapClips()
        {
            EnsureOutputFolder(_outputFolderPath);

            var remap = _entries
                .Where(entry => entry.enabled && !string.IsNullOrEmpty(entry.sourcePath) && !string.IsNullOrEmpty(entry.targetPath))
                .GroupBy(entry => entry.sourcePath)
                .ToDictionary(group => group.Key, group => group.Last().targetPath, StringComparer.Ordinal);

            if (remap.Count == 0)
            {
                EditorUtility.DisplayDialog("Remap 실패", "활성화된 매핑 항목이 없습니다.", "확인");
                return;
            }

            var createdCount = 0;
            foreach (var sourceClip in _clips)
            {
                if (sourceClip == null)
                {
                    continue;
                }

                var outputClip = new AnimationClip();
                EditorUtility.CopySerialized(sourceClip, outputClip);
                outputClip.name = $"{sourceClip.name}_Remapped";

                RemapCurves(sourceClip, outputClip, remap);
                RemapObjectCurves(sourceClip, outputClip, remap);

                var outputPath = AssetDatabase.GenerateUniqueAssetPath($"{_outputFolderPath}/{outputClip.name}.anim");
                AssetDatabase.CreateAsset(outputClip, outputPath);
                createdCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Remap 완료", $"{createdCount}개의 검증용 클립을 생성했습니다.", "확인");
        }

        private static void RemapCurves(
            AnimationClip sourceClip,
            AnimationClip outputClip,
            IReadOnlyDictionary<string, string> remap)
        {
            foreach (var sourceBinding in AnimationUtility.GetCurveBindings(sourceClip))
            {
                if (!remap.TryGetValue(sourceBinding.path, out var targetPath))
                {
                    continue;
                }

                var curve = AnimationUtility.GetEditorCurve(sourceClip, sourceBinding);
                var targetBinding = sourceBinding;
                targetBinding.path = targetPath;

                AnimationUtility.SetEditorCurve(outputClip, sourceBinding, null);
                AnimationUtility.SetEditorCurve(outputClip, targetBinding, curve);
            }
        }

        private static void RemapObjectCurves(
            AnimationClip sourceClip,
            AnimationClip outputClip,
            IReadOnlyDictionary<string, string> remap)
        {
            foreach (var sourceBinding in AnimationUtility.GetObjectReferenceCurveBindings(sourceClip))
            {
                if (!remap.TryGetValue(sourceBinding.path, out var targetPath))
                {
                    continue;
                }

                var curve = AnimationUtility.GetObjectReferenceCurve(sourceClip, sourceBinding);
                var targetBinding = sourceBinding;
                targetBinding.path = targetPath;

                AnimationUtility.SetObjectReferenceCurve(outputClip, sourceBinding, null);
                AnimationUtility.SetObjectReferenceCurve(outputClip, targetBinding, curve);
            }
        }

        private string GetTargetPath(Transform target)
        {
            if (target == _armatureRoot)
            {
                return _includeArmatureName ? _armatureRoot.name : string.Empty;
            }

            var stack = new Stack<string>();
            var current = target;
            while (current != null && current != _armatureRoot)
            {
                stack.Push(current.name);
                current = current.parent;
            }

            if (_includeArmatureName)
            {
                stack.Push(_armatureRoot.name);
            }

            return string.Join("/", stack);
        }

        private static void EnsureOutputFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            var parts = folderPath.Split('/');
            var current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static string NormalizeName(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value
                .Replace(" ", string.Empty)
                .Replace("_", string.Empty)
                .Replace("-", string.Empty)
                .Replace("(", string.Empty)
                .Replace(")", string.Empty)
                .ToLowerInvariant();
        }
    }
}
