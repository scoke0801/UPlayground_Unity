using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UPlayGround.Manager
{
    /// <summary>
    /// 모든 매니저 싱글톤이 공유하는 종료 플래그.
    /// 앱 종료(OnApplicationQuit) 시 싱글톤이 되살아나지 않도록 막는 용도.
    ///
    /// 제네릭 <see cref="BaseManager{T}"/>의 static 필드는 타입별로 분리되고,
    /// Enter Play Mode Options(도메인 리로드 비활성화) 환경에서는 플레이 세션 간
    /// static 값이 초기화되지 않는다. 종료 플래그를 제네릭 안에 두면 한 번 true로
    /// 굳었을 때 다음 플레이에서 Instance가 영영 null을 반환하는 버그가 생긴다.
    /// (BootLoader가 GameManager.Instance를 null로 받아 NRE 발생)
    ///
    /// 비제네릭 클래스에 모아두고 SubsystemRegistration 시점에 리셋하면
    /// 도메인 리로드 비활성화 여부와 무관하게 매 플레이 진입 시(모든 Awake 이전)
    /// 플래그가 항상 false로 보장된다.
    /// </summary>
    internal static class ManagerLifecycle
    {
        public static bool ApplicationIsQuitting = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnEnterPlayMode()
        {
            ApplicationIsQuitting = false;
        }
    }

    /// <summary>
    /// MonoBehaviour 기반 싱글톤 베이스 클래스
    /// </summary>
    public class BaseManager<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object _lock = new object();

        public static T Instance
        {
            get
            {
                if (ManagerLifecycle.ApplicationIsQuitting)
                {
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindFirstObjectByType<T>();

                        if (_instance == null)
                        {
                            GameObject singletonObject = new GameObject($"{typeof(T).Name} (Singleton)");
                            _instance = singletonObject.AddComponent<T>();
                            DontDestroyOnLoad(singletonObject);
                        }
                    }

                    return _instance;
                }
            }
        }

        [SerializeField] protected bool dontDestroyOnLoad = true;

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;

                if (dontDestroyOnLoad)
                {
                    DontDestroyOnLoad(gameObject);
                }
            }
            else if (_instance != this)
            {
                Debug.LogWarning($"[{typeof(T)}] 중복된 인스턴스가 존재하여 제거합니다: {gameObject.name}");
                Destroy(gameObject);
            }
        }

        protected virtual void OnApplicationQuit()
        {
            // 실제 앱/플레이 종료 시점. 종료 중 Instance 접근으로 싱글톤이 되살아나는 것을 막는다.
            // 다음 플레이 진입 시에는 ManagerLifecycle.ResetOnEnterPlayMode()가 다시 false로 되돌린다.
            ManagerLifecycle.ApplicationIsQuitting = true;
        }

        protected virtual void OnDestroy()
        {
            // _instance는 파괴된 UnityObject가 == null로 평가되어 Awake/getter에서 자동 복구되므로
            // 별도 리셋이 필수는 아니지만, 즉시 새 인스턴스 탐색이 가능하도록 명시적으로 비운다.
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}