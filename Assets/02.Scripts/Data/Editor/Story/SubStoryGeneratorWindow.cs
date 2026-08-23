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
    /// 야외 필드의 반복/보조 의뢰용 Quest/Dialogue/StoryEntry 에셋을 생성한다.
    /// 메인 진행도는 큰 잠금 조건으로만 사용하고, 실제 완료 여부는 퀘스트와 플래그에서 관리한다.
    /// </summary>
    public class SubStoryGeneratorWindow : EditorWindow
    {
        private const string QUEST_ROOT = "Assets/10.Datas/Quest/Generated/SubStory";
        private const string DIALOGUE_ROOT = "Assets/10.Datas/Dialogue/Generated/SubStory";
        private const string STORY_ROOT = "Assets/10.Datas/Story/Generated/SubStory";
        private const string QUEST_DB_SCAN_ROOT = "Assets/10.Datas/Quest";

        private Vector2 _scroll;
        private bool _overwriteExisting = true;
        private bool _refreshQuestDatabase = true;

        public static void ShowWindow()
        {
            var win = GetWindow<SubStoryGeneratorWindow>("Sub Story Generator");
            win.minSize = new Vector2(760, 520);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("서브 스토리 자동 생성", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                $"{StoryGeneratorMarkdownLoader.SubStoryDocPath}의 STORY_GENERATOR_SUB 블록을 기준으로 NPC 반복 의뢰와 보조 단서용 에셋을 생성합니다.",
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

        private void DrawSeedPreview(SubStorySeed seed)
        {
            EditorGUILayout.BeginVertical("helpBox");
            EditorGUILayout.LabelField($"{seed.QuestId}  {seed.QuestName}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Progress {seed.RequiredProgress} / 목표 {seed.Objectives.Length} / 대화 {seed.Dialogues.Length} / 반복 {seed.IsRepeatable}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField(seed.Description, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndVertical();
        }

        private bool GenerateAll(bool showDialog = true)
        {
            if (!TryGetSeeds(out var seeds, out var sourceMessage))
            {
                Debug.LogError($"[SubStoryGenerator] 생성 중단: {sourceMessage}");
                if (showDialog)
                    EditorUtility.DisplayDialog("생성 중단", sourceMessage, "확인");
                return false;
            }

            EnsureFolders();
            Debug.Log($"[SubStoryGenerator] {sourceMessage}");

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

            if (_refreshQuestDatabase)
                RefreshQuestDatabase();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (showDialog)
                EditorUtility.DisplayDialog("생성 완료", "서브 스토리 기본 에셋 생성/갱신이 완료되었습니다.", "확인");
            return true;
        }

        private static bool TryGetSeeds(out SubStorySeed[] seeds, out string sourceMessage)
        {
            if (StoryGeneratorMarkdownLoader.TryLoadSub(out var document, out var error))
            {
                try
                {
                    seeds = document.quests.Select(SubStorySeed.FromDocument).ToArray();
                    sourceMessage = $"문서 데이터 사용: {StoryGeneratorMarkdownLoader.SubStoryDocPath} ({StoryGeneratorMarkdownLoader.Summary(document)})";
                    return true;
                }
                catch (System.Exception exception)
                {
                    error = exception.Message;
                }
            }

            seeds = System.Array.Empty<SubStorySeed>();
            sourceMessage = $"권위 문서를 읽지 못해 생성을 중단합니다: {error}";
            return false;
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

            var talkNode = CreateInstance<DialogueNodeSO>();
            talkNode.name = "Node_Talk_Start";
            talkNode.nodeId = $"{seed.GraphId}_talk";
            talkNode.nodeType = NodeType.Talk;
            talkNode.channel = seed.Channel;
            talkNode.speakerId = seed.SpeakerId;
            talkNode.dialogueText = seed.Text;
            talkNode.nextNodeId = $"{seed.GraphId}_end";
            talkNode.editorPosition = new Vector2(120, 120);

            var endNode = CreateInstance<DialogueNodeSO>();
            endNode.name = "Node_End";
            endNode.nodeId = $"{seed.GraphId}_end";
            endNode.nodeType = NodeType.End;
            endNode.editorPosition = new Vector2(420, 120);

            graph.graphId = seed.GraphId;
            graph.graphName = seed.GraphName;
            graph.startNodeId = talkNode.nodeId;
            graph.nodes.Add(talkNode);
            graph.nodes.Add(endNode);
            graph.InvalidateCache();

            AssetDatabase.AddObjectToAsset(talkNode, graph);
            AssetDatabase.AddObjectToAsset(endNode, graph);
            EditorUtility.SetDirty(graph);
            return graph;
        }

        private QuestSO CreateOrUpdateQuest(SubStorySeed seed)
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
            quest.questType = QuestType.Sub;
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
            entry.maxProgressExclusive = 0;
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
                Debug.LogWarning("[SubStoryGenerator] QuestDatabase asset을 찾을 수 없어 DB 갱신을 건너뜁니다.");
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

        private class SubStorySeed
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

            public static SubStorySeed FromDocument(StoryGeneratorQuest quest) => new()
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
            public readonly DialogueChannel Channel;
            public readonly string SpeakerId;
            public readonly string Text;

            private DialogueSeed(string graphId, string graphName, DialogueChannel channel, string speakerId, string text)
            {
                GraphId = graphId;
                GraphName = graphName;
                Channel = channel;
                SpeakerId = speakerId;
                Text = text;
            }

            public static DialogueSeed FromDocument(StoryGeneratorDialogue dialogue)
                => new(
                    dialogue.graphId,
                    dialogue.graphName,
                    StoryGeneratorMarkdownLoader.ResolveChannel(dialogue.channel),
                    dialogue.speakerId,
                    dialogue.text);
        }

        private readonly struct StoryEntrySeed
        {
            public readonly string StoryId;
            public readonly int RequiredProgress;
            public readonly StoryTriggerMode TriggerMode;
            public readonly string DialogueGraphId;

            private StoryEntrySeed(
                string storyId,
                int requiredProgress,
                string dialogueGraphId,
                StoryTriggerMode triggerMode)
            {
                StoryId = storyId;
                RequiredProgress = requiredProgress;
                TriggerMode = triggerMode;
                DialogueGraphId = dialogueGraphId;
            }

            public static StoryEntrySeed FromDocument(StoryGeneratorEntry entry)
                => new(
                    entry.storyId,
                    entry.requiredProgress,
                    entry.dialogueGraphId,
                    StoryGeneratorMarkdownLoader.ResolveTriggerMode(
                        entry.triggerMode,
                        StoryTriggerMode.Zone));
        }
    }
}
#endif
