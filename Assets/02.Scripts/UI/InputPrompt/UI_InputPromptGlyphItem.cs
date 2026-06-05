using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.UI.InputPrompt;

/// <summary>
/// 콤보(복합 바인딩) 표시용 글리프 1개 항목. UI_InputPromptIcon이 템플릿으로 복제해 사용한다.
/// 항목 자신에 "+" 같은 구분자(_separator)를 자식으로 포함하고, 두 번째 파트부터만 켠다.
/// </summary>
public class UI_InputPromptGlyphItem : MonoBehaviour
{
    [Tooltip("앞 항목과의 구분자(예: \"+\"). index>0일 때만 표시")]
    [SerializeField] private GameObject _separator;
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _label; // 스프라이트 미등록 시 폴백

    public void Set(in GlyphPart part, bool showSeparator)
    {
        if (_separator != null)
            _separator.SetActive(showSeparator);

        bool hasSprite = part.HasSprite;

        if (_icon != null)
        {
            _icon.enabled = hasSprite;
            if (hasSprite)
                _icon.sprite = part.Sprite;
        }

        if (_label != null)
        {
            _label.enabled = !hasSprite;
            if (!hasSprite)
                _label.text = part.Text;
        }
    }
}
