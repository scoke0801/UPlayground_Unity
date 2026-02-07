using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UPlayGround.Data.Path;
using UPlayGround.InputDefine;

namespace UPlayGround.Manager
{
    /// <summary>
    /// UI 캔버스 레이어 정의
    /// </summary>
    public enum CanvasLayer
    {
        // HUD -> SceneWindow -> PopupWindow -> SystemMessage
        HUD = 0, // HUD    
        Scene = 1000, // 씬
        Popup = 2000, // 팝업  
        System = 3000, // 시스템
    }

    /// <summary>
    /// UI 관리 매니저
    /// 7개의 캔버스 레이어를 관리하고 UI를 배치합니다.
    /// </summary>
    public class UIManager : BaseManager<UIManager>, IManager
    {
        // 캔버스 레이어별 간격
        private const int SORTING_ORDER_GAP = 100;

        // 캔버스 딕셔너리
        private Dictionary<CanvasLayer, Canvas> _canvasDictionary;

        // UI 오브젝트 추적 (선택적)
        private Dictionary<string, GameObject> _activeUIObjects;

        // UI_Base 컴포넌트 추적
        private Dictionary<string, UI_Base> _activeUIComponents;

        // 타입별 UI 추적 (빠른 타입 검색용)
        private Dictionary<System.Type, UI_Base> _uiByType;

        private const string DATABASE_PATH = "UIPrefabDatabase";

        // UI 프리팹 데이터베이스
        [SerializeField] private UIPrefabDatabase _uiPrefabDatabase;

        public bool IsInitialized { get; set; } = false;
        #region IManager 구현

        public void Init()
        {
            Debug.Log("[UIManager] 초기화 시작");

            _canvasDictionary = new Dictionary<CanvasLayer, Canvas>();
            _activeUIObjects = new Dictionary<string, GameObject>();
            _activeUIComponents = new Dictionary<string, UI_Base>();
            _uiByType = new Dictionary<System.Type, UI_Base>();

            CreateCanvasLayers();
            LoadUIPrefabDatabase();

            RegisterInputEvents();
            
            Debug.Log("[UIManager] 초기화 완료");
        }

        public void AfterInit()
        {
            
        }

        public void Dispose()
        {
            Debug.Log("[UIManager] 정리 시작");

            UnRegisterInputEvents();

            // 모든 활성 UI 제거 (UI_Base 먼저 정리)
            foreach (var ui in _activeUIComponents.Values)
            {
                if (ui != null)
                {
                    ui.Close();
                }
            }

            foreach (var ui in _activeUIObjects.Values)
            {
                if (ui != null)
                {
                    Destroy(ui);
                }
            }

            _activeUIObjects.Clear();
            _activeUIComponents.Clear();
            _uiByType.Clear();
            _canvasDictionary.Clear();

            Debug.Log("[UIManager] 정리 완료");
        }
        public void OnUpdate()
        {
        }

        public void OnFixedUpdate()
        {
        }

        public void OnLateUpdate()
        {
        }

        #endregion

        /// <summary>
        /// UIPrefabDatabase를 Resources 폴더에서 자동 로드
        /// </summary>
        private async void LoadUIPrefabDatabase()
        {
            var handle = Addressables.LoadAssetAsync<UIPrefabDatabase>(DATABASE_PATH);

            try
            {
                _uiPrefabDatabase = await handle.Task;

                if (_uiPrefabDatabase == null)
                {
                    Debug.LogError($"[UIManager] UIPrefabDatabase를 '{DATABASE_PATH}' 경로에서 찾을 수 없습니다.");
                    return;
                }

                _uiPrefabDatabase.Initialize();
                IsInitialized = true;
                Debug.Log($"[UIManager] UIPrefabDatabase 로드 완료");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UIManager] UIPrefabDatabase 로드 실패: {e.Message}");
            }
        }

        #region 캔버스 생성 및 관리

