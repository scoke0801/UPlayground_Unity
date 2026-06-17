using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 공통 드롭다운 컴포넌트
/// Unity TMP_Dropdown을 래핑하여 외부에서 간편하게 사용할 수 있도록 합니다.
/// </summary>
[RequireComponent(typeof(TMP_Dropdown))]
public class UICommonDropdown : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown _dropdown;

    // 값 변경 시 외부에서 구독
    public event Action<int> OnIndexChanged;
    public event Action<string> OnValueChanged;

    public int CurrentIndex
    {
        get
        {
            EnsureDropdown();
            return _dropdown.value;
        }
    }

    public string CurrentValue
    {
        get
        {
            EnsureDropdown();
            if (_dropdown.options.Count == 0)
                return string.Empty;

            int index = Mathf.Clamp(_dropdown.value, 0, _dropdown.options.Count - 1);
            return _dropdown.options[index].text;
        }
    }

    private void Awake()
    {
        EnsureDropdown();
        _dropdown.onValueChanged.AddListener(HandleValueChanged);
    }

    private void OnDestroy()
    {
        if (_dropdown != null)
            _dropdown.onValueChanged.RemoveListener(HandleValueChanged);
    }

    // --- Public API ---

    /// <summary>
    /// 옵션 목록 전체 교체 후 기본 인덱스로 초기화
    /// </summary>
    public void SetOptions(IEnumerable<string> options, int defaultIndex = 0)
    {
        EnsureDropdown();

        _dropdown.ClearOptions();
        _dropdown.AddOptions(new List<string>(options));

        if (_dropdown.options.Count == 0)
            return;

        // 이벤트 발생 없이 초기값 세팅
        _dropdown.SetValueWithoutNotify(Mathf.Clamp(defaultIndex, 0, _dropdown.options.Count - 1));
        _dropdown.RefreshShownValue();
    }

    /// <summary>
    /// 특정 인덱스로 이동 (이벤트 발생)
    /// </summary>
    public void SetIndex(int index)
    {
        EnsureDropdown();
        if (_dropdown.options.Count == 0)
            return;

        _dropdown.value = Mathf.Clamp(index, 0, _dropdown.options.Count - 1);
    }

    /// <summary>
    /// 특정 인덱스로 이동 (이벤트 미발생)
    /// </summary>
    public void SetIndexWithoutNotify(int index)
    {
        EnsureDropdown();
        if (_dropdown.options.Count == 0)
            return;

        _dropdown.SetValueWithoutNotify(Mathf.Clamp(index, 0, _dropdown.options.Count - 1));
        _dropdown.RefreshShownValue();
    }

    public void SetInteractable(bool interactable)
    {
        EnsureDropdown();
        _dropdown.interactable = interactable;
    }

    // --- Private ---

    private void HandleValueChanged(int index)
    {
        OnIndexChanged?.Invoke(index);
        if (index >= 0 && _dropdown.options.Count > index)
            OnValueChanged?.Invoke(_dropdown.options[index].text);
    }

    private void EnsureDropdown()
    {
        if (_dropdown == null)
            _dropdown = GetComponent<TMP_Dropdown>();
    }
}
