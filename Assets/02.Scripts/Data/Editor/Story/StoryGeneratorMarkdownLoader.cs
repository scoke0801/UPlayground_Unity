#if UNITY_EDITOR
using System;
using System.IO;
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
    /// 스토리 문서의 생성기용 JSON 블록을 읽어 에디터 생성기에서 사용할 DTO로 변환한다.
    /// 일반 마크다운 문장은 자유롭게 수정하고, 자동 생성에 필요한 값은 marker 사이 JSON만 갱신한다.
    /// </summary>
    internal static class StoryGeneratorMarkdownLoader
    {
        public const string MainStoryDocPath = "Assets/docs/cycle/CYCLE_STORY_PLOT.md";
        public const string SubStoryDocPath = "Assets/docs/cycle/CYCLE_STORY_PLOT.md";
        public const string MainBegin = "<!-- STORY_GENERATOR_MAIN_BEGIN -->";
        public const string MainEnd = "<!-- STORY_GENERATOR_MAIN_END -->";
        public const string SubBegin = "<!-- STORY_GENERATOR_SUB_BEGIN -->";
        public const string SubEnd = "<!-- STORY_GENERATOR_SUB_END -->";

        public static bool TryLoadMain(out StoryGeneratorDocument document, out string error)
            => TryLoadBlock(MainStoryDocPath, MainBegin, MainEnd, out document, out error);

        public static bool TryLoadSub(out StoryGeneratorDocument document, out string error)
            => TryLoadBlock(SubStoryDocPath, SubBegin, SubEnd, out document, out error);

        private static bool TryLoadBlock(
            string storyDocPath,
            string beginMarker,
            string endMarker,
            out StoryGeneratorDocument document,
            out string error)
        {
            document = null;
            error = string.Empty;

            var fullPath = Path.GetFullPath(storyDocPath);
            if (!File.Exists(fullPath))
            {
                error = $"스토리 문서를 찾을 수 없습니다: {storyDocPath}";
                return false;
            }

            var text = File.ReadAllText(fullPath);
            var begin = text.IndexOf(beginMarker, StringComparison.Ordinal);
            var end = text.IndexOf(endMarker, StringComparison.Ordinal);
            if (begin < 0 || end < 0 || end <= begin)
            {
                error = $"생성기 marker를 찾을 수 없습니다: {beginMarker}";
                return false;
            }

            begin += beginMarker.Length;
            var json = text.Substring(begin, end - begin).Trim();
            json = StripFence(json);

            try
            {
                document = JsonUtility.FromJson<StoryGeneratorDocument>(json);
            }
            catch (Exception e)
            {
                error = $"JSON 파싱 실패: {e.Message}";
                return false;
            }

            if (document == null || document.quests == null || document.quests.Length == 0)
            {
                error = "생성기 JSON에 quests 항목이 없습니다.";
                return false;
            }

            return true;
        }

        private static string StripFence(string text)
        {
            if (!text.StartsWith("```", StringComparison.Ordinal)) return text;

            var firstLineEnd = text.IndexOf('\n');
            if (firstLineEnd < 0) return text;

            var lastFence = text.LastIndexOf("```", StringComparison.Ordinal);
            if (lastFence <= firstLineEnd) return text;

            return text.Substring(firstLineEnd + 1, lastFence - firstLineEnd - 1).Trim();
        }

        public static QuestObjectiveData ToObjectiveData(this StoryGeneratorObjective objective)
        {
            var data = new QuestObjectiveData
            {
                objectiveId = objective.objectiveId,
                description = objective.description,
                npcId = objective.npcId,
                targetStringId = objective.targetStringId,
                requiredCount = Mathf.Max(1, objective.requiredCount),
                revealAfterObjectiveIds = (objective.revealAfterObjectiveIds ?? Array.Empty<string>()).ToList()
            };

            if (!System.Enum.TryParse(objective.type, out QuestObjectiveType objectiveType))
                objectiveType = QuestObjectiveType.ReachLocation;
            data.type = objectiveType;

            if (objectiveType == QuestObjectiveType.MonsterKill)
            {
                data.targetId = 0;
                data.targetStringId = ResolveActorId(objective.actorId);
            }
            else
                data.targetId = objective.targetId;

            return data;
        }

        public static QuestItemReward ToQuestItemReward(this StoryGeneratorItemReward reward)
        {
            return new QuestItemReward
            {
                itemId = reward.itemId,
                count = Mathf.Max(1, reward.count)
            };
        }

        public static DialogueChannel ResolveChannel(string channel)
            => System.Enum.TryParse(channel, out DialogueChannel result) ? result : DialogueChannel.Main;

        public static StoryTriggerMode ResolveTriggerMode(
            string value,
            StoryTriggerMode fallback = StoryTriggerMode.NpcTalk)
        {
            return !string.IsNullOrWhiteSpace(value)
                   && Enum.TryParse(value, true, out StoryTriggerMode mode)
                   && Enum.IsDefined(typeof(StoryTriggerMode), mode)
                ? mode
                : fallback;
        }

        private static string ResolveActorId(string actorId)
        {
            if (string.IsNullOrWhiteSpace(actorId)) return string.Empty;
            return System.Enum.TryParse(actorId, out ActorIdType result)
                ? result.ToActorId()
                : actorId.Trim();
        }

        public static string Summary(StoryGeneratorDocument document)
            => document == null ? "문서 데이터 없음" : $"{document.quests?.Length ?? 0}개 퀘스트 / {document.quests?.Sum(q => q.dialogues?.Length ?? 0) ?? 0}개 대화 / {document.quests?.Sum(q => q.stories?.Length ?? 0) ?? 0}개 스토리";
    }

    [Serializable]
    internal class StoryGeneratorDocument
    {
        public StoryGeneratorQuest[] quests;
    }

    [Serializable]
    internal class StoryGeneratorQuest
    {
        public string questId;
        public string questName;
        public string shortSummary;
        public string description;
        public int requiredProgress;
        public int rewardGold;
        public int rewardExp;
        public StoryGeneratorItemReward[] rewardItems;
        public bool isContentEnabled = true;
        public bool isRepeatable;
        public bool autoComplete = true;
        public bool autoAcceptOnNewGame;
        public string[] requiredQuestIds;
        public string[] autoAcceptNextQuestIds;
        public StoryGeneratorObjective[] objectives;
        public StoryGeneratorDialogue[] dialogues;
        public StoryGeneratorEntry[] stories;
    }

    [Serializable]
    internal class StoryGeneratorItemReward
    {
        public int itemId;
        public int count = 1;
    }

    [Serializable]
    internal class StoryGeneratorObjective
    {
        public string objectiveId;
        public string description;
        public string type;
        public string actorId;
        public int targetId;
        public int npcId;
        public string targetStringId;
        public int requiredCount = 1;
        public string[] revealAfterObjectiveIds;
    }

    [Serializable]
    internal class StoryGeneratorDialogue
    {
        public string graphId;
        public string graphName;
        public string channel = "Main";
        public string speakerId;
        public string text;
        public StoryGeneratorDialogueLine[] lines;
    }

    [Serializable]
    internal class StoryGeneratorDialogueLine
    {
        public string channel;
        public string speakerId;
        public string text;
    }

    [Serializable]
    internal class StoryGeneratorEntry
    {
        public string storyId;
        public int requiredProgress;
        public int maxProgressExclusive;
        public string triggerMode = "NpcTalk";
        public string dialogueGraphId;
    }
}
#endif
