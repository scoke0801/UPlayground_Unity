using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UPlayGround.Data.Crafting;
using UPlayGround.Data.Path;
using UPlayGround.Data.Save;

namespace UPlayGround.Manager
{
    public enum CraftAvailabilityReason
    {
        Available,
        DatabaseNotLoaded,
        InvalidRecipe,
        InvalidQuantity,
        InvalidResult,
        Locked,
        AlreadyCrafting,
        NotEnoughCost,
        NotEnoughIngredients,
    }

    /// <summary>
    /// 제작(크래프팅) 시스템 매니저.
    /// GameManager에 등록되어 다른 매니저와 동일한 생명주기로 동작한다.
    ///
    /// 외부 연동 포인트:
    ///   - 몬스터 처치 시    → RecipeManager.Instance.NotifyMonsterKill(actorId)
    ///   - 레시피 직접 언락  → RecipeManager.Instance.UnlockRecipe(recipeID)
    ///   - 제작 시도         → RecipeManager.Instance.TryStartCrafting(recipeID, quantity)
    ///   - 제작 취소         → RecipeManager.Instance.CancelCrafting()
    /// </summary>
    public class RecipeManager : BaseManager<RecipeManager>, IManager, ISaveable, IAsyncInitializableManager,
        IUpdatableManager
    {
        private const string RECIPE_DATABASE_KEY = "RecipeDatabase";

        // ──── 데이터 ────
        private RecipeDatabase _db;

        // ──── 런타임 상태 ────
        private readonly Dictionary<int, bool> _unlocked       = new Dictionary<int, bool>();
        private readonly Dictionary<int, int>  _craftCounts    = new Dictionary<int, int>();
        private readonly Dictionary<int, int>  _monsterKills   = new Dictionary<int, int>();
        private readonly Dictionary<string, int> _monsterKillsByActorId = new Dictionary<string, int>();
        private readonly Dictionary<int, int>  _itemCollectCounts = new Dictionary<int, int>();

        // DB 로드 전에 LoadGame()이 호출될 경우 pending 보관
        private RecipeSaveData _pendingLoad;

        private int   _craftingRecipeID  = -1;
        private int   _craftingQuantity  = 1;
        private float _craftingProgress;          // 0~1
        private float _castTimeRemaining;
        private float _totalCastTime;

        public bool IsDBLoaded { get; private set; } = false;

        // ──── 이벤트 ────
        /// <summary> 레시피가 새로 언락될 때 </summary>
        public event Action<int> OnRecipeUnlocked;

        /// <summary> 제작이 시작될 때 (recipeID) </summary>
        public event Action<int> OnCraftingStarted;

        /// <summary> 제작이 완료될 때 (recipeID, 획득 수량) </summary>
        public event Action<int, int> OnCraftingCompleted;

        /// <summary> 제작이 취소될 때 </summary>
        public event Action OnCraftingCancelled;

        // ──────────────────────────────────────────────────────────
        #region IManager

        public void Init()
        {
            SaveManager.Instance.RegisterSaveable(this);
        }

        public UniTask InitializeAsync(CancellationToken cancellationToken) =>
            LoadDatabaseAsync(cancellationToken);

        public void AfterInit() { }
        public void Dispose()
        {
            _db = null;
            IsDBLoaded = false;
        }

        public void OnUpdate()
        {
            if (_craftingRecipeID != -1)
                TickCrafting(Time.deltaTime);
        }

        public void OnFixedUpdate() { }
        public void OnLateUpdate()  { }

        public void OnSceneChanged(string sceneType) { }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 데이터베이스 로드

        private async UniTask LoadDatabaseAsync(CancellationToken cancellationToken)
        {
            try
            {
                _db = await AssetManager.Instance.LoadGlobalAsync<RecipeDatabase>(
                    RECIPE_DATABASE_KEY,
                    nameof(RecipeManager),
                    cancellationToken);

                _db.Initialize();
                IsDBLoaded = true;

                if (_pendingLoad != null)
                    ApplyPendingLoad();
                else
                    InitUnlockStates();

                Debug.Log("[RecipeManager] RecipeDatabase 로드 완료");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"[RecipeManager] RecipeDatabase 로드 실패: {e.Message}");
                throw;
            }
        }

