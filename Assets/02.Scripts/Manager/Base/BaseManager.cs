using UnityEngine;

/// <summary>
/// MonoBehaviour 기반 싱글톤 베이스 클래스
/// </summary>
public class BaseManager<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static readonly object _lock = new object();
    private static bool _applicationIsQuitting = false;

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
                    _instance = FindObjectOfType<T>();

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
        _applicationIsQuitting = true;
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
            
            #if UNITY_EDITOR
            // 에디터에서 Play 모드 종료 시 정리
            _applicationIsQuitting = false;
            #endif
        }
    }

    #if UNITY_EDITOR
    /// <summary>
    /// 에디터 전용: Play 모드 변경 감지
    /// </summary>
    [UnityEditor.InitializeOnLoadMethod]
    private static void OnEditorLoad()
    {
        UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(UnityEditor.PlayModeStateChange state)
    {
        if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
        {
            // Play 모드 종료 시 정적 변수 초기화
            _instance = null;
            _applicationIsQuitting = false;
            Debug.Log($"[{typeof(T)}] 에디터 Play 모드 종료 - 정적 변수 초기화");
        }
    }
    #endif
}