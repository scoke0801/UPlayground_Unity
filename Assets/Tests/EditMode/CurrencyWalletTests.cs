using NUnit.Framework;
using UPlayGround.Economy;

namespace UPlayGround.Core.Tests
{
    public sealed class CurrencyWalletTests
    {
        [Test]
        public void TryDeposit_양수금액을잔액에더한다()
        {
            var wallet = new CurrencyWallet();

            bool deposited = wallet.TryDeposit(120);

            Assert.That(deposited, Is.True);
            Assert.That(wallet.Balance, Is.EqualTo(120));
        }

        [Test]
        public void TryDeposit_잘못된금액과오버플로를거부하고잔액을보존한다()
        {
            var wallet = new CurrencyWallet();
            wallet.Restore(int.MaxValue - 1);

            Assert.That(wallet.TryDeposit(0), Is.False);
            Assert.That(wallet.TryDeposit(-1), Is.False);
            Assert.That(wallet.TryDeposit(2), Is.False);
            Assert.That(wallet.Balance, Is.EqualTo(int.MaxValue - 1));
        }

        [Test]
        public void TryWithdraw_잔액이충분할때만차감한다()
        {
            var wallet = new CurrencyWallet();
            wallet.Restore(100);

            Assert.That(wallet.TryWithdraw(40), Is.True);
            Assert.That(wallet.TryWithdraw(61), Is.False);
            Assert.That(wallet.TryWithdraw(0), Is.False);
            Assert.That(wallet.Balance, Is.EqualTo(60));
        }

        [Test]
        public void Restore_음수세이브잔액을0으로보정한다()
        {
            var wallet = new CurrencyWallet();

            wallet.Restore(-10);

            Assert.That(wallet.Balance, Is.Zero);
        }
    }
}
