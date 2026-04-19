using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround;
using UPlayGround.Component;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Quest;
using UPlayGround.Data.UI;
using UPlayGround.Manager;
using UPlayGround.UI;

/// <summary>
/// 미니맵 HUD (MapImage 전용).
///
/// ■ 표시 항목
///   · 플레이어 — 항상 중심, 방향 화살표 회전
///   · 적       — 비전투(dimmed) / 전투(강조색) 구분 표시, 설정에 따라 감지된 적만 표시
///   · NPC·채집 — 씬에 배치된 액터 아이콘
///   · 퀘스트   — 활성 퀘스트의 ReachLocation / ItemDeliver 목표 마커
///   · 마을     — MinimapMarkerType.Town 정적 마커
///   · 포탈     — MinimapMarkerType.Portal 정적 마커
///   · 고정 NPC — MinimapMarkerType.Npc 정적 마커 (액터 시스템과 별개)
///   · 사용자 마커 — MinimapUserMarkerSystem 을 통해 런타임에 추가된 핀
///
/// ■ Config 로드
///   SceneContext.MapID → MapConfigDatabaseSO → MinimapIconConfigSO
///   씬 전환마다 OnShow() 시점에 자동으로 해당 맵 Config를 조회한다.
///
/// ■ 프리팹 구조 (필수)
/// <code>
/// UI_Minimap (Canvas, CanvasLayer = HUD)
///   └─ MinimapMask (Image — 원형 스프라이트, Mask 컴포넌트)
///        ├─ MapBackground (Image) ← _mapBackground
///        ├─ QuestContainer  (RectTransform) ← _questContainer
///        ├─ IconContainer   (RectTransform) ← _iconContainer
///        └─ PlayerIcon      (Image) ← _playerIcon
/// </code>
/// </summary>
[RequireComponent(typeof(Canvas))]
public class UI_Minimap : UI_Base
{
    [Header("컴포넌트")]
    [SerializeField] private RectTransform _iconContainer;
    [SerializeField] private RectTransform _questContainer;
    [SerializeField] private RectTransform _playerIcon;
    [SerializeField] private Image         _mapBackground;
    [SerializeField] private RectTransform _minimapMask;

    [Header("설정")]
    [SerializeField] private MapConfigDatabaseSO _mapConfigDB;

    // MapImage 모드 마스크 영역 픽셀 크기 (MinimapMask RectTransform.sizeDelta.x 와 일치)
    [SerializeField] private float _maskDisplaySize = 200f;

    // ── 런타임 ───────────────────────────────────────────────
    private PlayerActor         _player;
    private MinimapIconConfigSO _config;

    // ── 확대 맵 상태 ─────────────────────────────────────────
    private bool      _isExpanded;
    private float     _currentMaskSize;
    private float     _currentMapZoom;
    private Coroutine _expandCoroutine;

    // 섹션 1: 적 아이콘
    private readonly Dictionary<MonsterActor, MinimapEntityIcon> _enemyIconMap        = new();
    // 섹션 2: 일반 액터 아이콘 (NPC·채집 등)
    private readonly Dictionary<GameActor, MinimapEntityIcon>    _actorIconMap        = new();
    private readonly List<string> _tempRemoveIds = new();
    // 섹션 3: 퀘스트 마커 (활성 퀘스트 연동)
    private readonly Dictionary<string, MinimapEntityIcon>       _questIconMap        = new();
    // 섹션 4: 정적 마커 (마을·포탈·고정 NPC·Custom)
    private readonly Dictionary<string, MinimapEntityIcon>       _staticMarkerIconMap = new();
    // 섹션 5: 사용자 마커
    private readonly Dictionary<int, MinimapEntityIcon>          _userMarkerIconMap   = new();

    // ── UI_Base ──────────────────────────────────────────────

