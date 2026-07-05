#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UPlayGround.Data.EnumType;

namespace UPlayGround.Manager
{
    /// <summary>CheatManager — 파티 치트(해금/레벨/경험치/회복/스왑 쿨). 개발 빌드 전용.</summary>
    public partial class CheatManager
    {
        public bool UnlockCharacter(CharacterActorType type)
        {
            bool ok = PartyManager.Instance != null && PartyManager.Instance.UnlockCharacter(type);
            if (ok) Log(CheatCategory.Party, $"캐릭터 해금: {type}");
            return ok;
        }

        public bool SetLevel(CharacterActorType type, int level)
        {
            bool ok = PartyManager.Instance != null && PartyManager.Instance.SetLevelForDebug(type, level);
            if (ok) Log(CheatCategory.Party, $"레벨 설정: {type} Lv.{level}");
            return ok;
        }

        public bool GrantExp(CharacterActorType type, long amount)
        {
            if (PartyManager.Instance == null) return false;
            bool leveledUp = PartyManager.Instance.AddExp(type, amount);
            Log(CheatCategory.Party, $"경험치 지급: {type} +{amount}{(leveledUp ? " (레벨업)" : "")}");
            return true;
        }

        public void HealParty(bool reviveDowned)
        {
            if (PartyManager.Instance == null) return;
            PartyManager.Instance.HealAllParty(reviveDowned);
            Log(CheatCategory.Party, reviveDowned ? "파티 전체 회복 + 부활" : "파티 전체 회복");
        }

        public void ResetSwapCooldowns()
        {
            if (PartyManager.Instance == null) return;
            PartyManager.Instance.ClearAllSwapCooldowns();
            Log(CheatCategory.Party, "스왑 쿨타임 초기화");
        }
    }
}
#endif
