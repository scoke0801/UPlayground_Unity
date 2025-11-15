using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 모든 매니저를 관리하는 최상위 매니저
/// </summary>
public class GameManager : BaseManager<GameManager>
{
    // 등록된 매니저 리스트
    private List<IManager> _registeredManagers = new List<IManager>();
    
    // 초기화 플래그
    private bool _isInitialized = false;
    
    protected override void Awake()
    {
        base.Awake();
        
        // BaseManager의 Awake가 실행된 후, 이 인스턴스가 유효하면 초기화
        if (this != null && !_isInitialized)
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

        // 초기화 순서대로 등록
        RegisterManager(InputManager.Instance);        // 입력 시스템
        // RegisterManager(ResourceManager.Instance);  // 리소스 관리
        // RegisterManager(SoundManager.Instance);     // 사운드 관리
        RegisterManager(UIManager.Instance);           // UI 관리

        _isInitialized = true;
        
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
    
    private void Update()
    {
        if (!_isInitialized) return;
        
        // 모든 매니저의 OnUpdate 호출
        foreach (var manager in _registeredManagers)
        {
            manager?.OnUpdate();
        }
    }
    
    private void FixedUpdate()
    {
        if (!_isInitialized) return;
        
        // 모든 매니저의 OnFixedUpdate 호출
        foreach (var manager in _registeredManagers)
        {
            manager?.OnFixedUpdate();
        }
    }
    
    private void LateUpdate()
    {
        if (!_isInitialized) return;
        
        // 모든 매니저의 OnLateUpdate 호출
        foreach (var manager in _registeredManagers)
        {
            manager?.OnLateUpdate();
        }
    }
    
    protected override void OnDestroy()
    {
        // 모든 매니저 정리
        for (int i = _registeredManagers.Count - 1; i >= 0; i--)
        {
            _registeredManagers[i]?.Dispose();
        }
        
        _registeredManagers.Clear();
        _isInitialized = false;
        
        Debug.Log("[GameManager] 정리 완료");
        
        base.OnDestroy();
    }
}