    protected override void OnShow()
    {
        // SceneContext.MapID 기반으로 Config 동적 로드
        if (_mapConfigDB == null)
        {
            Debug.LogError("[UI_Minimap] MapConfigDatabaseSO가 할당되지 않았습니다.");
            return;
        }

        string mapId = SceneManager.Instance?.CurrentMapID;
        _config = _mapConfigDB.GetConfig(mapId);

        if (_config == null)
        {
            Debug.LogError($"[UI_Minimap] MapID '{mapId}'에 대한 MinimapIconConfigSO를 찾을 수 없습니다.");
            return;
        }

        _player = GameObjectManager.Instance?.Player;

        // 상태 초기화
        _isExpanded      = false;
        _currentMaskSize = _maskDisplaySize;
        _currentMapZoom  = _config.mapZoom;
        ApplyMaskSize(_currentMaskSize);

        SetupMapBackground();

        // 씬에 이미 존재하는 액터 등록
        if (GameObjectManager.Instance?.AllActors != null)
        {
            foreach (var actor in GameObjectManager.Instance.AllActors)
                RegisterActor(actor);
        }

        RefreshAllQuestMarkers();
        RefreshAllStaticMarkers();

        foreach (var marker in MinimapUserMarkerSystem.GetAll())
            AddUserMarker(marker);

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

        if (_expandCoroutine != null)
        {
            StopCoroutine(_expandCoroutine);
            _expandCoroutine = null;
        }
        _isExpanded = false;
        ApplyMaskSize(_maskDisplaySize);

        ClearAllIcons();
    }

    // ── 업데이트 ─────────────────────────────────────────────

    private void LateUpdate()
    {
        if (!IsVisible || _player == null || _config == null) return;

        UpdateContainerLayout();
        UpdatePlayerIcon();
        UpdateEnemyIcons();
        UpdateActorIcons();
        UpdateQuestMarkers();
        UpdateStaticMarkers();
        UpdateUserMarkers();
    }

    // ── 확대 맵 토글 ─────────────────────────────────────────

    public void ToggleExpandedMap()
    {
        if (_config == null) return;

        _isExpanded = !_isExpanded;

        if (_expandCoroutine != null)
            StopCoroutine(_expandCoroutine);
        _expandCoroutine = StartCoroutine(TransitionZoom(_isExpanded));
    }

    private IEnumerator TransitionZoom(bool toExpanded)
    {
        float targetMaskSize = toExpanded ? _config.expandedMapSize  : _maskDisplaySize;
        float targetMapZoom  = toExpanded ? _config.expandedMapZoom  : _config.mapZoom;

        float startMaskSize = _currentMaskSize;
        float startMapZoom  = _currentMapZoom;

        float duration = _config.expandTransitionDuration;
        float elapsed  = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            t = Mathf.SmoothStep(0f, 1f, t);

            _currentMaskSize = Mathf.Lerp(startMaskSize, targetMaskSize, t);
            _currentMapZoom  = Mathf.Lerp(startMapZoom,  targetMapZoom,  t);

            ApplyMaskSize(_currentMaskSize);
            yield return null;
        }

