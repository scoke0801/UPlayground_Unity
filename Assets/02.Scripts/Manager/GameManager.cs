using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임의 모든 매니저를 총괄하는 최상위 매니저
/// - 다른 매니저들의 초기화 순서 관리
/// - Unity 라이프사이클 이벤트를 각 매니저에 전파
/// </summary>
public class GameManager : BaseManager<GameManager>
{
    private List<IManager> _managers = new List<IManager>();
    private bool _isInitialized = false;

    protected override void Awake()
    {
        base.Awake();

        // 중복 인스턴스가 아닐 때만 초기화
        if (this == Instance)
        {
            InitializeManagers();
        }
    }

    /// <summary>
    /// 모든 매니저 초기화
    /// </summary>
    private void InitializeManagers()
    {
        if (_isInitialized)
            return;

        Debug.Log("[GameManager] 매니저 초기화 시작");

        // TODO: 여기에 사용할 매니저들을 추가
        // 예시:
        // RegisterManager(UIManager.Instance);
        // RegisterManager(SoundManager.Instance);
        // RegisterManager(ResourceManager.Instance);

        _isInitialized = true;
        Debug.Log($"[GameManager] 총 {_managers.Count}개의 매니저 초기화 완료");
    }

    /// <summary>
    /// 매니저 등록
    /// </summary>
    public void RegisterManager(IManager manager)
    {
        if (manager == null)
        {
            Debug.LogWarning("[GameManager] null 매니저는 등록할 수 없습니다.");
            return;
        }

        if (_managers.Contains(manager))
        {
            Debug.LogWarning($"[GameManager] 이미 등록된 매니저입니다: {manager.GetType().Name}");
            return;
        }

        _managers.Add(manager);
        manager.Init();
        Debug.Log($"[GameManager] 매니저 등록: {manager.GetType().Name}");
    }

    /// <summary>
    /// 매니저 등록 해제
    /// </summary>
    public void UnregisterManager(IManager manager)
    {
        if (manager == null)
            return;

        if (_managers.Remove(manager))
        {
            manager.Dispose();
            Debug.Log($"[GameManager] 매니저 등록 해제: {manager.GetType().Name}");
        }
    }

    private void Update()
    {
        if (!_isInitialized)
            return;

        // 모든 매니저에게 Update 전파
        for (int i = 0; i < _managers.Count; i++)
        {
            _managers[i]?.OnUpdate();
        }
    }

    private void FixedUpdate()
    {
        if (!_isInitialized)
            return;

        // 모든 매니저에게 FixedUpdate 전파
        for (int i = 0; i < _managers.Count; i++)
        {
            _managers[i]?.OnFixedUpdate();
        }
    }

    private void LateUpdate()
    {
        if (!_isInitialized)
            return;

        // 모든 매니저에게 LateUpdate 전파
        for (int i = 0; i < _managers.Count; i++)
        {
            _managers[i]?.OnLateUpdate();
        }
    }

    protected override void OnDestroy()
    {
        // 모든 매니저 정리
        for (int i = _managers.Count - 1; i >= 0; i--)
        {
            _managers[i]?.Dispose();
        }

        _managers.Clear();
        _isInitialized = false;

        base.OnDestroy();
    }

    /// <summary>
    /// 특정 타입의 매니저 가져오기
    /// </summary>
    public T GetManager<T>() where T : class, IManager
    {
        for (int i = 0; i < _managers.Count; i++)
        {
            if (_managers[i] is T manager)
                return manager;
        }

        return null;
    }
}
