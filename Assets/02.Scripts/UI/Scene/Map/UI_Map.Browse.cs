using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.Path;
using UPlayGround.Data.UI;
using UPlayGround.Manager;
using UPlayGround.UI;

/// <summary>
/// UI_Map 브라우즈 모드 확장.
///
/// ■ 개념
///   · 라이브 모드  : 현재 씬(_currentSceneMapId)의 실제 액터/적/퀘스트/정적 마커를 표시.
///   · 브라우즈 모드: 지역 목록에서 다른 지역을 선택하면 그 지역의 배경/지역정보/데이터 포탈만 미리보기.
///     로드돼 있지 않은 지역이므로 런타임 마커 대신 MapRegionInfoSO.portals(저작 데이터)를 렌더한다.
///
/// ■ 파스트트래블
///   브라우즈 모드에서 포탈 아이콘 클릭 → 확인 팝업 → SceneManager.LoadScene(scene, arrivalId).
///   도착 지점은 대상 씬의 SceneArrivalPoint(Id == arrivalId)에서 SceneManager가 처리한다.
/// </summary>
public partial class UI_Map
{
    // ── 지역 선택 / 브라우즈 UI (빌더가 생성) ───────────────────
    [Header("지역 선택 / 브라우즈")]
    [SerializeField] private RectTransform _regionListContent;    // 지역 버튼 부모(VerticalLayoutGroup)
    [SerializeField] private Button        _regionButtonTemplate; // 복제용 템플릿 버튼(비활성)

    [Header("이동 확인 팝업")]
    [SerializeField] private GameObject       _confirmPanel;       // 확인 팝업 루트(토글)
    [SerializeField] private TextMeshProUGUI  _confirmMessageText;
    [SerializeField] private Button           _confirmYesButton;
    [SerializeField] private Button           _confirmNoButton;

    [Header("지역 상세 정보 팝업")]
    [SerializeField] private Button           _regionInfoButton;      // 좌하단 "지역 정보" 버튼
    [SerializeField] private GameObject       _regionDetailPanel;     // 상세 팝업 루트(토글)
    [SerializeField] private TextMeshProUGUI  _regionDetailTitle;
    [SerializeField] private TextMeshProUGUI  _regionDetailBody;
    [SerializeField] private Button           _regionDetailCloseButton;

    // ── 런타임 상태 ─────────────────────────────────────────────
    private string _currentSceneMapId;   // 실제 진입해 있는 씬의 MapID (라이브 기준)
    private string _viewRegionMapId;      // 현재 지도에 표시 중인 지역의 MapID

    /// <summary>표시 중 지역이 실제 씬과 다르면 브라우즈(미리보기) 모드.</summary>
    private bool IsBrowsing =>
        !string.IsNullOrEmpty(_viewRegionMapId) &&
        !string.IsNullOrEmpty(_currentSceneMapId) &&
        _viewRegionMapId != _currentSceneMapId;

    private struct BrowsePortalIcon { public MinimapEntityIcon icon; public Vector3 worldPos; }
    private struct RegionButton     { public Button btn;            public string  mapId;    }

    private readonly List<BrowsePortalIcon> _browsePortalIcons = new();
    private readonly List<RegionButton>     _regionButtons     = new();
    private Action _pendingConfirm;

    // ── 초기화 ──────────────────────────────────────────────────

    /// <summary>Awake에서 1회 호출. 확인 팝업 버튼 바인딩 + 초기 숨김.</summary>
    private void HookBrowseUI()
    {
        if (_confirmYesButton != null) _confirmYesButton.onClick.AddListener(OnConfirmYes);
        if (_confirmNoButton  != null) _confirmNoButton.onClick.AddListener(HideConfirm);
        if (_regionButtonTemplate != null) _regionButtonTemplate.gameObject.SetActive(false);

        if (_regionInfoButton != null)        _regionInfoButton.onClick.AddListener(ShowRegionDetail);
        if (_regionDetailCloseButton != null) _regionDetailCloseButton.onClick.AddListener(HideRegionDetail);

        HideConfirm();
        HideRegionDetail();
    }

    // ── 지역 목록 ───────────────────────────────────────────────

