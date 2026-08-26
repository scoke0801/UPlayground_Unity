using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Path;
using UPlayGround.Data.Quest;
using UPlayGround.Data.Story;
using UPlayGround.Dialogue;
using UPlayGround.FlowGraph;
using UPlayGround.TriggerSystem;
using UPlayGround.UI;

namespace UPlayGround.Editor.Quest
{
    /// <summary>
    /// 퀘스트 목표의 완료 신호와 마커 지점이 실제로 어딘가에서 공급되는지 검사한다.
    ///
    /// 목표의 대상 ID는 퀘스트, NPC 데이터, 대화 액션, 트리거 액션, 흐름 그래프, 씬 마커에
    /// 각각 손으로 적힌다. 한 글자만 어긋나도 오류 없이 조용히 어긋나서, 목표가 영원히
    /// 완료되지 않거나 마커가 뜨지 않는다. 컨텐츠의 최대 결함이 진행 불능이므로 저작 단계에서 잡는다.
    /// </summary>
    internal static class QuestObjectiveWiringValidator
    {
        private const string MenuPath = "UPlayGround/콘텐츠/퀘스트/퀘스트 목표 배선 검증";

        /// <summary> 진행 수량이 누적되지 않고 첫 알림에 즉시 완료되는 목표 타입. </summary>
        private static bool IsSingleShotType(QuestObjectiveType type)
            => type is QuestObjectiveType.ReachLocation
                or QuestObjectiveType.StoryEvent
                or QuestObjectiveType.StoryProgress;

        [MenuItem(MenuPath)]
        private static void Validate()
        {
            var report = new Report();

            HashSet<string> locationSignals = CollectLocationSignals();
            HashSet<string> storyEventSignals = CollectStoryEventSignals();
            HashSet<string> markerPoints = CollectMarkerPoints();
            HashSet<int> knownItemIds = CollectKnownItemIds();

            List<QuestSO> quests = LoadAll<QuestSO>();
            var questIds = new HashSet<string>();
            foreach (QuestSO quest in quests)
            {
                if (!string.IsNullOrWhiteSpace(quest.questId))
                    questIds.Add(quest.questId);
            }

            foreach (QuestSO quest in quests)
            {
                ValidateQuest(quest, questIds, locationSignals, storyEventSignals, markerPoints, knownItemIds, report);
            }

            report.Flush(quests.Count);
        }

        private static void ValidateQuest(
            QuestSO quest,
            HashSet<string> questIds,
            HashSet<string> locationSignals,
            HashSet<string> storyEventSignals,
            HashSet<string> markerPoints,
            HashSet<int> knownItemIds,
            Report report)
        {
            foreach (string requiredId in quest.requiredQuestIds)
            {
                if (!string.IsNullOrWhiteSpace(requiredId) && !questIds.Contains(requiredId))
                    report.Error(quest, $"선행 퀘스트 '{requiredId}'가 없어 이 퀘스트를 수락할 수 없습니다.");
            }

            var seenObjectiveIds = new HashSet<string>();
            foreach (QuestObjectiveData objective in quest.objectives)
            {
                if (string.IsNullOrWhiteSpace(objective.objectiveId))
                {
                    report.Error(quest, "목표 ID가 비어 있습니다.");
                    continue;
                }

                if (!seenObjectiveIds.Add(objective.objectiveId))
                    report.Error(quest, $"목표 ID '{objective.objectiveId}'가 중복입니다.");

                ValidateRevealOrder(quest, objective, report);
                ValidateCompletionSignal(quest, objective, locationSignals, storyEventSignals, knownItemIds, report);
                ValidateMarker(quest, objective, markerPoints, report);

                if (objective.requiredCount > 1 && IsSingleShotType(objective.type))
                {
                    report.Warning(quest,
                        $"목표 '{objective.objectiveId}'는 {objective.type} 타입이라 수량이 누적되지 않고 "
                        + $"첫 알림에 즉시 완료됩니다. 필요 수량 {objective.requiredCount}는 무시됩니다. "
                        + "수량이 필요하면 ItemCollect 또는 MonsterKill로 표현하세요.");
                }
            }
        }

