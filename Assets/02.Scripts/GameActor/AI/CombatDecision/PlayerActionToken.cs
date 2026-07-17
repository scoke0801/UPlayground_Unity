namespace UPlayGround.AI.CombatDecision
{
    public enum PlayerActionToken
    {
        None = 0,
        Attack,
        HeavyAttack,
        Dodge,
        Guard,
        GuardBreak,
        Hit,
        Recover,
        DashApproach,
        DashRetreat
    }

    public static class PlayerActionTokenMapper
    {
        public static PlayerActionToken FromStateName(string stateName)
        {
            return stateName switch
            {
                "Attack" or "DashAttack" or "JumpAttack" or "JumpDashAttack" or "FinishAttack" or "SpecialBreakAttack" => PlayerActionToken.Attack,
                "Charge" or "HeavyAttack" => PlayerActionToken.HeavyAttack,
                "Dodge" => PlayerActionToken.Dodge,
                "Guard" => PlayerActionToken.Guard,
                "GuardBreak" => PlayerActionToken.GuardBreak,
                "Hit" or "Airborne" or "Knockdown" or "Stun" or "Grabbed" => PlayerActionToken.Hit,
                "Dash" => PlayerActionToken.DashApproach,
                _ => PlayerActionToken.None
            };
        }
    }
}
