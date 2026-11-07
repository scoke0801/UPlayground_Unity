namespace UPlayGround.Data.Combat
{
    /// <summary>액터와 공격에 사용되는 전투 속성.</summary>
    public enum CombatElement
    {
        None = 0,
        Fire = 1,
        Water = 2,
        Nature = 3,
        Light = 4,
        Dark = 5,
    }

    public enum CombatElementAssignmentMode
    {
        Fixed,
        RandomPerNewGame,
    }

    /// <summary>
    /// 속성 상성의 단일 계산 규칙.
    /// 물→불→자연→물 순환과 빛↔어둠 상호 약점을 사용한다.
    /// </summary>
    public static class CombatElementRules
    {
        public const float DefaultAdvantageMultiplier = 1.25f;
        private static readonly CombatElement[] RandomElements =
        {
            CombatElement.Fire,
            CombatElement.Water,
            CombatElement.Nature,
            CombatElement.Light,
            CombatElement.Dark,
        };

        public static bool HasAdvantage(
            CombatElement attack,
            CombatElement defense)
        {
            if (attack == CombatElement.None || defense == CombatElement.None)
                return false;

            return attack switch
            {
                CombatElement.Water => defense == CombatElement.Fire,
                CombatElement.Fire => defense == CombatElement.Nature,
                CombatElement.Nature => defense == CombatElement.Water,
                CombatElement.Light => defense == CombatElement.Dark,
                CombatElement.Dark => defense == CombatElement.Light,
                _ => false,
            };
        }

        public static float ResolveDamageMultiplier(
            CombatElement attack,
            CombatElement defense,
            float advantageMultiplier = DefaultAdvantageMultiplier)
        {
            return HasAdvantage(attack, defense)
                ? UnityEngine.Mathf.Max(1f, advantageMultiplier)
                : 1f;
        }

        public static CombatElement ResolveRandomElement(
            int newGameSeed,
            string actorId)
        {
            unchecked
            {
                uint hash = 2166136261;
                hash = (hash ^ (uint)newGameSeed) * 16777619;
                string key = actorId ?? string.Empty;
                for (int i = 0; i < key.Length; i++)
                    hash = (hash ^ key[i]) * 16777619;
                return RandomElements[
                    (int)(hash % (uint)RandomElements.Length)];
            }
        }
    }
}
