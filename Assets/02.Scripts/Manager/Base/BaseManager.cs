using UnityEngine;

/// <summary>
/// MonoBehaviour 기반 싱글톤 베이스 클래스
/// </summary>
public class BaseManager<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static readonly object _lock = new object();
    private static bool _applicationIsQuitting = false;

    /// <summary>
    /// 싱글톤 인스턴스
    /// </summary>
    public static T Instance
    {
        get
        {
            if (_applicationIsQuitting)
            {
                Debug.LogWarning($"[{typeof(T)}] 애플리케이션 종료 중에는 인스턴스에 접근할 수 없습니다.");
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

    /// <summary>
    /// 씬 전환 시에도 오브젝트를 유지할지 여부 (기본값: true)
    /// </summary>
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
        _applicationIsQuitting = true;
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}