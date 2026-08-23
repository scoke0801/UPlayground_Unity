using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UPlayGround.Data.Actor;

namespace UPlayGround.Dialogue.Editor
{
    /// <summary>
    /// Dialogue SO 파일의 speakerId와 Actor 후보를 스캔해
    /// SpeakerActorBindingTableSO를 생성/갱신하는 에디터 도구.
    /// </summary>
    public class SpeakerActorBindingTableGeneratorWindow : EditorWindow
    {
        private const string DefaultAssetPath = "Assets/10.Datas/Dialogue/SpeakerActorBindingTable.asset";
        private const string DefaultDialogueRoot = "Assets/10.Datas/Dialogue";

        // 보스 인물은 레거시 DLG_Npc_* 파일명/소유 프록시보다 실제 조우 Actor를 우선한다.
        // 구체 조우가 Boss* 변형을 사용하면 조우가 넘기는 지정 대화 상대 인스턴스가 이 매핑을 덮어쓴다.
        private static readonly IReadOnlyDictionary<string, string> StoryRoleBindings =
            new Dictionary<string, string>
            {
                ["라온"] = "MonsterBokusei",
                ["리안리안"] = "MonsterLianLian",
                ["화린"] = "MonsterHonoka",
            };

        private SpeakerActorBindingTableSO _targetTable;
        private bool _mainChannelOnly = true;
        private bool _scanYamlFiles = true;
        private bool _preferDialogueOwner = true;
        private bool _overwriteExisting;
        private string _dialogueRoot = DefaultDialogueRoot;
        private Vector2 _scroll;
        private List<BindingPreview> _previews = new();

        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/내러티브/대화/화자 액터 바인딩 생성기", priority = UPlayGround.Tool.Editor.UPlaygroundMenuPriority.NarrativeDialogue + 1)]
        public static void Open()
        {
            GetWindow<SpeakerActorBindingTableGeneratorWindow>("Speaker Actor Binding");
        }

        private void OnEnable()
        {
            _targetTable = FindExistingTable();
            RefreshPreview();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Speaker Actor Binding 자동 생성", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            _targetTable = (SpeakerActorBindingTableSO)EditorGUILayout.ObjectField(
                "Target Table",
                _targetTable,
                typeof(SpeakerActorBindingTableSO),
                false);

            _mainChannelOnly = EditorGUILayout.Toggle("Main 채널만 스캔", _mainChannelOnly);
            _scanYamlFiles = EditorGUILayout.Toggle("Dialogue SO 파일 직접 순회", _scanYamlFiles);
            _preferDialogueOwner = EditorGUILayout.Toggle("대화 파일 소유 NPC 우선", _preferDialogueOwner);
            _overwriteExisting = EditorGUILayout.Toggle("기존 매핑 덮어쓰기", _overwriteExisting);
            _dialogueRoot = EditorGUILayout.TextField("Dialogue Root", string.IsNullOrEmpty(_dialogueRoot) ? DefaultDialogueRoot : _dialogueRoot);

            EditorGUILayout.Space(6f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("테이블 생성/로드"))
                    _targetTable = CreateOrLoadTable();

                if (GUILayout.Button("미리보기 갱신"))
                    RefreshPreview();

                using (new EditorGUI.DisabledScope(_targetTable == null))
                {
                    if (GUILayout.Button("매핑 적용"))
                        ApplyBindings();
                }
            }

