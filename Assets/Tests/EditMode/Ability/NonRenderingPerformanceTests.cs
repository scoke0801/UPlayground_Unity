using System;
using System.Collections.Generic;
using System.Reflection;
using Animancer;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Animation;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Event;
using UPlayGround.Debugging;
using Object = UnityEngine.Object;

namespace UPlayGround.Ability.Tests
{
    /// <summary>정지 프레임의 할당과 거리 정지 그래프의 복구를 검사한다.</summary>
    public sealed class NonRenderingPerformanceTests
    {
        private readonly List<Object> _objects = new();
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
                if (_objects[i] != null)
                    Object.DestroyImmediate(_objects[i]);
            _objects.Clear();
            MotionSetEventDebugOverlay.Clear();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void 모션_유지_평가는_준비_후_할당하지_않는다(bool hasActiveEvents)
        {
            var target = CreateObject("MotionAllocation");
            var executor = target.AddComponent<MotionEventExecutor>();
            executor.SetTargetObject(target);
            var set = CreateMotionSet();
            if (hasActiveEvents)
            {
                set.globalEvents.Add(new QuietEvent { startTime = 0f, endTime = 9f, executionOrder = 2 });
                set.globalEvents.Add(new QuietEvent { startTime = 0f, endTime = 9f, executionOrder = 1 });
            }
            executor.PlayMotionSet(set);
            executor.UpdateTime(0.1f);
            executor.UpdateTime(0.2f);
            _ = GC.GetAllocatedBytesForCurrentThread();

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 100; i++)
                executor.UpdateTime(0.3f + i * 0.01f);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.That(allocated, Is.Zero);
        }

        [Test]
        public void 재사용_비교자는_모션_교체_후에도_시간과_실행순서를_지킨다()
        {
            var target = CreateObject("MotionOrder");
            var executor = target.AddComponent<MotionEventExecutor>();
            executor.SetTargetObject(target);
            var set = CreateMotionSet();
            var later = new QuietEvent { startTime = 0.5f, endTime = 9f };
            var second = new QuietEvent { startTime = 0.25f, endTime = 9f, executionOrder = 2 };
            var first = new QuietEvent { startTime = 0.25f, endTime = 9f, executionOrder = 1 };
            set.globalEvents.AddRange(new[] { later, second, first });
            var executed = new List<MotionEventBase>();
            executor.EventExecuted += executed.Add;
            executor.PlayMotionSet(CreateMotionSet());
            executor.UpdateTime(0.1f);
            executor.PlayMotionSet(set);
            executor.UpdateTime(0.6f);
            Assert.That(executed, Is.EqualTo(new[] { first, second, later }));
        }

