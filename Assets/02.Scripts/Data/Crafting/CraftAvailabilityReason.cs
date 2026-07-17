namespace UPlayGround.Data.Crafting
{
    public enum CraftAvailabilityReason
    {
        Available,
        DatabaseNotLoaded,
        InvalidRecipe,
        InvalidQuantity,
        InvalidResult,
        Locked,
        AlreadyCrafting,
        NotEnoughCost,
        NotEnoughIngredients,
    }
}
