using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Dialogue;

namespace UPlayGround.UI
{
    /// <summary>
    /// 대화 이력 패널의 한 줄. 화자명·본문·소형 초상화를 표시합니다.
    /// 본문은 색상 태그가 포함된 리치 문자열을 그대로 사용합니다(§인라인 색상).
    /// </summary>
    public class UIDialogueBacklogEntry : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI speakerText;
        [SerializeField] private TextMeshProUGUI bodyText;
        [SerializeField] private Image portraitImage;

        public void Setup(in DialogueLogEntry entry)
        {
            if (speakerText != null)
            {
                speakerText.text = entry.SpeakerName;
                speakerText.gameObject.SetActive(!string.IsNullOrEmpty(entry.SpeakerName));
            }

            if (bodyText != null)
            {
                // 이력은 항상 전체가 보여야 하므로 타이핑 제한을 걸지 않는다.
                bodyText.maxVisibleCharacters = int.MaxValue;
                bodyText.text = entry.RichBody;
            }

            if (portraitImage != null)
            {
                portraitImage.sprite = entry.Portrait;
                portraitImage.preserveAspect = true;
                portraitImage.gameObject.SetActive(entry.Portrait != null);
            }
        }
    }
}