        private static void ValidateRevealOrder(QuestSO quest, QuestObjectiveData objective, Report report)
        {
            foreach (string precedingId in objective.revealAfterObjectiveIds)
            {
                if (string.IsNullOrWhiteSpace(precedingId))
                    continue;

                if (!quest.objectives.Exists(other => other.objectiveId == precedingId))
                {
                    report.Error(quest,
                        $"목표 '{objective.objectiveId}'의 선행 목표 '{precedingId}'가 이 퀘스트에 없어 "
                        + "이 목표는 영원히 표시되지 않습니다.");
                }
            }
        }

        private static void ValidateCompletionSignal(
            QuestSO quest,
            QuestObjectiveData objective,
            HashSet<string> locationSignals,
            HashSet<string> storyEventSignals,
            HashSet<int> knownItemIds,
            Report report)
        {
            switch (objective.type)
            {
                case QuestObjectiveType.ReachLocation:
                    RequireSignal(quest, objective, locationSignals, "위치 도달", report);
                    break;

                case QuestObjectiveType.StoryEvent:
                    RequireSignal(quest, objective, storyEventSignals, "서사 이벤트", report);
                    break;

                case QuestObjectiveType.MonsterKill:
                    if (string.IsNullOrWhiteSpace(objective.targetStringId) && objective.targetId <= 0)
                        report.Error(quest, $"목표 '{objective.objectiveId}'에 처치 대상이 지정되지 않았습니다.");
                    break;

                case QuestObjectiveType.ItemCollect:
                case QuestObjectiveType.ItemDeliver:
                case QuestObjectiveType.ItemUse:
                    if (objective.targetId <= 0)
                        report.Error(quest, $"목표 '{objective.objectiveId}'에 아이템이 지정되지 않았습니다.");
                    else if (!knownItemIds.Contains(objective.targetId))
                        report.Error(quest,
                            $"목표 '{objective.objectiveId}'의 아이템 {objective.targetId}가 ItemDatabase에 없습니다.");

                    if (objective.type == QuestObjectiveType.ItemDeliver && objective.npcId <= 0)
                    {
                        report.Error(quest,
                            $"목표 '{objective.objectiveId}'는 전달 대상 NPC 번호가 없어 완료될 수 없습니다. "
                            + "전달 액션과 같은 번호를 지정하거나, 대화 종료 시점의 StoryEvent로 바꾸세요.");
                    }
                    break;
            }
        }

        private static void RequireSignal(
            QuestSO quest,
            QuestObjectiveData objective,
            HashSet<string> signals,
            string signalName,
            Report report)
        {
            if (string.IsNullOrWhiteSpace(objective.targetStringId))
            {
                report.Error(quest, $"목표 '{objective.objectiveId}'에 {signalName} ID가 비어 있습니다.");
                return;
            }

            if (!signals.Contains(objective.targetStringId))
            {
                report.Error(quest,
                    $"목표 '{objective.objectiveId}'의 {signalName} '{objective.targetStringId}'를 "
                    + "알려주는 곳이 없어 이 목표는 완료되지 않습니다.");
            }
        }

        private static void ValidateMarker(
            QuestSO quest,
            QuestObjectiveData objective,
            HashSet<string> markerPoints,
            Report report)
        {
            string markerLocationId = QuestObjectiveMarker.ResolveLocationId(objective);
            if (string.IsNullOrWhiteSpace(markerLocationId) || markerPoints.Contains(markerLocationId))
                return;

            // 지역 씬 파일이 저장소에 없어 열려 있지 않은 씬의 마커는 확인할 수 없다. 그래서 경고로 남긴다.
            report.Warning(quest,
                $"목표 '{objective.objectiveId}'의 마커 지점 '{markerLocationId}'를 제공하는 곳을 찾지 못했습니다. "
                + "해당 씬이 열려 있지 않으면 정상일 수 있습니다.");
        }

        // ── 신호·지점 수집 ──────────────────────────────────────────

        private static HashSet<string> CollectLocationSignals()
        {
            var signals = new HashSet<string>();

            foreach (NotifyLocationTriggerActionSO action in LoadAll<NotifyLocationTriggerActionSO>())
                AddSerializedString(signals, action, "_locationId");

            foreach (NotifyQuestLocationDialogueActionSO action in LoadAll<NotifyQuestLocationDialogueActionSO>())
                AddSerializedString(signals, action, "_locationId");

            return signals;
        }

