using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Manager;

/// <summary>
/// 세이브 슬롯 선택 UI (최대 3슬롯). 저장 모드/로드 모드를 공유한다.
/// - 포즈 메뉴 → 저장 모드: 슬롯 클릭 시 현재 상태를 그 슬롯에 저장.
/// - 타이틀 메뉴 → 로드 모드: 슬롯 클릭 시 해당 슬롯을 로드하고 저장된 씬으로 진입.
///
/// 프리팹: 3개 슬롯 행(버튼 + 정보 텍스트 + 삭제 버튼)을 인스펙터에 연결한다.
/// </summary>
public class UI_SaveSlotMenu : UI_Base
{
    public enum SaveSlotMode { Save, Load }

    /// <summary>
    /// UIManager 등록 키. UIKeyType은 프리팹에서 자동 생성되므로,
    /// 프리팹·Addressable 키를 이 문자열과 동일하게("SaveSlotMenu") 만들어야 한다.
    /// </summary>
    public const string UIKey = "SaveSlotMenu";

    [Serializable]
    private class SlotRow
    {
        public Button selectButton;
        public TextMeshProUGUI infoText;
        public Button deleteButton;
    }

    [Header("모드 표시")]
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private string _saveTitle = "저장할 슬롯 선택";
    [SerializeField] private string _loadTitle = "불러올 슬롯 선택";

    [Header("슬롯 행 (최대 3개)")]
    [SerializeField] private SlotRow[] _slots = new SlotRow[SaveManager.MAX_SLOTS];

    [Header("닫기 버튼 (선택)")]
    [SerializeField] private Button _closeButton;

    private SaveSlotMode _mode = SaveSlotMode.Save;

    protected override void OnInit()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            int slot = i;  // 클로저 캡처 방지
            var row = _slots[i];
            if (row == null) continue;

            if (row.selectButton != null)
                row.selectButton.onClick.AddListener(() => OnSlotClicked(slot));
            if (row.deleteButton != null)
                row.deleteButton.onClick.AddListener(() => OnDeleteClicked(slot));
        }

        if (_closeButton != null)
            _closeButton.onClick.AddListener(() => UIManager.Instance.HideUI(UIKey));
    }

    protected override void OnShow()
    {
        base.OnShow();
        Refresh();
    }

    /// <summary> 저장/로드 모드를 설정하고 슬롯 목록을 갱신한다. UI를 띄운 직후 호출한다. </summary>
    public void SetMode(SaveSlotMode mode)
    {
        _mode = mode;
        Refresh();
    }

    private void Refresh()
    {
        if (_titleText != null)
            _titleText.text = _mode == SaveSlotMode.Save ? _saveTitle : _loadTitle;

        var infos = SaveManager.Instance.GetAllSlotInfos();
        for (int i = 0; i < _slots.Length; i++)
        {
            var row = _slots[i];
            if (row == null) continue;

            var info = i < infos.Length ? infos[i] : null;
            bool hasSave = info != null;

            if (row.infoText != null)
            {
                row.infoText.text = hasSave
                    ? $"슬롯 {i + 1}\n{info.saveDateTime}\n맵: {info.mapId}  진행도: {info.storyProgress}"
                    : $"슬롯 {i + 1}\n- 비어 있음 -";
            }

            // 로드 모드에서 빈 슬롯은 선택 불가. 저장 모드는 빈 슬롯도 선택 가능(새 저장).
            if (row.selectButton != null)
                row.selectButton.interactable = _mode == SaveSlotMode.Save || hasSave;

            if (row.deleteButton != null)
                row.deleteButton.gameObject.SetActive(hasSave);
        }
    }

    private void OnSlotClicked(int slot)
    {
        if (_mode == SaveSlotMode.Save)
        {
            SaveManager.Instance.SaveGame(slot);
            Refresh();
        }
        else
        {
            if (!SaveManager.Instance.HasSaveFile(slot)) return;
            UIManager.Instance.HideAllUI();
            SaveManager.Instance.LoadGameToScene(slot);
        }
    }

    private void OnDeleteClicked(int slot)
    {
        SaveManager.Instance.DeleteSaveFile(slot);
        Refresh();
    }
}
