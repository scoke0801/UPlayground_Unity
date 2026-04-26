using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UPlayGround;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Path;
using UPlayGround.Data.Quest;
using UPlayGround.Data.UI;
using UPlayGround.InputDefine;
using UPlayGround.Manager;
using UPlayGround.UI;

/// <summary>
/// 전체 맵 뷰 UI (M키 토글, ESC 닫기)
///
/// ■ 줌/패닝 방식 (UI_Minimap과 동일 구조)
///   · MapBackground.localScale → 줌  (이미지 크기는 프리팹 고정, 보이는 범위만 변경)
///   · MapBackground·IconContainer·QuestContainer.anchoredPosition → 패닝 오프셋 공유
///   · 아이콘 위치 = WorldToMapImagePos * currentZoom  (컨테이너 로컬 좌표)
///   · PlayerIcon  = panOffset + mapPos * currentZoom  (컨테이너 형제이므로 offset 포함)
///
/// ■ Config 로드
///   SceneContext.MapID → MapConfigDatabaseSO → MinimapIconConfigSO
///   씬 전환마다 OnShow() 시점에 자동으로 해당 맵 Config를 조회한다.
///
/// ■ 사용자 마커
///   MapViewport 위에서 우클릭하면 해당 월드 좌표에 사용자 마커를 추가합니다.
///   이미 마커가 있는 위치를 우클릭하면 가장 가까운 마커를 제거합니다.
///
/// ■ 프리팹 구조 (필수)
/// <code>
/// UI_Map (Canvas, CanvasLayer = Popup)
///   ├─ MapViewport  (RectTransform + Image(alpha=0) + MapInputReceiver + RectMask2D) ← _mapViewport
///   │    └─ MapContainer (RectTransform — 변환 없음, 구조용)
///   │         ├─ MapBackground (Image — center anchor, 고정 크기)   ← _mapBackground
///   │         ├─ QuestContainer (RectTransform — center anchor)     ← _questContainer
///   │         ├─ IconContainer  (RectTransform — center anchor)     ← _iconContainer
///   │         └─ PlayerIcon     (Image — center anchor)             ← _playerIcon
///   ├─ CloseButton   (Button)  ← _closeButton
///   ├─ ZoomInButton  (Button)  ← _zoomInButton
///   ├─ ZoomOutButton (Button)  ← _zoomOutButton
///   └─ FindMeButton  (Button)  ← _findMeButton
/// </code>
/// </summary>
[RequireComponent(typeof(Canvas))]
public class UI_Map : UI_Base
{
    [Header("컴포넌트")]
    [SerializeField] private RectTransform _mapViewport;    // 클리핑 영역 (RectMask2D + MapInputReceiver 부착)
    [SerializeField] private Image         _mapBackground;  // 전체 맵 이미지 (크기 고정)
    [SerializeField] private RectTransform _iconContainer;  // 적·NPC 마커 부모
    [SerializeField] private RectTransform _questContainer; // 퀘스트 마커 부모
    [SerializeField] private RectTransform _playerIcon;     // 플레이어 위치 마커

    [Header("버튼")]
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _zoomInButton;
    [SerializeField] private Button _zoomOutButton;
    [SerializeField] private Button _findMeButton;

    [Header("설정")]
    [SerializeField] private MapConfigDatabaseSO _mapConfigDB;

    [Tooltip("MapBackground 프리팹 sizeDelta와 일치시킬 것 (픽셀). 좌표 변환 기준값.")]
    [SerializeField] private Vector2 _mapDisplaySize = new(1000f, 1000f);

    [Header("줌")]
    [Tooltip("맵이 열릴 때 초기 줌 배율")]
    [SerializeField] private float _initialZoom      = 2f;
    [SerializeField] private float _minZoom          = 1f;
    [SerializeField] private float _maxZoom          = 4f;
    [SerializeField] private float _zoomStep         = 0.5f;
    [SerializeField] private float _scrollZoomSpeed  = 0.1f;

    [Header("표시 옵션")]
    [Tooltip("미니맵 설정과 무관하게 맵에서 모든 적 표시")]
    [SerializeField] private bool _showAllEnemiesOnMap = false;

    [Tooltip("몬스터·NPC 아이콘을 표시할 플레이어 기준 반경 (월드 단위). 0 이하면 전체 표시.")]
    [SerializeField] private float _entityProximityRadius = 50f;

