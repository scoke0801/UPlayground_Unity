using System;
using UPlayGround.Data.Cycle;

namespace UPlayGround.Manager
{
    /// <summary>
    /// 월드 배치 구현 단위(02)가 제공할 사이클 시작 경계.
    /// 배치 계산과 실제 생성을 모두 성공한 뒤 중앙 보스 spawnId를 반환한다.
    /// </summary>
    public interface ICycleWorldSpawnService
    {
        bool TryBuildAndSpawn(
            CycleRunState run,
            Func<CycleRandomStream, Random> randomFactory,
            out CycleLayoutState layout,
            out string error);

        bool TryRestore(CycleRunState run, CycleLayoutState layout, out string error);
        void CleanupRunObjects();
        void OnSceneChanged(string sceneType);
    }

    /// <summary>정산 구현 단위(06)가 제공할 원자적 정산 경계.</summary>
    public interface ICycleSettlementService
    {
        bool TrySettle(CycleRunState run, out string error);
        void AbortRun();
    }
}
