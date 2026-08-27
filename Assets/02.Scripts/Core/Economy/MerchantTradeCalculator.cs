namespace UPlayGround.Economy
{
    /// <summary>거래 금액의 정수 오버플로를 막고 구매 가능한 최대 수량을 계산한다.</summary>
    public static class MerchantTradeCalculator
    {
        /// <summary>양수 단가와 수량의 총액을 정수 범위 안에서 계산한다.</summary>
        public static bool TryCalculateTotal(int unitPrice, int quantity, out int totalPrice)
        {
            totalPrice = 0;
            if (unitPrice <= 0 || quantity <= 0 || unitPrice > int.MaxValue / quantity)
                return false;

            totalPrice = unitPrice * quantity;
            return true;
        }

        /// <summary>잔액과 재고를 함께 고려해 한 번에 구매할 수 있는 최대 수량을 반환한다.</summary>
        public static int GetMaxAffordableQuantity(int balance, int unitPrice, int remainingStock)
        {
            if (balance < 0 || unitPrice <= 0)
                return 0;

            int affordable = balance / unitPrice;
            return remainingStock < 0 ? affordable : System.Math.Min(affordable, remainingStock);
        }
    }
}
