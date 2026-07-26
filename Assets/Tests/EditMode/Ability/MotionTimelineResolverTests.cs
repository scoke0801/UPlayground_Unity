using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data.Event;
using Motion = UPlayGround.Animation.Motion;

namespace UPlayGround.Ability.Tests
{
    public sealed class MotionTimelineResolverTests
    {
        [Test]
        public void SectionLayout_Section이_없으면_거부한다()
        {
            var set = new MotionSet
            {
                schemaVersion = MotionSet.CurrentSchemaVersion,
                motions = new List<Motion> { CreateMotion("base", 1f) },
            };

            Assert.That(
                MotionTimelineResolver.TryValidateSectionLayout(set, out string error),
                Is.False);
            Assert.That(error, Does.Contain("Section"));
        }

        [Test]
        public void SectionLayout_0초_Section과_유일_ID를_요구한다()
        {
            var set = new MotionSet
            {
                schemaVersion = MotionSet.CurrentSchemaVersion,
                motions = new List<Motion> { CreateMotion("base", 1f) },
                sections = new List<MotionSection>
                {
                    new() { id = "main", startTime = 0f, endPolicy = MotionSectionEndPolicy.Stop },
                },
            };

            Assert.That(
                MotionTimelineResolver.TryValidateSectionLayout(set, out string error),
                Is.True,
                error);
        }

        [Test]
        public void SectionLayout_레거시_스키마와_중복_시작시간을_거부한다()
        {
            var set = new MotionSet
            {
                schemaVersion = 0,
                motions = new List<Motion> { CreateMotion("base", 1f) },
                sections = new List<MotionSection>
                {
                    new() { id = "main", startTime = 0f },
                },
            };

            Assert.That(
                MotionTimelineResolver.TryValidateSectionLayout(set, out string schemaError),
                Is.False);
            Assert.That(schemaError, Does.Contain("스키마"));

            set.schemaVersion = MotionSet.CurrentSchemaVersion;
            set.sections.Add(new MotionSection { id = "duplicate", startTime = 0f });

            Assert.That(
                MotionTimelineResolver.TryValidateSectionLayout(set, out string timeError),
                Is.False);
            Assert.That(timeError, Does.Contain("시작 시간"));

            set.sections[1].startTime = float.NaN;
            Assert.That(
                MotionTimelineResolver.TryValidateSectionLayout(set, out string rangeError),
                Is.False);
            Assert.That(rangeError, Does.Contain("범위"));
        }

        [Test]
        public void LegacyEvent_UsesSequentialMotionOffset()
        {
            Motion first = CreateMotion("first", 1f);
            Motion second = CreateMotion("second", 2f);
            var motionEvent = new RecordingEvent { startTime = 0.25f, endTime = 0.75f };
            second.events.Add(motionEvent);
            var set = new MotionSet { motions = new List<Motion> { first, second } };

            bool resolved = MotionTimelineResolver.TryGetEventGlobalRange(
                set, motionEvent, out float start, out float end);

            Assert.That(resolved, Is.True);
            Assert.That(start, Is.EqualTo(1.25f).Within(0.001f));
            Assert.That(end, Is.EqualTo(1.75f).Within(0.001f));
        }

        [Test]
        public void MarkerLink_FollowsTrimmedMotionDuration()
        {
            Motion motion = CreateMotion("attack", 2f);
            motion.markers.Add(new MotionMarker
            {
                id = "impact",
                kind = MotionMarkerKind.Impact,
                normalizedTime = 0.5f,
            });
            var motionEvent = new RecordingEvent
            {
                timeLink = new MotionEventTimeLink
                {
                    enabled = true,
                    mode = MotionEventLinkMode.Marker,
                    linkedMotionId = motion.id,
                    markerId = "impact",
                    startValue = -0.1f,
                    endValue = 0.2f,
                },
            };
            motion.events.Add(motionEvent);
            var set = new MotionSet { motions = new List<Motion> { motion } };

            MotionTimelineResolver.TryGetEventGlobalRange(
                set, motionEvent, out float start, out float end);

            Assert.That(start, Is.EqualTo(0.9f).Within(0.001f));
            Assert.That(end, Is.EqualTo(1.2f).Within(0.001f));
        }

