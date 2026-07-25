using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using UPlayGround.Data;
using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Item;
using UPlayGround.Data.Path;
using UPlayGround.Data.UI;
using UPlayGround.InputDefine;
using UPlayGround.UI;

namespace UPlayGround.Manager
{
    public class UIManager : BaseManager<UIManager>, IManager, IAsyncInitializableManager, IActorUIService,
        IUIRuntimeService
    {
        private const string DATABASE_PATH       = "UIPrefabDatabase";
        private const string FLOATER_CONFIG_PATH = "DamageFloaterConfig";
        private const string DANGER_RING_KEY     = "DangerRing"; // UIPrefabDatabase 키. enum 미생성 상태 대비 문자열 사용.
        private const string BREAK_INTERACTION_KEY = "BreakInteraction"; // 브레이크 공격 가능 상호작용 UI 프리팹 키.
        private const string UI_ROOT_PREFAB_PATH = "UIRoot";

        private Dictionary<CanvasLayer, Canvas>  _canvasDictionary;
        private Dictionary<string, GameObject>   _activeUIObjects;
        private Dictionary<string, UI_Base>      _activeUIComponents;
        private Dictionary<System.Type, UI_Base> _uiByType;

        [Header("UI Root")]
        [SerializeField] private GameObject _uiRootPrefab;

        private GameObject  _uiRootInstance;
        private EventSystem _eventSystem;
        private readonly List<InputActionReference> _uiInputActionReferences = new();

        private UI_WorldSpaceHudLayer _worldSpaceHudLayer;

        private UIPrefabDatabase      _uiPrefabDatabase;
        private DamageFloaterConfigSO _floaterConfig;

        public bool IsInitialized { get; set; } = false;
        public UI_WorldSpaceHudLayer WorldSpaceHudLayer => _worldSpaceHudLayer;

        #region IManager

        public void Init()
        {
            _canvasDictionary   = new Dictionary<CanvasLayer, Canvas>();
            _activeUIObjects    = new Dictionary<string, GameObject>();
            _activeUIComponents = new Dictionary<string, UI_Base>();
            _uiByType           = new Dictionary<System.Type, UI_Base>();
        }

        public async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            if (_uiRootPrefab == null)
            {
                _uiRootPrefab = await Svc.Asset.LoadGlobalAsync<GameObject>(
                    UI_ROOT_PREFAB_PATH,
                    nameof(UIManager),
                    cancellationToken);
            }

            CreateUIRoot();
            CreateCanvasLayers();
            EnsureEventSystem();
            RegisterInputEvents();
            await LoadAssetsAsync(cancellationToken);
        }

        public void AfterInit() { }

        public void Dispose()
        {
            UnRegisterInputEvents();

            foreach (var ui in _activeUIComponents.Values)
                ui?.Close();

            foreach (var ui in _activeUIObjects.Values)
            {
                if (ui != null) Destroy(ui);
            }

            _activeUIObjects.Clear();
            _activeUIComponents.Clear();
            _uiByType.Clear();
            _canvasDictionary.Clear();

            if (_uiRootInstance != null)
                Destroy(_uiRootInstance);

            _uiRootInstance = null;
            _eventSystem    = null;

            foreach (InputActionReference reference in _uiInputActionReferences)
            {
                if (reference != null)
                    Destroy(reference);
            }
            _uiInputActionReferences.Clear();

            _uiPrefabDatabase = null;
            _floaterConfig = null;
            IsInitialized = false;
        }

        public void OnUpdate()      { }
        public void OnFixedUpdate() { }
        public void OnLateUpdate()  { }
        public void OnSceneChanged(string sceneType)
        {
            EnsureEventSystem();
        }

        #endregion

