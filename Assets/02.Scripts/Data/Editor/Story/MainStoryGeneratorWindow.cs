#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Quest;
using UPlayGround.Dialogue;
using UPlayGround.Story;

namespace UPlayGround.Editor
{
    /// <summary>
    /// 권위 문서의 메인 스토리 정의를 기준으로 Quest/Dialogue/StoryEntry와
    /// 진행도 자동 재생 시퀀스를 생성한다.
    /// </summary>
    public class MainStoryGeneratorWindow : EditorWindow
    {
        private const string QUEST_ROOT = "Assets/10.Datas/Quest/Generated/MainStory";
        private const string DIALOGUE_ROOT = "Assets/10.Datas/Dialogue/Generated/MainStory";
        private const string STORY_ROOT = "Assets/10.Datas/Story/Generated/MainStory";
        private const string MAIN_STORY_SEQUENCE_PATH = "Assets/Resources/MainStorySequence.asset";
        private const string QUEST_DB_SCAN_ROOT = "Assets/10.Datas/Quest";

        private Vector2 _scroll;
        private bool _overwriteExisting = true;
        private bool _refreshQuestDatabase = true;

        public static void ShowWindow()
        {
            var win = GetWindow<MainStoryGeneratorWindow>("Main Story Generator");
            win.minSize = new Vector2(760, 520);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("메인 스토리 자동 생성", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                $"{StoryGeneratorMarkdownLoader.MainStoryDocPath}의 STORY_GENERATOR_MAIN 블록을 기준으로 QuestSO, DialogueGraphSO, StoryEntrySO 초안을 생성합니다.",
                MessageType.Info);

            bool hasDocument = TryGetSeeds(out var seeds, out var sourceMessage);
            EditorGUILayout.HelpBox(
                sourceMessage,
                hasDocument ? MessageType.None : MessageType.Error);

            _overwriteExisting = EditorGUILayout.ToggleLeft("기존 생성 에셋 갱신", _overwriteExisting);
            _refreshQuestDatabase = EditorGUILayout.ToggleLeft("생성 후 QuestDatabase 갱신", _refreshQuestDatabase);

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(!hasDocument))
            {
                if (GUILayout.Button("생성/갱신", GUILayout.Height(32)))
                    GenerateAll();
            }
            if (GUILayout.Button("생성 폴더 선택", GUILayout.Height(32)))
                PingGeneratedFolder();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var seed in seeds)
                DrawSeedPreview(seed);
            EditorGUILayout.EndScrollView();
        }

        private void DrawSeedPreview(StorySeed seed)
        {
            EditorGUILayout.BeginVertical("helpBox");
            EditorGUILayout.LabelField($"{seed.QuestId}  {seed.QuestName}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Progress {seed.RequiredProgress} / 목표 {seed.Objectives.Length} / 대화 {seed.Dialogues.Length} / 스토리 {seed.Stories.Length}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField(seed.Description, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndVertical();
        }

        public static void GenerateFromDocumentBatch()
        {
            var window = CreateInstance<MainStoryGeneratorWindow>();
            try
            {
                if (!window.GenerateAll(showDialog: false))
                    throw new System.InvalidOperationException("메인 스토리 문서 생성에 실패했습니다.");
            }
            finally
            {
                DestroyImmediate(window);
            }
        }

        private bool GenerateAll(bool showDialog = true)
        {
            if (!TryGetSeeds(out var seeds, out var sourceMessage))
            {
                Debug.LogError($"[MainStoryGenerator] 생성 중단: {sourceMessage}");
                if (showDialog)
                    EditorUtility.DisplayDialog("생성 중단", sourceMessage, "확인");
                return false;
            }

            EnsureFolders();
            Debug.Log($"[MainStoryGenerator] {sourceMessage}");

            var dialogueMap = new Dictionary<string, DialogueGraphSO>();
            foreach (var seed in seeds)
            {
                foreach (var dialogue in seed.Dialogues)
                {
                    var graph = CreateOrUpdateDialogue(dialogue);
                    if (graph != null)
                        dialogueMap[dialogue.GraphId] = graph;
                }
            }

            foreach (var seed in seeds)
            {
                CreateOrUpdateQuest(seed);
                foreach (var story in seed.Stories)
                {
                    dialogueMap.TryGetValue(story.DialogueGraphId, out var graph);
                    CreateOrUpdateStoryEntry(story, graph);
                }
            }

            RefreshMainStorySequence(seeds);

            if (_refreshQuestDatabase)
                RefreshQuestDatabase();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (showDialog)
                EditorUtility.DisplayDialog("생성 완료", "메인 스토리 기본 에셋 생성/갱신이 완료되었습니다.", "확인");
            return true;
        }

        private static bool TryGetSeeds(out StorySeed[] seeds, out string sourceMessage)
        {
            if (StoryGeneratorMarkdownLoader.TryLoadMain(out var document, out var error))
            {
                try
                {
                    seeds = document.quests.Select(StorySeed.FromDocument).ToArray();
                    sourceMessage = $"문서 데이터 사용: {StoryGeneratorMarkdownLoader.MainStoryDocPath} ({StoryGeneratorMarkdownLoader.Summary(document)})";
                    return true;
                }
                catch (System.Exception exception)
                {
                    error = exception.Message;
                }
            }

            seeds = System.Array.Empty<StorySeed>();
            sourceMessage = $"권위 문서를 읽지 못해 생성을 중단합니다: {error}";
            return false;
        }

        private static void RefreshMainStorySequence(IEnumerable<StorySeed> seeds)
        {
            EnsureFolder("Assets/Resources");
            var sequence = AssetDatabase.LoadAssetAtPath<StorySequenceSO>(MAIN_STORY_SEQUENCE_PATH);
            if (sequence == null)
            {
                sequence = CreateInstance<StorySequenceSO>();
                AssetDatabase.CreateAsset(sequence, MAIN_STORY_SEQUENCE_PATH);
            }

            sequence.entries = seeds
                .SelectMany(seed => seed.Stories)
                .Select(story => AssetDatabase.LoadAssetAtPath<StoryEntrySO>(
                    $"{STORY_ROOT}/{story.StoryId}.asset"))
                .Where(entry => entry != null)
                .ToList();
            EditorUtility.SetDirty(sequence);
        }

        private static void EnsureFolders()
        {
            EnsureFolder(QUEST_ROOT);
            EnsureFolder(DIALOGUE_ROOT);
            EnsureFolder(STORY_ROOT);
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;

            var parts = folder.Split('/');
            var current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private DialogueGraphSO CreateOrUpdateDialogue(DialogueSeed seed)
        {
            var path = $"{DIALOGUE_ROOT}/{seed.GraphId}.asset";
            var graph = AssetDatabase.LoadAssetAtPath<DialogueGraphSO>(path);
            if (graph != null && !_overwriteExisting) return graph;

            if (graph == null)
            {
                graph = CreateInstance<DialogueGraphSO>();
                AssetDatabase.CreateAsset(graph, path);
            }
            else
            {
                foreach (var node in graph.nodes.Where(n => n != null).ToArray())
                    DestroyImmediate(node, true);
                graph.nodes.Clear();
            }

            var endNode = CreateInstance<DialogueNodeSO>();
            endNode.name = "Node_End";
            endNode.nodeId = $"{seed.GraphId}_end";
            endNode.nodeType = NodeType.End;
            endNode.editorPosition = new Vector2(120 + seed.Lines.Length * 300, 120);

            graph.graphId = seed.GraphId;
            graph.graphName = seed.GraphName;
            graph.startNodeId = GetTalkNodeId(seed.GraphId, 0, seed.Lines.Length);

            for (int i = 0; i < seed.Lines.Length; i++)
            {
                DialogueLineSeed line = seed.Lines[i];
                var talkNode = CreateInstance<DialogueNodeSO>();
                talkNode.name = seed.Lines.Length == 1 ? "Node_Talk_Start" : $"Node_Talk_{i + 1:00}";
                talkNode.nodeId = GetTalkNodeId(seed.GraphId, i, seed.Lines.Length);
                talkNode.nodeType = NodeType.Talk;
                talkNode.channel = line.Channel;
                talkNode.speakerId = line.SpeakerId;
                talkNode.dialogueText = line.Text;
                talkNode.nextNodeId = i + 1 < seed.Lines.Length
                    ? GetTalkNodeId(seed.GraphId, i + 1, seed.Lines.Length)
                    : endNode.nodeId;
                talkNode.editorPosition = new Vector2(120 + i * 300, 120);

                graph.nodes.Add(talkNode);
                AssetDatabase.AddObjectToAsset(talkNode, graph);
            }

            graph.nodes.Add(endNode);
            graph.InvalidateCache();

            AssetDatabase.AddObjectToAsset(endNode, graph);
            EditorUtility.SetDirty(graph);
            return graph;
        }

        private static string GetTalkNodeId(string graphId, int index, int lineCount)
            => lineCount == 1 ? $"{graphId}_talk" : $"{graphId}_talk_{index + 1:00}";

        private QuestSO CreateOrUpdateQuest(StorySeed seed)
        {
            var path = $"{QUEST_ROOT}/{seed.QuestId}.asset";
            var quest = AssetDatabase.LoadAssetAtPath<QuestSO>(path);
            if (quest != null && !_overwriteExisting) return quest;

            if (quest == null)
            {
                quest = CreateInstance<QuestSO>();
                AssetDatabase.CreateAsset(quest, path);
            }

            quest.questId = seed.QuestId;
            quest.questName = seed.QuestName;
            quest.questType = QuestType.Main;
            quest.shortSummary = seed.ShortSummary;
            quest.questDescription = seed.Description;
            quest.isContentEnabled = seed.IsContentEnabled;
            quest.requiredStoryProgress = seed.RequiredProgress;
            quest.requiredQuestIds = seed.RequiredQuestIds?.ToList() ?? new List<string>();
            quest.autoAcceptOnNewGame = seed.AutoAcceptOnNewGame;
            quest.autoAcceptNextQuestIds = seed.AutoAcceptNextQuestIds?.ToList() ?? new List<string>();
            quest.objectives = seed.Objectives.Select(x => x.ToData()).ToList();
            quest.reward.gold = seed.RewardGold;
            quest.reward.exp = seed.RewardExp;
            quest.reward.items = seed.RewardItems?.ToList() ?? new List<QuestItemReward>();
            quest.isRepeatable = seed.IsRepeatable;
            quest.autoComplete = seed.AutoComplete;

            EditorUtility.SetDirty(quest);
            return quest;
        }

        private StoryEntrySO CreateOrUpdateStoryEntry(StoryEntrySeed seed, DialogueGraphSO graph)
        {
            var path = $"{STORY_ROOT}/{seed.StoryId}.asset";
            var entry = AssetDatabase.LoadAssetAtPath<StoryEntrySO>(path);
            if (entry != null && !_overwriteExisting) return entry;

            if (entry == null)
            {
                entry = CreateInstance<StoryEntrySO>();
                AssetDatabase.CreateAsset(entry, path);
            }

            entry.storyId = seed.StoryId;
            entry.requiredProgress = seed.RequiredProgress;
            entry.maxProgressExclusive = seed.MaxProgressExclusive;
            entry.triggerMode = seed.TriggerMode;
            entry.dialogueGraph = graph;
            entry.variants = System.Array.Empty<StoryVariant>();

            EditorUtility.SetDirty(entry);
            return entry;
        }

        private static void RefreshQuestDatabase()
        {
            var dbGuid = AssetDatabase.FindAssets("t:QuestDatabase").FirstOrDefault();
            if (string.IsNullOrEmpty(dbGuid))
            {
                Debug.LogWarning("[MainStoryGenerator] QuestDatabase asset을 찾을 수 없어 DB 갱신을 건너뜁니다.");
                return;
            }

            var db = AssetDatabase.LoadAssetAtPath<QuestDatabase>(AssetDatabase.GUIDToAssetPath(dbGuid));
            if (db == null) return;
            db.RefreshDatabase(QUEST_DB_SCAN_ROOT);
        }

        private static void PingGeneratedFolder()
        {
            EnsureFolders();
            var folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(STORY_ROOT);
            if (folder == null) return;
            Selection.activeObject = folder;
            EditorGUIUtility.PingObject(folder);
        }

        private class StorySeed
        {
            public string QuestId;
            public string QuestName;
            public string ShortSummary;
            public string Description;
            public int RequiredProgress;
            public int RewardGold;
            public int RewardExp;
            public QuestItemReward[] RewardItems = System.Array.Empty<QuestItemReward>();
            public bool IsContentEnabled = true;
            public bool IsRepeatable;
            public bool AutoComplete = true;
            public bool AutoAcceptOnNewGame;
            public string[] RequiredQuestIds = System.Array.Empty<string>();
            public string[] AutoAcceptNextQuestIds = System.Array.Empty<string>();
            public ObjectiveSeed[] Objectives;
            public DialogueSeed[] Dialogues;
            public StoryEntrySeed[] Stories;

            public static StorySeed FromDocument(StoryGeneratorQuest quest) => new()
            {
                QuestId = quest.questId,
                QuestName = quest.questName,
                ShortSummary = quest.shortSummary,
                Description = quest.description,
                RequiredProgress = quest.requiredProgress,
                RewardGold = quest.rewardGold,
                RewardExp = quest.rewardExp,
                RewardItems = (quest.rewardItems ?? System.Array.Empty<StoryGeneratorItemReward>())
                    .Select(x => x.ToQuestItemReward())
                    .ToArray(),
                IsContentEnabled = quest.isContentEnabled,
                IsRepeatable = quest.isRepeatable,
                AutoComplete = quest.autoComplete,
                AutoAcceptOnNewGame = quest.autoAcceptOnNewGame,
                RequiredQuestIds = quest.requiredQuestIds ?? System.Array.Empty<string>(),
                AutoAcceptNextQuestIds = quest.autoAcceptNextQuestIds ?? System.Array.Empty<string>(),
                Objectives = (quest.objectives ?? System.Array.Empty<StoryGeneratorObjective>())
                    .Select(ObjectiveSeed.FromDocument)
                    .ToArray(),
                Dialogues = (quest.dialogues ?? System.Array.Empty<StoryGeneratorDialogue>())
                    .Select(DialogueSeed.FromDocument)
                    .ToArray(),
                Stories = (quest.stories ?? System.Array.Empty<StoryGeneratorEntry>())
                    .Select(StoryEntrySeed.FromDocument)
                    .ToArray()
            };
        }

        /// <summary>
        /// 문서 목표 항목을 QuestSO 데이터로 옮긴다. 필드를 하나씩 베끼면 표시 조건 같은 값이
        /// 조용히 누락되므로 변환이 끝난 데이터를 통째로 보관한다.
        /// </summary>
        private readonly struct ObjectiveSeed
        {
            private readonly QuestObjectiveData _data;

            private ObjectiveSeed(QuestObjectiveData data) => _data = data;

            public static ObjectiveSeed FromDocument(StoryGeneratorObjective objective)
                => new(objective.ToObjectiveData());

            public QuestObjectiveData ToData() => new()
            {
                objectiveId = _data.objectiveId,
                description = _data.description,
                type = _data.type,
                targetId = _data.targetId,
                npcId = _data.npcId,
                targetStringId = _data.targetStringId,
                markerLocationId = _data.markerLocationId,
                markerIntent = _data.markerIntent,
                requiredCount = _data.requiredCount,
                revealAfterObjectiveIds = new List<string>(_data.revealAfterObjectiveIds)
            };
        }

        private readonly struct DialogueSeed
        {
            public readonly string GraphId;
            public readonly string GraphName;
            public readonly DialogueLineSeed[] Lines;

            private DialogueSeed(string graphId, string graphName, DialogueLineSeed[] lines)
            {
                GraphId = graphId;
                GraphName = graphName;
                Lines = lines;
            }

            public static DialogueSeed FromDocument(StoryGeneratorDialogue dialogue)
            {
                StoryGeneratorDialogueLine[] sourceLines = dialogue.lines?
                    .Where(line => line != null)
                    .ToArray();

                DialogueLineSeed[] lines = sourceLines != null && sourceLines.Length > 0
                    ? sourceLines.Select(line => new DialogueLineSeed(
                            StoryGeneratorMarkdownLoader.ResolveChannel(
                                string.IsNullOrWhiteSpace(line.channel) ? dialogue.channel : line.channel),
                            string.IsNullOrWhiteSpace(line.speakerId) ? dialogue.speakerId : line.speakerId,
                            line.text))
                        .ToArray()
                    : new[]
                    {
                        new DialogueLineSeed(
                            StoryGeneratorMarkdownLoader.ResolveChannel(dialogue.channel),
                            dialogue.speakerId,
                            dialogue.text)
                    };

                return new DialogueSeed(dialogue.graphId, dialogue.graphName, lines);
            }
        }

        private readonly struct DialogueLineSeed
        {
            public readonly DialogueChannel Channel;
            public readonly string SpeakerId;
            public readonly string Text;

            public DialogueLineSeed(DialogueChannel channel, string speakerId, string text)
            {
                Channel = channel;
                SpeakerId = speakerId;
                Text = text;
            }
        }

        private readonly struct StoryEntrySeed
        {
            public readonly string StoryId;
            public readonly int RequiredProgress;
            public readonly int MaxProgressExclusive;
            public readonly StoryTriggerMode TriggerMode;
            public readonly string DialogueGraphId;

            private StoryEntrySeed(
                string storyId,
                int requiredProgress,
                int maxProgressExclusive,
                string dialogueGraphId,
                StoryTriggerMode triggerMode)
            {
                StoryId = storyId;
                RequiredProgress = requiredProgress;
                MaxProgressExclusive = maxProgressExclusive;
                TriggerMode = triggerMode;
                DialogueGraphId = dialogueGraphId;
            }

            public static StoryEntrySeed FromDocument(StoryGeneratorEntry entry)
                => new(
                    entry.storyId,
                    entry.requiredProgress,
                    entry.maxProgressExclusive,
                    entry.dialogueGraphId,
                    StoryGeneratorMarkdownLoader.ResolveTriggerMode(entry.triggerMode));
        }
    }
}
#endif
