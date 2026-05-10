using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Actor.Animation.Editor
{
    /// <summary>
    /// ActorAnimationMotionSet 과 그것이 참조하는 MotionSetAsset 들을 한 번에 복제하고,
    /// 새 ActorAnimationMotionSet 이 새로 복사된 MotionSetAsset 들을 참조하도록 자동 연결한다.
    /// </summary>
    public class ActorAnimationMotionSetDuplicator : EditorWindow
    {
        ActorAnimationMotionSet _source;
        DefaultAsset _targetFolder;
        string _newName = "";
        string _renameFrom = "";
        string _renameTo = "";
        bool _autoRename = true;
        bool _duplicateFallback;
        bool _overwriteExisting;
        Vector2 _scroll;

        readonly List<MotionSetAsset> _previewAssets = new();

        [MenuItem("Tools/UPlayGround/Animation/모션셋 복제기 (참조 포함)")]
        static void OpenFromMenu()
        {
            var w = GetWindow<ActorAnimationMotionSetDuplicator>("모션셋 복제기");
            w.minSize = new Vector2(460, 540);
            w.Show();
        }

        [MenuItem("Assets/UPlayGround/모션셋 복제 (참조 포함)", true)]
        static bool ValidateContextMenu()
        {
            return Selection.activeObject is ActorAnimationMotionSet;
        }

        [MenuItem("Assets/UPlayGround/모션셋 복제 (참조 포함)", false, 30)]
        static void OpenFromContext()
        {
            var w = GetWindow<ActorAnimationMotionSetDuplicator>("모션셋 복제기");
            w.minSize = new Vector2(460, 540);
            w._source = Selection.activeObject as ActorAnimationMotionSet;
            w.RefreshFromSource();
            w.Show();
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("모션셋 + 참조 에셋 복제기", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "원본 ActorAnimationMotionSet 과 그 안에서 참조하는 모든 MotionSetAsset 을 복사하고,\n" +
                "복사본이 새 MotionSetAsset 들을 참조하도록 자동 연결합니다.",
                MessageType.Info);

            EditorGUILayout.Space(4);
            EditorGUI.BeginChangeCheck();
            _source = (ActorAnimationMotionSet)EditorGUILayout.ObjectField(
                "원본 MotionSet", _source, typeof(ActorAnimationMotionSet), false);
            if (EditorGUI.EndChangeCheck())
                RefreshFromSource();

            using (new EditorGUI.DisabledScope(_source == null))
            {
                EditorGUILayout.Space(2);
                _newName = EditorGUILayout.TextField("새 에셋 이름", _newName);
                _targetFolder = (DefaultAsset)EditorGUILayout.ObjectField(
                    "저장 폴더", _targetFolder, typeof(DefaultAsset), false);

                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("자식 MotionSetAsset 이름 변환", EditorStyles.miniBoldLabel);
                _autoRename = EditorGUILayout.Toggle(
                    new GUIContent("자동 이름 변환",
                        "ON: 원본 root 이름을 새 이름으로 치환  /  OFF: 아래 from→to 직접 지정"),
                    _autoRename);

                using (new EditorGUI.DisabledScope(_autoRename))
                {
                    _renameFrom = EditorGUILayout.TextField("교체 from", _renameFrom);
                    _renameTo = EditorGUILayout.TextField("교체 to", _renameTo);
                }

                EditorGUILayout.Space(4);
                _duplicateFallback = EditorGUILayout.Toggle(
                    new GUIContent("Fallback 도 복제",
                        "체크 시 fallbackMotionSet 자체도 같은 폴더에 복사 (기본: 끄기 — 원본 공용 모션 그대로 참조)"),
                    _duplicateFallback);
                _overwriteExisting = EditorGUILayout.Toggle(
                    new GUIContent("기존 파일 덮어쓰기",
                        "끄면 같은 이름이 존재할 때 자동으로 ' 1', ' 2' 등을 붙여 새로 만든다"),
                    _overwriteExisting);

                EditorGUILayout.Space(8);
                DrawPreview();

                EditorGUILayout.Space(8);
                using (new EditorGUI.DisabledScope(!CanExecute()))
                {
                    GUI.backgroundColor = new Color(0.4f, 0.85f, 0.45f);
                    if (GUILayout.Button("복제 실행", GUILayout.Height(36)))
                        Execute();
                    GUI.backgroundColor = Color.white;
                }
            }
        }

        void RefreshFromSource()
        {
            _previewAssets.Clear();
            if (_source == null) return;

            _newName = _source.name + "_Copy";
            _renameFrom = _source.name;
            _renameTo = _newName;

            var path = AssetDatabase.GetAssetPath(_source);
            var dir = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(dir))
                _targetFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(dir);

            if (_source.motionSets != null)
            {
                foreach (var kv in _source.motionSets)
                {
                    if (kv.Value != null) _previewAssets.Add(kv.Value);
                }
            }
        }

        bool CanExecute()
        {
            return _source != null
                   && !string.IsNullOrWhiteSpace(_newName)
                   && _targetFolder != null;
        }

        void DrawPreview()
        {
            EditorGUILayout.LabelField($"참조 에셋 미리보기 ({_previewAssets.Count})", EditorStyles.miniBoldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MinHeight(60), GUILayout.MaxHeight(220));
                if (_previewAssets.Count == 0)
                {
                    EditorGUILayout.LabelField("참조하는 MotionSetAsset 없음.", EditorStyles.miniLabel);
                }
                else
                {
                    foreach (var a in _previewAssets)
                    {
                        if (a == null) continue;
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.ObjectField(a, typeof(MotionSetAsset), false);
                            EditorGUILayout.LabelField("→", GUILayout.Width(18));
                            EditorGUILayout.LabelField(ResolveChildNewName(a.name), EditorStyles.miniLabel);
                        }
                    }
                }
                EditorGUILayout.EndScrollView();
            }
        }

        string ResolveChildNewName(string original)
        {
            string from = _autoRename ? (_source != null ? _source.name : "") : _renameFrom;
            string to = _autoRename ? _newName : _renameTo;
            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(original)) return original;
            return original.Replace(from, to);
        }

        void Execute()
        {
            string folderPath = AssetDatabase.GetAssetPath(_targetFolder);
            if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
            {
                EditorUtility.DisplayDialog("오류", "유효한 폴더를 선택해주세요.", "확인");
                return;
            }

            var sourcePath = AssetDatabase.GetAssetPath(_source);
            if (string.IsNullOrEmpty(sourcePath))
            {
                EditorUtility.DisplayDialog("오류", "원본 에셋의 경로를 찾을 수 없습니다.", "확인");
                return;
            }

            // 1) 자식 MotionSetAsset 들을 새 폴더로 복사
            var copiedPaths = new Dictionary<string, string>();
            foreach (var src in _previewAssets)
            {
                if (src == null) continue;

                var srcPath = AssetDatabase.GetAssetPath(src)?.Replace('\\', '/');
                if (string.IsNullOrEmpty(srcPath)) continue;
                if (copiedPaths.ContainsKey(srcPath)) continue;

                var newName = ResolveChildNewName(src.name);
                var newPath = $"{folderPath}/{newName}.asset";
                newPath = ResolveTargetPath(newPath);

                if (AssetDatabase.CopyAsset(srcPath, newPath))
                    copiedPaths[srcPath] = newPath;
                else
                    Debug.LogError($"[모션셋 복제기] 자식 복사 실패: {srcPath} → {newPath}");
            }

            // 2) Root 복사
            var newRootPath = ResolveTargetPath($"{folderPath}/{_newName}.asset");
            if (!AssetDatabase.CopyAsset(sourcePath, newRootPath))
            {
                EditorUtility.DisplayDialog("오류", $"Root 복사 실패: {sourcePath} → {newRootPath}", "확인");
                return;
            }

            // 3) Fallback 복사 (옵션)
            string newFallbackPath = null;
            if (_duplicateFallback && _source.fallbackMotionSet != null)
            {
                CopyReferencedMotionAssets(_source.fallbackMotionSet, folderPath, copiedPaths);

                var fbSrcPath = AssetDatabase.GetAssetPath(_source.fallbackMotionSet)?.Replace('\\', '/');
                if (!string.IsNullOrEmpty(fbSrcPath))
                {
                    var fbNewName = ResolveChildNewName(_source.fallbackMotionSet.name);
                    newFallbackPath = ResolveTargetPath($"{folderPath}/{fbNewName}.asset");
                    if (!AssetDatabase.CopyAsset(fbSrcPath, newFallbackPath))
                    {
                        Debug.LogError($"[모션셋 복제기] Fallback 복사 실패: {fbSrcPath}");
                        newFallbackPath = null;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 4) 새 root 의 참조를 새 자식들로 교체
            var newRoot = AssetDatabase.LoadAssetAtPath<ActorAnimationMotionSet>(newRootPath);
            if (newRoot == null)
            {
                EditorUtility.DisplayDialog("오류", "새 root 에셋을 로드하지 못했습니다.", "확인");
                return;
            }

            int rewired = RewireMotionSetReferences(newRoot, copiedPaths);

            if (newFallbackPath != null)
            {
                var newFallback = AssetDatabase.LoadAssetAtPath<ActorAnimationMotionSet>(newFallbackPath);
                if (newFallback != null)
                {
                    Undo.RecordObject(newRoot, "Duplicate MotionSet Fallback Reference");
                    newRoot.fallbackMotionSet = newFallback;
                    EditorUtility.SetDirty(newRoot);
                    rewired += RewireMotionSetReferences(newFallback, copiedPaths);
                }
            }

            EditorUtility.SetDirty(newRoot);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorGUIUtility.PingObject(newRoot);
            Selection.activeObject = newRoot;

            Debug.Log($"[모션셋 복제기] 완료 → {newRootPath}\n  자식 {copiedPaths.Count}개 복사 / {rewired}개 재연결" +
                     (newFallbackPath != null ? $"\n  Fallback 복사: {newFallbackPath}" : ""));
        }

        string ResolveTargetPath(string desiredPath)
        {
            if (_overwriteExisting && AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(desiredPath) != null)
            {
                AssetDatabase.DeleteAsset(desiredPath);
                return desiredPath;
            }
            return AssetDatabase.GenerateUniqueAssetPath(desiredPath);
        }

        void CopyReferencedMotionAssets(
            ActorAnimationMotionSet sourceSet,
            string folderPath,
            IDictionary<string, string> copiedPaths)
        {
            if (sourceSet?.motionSets == null) return;

            foreach (var kv in sourceSet.motionSets)
            {
                MotionSetAsset src = kv.Value;
                if (src == null) continue;

                string srcPath = AssetDatabase.GetAssetPath(src)?.Replace('\\', '/');
                if (string.IsNullOrEmpty(srcPath) || copiedPaths.ContainsKey(srcPath)) continue;

                string newName = ResolveChildNewName(src.name);
                string newPath = ResolveTargetPath($"{folderPath}/{newName}.asset");

                if (AssetDatabase.CopyAsset(srcPath, newPath))
                    copiedPaths[srcPath] = newPath;
                else
                    Debug.LogError($"[모션셋 복제기] Fallback 자식 복사 실패: {srcPath} → {newPath}");
            }
        }

        static int RewireMotionSetReferences(ActorAnimationMotionSet target, IReadOnlyDictionary<string, string> copiedPaths)
        {
            if (target == null || copiedPaths == null || copiedPaths.Count == 0) return 0;

            var sObj = new SerializedObject(target);
            var listProp = sObj.FindProperty("motionSets")?.FindPropertyRelative("_serializedList");
            if (listProp == null) return 0;

            Undo.RecordObject(target, "Duplicate MotionSet References");

            int rewired = 0;
            for (int i = 0; i < listProp.arraySize; i++)
            {
                SerializedProperty valueProp = listProp.GetArrayElementAtIndex(i).FindPropertyRelative("Value");
                if (valueProp == null || valueProp.objectReferenceValue == null) continue;

                string sourcePath = AssetDatabase.GetAssetPath(valueProp.objectReferenceValue);
                if (string.IsNullOrEmpty(sourcePath)) continue;
                sourcePath = sourcePath.Replace('\\', '/');

                if (!copiedPaths.TryGetValue(sourcePath, out string copiedPath)) continue;

                var copiedAsset = AssetDatabase.LoadAssetAtPath<MotionSetAsset>(copiedPath);
                if (copiedAsset == null) continue;

                valueProp.objectReferenceValue = copiedAsset;
                rewired++;
            }

            if (rewired > 0)
            {
                sObj.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssetIfDirty(target);
            }

            return rewired;
        }
    }
}