    // ── 런타임 ───────────────────────────────────────────────
    private PlayerActor         _player;
    private MinimapIconConfigSO _config;
    private MapInputReceiver    _inputReceiver;
    private float               _currentZoom = 1f;
    private Vector2             _panOffset;

    private readonly Dictionary<MonsterActor, MinimapEntityIcon> _enemyIconMap        = new();
    private readonly Dictionary<GameActor,    MinimapEntityIcon> _actorIconMap        = new();
    private readonly List<string> _tempRemoveIds = new();
    private readonly Dictionary<string,       MinimapEntityIcon> _questIconMap        = new();
    private readonly Dictionary<string,       MinimapEntityIcon> _staticMarkerIconMap = new();
    private readonly Dictionary<int,          MinimapEntityIcon> _userMarkerIconMap   = new();

    // 우클릭으로 마커를 제거할 때 사용하는 픽셀 거리 임계값
    private const float UserMarkerRemoveThresholdPx = 20f;

    // ── UI_Base ──────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();

        _canvas = GetComponent<Canvas>();

        if (_closeButton   != null) _closeButton.onClick.AddListener(OnCloseClicked);
        if (_zoomInButton  != null) _zoomInButton.onClick.AddListener(() => ZoomAtCenter(_currentZoom + _zoomStep));
        if (_zoomOutButton != null) _zoomOutButton.onClick.AddListener(() => ZoomAtCenter(_currentZoom - _zoomStep));
        if (_findMeButton  != null) _findMeButton.onClick.AddListener(CenterOnPlayer);

