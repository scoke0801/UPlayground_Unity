// ============================================================
// AUTO-GENERATED — GameplayTagRegistry Editor
// UPlayGround/GameplayTag/Tag Registry Editor 에서 관리하세요.
// 직접 편집하지 마세요. 저장 후 에디터에서 "코드 생성"을 눌러야 반영됩니다.
// Generated: 2026-04-16 20:47
// ============================================================

namespace UPlayGround.Gameplay.Tag
{
    /// <summary>
    /// GameplayTag 식별자 열거형.
    /// GameplayTagRegistrySO + Tag Registry Editor 에서 관리하며 코드가 자동 생성된다.
    /// 코드에서는 반드시 이 enum을 사용하고, 문자열을 직접 쓰지 않는다.
    /// </summary>
    public enum GameplayTagId
    {
        None = 0,
        State_Move              = 1,
        State_Sprint            = 2,
        State_Dash              = 3,
        State_Jump              = 4,
        State_Airborne          = 5,
        State_Crouching         = 6,
        State_Dodge             = 7,
        State_Combat            = 8,
        State_Combat_Attack     = 9,
        State_Combat_Guard      = 10,
        State_Combat_Charge     = 11,
        State_Combat_DashAttack = 12,
        State_Combat_JumpAttack = 13,
        State_Hit               = 14,
        State_Death             = 15,
        State_Grabbed           = 16,
        State_Interaction       = 17,
        Combo_Light             = 18,
        Combo_Heavy             = 19,
        State_Combat_Counter      = 20,
        State_Combat_ParryCounter = 21,
    }

    /// <summary>
    /// GameplayTagId → GameplayTag 변환 확장 메서드.
    /// </summary>
    public static class GameplayTagIdExtensions
    {
        private static readonly string[] s_TagNames = new string[]
        {
            "",  // None = 0
            "State.Move",                 // State_Move = 1  (이동 중)
            "State.Sprint",               // State_Sprint = 2  (전력 질주 중)
            "State.Dash",                 // State_Dash = 3  (대시 중)
            "State.Jump",                 // State_Jump = 4  (점프 입력 진입)
            "State.Airborne",             // State_Airborne = 5  (공중 상태)
            "State.Crouching",            // State_Crouching = 6  (웅크리는 중)
            "State.Dodge",                // State_Dodge = 7  (회피 중)
            "State.Combat",               // State_Combat = 8  (전투 상태 (부모))
            "State.Combat.Attack",        // State_Combat_Attack = 9  (공격 중)
            "State.Combat.Guard",         // State_Combat_Guard = 10  (가드 중)
            "State.Combat.Charge",        // State_Combat_Charge = 11  (차지 중)
            "State.Combat.DashAttack",    // State_Combat_DashAttack = 12  (대시 공격 중)
            "State.Combat.JumpAttack",    // State_Combat_JumpAttack = 13  (점프 공격 중)
            "State.Hit",                  // State_Hit = 14  (피격 중)
            "State.Death",                // State_Death = 15  (사망)
            "State.Grabbed",              // State_Grabbed = 16  (잡힌 상태)
            "State.Interaction",          // State_Interaction = 17  (상호작용 중)
            "Combo.Light",                // Combo_Light = 18  (콤보: 약 공격 입력됨)
            "Combo.Heavy",                // Combo_Heavy = 19  (콤보: 강 공격 입력됨)
            "State.Combat.Counter",       // State_Combat_Counter = 20
            "State.Combat.ParryCounter",  // State_Combat_ParryCounter = 21
        };

        /// <summary>GameplayTagId를 GameplayTag 구조체로 변환한다.</summary>
        public static GameplayTag ToTag(this GameplayTagId id)
        {
            int i = (int)id;
            return new GameplayTag(i >= 0 && i < s_TagNames.Length ? s_TagNames[i] : string.Empty);
        }

        /// <summary>GameplayTagId의 태그 이름 문자열을 반환한다.</summary>
        public static string TagName(this GameplayTagId id)
        {
            int i = (int)id;
            return i >= 0 && i < s_TagNames.Length ? s_TagNames[i] : string.Empty;
        }
    }
}
