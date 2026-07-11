using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UPlayGround.UI
{
    [RequireComponent(typeof(Slider))]
    public class UICommonSlider : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _minText;
        [SerializeField] private TextMeshProUGUI _maxText;

        // null이면 표시 안 함 (선택적 사용)
        [SerializeField] private TextMeshProUGUI _currentValueText;

        // 현재값 포맷 (정수: "0", 소수점 1자리: "0.0" 등)
        [SerializeField] private string _valueFormat = "0";

        public event Action<float> OnValueChanged;

        private Slider _slider;

        public float Value
        {
            get
            {
                EnsureSlider();
                return _slider.value;
            }
        }

        private void Awake()
        {
            EnsureSlider();
            UpdateLabels();
            _slider.onValueChanged.AddListener(HandleValueChanged);
        }

        private void OnDestroy()
        {
            if (_slider != null)
                _slider.onValueChanged.RemoveListener(HandleValueChanged);
        }

        // --- Public API ---

        public void SetValueWithoutNotify(float value)
        {
            EnsureSlider();
            _slider.SetValueWithoutNotify(value);
            UpdateLabels();
        }

        public void SetInteractable(bool interactable)
        {
            EnsureSlider();
            _slider.interactable = interactable;
        }

        // --- Private ---

        private void HandleValueChanged(float value)
        {
            UpdateLabels();
            OnValueChanged?.Invoke(value);
        }

        private void UpdateLabels()
        {
            EnsureSlider();

            if (_minText)          _minText.text = _slider.minValue.ToString("0");
            if (_maxText)          _maxText.text = _slider.maxValue.ToString("0");
            if (_currentValueText) _currentValueText.text = _slider.value.ToString(_valueFormat);
        }

        private void EnsureSlider()
        {
            if (_slider == null)
                _slider = GetComponent<Slider>();
        }
    }
}