        private async UniTask LoadAssetsAsync(CancellationToken cancellationToken)
        {
            _uiPrefabDatabase = await Svc.Asset.LoadGlobalAsync<UIPrefabDatabase>(
                DATABASE_PATH,
                nameof(UIManager),
                cancellationToken);
            _floaterConfig = await Svc.Asset.LoadGlobalAsync<DamageFloaterConfigSO>(
                FLOATER_CONFIG_PATH,
                nameof(UIManager),
                cancellationToken);

            _uiPrefabDatabase.Initialize();
            IsInitialized = true;

            _worldSpaceHudLayer.SetHpBarPrefab(GetUIPrefabEntry(UIKeyType.ActorHpBar.ToKey()));

            // Danger Ring 기본 프리팹 — DB에 없으면 null이고 CreateDangerRing이 조용히 스킵한다.
            _worldSpaceHudLayer.SetDangerRingPrefab(GetUIPrefabEntry(DANGER_RING_KEY));

            // Break Interaction 프리팹 — DB에 없으면 null이고 CreateBreakInteraction이 조용히 스킵한다.
            _worldSpaceHudLayer.SetBreakInteractionPrefab(GetUIPrefabEntry(BREAK_INTERACTION_KEY));

            var floaterPrefab = GetUIPrefabEntry(UIKeyType.DamageFloater.ToKey());
            if (floaterPrefab != null)
                _worldSpaceHudLayer.SetupFloaterPool(floaterPrefab, _floaterConfig);
            else
                Debug.LogWarning("[UIManager] 'DamageFloater' 프리팹이 UIPrefabDatabase에 없습니다.");

            Debug.Log("[UIManager] 에셋 로드 완료");
        }

        #region 캔버스 생성

        private void CreateUIRoot()
        {
            if (_uiRootPrefab == null)
                return;

            _uiRootInstance      = Instantiate(_uiRootPrefab, transform);
            _uiRootInstance.name = _uiRootPrefab.name;
        }

        private void CreateCanvasLayers()
        {
            RegisterCanvasLayersFromPrefab();

            foreach (CanvasLayer layer in System.Enum.GetValues(typeof(CanvasLayer)))
            {
                _canvasDictionary.TryGetValue(layer, out Canvas canvas);

                if (canvas == null)
                {
                    canvas = CreateCanvas(layer);
                    _canvasDictionary.Add(layer, canvas);
                }

                if (layer == CanvasLayer.HUD)
                {
                    _worldSpaceHudLayer = canvas.GetComponentInChildren<UI_WorldSpaceHudLayer>(true);
                    if (_worldSpaceHudLayer == null)
                        _worldSpaceHudLayer = CreateWorldSpaceHudLayer(canvas);

                    _worldSpaceHudLayer.Init(canvas);
                }
            }

            EnsureFocusIndicator();
            EnsureCursorClickEffect();
        }

