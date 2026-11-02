using System.Reflection;
using Animancer;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Animation;
using Motion = UPlayGround.Animation.Motion;

namespace UPlayGround.Ability.Tests
{
    /// <summary>지연된 프레임의 Hold 경계에서 실제 포즈를 마지막 시각에 맞추는지 검사한다.</summary>
    public sealed class MotionHoldPoseTests
    {
        [Test]
        public void 지연된_포즈도_Hold_구간의_마지막_자세에서_멈춘다()
        {
            var target = new GameObject("HoldPoseTest");
            var clip = new AnimationClip();
            try
            {
                clip.SetCurve(string.Empty, typeof(Transform), "m_LocalPosition.x",
                    AnimationCurve.Linear(0f, 0f, 1f, 1f));
                var animator = target.AddComponent<Animator>();
                var animancer = target.AddComponent<AnimancerComponent>();
                animancer.Animator = animator;
                var actor = target.AddComponent<ActorAnimator>();
                var set = MotionSet.CreateAuthored("HoldPoseTest", MotionSectionEndPolicy.Hold);
                set.motions.Add(new Motion { motionClip = clip, playbackSpeed = 1f });
                AnimancerState state = animancer.Play(clip);
                state.Time = 0.1f;
                SetField(actor, "_animator", animancer);
                SetField(actor, "_currentMotionSet", set);
                SetField(actor, "_currentState", state);
                SetField(actor, "_currentMotionIndex", 0);
                SetField(actor, "_currentSectionId", set.sections[0].id);
                SetField(actor, "_globalTime", 1.4f);

                bool handled = (bool)typeof(ActorAnimator).GetMethod("HandleSectionBoundary",
                    BindingFlags.Instance | BindingFlags.NonPublic).Invoke(actor, null);

                Assert.That(handled, Is.True);
                Assert.That(state.Time, Is.EqualTo(0.999f).Within(0.0001f));
                Assert.That(state.Speed, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(clip);
            }
        }

        private static void SetField(ActorAnimator actor, string name, object value) =>
            typeof(ActorAnimator).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(actor, value);
    }
}
