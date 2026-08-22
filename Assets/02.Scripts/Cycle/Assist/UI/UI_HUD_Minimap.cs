using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Quest;
using UPlayGround.Data.UI;
using UPlayGround.Manager;
using UPlayGround.UI;
using UPlayGround.Cycle;

namespace UPlayGround.UI
{
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
    /// UI_HUD_Minimap (Canvas, CanvasLayer = HUD)
    ///   └─ MinimapMask (Image — 원형 스프라이트, Mask 컴포넌트)
    ///        ├─ MapBackground (Image) ← _mapBackground
    ///        ├─ QuestContainer  (RectTransform) ← _questContainer
    ///        ├─ IconContainer   (RectTransform) ← _iconContainer
    ///        └─ PlayerIcon      (Image) ← _playerIcon
    /// </code>
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class UI_HUD_Minimap : UI_Base
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

        [Tooltip("플레이어 아이콘을 제외한 동적 마커 갱신 간격(초)")]
        [Min(0.02f)]
        [SerializeField] private float _markerUpdateInterval = 0.1f;

        // ── 런타임 ───────────────────────────────────────────────
        private PlayerActor         _player;
        private MinimapIconConfigSO _config;

        // ── 확대 맵 상태 ─────────────────────────────────────────
        private bool      _isExpanded;
        private float     _currentMaskSize;
        private float     _currentMapZoom;
        private Vector2   _contentOffset;
        private Coroutine _expandCoroutine;
        private float     _markerUpdateTimer;

        // 섹션 1: 적 아이콘
        private readonly Dictionary<MonsterActor, MinimapEntityIcon> _enemyIconMap        = new();
        private readonly List<MonsterActor> _enemyRemovalBuffer = new();
        // 섹션 2: 일반 액터 아이콘 (NPC·채집 등)
        private readonly Dictionary<GameActor, MinimapEntityIcon>    _actorIconMap        = new();
        private readonly List<GameActor> _actorRemovalBuffer = new();
        private readonly List<string> _tempRemoveIds = new();
        // 섹션 3: 퀘스트 마커 (활성 퀘스트 연동)
        private readonly Dictionary<string, MinimapEntityIcon>       _questIconMap        = new();
        // 섹션 4: 정적 마커 (마을·포탈·고정 NPC·Custom)
        private readonly Dictionary<string, MinimapEntityIcon>       _staticMarkerIconMap = new();
        // 섹션 5: 사용자 마커
        private readonly Dictionary<int, MinimapEntityIcon>          _userMarkerIconMap   = new();
        private MinimapEntityIcon _remainsIcon;

        // ── UI_Base ──────────────────────────────────────────────

        protected override void OnShow()
        {
            // SceneContext.MapID 기반으로 Config 동적 로드
            if (_mapConfigDB == null)
            {
                Debug.LogError("[UI_HUD_Minimap] MapConfigDatabaseSO가 할당되지 않았습니다.");
                return;
            }

            string mapId = UISvc.Scene?.CurrentMapID;
            _config = _mapConfigDB.GetConfig(mapId);

            if (_config == null)
            {
                Debug.LogError($"[UI_HUD_Minimap] MapID '{mapId}'에 대한 MinimapIconConfigSO를 찾을 수 없습니다.");
                return;
            }

            _player = UISvc.Actors?.Player;

            // 상태 초기화
            _isExpanded      = false;
            _currentMaskSize = _maskDisplaySize;
            _currentMapZoom  = _config.mapZoom;
            _markerUpdateTimer = Mathf.Max(0.02f, _markerUpdateInterval);
            ApplyMaskSize(_currentMaskSize);

            NormalizeRectTransforms();
            SetupMapBackground();

            // 씬에 이미 존재하는 액터 등록
            if (UISvc.Actors?.AllActors != null)
            {
                foreach (var actor in UISvc.Actors.AllActors)
                    RegisterActor(actor);
            }

            RefreshAllQuestMarkers();
            RefreshAllStaticMarkers();

            foreach (var marker in MinimapUserMarkerSystem.GetAll())
                AddUserMarker(marker);

            if (CycleRemainsMarkerRegistry.HasMarker)
                OnRemainsMarkerChanged(CycleRemainsMarkerRegistry.Position);

            var gom = UISvc.Actors;
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
            CycleRemainsMarkerRegistry.OnMarkerChanged += OnRemainsMarkerChanged;
            CycleRemainsMarkerRegistry.OnMarkerRemoved += OnRemainsMarkerRemoved;

            var ev = Svc.Events;
            if (ev != null)
            {
                ev.Subscribe<QuestEvent, QuestStateEventData>(QuestEvent.QuestAccepted,  OnQuestStateChanged);
                ev.Subscribe<QuestEvent, QuestStateEventData>(QuestEvent.QuestCompleted, OnQuestStateChanged);
                ev.Subscribe<QuestEvent, QuestStateEventData>(QuestEvent.QuestFailed,    OnQuestStateChanged);
                // 목표가 하나 끝나면 다음 목표가 공개된다. 이 갱신을 놓치면 마커가 이전 목표에 멈춘다.
                ev.Subscribe<QuestEvent, QuestObjectiveEventData>(QuestEvent.QuestObjectiveUpdated, OnQuestObjectiveChanged);
            }
        }

