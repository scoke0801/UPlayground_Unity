using UnityEngine.InputSystem;
using UPlayGround.InputDefine;
using UPlayGround.Manager;

namespace UPlayGround.UI.InputPrompt
{
    /// <summary>
    /// 입력 프롬프트가 특정 장치에서 유효한지 가볍게 판정한다.
    /// 글리프 데이터나 표시 문자열을 만들지 않으므로 표시 여부 확인에 사용할 수 있다.
    /// </summary>
    public static class InputPromptAvailability
    {
        public static bool HasBindingFor(
            string mapName,
            string actionName,
            ActiveInputDevice device)
        {
            IInputService input = Svc.Input;
            InputAction action = input?.GetAction(mapName, actionName);
            return HasBindingFor(action, device);
        }

        public static bool HasBindingFor(InputAction action, ActiveInputDevice device)
            => InputGlyphResolver.HasBindingForAction(action, device);
    }
}