        private static HashSet<string> CollectStoryEventSignals()
        {
            var signals = new HashSet<string>();

            foreach (NotifyQuestStoryEventDialogueActionSO action in LoadAll<NotifyQuestStoryEventDialogueActionSO>())
                AddSerializedString(signals, action, "_eventId");

            foreach (TurnInQuestItemsDialogueActionSO action in LoadAll<TurnInQuestItemsDialogueActionSO>())
                AddSerializedString(signals, action, "_storyEventId");

            foreach (FlowGraphSO graph in LoadAll<FlowGraphSO>())
            {
                foreach (FlowNode node in graph.nodes)
                {
                    if (node is NotifyQuestStoryEventNode storyEventNode
                        && !string.IsNullOrWhiteSpace(storyEventNode.eventId))
                        signals.Add(storyEventNode.eventId);
                }
            }

            return signals;
        }

        private static HashSet<string> CollectMarkerPoints()
        {
            var points = new HashSet<string>();

            foreach (NpcActorSO npc in LoadAll<NpcActorSO>())
            {
                if (!string.IsNullOrWhiteSpace(npc.questMarkerLocationId))
                    points.Add(npc.questMarkerLocationId);
            }

            foreach (RecruitmentEncounterDefinitionSO encounter in LoadAll<RecruitmentEncounterDefinitionSO>())
            {
                if (!string.IsNullOrWhiteSpace(encounter.QuestMarkerLocationId))
                    points.Add(encounter.QuestMarkerLocationId);
            }

            MinimapMarkerRegistrar[] sceneMarkers = UnityEngine.Object.FindObjectsByType<MinimapMarkerRegistrar>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (MinimapMarkerRegistrar marker in sceneMarkers)
            {
                if (!string.IsNullOrWhiteSpace(marker.LocationId))
                    points.Add(marker.LocationId);
            }

            return points;
        }

        private static HashSet<int> CollectKnownItemIds()
        {
            var itemIds = new HashSet<int>();
            foreach (ItemDatabase database in LoadAll<ItemDatabase>())
            {
                foreach (var item in database.AllItems)
                {
                    if (item != null)
                        itemIds.Add(item.itemId);
                }
            }

            return itemIds;
        }

        private static void AddSerializedString(HashSet<string> target, UnityEngine.Object asset, string fieldName)
        {
            var serialized = new SerializedObject(asset);
            SerializedProperty property = serialized.FindProperty(fieldName);
            if (property != null && !string.IsNullOrWhiteSpace(property.stringValue))
                target.Add(property.stringValue);
        }

        private static List<T> LoadAll<T>() where T : UnityEngine.Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            var results = new List<T>(guids.Length);
            foreach (string guid in guids)
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null)
                    results.Add(asset);
            }

            return results;
        }

        private sealed class Report
        {
            private readonly StringBuilder _log = new();
            private int _errorCount;
            private int _warningCount;

            public void Error(QuestSO quest, string message)
            {
                _errorCount++;
                Debug.LogError($"[퀘스트 배선] {quest.questId}: {message}", quest);
                _log.AppendLine($"[오류] {quest.questId}: {message}");
            }

            public void Warning(QuestSO quest, string message)
            {
                _warningCount++;
                Debug.LogWarning($"[퀘스트 배선] {quest.questId}: {message}", quest);
                _log.AppendLine($"[경고] {quest.questId}: {message}");
            }

            public void Flush(int questCount)
            {
                string summary = _errorCount == 0 && _warningCount == 0
                    ? $"퀘스트 {questCount}개를 검사했고 문제를 찾지 못했습니다."
                    : $"퀘스트 {questCount}개 중 오류 {_errorCount}건, 경고 {_warningCount}건을 찾았습니다.\n"
                      + "자세한 내용은 Console을 확인하세요.";

                Debug.Log($"[퀘스트 배선] {summary}");
                EditorUtility.DisplayDialog("퀘스트 목표 배선 검증", summary, "확인");
            }
        }
    }
}
