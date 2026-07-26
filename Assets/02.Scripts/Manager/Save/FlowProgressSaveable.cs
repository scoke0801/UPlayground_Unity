using UPlayGround.Data.Save;
using UPlayGround.FlowGraph;

namespace UPlayGround.Manager
{
    /// <summary>
    /// FlowGraph 진행 기록(<see cref="FlowProgressState"/>)을 세이브 시스템에 잇는 어댑터.
    ///
    /// FlowGraphManager는 UPlayGround.FlowGraph asmdef에 있어 ISaveable(Assembly-CSharp)을
    /// 직접 구현할 수 없다. 진행 기록 자체도 러너·씬 수명과 무관한 static 저장소이므로,
    /// 매니저가 아닌 얇은 참여자를 SaveManager에 등록한다.
    /// </summary>
    public sealed class FlowProgressSaveable : ISaveable
    {
        public static readonly FlowProgressSaveable Instance = new FlowProgressSaveable();

        private FlowProgressSaveable() { }

        public void ExportSaveData(GameSaveData saveData)
        {
            saveData.flow = FlowProgressState.Export();
        }

        public void ImportSaveData(GameSaveData saveData)
        {
            // 구버전 세이브에는 flow 항목이 없다 — null이면 빈 기록으로 복원한다.
            FlowProgressState.Import(saveData.flow);
        }

        public void ResetForNewGame()
        {
            FlowProgressState.ResetAll();
        }
    }
}
