using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UISwitchButton : MonoBehaviour
{
    [SerializeField] private Button _switchButton;
    [SerializeField] private GameObject _offRoot;
    [SerializeField] private GameObject _onRoot;

    public event Action<bool> OnValueChanged;
    public bool IsOn = false;

    private void Awake()
    {
        if (_switchButton != null)
            _switchButton.onClick.AddListener(OnClickedSwitchButton);
    }

    private void Start()
    {
        RefreshVisual();
    }

    private void OnDestroy()
    {
        if (_switchButton != null)
            _switchButton.onClick.RemoveListener(OnClickedSwitchButton);
    }

    public void SetValueWithoutNotify(bool isOn)
    {
        IsOn = isOn;
        RefreshVisual();
    }

    private void OnClickedSwitchButton()
    {
        if(IsOn)
        {
            IsOn = false;
        }
        else
        {
            IsOn = true;
        }

        RefreshVisual();
        OnValueChanged?.Invoke(IsOn);
    }

    private void RefreshVisual()
    {
        if (_offRoot != null) _offRoot.SetActive(!IsOn);
        if (_onRoot != null) _onRoot.SetActive(IsOn);
    }
}
