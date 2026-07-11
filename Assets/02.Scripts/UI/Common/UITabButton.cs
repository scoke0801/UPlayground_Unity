using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UPlayGround.UI
{
    /// <summary>
    /// 재사용 탭 버튼 — 선택/비선택 시각 상태를 관리한다.
    ///
    /// 전환 요소(모두 선택):
    ///   - 배경 Image 색 (_tintBackground)
    ///   - 라벨 색 (_tintLabel)
    ///   - 선택 인디케이터 GameObject(밑줄/오버레이 등) 활성/비활성
    ///
    /// 클릭은 <see cref="Clicked"/> 이벤트로 노출한다. <see cref="UITabGroup"/>과 함께 쓰면
    /// 그룹이 단일 선택을 관리하고, 단독으로 쓸 경우 직접 <see cref="SetSelected"/>를 호출한다.
    /// UI_Base 파생이 아니므로 접두사 규약상 UITabButton으로 명명.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UITabButton : MonoBehaviour
    {
        [SerializeField] private Button           _button;
        [SerializeField] private Image            _background;         // 선택 시 색이 바뀌는 배경(보통 Button.targetGraphic)
        [SerializeField] private TextMeshProUGUI  _label;             // 선택 시 색이 바뀌는 라벨
        [SerializeField] private GameObject       _selectedIndicator; // 선택 시에만 켜지는 밑줄/오버레이(선택)

        [Header("배경 색")]
        [SerializeField] private bool  _tintBackground = true;
        [SerializeField] private Color _normalBg   = new Color(0.20f, 0.28f, 0.34f, 1f);
        [SerializeField] private Color _selectedBg = new Color(0.18f, 0.45f, 0.55f, 1f);

        [Header("라벨 색")]
        [SerializeField] private bool  _tintLabel = true;
        [SerializeField] private Color _normalText   = new Color(0.65f, 0.70f, 0.76f, 1f);
        [SerializeField] private Color _selectedText = new Color(0.96f, 0.98f, 1f,   1f);

        public Button Button      => _button;
        public bool   IsSelected  { get; private set; }

        /// <summary> 버튼이 클릭됐을 때 발생. </summary>
        public event Action Clicked;

        // 에디터에서 컴포넌트 추가 시 참조 자동 채움
        private void Reset()
        {
            _button     = GetComponent<Button>();
            _background = GetComponent<Image>();
            _label      = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        private void Awake()
        {
            if (_button == null) _button = GetComponent<Button>();
            _button?.onClick.AddListener(OnButtonClicked);
            ApplyVisual();
        }

        private void OnDestroy()
        {
            _button?.onClick.RemoveListener(OnButtonClicked);
        }

        private void OnButtonClicked() => Clicked?.Invoke();

        /// <summary> 선택 상태를 설정하고 시각을 갱신한다. </summary>
        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            ApplyVisual();
        }

        private void ApplyVisual()
        {
            if (_tintBackground && _background != null)
                _background.color = IsSelected ? _selectedBg : _normalBg;

            if (_tintLabel && _label != null)
                _label.color = IsSelected ? _selectedText : _normalText;

            if (_selectedIndicator != null)
                _selectedIndicator.SetActive(IsSelected);
        }

        /// <summary> 에디터/빌더에서 참조와 색을 일괄 주입할 때 사용. </summary>
        public void Configure(Button button, Image background, TextMeshProUGUI label,
                              Color normalBg, Color selectedBg, Color normalText, Color selectedText)
        {
            _button       = button;
            _background   = background;
            _label        = label;
            _normalBg     = normalBg;
            _selectedBg   = selectedBg;
            _normalText   = normalText;
            _selectedText = selectedText;
        }
    }
}
