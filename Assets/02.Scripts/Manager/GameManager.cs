using UnityEngine;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UPlayGround.Story;
using UPlayGround.Dialogue;
using UPlayGround.CameraSystem;
using UPlayGround.Manager.Handler;
using UPlayGround.Manager.Combat;
using UPlayGround.Manager.Cinematic;
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
        private readonly HashSet<IManager> _runtimeReadyManagers = new();
        private readonly HashSet<IManager> _afterInitializedManagers = new();
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

#if DEVELOPMENT_BUILD
            DisableDevelopmentConsole();
            Application.logMessageReceived += SuppressDevelopmentConsole;
#endif

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
            CameraRuntimeServices.Configure(new UPlayGroundCameraRuntimeAdapter());
            RegisterManager(CameraManager.Instance); // 카메라 시스템
            RegisterManager(CinematicStageManager.Instance); // 궁극기/처형기 전용 표현 무대
            RegisterManager(GameObjectManager.Instance);
            RegisterManager(ProjectileManager.Instance); // 조합형 투사체 풀/일괄 틱
            RegisterManager(PartyManager.Instance);
            RegisterManager(ActorSimulationManager.Instance); // 일반 몬스터/NPC 거리 기반 시뮬레이션
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
            RegisterManager(MonsterCodexManager.Instance); // 종별 도감 기록/전투 보정
            RegisterManager(AgentTickManager.Instance); // 적 AI 컴포넌트 일괄 틱 (개별 Update 통합)
            RegisterManager(SceneManager.Instance);
            RegisterManager(InteractionRespawnManager.Instance); // 월드 리스폰 시 소모된 인터랙션 오브젝트 복구
            RegisterManager(MonsterRespawnManager.Instance); // 시간 기반 몬스터 재스폰 (WorldState/ActorSpawn/Scene 이후)
            RegisterManager(WorldLightingManager.Instance);  // 낮밤 조명 (씬 컨텍스트 확정 이후)
#if UNITY_EDITOR
            RegisterManager(DebugGizmoManager.Instance); // 디버그 기즈모는 에디터 전용 — 빌드 제외
