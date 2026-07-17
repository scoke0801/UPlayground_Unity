namespace UPlayGround.Data.Item
{
    public enum InventoryActionResult
    {
        Success = 0,
        InvalidItem,
        NotEnoughCount,
        NotUsable,
        NotEquippable,
        EquippedItem,
        NoEffect,
        Failed,
    }
}
