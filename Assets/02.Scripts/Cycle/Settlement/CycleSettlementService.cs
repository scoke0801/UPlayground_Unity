using System;
using System.Collections.Generic;
using UPlayGround.Data.Cycle;
using UPlayGround.Manager;

namespace UPlayGround.Cycle
{
    public sealed class CycleSettlementService : ICycleSettlementService
    {
        private readonly CycleRunManager _runManager;
        public CycleSettlementService(CycleRunManager runManager) => _runManager = runManager;

        public bool TrySettle(CycleRunState run, out string error)
        {
            error = null;
            CycleRemainsManager remains = CycleRemainsManager.Instance;
            BossAssistManager assists = BossAssistManager.Instance;
            if (remains == null || InventoryManager.Instance == null) { error = "정산 필수 서비스가 준비되지 않았습니다."; return false; }
            if (!string.IsNullOrEmpty(assists?.Roster.PendingRecruitAssistId)) { error = "보류 중인 어시스트 로스터 결정을 먼저 완료해야 합니다."; return false; }

            CycleHistorySaveData history = _runManager.History;
            string settlementId = $"{run.seed}:{run.cycleIndex}:{history.completionSequence + 1}";
            if (history.lastSettlementId == settlementId) { error = "이미 적용된 정산입니다."; return false; }
            CycleSettlementPlan plan = new()
            {
                settlementId = settlementId,
                materialRewards = remains.Ledger.Snapshot(),
                completedCycleIndex = run.cycleIndex,
                discardRemains = remains.Current != null,
            };
            foreach (CycleItemStack item in plan.materialRewards)
            {
                if (item == null || item.itemId <= 0 || item.count <= 0) { error = "정산 재료에 잘못된 항목이 있습니다."; return false; }
            }

            Dictionary<int, int> inventoryCountsBefore = new();
            foreach (CycleItemStack item in plan.materialRewards)
            {
                if (ItemManager.Instance?.GetItemData(item.itemId) == null)
                { error = $"정산 재료 Item ID를 찾을 수 없습니다: {item.itemId}"; return false; }
                inventoryCountsBefore[item.itemId] = InventoryManager.Instance.GetItemCount(item.itemId);
            }
            int sequenceBefore = history.completionSequence;
            int completedCountBefore = history.completedCycleCount;
            string settlementIdBefore = history.lastSettlementId;
            List<CycleItemStack> ledgerBefore = remains.Ledger.Snapshot();
            RemainsState remainsBefore = remains.Current?.Clone();
            try
            {
                foreach (CycleItemStack item in plan.materialRewards)
                {
                    InventoryManager.Instance.AddItem(item.itemId, item.count);
                }
                history.completionSequence++;
                history.completedCycleCount++;
                history.lastSettlementId = settlementId;
                remains.Ledger.Clear();
                if (plan.discardRemains) remains.DiscardRemains();
                _runManager.NotifySettlementCommitted(plan);
                return true;
            }
            catch (Exception exception)
            {
                try
                {
                    foreach ((int itemId, int countBefore) in inventoryCountsBefore)
                    {
                        try
                        {
                            int added = InventoryManager.Instance.GetItemCount(itemId) - countBefore;
                            if (added > 0) InventoryManager.Instance.RemoveItem(itemId, added);
                        }
                        catch (Exception rollbackException) { UnityEngine.Debug.LogException(rollbackException); }
                    }
                }
                finally
                {
                    history.completionSequence = sequenceBefore;
                    history.completedCycleCount = completedCountBefore;
                    history.lastSettlementId = settlementIdBefore;
                    remains.RestoreTransactionState(ledgerBefore, remainsBefore);
                }
                error = exception.Message;
                return false;
            }
        }

        public void AbortRun()
        {
            CycleRemainsManager.Instance?.Ledger.Clear();
            CycleRemainsManager.Instance?.DiscardRemains();
        }
    }
}
