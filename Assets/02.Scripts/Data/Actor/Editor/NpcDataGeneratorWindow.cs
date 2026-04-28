#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;
using UPlayGround.Dialogue;

namespace UPlayGround.Tool.Editor.Actor
{
    /// <summary>
    /// NpcActorSO와 NPC용 ActorDefinitionSO를 생성/연결하는 에디터 도구.
    /// 메뉴: UPlayGround/NPC/NPC Data Generator
    /// </summary>
    public class NpcDataGeneratorWindow : EditorWindow
    {
        private const string DefaultNpcSavePath = "Assets/10.Datas/Actor/Npc";
        private const string DefaultDefinitionSavePath = "Assets/10.Datas/Actor/DataBase";
        private const string DefaultNpcPrefabPath = "Assets/03.Prefabs/Actor/NPC/NPC_Default.prefab";

        private string _actorId = "NPC_New";
        private string _displayName = "새 NPC";
        private string _description = "";
        private int _hp = 1;
        private DialogueGraphSO _dialogueGraph;

        private GameObject _npcPrefab;
        private ActorDefinitionSO _targetDefinition;
        private ActorDatabase _actorDatabase;

        private string _npcSavePath = DefaultNpcSavePath;
        private string _definitionSavePath = DefaultDefinitionSavePath;
        private bool _createDefinition = true;
        private bool _connectToDefinition = true;
        private bool _addToActorDatabase = true;
        private bool _selectCreatedAsset = true;
        private bool _overwriteExistingNpcData = false;

        private Vector2 _scroll;

        [MenuItem("UPlayGround/NPC/NPC Data Generator")]
        public static void Open()
        {
            var window = GetWindow<NpcDataGeneratorWindow>();
            window.titleContent = new GUIContent("NPC Data Generator", EditorGUIUtility.IconContent("d_ScriptableObject Icon").image);
            window.minSize = new Vector2(620f, 520f);
            window.Show();
        }

        private void OnEnable()
        {
            TryLoadActorDatabase();
            TryLoadDefaultNpcPrefab();
        }

        private void OnGUI()
        {
            DrawToolbar();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawNpcDataSection();
            EditorGUILayout.Space(6f);
            DrawDefinitionSection();
            EditorGUILayout.Space(6f);
            DrawPathSection();
            EditorGUILayout.Space(6f);
            DrawPreviewSection();
            EditorGUILayout.Space(10f);
            DrawActionSection();
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("새로고침", EditorStyles.toolbarButton, GUILayout.Width(70f)))
            {
                TryLoadActorDatabase();
                TryLoadDefaultNpcPrefab();
            }

            GUILayout.FlexibleSpace();
            _createDefinition = GUILayout.Toggle(_createDefinition, "Definition 생성", EditorStyles.toolbarButton, GUILayout.Width(105f));
            _connectToDefinition = GUILayout.Toggle(_connectToDefinition, "Definition 연결", EditorStyles.toolbarButton, GUILayout.Width(105f));
            _addToActorDatabase = GUILayout.Toggle(_addToActorDatabase, "DB 등록", EditorStyles.toolbarButton, GUILayout.Width(70f));
            _selectCreatedAsset = GUILayout.Toggle(_selectCreatedAsset, "생성 에셋 선택", EditorStyles.toolbarButton, GUILayout.Width(105f));

            EditorGUILayout.EndHorizontal();
        }

        private void DrawNpcDataSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("NPC 데이터", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _actorId = EditorGUILayout.TextField("Actor ID", _actorId);
            if (EditorGUI.EndChangeCheck() && string.IsNullOrWhiteSpace(_displayName))
                _displayName = _actorId;

            _displayName = EditorGUILayout.TextField("표시 이름", _displayName);
            _description = EditorGUILayout.TextField("설명", _description);
            _hp = Mathf.Max(0, EditorGUILayout.IntField("HP", _hp));
            _dialogueGraph = (DialogueGraphSO)EditorGUILayout.ObjectField("Dialogue Graph", _dialogueGraph, typeof(DialogueGraphSO), false);

            EditorGUILayout.EndVertical();
        }

        private void DrawDefinitionSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("ActorDefinition 연동", EditorStyles.boldLabel);

            _targetDefinition = (ActorDefinitionSO)EditorGUILayout.ObjectField("기존 Definition", _targetDefinition, typeof(ActorDefinitionSO), false);
            _npcPrefab = (GameObject)EditorGUILayout.ObjectField("NPC Prefab", _npcPrefab, typeof(GameObject), false);

