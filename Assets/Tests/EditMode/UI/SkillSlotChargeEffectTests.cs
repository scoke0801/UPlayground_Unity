using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Ability.Core;
using UPlayGround.UI.InputPrompt;

namespace UPlayGround.UI.Tests
{
    /// <summary>충전 효과가 행동 제한과 무관하게 게이지 충족 상태를 표시하는지 검증한다.</summary>
    public sealed class SkillSlotChargeEffectTests
    {
        [Test]
        public void 충전_효과는_필요_자원과_잠금에만_반응한다()
        {
            var root = new GameObject("slot", typeof(RectTransform));
            var effect = new GameObject("effect", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            effect.transform.SetParent(root.transform);
            var slot = root.AddComponent<UISkillSlot>();
            typeof(UISkillSlot).GetField("_availableEffect", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(slot, effect.GetComponent<Image>());
            typeof(UISkillSlot).GetField("_enableTween", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(slot, false);
            try
            {
                slot.SetAbilityState(State(9f, 10f, true, AbilityActivationResult.InsufficientResource));
                Assert.That(effect.activeSelf, Is.False);
                slot.SetAbilityState(State(10f, 10f, true, AbilityActivationResult.Success));
                Assert.That(effect.activeSelf, Is.True);
                slot.SetAbilityState(State(10f, 10f, true, AbilityActivationResult.InvalidGroundState));
                Assert.That(effect.activeSelf, Is.True);
                slot.SetAbilityState(State(10f, 10f, true, AbilityActivationResult.CooldownActive));
                Assert.That(effect.activeSelf, Is.True);
                slot.SetAbilityState(State(10f, 10f, false, AbilityActivationResult.Locked));
                Assert.That(effect.activeSelf, Is.False);
                slot.SetAbilityState(State(10f, 0f, true, AbilityActivationResult.Success));
                Assert.That(effect.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static AbilitySlotViewState State(float current, float required, bool unlocked, AbilityActivationResult reason)
        {
            return new AbilitySlotViewState("test", true, unlocked, reason == AbilityActivationResult.Success,
                reason, current, required, 0f, 0f, null);
        }
    }
}