            EditorGUILayout.Space(8f);
            DrawPreview();
        }

        private void DrawPreview()
        {
            int resolvedCount = _previews.Count(x => !string.IsNullOrEmpty(x.actorId));
            EditorGUILayout.LabelField($"스캔 결과: {_previews.Count}개 speakerId, 자동 매핑 {resolvedCount}개");

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (BindingPreview preview in _previews)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(preview.speakerId, GUILayout.Width(220f));
                    EditorGUILayout.LabelField(string.IsNullOrEmpty(preview.actorId) ? "<미해결>" : preview.actorId, GUILayout.Width(220f));
                    EditorGUILayout.LabelField($"{preview.reason} / {preview.sourceSummary}");
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void RefreshPreview()
        {
            Dictionary<string, string> existing = ReadExistingBindings(_targetTable);
            List<ActorCandidate> actors = LoadActorCandidates();
            Dictionary<string, SpeakerSourceInfo> speakerSources = LoadSpeakerSources();

            _previews = speakerSources.Values
                .Select(source => ResolvePreview(source, existing, actors))
                .OrderBy(x => x.speakerId)
                .ToList();

            Repaint();
        }

        private BindingPreview ResolvePreview(SpeakerSourceInfo source, Dictionary<string, string> existing, List<ActorCandidate> actors)
        {
            string speakerId = source.SpeakerId;
            if (!_overwriteExisting && existing.TryGetValue(speakerId, out string existingActorId) && !string.IsNullOrEmpty(existingActorId))
                return new BindingPreview(speakerId, existingActorId, "기존 매핑 유지", source.GetSummary());

            if (StoryRoleBindings.TryGetValue(speakerId, out string roleActorId))
                return new BindingPreview(speakerId, roleActorId, "스토리 역할 고정 매핑", source.GetSummary());

            ActorCandidate actor = FindByAlias(actors, speakerId, exactOnly: true);
            if (actor != null)
                return new BindingPreview(speakerId, actor.actorId, "speakerId 직접 일치", source.GetSummary());

            if (_preferDialogueOwner)
            {
                actor = ResolveByHints(source.ActorIdHints, actors);
                if (actor != null)
                    return new BindingPreview(speakerId, actor.actorId, "대화 파일/소유 NPC 힌트", source.GetSummary());
            }

            actor = FindByAlias(actors, speakerId, exactOnly: false);
            if (actor != null)
                return new BindingPreview(speakerId, actor.actorId, "speakerId 부분 일치", source.GetSummary());

            actor = ResolveByHints(source.ActorIdHints, actors);
            if (actor != null)
                return new BindingPreview(speakerId, actor.actorId, "파일명 힌트", source.GetSummary());

            return new BindingPreview(speakerId, string.Empty, "수동 지정 필요", source.GetSummary());
        }

        private void ApplyBindings()
        {
            if (_targetTable == null)
                _targetTable = CreateOrLoadTable();

            Dictionary<string, string> previousBindings = ReadExistingBindings(_targetTable);
            RefreshPreview();

            var serializedObject = new SerializedObject(_targetTable);
            SerializedProperty entries = serializedObject.FindProperty("entries");
            entries.ClearArray();

            var appliedSpeakerIds = new HashSet<string>();
            foreach (BindingPreview preview in _previews)
            {
                if (string.IsNullOrEmpty(preview.actorId))
                    continue;

                AddEntry(entries, preview.speakerId, preview.actorId);
                appliedSpeakerIds.Add(preview.speakerId);
            }

            if (!_overwriteExisting)
            {
                foreach (var pair in previousBindings.OrderBy(x => x.Key))
                {
                    if (appliedSpeakerIds.Contains(pair.Key))
                        continue;

                    AddEntry(entries, pair.Key, pair.Value);
                }
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(_targetTable);
            AssetDatabase.SaveAssets();
            EnsureAddressable(_targetTable, SpeakerActorBindingTableSO.AddressableKey);
            Debug.Log($"[SpeakerActorBindingTableGenerator] {_previews.Count}개 speakerId 스캔, {entries.arraySize}개 매핑 적용 완료");
        }

        private static void AddEntry(SerializedProperty entries, string speakerId, string actorId)
        {
            int index = entries.arraySize;
            entries.InsertArrayElementAtIndex(index);
            SerializedProperty entry = entries.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("speakerId").stringValue = speakerId;
            entry.FindPropertyRelative("actorId").stringValue = actorId;
        }

        private static SpeakerActorBindingTableSO FindExistingTable()
        {
            string guid = AssetDatabase.FindAssets($"t:{nameof(SpeakerActorBindingTableSO)}").FirstOrDefault();
            if (string.IsNullOrEmpty(guid))
                return null;

            return AssetDatabase.LoadAssetAtPath<SpeakerActorBindingTableSO>(AssetDatabase.GUIDToAssetPath(guid));
        }

        private static SpeakerActorBindingTableSO CreateOrLoadTable()
        {
            var table = AssetDatabase.LoadAssetAtPath<SpeakerActorBindingTableSO>(DefaultAssetPath);
            if (table != null)
                return table;

            string directory = Path.GetDirectoryName(DefaultAssetPath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            table = CreateInstance<SpeakerActorBindingTableSO>();
            AssetDatabase.CreateAsset(table, DefaultAssetPath);
            AssetDatabase.SaveAssets();
            EnsureAddressable(table, SpeakerActorBindingTableSO.AddressableKey);
            return table;
        }

        private Dictionary<string, SpeakerSourceInfo> LoadSpeakerSources()
        {
            var sources = new Dictionary<string, SpeakerSourceInfo>();
            Dictionary<string, string> graphOwnerMap = BuildGraphOwnerMap();

            string[] graphGuids = AssetDatabase.FindAssets($"t:{nameof(DialogueGraphSO)}");
            foreach (string guid in graphGuids)
            {
                string graphPath = AssetDatabase.GUIDToAssetPath(guid);
                var graph = AssetDatabase.LoadAssetAtPath<DialogueGraphSO>(graphPath);
                if (graph == null)
                    continue;

                foreach (DialogueNodeSO node in graph.nodes)
                    AddNodeSpeakerSource(sources, node, graphPath, graphOwnerMap);
            }

            string[] nodeGuids = AssetDatabase.FindAssets($"t:{nameof(DialogueNodeSO)}");
            foreach (string guid in nodeGuids)
            {
                string nodePath = AssetDatabase.GUIDToAssetPath(guid);
                var node = AssetDatabase.LoadAssetAtPath<DialogueNodeSO>(nodePath);
                AddNodeSpeakerSource(sources, node, nodePath, graphOwnerMap);
            }

            if (_scanYamlFiles)
                AddYamlSpeakerSources(sources, graphOwnerMap);

            return sources;
        }

        private void AddNodeSpeakerSource(Dictionary<string, SpeakerSourceInfo> sources, DialogueNodeSO node, string assetPath, Dictionary<string, string> graphOwnerMap)
        {
            if (node == null || string.IsNullOrEmpty(node.speakerId))
                return;

            if (_mainChannelOnly && node.channel != DialogueChannel.Main)
                return;

            SpeakerSourceInfo source = GetOrCreateSource(sources, node.speakerId);
            source.AddPath(assetPath);
            AddPathHints(source, assetPath);

            if (graphOwnerMap.TryGetValue(assetPath, out string ownerActorId))
                source.AddActorHint(ownerActorId);
        }

        private void AddYamlSpeakerSources(Dictionary<string, SpeakerSourceInfo> sources, Dictionary<string, string> graphOwnerMap)
        {
            string root = string.IsNullOrEmpty(_dialogueRoot) ? DefaultDialogueRoot : _dialogueRoot;
            if (!Directory.Exists(root))
                return;

            foreach (string fullPath in Directory.EnumerateFiles(root, "*.asset", SearchOption.AllDirectories))
            {
                string assetPath = ToAssetPath(fullPath);
                string[] lines = File.ReadAllLines(fullPath);
                bool includeCurrentObject = !_mainChannelOnly;
                string currentSpeakerId = null;

                foreach (string line in lines)
                {
                    Match channelMatch = Regex.Match(line, @"^\s*channel:\s*(\d+)\s*$");
                    if (channelMatch.Success)
                    {
                        includeCurrentObject = !_mainChannelOnly || channelMatch.Groups[1].Value == "0";
                        continue;
                    }

                    Match speakerMatch = Regex.Match(line, @"^\s*speakerId:\s*(.*)$");
                    if (!speakerMatch.Success)
                        continue;

                    currentSpeakerId = DecodeYamlScalar(speakerMatch.Groups[1].Value);
                    if (string.IsNullOrEmpty(currentSpeakerId) || !includeCurrentObject)
                        continue;

                    SpeakerSourceInfo source = GetOrCreateSource(sources, currentSpeakerId);
                    source.AddPath(assetPath);
                    AddPathHints(source, assetPath);

                    if (graphOwnerMap.TryGetValue(assetPath, out string ownerActorId))
                        source.AddActorHint(ownerActorId);
                }
            }
        }

        private static Dictionary<string, string> BuildGraphOwnerMap()
        {
            var result = new Dictionary<string, string>();
            string[] guids = AssetDatabase.FindAssets("t:NpcActorSO");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var npc = AssetDatabase.LoadAssetAtPath<NpcActorSO>(path);
                if (npc == null || npc.dialogueGraph == null)
                    continue;

                string graphPath = AssetDatabase.GetAssetPath(npc.dialogueGraph);
                if (string.IsNullOrEmpty(graphPath))
                    continue;

                result[graphPath] = Path.GetFileNameWithoutExtension(path);
            }

            return result;
        }

        private static SpeakerSourceInfo GetOrCreateSource(Dictionary<string, SpeakerSourceInfo> sources, string speakerId)
        {
            if (!sources.TryGetValue(speakerId, out SpeakerSourceInfo source))
            {
                source = new SpeakerSourceInfo(speakerId);
                sources.Add(speakerId, source);
            }

            return source;
        }

        private static void AddPathHints(SpeakerSourceInfo source, string assetPath)
        {
            string fileName = Path.GetFileNameWithoutExtension(assetPath);
            source.AddActorHint(fileName);

            if (fileName.StartsWith("DLG_Npc_"))
                source.AddActorHint("NPC_" + fileName.Substring("DLG_Npc_".Length));

            if (fileName.StartsWith("dlg_sub_guide"))
                source.AddActorHint("NPC_Story_Guide");

            if (fileName.Contains("_Raon"))
                source.AddActorHint("Raon");
        }

        private static List<ActorCandidate> LoadActorCandidates()
        {
            var actors = new List<ActorCandidate>();
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(ActorDefinitionSO)}");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var definition = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(path);
                if (definition == null || string.IsNullOrEmpty(definition.actorId))
                    continue;

                var candidate = new ActorCandidate(definition.actorId);
                candidate.AddAlias(definition.actorId);
                candidate.AddAlias(definition.displayName);
                candidate.AddAlias(Path.GetFileNameWithoutExtension(path));
                candidate.AddAlias(definition.name);
                actors.Add(candidate);
            }

            string[] npcGuids = AssetDatabase.FindAssets("t:NpcActorSO");
            foreach (string guid in npcGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var npc = AssetDatabase.LoadAssetAtPath<NpcActorSO>(path);
                if (npc == null)
                    continue;

                string assetName = Path.GetFileNameWithoutExtension(path);
                var candidate = new ActorCandidate(assetName);
                candidate.AddAlias(assetName);
                candidate.AddAlias(assetName.Replace("NPC_", ""));
                candidate.AddAlias(npc.actorName);
                candidate.AddAlias(npc.name);
                actors.Add(candidate);
            }

            return actors;
        }

        private static ActorCandidate FindByAlias(IEnumerable<ActorCandidate> actors, string value, bool exactOnly)
        {
            foreach (ActorCandidate actor in actors)
            {
                if (actor.HasExactAlias(value))
                    return actor;
            }

            if (exactOnly)
                return null;

            return actors.FirstOrDefault(actor => actor.HasPartialAlias(value));
        }

        private static ActorCandidate ResolveByHints(IEnumerable<string> hints, IEnumerable<ActorCandidate> actors)
        {
            foreach (string hint in hints)
            {
                ActorCandidate actor = FindByAlias(actors, hint, exactOnly: true);
                if (actor != null)
                    return actor;
            }

            foreach (string hint in hints)
            {
                ActorCandidate actor = FindByAlias(actors, hint, exactOnly: false);
                if (actor != null)
                    return actor;
            }

            return null;
        }

        private static Dictionary<string, string> ReadExistingBindings(SpeakerActorBindingTableSO table)
        {
            var result = new Dictionary<string, string>();
            if (table == null)
                return result;

            var serializedObject = new SerializedObject(table);
            SerializedProperty entries = serializedObject.FindProperty("entries");
            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                string speakerId = entry.FindPropertyRelative("speakerId").stringValue;
                string actorId = entry.FindPropertyRelative("actorId").stringValue;
                if (!string.IsNullOrEmpty(speakerId) && !string.IsNullOrEmpty(actorId))
                    result[speakerId] = actorId;
            }

            return result;
        }

        private static void EnsureAddressable(UnityEngine.Object asset, string address)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            string guid = AssetDatabase.AssetPathToGUID(path);
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null || string.IsNullOrEmpty(guid))
            {
                Debug.LogWarning("[SpeakerActorBindingTableGenerator] Addressables Settings를 찾지 못해 주소 등록을 건너뜁니다.");
                return;
            }

            AddressableAssetGroup group = settings.DefaultGroup;
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group);
            entry.address = address;
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
            AssetDatabase.SaveAssets();
        }

        private static bool ContainsIgnoreCase(string source, string value)
        {
            return !string.IsNullOrEmpty(source) &&
                   !string.IsNullOrEmpty(value) &&
                   source.ToLowerInvariant().Contains(value.ToLowerInvariant());
        }

        private static string DecodeYamlScalar(string rawValue)
        {
            string value = rawValue.Trim();
            if (value == "''" || value == "\"\"")
                return string.Empty;

            if (value.StartsWith("\"") && value.EndsWith("\"") && value.Length >= 2)
                value = value.Substring(1, value.Length - 2);

            try
            {
                return Regex.Unescape(value).Trim();
            }
            catch
            {
                return value.Trim();
            }
        }

        private static string ToAssetPath(string fullPath)
        {
            string normalized = fullPath.Replace('\\', '/');
            int index = normalized.IndexOf("Assets/", System.StringComparison.OrdinalIgnoreCase);
            return index >= 0 ? normalized.Substring(index) : normalized;
        }

        private sealed class ActorCandidate
        {
            public readonly string actorId;
            private readonly HashSet<string> _aliases = new();

            public ActorCandidate(string actorId)
            {
                this.actorId = actorId;
            }

            public void AddAlias(string alias)
            {
                if (!string.IsNullOrEmpty(alias))
                    _aliases.Add(alias);
            }

            public bool HasExactAlias(string value)
            {
                return !string.IsNullOrEmpty(value) && _aliases.Any(alias => alias == value);
            }

            public bool HasPartialAlias(string value)
            {
                return !string.IsNullOrEmpty(value) &&
                       _aliases.Any(alias => ContainsIgnoreCase(alias, value) || ContainsIgnoreCase(value, alias));
            }
        }

        private sealed class SpeakerSourceInfo
        {
            public string SpeakerId { get; }
            public HashSet<string> ActorIdHints { get; } = new();
            private readonly HashSet<string> _paths = new();

            public SpeakerSourceInfo(string speakerId)
            {
                SpeakerId = speakerId;
            }

            public void AddPath(string path)
            {
                if (!string.IsNullOrEmpty(path))
                    _paths.Add(path);
            }

            public void AddActorHint(string actorId)
            {
                if (!string.IsNullOrEmpty(actorId))
                    ActorIdHints.Add(actorId);
            }

            public string GetSummary()
            {
                if (_paths.Count == 0)
                    return "source 없음";

                string first = _paths.OrderBy(x => x).First();
                string fileName = Path.GetFileName(first);
                return _paths.Count == 1 ? fileName : $"{fileName} 외 {_paths.Count - 1}";
            }
        }

        private readonly struct BindingPreview
        {
            public readonly string speakerId;
            public readonly string actorId;
            public readonly string reason;
            public readonly string sourceSummary;

            public BindingPreview(string speakerId, string actorId, string reason, string sourceSummary)
            {
                this.speakerId = speakerId;
                this.actorId = actorId;
                this.reason = reason;
                this.sourceSummary = sourceSummary;
            }
        }
    }
}