        /// <summary>
        /// 7개의 캔버스 레이어 생성
        /// </summary>
        private void CreateCanvasLayers()
        {
            // UIManager의 자식으로 캔버스들을 생성
            foreach (CanvasLayer layer in System.Enum.GetValues(typeof(CanvasLayer)))
            {
                Canvas canvas = CreateCanvas(layer);
    
                // 딕셔너리에 추가
                if (!_canvasDictionary.ContainsKey(layer))
                {
                    _canvasDictionary.Add(layer, canvas);
                }
            }
        }

        /// <summary>
        /// 개별 캔버스 생성
        /// </summary>
        private Canvas CreateCanvas(CanvasLayer layer)
        {
            // 캔버스 오브젝트 생성
            GameObject canvasObj = new GameObject($"Canvas_{layer}");
            canvasObj.transform.SetParent(transform);

            // Canvas 컴포넌트 추가 및 설정
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = (int)layer;


            // CanvasScaler 추가 (해상도 대응)
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.referenceResolution = new Vector2(2560, 1440);
            scaler.matchWidthOrHeight = 0.5f;

            // GraphicRaycaster 추가 (UI 입력 처리)
            canvasObj.AddComponent<GraphicRaycaster>();

            Debug.Log($"[UIManager] {layer} 캔버스 생성 (SortingOrder: {canvas.sortingOrder})");

            return canvas;
        }

        /// <summary>
        /// 특정 레이어의 캔버스 가져오기
        /// </summary>
        public Canvas GetCanvas(CanvasLayer layer)
        {
            if (_canvasDictionary.TryGetValue(layer, out Canvas canvas))
            {
                return canvas;
            }

            Debug.LogWarning($"[UIManager] {layer} 캔버스를 찾을 수 없습니다.");
            return null;
        }

        #endregion

        #region UI 배치 및 관리

        /// <summary>
        /// UI를 지정된 캔버스 레이어에 배치
        /// </summary>
        /// <param name="uiPrefab">UI 프리팹</param>
        /// <param name="layer">배치할 캔버스 레이어</param>
        /// <param name="uiName">UI 식별 이름 (선택)</param>
        /// <returns>생성된 UI GameObject</returns>
        public GameObject ShowUI(GameObject uiPrefab, CanvasLayer layer, string uiName = null)
        {
            // 활성 UI에 존재하면 바로 반환
            if (_activeUIObjects.TryGetValue(uiName, out var uiObject))
            {
                UI_Base ui = uiObject.GetComponentInChildren<UI_Base>();
                if (ui != null)
                {
                    ui.Initialize();
                    ui.Show();
                }

                return uiObject;
            }

            if (uiPrefab == null)
            {
                Debug.LogError("[UIManager] UI 프리팹이 null입니다.");
                return null;
            }

            Canvas targetCanvas = GetCanvas(layer);
            if (targetCanvas == null)
            {
                return null;
            }

            // UI 인스턴스 생성
            GameObject uiInstance = Instantiate(uiPrefab, targetCanvas.transform);
            // ✨ false를 사용하면 로컬 위치/스케일 유지
            //uiInstance.transform.SetParent(targetCanvas.transform, false);

            // UI 이름 설정 및 추적
            string finalName = string.IsNullOrEmpty(uiName) ? uiPrefab.name : uiName;
            uiInstance.name = finalName;

            // 활성 UI 딕셔너리에 추가
            if (_activeUIObjects.ContainsKey(finalName))
            {
                Debug.LogWarning($"[UIManager] '{finalName}' UI가 이미 존재합니다. 기존 UI를 제거합니다.");
                HideUI(finalName);
            }

            _activeUIObjects.Add(finalName, uiInstance);

            // UI_Base 컴포넌트가 있으면 자동으로 관리
            UI_Base uiBase = uiInstance.GetComponentInChildren<UI_Base>();
            if (uiBase != null)
            {
                _activeUIComponents.Add(finalName, uiBase);

                // 타입별 추적 (같은 타입이 여러 개면 마지막 것만 저장)
                _uiByType[uiBase.GetType()] = uiBase;

                // UI_Base 초기화 및 표시
                uiBase.Initialize();
                uiBase.Show();
            }

            Debug.Log($"[UIManager] '{finalName}' UI를 {layer} 레이어에 배치했습니다.");

            return uiInstance;
        }

