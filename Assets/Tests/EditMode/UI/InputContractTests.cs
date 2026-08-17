using NUnit.Framework;
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
    }
}
