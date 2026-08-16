namespace UPlayGround.Economy
{
    /// <summary>음수가 될 수 없는 정수 재화 잔액과 원자적 입출금을 관리한다.</summary>
    public sealed class CurrencyWallet
    {
        public int Balance { get; private set; }

        /// <summary>양수 금액을 입금하며 정수 범위를 넘으면 변경하지 않는다.</summary>
        public bool TryDeposit(int amount)
        {
            if (amount <= 0 || Balance > int.MaxValue - amount)
                return false;

            Balance += amount;
            return true;
        }

        /// <summary>잔액이 충분할 때만 양수 금액을 출금한다.</summary>
        public bool TryWithdraw(int amount)
        {
            if (amount <= 0 || Balance < amount)
                return false;

            Balance -= amount;
            return true;
        }

        /// <summary>저장 잔액을 복원하며 손상된 음수 값은 0으로 보정한다.</summary>
        public void Restore(int balance)
        {
            Balance = balance < 0 ? 0 : balance;
        }
    }
}