        _currentMaskSize = targetMaskSize;
        _currentMapZoom  = targetMapZoom;
        ApplyMaskSize(_currentMaskSize);
        _expandCoroutine = null;
    }

    private void ApplyMaskSize(float size)
    {
        if (_minimapMask != null)
            _minimapMask.sizeDelta = new Vector2(size, size);
    }

    // ── 초기 설정 ────────────────────────────────────────────

    private void SetupMapBackground()
    {
        if (_mapBackground == null) return;

        if (_config.backgroundSprite != null)
        {
            _mapBackground.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _mapBackground.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _mapBackground.rectTransform.pivot     = new Vector2(0.5f, 0.5f);
            _mapBackground.rectTransform.sizeDelta = new Vector2(_maskDisplaySize, _maskDisplaySize);

            _mapBackground.sprite  = _config.backgroundSprite;
            _mapBackground.enabled = true;
        }
        else
        {
            _mapBackground.enabled = false;
        }
    }

    // ── 컨테이너 레이아웃 ────────────────────────────────────

    private void UpdateContainerLayout()
    {
        // 배경 이미지: captureCenter 기준 오프셋 (맵 이미지가 화면을 벗어나지 않도록 클램핑)
        Vector2 playerMapPos = _config.WorldToMapImagePos(_player.transform.position, _currentMaskSize);
        Vector2 bgOffset     = -playerMapPos * _currentMapZoom;

        float maxOffset = Mathf.Max(0f, _currentMaskSize * (_currentMapZoom - 1f) / 2f);
        bgOffset.x = Mathf.Clamp(bgOffset.x, -maxOffset, maxOffset);
        bgOffset.y = Mathf.Clamp(bgOffset.y, -maxOffset, maxOffset);

        if (_mapBackground != null)
        {
            _mapBackground.rectTransform.sizeDelta        = new Vector2(_currentMaskSize, _currentMaskSize);
            _mapBackground.rectTransform.localScale       = Vector3.one * _currentMapZoom;
            _mapBackground.rectTransform.anchoredPosition = bgOffset;
        }

        // 아이콘·퀘스트 컨테이너: 플레이어가 항상 원점이므로 오프셋 불필요
        if (_iconContainer  != null) _iconContainer.anchoredPosition  = Vector2.zero;
        if (_questContainer != null) _questContainer.anchoredPosition = Vector2.zero;
    }

    // ── 플레이어 아이콘 ──────────────────────────────────────

    private void UpdatePlayerIcon()
    {
        if (_playerIcon == null) return;
        _playerIcon.anchoredPosition = Vector2.zero;
        _playerIcon.localRotation    = Quaternion.Euler(0f, 0f, -_player.transform.eulerAngles.y);
    }

    // ── 적 아이콘 ────────────────────────────────────────────

    private void UpdateEnemyIcons()
    {
        var toRemove = new List<MonsterActor>();

        foreach (var (monster, icon) in _enemyIconMap)
        {
            if (monster == null || icon == null) { toRemove.Add(monster); continue; }

            bool isDetected = monster.Detection != null && monster.Detection.HasTarget;

            if (_config.showOnlyDetectedEnemies && !isDetected)
            {
                icon.UpdateIcon(Vector2.zero, false);
                continue;
            }

            icon.SetColor(isDetected ? _config.enemyDetected.color : _config.enemy.color);
            icon.UpdateIcon(CalcMinimapPos(monster.transform.position), true);
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
            icon.UpdateIcon(CalcMinimapPos(actor.transform.position), true);
        }

        CleanupDeadEntries(toRemove, _actorIconMap);
    }

    // ── 퀘스트 마커 ──────────────────────────────────────────

    private void UpdateQuestMarkers()
    {
        if (!_config.showQuestMarkers) return;

        foreach (var (locationId, icon) in _questIconMap)
        {
            if (!MinimapMarkerRegistry.TryGet(locationId, out var registrar) || icon == null)
                continue;
            icon.UpdateIcon(CalcMinimapPos(registrar.WorldPosition), true);
        }
    }

    private void RefreshAllQuestMarkers()
    {
        foreach (var icon in _questIconMap.Values)
            if (icon != null) Destroy(icon.gameObject);
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
        if (string.IsNullOrEmpty(locationId)) return;
        if (_questIconMap.ContainsKey(locationId)) return;
        if (!MinimapMarkerRegistry.TryGet(locationId, out _)) return;

        var entry = GetQuestMarkerEntry(objective);
        if (entry.sprite == null) return;

        var container = _questContainer != null ? _questContainer : _iconContainer;
        if (container == null) return;

        _questIconMap[locationId] = MinimapEntityIcon.CreateStatic(container, locationId, entry);
    }

    private void TryRemoveQuestMarker(string locationId)
    {
        if (!_questIconMap.TryGetValue(locationId, out var icon)) return;
        _questIconMap.Remove(locationId);
        if (icon != null) Destroy(icon.gameObject);
    }

    private static string ResolveQuestLocationId(QuestObjectiveData obj) => obj.type switch
    {
        QuestObjectiveType.ReachLocation => obj.targetStringId,
        QuestObjectiveType.ItemDeliver   => $"npc_{obj.npcId}",
        _                               => null,
    };

    private MinimapIconConfigSO.IconEntry GetQuestMarkerEntry(QuestObjectiveData obj)
    {
        return obj.type == QuestObjectiveType.ItemDeliver ? _config.questNpc : _config.questTarget;
    }

    // ── 정적 마커 (마을·포탈·고정 NPC·Custom) ────────────────

    private void RefreshAllStaticMarkers()
    {
        foreach (var icon in _staticMarkerIconMap.Values)
            if (icon != null) Destroy(icon.gameObject);
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
        _staticMarkerIconMap[registrar.LocationId] =
            MinimapEntityIcon.CreateStatic(_iconContainer, registrar.LocationId, entry);
    }

    private void UpdateStaticMarkers()
    {
        _tempRemoveIds.Clear();

        foreach (var (locationId, icon) in _staticMarkerIconMap)
        {
            if (icon == null) { _tempRemoveIds.Add(locationId); continue; }
            if (!MinimapMarkerRegistry.TryGet(locationId, out var registrar))
            { _tempRemoveIds.Add(locationId); continue; }
            icon.UpdateIcon(CalcMinimapPos(registrar.WorldPosition), true);
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
            icon.UpdateIcon(CalcMinimapPos(marker.WorldPosition), true);
        }
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
            TryRemoveQuestMarker(registrar.LocationId);
        else
            RemoveStaticMarkerById(registrar.LocationId);
    }

    private void RemoveStaticMarkerById(string locationId)
    {
        if (!_staticMarkerIconMap.TryGetValue(locationId, out var icon)) return;
        _staticMarkerIconMap.Remove(locationId);
        if (icon != null) Destroy(icon.gameObject);
    }

    // ── 액터 등록 / 해제 ─────────────────────────────────────

    private void RegisterActor(GameActor actor)
    {
        if (actor is PlayerActor)                   return;
        if (actor.HasActorType(ActorType.Obstacle)) return;

        if (actor is MonsterActor monster)
        {
            if (!_config.showEnemies) return;
            if (_enemyIconMap.ContainsKey(monster)) return;
            if (_iconContainer == null) return;
            var entry = _config.enemy;
            // sprite 미설정 시 MinimapEntityIcon이 자동으로 원형 점 스프라이트를 생성
            _enemyIconMap[monster] = MinimapEntityIcon.Create(_iconContainer, monster, entry);
            return;
        }

        if (actor.HasActorType(ActorType.NPC)  && !_config.showNpcs)      return;
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

    // ── 좌표 변환 ────────────────────────────────────────────

    /// <summary>월드 좌표 → 미니맵 컨테이너 내 픽셀 좌표 (플레이어 기준 상대 좌표, captureCenter 무관)</summary>
    private Vector2 CalcMinimapPos(Vector3 worldPos)
    {
        float scale = _currentMaskSize * _currentMapZoom / _config.captureWorldSize;
        Vector3 delta = worldPos - _player.transform.position;
        return new Vector2(delta.x * scale, delta.z * scale);
    }

    // ── 유틸 ─────────────────────────────────────────────────

    private static void CleanupDeadEntries<T>(List<T> toRemove, Dictionary<T, MinimapEntityIcon> map)
        where T : class
    {
        foreach (var dead in toRemove)
        {
            if (map.TryGetValue(dead, out var icon) && icon != null)
                Object.Destroy(icon.gameObject);
            map.Remove(dead);
        }
    }
}
