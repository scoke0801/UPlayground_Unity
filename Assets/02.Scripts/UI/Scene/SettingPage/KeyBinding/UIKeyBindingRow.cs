using System;
using TMPro;
using UPlayGround.InputDefine;
using UnityEngine;
using UnityEngine.UI;

namespace UPlayGround.UI
{
    public sealed class UIKeyBindingRow : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _actionLabel;
        [SerializeField] private Button _primaryButton;
        [SerializeField] private TextMeshProUGUI _primaryLabel;
        [SerializeField] private Button _secondaryButton;
        [SerializeField] private TextMeshProUGUI _secondaryLabel;
        [SerializeField] private Button _resetButton;

        private InputBindingTarget _primaryTarget;
        private InputBindingTarget _secondaryTarget;
        private Action<InputBindingTarget> _onRebind;
        private Action<InputBindingTarget> _onReset;

        private void Awake()
        {
            _primaryButton?.onClick.AddListener(OnPrimaryClicked);
            _secondaryButton?.onClick.AddListener(OnSecondaryClicked);
            _resetButton?.onClick.AddListener(OnResetClicked);
        }

        private void OnDestroy()
        {
            _primaryButton?.onClick.RemoveListener(OnPrimaryClicked);
            _secondaryButton?.onClick.RemoveListener(OnSecondaryClicked);
            _resetButton?.onClick.RemoveListener(OnResetClicked);
        }

        public void Configure(
            InputBindingDescriptor primary,
            InputBindingDescriptor secondary,
            Action<InputBindingTarget> onRebind,
            Action<InputBindingTarget> onReset)
        {
            _primaryTarget = primary.Target;
            _secondaryTarget = secondary.Target;
            _onRebind = onRebind;
            _onReset = onReset;

            if (_actionLabel != null)
                _actionLabel.text = primary.DisplayName;
            if (_primaryLabel != null)
                _primaryLabel.text = primary.BindingDisplay;
            if (_secondaryLabel != null)
                _secondaryLabel.text = secondary.BindingDisplay;
        }

        private void OnPrimaryClicked() => _onRebind?.Invoke(_primaryTarget);
        private void OnSecondaryClicked() => _onRebind?.Invoke(_secondaryTarget);

        private void OnResetClicked()
        {
            _onReset?.Invoke(_primaryTarget);
            _onReset?.Invoke(_secondaryTarget);
        }
    }
}
