namespace UPlayGround.Combat
{
    /// <summary>전투 해석 파이프라인이 피격 대상을 개방형으로 디스패치하기 위한 계약.</summary>
    public interface ICombatResolvable : IDamageable
    {
        bool CanResolveHit(in HitRequest request);
        CombatResult ResolveHit(in HitRequest request);
        CombatResult ApplyResolvedHit(in HitRequest request, in CombatResult resolved);
    }
}