        /// <summary>
        /// Key로 UI 프리팹을 로드하여 표시
        /// </summary>
        /// <param name="uiKey">UI 식별 키</param>
        /// <param name="layer">배치할 캔버스 레이어 (null이면 기본 레이어 사용)</param>
        /// <returns>생성된 UI GameObject</returns>
        public GameObject ShowUI(string uiKey, CanvasLayer? layer = null)
        {
            if (_uiPrefabDatabase == null)
            {
                Debug.LogError("[UIManager] UIPrefabDatabase가 로드되지 않았습니다.");
                return null;
            }

            var prefabEntry = _uiPrefabDatabase.GetPrefabEntry(uiKey);
            if (prefabEntry == null)
            {
                Debug.LogError($"[UIManager] '{uiKey}' UI를 찾을 수 없습니다.");
                return null;
            }

            if (prefabEntry.prefab == null)
            {
                Debug.LogError($"[UIManager] '{uiKey}' UI의 프리팹이 null입니다.");
                return null;
            }

            CanvasLayer targetLayer = layer ?? prefabEntry.defaultLayer;

            return ShowUI(prefabEntry.prefab, targetLayer, uiKey);
        }

        /// <summary>
        /// UI를 이름으로 숨기기
        /// </summary>
        public void HideUI(string uiName)
        {
            if (_activeUIObjects.TryGetValue(uiName, out GameObject uiObj))
            {
                // UI_Base 컴포넌트가 있으면 먼저 정리
                if (_activeUIComponents.TryGetValue(uiName, out UI_Base uiBase))
                {
                    // 타입별 추적에서 제거
                    if (_uiByType.ContainsKey(uiBase.GetType()) && _uiByType[uiBase.GetType()] == uiBase)
                    {
                        _uiByType.Remove(uiBase.GetType());
                    }

                    uiBase.Hide();
                }
                else
                {
                    // UI_Base가 아닌 경우 직접 숨김
                    uiObj.SetActive(false);
                }

                Debug.Log($"[UIManager] '{uiName}' UI를 숨김처리 했습니다.");
            }
            else
            {
                Debug.LogWarning($"[UIManager] '{uiName}' UI를 찾을 수 없습니다.");
            }
        }

        /// <summary>
        /// UI를 이름으로 삭제
        /// </summary>
        public void CloseUI(string uiName)
        {
            if (_activeUIObjects.TryGetValue(uiName, out GameObject uiObj))
            {
                // UI_Base 컴포넌트가 있으면 먼저 정리
                if (_activeUIComponents.TryGetValue(uiName, out UI_Base uiBase))
                {
                    // 타입별 추적에서 제거
                    if (_uiByType.ContainsKey(uiBase.GetType()) && _uiByType[uiBase.GetType()] == uiBase)
                    {
                        _uiByType.Remove(uiBase.GetType());
                    }

                    _activeUIComponents.Remove(uiName);
                    uiBase.Close();
                }
                else
                {
                    // UI_Base가 아닌 경우 직접 삭제
                    Destroy(uiObj);
                }

                _activeUIObjects.Remove(uiName);

                Debug.Log($"[UIManager] '{uiName}' UI를 제거했습니다.");
            }
            else
            {
                Debug.LogWarning($"[UIManager] '{uiName}' UI를 찾을 수 없습니다.");
            }
        }

