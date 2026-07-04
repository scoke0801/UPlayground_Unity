using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Manager;

/// <summary>
/// 세이브 슬롯 선택 UI. 저장 모드/로드 모드를 공유한다.
/// - 포즈 메뉴 → 저장 모드: 슬롯 클릭 시 현재 상태를 그 슬롯에 저장.
/// - 타이틀 메뉴 → 로드 모드: 슬롯 클릭 시 해당 슬롯을 로드하고 저장된 씬으로 진입.
///
/// 프리팹: 슬롯 행 템플릿(버튼 + 정보 텍스트 + 삭제 버튼)을 인스펙터에 연결한다.
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
        public GameObject root;
        public Button selectButton;
        public Button deleteButton;
        [Tooltip("슬롯 선택 버튼의 라벨(선택). 연결 시 모드에 따라 '저장'/'불러오기'로 갱신된다.")]
        public TextMeshProUGUI selectLabel;

        [Header("정보 표시 (모두 선택 — 연결된 것만 갱신)")]
        [Tooltip("레거시 통합 정보 텍스트. 세분화 필드를 쓰면 비워도 된다.")]
        public TextMeshProUGUI infoText;
        [Tooltip("슬롯 썸네일 이미지. 캡처 파일이 없으면 플레이스홀더 색으로 표시한다.")]
        public Image thumbnail;
        [Tooltip("세이브 유무 상태 문구 ('세이브 있음'/'비어 있음').")]
        public TextMeshProUGUI statusText;
        [Tooltip("저장 일시.")]
        public TextMeshProUGUI dateText;
        [Tooltip("맵 식별자 ('맵: {mapId}').")]
        public TextMeshProUGUI mapText;
        [Tooltip("스토리 진행도 ('진행도: {n}').")]
        public TextMeshProUGUI progressText;
    }

    [Header("모드 표시")]
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private string _saveTitle = "저장할 슬롯 선택";
    [SerializeField] private string _loadTitle = "불러올 슬롯 선택";
    [SerializeField] private string _saveButtonLabel = "저장";
    [SerializeField] private string _loadButtonLabel = "불러오기";

    [Header("슬롯 상태 문구")]
    [SerializeField] private string _slotFilledLabel = "세이브 있음";
    [SerializeField] private string _slotEmptyLabel = "비어 있음";

    [Tooltip("썸네일이 없는(빈) 슬롯의 플레이스홀더 색.")]
    [SerializeField] private Color _emptyThumbnailColor = new Color(0.03f, 0.04f, 0.06f, 1f);

    [Header("슬롯 행")]
    [Tooltip("동적으로 생성된 슬롯 행이 배치될 Content Transform. 비어 있으면 템플릿 행의 부모를 사용한다.")]
    [SerializeField] private Transform _slotRoot;
    [Tooltip("동적 생성에 사용할 슬롯 행 템플릿. 비어 있으면 레거시 _slots[0] 행을 템플릿으로 사용한다.")]
    [SerializeField] private SlotRow _slotTemplate;
    [Tooltip("레거시 고정 슬롯 행. 기존 프리팹 호환용이며 새 빌더에서는 템플릿 1개만 사용한다.")]
    [SerializeField] private SlotRow[] _slots = Array.Empty<SlotRow>();

    [Header("닫기 버튼 (선택)")]
    [Tooltip("주 닫기 버튼(예: 상단 X).")]
    [SerializeField] private Button _closeButton;
    [Tooltip("보조 닫기 버튼(예: 하단 '닫기'). 연결 시 동일하게 닫기 동작.")]
    [SerializeField] private Button _closeButtonAlt;

    private SaveSlotMode _mode = SaveSlotMode.Save;
    private readonly List<SlotRow> _activeRows = new List<SlotRow>();
    private readonly List<int> _activeSlotIndices = new List<int>();
    private readonly List<GameObject> _spawnedRows = new List<GameObject>();
    private GameObject _templateRoot;

    protected override void OnInit()
    {
        ResolveTemplate();

        if (_closeButton != null)
            _closeButton.onClick.AddListener(() => UIManager.Instance.HideUI(UIKey));
        if (_closeButtonAlt != null)
            _closeButtonAlt.onClick.AddListener(() => UIManager.Instance.HideUI(UIKey));
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

        RebuildRows();

        for (int i = 0; i < _activeRows.Count; i++)
        {
            var row = _activeRows[i];
            if (row == null) continue;

            int slot = i < _activeSlotIndices.Count ? _activeSlotIndices[i] : i;
            var info = SaveManager.Instance.GetSaveSlotInfo(slot);
            bool hasSave = info != null;

            // 레거시 통합 텍스트(연결돼 있을 때만).
            if (row.infoText != null)
            {
                row.infoText.text = hasSave
                    ? $"슬롯 {slot + 1}\n{info.saveDateTime}\n맵: {info.mapId}  진행도: {info.storyProgress}"
                    : $"슬롯 {slot + 1}\n- 비어 있음 -";
            }

            // 썸네일(연결돼 있을 때만). 세이브가 있으면 캡처 스프라이트, 없으면 회색 플레이스홀더.
            if (row.thumbnail != null)
            {
                var thumb = hasSave ? SaveManager.Instance.GetSlotThumbnail(slot) : null;
                if (thumb != null)
                {
                    row.thumbnail.sprite = thumb;
                    row.thumbnail.type = Image.Type.Simple;
                    row.thumbnail.preserveAspect = true;
                    row.thumbnail.color = Color.white;
                }
                else
                {
                    row.thumbnail.sprite = null;
                    row.thumbnail.color = _emptyThumbnailColor;
                }
            }

            // 세분화 정보 필드(연결된 것만 갱신).
            if (row.statusText != null)
                row.statusText.text = hasSave ? _slotFilledLabel : _slotEmptyLabel;
            if (row.dateText != null)
                row.dateText.text = hasSave ? info.saveDateTime : "-";
            if (row.mapText != null)
                row.mapText.text = hasSave ? $"맵: {info.mapId}" : "맵: -";
            if (row.progressText != null)
                row.progressText.text = $"진행도: {(hasSave ? info.storyProgress : 0)}";

            // 슬롯 버튼 라벨을 모드에 맞게 갱신(연결돼 있을 때만).
            if (row.selectLabel != null)
                row.selectLabel.text = _mode == SaveSlotMode.Save ? _saveButtonLabel : _loadButtonLabel;

            // 로드 모드에서 빈 슬롯은 선택 불가. 저장 모드는 빈 슬롯도 선택 가능(새 저장).
            if (row.selectButton != null)
                row.selectButton.interactable = _mode == SaveSlotMode.Save || hasSave;

            if (row.deleteButton != null)
                row.deleteButton.gameObject.SetActive(hasSave);
        }
    }

    private void RebuildRows()
    {
        ResolveTemplate();

        for (int i = 0; i < _spawnedRows.Count; i++)
        {
            if (_spawnedRows[i] != null)
            {
                _spawnedRows[i].SetActive(false);
                Destroy(_spawnedRows[i]);
            }
        }

        _spawnedRows.Clear();
        _activeRows.Clear();
        _activeSlotIndices.Clear();

        if (_templateRoot == null)
            return;

        _templateRoot.SetActive(false);

        var slots = SaveManager.Instance.GetSlotIndicesForMenu(_mode == SaveSlotMode.Save);
        foreach (int slot in slots)
        {
            var rowRoot = Instantiate(_templateRoot, _slotRoot != null ? _slotRoot : _templateRoot.transform.parent);
            rowRoot.name = $"Slot{slot}";
            rowRoot.SetActive(true);

            var row = BuildRowFromRoot(rowRoot);
            _spawnedRows.Add(rowRoot);
            _activeRows.Add(row);
            _activeSlotIndices.Add(slot);

            if (row.selectButton != null)
            {
                row.selectButton.onClick.RemoveAllListeners();
                row.selectButton.onClick.AddListener(() => OnSlotClicked(slot));
            }

            if (row.deleteButton != null)
            {
                row.deleteButton.onClick.RemoveAllListeners();
                row.deleteButton.onClick.AddListener(() => OnDeleteClicked(slot));
            }

            UpdateStaticSlotLabels(row, slot);
        }
    }

    private void ResolveTemplate()
    {
        if (_templateRoot != null)
            return;

        if (_slotTemplate != null)
            _templateRoot = ResolveRowRoot(_slotTemplate);

        if (_templateRoot == null && _slots != null && _slots.Length > 0 && _slots[0] != null)
        {
            _slotTemplate = _slots[0];
            _templateRoot = ResolveRowRoot(_slotTemplate);
        }

        if (_slotRoot == null && _templateRoot != null)
            _slotRoot = _templateRoot.transform.parent;

        if (_slots != null)
        {
            for (int i = 1; i < _slots.Length; i++)
            {
                var root = ResolveRowRoot(_slots[i]);
                if (root != null)
                    root.SetActive(false);
            }
        }
    }

    private static GameObject ResolveRowRoot(SlotRow row)
    {
        if (row == null)
            return null;
        if (row.root != null)
            return row.root;
        if (row.selectButton != null)
            return row.selectButton.transform.parent != null
                ? row.selectButton.transform.parent.parent != null
                    ? row.selectButton.transform.parent.parent.gameObject
                    : row.selectButton.transform.parent.gameObject
                : row.selectButton.gameObject;
        if (row.infoText != null)
            return row.infoText.transform.parent != null ? row.infoText.transform.parent.gameObject : row.infoText.gameObject;
        return null;
    }

    private static SlotRow BuildRowFromRoot(GameObject root)
    {
        return new SlotRow
        {
            root = root,
            selectButton = FindComponent<Button>(root.transform, "ButtonCol/SaveButton"),
            selectLabel = FindComponent<TextMeshProUGUI>(root.transform, "ButtonCol/SaveButton/Label"),
            deleteButton = FindComponent<Button>(root.transform, "ButtonCol/DeleteButton"),
            thumbnail = FindComponent<Image>(root.transform, "Thumbnail"),
            statusText = FindComponent<TextMeshProUGUI>(root.transform, "InfoCol/StatusText"),
            dateText = FindComponent<TextMeshProUGUI>(root.transform, "InfoCol/DateText"),
            mapText = FindComponent<TextMeshProUGUI>(root.transform, "InfoCol/MapText"),
            progressText = FindComponent<TextMeshProUGUI>(root.transform, "InfoCol/ProgressText"),
            infoText = FindComponent<TextMeshProUGUI>(root.transform, "InfoText")
        };
    }

    private static T FindComponent<T>(Transform root, string path) where T : Component
    {
        var child = root.Find(path);
        return child != null ? child.GetComponent<T>() : null;
    }

    private static void UpdateStaticSlotLabels(SlotRow row, int slot)
    {
        if (row?.root == null)
            return;

        var number = FindComponent<TextMeshProUGUI>(row.root.transform, "NumberCol/Number");
        if (number != null)
            number.text = (slot + 1).ToString();

        var label = FindComponent<TextMeshProUGUI>(row.root.transform, "NumberCol/SlotLabel");
        if (label != null)
            label.text = $"슬롯 {slot + 1}";
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
