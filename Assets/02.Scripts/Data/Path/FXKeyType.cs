// 자동 생성 파일입니다. 직접 수정하지 마세요.
// UPlayGround/ID Enum Generator 창에서 재생성하세요.
// Generated: 2026-05-23 00:22
namespace UPlayGround.Data.Path
{
    /// <summary>FXKeyType — FX Prefab 키 열거형 (자동 생성)</summary>
    public enum FXKeyType
    {
        None = 0,
        InteractionObjectHitFX = 1,
        ObjectDestroyFX = 2,
        ItemArrivedToPlayerPos = 3,
        DefaultCombatHit = 4,
        EnemyLightAttackHit = 5,
        Heal = 6,
        HealAura = 7,
        LiteHit = 8,
        PlayerHeal = 9,
        playerGuardFX = 10,
        playerFullChargeFX = 11,
        GriffinDiveImpact = 12,
        ParryFX = 13,
        EnemyHeavyAttackTelegraph_Circle = 14,
        PlayerSwap = 15,
    }

    public static class FXKeyTypeExtensions
    {
        /// <summary>enum 값을 FX Prefab 키 문자열로 변환한다.</summary>
        public static string ToKey(this FXKeyType type) => type switch
        {
            FXKeyType.InteractionObjectHitFX => "InteractionObjectHitFX",
            FXKeyType.ObjectDestroyFX => "ObjectDestroyFX",
            FXKeyType.ItemArrivedToPlayerPos => "ItemArrivedToPlayerPos",
            FXKeyType.DefaultCombatHit => "DefaultCombatHit",
            FXKeyType.EnemyLightAttackHit => "EnemyLightAttackHit",
            FXKeyType.Heal => "Heal",
            FXKeyType.HealAura => "HealAura",
            FXKeyType.LiteHit => "LiteHit",
            FXKeyType.PlayerHeal => "PlayerHeal",
            FXKeyType.playerGuardFX => "playerGuardFX",
            FXKeyType.playerFullChargeFX => "playerFullChargeFX",
            FXKeyType.GriffinDiveImpact => "GriffinDiveImpact",
            FXKeyType.ParryFX => "ParryFX",
            FXKeyType.EnemyHeavyAttackTelegraph_Circle => "EnemyHeavyAttackTelegraph_Circle",
            FXKeyType.PlayerSwap => "PlayerSwap",
            _ => string.Empty,
        };
    }
}