#endif
            RegisterManager(CheatManager.Instance);
            RegisterManager(RecipeManager.Instance);
            RegisterManager(QuestManager.Instance);
            RegisterManager(UPlayGround.FlowGraph.FlowGraphManager.Instance); // 게임 흐름 노드 그래프 (Flag/Quest/Story/Dialogue 이후)
            // FlowGraph 진행 기록은 매니저가 아닌 static 저장소라 별도 참여자로 세이브에 등록한다.
            SaveManager.Instance.RegisterSaveable(FlowProgressSaveable.Instance);
            RegisterManager(GameGuideManager.Instance);

            var bootStopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Addressables 자체와 전역 설정은 다른 비동기 매니저보다 먼저 준비한다.
            await InitializeAsyncManager(AssetManager.Instance, cancellationToken);
            await InitializeAsyncManager(SettingsManager.Instance, cancellationToken);

            // 서로의 InitializeAsync 결과를 직접 요구하지 않는 핵심 런타임 시스템은 병렬 로드한다.
            // 가장 오래 걸리는 UI/카메라/각종 DB 로드가 직렬로 누적되지 않게 한다.
            await UniTask.WhenAll(
                InitializeAsyncManager(SoundManager.Instance, cancellationToken),
                InitializeAsyncManager(UIManager.Instance, cancellationToken),
                InitializeAsyncManager(CameraManager.Instance, cancellationToken),
                InitializeAsyncManager(GameObjectManager.Instance, cancellationToken),
                InitializeAsyncManager(PartyManager.Instance, cancellationToken),
                InitializeAsyncManager(ItemManager.Instance, cancellationToken),
                InitializeAsyncManager(DialogueManager.Instance, cancellationToken),
                InitializeAsyncManager(ActorSpawnManager.Instance, cancellationToken),
                InitializeAsyncManager(MonsterCodexManager.Instance, cancellationToken));
            // DebugGizmoManager는 더 이상 비동기 초기화를 하지 않는다.
            // 설정 Addressable은 SetEnabled(true)로 실제 사용이 시작될 때 지연 로드한다.

            // 실제 플레이어 탐색과 CameraManager.SetTarget 연결은 PartyManager.AfterInit에서 일어난다.
            // Recipe/Quest DB가 느려도 첫 카메라 세팅은 기다리지 않도록 핵심 후처리를 먼저 수행한다.
            RunAfterInit(CameraManager.Instance);
            RunAfterInit(GameObjectManager.Instance);
            RunAfterInit(PartyManager.Instance);
            Debug.Log($"[GameManager] 핵심 런타임 준비 완료 ({bootStopwatch.Elapsed.TotalMilliseconds:F1} ms)");

            // 제작/퀘스트는 핵심 런타임과 데이터 매니저가 준비된 뒤 서로 병렬로 로드한다.
            await UniTask.WhenAll(
                InitializeAsyncManager(RecipeManager.Instance, cancellationToken),
                InitializeAsyncManager(QuestManager.Instance, cancellationToken));

            // 비동기 초기화 구현이 새로 추가됐지만 위 단계에 명시되지 않은 매니저가 있으면
            // 누락하지 않고 마지막 안전 단계에서 초기화한다.
            await InitializeRemainingAsyncManagers(cancellationToken);

            RunRemainingAfterInit();
            
            IsInitialized = true;
            BootState = GameBootState.Ready;
            bootStopwatch.Stop();

            Debug.Log(
                $"[GameManager] {_registeredManagers.Count}개의 매니저 초기화 완료 " +
                $"({bootStopwatch.Elapsed.TotalMilliseconds:F1} ms)");
        }

        private async UniTask InitializeRemainingAsyncManagers(CancellationToken cancellationToken)
        {
            foreach (var manager in _registeredManagers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (manager is not IAsyncInitializableManager ||
                    _runtimeReadyManagers.Contains(manager))
                    continue;

                await InitializeAsyncManager(manager, cancellationToken);
            }
        }

        private async UniTask InitializeAsyncManager(
            IManager manager,
            CancellationToken cancellationToken)
        {
            if (manager == null || _runtimeReadyManagers.Contains(manager))
                return;

            if (manager is not IAsyncInitializableManager asyncManager)
            {
                _runtimeReadyManagers.Add(manager);
                return;
            }

            string managerName = manager.GetType().Name;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            Debug.Log($"[GameManager] {managerName} 비동기 초기화 시작");

            try
            {
                await asyncManager.InitializeAsync(cancellationToken);
                _runtimeReadyManagers.Add(manager);
            }
            finally
            {
                stopwatch.Stop();
                float elapsedMilliseconds = (float)stopwatch.Elapsed.TotalMilliseconds;
                _managerInitializationMilliseconds[managerName] = elapsedMilliseconds;
                Debug.Log($"[GameManager] {managerName} 비동기 초기화 종료 ({elapsedMilliseconds:F1} ms)");
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

            MoveUnderManagerRoot(manager);
            _registeredManagers.Add(manager);
            _managerLookup[manager.GetType()] = manager;
            if (manager is IGameService gameService)
                Services.Register(gameService);
            if (manager is IUpdatableManager updatable)
                _updatableManagers.Add(updatable);
            if (manager is IFixedUpdatableManager fixedUpdatable)
                _fixedUpdatableManagers.Add(fixedUpdatable);
            if (manager is ILateUpdatableManager lateUpdatable)
                _lateUpdatableManagers.Add(lateUpdatable);
            manager.Init();
            if (manager is not IAsyncInitializableManager)
                _runtimeReadyManagers.Add(manager);

            Debug.Log($"[GameManager] {manager.GetType().Name} 등록 완료");
        }

        private void MoveUnderManagerRoot(IManager manager)
        {
            if (manager is not Component managerComponent)
                return;

            Transform managerTransform = managerComponent.transform;
            if (managerTransform == transform || managerTransform.parent == transform)
                return;

            managerTransform.SetParent(transform, worldPositionStays: true);
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
                if (manager is IGameService gameService)
                    Services.Unregister(gameService);
                if (manager is IUpdatableManager updatable)
                    _updatableManagers.Remove(updatable);
                if (manager is IFixedUpdatableManager fixedUpdatable)
                    _fixedUpdatableManagers.Remove(fixedUpdatable);
                if (manager is ILateUpdatableManager lateUpdatable)
                    _lateUpdatableManagers.Remove(lateUpdatable);
                _runtimeReadyManagers.Remove(manager);
                _afterInitializedManagers.Remove(manager);
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

        private void RunAfterInit(IManager manager)
        {
            if (manager == null ||
                !_runtimeReadyManagers.Contains(manager) ||
                !_afterInitializedManagers.Add(manager))
                return;

            manager.AfterInit();
        }

        private void RunRemainingAfterInit()
        {
            foreach (var manager in _registeredManagers)
                RunAfterInit(manager);
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

        // [부분 활성 틱 계약]
        // 매니저는 자신의 AfterInit가 끝난 시점부터 OnUpdate/OnFixedUpdate/OnLateUpdate를 받는다.
        // 부트가 단계화되어(Camera/GameObject/Party 등 핵심 매니저 우선 AfterInit) 일부 매니저는
        // 전체 부트 완료(IsInitialized) 전에 먼저 틱이 시작될 수 있다.
        // => 모든 매니저의 OnUpdate 계열은 아직 준비되지 않았을 수 있는 다른 매니저/타깃 참조를
        //    반드시 null 가드해야 한다(예: CameraManager는 _target null 시 즉시 return).
        private void Update()
        {
            for (int i = 0; i < _updatableManagers.Count; i++)
            {
                if (_updatableManagers[i] is IManager manager &&
                    _afterInitializedManagers.Contains(manager))
                    _updatableManagers[i].OnUpdate();
            }
        }

        private void FixedUpdate()
        {
            for (int i = 0; i < _fixedUpdatableManagers.Count; i++)
            {
                if (_fixedUpdatableManagers[i] is IManager manager &&
                    _afterInitializedManagers.Contains(manager))
                    _fixedUpdatableManagers[i].OnFixedUpdate();
            }
        }

        private void LateUpdate()
        {
            for (int i = 0; i < _lateUpdatableManagers.Count; i++)
            {
                if (_lateUpdatableManagers[i] is IManager manager &&
                    _afterInitializedManagers.Contains(manager))
                    _lateUpdatableManagers[i].OnLateUpdate();
            }
        }

#if DEVELOPMENT_BUILD
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void DisableDevelopmentConsoleBeforeSceneLoad()
        {
            DisableDevelopmentConsole();
        }

        // 개발 빌드의 인게임 Development Console 오버레이만 비활성화한다.
        // 로그 자체는 Player.log에 그대로 남는다.
        private static void DisableDevelopmentConsole()
        {
            Debug.developerConsoleEnabled = false;
            Debug.developerConsoleVisible = false;
        }

        // Unity가 에러/예외 로그로 콘솔을 다시 켜려고 할 때 한 번 더 막는다.
        private static void SuppressDevelopmentConsole(string condition, string stackTrace, LogType type)
        {
            DisableDevelopmentConsole();
        }
#endif

        protected override void OnDestroy()
        {
#if DEVELOPMENT_BUILD
            Application.logMessageReceived -= SuppressDevelopmentConsole;
#endif

            BootState = GameBootState.Disposing;
            _initializationCancellation?.Cancel();

            // 모든 매니저 정리
            for (int i = _registeredManagers.Count - 1; i >= 0; i--)
            {
                IManager manager = _registeredManagers[i];
                // Unity는 같은 GameObject의 컴포넌트 OnDestroy 순서를 보장하지 않는다.
                // GameManager보다 먼저 파괴된 MonoBehaviour 매니저에는 Dispose를 호출하지 않는다.
                if (manager is UnityEngine.Object unityObject && unityObject == null)
                    continue;

                manager?.Dispose();
            }

            _registeredManagers.Clear();
            _updatableManagers.Clear();
            _fixedUpdatableManagers.Clear();
            _lateUpdatableManagers.Clear();
            _runtimeReadyManagers.Clear();
            _afterInitializedManagers.Clear();
            _managerLookup.Clear();
            Services.Clear();
            CameraRuntimeServices.Reset();
            _managerInitializationMilliseconds.Clear();
            IsInitialized = false;
            _initializationCancellation?.Dispose();
            _initializationCancellation = null;

            Debug.Log("[GameManager] 정리 완료");

            base.OnDestroy();
        }
    }
}
