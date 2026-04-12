using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UPlayGround.Data.Path;
using UPlayGround.Data.UI;
using UPlayGround.Enum;
using UPlayGround.InputDefine;
using UPlayGround.UI;

namespace UPlayGround.Manager
{
    public enum CanvasLayer
    {
        HUD        = 0,
        Scene      = 1000,
        Popup      = 2000,
        System     = 3000,
        WorldSpace = 10000,
    }

    public class UIManager : BaseManager<UIManager>, IManager
    {
        private const string DATABASE_PATH       = "UIPrefabDatabase";
        private const string FLOATER_CONFIG_PATH = "DamageFloaterConfig";

        private Dictionary<CanvasLayer, Canvas>  _canvasDictionary;
        private Dictionary<string, GameObject>   _activeUIObjects;
        private Dictionary<string, UI_Base>      _activeUIComponents;
        private Dictionary<System.Type, UI_Base> _uiByType;

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

            CreateCanvasLayers();
            LoadAssetsAsync();
            RegisterInputEvents();
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
        }

        public void OnUpdate()      { }
        public void OnFixedUpdate() { }
        public void OnLateUpdate()  { }
        public void OnSceneChanged(string sceneType) { }

        #endregion

        private async void LoadAssetsAsync()
        {
            var dbTask     = Addressables.LoadAssetAsync<UIPrefabDatabase>(DATABASE_PATH).Task;
            var configTask = Addressables.LoadAssetAsync<DamageFloaterConfigSO>(FLOATER_CONFIG_PATH).Task;

            await Task.WhenAll(dbTask, configTask);

            _uiPrefabDatabase = dbTask.Result;
            _floaterConfig    = configTask.Result;

            if (_uiPrefabDatabase == null)
            {
                Debug.LogError($"[UIManager] UIPrefabDatabase '{DATABASE_PATH}' 로드 실패");
                return;
            }
            if (_floaterConfig == null)
            {
                Debug.LogError($"[UIManager] DamageFloaterConfig '{FLOATER_CONFIG_PATH}' 로드 실패");
                return;
            }

            _uiPrefabDatabase.Initialize();
            IsInitialized = true;

            _worldSpaceHudLayer.SetHpBarPrefab(GetUIPrefabEntry(UIKeyType.ActorHpBar.ToKey()));

            var floaterPrefab = GetUIPrefabEntry(UIKeyType.DamageFloater.ToKey());
            if (floaterPrefab != null)
                _worldSpaceHudLayer.SetupFloaterPool(floaterPrefab, _floaterConfig);
            else
                Debug.LogWarning("[UIManager] 'DamageFloater' 프리팹이 UIPrefabDatabase에 없습니다.");

            Debug.Log("[UIManager] 에셋 로드 완료");
        }

        #region 캔버스 생성

        private void CreateCanvasLayers()
        {
            foreach (CanvasLayer layer in System.Enum.GetValues(typeof(CanvasLayer)))
            {
                Canvas canvas = CreateCanvas(layer);

                if (!_canvasDictionary.ContainsKey(layer))
                    _canvasDictionary.Add(layer, canvas);

                if (layer == CanvasLayer.HUD)
                {
                    _worldSpaceHudLayer = CreateWorldSpaceHudLayer(canvas);
                    _worldSpaceHudLayer.Init(canvas);
                }
            }
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
            canvasObj.transform.SetParent(transform);

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
            if (!_activeUIObjects.TryGetValue(uiName, out _)) return;

            if (_activeUIComponents.TryGetValue(uiName, out UI_Base uiBase))
            {
                if (_uiByType.TryGetValue(uiBase.GetType(), out var tracked) && tracked == uiBase)
                    _uiByType.Remove(uiBase.GetType());
                _activeUIComponents.Remove(uiName);
                uiBase.Close();
            }
            else
            {
                Destroy(_activeUIObjects[uiName]);
            }

            _activeUIObjects.Remove(uiName);
        }

        public GameObject GetActiveUI(string uiName)
        {
            _activeUIObjects.TryGetValue(uiName, out GameObject uiObj);
            return uiObj;
        }

        #endregion

        #region WorldSpace HUD

        public UI_ActorHpBar CreateHpBar(GameActor actor)
        {
            return _worldSpaceHudLayer?.CreateHpBar(actor);
        }

        public void ShowDamageFloater(Vector3 worldPos, float damage, FloatStyle style = FloatStyle.Normal)
        {
            _worldSpaceHudLayer?.ShowFloater(worldPos, damage, style);
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

        #endregion

        #region Input

        private void RegisterInputEvents()
        {
            InputManager.Instance.RegisterInputEvent(InputMapNames.System, SystemAction.Back,
                null, OnPerformedBack, null, null, null, InputLayer.None);
        }

        private void UnRegisterInputEvents()
        {
            if (InputManager.Instance == null) return;
            InputManager.Instance.UnRegisterInputEvent(InputMapNames.System, SystemAction.Back,
                null, OnPerformedBack, null);
        }

        private void OnPerformedBack(InputAction.CallbackContext obj)
        {
            var layers = (CanvasLayer[])System.Enum.GetValues(typeof(CanvasLayer));
            bool handled = false;

            for (int i = layers.Length - 1; i >= 0; i--)
            {
                if (!_canvasDictionary.TryGetValue(layers[i], out Canvas canvas)) continue;

                for (int c = canvas.transform.childCount - 1; c >= 0; c--)
                {
                    var uiBase = canvas.transform.GetChild(c).GetComponentInChildren<UI_Base>();
                    if (uiBase == null || !uiBase.IsVisible || !uiBase.IsCanCloseWithEsc) continue;

                    if (!uiBase.PerformBackFunction()) return;
                    handled = true;
                }
            }

            // 열린 UI가 없을 때만 PauseMenu 토글
            if (!handled && SceneManager.Instance?.CurrentSceneType == SceneType.GamePlay)
            {
                UI_Base ui = GetActiveUI("PauseMenu")?.GetComponent<UI_Base>();
                if (ui == null || !ui.IsVisible) ShowUI(UIKeyType.PauseMenu);
                else HideUI(UIKeyType.PauseMenu);
            }
        }

        #endregion
    }
}