        [Test]
        public void Sections_ResolveRangeAndDefaultNextInTimeOrder()
        {
            Motion motion = CreateMotion("base", 3f);
            var intro = new MotionSection { id = "intro", startTime = 0f };
            var impact = new MotionSection { id = "impact", startTime = 1f };
            var recovery = new MotionSection { id = "recovery", startTime = 2f };
            var set = new MotionSet
            {
                motions = new List<Motion> { motion },
                sections = new List<MotionSection> { recovery, intro, impact },
            };

            Assert.That(
                MotionTimelineResolver.TryGetSection(set, "impact", out MotionSectionRange range),
                Is.True);
            Assert.That(range.startTime, Is.EqualTo(1f));
            Assert.That(range.endTime, Is.EqualTo(2f));
            Assert.That(MotionTimelineResolver.ResolveDefaultNextSectionId(set, impact),
                Is.EqualTo("recovery"));
        }

        [Test]
        public void FollowerSync_InterpolatesBetweenCommonMarkers()
        {
            Motion leaderMotion = CreateMotion("leader", 2f);
            leaderMotion.markers.Add(Marker("a", 0.25f));
            leaderMotion.markers.Add(Marker("b", 0.75f));
            Motion followerMotion = CreateMotion("follower", 4f);
            followerMotion.markers.Add(Marker("a", 0.25f));
            followerMotion.markers.Add(Marker("b", 0.75f));

            var set = new MotionSet
            {
                motions = new List<Motion> { leaderMotion },
                sync = new MotionSyncSettings { groupId = "attack", role = MotionSyncRole.Leader },
            };
            var layer = new MotionLayer
            {
                motions = new List<Motion> { followerMotion },
                sync = new MotionSyncSettings { groupId = "attack", role = MotionSyncRole.Follower },
            };

            float followerTime = MotionTimelineResolver.ResolveSynchronizedTime(set, layer, 1f);

            Assert.That(followerTime, Is.EqualTo(2f).Within(0.001f));
        }

        [Test]
        public void TimeStretch_PreservesImpactProtectionWindow()
        {
            Motion motion = CreateMotion("attack", 2f);
            motion.markers.Add(new MotionMarker
            {
                id = "impact",
                kind = MotionMarkerKind.Impact,
                normalizedTime = 0.5f,
            });
            var set = new MotionSet
            {
                motions = new List<Motion> { motion },
                timeStretch = new MotionTimeStretchSettings
                {
                    enabled = true,
                    protectImpact = true,
                    protectionBefore = 0.1f,
                    protectionAfter = 0.1f,
                },
            };

            Assert.That(MotionTimelineResolver.EvaluateTimeStretchRate(set, 1f, 2f), Is.EqualTo(1f));
            Assert.That(MotionTimelineResolver.EvaluateTimeStretchRate(set, 0.5f, 2f), Is.EqualTo(2f));
        }

        [Test]
        public void Executor_ProvidesEnterTickExitAndSignal()
        {
            var target = new GameObject("MotionEventExecutorTest");
            try
            {
                var executor = target.AddComponent<MotionEventExecutor>();
                var motionEvent = new RecordingEvent
                {
                    startTime = 0f,
                    endTime = 1f,
                    signalId = "AttackWindow",
                };
                Motion motion = CreateMotion("base", 2f);
                motion.events.Add(motionEvent);
                var set = new MotionSet { motions = new List<Motion> { motion } };
                var signals = new List<bool>();
                executor.SignalChanged += (_, active) => signals.Add(active);

                executor.PlayMotionSet(set);
                executor.UpdateTime(0f);
                executor.UpdateTime(0.5f);
                executor.ExitActiveEvents();

                Assert.That(motionEvent.enterCount, Is.EqualTo(1));
                Assert.That(motionEvent.tickCount, Is.EqualTo(1));
                Assert.That(motionEvent.exitCount, Is.EqualTo(1));
                Assert.That(signals, Is.EqualTo(new[] { true, false }));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        static Motion CreateMotion(string id, float duration)
        {
            var clip = new AnimationClip();
            clip.SetCurve(
                string.Empty,
                typeof(Transform),
                "m_LocalPosition.x",
                AnimationCurve.Linear(0f, 0f, duration, 1f));
            return new Motion
            {
                id = id,
                motionName = id,
                motionClip = clip,
            };
        }

        static MotionMarker Marker(string id, float normalizedTime) =>
            new MotionMarker { id = id, normalizedTime = normalizedTime };

        [Serializable]
        sealed class RecordingEvent : MotionEventBase, IMotionEventTick, IMotionEventSignal
        {
            public int enterCount;
            public int tickCount;
            public int exitCount;
            public string signalId;

            public string SignalId => signalId;
            public override string GetDisplayName() => "Recording";
            public override void Execute(GameObject target) => enterCount++;
            public void Tick(GameObject target, float normalizedTime, float deltaTime) => tickCount++;
            public override void OnCompleteEvent(GameObject target) => exitCount++;
        }
    }
}
