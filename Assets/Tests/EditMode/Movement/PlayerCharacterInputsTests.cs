using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Input;
using UPlayGround.MovementController;

namespace UPlayGround.Movement.Tests
{
    public sealed class PlayerCharacterInputsTests
    {
        [Test]
        public void ClearAll_모든_일회성과_홀드_입력을_해제한다()
        {
            var skillInput = new List<InputCondition>
            {
                InputCondition.Pressed,
                InputCondition.Handled,
                InputCondition.Canceled,
            };
            var inputs = new PlayerCharacterInputs
            {
                MoveInput = Vector2.one,
                CameraRotation = Quaternion.Euler(10f, 20f, 30f),
                CrouchInput = InputCondition.Pressed,
                DodgeInput = InputCondition.Pressed,
                DashInput = InputCondition.Pressed,
                JumpInput = InputCondition.Pressed,
                AttackInput = InputCondition.Pressed,
                HeavyAttackInput = InputCondition.Pressed,
                ChargeAttackHeld = true,
                ChargeHoldTime = 1f,
                EquipInput = InputCondition.Pressed,
                InteractInput = InputCondition.Pressed,
                InteractHeld = true,
                SkillInput = skillInput,
                GuardInput = InputCondition.Pressed,
            };

            inputs.ClearAll();

            Assert.That(inputs.MoveInput, Is.EqualTo(Vector2.zero));
            Assert.That(inputs.CameraRotation, Is.EqualTo(Quaternion.identity));
            Assert.That(inputs.CrouchInput, Is.EqualTo(InputCondition.None));
            Assert.That(inputs.DodgeInput, Is.EqualTo(InputCondition.None));
            Assert.That(inputs.DashInput, Is.EqualTo(InputCondition.None));
            Assert.That(inputs.JumpInput, Is.EqualTo(InputCondition.None));
            Assert.That(inputs.AttackInput, Is.EqualTo(InputCondition.None));
            Assert.That(inputs.HeavyAttackInput, Is.EqualTo(InputCondition.None));
            Assert.That(inputs.EquipInput, Is.EqualTo(InputCondition.None));
            Assert.That(inputs.InteractInput, Is.EqualTo(InputCondition.None));
            Assert.That(inputs.InteractHeld, Is.False);
            Assert.That(inputs.GuardInput, Is.EqualTo(InputCondition.None));
            Assert.That(inputs.ChargeAttackHeld, Is.False);
            Assert.That(inputs.ChargeHoldTime, Is.Zero);
            Assert.That(skillInput, Is.All.EqualTo(InputCondition.None));
        }

        [Test]
        public void ClearInputAll_캐시된_이동벡터도_즉시_해제한다()
        {
            var gameObject = new GameObject(nameof(PlayerCharacterInputsTests));
            gameObject.SetActive(false);
            var controller = gameObject.AddComponent<PlayerMovementController>();

            try
            {
                FieldInfo moveInputVector = typeof(PlayerMovementController).GetField(
                    "_moveInputVector",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(moveInputVector, Is.Not.Null);
                moveInputVector.SetValue(controller, Vector3.forward);

                controller.ClearInputAll();

                Assert.That(controller.HasMoveInput(), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }
    }
}