            EditorGUILayout.BeginHorizontal();
            _actorDatabase = (ActorDatabase)EditorGUILayout.ObjectField("ActorDatabase", _actorDatabase, typeof(ActorDatabase), false);
            if (GUILayout.Button("자동", GUILayout.Width(44f)))
                TryLoadActorDatabase();
            EditorGUILayout.EndHorizontal();

            if (_targetDefinition != null)
            {
                EditorGUILayout.HelpBox(
                    "기존 Definition이 선택되어 있으면 새 Definition 생성 대신 해당 Definition의 actorId, displayName, prefab, npcData를 갱신합니다.",
                    MessageType.None);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawPathSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("저장 경로", EditorStyles.boldLabel);

            DrawPathField("NPC Data", ref _npcSavePath);
            DrawPathField("Definition", ref _definitionSavePath);
            _overwriteExistingNpcData = EditorGUILayout.Toggle("동일 이름 NPC 데이터 갱신", _overwriteExistingNpcData);

            EditorGUILayout.EndVertical();
        }

        private void DrawPreviewSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("생성 미리보기", EditorStyles.boldLabel);

            string npcAssetName = BuildNpcAssetName();
            string definitionAssetName = BuildDefinitionAssetName();
            EditorGUILayout.LabelField("NpcActorSO", $"{_npcSavePath}/{npcAssetName}.asset");
            EditorGUILayout.LabelField("ActorDefinitionSO", _targetDefinition != null
                ? AssetDatabase.GetAssetPath(_targetDefinition)
                : $"{_definitionSavePath}/{definitionAssetName}.asset");
            EditorGUILayout.LabelField("ActorType", (ActorType.NPC | ActorType.Talkable).ToString());

            var existingNpc = FindNpcDataByName(npcAssetName);
            if (existingNpc != null)
                EditorGUILayout.HelpBox($"동일 이름 NpcActorSO가 이미 있습니다: {AssetDatabase.GetAssetPath(existingNpc)}", MessageType.Warning);

            if (_actorDatabase == null && _addToActorDatabase)
                EditorGUILayout.HelpBox("ActorDatabase를 찾지 못했습니다. DB 등록 옵션은 생성 시 건너뜁니다.", MessageType.Warning);

