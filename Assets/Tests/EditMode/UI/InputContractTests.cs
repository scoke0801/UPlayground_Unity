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

        [Test]
        public void 실제_기본_키맵의_중복은_계속_실패한다()
        {
            var asset = InputActionAsset.FromJson(File.ReadAllText(
                "Assets/Resources/Input/PlayerInputActions.inputactions"));
            try
            {
                asset.FindActionMap("UI", true).FindAction("Cancel", true)
                    .AddBinding("<Gamepad>/buttonSouth", groups: "Gamepad");
                var report = new UIInputContractReport();
                typeof(UIInputContractValidator).GetMethod("ValidateInputActionAsset",
                    BindingFlags.Static | BindingFlags.NonPublic, null,
                    new[] { typeof(InputActionAsset), typeof(UIInputContractReport) }, null)
                    .Invoke(null, new object[] { asset, report });
                Assert.That(report.IsValid, Is.False);
                Assert.That(report.ToString(), Does.Contain("UI/<Gamepad>/buttonSouth"));
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }
    }
}
