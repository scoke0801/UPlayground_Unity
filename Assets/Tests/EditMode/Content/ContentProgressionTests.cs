using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Quest;
using UPlayGround.Story;

namespace UPlayGround.Content.Tests
{
    public sealed class StoryPlaybackTrackerTests
    {
        [Test]
        public void 시작만으로는_완료되지_않는다()
        {
            var tracker = new StoryPlaybackTracker();

            Assert.IsTrue(tracker.TryBegin("story.test"));
            Assert.IsTrue(tracker.IsPlaying("story.test"));
            Assert.IsFalse(tracker.IsCompleted("story.test"));
            Assert.IsFalse(tracker.CanBegin("story.test"));
        }

        [Test]
        public void 정상_종료된_스토리만_완료된다()
        {
            var tracker = new StoryPlaybackTracker();
            tracker.TryBegin("story.test");

            Assert.IsTrue(tracker.Complete("story.test"));
            Assert.IsFalse(tracker.IsPlaying("story.test"));
            Assert.IsTrue(tracker.IsCompleted("story.test"));
            Assert.IsFalse(tracker.CanBegin("story.test"));
        }

        [Test]
        public void 취소된_스토리는_다시_시작할_수_있다()
        {
            var tracker = new StoryPlaybackTracker();
            tracker.TryBegin("story.test");

            Assert.IsTrue(tracker.Cancel("story.test"));
            Assert.IsTrue(tracker.CanBegin("story.test"));
            Assert.IsTrue(tracker.TryBegin("story.test"));
        }

        [Test]
        public void 로드는_재생중_상태를_버리고_완료_목록만_복원한다()
        {
            var tracker = new StoryPlaybackTracker();
            tracker.TryBegin("story.playing");

            tracker.RestoreCompleted(new[] { "story.completed", "", null });

            Assert.IsFalse(tracker.IsPlaying("story.playing"));
            Assert.IsTrue(tracker.CanBegin("story.playing"));
            Assert.IsTrue(tracker.IsCompleted("story.completed"));
        }
    }

    public sealed class QuestObjectiveVisibilityTests
    {
        private QuestSO _quest;
        private QuestObjectiveData _first;
        private QuestObjectiveData _second;

        [SetUp]
        public void SetUp()
        {
            _quest = ScriptableObject.CreateInstance<QuestSO>();
            _first = new QuestObjectiveData
            {
                objectiveId = "first",
                requiredCount = 1,
            };
            _second = new QuestObjectiveData
            {
                objectiveId = "second",
                requiredCount = 1,
                revealAfterObjectiveIds = new List<string> { "first" },
            };
            _quest.objectives = new List<QuestObjectiveData> { _first, _second };
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_quest);
        }

        [Test]
        public void 선행_목표_완료_전에는_후속_목표를_숨긴다()
        {
            var runtime = new QuestRuntimeData(_quest);

            Assert.IsTrue(runtime.IsObjectiveVisible(_first));
            Assert.IsFalse(runtime.IsObjectiveVisible(_second));
        }

        [Test]
        public void 숨겨진_목표의_진행도도_먼저_누적된다()
        {
            var runtime = new QuestRuntimeData(_quest);
            runtime.SetProgress("second", 1);

            Assert.IsFalse(runtime.IsObjectiveVisible(_second));
            Assert.IsTrue(runtime.IsObjectiveComplete(_second));

            runtime.SetProgress("first", 1);

            Assert.IsTrue(runtime.IsObjectiveVisible(_second));
            Assert.IsTrue(runtime.IsObjectiveComplete(_second));
        }

        [Test]
        public void 완료_기록_화면은_모든_목표를_표시할_수_있다()
        {
            Assert.IsTrue(QuestObjectiveVisibility.IsVisible(
                _quest,
                null,
                _second,
                revealAll: true));
        }
    }

    public sealed class LakeMainQuestLineAssetTests
    {
        private const string QuestDatabasePath = "Assets/10.Datas/Quest/QuestDatabase.asset";
        private const string OpeningQuestPath =
            "Assets/10.Datas/Quest/Generated/SubStory/quest_sub_lake_missing_villagers.asset";

        private static readonly string[] MainQuestPaths =
        {
            OpeningQuestPath,
            "Assets/10.Datas/Quest/Generated/SubStory/quest_sub_lake_rescue_hwarin.asset",
            "Assets/10.Datas/Quest/Generated/SubStory/quest_sub_lake_rescue_lian.asset",
            "Assets/10.Datas/Quest/Generated/SubStory/quest_sub_lake_follow_tracks.asset",
        };

        [Test]
        public void 안내인에서_시작하는_호숫가_퀘스트라인은_모두_메인으로_등록된다()
        {
            QuestDatabase database = AssetDatabase.LoadAssetAtPath<QuestDatabase>(QuestDatabasePath);
            Assert.IsNotNull(database, $"QuestDatabase를 찾을 수 없습니다: {QuestDatabasePath}");

            foreach (string questPath in MainQuestPaths)
            {
                QuestSO quest = AssetDatabase.LoadAssetAtPath<QuestSO>(questPath);
                Assert.IsNotNull(quest, $"메인 퀘스트 에셋을 찾을 수 없습니다: {questPath}");
                Assert.IsTrue(quest.isContentEnabled, $"비활성 메인 퀘스트입니다: {quest.questId}");
                Assert.AreEqual(QuestType.Main, quest.questType, $"메인 분류가 아닙니다: {quest.questId}");
                Assert.Contains(quest, database.QuestList, $"QuestDatabase에 등록되지 않았습니다: {quest.questId}");
            }
        }

        [Test]
        public void 첫_메인퀘스트의_첫_목표는_안내인_대화다()
        {
            QuestSO openingQuest = AssetDatabase.LoadAssetAtPath<QuestSO>(OpeningQuestPath);
            Assert.IsNotNull(openingQuest);
            Assert.IsNotEmpty(openingQuest.objectives);
            Assert.AreEqual("lake.story.guide_briefed", openingQuest.objectives[0].targetStringId);
        }
    }
}
