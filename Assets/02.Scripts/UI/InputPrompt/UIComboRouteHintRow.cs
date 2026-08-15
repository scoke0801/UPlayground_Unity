using TMPro;
using UnityEngine;

namespace UPlayGround.UI.InputPrompt
{
    /// <summary>
    /// 콤보 라우트 힌트 한 줄: "다음에 누를 키(글리프) + 발동 스킬명".
    /// 글리프는 <see cref="UIInputPromptIcon"/>을 재사용해 활성 디바이스/브랜드 전환을 자동 처리한다.
    /// <see cref="UIComboRouteHint"/>가 템플릿으로 복제해 사용한다.
    /// </summary>
    public class UIComboRouteHintRow : MonoBehaviour
    {
        [SerializeField] private UIInputPromptIcon _keyIcon;
        [SerializeField] private TMP_Text           _skillLabel;
        [Tooltip("차지(홀드) 입력일 때 켜는 보조 표식(예: \"홀드\" 배지). 선택")]
        [SerializeField] private GameObject         _holdBadge;

        /// <summary>
        /// 이 행을 (다음 입력 액션, 스킬명, 홀드여부)로 갱신한다.
        /// </summary>
        public void Set(string mapName, string actionName, string skillName, bool isHold)
        {
            if (_keyIcon != null)
                _keyIcon.SetAction(mapName, actionName);

            if (_skillLabel != null)
                _skillLabel.text = skillName;

            if (_holdBadge != null)
                _holdBadge.SetActive(isHold);
        }
    }
}