            EditorGUILayout.EndVertical();
        }

        private void DrawActionSection()
        {
            string validation = GetValidationMessage();
            if (!string.IsNullOrEmpty(validation))
                EditorGUILayout.HelpBox(validation, MessageType.Warning);

            using (new EditorGUI.DisabledScope(!string.IsNullOrEmpty(validation)))
            {
                if (GUILayout.Button("NPC 데이터 생성", GUILayout.Height(36f)))
                    CreateNpcData();
            }
        }

        private void CreateNpcData()
        {
            EnsureFolder(_npcSavePath);
            EnsureFolder(_definitionSavePath);

            var npcData = CreateOrUpdateNpcData();
            ActorDefinitionSO definition = null;

            if (_connectToDefinition)
                definition = CreateOrUpdateDefinition(npcData);

            if (_addToActorDatabase && _actorDatabase != null && definition != null)
                _actorDatabase.AddDefinition(definition);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (_selectCreatedAsset)
            {
                Selection.activeObject = definition != null ? definition : npcData;
                EditorGUIUtility.PingObject(Selection.activeObject);
            }

            EditorUtility.DisplayDialog(
                "NPC 데이터 생성 완료",
                $"NpcActorSO: {AssetDatabase.GetAssetPath(npcData)}\n" +
                (definition != null ? $"ActorDefinitionSO: {AssetDatabase.GetAssetPath(definition)}" : "ActorDefinitionSO: 생성/연결 안 함"),
                "확인");
        }

        private NpcActorSO CreateOrUpdateNpcData()
        {
            string assetName = BuildNpcAssetName();
            var existing = FindNpcDataByName(assetName);
            NpcActorSO npcData;

            if (existing != null && _overwriteExistingNpcData)
            {
                npcData = existing;
                Undo.RecordObject(npcData, "Update NPC Data");
            }
            else
            {
                npcData = CreateInstance<NpcActorSO>();
                string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{_npcSavePath}/{assetName}.asset");
                AssetDatabase.CreateAsset(npcData, assetPath);
            }

            npcData.actorName = _displayName;
            npcData.description = _description;
            npcData.hp = _hp;
            npcData.dialogueGraph = _dialogueGraph;
            npcData.showInfoUI = false;
            npcData.showShakeEffect = false;
            EditorUtility.SetDirty(npcData);

            return npcData;
        }

        private ActorDefinitionSO CreateOrUpdateDefinition(NpcActorSO npcData)
        {
            ActorDefinitionSO definition = _targetDefinition;

            if (definition == null)
            {
                if (!_createDefinition)
                    return null;

                definition = CreateInstance<ActorDefinitionSO>();
                string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{_definitionSavePath}/{BuildDefinitionAssetName()}.asset");
                AssetDatabase.CreateAsset(definition, assetPath);
            }
            else
            {
                Undo.RecordObject(definition, "Update NPC Actor Definition");
            }

            definition.actorId = _actorId.Trim();
            definition.displayName = _displayName;
            definition.description = _description;
            definition.actorType = ActorType.NPC | ActorType.Talkable;
            definition.characterType = CharacterActorType.None;
            definition.prefab = _npcPrefab;
            definition.npcData = npcData;
            EditorUtility.SetDirty(definition);

            return definition;
        }

        private string GetValidationMessage()
        {
            if (string.IsNullOrWhiteSpace(_actorId))
                return "Actor ID가 비어 있습니다.";
            if (string.IsNullOrWhiteSpace(_displayName))
                return "표시 이름이 비어 있습니다.";
            if (_connectToDefinition && _createDefinition && _npcPrefab == null)
                return "Definition을 생성하려면 NPC Prefab이 필요합니다.";
            if (string.IsNullOrWhiteSpace(_npcSavePath) || !_npcSavePath.StartsWith("Assets", StringComparison.Ordinal))
                return "NPC Data 저장 경로는 Assets 하위여야 합니다.";
            if (string.IsNullOrWhiteSpace(_definitionSavePath) || !_definitionSavePath.StartsWith("Assets", StringComparison.Ordinal))
                return "Definition 저장 경로는 Assets 하위여야 합니다.";

            var existingNpc = FindNpcDataByName(BuildNpcAssetName());
            if (existingNpc != null && !_overwriteExistingNpcData)
                return "동일 이름 NpcActorSO가 이미 있습니다. 파일명을 바꾸거나 '동일 이름 NPC 데이터 갱신'을 켜세요.";

            return "";
        }

        private void TryLoadActorDatabase()
        {
            string[] guids = AssetDatabase.FindAssets("t:ActorDatabase");
            if (guids.Length > 0)
                _actorDatabase = AssetDatabase.LoadAssetAtPath<ActorDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private void TryLoadDefaultNpcPrefab()
        {
            if (_npcPrefab != null)
                return;

            _npcPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultNpcPrefabPath);
        }

        private NpcActorSO FindNpcDataByName(string assetName)
        {
            string[] guids = AssetDatabase.FindAssets($"{assetName} t:NpcActorSO");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var npc = AssetDatabase.LoadAssetAtPath<NpcActorSO>(path);
                if (npc != null && npc.name == assetName)
                    return npc;
            }

            return null;
        }

        private string BuildNpcAssetName()
        {
            string source = string.IsNullOrWhiteSpace(_actorId) ? "NPC_New" : _actorId.Trim();
            return SanitizeFileName(source.StartsWith("NPC_", StringComparison.OrdinalIgnoreCase) ? source : $"NPC_{source}");
        }

        private string BuildDefinitionAssetName()
        {
            string source = string.IsNullOrWhiteSpace(_actorId) ? "NPC_New" : _actorId.Trim();
            return SanitizeFileName(source);
        }

        private void DrawPathField(string label, ref string path)
        {
            EditorGUILayout.BeginHorizontal();
            path = EditorGUILayout.TextField(label, path);
            if (GUILayout.Button("...", GUILayout.Width(28f)))
                BrowseSavePath(ref path);
            EditorGUILayout.EndHorizontal();
        }

        private static void BrowseSavePath(ref string targetPath)
        {
            string abs = EditorUtility.OpenFolderPanel("저장 경로 선택", targetPath, "");
            if (string.IsNullOrEmpty(abs))
                return;

            string projectRoot = Application.dataPath.Replace("/Assets", "");
            if (abs.StartsWith(projectRoot))
                targetPath = "Assets" + abs.Substring(projectRoot.Length + "/Assets".Length).Replace("\\", "/");
            else
                EditorUtility.DisplayDialog("경고", "프로젝트 폴더 내부 경로를 선택해야 합니다.", "확인");
        }

        private static void EnsureFolder(string path)
        {
            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path))
                return;

            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static string SanitizeFileName(string value)
        {
            string name = value.Trim();
            foreach (char invalid in Path.GetInvalidFileNameChars())
                name = name.Replace(invalid, '_');
            return string.IsNullOrWhiteSpace(name) ? "NPC_New" : name.Replace('/', '_').Replace('\\', '_');
        }
    }
}
#endif
