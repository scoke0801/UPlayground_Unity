using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Editor
{
    /// <summary>
    /// Modular Avatar Merge Armature의 핵심 사용 사례를 프로젝트 내부에서 처리하기 위한 일회성 베이크 툴.
    /// VRChat/NDMF 의존성 없이 의상·헤어 본을 대상 아바타 본에 병합하고 SkinnedMeshRenderer 본 참조를 갱신한다.
    /// </summary>
    public sealed class AvatarArmatureBakeTool : EditorWindow
    {
        private GameObject _avatarObject;
        private GameObject _sourceObject;
        private Transform _targetRoot;
        private Transform _sourceRoot;
        private string _prefix = string.Empty;
        private string _suffix = string.Empty;
        private bool _retargetSkinnedMeshes = true;
        private bool _deleteRetargetedDuplicateBones = true;
        private bool _keepSourceRootAsChild = true;
        private bool _autoDetectPrefixSuffix = true;
        private const string DefaultBakedMeshFolder = "Assets/10.Datas/Generated/ArmatureBakeMeshes";
        private bool _saveBakedMeshesAsAssets = true;
        private string _bakedMeshFolder = DefaultBakedMeshFolder;
        private int _savedMeshCount;
        private Vector2 _scroll;
        private string _status = "아바타와 헤어/의상 오브젝트를 지정하세요.";

        [MenuItem("UPlayGround/유틸/아바타 Armature 베이크 도구")]
        private static void Open()
        {
            var window = GetWindow<AvatarArmatureBakeTool>("Armature Bake");
            window.minSize = new Vector2(520f, 520f);
            window.Show();
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.LabelField("Avatar Armature Bake Tool", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Modular Avatar 제거를 전제로 한 일회성 베이크 툴입니다. 원본 프리팹이 아니라 씬에 복사한 작업본에서 실행하세요.",
                MessageType.Info);

            using (new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("입력", EditorStyles.boldLabel);

                _avatarObject = (GameObject)EditorGUILayout.ObjectField("Avatar Object", _avatarObject, typeof(GameObject), true);
                _sourceObject = (GameObject)EditorGUILayout.ObjectField("Hair/Outfit Object", _sourceObject, typeof(GameObject), true);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("본 루트", EditorStyles.boldLabel);
                _targetRoot = (Transform)EditorGUILayout.ObjectField("Target Root", _targetRoot, typeof(Transform), true);
                _sourceRoot = (Transform)EditorGUILayout.ObjectField("Source Root", _sourceRoot, typeof(Transform), true);

                if (GUILayout.Button("Auto Find Roots"))
                {
                    AutoFindRoots();
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("이름 매칭", EditorStyles.boldLabel);
                _autoDetectPrefixSuffix = EditorGUILayout.ToggleLeft("Auto Detect Prefix/Suffix", _autoDetectPrefixSuffix);
                using (new EditorGUI.DisabledScope(_autoDetectPrefixSuffix))
                {
                    _prefix = EditorGUILayout.TextField("Prefix", _prefix);
                    _suffix = EditorGUILayout.TextField("Suffix", _suffix);
                }

                if (GUILayout.Button("Detect Prefix/Suffix From Roots"))
                {
                    DetectPrefixSuffix();
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("처리 옵션", EditorStyles.boldLabel);
                _keepSourceRootAsChild = EditorGUILayout.ToggleLeft("Keep Source Root As Child (hair/accessory)", _keepSourceRootAsChild);
                using (new EditorGUI.DisabledScope(_keepSourceRootAsChild))
                {
                    _retargetSkinnedMeshes = EditorGUILayout.ToggleLeft("Retarget SkinnedMeshRenderer bones/rootBone", _retargetSkinnedMeshes);
                    _deleteRetargetedDuplicateBones = EditorGUILayout.ToggleLeft("Delete duplicate source bones after retarget", _deleteRetargetedDuplicateBones);
                    _saveBakedMeshesAsAssets = EditorGUILayout.ToggleLeft("Save baked meshes as project assets", _saveBakedMeshesAsAssets);
                    using (new EditorGUI.DisabledScope(!_saveBakedMeshesAsAssets))
                    {
                        EditorGUILayout.BeginHorizontal();
                        _bakedMeshFolder = EditorGUILayout.TextField("Baked Mesh Folder", _bakedMeshFolder);
                        if (GUILayout.Button("...", GUILayout.Width(32f)))
                        {
                            SelectBakedMeshFolder();
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(_status, MessageType.None);

            using (new EditorGUI.DisabledScope(!CanBake()))
            {
                if (GUILayout.Button("Bake Armature", GUILayout.Height(32f)))
                {
                    Bake();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private bool CanBake()
        {
            return _avatarObject != null &&
                   _sourceObject != null &&
                   _targetRoot != null &&
                   _sourceRoot != null &&
                   _targetRoot != _sourceRoot &&
                   !_targetRoot.IsChildOf(_sourceRoot);
        }

        private void AutoFindRoots()
        {
            if (_avatarObject != null && _targetRoot == null)
            {
                _targetRoot = FindLikelyArmatureRoot(_avatarObject);
            }

            if (_sourceObject != null && _sourceRoot == null)
            {
                _sourceRoot = FindLikelyArmatureRoot(_sourceObject);
            }

            if (_autoDetectPrefixSuffix)
            {
                DetectPrefixSuffix();
            }

            _status = "루트 자동 탐색을 완료했습니다. Target/Source Root가 의도한 본인지 확인하세요.";
        }

        private static Transform FindLikelyArmatureRoot(GameObject root)
        {
            if (root == null)
            {
                return null;
            }

            string[] preferredNames = { "Armature", "Hips", "Root", "Skeleton" };
            foreach (var preferredName in preferredNames)
            {
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                {
                    if (string.Equals(transform.name, preferredName, StringComparison.OrdinalIgnoreCase))
                    {
                        return transform;
                    }
                }
            }

            Transform best = root.transform;
            int bestChildCount = -1;
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (transform.childCount > bestChildCount)
                {
                    best = transform;
                    bestChildCount = transform.childCount;
                }
            }

            return best;
        }

        private void DetectPrefixSuffix()
        {
            if (_targetRoot == null || _sourceRoot == null)
            {
                _status = "Prefix/Suffix 감지를 위해 Target Root와 Source Root가 필요합니다.";
                return;
            }

            var targetChild = GetFirstBoneChild(_targetRoot);
            var sourceChild = GetFirstBoneChild(_sourceRoot);
            if (targetChild == null || sourceChild == null)
            {
                _prefix = string.Empty;
                _suffix = string.Empty;
                _status = "하위 본이 없어 Prefix/Suffix를 빈 값으로 사용합니다.";
                return;
            }

            var sourceName = sourceChild.name;
            var targetName = targetChild.name;
            int index = sourceName.IndexOf(targetName, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                _prefix = string.Empty;
                _suffix = string.Empty;
                _status = "공통 이름 패턴을 찾지 못했습니다. 필요하면 Prefix/Suffix를 직접 입력하세요.";
                return;
            }

            _prefix = sourceName.Substring(0, index);
            _suffix = sourceName.Substring(index + targetName.Length);
            _status = $"Prefix/Suffix 감지: prefix='{_prefix}', suffix='{_suffix}'";
        }

        private static Transform GetFirstBoneChild(Transform root)
        {
            foreach (Transform child in root)
            {
                if (child.GetComponent<SkinnedMeshRenderer>() == null)
                {
                    return child;
                }
            }

            return null;
        }

        private void Bake()
        {
            if (!CanBake())
            {
                _status = "입력값이 유효하지 않습니다.";
                return;
            }

            if (PrefabUtility.IsPartOfPrefabAsset(_sourceRoot) || PrefabUtility.IsPartOfPrefabAsset(_targetRoot))
            {
                EditorUtility.DisplayDialog("Bake 중단", "프로젝트 창의 프리팹 에셋에는 직접 실행할 수 없습니다. 씬에 배치한 인스턴스에서 실행하세요.", "확인");
                return;
            }

            if (PrefabUtility.IsPartOfPrefabInstance(_sourceRoot) &&
                !EditorUtility.DisplayDialog("Prefab Instance 경고",
                    "Source가 프리팹 인스턴스입니다. 베이크 과정에서 계층과 메시 참조가 변경됩니다.\n\n계속할까요?",
                    "계속", "취소"))
            {
                return;
            }

            if (_keepSourceRootAsChild && IsLikelyBodyArmatureBake())
            {
                bool continueAsAccessory = EditorUtility.DisplayDialog(
                    "처리 모드 확인",
                    "Target Root와 Source Root가 전신 의상 본처럼 보입니다.\n\n" +
                    "현재 Keep Source Root As Child 모드는 헤어/악세서리용으로, 본 병합과 SkinnedMeshRenderer 리타겟을 수행하지 않습니다.\n" +
                    "전신 의상이라면 이 옵션을 끄고 다시 실행하세요.\n\n" +
                    "그래도 Source Root를 Target Root 하위에 그대로 붙일까요?",
                    "그대로 붙이기", "취소");

                if (!continueAsAccessory)
                {
                    _status = "베이크를 취소했습니다. 전신 의상은 Keep Source Root As Child 옵션을 끄고 실행하세요.";
                    return;
                }
            }

            Undo.RegisterFullObjectHierarchyUndo(_avatarObject, "Bake Avatar Armature");
            Undo.RegisterFullObjectHierarchyUndo(_sourceObject, "Bake Source Armature");

            var mapping = new Dictionary<Transform, Transform>();
            if (_keepSourceRootAsChild)
            {
                Undo.SetTransformParent(_sourceRoot, _targetRoot, "Attach source root to target root");

                EditorUtility.SetDirty(_avatarObject);
                EditorUtility.SetDirty(_sourceObject);
                _status = $"베이크 완료: '{_sourceRoot.name}'를 '{_targetRoot.name}' 하위로 부착했습니다. 본 병합/SMR 리타겟은 수행하지 않았습니다.";
                return;
            }

            MergeRecursive(_sourceRoot, _targetRoot, mapping, true);

            int rendererCount = 0;
            _savedMeshCount = 0;
            if (!_keepSourceRootAsChild && _retargetSkinnedMeshes)
            {
                rendererCount = RetargetSkinnedMeshRenderers(mapping);
            }

            int deletedCount = 0;
            if (!_keepSourceRootAsChild && _deleteRetargetedDuplicateBones)
            {
                deletedCount = DeleteRetargetedDuplicateBones(mapping);
            }

            EditorUtility.SetDirty(_avatarObject);
            EditorUtility.SetDirty(_sourceObject);
            if (_savedMeshCount > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            _status = $"베이크 완료: 매칭 본 {mapping.Count}개, SMR {rendererCount}개 갱신, 저장 Mesh {_savedMeshCount}개, 중복 본 {deletedCount}개 삭제.";
        }

        private bool IsLikelyBodyArmatureBake()
        {
            if (_targetRoot == null || _sourceRoot == null)
            {
                return false;
            }

            bool matchingRootNames = string.Equals(
                StripPrefixSuffix(_sourceRoot.name),
                _targetRoot.name,
                StringComparison.OrdinalIgnoreCase);

            if (!matchingRootNames)
            {
                return false;
            }

            int matchingChildCount = 0;
            foreach (Transform sourceChild in _sourceRoot)
            {
                if (sourceChild.GetComponent<SkinnedMeshRenderer>() != null)
                {
                    continue;
                }

                if (FindMatchingChild(_targetRoot, sourceChild.name) != null)
                {
                    matchingChildCount++;
                }
            }

            return matchingChildCount > 0;
        }

        private void MergeRecursive(Transform source, Transform targetParent, Dictionary<Transform, Transform> mapping, bool isRoot)
        {
            var target = isRoot ? targetParent : FindMatchingChild(targetParent, source.name);
            bool matched = target != null;
            if (matched)
            {
                mapping[source] = target;
            }
            else
            {
                target = source;
                Undo.SetTransformParent(source, targetParent, "Move unique source bone");
            }

            var children = new List<Transform>();
            foreach (Transform child in source)
            {
                children.Add(child);
            }

            foreach (var child in children)
            {
                if (child.GetComponent<SkinnedMeshRenderer>() != null)
                {
                    continue;
                }

                MergeRecursive(child, target, mapping, false);
            }
        }

        private Transform FindMatchingChild(Transform targetParent, string sourceName)
        {
            string targetName = StripPrefixSuffix(sourceName);
            if (string.IsNullOrEmpty(targetName))
            {
                return null;
            }

            var match = targetParent.Find(targetName);
            return match != null ? match : targetParent.Find(sourceName);
        }

        private string StripPrefixSuffix(string value)
        {
            if (!string.IsNullOrEmpty(_prefix) && value.StartsWith(_prefix, StringComparison.Ordinal))
            {
                value = value.Substring(_prefix.Length);
            }

            if (!string.IsNullOrEmpty(_suffix) && value.EndsWith(_suffix, StringComparison.Ordinal))
            {
                value = value.Substring(0, value.Length - _suffix.Length);
            }

            return value;
        }

        private int RetargetSkinnedMeshRenderers(Dictionary<Transform, Transform> mapping)
        {
            int changedCount = 0;
            foreach (var renderer in _sourceObject.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                bool changed = false;
                var originalBones = renderer.bones;
                var newBones = new Transform[originalBones.Length];
                var originalBindposes = renderer.sharedMesh != null ? renderer.sharedMesh.bindposes : null;
                Matrix4x4[] newBindposes = originalBindposes != null ? (Matrix4x4[])originalBindposes.Clone() : null;

                for (int i = 0; i < originalBones.Length; i++)
                {
                    var originalBone = originalBones[i];
                    if (originalBone != null && mapping.TryGetValue(originalBone, out var newBone))
                    {
                        newBones[i] = newBone;
                        changed = true;

                        if (newBindposes != null && i < newBindposes.Length)
                        {
                            newBindposes[i] = newBone.worldToLocalMatrix * originalBone.localToWorldMatrix * newBindposes[i];
                        }
                    }
                    else
                    {
                        newBones[i] = originalBone;
                    }
                }

                Undo.RecordObject(renderer, "Retarget SkinnedMeshRenderer");

                var rootBone = renderer.rootBone;
                if (rootBone != null && mapping.TryGetValue(rootBone, out var newRootBone))
                {
                    renderer.rootBone = newRootBone;
                    changed = true;
                }

                if (!changed)
                {
                    if (SaveTransientSharedMeshIfNeeded(renderer))
                    {
                        changedCount++;
                    }

                    continue;
                }

                renderer.bones = newBones;

                if (renderer.sharedMesh != null && newBindposes != null)
                {
                    var mesh = UnityEngine.Object.Instantiate(renderer.sharedMesh);
                    mesh.name = renderer.sharedMesh.name + "_BakedArmature";
                    mesh.bindposes = newBindposes;
                    if (_saveBakedMeshesAsAssets)
                    {
                        mesh = SaveBakedMeshAsset(mesh, renderer);
                    }
                    renderer.sharedMesh = mesh;
                }

                changedCount++;
            }

            return changedCount;
        }

        private bool SaveTransientSharedMeshIfNeeded(SkinnedMeshRenderer renderer)
        {
            if (!_saveBakedMeshesAsAssets || renderer == null || renderer.sharedMesh == null)
            {
                return false;
            }

            if (AssetDatabase.Contains(renderer.sharedMesh))
            {
                return false;
            }

            Undo.RecordObject(renderer, "Save transient baked mesh");
            var mesh = UnityEngine.Object.Instantiate(renderer.sharedMesh);
            mesh.name = renderer.sharedMesh.name;
            renderer.sharedMesh = SaveBakedMeshAsset(mesh, renderer);
            EditorUtility.SetDirty(renderer);
            return true;
        }

        private void SelectBakedMeshFolder()
        {
            string selectedPath = EditorUtility.OpenFolderPanel("Baked Mesh 저장 폴더", "Assets", string.Empty);
            if (string.IsNullOrEmpty(selectedPath))
            {
                return;
            }

            string projectPath = Application.dataPath.Replace('\\', '/');
            selectedPath = selectedPath.Replace('\\', '/');
            if (!selectedPath.StartsWith(projectPath, StringComparison.Ordinal))
            {
                EditorUtility.DisplayDialog("폴더 선택 오류", "프로젝트의 Assets 폴더 아래 경로를 선택하세요.", "확인");
                return;
            }

            _bakedMeshFolder = "Assets" + selectedPath.Substring(projectPath.Length);
        }

        private Mesh SaveBakedMeshAsset(Mesh mesh, SkinnedMeshRenderer renderer)
        {
            string folder = NormalizeBakedMeshFolder(_bakedMeshFolder);
            EnsureAssetFolder(folder);

            string rendererName = renderer != null ? SanitizeFileName(renderer.name) : "SkinnedMesh";
            string meshName = SanitizeFileName(mesh.name);
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{rendererName}_{meshName}.asset");

            AssetDatabase.CreateAsset(mesh, path);
            _savedMeshCount++;
            return AssetDatabase.LoadAssetAtPath<Mesh>(path);
        }

        // 텍스트 필드로 직접 입력된 폴더가 Assets 루트 밖이거나 비어 있으면 기본 폴더로 폴백한다.
        // (그대로 두면 CreateFolder가 조용히 실패해 메시 저장이 누락된다.)
        private static string NormalizeBakedMeshFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
            {
                return DefaultBakedMeshFolder;
            }

            folder = folder.Replace('\\', '/').TrimEnd('/');
            if (folder != "Assets" && !folder.StartsWith("Assets/", StringComparison.Ordinal))
            {
                Debug.LogWarning($"[ArmatureBake] Baked Mesh 폴더 '{folder}'가 Assets 경로가 아니어서 기본 폴더로 저장합니다: {DefaultBakedMeshFolder}");
                return DefaultBakedMeshFolder;
            }

            return folder;
        }

        private static void EnsureAssetFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string[] parts = folderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static string SanitizeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "BakedMesh";
            }

            foreach (char invalid in System.IO.Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid, '_');
            }

            return value.Replace(' ', '_');
        }

        private static int DeleteRetargetedDuplicateBones(Dictionary<Transform, Transform> mapping)
        {
            int deletedCount = 0;
            var sources = new List<Transform>(mapping.Keys);
            sources.Sort((a, b) => GetDepth(b).CompareTo(GetDepth(a)));

            foreach (var source in sources)
            {
                if (source == null || source.childCount > 0 || HasComponentsOtherThanTransform(source))
                {
                    continue;
                }

                Undo.DestroyObjectImmediate(source.gameObject);
                deletedCount++;
            }

            return deletedCount;
        }

        private static bool HasComponentsOtherThanTransform(Transform transform)
        {
            var components = transform.GetComponents<UnityEngine.Component>();
            for (int i = 0; i < components.Length; i++)
            {
                if (!(components[i] is Transform))
                {
                    return true;
                }
            }

            return false;
        }

        private static int GetDepth(Transform transform)
        {
            int depth = 0;
            while (transform != null)
            {
                depth++;
                transform = transform.parent;
            }

            return depth;
        }
    }
}
