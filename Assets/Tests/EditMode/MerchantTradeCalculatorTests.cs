using NUnit.Framework;
using UPlayGround.Economy;

namespace UPlayGround.Core.Tests
{
    public sealed class MerchantTradeCalculatorTests
    {
        [Test]
        public void TryCalculateTotal_정상가격과수량의총액을계산한다()
        {
            bool calculated = MerchantTradeCalculator.TryCalculateTotal(220, 3, out int total);

            Assert.That(calculated, Is.True);
            Assert.That(total, Is.EqualTo(660));
        }

        [Test]
        public void TryCalculateTotal_잘못된값과오버플로를거부한다()
        {
            Assert.That(MerchantTradeCalculator.TryCalculateTotal(0, 1, out _), Is.False);
            Assert.That(MerchantTradeCalculator.TryCalculateTotal(1, 0, out _), Is.False);
            Assert.That(MerchantTradeCalculator.TryCalculateTotal(int.MaxValue, 2, out _), Is.False);
        }

        [Test]
        public void GetMaxAffordableQuantity_잔액과한정재고중작은값을쓴다()
        {
            Assert.That(MerchantTradeCalculator.GetMaxAffordableQuantity(1_000, 200, 3), Is.EqualTo(3));
            Assert.That(MerchantTradeCalculator.GetMaxAffordableQuantity(1_000, 200, -1), Is.EqualTo(5));
        }
    }
}
