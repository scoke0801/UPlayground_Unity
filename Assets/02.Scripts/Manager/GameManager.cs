using UnityEngine;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UPlayGround.Story;
using UPlayGround.Dialogue;
using UPlayGround.Manager.Handler;
using UPlayGround.Manager.Combat;
#if UNITY_EDITOR
using UPlayGround.Debugging;
#endif

namespace UPlayGround.Manager
{
    /// <summary>
    /// 모든 매니저를 관리하는 최상위 매니저
    /// </summary>
    public class GameManager : BaseManager<GameManager>
    {
        // 등록된 매니저 리스트
        private List<IManager> _registeredManagers = new List<IManager>();
        private readonly List<IUpdatableManager> _updatableManagers = new();
        private readonly List<IFixedUpdatableManager> _fixedUpdatableManagers = new();
        private readonly List<ILateUpdatableManager> _lateUpdatableManagers = new();
        // GetManager<T> 선형 탐색 제거용 타입 캐시
        private Dictionary<System.Type, IManager> _managerLookup = new Dictionary<System.Type, IManager>();
        private readonly Dictionary<string, float> _managerInitializationMilliseconds = new();
        private CancellationTokenSource _initializationCancellation;

        // 초기화 플래그
        public bool IsInitialized { get; private set; } = false;
        public GameBootState BootState { get; private set; } = GameBootState.None;
        public string InitializationFailure { get; private set; }
        public IReadOnlyDictionary<string, float> ManagerInitializationMilliseconds =>
            _managerInitializationMilliseconds;

        protected override void Awake()
        {
            base.Awake();

            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = -1;

            // KCC AutoSimulation 제어권을 KCCSimulator에 위임
            gameObject.AddComponent<KCCSimulator>();

            if (this != null && BootState == GameBootState.None)
                InitializeManagersAsync().Forget(HandleInitializationException);
        }

        /// <summary>
        /// 모든 매니저 초기화
        /// </summary>
        private async UniTask InitializeManagersAsync()
        {
            if (BootState is GameBootState.Initializing or GameBootState.Ready)
                return;

            BootState = GameBootState.Initializing;
            InitializationFailure = null;
            _managerInitializationMilliseconds.Clear();
            _initializationCancellation?.Dispose();
            _initializationCancellation = new CancellationTokenSource();
            CancellationToken cancellationToken = _initializationCancellation.Token;

            int width = PlayerPrefs.GetInt("ResWidth", 2560);
            int height = PlayerPrefs.GetInt("ResHeight", 1440);
            FullScreenMode mode = (FullScreenMode)PlayerPrefs.GetInt("FullscreenMode", (int)FullScreenMode.FullScreenWindow);

            Screen.SetResolution(width, height, mode);
            
            Debug.Log("[GameManager] 매니저 초기화 시작");

            // 초기화 순서대로 등록
            RegisterManager(SaveManager.Instance);  // 세이브/로드 (다른 매니저보다 먼저)
            RegisterManager(InputManager.Instance); // 입력 시스템

            RegisterManager(AssetManager.Instance);
            RegisterManager(SettingsManager.Instance); // 설정 (Addressable 로드 → 시스템 반영)
            RegisterManager(SoundManager.Instance); // 사운드 재생/풀링
            RegisterManager(UIManager.Instance); // UI 관리
            RegisterManager(CameraManager.Instance); // 카메라 시스템
            RegisterManager(GameObjectManager.Instance);
            RegisterManager(PartyManager.Instance);
            RegisterManager(ItemManager.Instance);
            RegisterManager(InventoryManager.Instance);
            RegisterManager(EventManager.Instance);
            RegisterManager(GameCombatManager.Instance);
            RegisterManager(GlobalFlagManager.Instance);
            RegisterManager(DialogueManager.Instance);
            RegisterManager(StoryManager.Instance);
            RegisterManager(GameTimeManager.Instance);

            RegisterManager(WorldStateManager.Instance);  // 맵 월드 상태(몬스터 처치 영속)
            RegisterManager(ActorSpawnManager.Instance);
            RegisterManager(AgentTickManager.Instance); // 적 AI 컴포넌트 일괄 틱 (개별 Update 통합)
            RegisterManager(SceneManager.Instance);
#if UNITY_EDITOR
            RegisterManager(DebugGizmoManager.Instance); // 디버그 기즈모는 에디터 전용 — 빌드 제외
#endif
            RegisterManager(CheatManager.Instance);
            RegisterManager(RecipeManager.Instance);
            RegisterManager(QuestManager.Instance);

            await InitializeAsyncManagers(cancellationToken);

            // Init이후에 후처리 필요한 경우 
            AfterInit();
            
            IsInitialized = true;
            BootState = GameBootState.Ready;

            Debug.Log($"[GameManager] {_registeredManagers.Count}개의 매니저 초기화 완료");
        }

