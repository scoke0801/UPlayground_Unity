using NUnit.Framework;
using System;
using System.Reflection;
using UnityEngine;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.Ability.Tests
{
    public sealed class GameplayTagRegistryTests
    {
        [Test]
        public void 등록된_태그만_Resolve할_수_있다()
        {
            Assert.That(
                GameplayTagRegistry.TryResolve(
                    "State.Combat.Attack",
                    out GameplayTag registered),
                Is.True);
            Assert.That(registered.IsValid(), Is.True);
            Assert.That(registered.TagName, Is.EqualTo("State.Combat.Attack"));

            Assert.That(
                GameplayTagRegistry.TryResolve(
                    "State.Combt.Attack",
                    out GameplayTag unregistered),
                Is.False);
            Assert.That(unregistered.IsValid(), Is.False);
        }

        [Test]
        public void 미등록_태그의_필수_Resolve는_예외를_발생시킨다()
        {
            Assert.Throws<System.ArgumentException>(
                () => GameplayTagRegistry.GetRequired("Unknown.Tag"));
        }

        [Test]
        public void 코드_표준_태그는_Registry에_등록되어_있다()
        {
            Type[] containers =
            {
                typeof(GameplayTags),
                typeof(MotionTags),
            };

            foreach (Type container in containers)
            {
                foreach (FieldInfo field in container.GetFields(
                             BindingFlags.Public | BindingFlags.Static))
                {
                    if (field.FieldType != typeof(GameplayTag))
                        continue;

                    GameplayTag tag = (GameplayTag)field.GetValue(null);
                    if (string.IsNullOrEmpty(tag.TagName))
                        continue;

                    Assert.That(
                        GameplayTagRegistry.IsRegistered(tag.TagName),
                        Is.True,
                        $"{container.Name}.{field.Name}의 "
                        + $"{tag.TagName}이 Registry에 없습니다.");
                    Assert.That(tag.IsValid(), Is.True);
                }
            }
        }

        [Test]
        public void 직렬화된_미등록_문자열은_유효한_GameplayTag가_아니다()
        {
            GameplayTag tag = JsonUtility.FromJson<GameplayTag>(
                "{\"_tagName\":\"Unknown.Serialized.Tag\"}");

            Assert.That(tag.TagName, Is.EqualTo("Unknown.Serialized.Tag"));
            Assert.That(tag.IsValid(), Is.False);
            Assert.That(
                tag.IsChildOf(GameplayTags.State_Combat),
                Is.False);
        }
    }
}
