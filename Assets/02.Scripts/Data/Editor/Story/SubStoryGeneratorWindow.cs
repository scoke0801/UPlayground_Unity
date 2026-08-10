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

        private static readonly SubStorySeed[] Seeds =
        {
            new SubStorySeed
            {
                QuestId = "quest_sub_hunter_skeleton_patrol",
                QuestName = "길목의 뼈 무리",
                Description = "마을과 호수 사이 길목에 모인 Skeleton 무리를 정리한다.",
                RequiredProgress = 0,
                RewardGold = 80,
                RewardExp = 50,
                IsRepeatable = true,
                Objectives = new[]
                {
                    ObjectiveSeed.Kill("obj_kill_skeleton_patrol", "길목의 Skeleton을 처치한다.", ActorIdType.Skeleton_Sword, 5)
                },
                Dialogues = new[]
                {
                    DialogueSeed.Main("dlg_sub_hunter_skeleton_patrol_start", "길목의 뼈 무리 - 시작", "사냥꾼",
                        "호수로 가는 길목에 뼈들이 다시 모이고 있어.\n큰 위협은 아니지만 방치하면 마을 사람이 지나갈 수 없게 된다."),
                    DialogueSeed.Main("dlg_sub_hunter_skeleton_patrol_done", "길목의 뼈 무리 - 완료", "사냥꾼",
                        "길목이 조용해졌군.\n이 정도면 당분간 보급길은 쓸 수 있겠어.")
                },
                Stories = new[]
                {
                    StoryEntrySeed.Basic("sub_hunter_skeleton_patrol_done", 0, "dlg_sub_hunter_skeleton_patrol_done")
                }
            },
            new SubStorySeed
            {
                QuestId = "quest_sub_hunter_spider_web",
                QuestName = "숲가의 거미줄",
                Description = "거미 숲 바깥쪽의 Spider를 처치해 주민들이 우회로를 쓸 수 있게 한다.",
                RequiredProgress = 10,
                RewardGold = 120,
                RewardExp = 80,
                IsRepeatable = true,
                Objectives = new[]
                {
                    ObjectiveSeed.Kill("obj_kill_spider_web", "거미 숲 바깥쪽의 Spider를 처치한다.", ActorIdType.SpiderMinion_1, 6)
                },
                Dialogues = new[]
                {
                    DialogueSeed.Main("dlg_sub_hunter_spider_web_start", "숲가의 거미줄 - 시작", "사냥꾼",
                        "숲 깊은 곳은 네가 아니면 무리겠지만, 바깥쪽 거미줄부터 줄여야 해.\n우회로라도 살아 있어야 사람이 움직일 수 있다."),
                    DialogueSeed.Monologue("dlg_sub_spider_web_clear", "숲가의 거미줄 정리",
                        "바깥쪽 거미줄이 줄었다.\n깊은 숲은 여전히 위험하지만, 이 길은 다시 쓸 수 있겠어.")
                },
                Stories = new[]
                {
                    StoryEntrySeed.Basic("sub_spider_web_clear", 10, "dlg_sub_spider_web_clear")
                }
            },
            new SubStorySeed
            {
                QuestId = "quest_sub_herbalist_lake_herb",
                QuestName = "호수의 약초 자리",
                Description = "중앙 호수 근처의 약초 자리를 확인하고 약초상이 다시 채집할 수 있는지 살핀다.",
                RequiredProgress = 10,
                RewardGold = 90,
                RewardExp = 45,
                Objectives = new[]
                {
                    ObjectiveSeed.Reach("obj_reach_lake_herb_patch", "중앙 호수 근처 약초 자리를 확인한다.", "loc_lake_herb_patch")
                },
                Dialogues = new[]
                {
                    DialogueSeed.Main("dlg_sub_herbalist_lake_herb_start", "호수의 약초 자리 - 시작", "약초상",
                        "호수 근처 낮은 풀밭에 약초가 자라.\n몬스터가 너무 많아 직접 갈 수 없으니, 자리만이라도 남아 있는지 확인해 줘."),
                    DialogueSeed.Monologue("dlg_sub_lake_herb_patch_found", "호수 약초 자리 확인",
                        "약초가 아직 남아 있다.\n길만 정리되면 마을에서도 다시 채집하러 올 수 있겠어.")
                },
                Stories = new[]
                {
                    StoryEntrySeed.Basic("sub_lake_herb_patch_found", 10, "dlg_sub_lake_herb_patch_found")
                }
            },
            new SubStorySeed
            {
                QuestId = "quest_sub_guide_broken_lantern",
                QuestName = "쓰러진 등롱",
                Description = "석등 길 초입의 쓰러진 등롱을 확인해 길잡이에게 위치 정보를 전한다.",
                RequiredProgress = 10,
                RewardGold = 90,
                RewardExp = 45,
                Objectives = new[]
                {
                    ObjectiveSeed.Reach("obj_reach_broken_lantern", "석등 길 초입의 쓰러진 등롱을 확인한다.", "loc_broken_lantern")
                },
                Dialogues = new[]
                {
                    DialogueSeed.Main("dlg_sub_guide_broken_lantern_start", "쓰러진 등롱 - 시작", "길잡이",
                        "석등 길 초입의 등롱 하나가 쓰러졌다는 말이 있어.\n그 표식이 사라지면 던전 쪽 길을 헷갈리는 사람이 생긴다."),
                    DialogueSeed.Monologue("dlg_sub_broken_lantern_found", "쓰러진 등롱 확인",
                        "등롱이 쓰러져 있다.\n누군가 지나간 길이라면, 몬스터도 그 길을 알고 있겠지.")
                },
                Stories = new[]
                {
                    StoryEntrySeed.Basic("sub_broken_lantern_found", 10, "dlg_sub_broken_lantern_found")
                }
            },
            new SubStorySeed
            {
                QuestId = "quest_sub_highland_golem_trace",
                QuestName = "고지대의 발자국",
                Description = "바위 고지대의 Golem을 처치하고 고지대 길의 위험도를 낮춘다.",
                RequiredProgress = 20,
                RewardGold = 160,
                RewardExp = 120,
                Objectives = new[]
                {
                    ObjectiveSeed.Kill("obj_kill_highland_golem", "바위 고지대의 Golem을 처치한다.", ActorIdType.Golem_Normal, 1)
                },
                Dialogues = new[]
                {
                    DialogueSeed.Main("dlg_sub_highland_golem_trace_start", "고지대의 발자국 - 시작", "사냥꾼",
                        "고지대 길에 커다란 발자국이 새로 생겼어.\n돌이 움직인 흔적이라면 그냥 지나칠 수 없지."),
                    DialogueSeed.Monologue("dlg_sub_highland_golem_trace_done", "고지대 Golem 처치",
                        "바위가 멈췄다.\n이 길은 아직 거칠지만, 적어도 등 뒤에서 무너질 걱정은 줄었어.")
                },
                Stories = new[]
                {
                    StoryEntrySeed.Basic("sub_highland_golem_trace_done", 20, "dlg_sub_highland_golem_trace_done")
                }
            },
            new SubStorySeed
            {
                QuestId = "quest_sub_survivor_lost_pack",
                QuestName = "도망친 자의 짐",
                Description = "던전 입구 근처에서 생존자가 잃어버린 짐을 확인한다.",
                RequiredProgress = 30,
                RewardGold = 140,
                RewardExp = 70,
                Objectives = new[]
                {
                    ObjectiveSeed.Reach("obj_reach_survivor_pack", "던전 입구 근처의 잃어버린 짐을 확인한다.", "loc_survivor_lost_pack")
                },
                Dialogues = new[]
                {
                    DialogueSeed.Main("dlg_sub_survivor_lost_pack_start", "도망친 자의 짐 - 시작", "떠돌이 생존자",
                        "도망치면서 짐을 버렸어.\n중요한 건 아니지만, 그 안에 누가 같이 갔는지 적힌 쪽지가 있다."),
                    DialogueSeed.Monologue("dlg_sub_survivor_lost_pack_found", "생존자의 짐 확인",
                        "찢어진 짐이 남아 있다.\n안쪽으로 들어간 사람이 더 있었다는 뜻이다.")
                },
                Stories = new[]
                {
                    StoryEntrySeed.Basic("sub_survivor_lost_pack_found", 30, "dlg_sub_survivor_lost_pack_found")
                }
            }
        };

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
            quest.requiredStoryProgress = seed.RequiredProgress;
            quest.requiredQuestIds = seed.RequiredQuestIds?.ToList() ?? new List<string>();
            quest.autoAcceptOnNewGame = seed.AutoAcceptOnNewGame;
            quest.autoAcceptNextQuestIds = seed.AutoAcceptNextQuestIds?.ToList() ?? new List<string>();
            quest.objectives = seed.Objectives.Select(x => x.ToData()).ToList();
            quest.reward.gold = seed.RewardGold;
            quest.reward.exp = seed.RewardExp;
            quest.reward.items = seed.RewardItems?.ToList() ?? new List<QuestItemReward>();
            quest.isRepeatable = seed.IsRepeatable;
            quest.autoComplete = true;

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
            public bool IsRepeatable;
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
                IsRepeatable = quest.isRepeatable,
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

        private readonly struct ObjectiveSeed
        {
            private readonly string _objectiveId;
            private readonly string _description;
            private readonly QuestObjectiveType _type;
            private readonly int _targetId;
            private readonly string _targetStringId;
            private readonly int _requiredCount;

            private ObjectiveSeed(string objectiveId, string description, QuestObjectiveType type, int targetId, string targetStringId, int requiredCount)
            {
                _objectiveId = objectiveId;
                _description = description;
                _type = type;
                _targetId = targetId;
                _targetStringId = targetStringId;
                _requiredCount = requiredCount;
            }

            public static ObjectiveSeed Kill(string objectiveId, string description, ActorIdType actorId, int count)
                => new(objectiveId, description, QuestObjectiveType.MonsterKill, 0, actorId.ToActorId(), count);

            public static ObjectiveSeed Reach(string objectiveId, string description, string locationId)
                => new(objectiveId, description, QuestObjectiveType.ReachLocation, 0, locationId, 1);

            public static ObjectiveSeed FromDocument(StoryGeneratorObjective objective)
            {
                var data = objective.ToObjectiveData();
                return new ObjectiveSeed(
                    data.objectiveId,
                    data.description,
                    data.type,
                    data.targetId,
                    data.targetStringId,
                    data.requiredCount);
            }

            public QuestObjectiveData ToData() => new()
            {
                objectiveId = _objectiveId,
                description = _description,
                type = _type,
                targetId = _targetId,
                targetStringId = _targetStringId,
                requiredCount = _requiredCount
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

            public static DialogueSeed Main(string graphId, string graphName, string speakerId, string text)
                => new(graphId, graphName, DialogueChannel.Main, speakerId, text);

            public static DialogueSeed Monologue(string graphId, string graphName, string text)
                => new(graphId, graphName, DialogueChannel.Monologue, "Bokusei", text);

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
            public readonly string DialogueGraphId;

            private StoryEntrySeed(string storyId, int requiredProgress, string dialogueGraphId)
            {
                StoryId = storyId;
                RequiredProgress = requiredProgress;
                DialogueGraphId = dialogueGraphId;
            }

            public static StoryEntrySeed Basic(string storyId, int requiredProgress, string dialogueGraphId)
                => new(storyId, requiredProgress, dialogueGraphId);

            public static StoryEntrySeed FromDocument(StoryGeneratorEntry entry)
                => new(entry.storyId, entry.requiredProgress, entry.dialogueGraphId);
        }
    }
}
#endif