        private void InitUnlockStates()
        {
            foreach (var id in _db.GetAllRecipeIDs())
                _unlocked[id] = false;

            CheckUnlockConditions();
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 제작 판정

        /// <summary>
        /// recipeID 레시피를 quantity회 제작할 수 있는지 여부.
        /// UI에서 버튼 활성화/비활성화에 사용한다.
        /// </summary>
        public bool CanCraft(int recipeID, int quantity = 1)
        {
            return GetCraftAvailabilityReason(recipeID, quantity) == CraftAvailabilityReason.Available;
        }

        public CraftAvailabilityReason GetCraftAvailabilityReason(int recipeID, int quantity = 1)
        {
            if (!IsDBLoaded) return CraftAvailabilityReason.DatabaseNotLoaded;

            var recipe = _db.GetRecipe(recipeID);
            if (recipe == null) return CraftAvailabilityReason.InvalidRecipe;
            if (quantity <= 0) return CraftAvailabilityReason.InvalidQuantity;
            if (!HasValidResult(recipe)) return CraftAvailabilityReason.InvalidResult;
            if (!IsRecipeUnlocked(recipeID)) return CraftAvailabilityReason.Locked;
            if (IsCrafting()) return CraftAvailabilityReason.AlreadyCrafting;
            if (!HasEnoughCost(recipe, quantity)) return CraftAvailabilityReason.NotEnoughCost;
            if (!HasEnoughIngredients(recipeID, quantity)) return CraftAvailabilityReason.NotEnoughIngredients;

            return CraftAvailabilityReason.Available;
        }

        private bool HasValidResult(RecipeData recipe)
        {
            if (recipe == null) return false;
            if (recipe.resultItemID <= 0) return false;
            if (recipe.resultQuantity <= 0) return false;
            return ItemManager.Instance.GetItemData(recipe.resultItemID) != null;
        }

        private bool HasEnoughCost(RecipeData recipe, int quantity)
        {
            if (recipe.costType == CostType.Free) return true;
            return InventoryManager.Instance.Gold >= recipe.costAmount * quantity;
        }

        private bool HasEnoughIngredients(int recipeID, int quantity)
        {
            foreach (var ingr in _db.GetIngredients(recipeID))
            {
                int needed = ingr.requiredQuantity * quantity;
                if (InventoryManager.Instance.GetItemCount(ingr.ingredientItemID) < needed)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 재료가 부족한 재료 목록을 반환한다 (UI에서 빨간 표시 등에 활용).
        /// </summary>
        public List<int> GetMissingIngredients(int recipeID, int quantity = 1)
        {
            var missing = new List<int>();
            if (!IsDBLoaded) return missing;

            foreach (var ingr in _db.GetIngredients(recipeID))
            {
                int needed = ingr.requiredQuantity * quantity;
                if (InventoryManager.Instance.GetItemCount(ingr.ingredientItemID) < needed)
                    missing.Add(ingr.ingredientItemID);
            }
            return missing;
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 제작 실행

        /// <summary>
        /// 제작을 시작한다. 성공 시 true 반환.
        /// castTimeSeconds 경과 후 OnCraftingCompleted 이벤트 발생.
        /// </summary>
        public bool TryStartCrafting(int recipeID, int quantity = 1)
        {
            if (!CanCraft(recipeID, quantity)) return false;

            if (!DeductResources(recipeID, quantity)) return false;

            var recipe = _db.GetRecipe(recipeID);
            _craftingRecipeID  = recipeID;
            _craftingQuantity  = quantity;
            // 제작 시간은 수량과 무관하게 레시피 기준 시간만 소모한다 (1개든 여러 개든 동일).
            _totalCastTime     = recipe.castTimeSeconds;
            _castTimeRemaining = _totalCastTime;
            _craftingProgress  = 0f;

            OnCraftingStarted?.Invoke(recipeID);
            Debug.Log($"[RecipeManager] 제작 시작: {recipe.recipeName} x{quantity}");
            return true;
        }

        /// <summary>
        /// 현재 진행 중인 제작을 취소한다.
        /// 재료는 환불되지 않는다 (기획 결정).
        /// </summary>
        public void CancelCrafting()
        {
            if (_craftingRecipeID == -1) return;

            _craftingRecipeID  = -1;
            _craftingQuantity  = 1;
            _craftingProgress  = 0f;
            _castTimeRemaining = 0f;
            _totalCastTime     = 0f;

            OnCraftingCancelled?.Invoke();
        }

        private bool DeductResources(int recipeID, int quantity)
        {
            var recipe      = _db.GetRecipe(recipeID);
            var ingredients = _db.GetIngredients(recipeID);

            // 차감 성공한 재료를 기록해 두고, 중간 실패 시 롤백
            var deducted = new List<(int itemID, int amount)>();

            foreach (var ingr in ingredients)
            {
                int toRemove = ingr.requiredQuantity * quantity;
                if (!InventoryManager.Instance.RemoveItem(ingr.ingredientItemID, toRemove))
                {
                    Debug.LogError($"[RecipeManager] 재료 차감 실패 — ItemID: {ingr.ingredientItemID}");
                    foreach (var (itemID, amount) in deducted)
                        InventoryManager.Instance.RestoreItem(itemID, amount);
                    return false;
                }
                deducted.Add((ingr.ingredientItemID, toRemove));
            }

            if (recipe.costType == CostType.Gold)
                InventoryManager.Instance.Gold -= recipe.costAmount * quantity;

            return true;
        }

        private void TickCrafting(float deltaTime)
        {
            _castTimeRemaining -= deltaTime;
            _craftingProgress   = _totalCastTime > 0f
                ? 1f - (_castTimeRemaining / _totalCastTime)
                : 1f;

            if (_castTimeRemaining <= 0f)
                FinishCrafting();
        }

        private void FinishCrafting()
        {
            int recipeID = _craftingRecipeID;
            int quantity = _craftingQuantity;
            var recipe   = _db.GetRecipe(recipeID);

            if (!HasValidResult(recipe))
            {
                Debug.LogError($"[RecipeManager] 제작 결과 아이템이 유효하지 않습니다. RecipeID: {recipeID}");
                CancelCrafting();
                return;
            }

            int totalYield = recipe.resultQuantity * quantity;
            InventoryManager.Instance.AddItem(recipe.resultItemID, totalYield);

            if (!_craftCounts.ContainsKey(recipeID))
                _craftCounts[recipeID] = 0;
            _craftCounts[recipeID] += quantity;

            // 제작 완료로 새로운 레시피가 언락될 수 있음
            CheckUnlockConditions();

            // 완료 이벤트 발행 전에 제작 상태를 먼저 해제한다.
            // (IsCrafting()이 false여야 UI가 버튼 텍스트를 "취소"→"제작"으로 복원한다.)
            _craftingRecipeID  = -1;
            _craftingQuantity  = 1;
            _craftingProgress  = 0f;
            _castTimeRemaining = 0f;
            _totalCastTime     = 0f;

            QuestManager.Instance?.NotifyItemCrafted(recipeID, quantity);
            OnCraftingCompleted?.Invoke(recipeID, totalYield);
            Debug.Log($"[RecipeManager] 제작 완료: {recipe.recipeName} x{totalYield}");
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 언락 시스템

        public bool IsRecipeUnlocked(int recipeID)
        {
            var recipe = _db?.GetRecipe(recipeID);
            if (recipe != null && recipe.isDebugUnlocked) return true;
            return _unlocked.TryGetValue(recipeID, out var v) && v;
        }

        /// <summary> 레시피를 직접 언락한다 (스토리 이벤트, 치트 등). </summary>
        public void UnlockRecipe(int recipeID)
        {
            if (_unlocked.TryGetValue(recipeID, out var current) && current) return;

            _unlocked[recipeID] = true;
            OnRecipeUnlocked?.Invoke(recipeID);
            Debug.Log($"[RecipeManager] 레시피 언락: {_db?.GetRecipe(recipeID)?.recipeName ?? recipeID.ToString()}");
        }

        /// <summary>
        /// 모든 레시피의 언락 조건을 재평가한다.
        /// 몬스터 처치, 아이템 수집 등 외부 이벤트 발생 후 호출하거나,
        /// 자동으로 OnCraftingCompleted 이후에 호출된다.
        /// </summary>
        public void CheckUnlockConditions()
        {
            if (!IsDBLoaded) return;

            foreach (var id in _db.GetAllRecipeIDs())
            {
                if (IsRecipeUnlocked(id)) continue;

                var cond = _db.GetUnlockCondition(id);
                // 조건이 없거나 None이면 즉시 언락
                if (cond == null || EvaluateCondition(cond))
                    UnlockRecipe(id);
            }
        }

        private bool EvaluateCondition(RecipeUnlockCondition cond)
        {
            return cond.conditionType switch
            {
                UnlockConditionType.None =>
                    true,

                UnlockConditionType.MonsterKill =>
                    GetMonsterKillProgress(cond) >= Mathf.Max(1, cond.conditionValue2),

                UnlockConditionType.ItemCollect =>
                    GetItemCollectCount(cond.conditionValue) >= Mathf.Max(1, cond.conditionValue2),

                UnlockConditionType.ItemHave =>
                    InventoryManager.Instance.GetItemCount(cond.conditionValue) >= Mathf.Max(1, cond.conditionValue2),

                UnlockConditionType.RecipeCraft =>
                    _craftCounts.TryGetValue(cond.conditionValue, out var cnt)
                    && cnt >= Mathf.Max(1, cond.conditionValue2),

                _ => false
            };
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 외부 이벤트 수신

        /// <summary>
        /// 몬스터 처치 시 호출. MonsterActor.ActorId 문자열을 기준으로 처치 수를 집계한다.
        /// ActorId가 숫자로 변환되면 레거시 숫자 ID 조건도 함께 갱신한다.
        /// </summary>
        public void NotifyMonsterKill(string actorId)
        {
            if (string.IsNullOrEmpty(actorId)) return;

            if (!_monsterKillsByActorId.ContainsKey(actorId))
                _monsterKillsByActorId[actorId] = 0;
            _monsterKillsByActorId[actorId]++;

            if (int.TryParse(actorId, out int monsterID))
            {
                if (!_monsterKills.ContainsKey(monsterID))
                    _monsterKills[monsterID] = 0;
                _monsterKills[monsterID]++;
            }

            CheckUnlockConditions();
        }

        /// <summary>
        /// 레거시 숫자 ID 기반 몬스터 처치 알림. 신규 코드는 ActorId 오버로드를 사용한다.
        /// </summary>
        public void NotifyMonsterKill(int monsterID)
        {
            NotifyMonsterKill(monsterID.ToString());
        }

        /// <summary>
        /// 아이템을 새로 획득했을 때 호출한다. ItemCollect 조건은 현재 보유량이 아니라 누적 획득량을 기준으로 한다.
        /// </summary>
        public void NotifyItemCollected(int itemID, int count)
        {
            if (itemID <= 0 || count <= 0) return;

            if (!_itemCollectCounts.ContainsKey(itemID))
                _itemCollectCounts[itemID] = 0;
            _itemCollectCounts[itemID] += count;
            CheckUnlockConditions();
        }

        // conditionStringValue(ActorId)가 지정되면 우선하고, 없으면 레거시 숫자 ID로 폴백한다.
        private int GetMonsterKillProgress(RecipeUnlockCondition cond)
        {
            if (!string.IsNullOrEmpty(cond.conditionStringValue))
                return _monsterKillsByActorId.TryGetValue(cond.conditionStringValue, out var s) ? s : 0;

            return _monsterKills.TryGetValue(cond.conditionValue, out var c) ? c : 0;
        }

        private int GetItemCollectCount(int itemID)
        {
            return _itemCollectCounts.TryGetValue(itemID, out var c) ? c : 0;
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 정보 조회 (UI용)

        public RecipeData          GetRecipeData(int recipeID)    => _db?.GetRecipe(recipeID);
        public List<IngredientData> GetIngredients(int recipeID)  => _db?.GetIngredients(recipeID) ?? new List<IngredientData>();
        public float               GetCraftingProgress()          => _craftingProgress;
        public int                 GetCurrentCraftingRecipeID()   => _craftingRecipeID;
        public bool                IsCrafting()                   => _craftingRecipeID != -1;

        public List<int> GetUnlockedRecipeIDs()
        {
            if (!IsDBLoaded) return new List<int>();
            return _db.GetAllRecipeIDs().Where(IsRecipeUnlocked).ToList();
        }

        public int GetCraftingCount(int recipeID)
        {
            return _craftCounts.TryGetValue(recipeID, out var c) ? c : 0;
        }

        /// <summary>
        /// 현재 보유 재료·골드로 제작할 수 있는 최대 수량을 반환한다.
        /// UI의 MAX 버튼에서 사용한다. 언락되지 않았거나 결과물이 유효하지 않으면 0.
        /// </summary>
        public int GetMaxCraftableQuantity(int recipeID)
        {
            if (!IsDBLoaded) return 0;

            var recipe = _db.GetRecipe(recipeID);
            if (recipe == null)             return 0;
            if (!HasValidResult(recipe))     return 0;
            if (!IsRecipeUnlocked(recipeID)) return 0;

            int max = int.MaxValue;

            // 재료별 제약
            foreach (var ingr in _db.GetIngredients(recipeID))
            {
                if (ingr.requiredQuantity <= 0) continue;
                int have = InventoryManager.Instance.GetItemCount(ingr.ingredientItemID);
                max = Mathf.Min(max, have / ingr.requiredQuantity);
            }

            // 골드 제약
            if (recipe.costType != CostType.Free && recipe.costAmount > 0)
                max = Mathf.Min(max, InventoryManager.Instance.Gold / recipe.costAmount);

            // 재료가 하나도 없는 레시피(int.MaxValue 유지)는 0으로 취급
            return max == int.MaxValue ? 0 : Mathf.Max(0, max);
        }

        /// <summary>
        /// 1회 제작에 사용하는 재료가 인벤토리에 충분한지 여부를 재료별로 반환.
        /// key=ingredientItemID, value=충분 여부
        /// </summary>
        public Dictionary<int, bool> GetIngredientAvailability(int recipeID, int quantity = 1)
        {
            var result = new Dictionary<int, bool>();
            if (!IsDBLoaded) return result;

            foreach (var ingr in _db.GetIngredients(recipeID))
            {
                int needed = ingr.requiredQuantity * quantity;
                result[ingr.ingredientItemID] =
                    InventoryManager.Instance.GetItemCount(ingr.ingredientItemID) >= needed;
            }
            return result;
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region ISaveable

        public void ExportSaveData(GameSaveData saveData)
        {
            saveData.recipe.unlockedRecipeIDs = _unlocked
                .Where(kv => kv.Value)
                .Select(kv => kv.Key)
                .ToList();

            saveData.recipe.craftCounts = new Dictionary<int, int>(_craftCounts);
            saveData.recipe.monsterKills = new Dictionary<int, int>(_monsterKills);
            saveData.recipe.monsterKillsByActorId = new Dictionary<string, int>(_monsterKillsByActorId);
            saveData.recipe.itemCollectCounts = new Dictionary<int, int>(_itemCollectCounts);
        }

        public void ImportSaveData(GameSaveData saveData)
        {
            _pendingLoad = saveData.recipe;

            // DB가 이미 로드된 경우 즉시 복원
            if (IsDBLoaded)
                ApplyPendingLoad();
        }

        public void ResetForNewGame()
        {
            _pendingLoad = null;

            // 진행 중이던 제작 취소(신규 실행과 동일한 기본 상태).
            _craftingRecipeID = -1;
            _craftingQuantity = 1;
            _craftingProgress = 0f;
            _castTimeRemaining = 0f;
            _totalCastTime = 0f;

            _unlocked.Clear();
            _craftCounts.Clear();
            _monsterKills.Clear();
            _monsterKillsByActorId.Clear();
            _itemCollectCounts.Clear();

            // DB가 로드돼 있으면 기본 언락 상태로 재시딩(InitUnlockStates = fresh launch 경로).
            if (IsDBLoaded)
                InitUnlockStates();
        }

        private void ApplyPendingLoad()
        {
            var data = _pendingLoad;
            _pendingLoad = null;

            // 전체 초기화 후 저장된 언락 상태 복원
            foreach (var id in _db.GetAllRecipeIDs())
                _unlocked[id] = false;

            foreach (var id in data.unlockedRecipeIDs ?? new List<int>())
            {
                if (_unlocked.ContainsKey(id))
                    _unlocked[id] = true;
                else
                    Debug.LogWarning($"[RecipeManager] 세이브 파일의 레시피 ID {id}가 현재 DB에 존재하지 않습니다. 무시합니다.");
            }

            _craftCounts.Clear();
            foreach (var kv in data.craftCounts ?? new Dictionary<int, int>())
                _craftCounts[kv.Key] = kv.Value;

            _monsterKills.Clear();
            foreach (var kv in data.monsterKills ?? new Dictionary<int, int>())
                _monsterKills[kv.Key] = kv.Value;

            _monsterKillsByActorId.Clear();
            foreach (var kv in data.monsterKillsByActorId ?? new Dictionary<string, int>())
                _monsterKillsByActorId[kv.Key] = kv.Value;

            _itemCollectCounts.Clear();
            foreach (var kv in data.itemCollectCounts ?? new Dictionary<int, int>())
                _itemCollectCounts[kv.Key] = kv.Value;

            CheckUnlockConditions();
        }

        #endregion
    }
}
