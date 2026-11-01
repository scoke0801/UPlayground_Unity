using TMPro;
using UnityEngine;
using System.Collections.Generic;
using UPlayGround.Data.Combat;

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
        [SerializeField] private UIInputPromptIcon[] _sequenceIcons;
        [SerializeField] private CanvasGroup[] _sequenceGroups;
        [SerializeField] private TMP_Text _progressLabel;
        [SerializeField] private Color _activeColor = new(0.87f, 0.78f, 0.55f);
        private readonly List<ComboInputToken> _displayedTokens = new();

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

        /// <summary>入力列の達成部分と次の入力を区別し、再バインドは列の変更時だけ行う。</summary>
        public void SetSequence(IReadOnlyList<ComboInputToken> tokens, int matched, string label, string status)
        {
            if (_skillLabel != null && _skillLabel.text != label) _skillLabel.text = label;
            if (_progressLabel != null && _progressLabel.text != status) _progressLabel.text = status;
            if (_skillLabel != null) _skillLabel.color = matched > 0 ? _activeColor : Color.white;
            if (_sequenceIcons == null) return;
            int count = Mathf.Min(tokens.Count, _sequenceIcons.Length);
            int offset = Mathf.Max(0, tokens.Count - count);
            for (int i = 0; i < _sequenceIcons.Length; i++)
            {
                bool visible = i < count;
                if (_sequenceIcons[i].gameObject.activeSelf != visible)
                    _sequenceIcons[i].gameObject.SetActive(visible);
                if (!visible) continue;
                if (i >= _displayedTokens.Count || _displayedTokens[i] != tokens[offset + i])
                {
                    if (ComboTokenInput.TryGetAction(tokens[offset + i], out string map, out string action, out _))
                        _sequenceIcons[i].SetAction(map, action);
                }
                if (_sequenceGroups != null && i < _sequenceGroups.Length)
                {
                    float alpha = offset + i < matched ? 0.8f : 0.6f;
                    if (matched >= tokens.Count || offset + i == matched) alpha = 1f;
                    _sequenceGroups[i].alpha = alpha;
                }
            }
            _displayedTokens.Clear();
            for (int i = 0; i < count; i++) _displayedTokens.Add(tokens[offset + i]);
        }
    }
}
