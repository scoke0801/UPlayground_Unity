using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UPlayGround.AI.BehaviorTree;
using UPlayGround.AI.BehaviorTree.Editor;
using UPlayGround.Components;

namespace UPlayGround.Editor
{
    /// <summary>
    /// BehaviorTreeAsset을 비행 몬스터 프리팹의 <see cref="EnemyFlyingAIController"/>에 연결한다.
    ///
    /// 비행 컨트롤러는 의사결정을 전부 BT에 위임하므로 _behaviorTree가 비어 있으면
    /// Start에서 경고만 남기고 아무 판단도 하지 않는다. 프리팹마다 인스펙터에서 끌어다
    /// 놓는 대신 여러 변종을 한 번에 연결하기 위한 도구다.
    ///
    /// 특정 몬스터에 묶이지 않는다. 대상 프리팹은 Project 창 선택으로 채운다.
    /// </summary>
    public sealed class FlyingBehaviorTreeBinderWindow : EditorWindow
    {
        /// <summary>EnemyFlyingAIController의 직렬화 필드 이름.</summary>
        private const string BehaviorTreeField = "_behaviorTree";
        private const string DiveHarasserSourcePath =
            "Assets/10.Datas/AI/BehaviorTree/SourceJson/Flying/EnemyBehavior_Flying_DiveHarasser.json";
        private const string DiveHarasserGeneratedPath =
            "Assets/10.Datas/AI/BehaviorTree/Generated/BT_EnemyBehavior_Flying_DiveHarasser.asset";

        private static readonly string[] DiveHarasserPrefabPaths =
        {
            "Assets/03.Prefabs/Actor/Monster/Monster_Griffin_Brown.prefab",
            "Assets/03.Prefabs/Actor/Monster/Monster_Griffin_Dark.prefab",
        };

        [SerializeField] private BehaviorTreeAsset _behaviorTree;
        [SerializeField] private List<GameObject> _prefabs = new();

        private SerializedObject _serialized;
        private Vector2 _scroll;
        private string _report = string.Empty;

        [InitializeOnLoadMethod]
        private static void ScheduleMissingDiveHarasserRepair()
        {
            EditorApplication.delayCall += EnsureDiveHarasserContent;
        }

        /// <summary>
        /// 권위 SourceJson이 추가됐지만 Generated 에셋/프리팹 연결이 빠진 checkout을 복구한다.
        /// 공식 importer를 그대로 사용하며, 이미 생성·연결된 프로젝트에서는 아무것도 쓰지 않는다.
        /// </summary>
        private static void EnsureDiveHarasserContent()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += EnsureDiveHarasserContent;
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<TextAsset>(DiveHarasserSourcePath) == null)
                return;

