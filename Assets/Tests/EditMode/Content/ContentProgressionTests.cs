using System.Collections.Generic;
using NUnit.Framework;
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
}
