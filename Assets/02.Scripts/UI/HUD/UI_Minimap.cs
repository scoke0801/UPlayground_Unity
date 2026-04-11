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
/// 아이콘 기반 미니맵 HUD.
///
/// ■ 표시 항목
///   · 플레이어 — 항상 중심, 방향 화살표 회전
///   · 적       — 비전투(dimmed) / 전투(강조색) 구분 표시, 설정에 따라 감지된 적만 표시
///   · 퀘스트   — 활성 퀘스트의 ReachLocation 목표 지점을 "!" 마커로 표시
///                (씬에 MinimapMarkerRegistrar 배치 + locationId = QuestObjectiveData.targetStringId)
///
/// ■ 프리팹 구조 (필수)
/// <code>
/// UI_Minimap (Canvas, CanvasLayer = HUD)
///   └─ MinimapMask (Image — 원형 스프라이트, Mask 컴포넌트)
///        ├─ MapBackground (Image) ← _mapBackground  [MapImage 모드]
///        ├─ QuestContainer  (RectTransform) ← _questContainer
///        ├─ IconContainer   (RectTransform) ← _iconContainer
///        └─ PlayerIcon      (Image) ← _playerIcon
/// </code>
/// </summary>
[RequireComponent(typeof(Canvas))]
public class UI_Minimap : UI_Base
{
    [Header("컴포넌트")]
    [SerializeField] private RectTransform _iconContainer;    // 액터 아이콘 부모
    [SerializeField] private RectTransform _questContainer;   // 퀘스트 마커 부모 (아이콘 컨테이너와 동일 부모도 가능)
    [SerializeField] private RectTransform _playerIcon;       // 플레이어 방향 화살표
    [SerializeField] private Image         _mapBackground;    // MapImage 모드 배경

    [Header("설정")]
    [SerializeField] private MinimapIconConfigSO _config;

    // MapImage 모드 마스크 영역 픽셀 크기 (MinimapMask RectTransform.sizeDelta.x 와 일치)
    [SerializeField] private float _maskDisplaySize = 200f;

    // ── 런타임 ───────────────────────────────────────────────
    private PlayerActor _player;

    // 섹션 1: 적 아이콘 (MonsterActor → icon)
    private readonly Dictionary<MonsterActor, MinimapEntityIcon> _enemyIconMap = new();

    // 섹션 2: 일반 액터 아이콘 (NPC·채집 등)
    private readonly Dictionary<GameActor, MinimapEntityIcon> _actorIconMap = new();

    // 섹션 3: 퀘스트 마커 (locationId → icon)
    private readonly Dictionary<string, MinimapEntityIcon> _questIconMap = new();

    // ── UI_Base ──────────────────────────────────────────────

    protected override void OnShow()
    {
        if (_config == null)
        {
            Debug.LogError("[UI_Minimap] MinimapIconConfigSO가 할당되지 않았습니다.");
            return;
        }

        _player = GameObjectManager.Instance?.Player;

        SetupDisplayMode();

        // 씬에 이미 존재하는 액터 등록
        if (GameObjectManager.Instance?.AllActors != null)
        {
            foreach (var actor in GameObjectManager.Instance.AllActors)
                RegisterActor(actor);
        }

        // 퀘스트 마커 초기 설정
        RefreshAllQuestMarkers();

        // 이벤트 구독
        var gom = GameObjectManager.Instance;
        if (gom != null)
        {
            gom.OnActorRegistered   += RegisterActor;
            gom.OnActorUnregistered += UnregisterActor;
        }

        MinimapMarkerRegistry.OnMarkerAdded   += OnMarkerAdded;
        MinimapMarkerRegistry.OnMarkerRemoved += OnMarkerRemoved;

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

        if (EventManager.Instance != null)
        {
            var ev = EventManager.Instance;
            ev.Unsubscribe<QuestEvent, QuestStateEventData>(QuestEvent.QuestAccepted,  OnQuestStateChanged);
            ev.Unsubscribe<QuestEvent, QuestStateEventData>(QuestEvent.QuestCompleted, OnQuestStateChanged);
            ev.Unsubscribe<QuestEvent, QuestStateEventData>(QuestEvent.QuestFailed,    OnQuestStateChanged);
        }

        ClearAllIcons();
    }

    // ── 업데이트 ─────────────────────────────────────────────

    private void LateUpdate()
    {
        if (!IsVisible || _player == null || _config == null) return;

        bool isMapImageMode = _config.displayMode == MinimapDisplayMode.MapImage;

        UpdateContainerLayout(isMapImageMode);
        UpdatePlayerIcon(isMapImageMode);
        UpdateEnemyIcons(isMapImageMode);
        UpdateActorIcons(isMapImageMode);
        UpdateQuestMarkers(isMapImageMode);
    }

