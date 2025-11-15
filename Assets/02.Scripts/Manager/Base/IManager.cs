/// <summary>
/// 모든 매니저가 구현해야 하는 인터페이스
/// </summary>
public interface IManager
{
    /// <summary>
    /// 매니저 초기화 (최초 1회 실행)
    /// </summary>
    void Init();
    
    /// <summary>
    /// 매니저 정리 (씬 전환 또는 종료 시 실행)
    /// </summary>
    void Dispose();
    
    /// <summary>
    /// 매 프레임 실행 (Update)
    /// </summary>
    void OnUpdate();
    
    /// <summary>
    /// 고정 시간 간격 실행 (FixedUpdate)
    /// </summary>
    void OnFixedUpdate();
    
    /// <summary>
    /// Update 이후 실행 (LateUpdate)
    /// </summary>
    void OnLateUpdate();
}
