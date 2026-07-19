using UPlayGround.Data.Combat;
using UPlayGround.InputDefine;

namespace UPlayGround.UI.InputPrompt
{
    /// <summary>
    /// 연계 라우트 토큰(<see cref="ComboInputToken"/>)을 글리프 표시용 입력 액션으로 매핑한다.
    /// 차지(Charge)는 강공 버튼의 '홀드' 변형이라 같은 액션을 쓰되 <paramref name="isHold"/>로 구분한다.
    /// </summary>
    public static class ComboTokenInput
    {
        /// <summary>
        /// 토큰 → (액션맵, 액션명, 홀드여부). 글리프가 없는 토큰(향후 확장)은 false.
        /// </summary>
        public static bool TryGetAction(ComboInputToken token,
            out string mapName, out string actionName, out bool isHold)
        {
            mapName = InputMapNames.PlayerAction;
            isHold  = false;

            switch (token)
            {
                case ComboInputToken.LightAttack: actionName = PlayerAction.Attack;        return true;
                case ComboInputToken.HeavyAttack: actionName = PlayerAction.HeavyAttack;   return true;
                case ComboInputToken.Charge:      actionName = PlayerAction.HeavyAttack; isHold = true; return true;
                case ComboInputToken.Dodge:       actionName = PlayerAction.Dodge;         return true;
                case ComboInputToken.Dash:        actionName = PlayerAction.Dash;          return true;
                case ComboInputToken.Jump:        actionName = PlayerAction.Jump;          return true;
                case ComboInputToken.Skill1:      actionName = PlayerAction.SkillAbility;  return true;
                case ComboInputToken.Skill2:      actionName = PlayerAction.SkillUltimate; return true;
                case ComboInputToken.ElementalImbue:
                    actionName = PlayerAction.ElementBuff;
                    return true;
                default:                          actionName = null;                       return false;
            }
        }
    }
}