    // ── 모드 초기화 ──────────────────────────────────────────

    private void SetupDisplayMode()
    {
        if (_config.displayMode == MinimapDisplayMode.MapImage
            && _mapBackground != null
            && _config.backgroundSprite != null)
        {
            _mapBackground.sprite  = _config.backgroundSprite;
            _mapBackground.enabled = true;
            _mapBackground.rectTransform.sizeDelta = new Vector2(_maskDisplaySize, _maskDisplaySize);
        }
        else if (_mapBackground != null)
        {
            _mapBackground.enabled = false;
        }
    }

    // ── 컨테이너 레이아웃 ────────────────────────────────────

    private void UpdateContainerLayout(bool isMapImageMode)
    {
        if (isMapImageMode)
        {
            // 맵 이미지 모드: 배경·아이콘 컨테이너를 플레이어 위치로 오프셋
            Vector2 playerMapPos = _config.WorldToMapImagePos(_player.transform.position, _maskDisplaySize);
            Vector2 offset       = -playerMapPos;

            if (_mapBackground != null)
                _mapBackground.rectTransform.anchoredPosition = offset;
            if (_iconContainer != null)
                _iconContainer.anchoredPosition = offset;
            if (_questContainer != null)
                _questContainer.anchoredPosition = offset;
        }
        else
        {
            // IconOnly 모드: 플레이어 방향이 위가 되도록 컨테이너 회전
            if (_config.rotateWithPlayer)
            {
                float yaw = _player.transform.eulerAngles.y;
                Quaternion rot = Quaternion.Euler(0f, 0f, yaw);
                if (_iconContainer  != null) _iconContainer.localRotation  = rot;
                if (_questContainer != null) _questContainer.localRotation = rot;
            }
            else
            {
                if (_iconContainer  != null) _iconContainer.localRotation  = Quaternion.identity;
                if (_questContainer != null) _questContainer.localRotation = Quaternion.identity;
            }
        }
    }

    // ── 섹션 1: 플레이어 아이콘 ──────────────────────────────

    private void UpdatePlayerIcon(bool isMapImageMode)
    {
        if (_playerIcon == null) return;

        if (isMapImageMode)
        {
            // 맵 이미지 모드: 플레이어 아이콘은 뷰 중심에 고정
            _playerIcon.anchoredPosition = Vector2.zero;
        }

        // 방향 화살표 회전 — 컨테이너 회전과 역방향으로 보정
        float yaw = _player.transform.eulerAngles.y;
        _playerIcon.localRotation = Quaternion.Euler(0f, 0f, -yaw);
    }

    // ── 섹션 2: 적 아이콘 ────────────────────────────────────

    private void UpdateEnemyIcons(bool isMapImageMode)
    {
        var toRemove = new List<MonsterActor>();

        foreach (var (monster, icon) in _enemyIconMap)
        {
            if (monster == null || icon == null) { toRemove.Add(monster); continue; }

            bool isDetected = monster.Detection != null && monster.Detection.HasTarget;

            // 감지된 적만 표시 옵션
            if (_config.showOnlyDetectedEnemies && !isDetected)
            {
                icon.UpdateIcon(Vector2.zero, false);
                continue;
            }

            // 감지 상태에 따라 아이콘 색상 전환
            icon.SetColor(isDetected ? _config.enemyDetected.color : _config.enemy.color);

            Vector2 pos       = CalcMinimapPos(monster.transform.position, isMapImageMode);
            bool    inBounds  = isMapImageMode || pos.magnitude <= _config.minimapRadius;
            icon.UpdateIcon(pos, inBounds);
        }

        CleanupDeadEntries(toRemove, _enemyIconMap);
    }

    // ── 섹션 3: 일반 액터 아이콘 (NPC·채집 등) ───────────────

    private void UpdateActorIcons(bool isMapImageMode)
    {
        var toRemove = new List<GameActor>();

        foreach (var (actor, icon) in _actorIconMap)
        {
            if (actor == null || icon == null) { toRemove.Add(actor); continue; }

            Vector2 pos      = CalcMinimapPos(actor.transform.position, isMapImageMode);
            bool    inBounds = isMapImageMode || pos.magnitude <= _config.minimapRadius;
            icon.UpdateIcon(pos, inBounds);
        }

        CleanupDeadEntries(toRemove, _actorIconMap);
    }

    // ── 섹션 4: 퀘스트 마커 ──────────────────────────────────

    private void UpdateQuestMarkers(bool isMapImageMode)
    {
        if (!_config.showQuestMarkers) return;

        foreach (var (locationId, icon) in _questIconMap)
        {
            if (!MinimapMarkerRegistry.TryGet(locationId, out var registrar) || icon == null)
                continue;

            Vector2 pos      = CalcMinimapPos(registrar.WorldPosition, isMapImageMode);
            bool    inBounds = isMapImageMode || pos.magnitude <= _config.minimapRadius;
            icon.UpdateIcon(pos, inBounds);
        }
    }