        /// <summary>
        /// 활성화된 UI 가져오기
        /// </summary>
        public GameObject GetActiveUI(string uiName)
        {
            if (_activeUIObjects.TryGetValue(uiName, out GameObject uiObj))
            {
                return uiObj;
            }

            return null;
        }

        /// <summary>
        /// 특정 레이어의 모든 UI 숨기기
        /// </summary>
        public void HideAllUIInLayer(CanvasLayer layer)
        {
            Canvas targetCanvas = GetCanvas(layer);
            if (targetCanvas == null)
            {
                return;
            }

            // 해당 캔버스의 모든 자식 제거
            List<string> uisToRemove = new List<string>();

            foreach (var kvp in _activeUIObjects)
            {
                if (kvp.Value != null && kvp.Value.transform.parent == targetCanvas.transform)
                {
                    uisToRemove.Add(kvp.Key);
                }
            }

            foreach (var uiName in uisToRemove)
            {
                HideUI(uiName);
            }

            Debug.Log($"[UIManager] {layer} 레이어의 모든 UI를 제거했습니다.");
        }

        /// <summary>
        /// 모든 UI 숨기기
        /// </summary>
        public void HideAllUI()
        {
            List<string> allUINames = new List<string>(_activeUIObjects.Keys);

            foreach (var uiName in allUINames)
            {
                HideUI(uiName);
            }

            Debug.Log("[UIManager] 모든 UI를 제거했습니다.");
        }

        #endregion

        #region UI_Base 전용 관리

        /// <summary>
        /// UI_Base를 상속받은 UI 표시 (제네릭 버전)
        /// </summary>
        /// <typeparam name="T">UI_Base를 상속받은 타입</typeparam>
        /// <param name="uiPrefab">UI 프리팹</param>
        /// <param name="layer">배치할 캔버스 레이어</param>
        /// <param name="uiName">UI 식별 이름 (선택, 미지정 시 타입 이름 사용)</param>
        /// <returns>생성된 UI 컴포넌트</returns>
        public T ShowUI<T>(GameObject uiPrefab, CanvasLayer layer, string uiName = null) where T : UI_Base
        {
            // 이름이 없으면 타입 이름 사용
            string finalName = string.IsNullOrEmpty(uiName) ? typeof(T).Name : uiName;

            // 기존 ShowUI 메서드 호출
            GameObject uiInstance = ShowUI(uiPrefab, layer, finalName);

            if (uiInstance == null)
            {
                return null;
            }

            // UI_Base 컴포넌트 가져오기
            T uiComponent = uiInstance.GetComponent<T>();

            if (uiComponent == null)
            {
                Debug.LogError($"[UIManager] UI 프리팹에 {typeof(T)} 컴포넌트가 없습니다.");
                HideUI(finalName);
                return null;
            }

            return uiComponent;
        }

        /// <summary>
        /// UI_Base를 상속받은 UI 가져오기 (이름으로 검색)
        /// </summary>
        public T GetUI<T>(string uiName) where T : UI_Base
        {
            if (_activeUIComponents.TryGetValue(uiName, out UI_Base uiBase))
            {
                return uiBase as T;
            }

            return null;
        }

        /// <summary>
        /// UI_Base를 상속받은 UI 가져오기 (타입으로 검색)
        /// </summary>
        public T GetUI<T>() where T : UI_Base
        {
            if (_uiByType.TryGetValue(typeof(T), out UI_Base uiBase))
            {
                return uiBase as T;
            }

            return null;
        }

        /// <summary>
        /// 특정 타입의 UI가 활성화되어 있는지 확인
        /// </summary>
        public bool IsUIActive<T>() where T : UI_Base
        {
            return _uiByType.ContainsKey(typeof(T));
        }

        /// <summary>
        /// UI_Base 컴포넌트를 가진 모든 활성 UI 가져오기
        /// </summary>
        public List<UI_Base> GetAllActiveUIBases()
        {
            return new List<UI_Base>(_activeUIComponents.Values);
        }