        private async UniTask InitializeAsyncManagers(CancellationToken cancellationToken)
        {
            foreach (var manager in _registeredManagers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (manager is not IAsyncInitializableManager asyncManager)
                    continue;

                string managerName = manager.GetType().Name;
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                Debug.Log($"[GameManager] {managerName} 비동기 초기화 시작");

                try
                {
                    await asyncManager.InitializeAsync(cancellationToken);
                }
                finally
                {
                    stopwatch.Stop();
                    float elapsedMilliseconds = (float)stopwatch.Elapsed.TotalMilliseconds;
                    _managerInitializationMilliseconds[managerName] = elapsedMilliseconds;
                    Debug.Log($"[GameManager] {managerName} 비동기 초기화 종료 ({elapsedMilliseconds:F1} ms)");
                }
            }
        }

        private void HandleInitializationException(System.Exception exception)
        {
            if (exception is System.OperationCanceledException &&
                BootState == GameBootState.Disposing)
            {
                return;
            }

            IsInitialized = false;
            BootState = GameBootState.Failed;
            InitializationFailure = exception.Message;
            Debug.LogException(exception);
            Debug.LogError($"[GameManager] 초기화 실패: {InitializationFailure}");
        }


        /// <summary>
        /// 매니저 등록 및 초기화
        /// </summary>
        public void RegisterManager(IManager manager)
        {
            if (manager == null)
            {
                Debug.LogWarning("[GameManager] null 매니저는 등록할 수 없습니다.");
                return;
            }

            if (_registeredManagers.Contains(manager))
            {
                Debug.LogWarning($"[GameManager] {manager.GetType().Name}은 이미 등록되어 있습니다.");
                return;
            }

            _registeredManagers.Add(manager);
            _managerLookup[manager.GetType()] = manager;
            if (manager is IUpdatableManager updatable)
                _updatableManagers.Add(updatable);
            if (manager is IFixedUpdatableManager fixedUpdatable)
                _fixedUpdatableManagers.Add(fixedUpdatable);
            if (manager is ILateUpdatableManager lateUpdatable)
                _lateUpdatableManagers.Add(lateUpdatable);
            manager.Init();

            Debug.Log($"[GameManager] {manager.GetType().Name} 등록 완료");
        }

        /// <summary>
        /// 매니저 등록 해제
        /// </summary>
        public void UnregisterManager(IManager manager)
        {
            if (manager == null) return;

            if (_registeredManagers.Contains(manager))
            {
                manager.Dispose();
                _registeredManagers.Remove(manager);
                _managerLookup.Remove(manager.GetType());
                if (manager is IUpdatableManager updatable)
                    _updatableManagers.Remove(updatable);
                if (manager is IFixedUpdatableManager fixedUpdatable)
                    _fixedUpdatableManagers.Remove(fixedUpdatable);
                if (manager is ILateUpdatableManager lateUpdatable)
                    _lateUpdatableManagers.Remove(lateUpdatable);
                Debug.Log($"[GameManager] {manager.GetType().Name} 등록 해제");
            }
        }

        /// <summary>
        /// 특정 타입의 매니저 가져오기
        /// </summary>
        public T GetManager<T>() where T : class, IManager
        {
            // 구체 타입 조회는 딕셔너리로 O(1)
            if (_managerLookup.TryGetValue(typeof(T), out var exact))
                return exact as T;

            // 인터페이스/베이스 타입 조회는 선형 폴백
            foreach (var manager in _registeredManagers)
            {
                if (manager is T typedManager)
                {
                    return typedManager;
                }
            }

            return null;
        }

        private void AfterInit()
        {
            foreach (var manager in _registeredManagers)
                manager?.AfterInit();
        }

        /// <summary>
        /// SceneManager가 씬 컨텍스트 확정 후 호출.
        /// 모든 매니저에 씬 전환 사실을 전파한다.
        /// </summary>
        public void NotifySceneChanged(string sceneType)
        {
            foreach (var manager in _registeredManagers)
                manager?.OnSceneChanged(sceneType);
        }
        private void Update()
        {
            if (!IsInitialized) return;

            for (int i = 0; i < _updatableManagers.Count; i++)
                _updatableManagers[i].OnUpdate();
        }

        private void FixedUpdate()
        {
            if (!IsInitialized) return;

            for (int i = 0; i < _fixedUpdatableManagers.Count; i++)
                _fixedUpdatableManagers[i].OnFixedUpdate();
        }

        private void LateUpdate()
        {
            if (!IsInitialized) return;

            for (int i = 0; i < _lateUpdatableManagers.Count; i++)
                _lateUpdatableManagers[i].OnLateUpdate();
        }

        protected override void OnDestroy()
        {
            BootState = GameBootState.Disposing;
            _initializationCancellation?.Cancel();

            // 모든 매니저 정리
            for (int i = _registeredManagers.Count - 1; i >= 0; i--)
            {
                _registeredManagers[i]?.Dispose();
            }

            _registeredManagers.Clear();
            _updatableManagers.Clear();
            _fixedUpdatableManagers.Clear();
            _lateUpdatableManagers.Clear();
            _managerLookup.Clear();
            _managerInitializationMilliseconds.Clear();
            IsInitialized = false;
            _initializationCancellation?.Dispose();
            _initializationCancellation = null;

            Debug.Log("[GameManager] 정리 완료");

            base.OnDestroy();
        }
    }
}
