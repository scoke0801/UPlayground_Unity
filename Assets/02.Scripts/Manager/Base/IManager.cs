/// <summary>
/// 모든 매니저가 구현해야 하는 인터페이스
/// </summary>
public interface IManager
{
    /// <summary>
    /// 매니저 초기화 (GameManager에서 호출)
    /// </summary>
    void Init();

    /// <summary>
    /// 매니저 정리 (씬 전환 또는 게임 종료 시)
    /// </summary>
    void Dispose();

    /// <summary>
    /// 매니저 업데이트 (매 프레임)
    /// </summary>
    void OnUpdate();

    /// <summary>
    /// 물리 업데이트 (고정 프레임)
    /// </summary>
    void OnFixedUpdate();

    /// <summary>
    /// 후처리 업데이트 (카메라 이후)
    /// </summary>
    void OnLateUpdate();
}