    // ── 퀘스트 마커 관리 ─────────────────────────────────────

    private void RefreshAllQuestMarkers()
    {
        // 기존 퀘스트 마커 전체 제거 후 재구성
        foreach (var icon in _questIconMap.Values)
            if (icon != null) Destroy(icon.gameObject);
        _questIconMap.Clear();

        if (!_config.showQuestMarkers) return;

        var questManager = QuestManager.Instance;
        if (questManager == null || !questManager.IsDBLoaded) return;

        foreach (var runtime in questManager.GetActiveQuests())
        {
            foreach (var obj in runtime.QuestSO.objectives)
            {
                if (runtime.IsObjectiveComplete(obj)) continue;
                TryAddQuestMarker(obj);
            }
        }
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

    /// <summary>
    /// 퀘스트 목표 타입에 따라 미니맵 locationId를 반환합니다.
    /// · ReachLocation: targetStringId 를 그대로 사용
    /// · ItemDeliver  : "npc_{npcId}" 형식으로 NPC 마커를 찾음
    /// · 그 외        : locationId 없음 (마커 표시 안 함)
    /// </summary>
    private static string ResolveQuestLocationId(QuestObjectiveData obj)
    {
        return obj.type switch
        {
            QuestObjectiveType.ReachLocation => obj.targetStringId,
            QuestObjectiveType.ItemDeliver   => $"npc_{obj.npcId}",
            _                               => null,
        };
    }

    private MinimapIconConfigSO.IconEntry GetQuestMarkerEntry(QuestObjectiveData obj)
    {
        return obj.type == QuestObjectiveType.ItemDeliver
            ? _config.questNpc
            : _config.questTarget;
    }

    // ── 이벤트 핸들러 ────────────────────────────────────────

    private void OnQuestStateChanged(QuestStateEventData data)
    {
        // 퀘스트 수락/완료/실패 시 마커 전체 재구성
        RefreshAllQuestMarkers();
    }

    private void OnMarkerAdded(MinimapMarkerRegistrar registrar)
    {
        // 새로운 마커 등록 시 해당 locationId에 퀘스트가 있으면 즉시 표시
        if (!_config.showQuestMarkers) return;

        var questManager = QuestManager.Instance;
        if (questManager == null) return;

        foreach (var runtime in questManager.GetActiveQuests())
        {
            foreach (var obj in runtime.QuestSO.objectives)
            {
                if (runtime.IsObjectiveComplete(obj)) continue;
                if (ResolveQuestLocationId(obj) == registrar.LocationId)
                    TryAddQuestMarker(obj);
            }
        }
    }

    private void OnMarkerRemoved(MinimapMarkerRegistrar registrar)
    {
        TryRemoveQuestMarker(registrar.LocationId);
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

            var entry = _config.enemy;
            if (entry.sprite == null) return;
            if (_iconContainer == null) return;

            _enemyIconMap[monster] = MinimapEntityIcon.Create(_iconContainer, monster, entry);
            return;
        }

        // NPC / 채집 등 일반 액터
        if (actor.HasActorType(ActorType.NPC) && !_config.showNpcs) return;
        if (!actor.HasActorType(ActorType.NPC) && !_config.showGathering) return;
        if (_actorIconMap.ContainsKey(actor)) return;

        var actorEntry = _config.GetActorIconEntry(actor.ActorType);
        if (actorEntry.sprite == null) return;
        if (_iconContainer == null) return;

        _actorIconMap[actor] = MinimapEntityIcon.Create(_iconContainer, actor, actorEntry);
    }

    private void UnregisterActor(GameActor actor)
    {
        if (actor is MonsterActor monster)
        {
            RemoveFromMap(monster, _enemyIconMap);
            return;
        }
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
        foreach (var icon in _enemyIconMap.Values) if (icon) Destroy(icon.gameObject);
        foreach (var icon in _actorIconMap.Values)  if (icon) Destroy(icon.gameObject);
        foreach (var icon in _questIconMap.Values)  if (icon) Destroy(icon.gameObject);
        _enemyIconMap.Clear();
        _actorIconMap.Clear();
        _questIconMap.Clear();
    }

    // ── 좌표 변환 ────────────────────────────────────────────

    private Vector2 CalcMinimapPos(Vector3 worldPos, bool isMapImageMode)
    {
        if (isMapImageMode)
            return _config.WorldToMapImagePos(worldPos, _maskDisplaySize);

        Vector3 offset = worldPos - _player.transform.position;
        return new Vector2(offset.x, offset.z) * _config.worldToMinimapScale;
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
