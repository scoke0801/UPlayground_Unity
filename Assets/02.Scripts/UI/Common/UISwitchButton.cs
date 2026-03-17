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
        _switchButton.onClick.AddListener(OnClickedSwitchButton);
    }

    private void Start()
    {
        if (IsOn)
        {
            _offRoot.SetActive(false);
            _onRoot.SetActive(true);
        }
        else
        {
            _offRoot.SetActive(true);
            _onRoot.SetActive(false);
        }
    }

    private void OnClickedSwitchButton()
    {
        if(IsOn)
        {
            _offRoot.SetActive(true);
            _onRoot.SetActive(false);
            IsOn = false;
        }
        else
        {
            _offRoot.SetActive(false);
            _onRoot.SetActive(true);
            IsOn = true;
        }
        
        OnValueChanged?.Invoke(IsOn);
    }
}