        /// <summary>
        /// 특정 타입의 모든 UI 가져오기
        /// </summary>
        public List<T> GetAllUI<T>() where T : UI_Base
        {
            List<T> result = new List<T>();

            foreach (var uiBase in _activeUIComponents.Values)
            {
                if (uiBase is T typedUI)
                {
                    result.Add(typedUI);
                }
            }

            return result;
        }

        /// <summary>
        /// 특정 레이어의 모든 UI_Base 숨기기
        /// </summary>
        public void HideAllUIBaseInLayer(CanvasLayer layer)
        {
            Canvas targetCanvas = GetCanvas(layer);
            if (targetCanvas == null)
            {
                return;
            }

            List<string> uisToRemove = new List<string>();

            foreach (var kvp in _activeUIComponents)
            {
                if (kvp.Value != null && kvp.Value.transform.parent == targetCanvas.transform)
                {
                    uisToRemove.Add(kvp.Key);
                }
            }

            foreach (var uiName in uisToRemove)
            {
                HideUI(uiName);
            }

            Debug.Log($"[UIManager] {layer} 레이어의 모든 UI_Base를 제거했습니다.");
        }

        #endregion

        #region 유틸리티

        /// <summary>
        /// UI가 활성화되어 있는지 확인 (이름으로)
        /// </summary>
        public bool IsUIActive(string uiName)
        {
            if (_activeUIComponents.TryGetValue(uiName, out UI_Base ui))
            {
                return ui.IsVisible;
            }

            return false;
        }

        #endregion

        public CanvasLayer GetTopCanvasLayer()
        {
            
            // 열린 UI 중 상위 UI 처리
            var layers = (CanvasLayer[])System.Enum.GetValues(typeof(CanvasLayer));
            for (int i = layers.Length - 1; i >= 0; i--)
            {
                var layer = layers[i];

                if (_canvasDictionary.TryGetValue(layer, out Canvas canvas) == false)
                {
                    continue;
                }
                for (int childIndex = canvas.transform.childCount - 1; childIndex >= 0; childIndex--)
                {
                    Transform child = canvas.transform.GetChild(childIndex);
                    UI_Base uiBase = child.GetComponentInChildren<UI_Base>();
            
                    if (uiBase != null && uiBase.IsVisible)
                    {
                        return uiBase.Layer;
                    }
                }
            }

            return CanvasLayer.HUD;
        }
        
        private void RegisterInputEvents()
        {
            InputManager.Instance.RegisterInputEvent(InputMapNames.System, SystemAction.Back,
                null, OnPerformedBack, null, null, null, InputLayer.None);
        }

        private void UnRegisterInputEvents()
        {
            if (InputManager.Instance == null)
            {
                return;
            }
            
            InputManager.Instance.UnRegisterInputEvent(InputMapNames.System, SystemAction.Back,
                null, OnPerformedBack, null);

        }

        private void OnPerformedBack(InputAction.CallbackContext obj)
        {
            // 열린 UI 중 상위 UI 처리
            var layers = (CanvasLayer[])System.Enum.GetValues(typeof(CanvasLayer));
            for (int i = layers.Length - 1; i >= 0; i--)
            {
                var layer = layers[i];

                if (_canvasDictionary.TryGetValue(layer, out Canvas canvas) == false)
                {
                    continue;
                }
                for (int childIndex = canvas.transform.childCount - 1; childIndex >= 0; childIndex--)
                {
                    Transform child = canvas.transform.GetChild(childIndex);
                    UI_Base uiBase = child.GetComponentInChildren<UI_Base>();
            
                    if (uiBase != null && uiBase.IsVisible)
                    {
                        if (uiBase.IsCanCloseWithEsc == false)
                        {
                            continue;
                        }
                        
                        bool shouldContinue = uiBase.PerformBackFunction();
                        if (!shouldContinue)
                        {
                            return;
                        }
                    }
                }
            }
            
        }

    }
}