            try
            {
                BehaviorTreeAsset tree = AssetDatabase.LoadAssetAtPath<BehaviorTreeAsset>(
                    DiveHarasserGeneratedPath);
                if (tree == null)
                {
                    tree = MonsterBehaviorTreeJsonImporter.ImportJsonFiles(
                            new[] { Path.GetFullPath(DiveHarasserSourcePath) })
                        .FirstOrDefault();
                }

                if (tree == null)
                    throw new InvalidDataException(
                        $"비행 BT를 생성하지 못했습니다: {DiveHarasserGeneratedPath}");

                int boundCount = BindTreeToPrefabs(tree, DiveHarasserPrefabPaths);
                if (boundCount > 0)
                    Debug.Log($"[FlyingBehaviorTreeBinder] Griffin 프리팹 {boundCount}개 자동 연결 완료");
            }
            catch (System.Exception exception)
            {
                Debug.LogError(
                    "[FlyingBehaviorTreeBinder] 비행 BT 생성/연결 자동 복구 실패. "
                    + "SourceJson과 Blackboard Registry를 확인하세요.\n"
                    + exception);
            }
        }

        [MenuItem("UPlayGround/툴 런처/캐릭터 · AI/비행 BT 프리팹 연결", priority = 240)]
        [UPlayGround.EditorTools.UPlaygroundTool(
            "UPlayGround/툴 런처/캐릭터 · AI/비행 BT 프리팹 연결",
            false,
            240)]
        public static void Open()
        {
            var window = GetWindow<FlyingBehaviorTreeBinderWindow>(true, "비행 BT 프리팹 연결");
            window.minSize = new Vector2(520f, 420f);
            window.Show();
        }

        private void OnEnable() => _serialized = new SerializedObject(this);

        private void OnGUI()
        {
            _serialized ??= new SerializedObject(this);
            _serialized.Update();

            EditorGUILayout.HelpBox(
                "EnemyFlyingAIController를 가진 프리팹에 BehaviorTreeAsset을 연결합니다.\n"
                + "비행 컨트롤러는 BT가 없으면 아무 의사결정도 하지 않습니다.",
                MessageType.Info);

            EditorGUILayout.PropertyField(
                _serialized.FindProperty(nameof(_behaviorTree)),
                new GUIContent("Behavior Tree"));
            EditorGUILayout.PropertyField(
                _serialized.FindProperty(nameof(_prefabs)),
                new GUIContent("대상 프리팹"),
                true);
            _serialized.ApplyModifiedProperties();

            if (GUILayout.Button("Project 창 선택으로 대상 채우기"))
                FillFromSelection();

            using (new EditorGUI.DisabledScope(_behaviorTree == null || _prefabs.Count == 0))
            {
                if (GUILayout.Button("연결", GUILayout.Height(28f)))
                    Bind();
            }

            EditorGUILayout.Space(6f);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.TextArea(_report, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private void FillFromSelection()
        {
            _prefabs.Clear();
            foreach (Object selected in Selection.GetFiltered(
                         typeof(GameObject),
                         SelectionMode.Assets))
            {
                if (selected is GameObject prefab
                    && prefab.GetComponentInChildren<EnemyFlyingAIController>(true) != null)
                    _prefabs.Add(prefab);
            }

            _report = _prefabs.Count > 0
                ? $"선택에서 비행 프리팹 {_prefabs.Count}개를 찾았습니다."
                : "선택된 에셋 중 EnemyFlyingAIController를 가진 프리팹이 없습니다.";
        }

        private void Bind()
        {
            var changed = new List<string>();
            var skipped = new List<string>();

            foreach (GameObject prefab in _prefabs)
            {
                if (prefab == null)
                    continue;

                string path = AssetDatabase.GetAssetPath(prefab);
                var controller = prefab.GetComponentInChildren<EnemyFlyingAIController>(true);
                if (controller == null)
                {
                    skipped.Add($"{path} — EnemyFlyingAIController 없음");
                    continue;
                }

                var serialized = new SerializedObject(controller);
                SerializedProperty property = serialized.FindProperty(BehaviorTreeField);
                if (property == null)
                {
                    skipped.Add($"{path} — {BehaviorTreeField} 필드를 찾지 못함");
                    continue;
                }

                if (property.objectReferenceValue == _behaviorTree)
                {
                    skipped.Add($"{path} — 이미 연결됨");
                    continue;
                }

                property.objectReferenceValue = _behaviorTree;
                serialized.ApplyModifiedProperties();
                EditorUtility.SetDirty(prefab);
                changed.Add(path);
            }

            if (changed.Count > 0)
                AssetDatabase.SaveAssets();

            var message = new StringBuilder();
            message.AppendLine($"연결 {changed.Count}건 / 건너뜀 {skipped.Count}건");
            foreach (string path in changed)
                message.AppendLine($"  · 연결: {path}");
            foreach (string entry in skipped)
                message.AppendLine($"  · 건너뜀: {entry}");

            _report = message.ToString();
            Debug.Log($"[FlyingBehaviorTreeBinder]\n{_report}");
        }

        private static int BindTreeToPrefabs(
            BehaviorTreeAsset tree,
            IEnumerable<string> prefabPaths)
        {
            int changed = 0;
            foreach (string path in prefabPaths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                    throw new FileNotFoundException("비행 몬스터 프리팹을 찾을 수 없습니다.", path);

                var controller = prefab.GetComponentInChildren<EnemyFlyingAIController>(true);
                if (controller == null)
                    throw new MissingComponentException(
                        $"{path}에 EnemyFlyingAIController가 없습니다.");

                var serialized = new SerializedObject(controller);
                SerializedProperty property = serialized.FindProperty(BehaviorTreeField);
                if (property == null)
                    throw new System.MissingFieldException(
                        typeof(EnemyFlyingAIController).FullName,
                        BehaviorTreeField);
                if (property.objectReferenceValue == tree)
                    continue;

                property.objectReferenceValue = tree;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(prefab);
                changed++;
            }

            if (changed > 0)
                AssetDatabase.SaveAssets();
            return changed;
        }
    }
}
