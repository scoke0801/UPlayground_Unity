using UPlayGround.Data.Save;

namespace UPlayGround.Manager
{
    /// <summary>
    /// 게임 세이브/로드에 참여하는 매니저가 구현하는 인터페이스.
    /// ExportSaveData: saveData에 자신의 상태를 기록한다.
    /// ImportSaveData: saveData에서 자신의 상태를 복원한다.
    ///                 DB 로드가 비동기인 매니저는 pending 데이터를 보관 후
    ///                 DB 준비 완료 시점에 실제 복원한다.
    /// </summary>
    public interface ISaveable
    {
        void ExportSaveData(GameSaveData saveData);
        void ImportSaveData(GameSaveData saveData);
    }
}