        /// <summary>
        /// 게임패드 선택 표시기를 최상위 스크린 캔버스에 1개만 만든다.
        /// 프리팹마다 Selectable ColorBlock을 손보지 않고 전역으로 해결하기 위한 것이다.
        /// </summary>
        private void EnsureFocusIndicator()
        {
            if (!_canvasDictionary.TryGetValue(CanvasLayer.System, out Canvas systemCanvas)
                || systemCanvas == null)
            {
                return;
            }

            if (systemCanvas.GetComponentInChildren<UIFocusIndicator>(true) != null)
                return;

            var indicatorObject = new GameObject(
                "UIFocusIndicator",
                typeof(RectTransform),
                typeof(UIFocusIndicator));
            var rect = (RectTransform)indicatorObject.transform;
            rect.SetParent(systemCanvas.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            // 다른 UI가 나중에 추가돼도 테두리가 가려지지 않게 항상 맨 위로 둔다.
            rect.SetAsLastSibling();
        }

        /// <summary>
        /// 커서가 표시된 동안 모든 마우스 클릭 위치에 전역 리플 FX를 표시한다.
        /// </summary>
        private void EnsureCursorClickEffect()
        {
            if (!_canvasDictionary.TryGetValue(CanvasLayer.System, out Canvas systemCanvas)
                || systemCanvas == null)
            {
                return;
            }

            if (systemCanvas.GetComponentInChildren<UICursorClickEffect>(true) != null)
                return;

            var effectObject = new GameObject(
                "UICursorClickEffect",
                typeof(RectTransform));
            var rect = (RectTransform)effectObject.transform;
            rect.SetParent(systemCanvas.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.SetAsLastSibling();

            effectObject.AddComponent<UICursorClickEffect>();
        }

        private void RegisterCanvasLayersFromPrefab()
        {
            if (_uiRootInstance == null)
                return;

            var bindings = _uiRootInstance.GetComponentsInChildren<UICanvasLayerBinding>(true);
            foreach (var binding in bindings)
                RegisterCanvas(binding.Layer, binding.Canvas);

            if (bindings.Length > 0)
                return;

            var canvases = _uiRootInstance.GetComponentsInChildren<Canvas>(true);
            foreach (var canvas in canvases)
            {
                if (TryGetLayerFromCanvasName(canvas.name, out CanvasLayer layer))
                    RegisterCanvas(layer, canvas);
            }
        }

        private void RegisterCanvas(CanvasLayer layer, Canvas canvas)
        {
            if (canvas == null)
                return;

            canvas.sortingOrder = (int)layer;

            if (_canvasDictionary.ContainsKey(layer))
            {
                Debug.LogWarning($"[UIManager] {layer} 캔버스가 중복 등록되어 무시합니다: {canvas.name}");
                return;
            }

            _canvasDictionary.Add(layer, canvas);
        }

        private bool TryGetLayerFromCanvasName(string canvasName, out CanvasLayer layer)
        {
            foreach (CanvasLayer candidate in System.Enum.GetValues(typeof(CanvasLayer)))
            {
                if (canvasName.Equals($"Canvas_{candidate}", System.StringComparison.OrdinalIgnoreCase))
                {
                    layer = candidate;
                    return true;
                }
            }

            layer = default;
            return false;
        }

        private UI_WorldSpaceHudLayer CreateWorldSpaceHudLayer(Canvas hudCanvas)
        {
            GameObject layerObj = new GameObject("WorldSpaceHudLayer");
            layerObj.transform.SetParent(hudCanvas.transform, false);

            var rect = layerObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return layerObj.AddComponent<UI_WorldSpaceHudLayer>();
        }

        private Canvas CreateCanvas(CanvasLayer layer)
        {
            GameObject canvasObj = new GameObject($"Canvas_{layer}");
            canvasObj.transform.SetParent(_uiRootInstance != null ? _uiRootInstance.transform : transform);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = (int)layer;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.Expand;
            scaler.referenceResolution = new Vector2(2560, 1440);
            scaler.matchWidthOrHeight  = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public Canvas GetCanvas(CanvasLayer layer)
        {
            if (_canvasDictionary.TryGetValue(layer, out Canvas canvas))
                return canvas;

            Debug.LogWarning($"[UIManager] {layer} 캔버스를 찾을 수 없습니다.");
            return null;
        }

        private void EnsureEventSystem()
        {
            if (_uiRootInstance != null)
                _eventSystem = _uiRootInstance.GetComponentInChildren<EventSystem>(true);

            if (_eventSystem == null)
            {
                _eventSystem = EventSystem.current;

                if (_eventSystem != null && _uiRootInstance != null)
                    _eventSystem.transform.SetParent(_uiRootInstance.transform, true);
            }

            if (_eventSystem == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.transform.SetParent(_uiRootInstance != null ? _uiRootInstance.transform : transform);

                _eventSystem = eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<InputSystemUIInputModule>();
            }

            _eventSystem.gameObject.SetActive(true);

            if (_eventSystem.GetComponent<BaseInputModule>() == null)
            {
                _eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            ConfigureProjectUIInputActions();
            RemoveDuplicateEventSystems();
        }

        private void ConfigureProjectUIInputActions()
        {
            if (_eventSystem == null || Svc.Input == null)
                return;

            InputSystemUIInputModule module = _eventSystem.GetComponent<InputSystemUIInputModule>();
            if (module == null)
                return;

            foreach (InputActionReference reference in _uiInputActionReferences)
            {
                if (reference != null)
                    Destroy(reference);
            }
            _uiInputActionReferences.Clear();

            module.point = CreateUIActionReference(UIAction.Point);
            module.move = CreateUIActionReference(UIAction.Navigate);
            module.submit = CreateUIActionReference(UIAction.Submit);
            module.cancel = CreateUIActionReference(UIAction.Cancel);
            module.leftClick = CreateUIActionReference(UIAction.Click);
            module.rightClick = CreateUIActionReference(UIAction.RightClick);
            module.middleClick = CreateUIActionReference(UIAction.MiddleClick);
            module.scrollWheel = CreateUIActionReference(UIAction.ScrollWheel);
            // 빈 배경 클릭으로 게임패드 포커스가 유실되지 않게 유지한다.
            // 마우스 사용 중에는 UIFocusIndicator가 숨으므로 시각 충돌도 없다.
            module.deselectOnBackgroundClick = false;
            module.moveRepeatDelay = 0.35f;
            module.moveRepeatRate = 0.09f;
        }

        private InputActionReference CreateUIActionReference(string actionName)
        {
            InputAction action = Svc.Input.GetAction(InputMapNames.UI, actionName);
            if (action == null)
            {
                Debug.LogError($"[UIManager] UI 입력 액션을 찾을 수 없습니다: {actionName}");
                return null;
            }

            InputActionReference reference = InputActionReference.Create(action);
            _uiInputActionReferences.Add(reference);
            return reference;
        }

        private void RemoveDuplicateEventSystems()
        {
            var eventSystems = FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var eventSystem in eventSystems)
            {
                if (eventSystem == null || eventSystem == _eventSystem)
                    continue;

                Destroy(eventSystem.gameObject);
            }
        }

        #endregion

        #region UI 배치 및 관리

        public GameObject ShowUI(GameObject uiPrefab, CanvasLayer layer, string uiName = null)
        {
            if (_activeUIObjects.TryGetValue(uiName, out var uiObject))
            {
                UI_Base ui = uiObject.GetComponentInChildren<UI_Base>();
                ui?.Initialize();
                ui?.Show();
                return uiObject;
            }

            if (uiPrefab == null) { Debug.LogError("[UIManager] UI 프리팹이 null입니다."); return null; }

            Canvas targetCanvas = GetCanvas(layer);
            if (targetCanvas == null) return null;

            GameObject uiInstance = Instantiate(uiPrefab, targetCanvas.transform);
            string finalName = string.IsNullOrEmpty(uiName) ? uiPrefab.name : uiName;
            uiInstance.name = finalName;

            if (_activeUIObjects.ContainsKey(finalName))
            {
                Debug.LogWarning($"[UIManager] '{finalName}' 이미 존재, 기존 제거");
                HideUI(finalName);
            }

            _activeUIObjects.Add(finalName, uiInstance);

            UI_Base uiBase = uiInstance.GetComponentInChildren<UI_Base>();
            if (uiBase != null)
            {
                _activeUIComponents.Add(finalName, uiBase);
                _uiByType[uiBase.GetType()] = uiBase;
                uiBase.Initialize();
                uiBase.Show();
            }

            return uiInstance;
        }

        public GameObject ShowUI(string uiKey, CanvasLayer? layer = null)
        {
            if (_uiPrefabDatabase == null) { Debug.LogError("[UIManager] DB 미로드"); return null; }

            var entry = _uiPrefabDatabase.GetPrefabEntry(uiKey);
            if (entry?.prefab == null) { Debug.LogError($"[UIManager] '{uiKey}' 프리팹 없음"); return null; }

            return ShowUI(entry.prefab, layer ?? entry.defaultLayer, uiKey);
        }

        public GameObject ShowUI(UIKeyType uiKey, CanvasLayer? layer = null) => ShowUI(uiKey.ToKey(), layer);

        public GameObject GetUIPrefabEntry(string uiKey)
        {
            return _uiPrefabDatabase?.GetPrefabEntry(uiKey)?.prefab;
        }

        public void HideUI(string uiName)
        {
            if (!_activeUIObjects.TryGetValue(uiName, out GameObject uiObj)) return;

            if (_activeUIComponents.TryGetValue(uiName, out UI_Base uiBase))
            {
                if (_uiByType.TryGetValue(uiBase.GetType(), out var tracked) && tracked == uiBase)
                    _uiByType.Remove(uiBase.GetType());
                uiBase.Hide();
            }
            else
            {
                uiObj.SetActive(false);
            }
        }

        public void HideUI(UIKeyType uiKey) => HideUI(uiKey.ToKey());

        public void CloseUI(string uiName)
        {
            if (!_activeUIObjects.TryGetValue(uiName, out GameObject uiObj)) return;

            if (_activeUIComponents.TryGetValue(uiName, out UI_Base uiBase))
            {
                if (_uiByType.TryGetValue(uiBase.GetType(), out var tracked) && tracked == uiBase)
                    _uiByType.Remove(uiBase.GetType());
                _activeUIComponents.Remove(uiName);
                uiBase.Close();
                if (uiObj != null) Destroy(uiObj);
            }
            else
            {
                if (uiObj != null) Destroy(uiObj);
            }

            _activeUIObjects.Remove(uiName);
        }

        public GameObject GetActiveUI(string uiName)
        {
            _activeUIObjects.TryGetValue(uiName, out GameObject uiObj);
            return uiObj;
        }

        public GameObject GetActiveUI(UIKeyType uiKey) => GetActiveUI(uiKey.ToKey());

        #endregion

        #region WorldSpace HUD

        public UI_ActorHpBar CreateHpBar(GameActor actor)
        {
            return _worldSpaceHudLayer?.CreateHpBar(actor);
        }

        /// <summary>
        /// 브레이크 공격 가능(노출) 표시 상호작용 UI 생성. 프리팹 미등록 시 null 반환(조용히 스킵).
        /// </summary>
        public UI_BreakInteraction CreateBreakInteraction(GameActor actor)
        {
            return _worldSpaceHudLayer?.CreateBreakInteraction(actor);
        }

        /// <summary>
        /// 몬스터 공격 윈드업 Danger Ring 생성. skill.dangerRingPrefabKey가 있으면 해당 프리팹,
        /// 없으면 기본 프리팹을 사용한다. 프리팹 미등록 시 null 반환(조용히 스킵).
        /// </summary>
        public UI_DangerRing CreateDangerRing(GameActor actor, AbilityAttackInfo skill, float duration)
        {
            if (_worldSpaceHudLayer == null || skill == null) return null;

            GameObject overridePrefab = !string.IsNullOrWhiteSpace(skill.dangerRingPrefabKey)
                ? GetUIPrefabEntry(skill.dangerRingPrefabKey)
                : GetUIPrefabEntry(DANGER_RING_KEY);

            return _worldSpaceHudLayer.CreateDangerRing(actor, duration, skill.defenseType, overridePrefab);
        }

        public void ShowDamageFloater(Vector3 worldPos, float damage, FloatStyle style = FloatStyle.Normal)
        {
            _worldSpaceHudLayer?.ShowFloater(worldPos, damage, style);
        }

        public void ShowDamageFloaterLabel(Vector3 worldPos, string label, FloatStyle style = FloatStyle.Normal)
        {
            _worldSpaceHudLayer?.ShowFloaterLabel(worldPos, label, style);
        }

        public void ShowDamageFloaterMiss(Vector3 worldPos)
        {
            _worldSpaceHudLayer?.ShowFloaterMiss(worldPos);
        }

        /// <param name="style">기본값 Heal (플레이어). 몬스터 힐은 MonsterHeal 전달.</param>
        public void ShowDamageFloaterHeal(Vector3 worldPos, float amount, FloatStyle style = FloatStyle.Heal)
        {
            _worldSpaceHudLayer?.ShowFloaterHeal(worldPos, amount, style);
        }

        IActorHpBarView IActorUIService.CreateHpBar(GameActor actor) => CreateHpBar(actor);
        IActorDangerRingView IActorUIService.CreateDangerRing(
            GameActor actor,
            AbilityAttackInfo skill,
            float duration) => CreateDangerRing(actor, skill, duration);
        IActorBreakInteractionView IActorUIService.CreateBreakInteraction(GameActor actor) =>
            CreateBreakInteraction(actor);

        public bool HideHud(UIKeyType key)
        {
            GameObject active = GetActiveUI(key);
            UI_Base ui = active != null ? active.GetComponent<UI_Base>() : null;
            if (ui == null || !ui.IsVisible)
                return false;

            HideUI(key);
            return true;
        }

        public void ShowHud(UIKeyType key) => ShowUI(key);

        public void ShowItemAcquisition(ItemSO item)
        {
            if (item == null)
                return;

            GameObject ui = ShowUI(UIKeyType.ItemAcquisitionList);
            ui?.GetComponent<UI_ItemAcquisitionList>()?.SetItem(item);
        }

        public void RefreshInventoryIfVisible()
        {
            UI_Inventory inventory = GetActiveUI(UIKeyType.Inventory)?.GetComponent<UI_Inventory>();
            if (inventory != null && inventory.IsVisible)
                inventory.Show();
        }

        public void ShowInteractionBoard(InteractableActorSO data, float current, float max)
        {
            UI_InteractionHPBoard board =
                ShowUI(UIKeyType.InteractionHPBoard)?.GetComponent<UI_InteractionHPBoard>();
            if (board == null)
                return;

            board.BoardFill(current, max);
            board.SetInteractionData(data);
        }

        public void UpdateInteractionBoard(float current, float max)
        {
            GetUI<UI_InteractionHPBoard>(UIKeyType.InteractionHPBoard)?.BoardFill(current, max);
        }

        public void ShowRestGrowth() => ShowUI("RestGrowth", CanvasLayer.Popup);

        public void ShowRespawn(System.Action<float> onSpotRevive, System.Action onPortalRevive)
        {
            GameObject ui = ShowUI(UIKeyType.RespawnPopup);
            UI_RespawnPopup popup = ui?.GetComponentInChildren<UI_RespawnPopup>();
            if (popup == null)
            {
                onPortalRevive?.Invoke();
                return;
            }

            float spotHealPercent = popup.SpotHealPercent;
            popup.Setup(
                () => onSpotRevive?.Invoke(spotHealPercent),
                onPortalRevive);
        }

        #endregion

        #region UI_Base 전용 관리

        public T ShowUI<T>(GameObject uiPrefab, CanvasLayer layer, string uiName = null) where T : UI_Base
        {
            string finalName    = string.IsNullOrEmpty(uiName) ? typeof(T).Name : uiName;
            GameObject instance = ShowUI(uiPrefab, layer, finalName);
            if (instance == null) return null;

            T uiComponent = instance.GetComponent<T>();
            if (uiComponent == null)
            {
                Debug.LogError($"[UIManager] {typeof(T)} 컴포넌트 없음");
                HideUI(finalName);
                return null;
            }
            return uiComponent;
        }

        public T GetUI<T>(string uiName) where T : UI_Base
        {
            _activeUIComponents.TryGetValue(uiName, out UI_Base uiBase);
            return uiBase as T;
        }

        public T GetUI<T>(UIKeyType uiKey) where T : UI_Base => GetUI<T>(uiKey.ToKey());

        public T GetUI<T>() where T : UI_Base
        {
            _uiByType.TryGetValue(typeof(T), out UI_Base uiBase);
            return uiBase as T;
        }

        public bool IsUIActive<T>() where T : UI_Base => _uiByType.ContainsKey(typeof(T));

        public List<UI_Base> GetAllActiveUIBases() => new List<UI_Base>(_activeUIComponents.Values);

        public List<T> GetAllUI<T>() where T : UI_Base
        {
            var result = new List<T>();
            foreach (var uiBase in _activeUIComponents.Values)
                if (uiBase is T t) result.Add(t);
            return result;
        }

        #endregion

        #region 유틸리티

        public bool IsUIActive(string uiName)
        {
            return _activeUIComponents.TryGetValue(uiName, out UI_Base ui) && ui.IsVisible;
        }

        public void HideAllUIInLayer(CanvasLayer layer)
        {
            Canvas targetCanvas = GetCanvas(layer);
            if (targetCanvas == null) return;

            var toRemove = new List<string>();
            foreach (var kvp in _activeUIObjects)
                if (kvp.Value != null && kvp.Value.transform.parent == targetCanvas.transform)
                    toRemove.Add(kvp.Key);

            foreach (var name in toRemove) HideUI(name);
        }

        public void HideAllUI()
        {
            var allNames = new List<string>(_activeUIObjects.Keys);
            foreach (var name in allNames) HideUI(name);
        }

        public CanvasLayer GetTopCanvasLayer()
        {
            var layers = (CanvasLayer[])System.Enum.GetValues(typeof(CanvasLayer));
            for (int i = layers.Length - 1; i >= 0; i--)
            {
                if (!_canvasDictionary.TryGetValue(layers[i], out Canvas canvas)) continue;

                for (int c = canvas.transform.childCount - 1; c >= 0; c--)
                {
                    var uiBase = canvas.transform.GetChild(c).GetComponentInChildren<UI_Base>();
                    if (uiBase != null && uiBase.IsVisible) return uiBase.Layer;
                }
            }
            return CanvasLayer.HUD;
        }

        /// <summary>
        /// 현재 가시(visible)이면서 입력을 차단(BlocksInput)하는 UI들 중 최상위 레이어를 InputLayer로 반환한다.
        /// 그런 UI가 없으면 Level_0(게임플레이)을 반환한다.
        /// GetTopCanvasLayer와 달리 비-차단 UI(커서 전용 팝업 등)는 계산에서 제외해, 입력 레이어를
        /// "올린 기준(BlocksLowerInput)"과 "복원 기준"을 대칭으로 맞춘다.
        /// 중첩된 UI_Base까지 모두 검사하므로 계층 구조에 관계없이 정확하다.
        /// </summary>
        public InputLayer GetTopBlockingInputLayer()
        {
            InputLayer top = InputLayer.Level_0;
            foreach (var canvas in _canvasDictionary.Values)
            {
                if (canvas == null) continue;

                var uiBases = canvas.GetComponentsInChildren<UI_Base>(true);
                foreach (var ui in uiBases)
                {
                    if (ui == null || !ui.IsVisible || !ui.BlocksInput) continue;

                    InputLayer layer = ui.Layer.ToInputLayer();
                    if (layer > top) top = layer;
                }
            }
            return top;
        }

        #endregion

        #region Input

        private void RegisterInputEvents()
        {
            Svc.Input.RegisterInputEvent(InputMapNames.UI, UIAction.Cancel,
                null, OnPerformedBack, null, null, null, InputLayer.None);
        }

        private void UnRegisterInputEvents()
        {
            if (Svc.Input == null) return;
            Svc.Input.UnRegisterInputEvent(InputMapNames.UI, UIAction.Cancel,
                null, OnPerformedBack, null);
        }

        private void OnPerformedBack(InputAction.CallbackContext obj)
        {
            var layers = (CanvasLayer[])System.Enum.GetValues(typeof(CanvasLayer));

            for (int i = layers.Length - 1; i >= 0; i--)
            {
                if (!_canvasDictionary.TryGetValue(layers[i], out Canvas canvas)) continue;

                for (int c = canvas.transform.childCount - 1; c >= 0; c--)
                {
                    var uiBase = canvas.transform.GetChild(c).GetComponentInChildren<UI_Base>();
                    if (uiBase == null || !uiBase.IsVisible)
                        continue;

                    if (uiBase.IsCanCloseWithEsc)
                    {
                        uiBase.PerformBackFunction();
                        return;
                    }

                    // 닫기 불가 차단 모달은 Cancel을 소비하고,
                    // 비차단 HUD/알림은 건너뛰어 상위 메뉴 또는 Pause 토글까지 전달한다.
                    if (uiBase.BlocksInput)
                        return;
                }
            }

            // 열린 UI가 없을 때만 PauseMenu 토글
            if (UISvc.Scene?.CurrentSceneType == SceneType.GamePlay)
            {
                UI_Base ui = GetActiveUI(UIKeyType.PauseMenu)?.GetComponent<UI_Base>();
                if (ui == null || !ui.IsVisible) ShowUI(UIKeyType.PauseMenu);
                else HideUI(UIKeyType.PauseMenu);
            }
        }

        #endregion
    }
}

namespace UPlayGround.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    public class UICanvasLayerBinding : MonoBehaviour
    {
        [SerializeField] private UPlayGround.Manager.CanvasLayer _layer = UPlayGround.Manager.CanvasLayer.Scene;

        public UPlayGround.Manager.CanvasLayer Layer => _layer;
        public Canvas Canvas => GetComponent<Canvas>();
    }
}
