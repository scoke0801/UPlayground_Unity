using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data.Event;
using Motion = UPlayGround.Animation.Motion;
using MotionSetData = UPlayGround.Animation.MotionSet;

namespace UPlayGround.MotionSet.Tests
{
    public sealed class MotionSetCoreTests
    {
        [Test]
        public void Resolver_두번째_모션_이벤트에_누적_시간을_적용한다()
        {
            Motion first = CreateMotion("first", 1f);
            Motion second = CreateMotion("second", 2f);
            RecordingEvent motionEvent = new()
            {
                startTime = 0.25f,
                endTime = 0.75f,
            };
            second.events.Add(motionEvent);
            MotionSetData set = new()
            {
                motions = new List<Motion> { first, second },
            };

            bool resolved = MotionTimelineResolver.TryGetEventGlobalRange(
                set,
                motionEvent,
                out float start,
                out float end);

            Assert.That(resolved, Is.True);
            Assert.That(start, Is.EqualTo(1.25f).Within(0.001f));
            Assert.That(end, Is.EqualTo(1.75f).Within(0.001f));
        }

        [Test]
        public void MotionSet_내부블렌드시간은_음수가_되지않는다()
        {
            MotionSetData set = new() { internalBlendDuration = -1f };

            Assert.That(set.InternalBlendDuration, Is.EqualTo(0f));
        }

        [Test]
        public void Executor_명시적대상_부모Provider_Self_순서로_해석한다()
        {
            GameObject parent = new("Provider");
            GameObject child = new("Executor");
            GameObject explicitTarget = new("Explicit");
            try
            {
                child.transform.SetParent(parent.transform);
                TargetProvider provider = parent.AddComponent<TargetProvider>();
                provider.Target = parent;
                MotionEventExecutor executor = child.AddComponent<MotionEventExecutor>();

                Assert.That(executor.TargetObject, Is.SameAs(parent));

                executor.SetTargetObject(explicitTarget);
                Assert.That(executor.TargetObject, Is.SameAs(explicitTarget));

                executor.SetTargetObject(null);
                Assert.That(executor.TargetObject, Is.SameAs(parent));

                UnityEngine.Object.DestroyImmediate(provider);
                child.transform.SetParent(null);
                Assert.That(executor.TargetObject, Is.SameAs(child));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(child);
                UnityEngine.Object.DestroyImmediate(parent);
                UnityEngine.Object.DestroyImmediate(explicitTarget);
            }
        }

        [Test]
        public void Executor_EnterTickExit과_Signal을_발행한다()
        {
            GameObject target = new("MotionEventExecutorTest");
            try
            {
                MotionEventExecutor executor = target.AddComponent<MotionEventExecutor>();
                RecordingEvent motionEvent = new()
                {
                    startTime = 0f,
                    endTime = 1f,
                    Signal = "Window",
                };
                Motion motion = CreateMotion("base", 2f);
                motion.events.Add(motionEvent);
                MotionSetData set = new() { motions = new List<Motion> { motion } };
                List<bool> signals = new();
                executor.SignalChanged += (_, active) => signals.Add(active);

                executor.PlayMotionSet(set);
                executor.UpdateTime(0f);
                executor.UpdateTime(0.5f);
                executor.ExitActiveEvents();

                Assert.That(motionEvent.EnterCount, Is.EqualTo(1));
                Assert.That(motionEvent.TickCount, Is.EqualTo(1));
                Assert.That(motionEvent.ExitCount, Is.EqualTo(1));
                Assert.That(signals, Is.EqualTo(new[] { true, false }));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void Executor_적대상에서_무시정책_Event를_활성화하지않는다()
        {
            GameObject target = new("EnemyMotionEventTarget");
            try
            {
                GameObject eventTarget = new("ExplicitChildTarget");
                eventTarget.transform.SetParent(target.transform);
                TargetProvider provider = target.AddComponent<TargetProvider>();
                provider.Target = eventTarget;
                provider.IsEnemy = true;
                MotionEventExecutor executor = eventTarget.AddComponent<MotionEventExecutor>();
                RecordingEvent motionEvent = new()
                {
                    startTime = 0f,
                    endTime = 1f,
                    Policy = MotionEventEnemyExecutionPolicy.Ignored,
                    Signal = "Ignored",
                };
                Motion motion = CreateMotion("enemy", 2f);
                motion.events.Add(motionEvent);
                MotionSetData set = new() { motions = new List<Motion> { motion } };
                var signals = new List<bool>();
                executor.SignalChanged += (_, active) => signals.Add(active);

                executor.PlayMotionSet(set);
                executor.UpdateTime(0f);
                executor.UpdateTime(1.5f);

                Assert.That(motionEvent.EnterCount, Is.Zero);
                Assert.That(motionEvent.TickCount, Is.Zero);
                Assert.That(motionEvent.ExitCount, Is.Zero);
                Assert.That(signals, Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static Motion CreateMotion(string id, float duration)
        {
            AnimationClip clip = new();
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

        private sealed class TargetProvider : MonoBehaviour, IMotionEventTargetProvider,
            IMotionEventExecutionScope
        {
            public GameObject Target;
            public bool IsEnemy;
            public GameObject MotionEventTarget => Target;
            public bool IsEnemyMotionEventTarget => IsEnemy;
        }

        [Serializable]
        private sealed class RecordingEvent : MotionEventBase, IMotionEventTick, IMotionEventSignal
        {
            public int EnterCount;
            public int TickCount;
            public int ExitCount;
            public string Signal;
            public MotionEventEnemyExecutionPolicy Policy;

            public string SignalId => Signal;
            public override MotionEventEnemyExecutionPolicy EnemyExecutionPolicy => Policy;
            public override string GetDisplayName() => "Recording";
            public override void Execute(GameObject target) => EnterCount++;
            public void Tick(GameObject target, float normalizedTime, float deltaTime) => TickCount++;
            public override void OnCompleteEvent(GameObject target) => ExitCount++;
        }
    }
}
