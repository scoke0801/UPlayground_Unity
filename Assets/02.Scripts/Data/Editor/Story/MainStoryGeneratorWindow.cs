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
    /// Assets/docs/story의 메인 스토리 초안을 기준으로 기본 Quest/Dialogue/StoryEntry 에셋을 생성한다.
    /// 생성 결과는 초안 데이터이므로 배치, Addressables, 트리거 연결은 씬에서 별도로 처리한다.
    /// </summary>
    public class MainStoryGeneratorWindow : EditorWindow
    {
        private const string QUEST_ROOT = "Assets/10.Datas/Quest/Generated/MainStory";
        private const string DIALOGUE_ROOT = "Assets/10.Datas/Dialogue/Generated/MainStory";
        private const string STORY_ROOT = "Assets/10.Datas/Story/Generated/MainStory";
        private const string QUEST_DB_SCAN_ROOT = "Assets/10.Datas/Quest";

        private Vector2 _scroll;
        private bool _overwriteExisting = true;
        private bool _refreshQuestDatabase = true;

        private static readonly StorySeed[] Seeds =
        {
            new StorySeed
            {
                QuestId = "quest_main_001",
                QuestName = "끊긴 길",
                Description = "마을 밖 길이 끊기기 시작했다. 중앙 호수 주변을 조사해 원인을 확인한다.",
                RequiredProgress = 0,
                RewardGold = 100,
                RewardExp = 60,
                Objectives = new[]
                {
                    ObjectiveSeed.Reach("obj_reach_lake", "중앙 호수 주변을 조사한다.", "loc_central_lake")
                },
                Dialogues = new[]
                {
                    DialogueSeed.Main("dlg_quest_main_001_start", "끊긴 길 - 시작", "촌장",
                        "마을 밖 길이 하나씩 끊기고 있네.\n호수 근처에서 돌아오지 못한 사람들이 있어. 먼저 그 길이 아직 살아 있는지 확인해 주게."),
                    DialogueSeed.Monologue("dlg_field_lake_first_arrive", "중앙 호수 발견",
                        "호수 가운데 붉은 나무가 보인다.\n저걸 기준으로 삼으면 어느 길에서든 돌아올 수 있겠어.")
                },
                Stories = new[]
                {
                    StoryEntrySeed.Basic("field_lake_first_arrive", 0, "dlg_field_lake_first_arrive")
                }
            },
            new StorySeed
            {
                QuestId = "quest_main_002",
                QuestName = "거미줄에 막힌 숲",
                Description = "거미 숲 깊은 곳의 Spider Queen을 처치해 막힌 숲길을 연다.",
                RequiredProgress = 10,
                RewardGold = 180,
                RewardExp = 150,
                Objectives = new[]
                {
                    ObjectiveSeed.Kill("obj_kill_spider_queen", "거미 숲 깊은 곳의 Spider Queen을 처치한다.", ActorIdType.SpiderQueen_1)
                },
                Dialogues = new[]
                {
                    DialogueSeed.Main("dlg_quest_main_002_start", "거미줄에 막힌 숲 - 시작", "사냥꾼",
                        "작은 거미가 문제가 아니야.\n숲 안쪽에 둥지를 튼 큰 놈이 길을 완전히 막고 있어."),
                    DialogueSeed.Monologue("dlg_field_spider_queen_defeat", "Spider Queen 처치",
                        "숲 안쪽 길이 보인다.\n이제 이쪽으로도 호수 반대편에 갈 수 있겠어.")
                },
                Stories = new[]
                {
                    StoryEntrySeed.Basic("monster_spider_queen_defeat", 10, "dlg_field_spider_queen_defeat")
                }
            },
            new StorySeed
            {
                QuestId = "quest_main_003",
                QuestName = "움직이는 바위",
                Description = "바위 고지대나 숲 경계의 중형 몬스터를 처치하고 던전 방향 단서를 확보한다.",
                RequiredProgress = 10,
                RewardGold = 180,
                RewardExp = 130,
                Objectives = new[]
                {
                    ObjectiveSeed.Kill("obj_kill_highland_guardian", "바위 고지대의 Golem을 처치한다.", ActorIdType.Golem_Normal)
                },
                Dialogues = new[]
                {
                    DialogueSeed.Main("dlg_quest_main_003_start", "움직이는 바위 - 시작", "사냥꾼",
                        "고지대에서 돌이 움직이는 소리가 난다고들 하지.\n그쪽을 정리하면 던전 방향도 내려다볼 수 있을 거야."),
                    DialogueSeed.Monologue("dlg_field_highland_guardian_defeat", "고지대 강적 처치",
                        "고지대가 조용해졌다.\n저 아래, 석등이 이어지는 길 끝에 입구 같은 게 보인다.")
                },
                Stories = new[]
                {
                    StoryEntrySeed.Basic("monster_highland_guardian_defeat", 10, "dlg_field_highland_guardian_defeat")
                }
            },
            new StorySeed
            {
                QuestId = "quest_main_004",
                QuestName = "등롱이 가리키는 곳",
                Description = "석등과 목조 등롱이 이어지는 길 끝을 조사해 던전 입구의 위치를 확인한다.",
                RequiredProgress = 10,
                RewardGold = 150,
                RewardExp = 90,
                Objectives = new[]
                {
                    ObjectiveSeed.Reach("obj_reach_lantern_path_end", "석등 길 끝을 조사한다.", "loc_lantern_path_end")
                },
                Dialogues = new[]
                {
                    DialogueSeed.Main("dlg_quest_main_004_start", "등롱이 가리키는 곳 - 시작", "길잡이",
                        "호수 가운데 붉은 나무가 보이면 아직 길을 잃은 건 아니야.\n던전 쪽 길은 석등이 이어지는 방향을 보면 된다."),
                    DialogueSeed.Monologue("dlg_field_lantern_path_end", "석등 길 끝",
                        "등롱과 석등이 같은 방향으로 이어져 있다.\n길을 숨기려던 게 아니라, 잊지 않으려고 세워 둔 표식 같아.")
                },
                Stories = new[]
                {
                    StoryEntrySeed.Basic("field_lantern_path_end", 20, "dlg_field_lantern_path_end")
                }
            },
            new StorySeed
            {
                QuestId = "quest_main_005",
                QuestName = "던전 입구의 Lich",
                Description = "던전 입구를 막고 있는 Lich를 처치하고 내부로 진입할 길을 연다.",
                RequiredProgress = 30,
                RewardGold = 300,
                RewardExp = 300,
                RequiredQuestIds = new[] { "quest_main_001" },
                Objectives = new[]
                {
                    ObjectiveSeed.Kill("obj_kill_lich", "던전 입구의 Lich를 처치한다.", ActorIdType.Lich_Normal)
                },
                Dialogues = new[]
                {
                    DialogueSeed.Main("dlg_quest_main_005_start", "던전 입구의 Lich - 시작", "촌장",
                        "그자는 입구 앞에 서 있었다고 했네.\n뼈들이 그 뒤를 따랐고, 아무도 안으로 들어가지 못했다고 하더군."),
                    DialogueSeed.Monologue("dlg_dungeon_entrance_arrive", "던전 입구 도달",
                        "던전 입구가 맞다.\n여길 지나가려면 저걸 먼저 쓰러뜨려야 해."),
                    DialogueSeed.Monologue("dlg_dungeon_entrance_open", "던전 입구 개방",
                        "입구를 막던 기운이 사라졌다.\n이제 안으로 들어갈 수 있다.")
                },
                Stories = new[]
                {
                    StoryEntrySeed.Basic("dungeon_entrance_arrive", 30, "dlg_dungeon_entrance_arrive"),
                    StoryEntrySeed.Basic("dungeon_entrance_open", 40, "dlg_dungeon_entrance_open")
                }
            }
        };

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
                "Assets/docs/story/MAIN_STORY.md의 STORY_GENERATOR_MAIN 블록을 기준으로 QuestSO, DialogueGraphSO, StoryEntrySO 초안을 생성합니다.",
                MessageType.Info);

            var seeds = GetSeeds(out var sourceMessage);
            EditorGUILayout.HelpBox(sourceMessage, MessageType.None);

            _overwriteExisting = EditorGUILayout.ToggleLeft("기존 생성 에셋 갱신", _overwriteExisting);
            _refreshQuestDatabase = EditorGUILayout.ToggleLeft("생성 후 QuestDatabase 갱신", _refreshQuestDatabase);

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("생성/갱신", GUILayout.Height(32)))
                GenerateAll();
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

        private void GenerateAll()
        {
            EnsureFolders();
            var seeds = GetSeeds(out var sourceMessage);
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

            if (_refreshQuestDatabase)
                RefreshQuestDatabase();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("생성 완료", "메인 스토리 기본 에셋 생성/갱신이 완료되었습니다.", "확인");
        }

        private static StorySeed[] GetSeeds(out string sourceMessage)
        {
            if (StoryGeneratorMarkdownLoader.TryLoadMain(out var document, out var error))
            {
                sourceMessage = $"문서 데이터 사용: {StoryGeneratorMarkdownLoader.StoryDocPath} ({StoryGeneratorMarkdownLoader.Summary(document)})";
                return document.quests.Select(StorySeed.FromDocument).ToArray();
            }

            sourceMessage = $"내장 기본값 사용: {error}";
            return Seeds;
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
            quest.questDescription = seed.Description;
            quest.requiredStoryProgress = seed.RequiredProgress;
            quest.requiredQuestIds = seed.RequiredQuestIds?.ToList() ?? new List<string>();
            quest.objectives = seed.Objectives.Select(x => x.ToData()).ToList();
            quest.reward.gold = seed.RewardGold;
            quest.reward.exp = seed.RewardExp;
            quest.reward.items.Clear();
            quest.isRepeatable = false;
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
            public string Description;
            public int RequiredProgress;
            public int RewardGold;
            public int RewardExp;
            public string[] RequiredQuestIds = System.Array.Empty<string>();
            public ObjectiveSeed[] Objectives;
            public DialogueSeed[] Dialogues;
            public StoryEntrySeed[] Stories;

            public static StorySeed FromDocument(StoryGeneratorQuest quest) => new()
            {
                QuestId = quest.questId,
                QuestName = quest.questName,
                Description = quest.description,
                RequiredProgress = quest.requiredProgress,
                RewardGold = quest.rewardGold,
                RewardExp = quest.rewardExp,
                RequiredQuestIds = quest.requiredQuestIds ?? System.Array.Empty<string>(),
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

            public static ObjectiveSeed Kill(string objectiveId, string description, ActorIdType actorId)
                => new(objectiveId, description, QuestObjectiveType.MonsterKill, (int)actorId, string.Empty, 1);

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
