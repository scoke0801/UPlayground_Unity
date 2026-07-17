using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.EnumType;

namespace UPlayGround.UI
{
    /// <summary>
    /// 신규 게임 캐릭터 선택 화면의 캐릭터 카드.
    /// 선택/비선택 상태를 DOTween 트윈(스케일·이동·글로우 프레임·dim)으로 표현한다.
    /// </summary>
    public class UI_CharacterSelectCard : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button _button;
        [SerializeField] private RectTransform _content;     // 스케일/이동 대상
        [SerializeField] private CanvasGroup _canvasGroup;   // dim 처리
        [SerializeField] private Image _portrait;
        [SerializeField] private CanvasGroup _selectedFrame;  // 선택 강조 테두리(페이드)
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private GameObject _lockedOverlay;  // 잠금(비활성) 오버레이

        [Header("Tween")]
        [SerializeField] private float _selectedScale = 1.12f;
        [SerializeField] private float _selectedLift = 24f;
        [Tooltip("비선택 카드의 알파. 1이면 흐려지지 않음(잠금 카드만 별도로 어둡게 표시).")]
        [SerializeField] private float _dimAlpha = 1f;
        [SerializeField] private float _duration = 0.25f;

        private UI_CharacterSelect _parent;
        private int _index;
        private CharacterActorType _characterType;
        private Vector2 _baseAnchoredPos;
        private Sequence _seq;
        private bool _locked;

        public CharacterActorType CharacterType => _characterType;
        public int Index => _index;
        public bool IsLocked => _locked;

        private void Awake()
        {
            if (_button == null) _button = GetComponent<Button>();
            if (_content == null) _content = (RectTransform)transform;
            _baseAnchoredPos = _content.anchoredPosition;

            if (_button != null) _button.onClick.AddListener(OnClicked);
        }

        public void Init(UI_CharacterSelect parent, int index, CharacterActorType type, string displayName, Sprite portrait, bool locked)
        {
            _parent = parent;
            _index = index;
            _characterType = type;
            _locked = locked;

            if (_nameText != null)
                _nameText.text = string.IsNullOrEmpty(displayName) ? type.ToString() : displayName;

            if (_portrait != null)
            {
                _portrait.sprite = portrait;
                _portrait.enabled = portrait != null;
            }

            // 잠긴 카드는 클릭을 받지 않고 자물쇠 오버레이를 노출한다.
            if (_button != null) _button.interactable = !locked;
            if (_lockedOverlay != null) _lockedOverlay.SetActive(locked);

            ResetInstant();
        }

        /// <summary> 트윈 없이 중립 상태로 즉시 되돌린다. </summary>
        public void ResetInstant()
        {
            _seq?.Kill();
            if (_canvasGroup != null) _canvasGroup.DOKill();

            if (_content != null)
            {
                _content.localScale = Vector3.one;
                _content.anchoredPosition = _baseAnchoredPos;
            }
            if (_canvasGroup != null) _canvasGroup.alpha = 1f;
            if (_selectedFrame != null)
            {
                SetFrameAlpha(0f);
                _selectedFrame.gameObject.SetActive(false);
            }
        }

        public void SetSelected(bool selected, bool animate)
        {
            _seq?.Kill();
            float dur = animate ? _duration : 0f;

            if (_selectedFrame != null && selected)
                _selectedFrame.gameObject.SetActive(true);

            _seq = DOTween.Sequence().SetUpdate(true); // 일시정지(timeScale=0) 중에도 동작
            if (_content != null)
            {
                _seq.Join(_content.DOScale(selected ? _selectedScale : 1f, dur).SetEase(Ease.OutBack));
                Vector2 targetPosition = selected
                    ? _baseAnchoredPos + Vector2.up * _selectedLift
                    : _baseAnchoredPos;
                _seq.Join(DOTween.To(
                    () => _content.anchoredPosition,
                    value => _content.anchoredPosition = value,
                    targetPosition,
                    dur).SetEase(Ease.OutCubic));
            }
            if (_selectedFrame != null)
                _seq.Join(DOTween.To(
                    () => _selectedFrame.alpha,
                    value => _selectedFrame.alpha = value,
                    selected ? 1f : 0f,
                    dur));

            if (!selected && _selectedFrame != null)
                _seq.OnComplete(() =>
                {
                    if (_selectedFrame != null) _selectedFrame.gameObject.SetActive(false);
                });
        }

        public void SetDimmed(bool dimmed, bool animate)
        {
            if (_canvasGroup == null) return;
            _canvasGroup.DOKill();
            float dur = animate ? _duration : 0f;
            DOTween.To(
                () => _canvasGroup.alpha,
                value => _canvasGroup.alpha = value,
                dimmed ? _dimAlpha : 1f,
                dur).SetUpdate(true);
        }

        private void SetFrameAlpha(float a)
        {
            if (_selectedFrame == null) return;
            _selectedFrame.alpha = a;
        }

        private void OnClicked()
        {
            if (_locked) return;
            _parent?.OnCardClicked(_index);
        }

        private void OnDestroy()
        {
            _seq?.Kill();
            if (_canvasGroup != null) _canvasGroup.DOKill();
        }
    }
}
