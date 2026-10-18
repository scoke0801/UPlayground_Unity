using System;
using System.Collections.Generic;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Save
{
    /// <summary>별도 슬롯으로 읽은 저장의 구 사건을 새 도입과 분리한다. 원본 슬롯에는 적용하지 않는다.</summary>
    public static class RedTreeSaveMigration
    {
        public const string ContentVersionFlag = "lake.red_tree.content.v1";
        public const string ArrivalQuestId = "quest_main_red_tree_s00";

        private static readonly HashSet<string> RetiredQuestIds = new(StringComparer.Ordinal)
        {
            "quest_sub_lake_missing_villagers",
            "quest_sub_lake_rescue_honoka",
            "quest_sub_lake_rescue_lian",
            "quest_sub_lake_follow_tracks",
            "quest_main_lake_shrine",
            "quest_sub_hunter_skeleton_patrol",
            "quest_sub_penny_lantern_order",
            "quest_sub_morgan_west_road",
            "quest_sub_hazel_moved_herb",
            "quest_sub_guide_broken_lantern",
        };

        /// <summary>입력을 모두 확인한 뒤 복사본만 도착 스폰으로 옮긴다. 이미 이관한 진행은 다시 초기화하지 않는다.</summary>
        public static bool TryApplyToCopy(
            GameSaveData copy,
            string resumeMapId,
            string resumeSceneName,
            out string error)
        {
            error = null;
            if (copy == null || copy.party == null)
            {
                error = "저장 또는 파티 정보가 없습니다.";
                return false;
            }
            if (copy.flags?.flags != null
                && copy.flags.flags.TryGetValue(ContentVersionFlag, out bool migrated)
                && migrated)
            {
                return true;
            }
            if (string.IsNullOrWhiteSpace(resumeMapId) || string.IsNullOrWhiteSpace(resumeSceneName))
            {
                error = "확인된 재개 맵과 씬이 필요합니다.";
                return false;
            }
            if (!CharacterActorTypeUtility.TryParsePersistentName(
                    copy.party.storyProtagonistType, out CharacterActorType protagonist)
                || protagonist is not CharacterActorType.Raon and not CharacterActorType.Arin)
            {
                // 활성 캐릭터를 주인공으로 추정하면 이미 교체한 구 저장의 시점 인물을 바꿔 버린다.
                error = "선택 주인공을 확인할 수 없어 이관하지 않았습니다.";
                return false;
            }

            copy.quest ??= new QuestSaveData();
            copy.quest.activeQuests?.RemoveAll(entry => entry != null && RetiredQuestIds.Contains(entry.questId));
            if (RetiredQuestIds.Contains(copy.quest.trackedQuestId ?? string.Empty))
            {
                copy.quest.trackedQuestId = ArrivalQuestId;
                copy.quest.questTrackingSuppressed = false;
            }
            copy.party.mapId = resumeMapId;
            copy.party.loadSceneName = resumeSceneName;
            copy.party.hasLocation = false;
            copy.flags ??= new FlagSaveData();
            copy.flags.flags ??= new Dictionary<string, bool>();
            copy.flags.flags[ContentVersionFlag] = true;
            return true;
        }
    }
}
