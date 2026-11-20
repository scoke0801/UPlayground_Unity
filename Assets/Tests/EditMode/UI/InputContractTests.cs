using NUnit.Framework;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround.UI.EditorTools;

namespace UPlayGround.UI.Tests
{
    public sealed class InputContractTests
    {
        [Test]
        public void UI입력계약을만족한다()
        {
            UIInputContractReport report = UIInputContractValidator.ValidateAll();

            Assert.IsTrue(report.IsValid, report.ToString());
        }

        [TestCase("UI", "Cancel", "<Gamepad>/buttonSouth")]
        [TestCase("UI", "Cancel", "<Gamepad>/select")]
        [TestCase("PlayerAction", "SuperJump", "<Gamepad>/buttonSouth")]
        public void 실제_기본_키맵의_중복은_계속_실패한다(string map, string action, string path)
        {
            var asset = InputActionAsset.FromJson(File.ReadAllText(
                "Assets/Resources/Input/PlayerInputActions.inputactions"));
            try
            {
                asset.FindActionMap(map, true).FindAction(action, true).AddBinding(path, groups: "Gamepad");
                var report = new UIInputContractReport();
                typeof(UIInputContractValidator).GetMethod("ValidateInputActionAsset",
                    BindingFlags.Static | BindingFlags.NonPublic, null,
                    new[] { typeof(InputActionAsset), typeof(UIInputContractReport) }, null)
                    .Invoke(null, new object[] { asset, report });
                Assert.That(report.IsValid, Is.False);
                Assert.That(report.ToString(), Does.Contain($"{map}/{path}"));
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }
    }
}