        protected override void OnHide()
        {
            if (UISvc.Actors != null)
            {
                UISvc.Actors.OnActorRegistered   -= RegisterActor;
                UISvc.Actors.OnActorUnregistered -= UnregisterActor;
            }

            MinimapMarkerRegistry.OnMarkerAdded   -= OnMarkerAdded;
            MinimapMarkerRegistry.OnMarkerRemoved -= OnMarkerRemoved;

            MinimapUserMarkerSystem.OnMarkerAdded      -= AddUserMarker;
            MinimapUserMarkerSystem.OnMarkerRemoved    -= RemoveUserMarker;
            MinimapUserMarkerSystem.OnAllMarkersCleared -= ClearUserMarkers;
            CycleRemainsMarkerRegistry.OnMarkerChanged -= OnRemainsMarkerChanged;
            CycleRemainsMarkerRegistry.OnMarkerRemoved -= OnRemainsMarkerRemoved;

            if (Svc.Events != null)
            {
                var ev = Svc.Events;
                ev.Unsubscribe<QuestEvent, QuestStateEventData>(QuestEvent.QuestAccepted,  OnQuestStateChanged);
                ev.Unsubscribe<QuestEvent, QuestStateEventData>(QuestEvent.QuestCompleted, OnQuestStateChanged);
                ev.Unsubscribe<QuestEvent, QuestStateEventData>(QuestEvent.QuestFailed,    OnQuestStateChanged);
                ev.Unsubscribe<QuestEvent, QuestObjectiveEventData>(QuestEvent.QuestObjectiveUpdated, OnQuestObjectiveChanged);
            }

            if (_expandCoroutine != null)
            {
                StopCoroutine(_expandCoroutine);
                _expandCoroutine = null;
            }
            _isExpanded = false;
            _markerUpdateTimer = 0f;
            ApplyMaskSize(_maskDisplaySize);

            ClearAllIcons();
        }

        // ── 업데이트 ─────────────────────────────────────────────