    /// <summary>MapConfigDatabase의 모든 지역으로 선택 버튼을 재구성한다. OnShow에서 호출.</summary>
    private void PopulateRegionList()
    {
        if (_regionListContent == null || _regionButtonTemplate == null || _mapConfigDB == null) return;

        foreach (var rb in _regionButtons) if (rb.btn != null) Destroy(rb.btn.gameObject);
        _regionButtons.Clear();

        _regionButtonTemplate.gameObject.SetActive(false);

        foreach (var entry in _mapConfigDB.Entries)
        {
            if (string.IsNullOrEmpty(entry.mapId)) continue;

            // showContinentName이 켜지고 continentName이 있는 지역만 목록에 노출한다.
            // (플래그가 꺼진 지역은 mapId로도 노출하지 않고 아예 제외)
            if (entry.regionInfo == null
                || !entry.regionInfo.showContinentName
                || string.IsNullOrEmpty(entry.regionInfo.continentName))
                continue;

            var btn = Instantiate(_regionButtonTemplate, _regionListContent);
            btn.gameObject.SetActive(true);

            var txt = btn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (txt != null) txt.text = entry.regionInfo.continentName;

            string targetMapId = entry.mapId;   // 캡처
            btn.onClick.AddListener(() => ShowRegion(targetMapId));

            _regionButtons.Add(new RegionButton { btn = btn, mapId = targetMapId });
        }

        RefreshRegionListSelection();
    }

    /// <summary>현재 표시 지역 버튼은 비활성(interactable=false)으로 "보는 중" 표시.</summary>
    private void RefreshRegionListSelection()
    {
        foreach (var rb in _regionButtons)
            if (rb.btn != null)
                rb.btn.interactable = rb.mapId != _viewRegionMapId;
    }

    // ── 지역 전환 ───────────────────────────────────────────────

    /// <summary>
    /// 지정 지역을 지도에 표시한다. 실제 씬 지역이면 라이브 마커, 다른 지역이면 데이터 포탈 미리보기.
    /// </summary>
    public void ShowRegion(string mapId)
    {
        if (_mapConfigDB == null || string.IsNullOrEmpty(mapId)) return;

        var cfg = _mapConfigDB.GetConfig(mapId);
        if (cfg == null) return;

        HideConfirm();
        HideRegionDetail();

        _viewRegionMapId = mapId;
        _config = cfg;

        var ri = _mapConfigDB.GetRegionInfo(mapId);
        if (ri != null) _regionInfo = ri;
        else if (IsBrowsing) _regionInfo = null;   // 알 수 없는 지역: 지역정보 비움

        ClearAllIcons();
        ClearBrowsePortals();
        SetupMapBackground();

        if (IsBrowsing)
        {
            if (_playerIcon != null) _playerIcon.gameObject.SetActive(false);
            BuildBrowsePortals();
            _currentZoom = _initialZoom;
            CenterOnRegion();
        }
        else
        {
            SetupMarkers();
            _currentZoom = _initialZoom;
            CenterOnPlayer();
        }

        RefreshRegionInfo();
        RefreshRegionListSelection();
    }

    // ── 브라우즈 포탈 (데이터 기반) ──────────────────────────────

    private void BuildBrowsePortals()
    {
        ClearBrowsePortals();
        if (_regionInfo == null || _config == null || _iconContainer == null) return;

        var entry = _config.GetStaticMarkerEntry(MinimapMarkerType.Portal);
        foreach (var portal in _regionInfo.portals)
        {
            string name = string.IsNullOrEmpty(portal.label) ? "Portal" : portal.label;
            var icon = MinimapEntityIcon.CreateStatic(_iconContainer, name, entry);

            var captured = portal;   // 클로저 캡처
            icon.OnClickEvent += _ => OnBrowsePortalClicked(captured);

            _browsePortalIcons.Add(new BrowsePortalIcon { icon = icon, worldPos = portal.worldPosition });
        }
    }

    private void UpdateBrowsePortals()
    {
        if (_config == null) return;
        foreach (var bp in _browsePortalIcons)
            if (bp.icon != null)
                bp.icon.UpdateIcon(_config.WorldToMapImagePos(bp.worldPos, _mapDisplaySize) * _currentZoom, true);
    }

