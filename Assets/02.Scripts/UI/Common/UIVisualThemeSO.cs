using UnityEngine;

namespace UPlayGround.UI
{
    /// <summary>
    /// 런타임 UI와 에디터 프리팹 빌더가 공유하는 비주얼 토큰의 단일 원본.
    /// 프레임·버튼은 Layer Lab 주력 스킨을 사용하고, 프로젝트 전용 아이콘·초상화는
    /// 의미 리소스로만 조합하는 것을 전제로 한다.
    /// </summary>
    [CreateAssetMenu(
        menuName = "UPlayGround/UI/비주얼 테마",
        fileName = "UIVisualTheme")]
    public sealed class UIVisualThemeSO : ScriptableObject
    {
        [Header("주력 스킨")]
        [SerializeField] private Sprite _panelFrame;
        [SerializeField] private Sprite _buttonFrame;
        [SerializeField] private Sprite _tabFocusFrame;
        [SerializeField] private Sprite _cardFrame;
        [SerializeField] private Sprite _slotFocusFrame;

        [Header("표면 색")]
        [SerializeField] private Color _screenDim = new(0.01f, 0.015f, 0.025f, 0.78f);
        [SerializeField] private Color _panel = new(0.045f, 0.06f, 0.08f, 0.97f);
        [SerializeField] private Color _surface = new(0.08f, 0.105f, 0.13f, 0.96f);
        [SerializeField] private Color _surfaceRaised = new(0.12f, 0.15f, 0.18f, 0.98f);
        [SerializeField] private Color _focus = new(0.82f, 0.65f, 0.32f, 1f);

        [Header("텍스트·상태 색")]
        [SerializeField] private Color _textMain = new(0.96f, 0.97f, 0.98f, 1f);
        [SerializeField] private Color _textSub = new(0.72f, 0.76f, 0.80f, 1f);
        [SerializeField] private Color _textMuted = new(0.46f, 0.50f, 0.54f, 1f);
        [SerializeField] private Color _positive = new(0.34f, 0.82f, 0.53f, 1f);
        [SerializeField] private Color _negative = new(0.90f, 0.30f, 0.32f, 1f);
        [SerializeField] private Color _warning = new(0.95f, 0.70f, 0.25f, 1f);
        [SerializeField] private Color _disabled = new(0.36f, 0.39f, 0.43f, 1f);

        [Header("타이포그래피 단계 (2560x1440 기준)")]
        [SerializeField, Min(1f)] private float _titleSize = 40f;
        [SerializeField, Min(1f)] private float _headingSize = 32f;
        [SerializeField, Min(1f)] private float _bodySize = 24f;
        [SerializeField, Min(1f)] private float _labelSize = 20f;
        [SerializeField, Min(1f)] private float _captionSize = 18f;

        [Header("소형 상호작용 연출")]
        [SerializeField, Range(0.01f, 0.5f)] private float _focusDuration = 0.10f;
        [SerializeField, Range(1f, 1.15f)] private float _focusScale = 1.035f;
        [SerializeField, Range(0.8f, 1f)] private float _pressedScale = 0.96f;

        public Sprite PanelFrame => _panelFrame;
        public Sprite ButtonFrame => _buttonFrame;
        public Sprite TabFocusFrame => _tabFocusFrame;
        public Sprite CardFrame => _cardFrame;
        public Sprite SlotFocusFrame => _slotFocusFrame;

        public Color ScreenDim => _screenDim;
        public Color Panel => _panel;
        public Color Surface => _surface;
        public Color SurfaceRaised => _surfaceRaised;
        public Color Focus => _focus;
        public Color TextMain => _textMain;
        public Color TextSub => _textSub;
        public Color TextMuted => _textMuted;
        public Color Positive => _positive;
        public Color Negative => _negative;
        public Color Warning => _warning;
        public Color Disabled => _disabled;

        public float TitleSize => _titleSize;
        public float HeadingSize => _headingSize;
        public float BodySize => _bodySize;
        public float LabelSize => _labelSize;
        public float CaptionSize => _captionSize;
        public float FocusDuration => _focusDuration;
        public float FocusScale => _focusScale;
        public float PressedScale => _pressedScale;
    }
}
