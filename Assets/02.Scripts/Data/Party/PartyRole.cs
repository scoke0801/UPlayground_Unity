namespace UPlayGround.Data.Party
{
    /// <summary>
    /// 파티 캐릭터의 역할/특성 분류. 동료 상세 패널의 역할 태그에 사용한다.
    /// </summary>
    public enum PartyRole
    {
        Melee    = 0,  // 근접
        Balanced = 1,  // 균형
        Mobility = 2,  // 기동
    }

    public static class PartyRoleExtensions
    {
        public static string ToDisplayString(this PartyRole role) => role switch
        {
            PartyRole.Melee    => "근접",
            PartyRole.Balanced => "균형",
            PartyRole.Mobility => "기동",
            _                  => string.Empty,
        };
    }
}