        private void LateUpdate()
        {
            if (!IsVisible || _player == null || _config == null) return;

            UpdateContainerLayout();
            UpdatePlayerIcon();

            _markerUpdateTimer += Time.unscaledDeltaTime;
            float markerUpdateInterval = Mathf.Max(0.02f, _markerUpdateInterval);
            if (_markerUpdateTimer < markerUpdateInterval)
                return;

            _markerUpdateTimer %= markerUpdateInterval;
            UpdateEnemyIcons();
            UpdateActorIcons();
            UpdateQuestMarkers();
            UpdateStaticMarkers();
            UpdateUserMarkers();
            UpdateCycleMarkers();
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

        private void NormalizeRectTransforms()
        {
            SetupCenteredRect(_mapBackground != null ? _mapBackground.rectTransform : null);
            SetupCenteredRect(_iconContainer);
            SetupCenteredRect(_questContainer);
            SetupCenteredRect(_playerIcon);
        }

        private static void SetupCenteredRect(RectTransform rect)
        {
            if (rect == null) return;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot     = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;
        }

        // ── 초기 설정 ────────────────────────────────────────────

        private void SetupMapBackground()
        {
            if (_mapBackground == null) return;

            if (_config.backgroundSprite != null)
            {
                _mapBackground.rectTransform.sizeDelta = _config.GetMapDisplaySizeByHeight(_maskDisplaySize);

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
            // HUD 미니맵은 플레이어를 항상 중심에 둔다.
            Vector2 mapDisplaySize = GetCurrentMapDisplaySize();
            Vector2 playerMapPos = _config.WorldToMapImagePos(_player.transform.position, mapDisplaySize);
            Vector2 bgOffset = -playerMapPos * _currentMapZoom;
            _contentOffset = Vector2.zero;

            if (_mapBackground != null)
            {
                _mapBackground.rectTransform.sizeDelta        = mapDisplaySize;
                _mapBackground.rectTransform.localScale       = Vector3.one * _currentMapZoom;
                _mapBackground.rectTransform.anchoredPosition = bgOffset;
            }

            if (_iconContainer  != null) _iconContainer.anchoredPosition  = _contentOffset;
            if (_questContainer != null) _questContainer.anchoredPosition = _contentOffset;
        }

        // ── 플레이어 아이콘 ──────────────────────────────────────

        private void UpdatePlayerIcon()
        {
            if (_playerIcon == null) return;
            _playerIcon.anchoredPosition = _contentOffset;
            _playerIcon.localRotation    = Quaternion.Euler(0f, 0f, -_player.transform.eulerAngles.y);
        }

        // ── 적 아이콘 ────────────────────────────────────────────

        private void UpdateEnemyIcons()
        {
            _enemyRemovalBuffer.Clear();

            foreach (var (monster, icon) in _enemyIconMap)
            {
                if (monster == null || icon == null) { _enemyRemovalBuffer.Add(monster); continue; }
                // 런타임 스폰 직후 CycleBossRuntimeHandle이 붙기 전 일반 적으로 등록될 수 있다.
                // 전용 사이클 표식과 중복되지 않도록 다음 갱신에서 일반 적 아이콘을 제거한다.
                if (monster.GetComponent<CycleBossRuntimeHandle>() != null)
                {
                    _enemyRemovalBuffer.Add(monster);
                    continue;
                }

                bool isDetected = monster.Detection != null && monster.Detection.HasTarget;

                if (_config.showOnlyDetectedEnemies && !isDetected)
                {
                    icon.UpdateIcon(Vector2.zero, false);
                    continue;
                }

                icon.SetColor(isDetected ? _config.enemyDetected.color : _config.enemy.color);
                icon.UpdateIcon(CalcMinimapPos(monster.transform.position), true);
            }

            CleanupDeadEntries(_enemyRemovalBuffer, _enemyIconMap);
        }

        // ── 일반 액터 아이콘 ─────────────────────────────────────

        private void UpdateActorIcons()
        {
            _actorRemovalBuffer.Clear();

            foreach (var (actor, icon) in _actorIconMap)
            {
                if (actor == null || icon == null) { _actorRemovalBuffer.Add(actor); continue; }
                icon.UpdateIcon(CalcMinimapPos(actor.transform.position), true);
            }

            CleanupDeadEntries(_actorRemovalBuffer, _actorIconMap);
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

            var questManager = UISvc.Quest;
            if (questManager == null || !questManager.IsDBLoaded) return;

            // 아직 공개되지 않은 목표는 마커도 만들지 않는다. 지도와 월드 마커가 같은 순서를 안내해야 한다.
            foreach (var runtime in questManager.GetActiveQuests())
                foreach (var obj in runtime.GetActiveObjectives())
                    TryAddQuestMarker(obj);
        }

        private void TryAddQuestMarker(QuestObjectiveData objective)
        {
            if (!_config.showQuestMarkers) return;

            string locationId = QuestObjectiveMarker.ResolveLocationId(objective);
            if (string.IsNullOrEmpty(locationId)) return;
            if (_questIconMap.ContainsKey(locationId)) return;
            if (!MinimapMarkerRegistry.TryGet(locationId, out _)) return;

            var entry = _config.GetQuestMarkerEntry(objective);
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
        private void OnQuestObjectiveChanged(QuestObjectiveEventData data) => RefreshAllQuestMarkers();

        private void OnMarkerAdded(MinimapMarkerRegistrar registrar)
        {
            if (registrar.MarkerType == MinimapMarkerType.QuestTarget)
            {
                if (!_config.showQuestMarkers) return;
                var questManager = UISvc.Quest;
                if (questManager == null) return;
                foreach (var runtime in questManager.GetActiveQuests())
                    foreach (var obj in runtime.GetActiveObjectives())
                        if (QuestObjectiveMarker.ResolveLocationId(obj) == registrar.LocationId)
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
                if (monster.GetComponent<CycleBossRuntimeHandle>() != null) return;
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
            if (_remainsIcon != null) Destroy(_remainsIcon.gameObject);
            _enemyIconMap.Clear();
            _actorIconMap.Clear();
            _questIconMap.Clear();
            _staticMarkerIconMap.Clear();
            _userMarkerIconMap.Clear();
            _remainsIcon = null;
        }

        private void OnRemainsMarkerChanged(Vector3 position)
        {
            if (_config == null || !_config.showRemainsMarker || _iconContainer == null) return;
            if (_remainsIcon == null) _remainsIcon = MinimapEntityIcon.CreateStatic(_iconContainer, "cycle_remains", _config.remains);
        }

        private void OnRemainsMarkerRemoved()
        {
            if (_remainsIcon != null) Destroy(_remainsIcon.gameObject);
            _remainsIcon = null;
        }

        private void UpdateCycleMarkers()
        {
            if (_remainsIcon != null && CycleRemainsMarkerRegistry.HasMarker) _remainsIcon.UpdateIcon(CalcMinimapPos(CycleRemainsMarkerRegistry.Position), true);
        }

        // ── 좌표 변환 ────────────────────────────────────────────

        /// <summary>월드 좌표 → 미니맵 컨테이너 내 픽셀 좌표 (플레이어 기준 상대 좌표)</summary>
        private Vector2 CalcMinimapPos(Vector3 worldPos)
        {
            Vector2 mapDisplaySize = GetCurrentMapDisplaySize();
            Vector2 mapPos       = _config.WorldToMapImagePos(worldPos, mapDisplaySize);
            Vector2 playerMapPos = _config.WorldToMapImagePos(_player.transform.position, mapDisplaySize);
            return (mapPos - playerMapPos) * _currentMapZoom;
        }

        private Vector2 GetCurrentMapDisplaySize()
        {
            return _config != null
                ? _config.GetMapDisplaySizeByHeight(_currentMaskSize)
                : new Vector2(_currentMaskSize, _currentMaskSize);
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
}
