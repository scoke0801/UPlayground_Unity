#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace UPlayGround.Manager
{
    /// <summary>CheatManager — 몬스터 도감 치트(대상 등록/제거). 개발 빌드 전용.</summary>
    public partial class CheatManager
    {
        /// <summary>도감 대상을 100% 기록 상태로 등록한다.</summary>
        public bool RegisterCodexTarget(string actorId, string displayName = null)
        {
            bool ok = MonsterCodexManager.Instance != null &&
                      MonsterCodexManager.Instance.CheatRegisterFull(actorId);
            if (ok) Log(CheatCategory.Codex, $"도감 등록(100% 기록): {displayName ?? actorId}");
            return ok;
        }

        /// <summary>도감 대상의 기록을 제거해 미발견 상태로 되돌린다.</summary>
        public bool RemoveCodexTarget(string actorId, string displayName = null)
        {
            bool ok = MonsterCodexManager.Instance != null &&
                      MonsterCodexManager.Instance.CheatRemove(actorId);
            if (ok) Log(CheatCategory.Codex, $"도감 제거: {displayName ?? actorId}");
            return ok;
        }
    }
}
#endif