        [Test]
        public void 슬롯_조회는_순환_후에도_방문집합을_반납하고_변경을_즉시_읽는다()
        {
            var root = CreateAsset<AbilitySetSO>();
            var derived = CreateAsset<AbilitySetSO>();
            derived.baseSet = root;
            root.baseSet = derived;
            Assert.That(derived.GetPlayerAbility(PlayerSkillSlot.Ability), Is.Null);
            var ability = CreateAsset<GameplayAbilitySO>();
            root.playerSlots.Add(new AbilitySetSO.PlayerSlotEntry { slot = PlayerSkillSlot.Ability, ability = ability });
            Assert.That(derived.GetPlayerAbility(PlayerSkillSlot.Ability), Is.SameAs(ability));
            _ = derived.GetPlayerAbility(PlayerSkillSlot.Ability);
            _ = GC.GetAllocatedBytesForCurrentThread();
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 100; i++)
                _ = derived.GetPlayerAbility(PlayerSkillSlot.Ability);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
            root.playerSlots.Clear();
            Assert.That(derived.GetPlayerAbility(PlayerSkillSlot.Ability), Is.Null);
        }

        [Test]
        public void 쿨다운_단독_조회는_발동조건과_무관하며_할당하지_않는다()
        {
            var owner = CreateObject("CooldownOwner").AddComponent<TestGameActor>();
            owner.InitializeForEditMode();
            var set = CreateAsset<AbilitySetSO>();
            var ability = CreateAsset<GameplayAbilitySO>();
            ability.abilityId = "Test.Performance.Cooldown";
            set.playerSlots.Add(new AbilitySetSO.PlayerSlotEntry { slot = PlayerSkillSlot.Ability, ability = ability });
            owner.Abilities.SetAbilitySet(set);
            string group = ability.cooldown.ResolveGroupId(ability.abilityId);
            owner.AbilitySystem.Runtime.Cooldowns.Start(group, 30f);
            // Variant가 없어 발동할 수 없는 Ability도 쿨다운 표시는 유지해야 한다.
            Assert.That(owner.Abilities.GetPlayerCooldownRemaining(PlayerSkillSlot.Ability), Is.GreaterThan(29f));
            _ = GC.GetAllocatedBytesForCurrentThread();
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 100; i++)
                _ = owner.Abilities.GetPlayerCooldownRemaining(PlayerSkillSlot.Ability);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
            set.playerSlots.Clear();
            Assert.That(owner.Abilities.GetPlayerCooldownRemaining(PlayerSkillSlot.Ability), Is.Zero);
        }

        [Test]
        public void 거리_정지는_본체와_보조_그래프를_멈추고_요청속도로_복구한다()
        {
            ActorAnimator actor = CreateAnimator();
            ActorAnimator sub = CreateAnimator();
            typeof(ActorAnimator).GetField("_subAnimator", PrivateInstance).SetValue(actor, sub);
            actor.Speed = 0.75f;
            actor.SetSimulationPaused(true);
            Assert.That(actor.GetAnimancerComponent().Graph.IsGraphPlaying, Is.False);
            Assert.That(sub.GetAnimancerComponent().Graph.IsGraphPlaying, Is.False);
            actor.Speed = 0.5f;
            actor.SetSimulationPaused(false);
            Assert.That(actor.GetAnimancerComponent().Graph.IsGraphPlaying, Is.True);
            Assert.That(sub.GetAnimancerComponent().Graph.IsGraphPlaying, Is.True);
            Assert.That(actor.GetAnimancerComponent().Graph.Speed, Is.EqualTo(0.5f));
            Assert.That(sub.GetAnimancerComponent().Graph.Speed, Is.EqualTo(0.5f));
        }

        [Test]
        public void 원래_중단된_그래프는_거리_복구로_재생하지_않는다()
        {
            ActorAnimator actor = CreateAnimator();
            actor.GetAnimancerComponent().Graph.PauseGraph();
            actor.SetSimulationPaused(true);
            actor.SetSimulationPaused(false);
            Assert.That(actor.GetAnimancerComponent().Graph.IsGraphPlaying, Is.False);
        }

        [Test]
        public void 정지중_모델교체는_기존그래프를_반환하고_새그래프에_적용한다()
        {
            ActorAnimator actor = CreateAnimator();
            AnimancerComponent original = actor.GetAnimancerComponent();
            actor.SetSimulationPaused(true);
            AnimancerComponent replacement = CreateAnimator().GetAnimancerComponent();
            typeof(ActorAnimator).GetField("_animator", PrivateInstance).SetValue(actor, replacement);
            InvokeAnimator(actor, "Update");
            Assert.That(original.Graph.IsGraphPlaying, Is.True);
            Assert.That(replacement.Graph.IsGraphPlaying, Is.False);
            actor.SetSimulationPaused(false);
            Assert.That(replacement.Graph.IsGraphPlaying, Is.True);
        }

        [Test]
        public void 비활성화는_그래프를_반환하고_재활성_평가는_거리정지를_유지한다()
        {
            ActorAnimator actor = CreateAnimator();
            actor.SetSimulationPaused(true);
            InvokeAnimator(actor, "OnDisable");
            Assert.That(actor.GetAnimancerComponent().Graph.IsGraphPlaying, Is.True);
            Assert.That(actor.IsSimulationPaused, Is.True);
            InvokeAnimator(actor, "Update");
            Assert.That(actor.GetAnimancerComponent().Graph.IsGraphPlaying, Is.False);
            actor.SetSimulationPaused(false);
            Assert.That(actor.GetAnimancerComponent().Graph.IsGraphPlaying, Is.True);
        }

        private GameObject CreateObject(string name)
        {
            var value = new GameObject(name);
            _objects.Add(value);
            return value;
        }

        private T CreateAsset<T>() where T : ScriptableObject
        {
            T value = ScriptableObject.CreateInstance<T>();
            _objects.Add(value);
            return value;
        }

        private ActorAnimator CreateAnimator()
        {
            GameObject target = CreateObject("SimulationGraph");
            var animator = target.AddComponent<Animator>();
            var animancer = target.AddComponent<AnimancerComponent>();
            animancer.Animator = animator;
            var actor = target.AddComponent<ActorAnimator>();
            typeof(ActorAnimator).GetField("_animator", PrivateInstance).SetValue(actor, animancer);
            animancer.Graph.UnpauseGraph();
            return actor;
        }

        private MotionSet CreateMotionSet()
        {
            var clip = new AnimationClip();
            _objects.Add(clip);
            clip.SetCurve(string.Empty, typeof(Transform), "m_LocalPosition.x", AnimationCurve.Linear(0, 0, 10, 1));
            return new MotionSet
            {
                motionSetName = "PerformanceTest",
                motions = new List<global::UPlayGround.Animation.Motion> { new() { motionClip = clip } },
            };
        }

        private static void InvokeAnimator(ActorAnimator actor, string method) =>
            typeof(ActorAnimator).GetMethod(method, PrivateInstance).Invoke(actor, null);

        [Serializable]
        private sealed class QuietEvent : MotionEventBase
        {
            public override string GetDisplayName() => "PerformanceTest";
            public override void Execute(GameObject target) { }
            public override void OnCompleteEvent(GameObject target) { }
        }
    }
}