        if (_mapViewport != null)
        {
            _inputReceiver = _mapViewport.GetComponent<MapInputReceiver>();
            if (_inputReceiver != null)
            {
                _inputReceiver.OnBeginDragEvent  += OnBeginDrag;
                _inputReceiver.OnDragEvent       += OnDrag;
                _inputReceiver.OnScrollEvent     += OnScroll;
                _inputReceiver.OnRightClickEvent += OnMapRightClick;
            }
        }
    }

    protected override void OnShow()
    {
        if (_mapConfigDB == null)
        {
            Debug.LogError("[UI_Map] MapConfigDatabaseSO가 할당되지 않았습니다.");
            return;
        }

        string mapId = SceneManager.Instance?.CurrentMapID;
        _config = _mapConfigDB.GetConfig(mapId);

        if (_config == null)
        {
            Debug.LogError($"[UI_Map] MapID '{mapId}'에 대한 MinimapIconConfigSO를 찾을 수 없습니다.");
            return;
        }

        InputManager.Instance.SetInputLayer(_layer.ToInputLayer());
        InputManager.Instance.ShowCursor(true, true);
        _player = GameObjectManager.Instance?.Player;

        SetupMapBackground();
        SetupMarkers();

        // 초기 줌으로 플레이어 위치를 중심으로 열기
        _currentZoom = _initialZoom;
        CenterOnPlayer();

        var gom = GameObjectManager.Instance;
        if (gom != null)
        {
            gom.OnActorRegistered   += RegisterActor;
            gom.OnActorUnregistered += UnregisterActor;
        }

        MinimapMarkerRegistry.OnMarkerAdded   += OnMarkerAdded;
        MinimapMarkerRegistry.OnMarkerRemoved += OnMarkerRemoved;

        MinimapUserMarkerSystem.OnMarkerAdded      += AddUserMarker;
        MinimapUserMarkerSystem.OnMarkerRemoved    += RemoveUserMarker;
        MinimapUserMarkerSystem.OnAllMarkersCleared += ClearUserMarkers;

        var ev = EventManager.Instance;
        if (ev != null)
        {
            ev.Subscribe<QuestEvent, QuestStateEventData>(QuestEvent.QuestAccepted,  OnQuestStateChanged);
            ev.Subscribe<QuestEvent, QuestStateEventData>(QuestEvent.QuestCompleted, OnQuestStateChanged);
            ev.Subscribe<QuestEvent, QuestStateEventData>(QuestEvent.QuestFailed,    OnQuestStateChanged);
        }
    }

    protected override void OnHide()
    {
        InputManager.Instance.SetInputLayer(InputLayer.None);

        InputManager.Instance.ShowCursor(false);
        if (GameObjectManager.Instance != null)
        {
            GameObjectManager.Instance.OnActorRegistered   -= RegisterActor;
            GameObjectManager.Instance.OnActorUnregistered -= UnregisterActor;
        }

        MinimapMarkerRegistry.OnMarkerAdded   -= OnMarkerAdded;
        MinimapMarkerRegistry.OnMarkerRemoved -= OnMarkerRemoved;

        MinimapUserMarkerSystem.OnMarkerAdded      -= AddUserMarker;
        MinimapUserMarkerSystem.OnMarkerRemoved    -= RemoveUserMarker;
        MinimapUserMarkerSystem.OnAllMarkersCleared -= ClearUserMarkers;

        if (EventManager.Instance != null)
        {
            var ev = EventManager.Instance;
            ev.Unsubscribe<QuestEvent, QuestStateEventData>(QuestEvent.QuestAccepted,  OnQuestStateChanged);
            ev.Unsubscribe<QuestEvent, QuestStateEventData>(QuestEvent.QuestCompleted, OnQuestStateChanged);
            ev.Unsubscribe<QuestEvent, QuestStateEventData>(QuestEvent.QuestFailed,    OnQuestStateChanged);
        }

        ClearAllIcons();
    }

    protected override void RegisterInputEvents()
    {
        InputManager.Instance.RegisterInputEvent(InputMapNames.UI, UIAction.Map,
            null, OnPerformedMap, null, null, null, InputLayer.Level_1);
    }

    protected override void UnRegisterInputEvents()
    {
        InputManager.Instance.UnRegisterInputEvent(InputMapNames.UI, UIAction.Map,
            null, OnPerformedMap, null);
    }

    private void LateUpdate()
    {
        if (!IsVisible || _player == null || _config == null) return;

        UpdatePlayerIcon();
        UpdateEnemyIcons();
        UpdateActorIcons();
        UpdateQuestMarkers();
        UpdateStaticMarkers();
        UpdateUserMarkers();
    }

    // ── 초기 설정 ────────────────────────────────────────────

    private void SetupMapBackground()
    {
        if (_mapBackground == null) return;

        if (_config.backgroundSprite != null)
        {
            _mapBackground.sprite  = _config.backgroundSprite;
            _mapBackground.enabled = true;

            RectTransform bgRect = _mapBackground.rectTransform;
            float width = bgRect.rect.width > 0f ? bgRect.rect.width : _mapDisplaySize.x;
            bgRect.sizeDelta = _config.GetMapDisplaySizeByHeight(width / _config.GetBackgroundAspect());
        }
        else
        {
            _mapBackground.enabled = false;
        }
        
        // 프리팹 sizeDelta 기준으로 좌표 변환 크기를 동기화
        _mapDisplaySize = _mapBackground.rectTransform.rect.size;
    }

    private void SetupMarkers()
    {
        ClearAllIcons();

        if (GameObjectManager.Instance?.AllActors != null)
            foreach (var actor in GameObjectManager.Instance.AllActors)
                RegisterActor(actor);

        RefreshAllQuestMarkers();
        RefreshAllStaticMarkers();

        foreach (var marker in MinimapUserMarkerSystem.GetAll())
            AddUserMarker(marker);
    }

    // ── 줌 / 패닝 ────────────────────────────────────────────

    /// <summary>뷰 중심 기준 줌 변경 (±버튼용)</summary>
    private void ZoomAtCenter(float newZoom)
    {
        newZoom      = Mathf.Clamp(newZoom, _minZoom, _maxZoom);
        _panOffset   = _panOffset * (newZoom / _currentZoom);
        _currentZoom = newZoom;
        _panOffset   = ClampPan(_panOffset);
        ApplyLayout();
    }

    /// <summary>마우스 위치 기준 줌 변경 (스크롤용)</summary>
    private void ZoomAtMouse(float newZoom, Vector2 screenMousePos)
    {
        newZoom = Mathf.Clamp(newZoom, _minZoom, _maxZoom);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _mapViewport, screenMousePos, _canvas.worldCamera, out Vector2 mouseLocal);

        // 마우스 아래 맵 좌표를 고정한 채 줌
        _panOffset   = mouseLocal + (_panOffset - mouseLocal) * (newZoom / _currentZoom);
        _currentZoom = newZoom;
        _panOffset   = ClampPan(_panOffset);
        ApplyLayout();
    }

    /// <summary>
    /// Background.localScale = 줌 / Background·Container.anchoredPosition = 패닝 오프셋.
    /// MapContainer는 변환하지 않아 이미지 크기가 프리팹 값으로 유지된다.
    /// </summary>
    private void ApplyLayout()
    {
        if (_mapBackground != null)
        {
            _mapBackground.rectTransform.localScale       = Vector3.one * _currentZoom;
            _mapBackground.rectTransform.anchoredPosition = _panOffset;
        }
        if (_iconContainer  != null) _iconContainer.anchoredPosition  = _panOffset;
        if (_questContainer != null) _questContainer.anchoredPosition = _panOffset;
    }

    /// <summary>플레이어 위치가 뷰 중심에 오도록 패닝 오프셋 재설정</summary>
    public void CenterOnPlayer()
    {
        if (_player == null || _config == null) return;
        Vector2 playerMapPos = _config.WorldToMapImagePos(_player.transform.position, _mapDisplaySize);
        _panOffset = -playerMapPos * _currentZoom;
        _panOffset = ClampPan(_panOffset);
        ApplyLayout();
    }

    /// <summary>패닝 범위를 맵 경계 안으로 제한</summary>
    private Vector2 ClampPan(Vector2 pos)
    {
        if (_mapViewport == null) return pos;

        float mapHalfX  = _mapDisplaySize.x * _currentZoom * 0.5f;
        float mapHalfY  = _mapDisplaySize.y * _currentZoom * 0.5f;
        float viewHalfX = _mapViewport.rect.width  * 0.5f;
        float viewHalfY = _mapViewport.rect.height * 0.5f;
        float maxX = Mathf.Max(0f, mapHalfX - viewHalfX);
        float maxY = Mathf.Max(0f, mapHalfY - viewHalfY);

        return new Vector2(Mathf.Clamp(pos.x, -maxX, maxX), Mathf.Clamp(pos.y, -maxY, maxY));
    }

    // ── 입력 핸들러 ───────────────────────────────────────────

    private void OnBeginDrag(PointerEventData e) { }

    private void OnDrag(PointerEventData e)
    {
        // e.delta는 스크린 픽셀 단위 → Canvas scaleFactor로 나눠 UI 좌표계로 변환
        float scaleFactor = _canvas != null ? _canvas.scaleFactor : 1f;
        _panOffset += e.delta / scaleFactor;
        _panOffset  = ClampPan(_panOffset);
        ApplyLayout();
    }

    private void OnScroll(PointerEventData e)
    {
        ZoomAtMouse(_currentZoom + e.scrollDelta.y * _scrollZoomSpeed, e.position);
    }

    private void OnCloseClicked() => UIManager.Instance.HideUI(UIKeyType.Map);

    private void OnPerformedMap(InputAction.CallbackContext obj) => UIManager.Instance.HideUI(UIKeyType.Map);

    // ── 플레이어 아이콘 ──────────────────────────────────────

    private void UpdatePlayerIcon()
    {
        if (_playerIcon == null) return;
        // PlayerIcon은 IconContainer의 형제 → 동일한 공식: panOffset + mapPos * zoom
        Vector2 mapPos = _config.WorldToMapImagePos(_player.transform.position, _mapDisplaySize);
        _playerIcon.anchoredPosition = _panOffset + mapPos * _currentZoom;
        _playerIcon.localRotation    = Quaternion.Euler(0f, 0f, -_player.transform.eulerAngles.y);
    }

    // ── 적 아이콘 ────────────────────────────────────────────

    private void UpdateEnemyIcons()
    {
        var toRemove = new List<MonsterActor>();
        foreach (var (monster, icon) in _enemyIconMap)
        {
            if (monster == null || icon == null) { toRemove.Add(monster); continue; }
            bool isDetected = monster.Detection?.HasTarget == true;
            icon.SetColor(isDetected ? _config.enemyDetected.color : _config.enemy.color);
            bool inRange = IsInProximity(monster.transform.position);
            icon.UpdateIcon(_config.WorldToMapImagePos(monster.transform.position, _mapDisplaySize) * _currentZoom, inRange);
        }
        CleanupDeadEntries(toRemove, _enemyIconMap);
    }

    // ── 일반 액터 아이콘 ─────────────────────────────────────

    private void UpdateActorIcons()
    {
        var toRemove = new List<GameActor>();
        foreach (var (actor, icon) in _actorIconMap)
        {
            if (actor == null || icon == null) { toRemove.Add(actor); continue; }
            bool inRange = IsInProximity(actor.transform.position);
            icon.UpdateIcon(_config.WorldToMapImagePos(actor.transform.position, _mapDisplaySize) * _currentZoom, inRange);
        }
        CleanupDeadEntries(toRemove, _actorIconMap);
    }

    // ── 퀘스트 마커 ──────────────────────────────────────────

    private void UpdateQuestMarkers()
    {
        if (!_config.showQuestMarkers) return;
        foreach (var (locationId, icon) in _questIconMap)
        {
            if (!MinimapMarkerRegistry.TryGet(locationId, out var registrar) || icon == null) continue;
            icon.UpdateIcon(_config.WorldToMapImagePos(registrar.WorldPosition, _mapDisplaySize) * _currentZoom, true);
        }
    }

    private void RefreshAllQuestMarkers()
    {
        foreach (var icon in _questIconMap.Values) if (icon) Destroy(icon.gameObject);
        _questIconMap.Clear();

        if (!_config.showQuestMarkers) return;
        var questManager = QuestManager.Instance;
        if (questManager == null || !questManager.IsDBLoaded) return;

        foreach (var runtime in questManager.GetActiveQuests())
            foreach (var obj in runtime.QuestSO.objectives)
                if (!runtime.IsObjectiveComplete(obj)) TryAddQuestMarker(obj);
    }

    private void TryAddQuestMarker(QuestObjectiveData objective)
    {
        if (!_config.showQuestMarkers) return;
        string locationId = ResolveQuestLocationId(objective);
        if (string.IsNullOrEmpty(locationId) || _questIconMap.ContainsKey(locationId)) return;
        if (!MinimapMarkerRegistry.TryGet(locationId, out _)) return;

        var entry = objective.type == QuestObjectiveType.ItemDeliver ? _config.questNpc : _config.questTarget;
        if (entry.sprite == null) return;

        var container = _questContainer != null ? _questContainer : _iconContainer;
        if (container == null) return;

        _questIconMap[locationId] = MinimapEntityIcon.CreateStatic(container, locationId, entry);
    }

    private static string ResolveQuestLocationId(QuestObjectiveData obj) => obj.type switch
    {
        QuestObjectiveType.ReachLocation => obj.targetStringId,
        QuestObjectiveType.ItemDeliver   => $"npc_{obj.npcId}",
        _                               => null,
    };

    // ── 정적 마커 (마을·포탈·고정 NPC·Custom) ────────────────

    private void RefreshAllStaticMarkers()
    {
        foreach (var icon in _staticMarkerIconMap.Values) if (icon) Destroy(icon.gameObject);
        _staticMarkerIconMap.Clear();

        if (_config == null) return;

        foreach (var registrar in MinimapMarkerRegistry.GetAll())
        {
            if (registrar.MarkerType != MinimapMarkerType.QuestTarget)
                AddStaticMarker(registrar);
        }
    }

    private void AddStaticMarker(MinimapMarkerRegistrar registrar)
    {
        if (_config == null) return;
        if (!_config.IsStaticMarkerVisible(registrar.MarkerType)) return;
        if (_staticMarkerIconMap.ContainsKey(registrar.LocationId)) return;
        if (_iconContainer == null) return;

        var entry = _config.GetStaticMarkerEntry(registrar.MarkerType);
        var icon  = MinimapEntityIcon.CreateStatic(_iconContainer, registrar.LocationId, entry);
        _staticMarkerIconMap[registrar.LocationId] = icon;

        if (registrar.MarkerType == MinimapMarkerType.Portal)
        {
            string locationId = registrar.LocationId;
            icon.OnClickEvent += _ => TeleportToPortal(locationId);
        }
    }

    private void TeleportToPortal(string locationId)
    {
        if (!MinimapMarkerRegistry.TryGet(locationId, out var registrar)) return;
        var portal = registrar.GetComponent<PortalActor>();
        if (portal == null) return;
        var player = GameObjectManager.Instance?.Player;
        if (player == null) return;
        portal.TeleportPlayerHere(player);
    }

    private void UpdateStaticMarkers()
    {
        _tempRemoveIds.Clear();

        foreach (var (locationId, icon) in _staticMarkerIconMap)
        {
            if (icon == null) { _tempRemoveIds.Add(locationId); continue; }
            if (!MinimapMarkerRegistry.TryGet(locationId, out var registrar))
            { _tempRemoveIds.Add(locationId); continue; }
            icon.UpdateIcon(_config.WorldToMapImagePos(registrar.WorldPosition, _mapDisplaySize) * _currentZoom, true);
        }

        foreach (var id in _tempRemoveIds)
        {
            if (_staticMarkerIconMap.TryGetValue(id, out var icon) && icon != null) Destroy(icon.gameObject);
            _staticMarkerIconMap.Remove(id);
        }
    }

    // ── 사용자 마커 ──────────────────────────────────────────

    private void AddUserMarker(UserMapMarker marker)
    {
        if (_config == null || !_config.showUserMarkers) return;
        if (_userMarkerIconMap.ContainsKey(marker.Id)) return;
        if (_iconContainer == null) return;

        var entry = _config.userMarker;
        _userMarkerIconMap[marker.Id] =
            MinimapEntityIcon.CreateStatic(_iconContainer, $"user_{marker.Id}", entry);
    }

    private void RemoveUserMarker(UserMapMarker marker)
    {
        if (!_userMarkerIconMap.TryGetValue(marker.Id, out var icon)) return;
        _userMarkerIconMap.Remove(marker.Id);
        if (icon != null) Destroy(icon.gameObject);
    }

    private void ClearUserMarkers()
    {
        foreach (var icon in _userMarkerIconMap.Values) if (icon) Destroy(icon.gameObject);
        _userMarkerIconMap.Clear();
    }

    private void UpdateUserMarkers()
    {
        if (_config == null || !_config.showUserMarkers) return;

        foreach (var (id, icon) in _userMarkerIconMap)
        {
            if (icon == null) continue;
            if (!MinimapUserMarkerSystem.TryGet(id, out var marker)) continue;
            icon.UpdateIcon(_config.WorldToMapImagePos(marker.WorldPosition, _mapDisplaySize) * _currentZoom, true);
        }
    }

    /// <summary>
    /// 맵 위에서 우클릭하면 해당 위치에 사용자 마커를 추가하거나, 근처 마커를 제거합니다.
    /// </summary>
    private void OnMapRightClick(PointerEventData e)
    {
        if (_config == null || !_config.showUserMarkers) return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _iconContainer, e.position, _canvas.worldCamera, out Vector2 localPoint)) return;

        // 근처 사용자 마커가 있으면 제거
        float threshold = UserMarkerRemoveThresholdPx * _currentZoom;
        int   nearest   = FindNearestUserMarker(localPoint, threshold);
        if (nearest >= 0)
        {
            MinimapUserMarkerSystem.RemoveMarker(nearest);
            return;
        }

        // 없으면 해당 위치에 새 마커 추가
        Vector3 worldPos = MapLocalPosToWorld(localPoint);
        MinimapUserMarkerSystem.AddMarker(worldPos);
    }

    private int FindNearestUserMarker(Vector2 localPoint, float threshold)
    {
        int   bestId   = -1;
        float bestDist = threshold;

        foreach (var (id, icon) in _userMarkerIconMap)
        {
            if (icon == null) continue;
            float dist = Vector2.Distance(icon.GetComponent<RectTransform>().anchoredPosition, localPoint);
            if (dist < bestDist) { bestDist = dist; bestId = id; }
        }
        return bestId;
    }

    /// <summary>iconContainer 로컬 좌표 → 월드 XZ 좌표 (Y = 0)</summary>
    private Vector3 MapLocalPosToWorld(Vector2 localPoint)
    {
        Vector2 mapPos = localPoint / _currentZoom;
        return _config.MapImagePosToWorld(mapPos, _mapDisplaySize);
    }

    // ── 이벤트 핸들러 ────────────────────────────────────────

    private void OnQuestStateChanged(QuestStateEventData data) => RefreshAllQuestMarkers();

    private void OnMarkerAdded(MinimapMarkerRegistrar registrar)
    {
        if (registrar.MarkerType == MinimapMarkerType.QuestTarget)
        {
            if (!_config.showQuestMarkers) return;
            var questManager = QuestManager.Instance;
            if (questManager == null) return;
            foreach (var runtime in questManager.GetActiveQuests())
                foreach (var obj in runtime.QuestSO.objectives)
                    if (!runtime.IsObjectiveComplete(obj) && ResolveQuestLocationId(obj) == registrar.LocationId)
                        TryAddQuestMarker(obj);
        }
        else
        {
            AddStaticMarker(registrar);
        }
    }

    private void OnMarkerRemoved(MinimapMarkerRegistrar registrar)
    {
        if (registrar.MarkerType == MinimapMarkerType.QuestTarget)
        {
            if (!_questIconMap.TryGetValue(registrar.LocationId, out var icon)) return;
            _questIconMap.Remove(registrar.LocationId);
            if (icon != null) Destroy(icon.gameObject);
        }
        else
        {
            if (!_staticMarkerIconMap.TryGetValue(registrar.LocationId, out var icon)) return;
            _staticMarkerIconMap.Remove(registrar.LocationId);
            if (icon != null) Destroy(icon.gameObject);
        }
    }

    // ── 액터 등록/해제 ───────────────────────────────────────

    private void RegisterActor(GameActor actor)
    {
        if (actor is PlayerActor || actor.HasActorType(ActorType.Obstacle)) return;

        if (actor is MonsterActor monster)
        {
            if (!_config.showEnemies && !_showAllEnemiesOnMap) return;
            if (_enemyIconMap.ContainsKey(monster)) return;
            var entry = _config.enemy;
            if (entry.sprite == null || _iconContainer == null) return;
            _enemyIconMap[monster] = MinimapEntityIcon.Create(_iconContainer, monster, entry);
            return;
        }

        if (actor.HasActorType(ActorType.NPC) && !_config.showNpcs) return;
        if (!actor.HasActorType(ActorType.NPC) && !_config.showGathering) return;
        if (_actorIconMap.ContainsKey(actor)) return;

        var actorEntry = _config.GetActorIconEntry(actor.ActorType);
        if (actorEntry.sprite == null || _iconContainer == null) return;
        _actorIconMap[actor] = MinimapEntityIcon.Create(_iconContainer, actor, actorEntry);
    }

    private void UnregisterActor(GameActor actor)
    {
        if (actor is MonsterActor monster) { RemoveFromMap(monster, _enemyIconMap); return; }
        RemoveFromMap(actor, _actorIconMap);
    }

    private static void RemoveFromMap<T>(T key, Dictionary<T, MinimapEntityIcon> map)
    {
        if (!map.TryGetValue(key, out var icon)) return;
        map.Remove(key);
        if (icon != null) Object.Destroy(icon.gameObject);
    }

    private void ClearAllIcons()
    {
        foreach (var icon in _enemyIconMap.Values)        if (icon) Destroy(icon.gameObject);
        foreach (var icon in _actorIconMap.Values)         if (icon) Destroy(icon.gameObject);
        foreach (var icon in _questIconMap.Values)         if (icon) Destroy(icon.gameObject);
        foreach (var icon in _staticMarkerIconMap.Values)  if (icon) Destroy(icon.gameObject);
        foreach (var icon in _userMarkerIconMap.Values)    if (icon) Destroy(icon.gameObject);
        _enemyIconMap.Clear();
        _actorIconMap.Clear();
        _questIconMap.Clear();
        _staticMarkerIconMap.Clear();
        _userMarkerIconMap.Clear();
    }

    // ── 유틸 ─────────────────────────────────────────────────

    private bool IsInProximity(Vector3 worldPos)
    {
        if (_entityProximityRadius <= 0f || _player == null) return true;
        float sqDist = (worldPos - _player.transform.position).sqrMagnitude;
        return sqDist <= _entityProximityRadius * _entityProximityRadius;
    }

    private static void CleanupDeadEntries<T>(List<T> toRemove, Dictionary<T, MinimapEntityIcon> map)
        where T : class
    {
        foreach (var dead in toRemove)
        {
            if (map.TryGetValue(dead, out var icon) && icon != null) Object.Destroy(icon.gameObject);
            map.Remove(dead);
        }
    }
}
