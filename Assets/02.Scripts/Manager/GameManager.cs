using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using UPlayGround.Story;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UPlayGround.Dialogue;
using UPlayGround.Manager.Handler;

namespace UPlayGround.Manager
{
    /// <summary>
    /// 모든 매니저를 관리하는 최상위 매니저
    /// </summary>
    public class GameManager : BaseManager<GameManager>
    {
        // 등록된 매니저 리스트
        private List<IManager> _registeredManagers = new List<IManager>();

        // 초기화 플래그
        public bool IsInitialized { get; private set; } = false;

        protected override void Awake()
        {
            base.Awake();

            Application.targetFrameRate = 60;

            // BaseManager의 Awake가 실행된 후, 이 인스턴스가 유효하면 초기화
            if (this != null && !IsInitialized)
            {
                InitializeManagers();
            }
        }

        /// <summary>
        /// 모든 매니저 초기화
        /// </summary>
        private void InitializeManagers()
        {
            if (IsInitialized)
                return;

            Debug.Log("[GameManager] 매니저 초기화 시작");

            // 초기화 순서대로 등록
            RegisterManager(InputManager.Instance); // 입력 시스템
            
            RegisterManager(AssetManager.Instance);
            RegisterManager(UIManager.Instance); // UI 관리
            RegisterManager(CameraManager.Instance); // 카메라 시스템
            RegisterManager(GameObjectManager.Instance);
            RegisterManager(ItemManager.Instance);
            RegisterManager(InventoryManager.Instance);
            RegisterManager(EventManager.Instance);
            RegisterManager(GameHitStopManager.Instance);
            RegisterManager(VitalOrbManager.Instance);
            RegisterManager(GlobalFlagManager.Instance);
            RegisterManager(DialogueManager.Instance);
            RegisterManager(StoryManager.Instance);
            RegisterManager(GameTimeManager.Instance);
            
            RegisterManager(SceneManager.Instance);

            // Init이후에 후처리 필요한 경우 
            AfterInit();
            
            IsInitialized = true;

            Debug.Log($"[GameManager] {_registeredManagers.Count}개의 매니저 초기화 완료");
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
                Debug.Log($"[GameManager] {manager.GetType().Name} 등록 해제");
            }
        }

        /// <summary>
        /// 특정 타입의 매니저 가져오기
        /// </summary>
        public T GetManager<T>() where T : class, IManager
        {
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

            foreach (var manager in _registeredManagers)
                manager?.OnUpdate();
        }

        private void FixedUpdate()
        {
            if (!IsInitialized) return;

            foreach (var manager in _registeredManagers)
                manager?.OnFixedUpdate();
        }

        private void LateUpdate()
        {
            if (!IsInitialized) return;

            foreach (var manager in _registeredManagers)
                manager?.OnLateUpdate();
        }

        protected override void OnDestroy()
        {
            // 모든 매니저 정리
            for (int i = _registeredManagers.Count - 1; i >= 0; i--)
            {
                _registeredManagers[i]?.Dispose();
            }

            _registeredManagers.Clear();
            IsInitialized = false;

            Debug.Log("[GameManager] 정리 완료");

            base.OnDestroy();
        }


        #region Util

        public static async Task<T> LoadAddressableAsync<T>(string addressableKey)
        {
            var handle = Addressables.LoadAssetAsync<T>(addressableKey);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                return handle.Result;
            }
            else
            {
                Debug.LogError($"Failed to load Addressable: {addressableKey}, Status: {handle.Status}");
                return default(T);
            }
        }

        #endregion
    }
}