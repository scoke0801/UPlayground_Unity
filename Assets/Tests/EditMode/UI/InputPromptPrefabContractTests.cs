using NUnit.Framework;
using UPlayGround.UI.EditorTools;

namespace UPlayGround.UI.Tests
{
    public sealed class InputPromptPrefabContractTests
    {
        [Test]
        public void 전체화면UI_입력프롬프트계약을만족한다()
        {
            UIInputPromptValidationReport report =
                UIInputPromptPrefabTool.ValidateAll();

            Assert.IsTrue(report.IsValid, report.ToString());
            Assert.AreEqual(11, report.CheckedPrefabCount, report.ToString());
        }
    }
}
