using NUnit.Framework;
using UPlayGround.Core.Party;

namespace UPlayGround.Core.Tests
{
    public class PartyRosterServiceTests
    {
        private enum Character
        {
            A,
            B,
            C,
            D,
        }

        [Test]
        public void AddToBattle_보유하지_않은_캐릭터는_거부한다()
        {
            var service = new PartyRosterService<Character>();

            Assert.That(service.AddToBattle(Character.A, 4), Is.False);
        }

        [Test]
        public void SetBattleOrder_중복과_상한을_정규화한다()
        {
            var service = new PartyRosterService<Character>();
            service.AddToRoster(Character.A);
            service.AddToRoster(Character.B);
            service.AddToRoster(Character.C);

            bool changed = service.SetBattleOrder(
                new[] { Character.A, Character.A, Character.B, Character.C },
                maxBattleSize: 2);

            Assert.That(changed, Is.True);
            Assert.That(service.BattleOrder, Is.EqualTo(new[] { Character.A, Character.B }));
        }

        [Test]
        public void ReplaceBattleSlot_이미_출전_중이면_두_슬롯을_교환한다()
        {
            var service = new PartyRosterService<Character>();
            service.AddToRoster(Character.A);
            service.AddToRoster(Character.B);
            service.AddToBattle(Character.A, 4);
            service.AddToBattle(Character.B, 4);

            bool changed = service.ReplaceBattleSlot(0, Character.B, out int existingIndex);

            Assert.That(changed, Is.True);
            Assert.That(existingIndex, Is.EqualTo(1));
            Assert.That(service.BattleOrder, Is.EqualTo(new[] { Character.B, Character.A }));
        }
    }
}