    private void ClearBrowsePortals()
    {
        foreach (var bp in _browsePortalIcons) if (bp.icon != null) Destroy(bp.icon.gameObject);
        _browsePortalIcons.Clear();
    }

    /// <summary>플레이어가 없는 브라우즈 모드에서 지도를 중앙 정렬한다.</summary>
    private void CenterOnRegion()
    {
        _panOffset = ClampPan(Vector2.zero);
        ApplyLayout();
    }

    // ── 파스트트래블 + 확인 팝업 ────────────────────────────────

    private void OnBrowsePortalClicked(MapRegionInfoSO.PortalEntry portal)
    {
        // 대상 씬: 포탈에 targetSceneName이 명시돼 있으면 그 씬(인터맵 연결),
        // 없으면 현재 보고 있는 지역 자신(웨이포인트 이동).
        string destScene = string.IsNullOrEmpty(portal.targetSceneName) ? _viewRegionMapId : portal.targetSceneName;
        if (string.IsNullOrEmpty(destScene))
        {
            Debug.LogWarning("[UI_Map] 포탈 이동 대상 씬을 확인할 수 없습니다.");
            return;
        }

        ShowConfirm($"'{RegionDisplayName(destScene)}'(으)로 이동하시겠습니까?", () => DoFastTravel(portal, destScene));
    }

    private void DoFastTravel(MapRegionInfoSO.PortalEntry portal, string destScene)
    {
        HideConfirm();
        UIManager.Instance?.HideUI(UIKeyType.Map);

        if (!string.IsNullOrEmpty(portal.arrivalId))
            SceneManager.Instance?.LoadScene(destScene, portal.arrivalId);                 // 지정 도착 지점
        else if (string.IsNullOrEmpty(portal.targetSceneName))
            SceneManager.Instance?.LoadScene(destScene, portal.worldPosition);             // 포탈 위치에 스폰(웨이포인트)
        else
            SceneManager.Instance?.LoadScene(destScene);                                   // 대상 씬 기본 스폰
    }

    /// <summary>mapId에 대한 표시 이름(continentName 우선, 없으면 mapId).</summary>
    private string RegionDisplayName(string mapId)
    {
        var ri = _mapConfigDB != null ? _mapConfigDB.GetRegionInfo(mapId) : null;
        return ri != null && !string.IsNullOrEmpty(ri.continentName) ? ri.continentName : mapId;
    }

    private void ShowConfirm(string message, Action onYes)
    {
        _pendingConfirm = onYes;
        if (_confirmMessageText != null) _confirmMessageText.text = message;
        if (_confirmPanel != null) _confirmPanel.SetActive(true);
    }

    private void HideConfirm()
    {
        _pendingConfirm = null;
        if (_confirmPanel != null) _confirmPanel.SetActive(false);
    }

    private void OnConfirmYes()
    {
        var action = _pendingConfirm;
        HideConfirm();
        action?.Invoke();
    }

    // ── 지역 상세 정보 팝업 ──────────────────────────────────────

    /// <summary>현재 표시 중인 지역의 상세 정보(대륙·권장레벨·설명)를 팝업으로 보여준다.</summary>
    private void ShowRegionDetail()
    {
        if (_regionDetailTitle != null) _regionDetailTitle.text = _viewRegionMapId;

        if (_regionDetailBody != null)
        {
            var lines = new List<string>();
            if (_regionInfo != null)
            {
                if (!string.IsNullOrEmpty(_regionInfo.continentName))
                    lines.Add($"대륙: {_regionInfo.continentName}");
                lines.Add($"권장 레벨: {_regionInfo.GetRecommendedLevelText()}");
                if (!string.IsNullOrEmpty(_regionInfo.description))
                {
                    lines.Add(string.Empty);
                    lines.Add(_regionInfo.description);
                }
            }
            _regionDetailBody.text = lines.Count > 0
                ? string.Join("\n", lines)
                : "등록된 지역 정보가 없습니다.";
        }

        if (_regionDetailPanel != null) _regionDetailPanel.SetActive(true);
    }

    private void HideRegionDetail()
    {
        if (_regionDetailPanel != null) _regionDetailPanel.SetActive(false);
    }
}
