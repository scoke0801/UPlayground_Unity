using UnityEngine;
using UPlayGround.Data.Pet;

namespace UPlayGround.Manager
{
    public sealed partial class PetManager
    {
        private bool _isUpgrading;
        private int _revision;
        private int _exportedRevision;
        private int _lastUpgradeFrame = -1;
        public bool HasUnsavedChanges { get; private set; }
        public bool IsAutoPickupEnabled => _save.isAutoPickupEnabled;
        public float PickupInterval => GetExplorationValue(PetSkillCatalogSO.Pickup);
        public float PickupRadius => GetExplorationValue(PetSkillCatalogSO.PickupRange);
        public float LootLeash => PickupRadius + (Catalog?.skills?.lootLeashMargin ?? 0f);

        public int GetSkillLevel(string skillId)
        {
            for (int i = 0; i < _save.skills.Count; i++)
                if (_save.skills[i].skillId == skillId) return _save.skills[i].level;
            return 1;
        }
        private float GetExplorationValue(string id) => Catalog?.skills?.Find(id)?.GetLevel(GetSkillLevel(id))?.explorationValue ?? 0f;

        public bool CanEdit(out string reason)
        {
            if (!_ready || Catalog?.skills == null)
            { reason = "지금은 바꿀 수 없어요."; return false; }
            var player = ActorSvc.Objects?.Player;
            reason = null;
            if (_isUpgrading || SaveManager.Instance.IsPreparingSceneLoad
                || SceneManager.Instance.IsLoading || player == null || !player.IsAlive())
                reason = "지금은 바꿀 수 없어요.";
            else if (player.IsInCombat) reason = "싸움이 끝나면 바꿀 수 있어요.";
            else if (player.ActorController is UPlayGround.MovementController.PlayerMovementController movement
                && (movement.IsSwimming || movement.IsTraversing))
                reason = "땅에 내려온 뒤 바꿀 수 있어요.";
            return reason == null;
        }
        public bool TrySetModel(string modelId)
        {
            if (!CanEdit(out _)) return false;
            var definition = Catalog.FindModel(modelId);
            var owned = FindOwned(definition?.petId);
            if (definition?.actor?.prefab == null || definition.previewPrefab == null || owned == null) return false;
            if (_save.modelId == modelId) return true;
            if (!TryReplaceActor(definition, owned.appearanceId)) return false;
            _save.selectedPetId = definition.petId; _save.modelId = modelId;
            MarkChanged(); return true;
        }

        private bool TryReplaceActor(PetDefinitionSO definition, string appearanceId)
        {
            if (_actor == null) return true;
            var previous = _actor;
            var position = previous.transform.position;
            var rotation = previous.transform.rotation;
            previous.SetSuspended(true);
            PetActor replacement = null;
            try
            {
                var spawned = ActorSpawnManager.Instance.SpawnActor(definition.actor.actorId, position, rotation);
                replacement = spawned as PetActor;
                if (replacement == null)
                { if (spawned != null) Destroy(spawned.gameObject); return false; }
                replacement.Configure(definition, _owner, appearanceId);
                replacement.PreserveGroundPose(position, rotation);
                replacement.SetSuspended(true);
                AgentTickManager.Instance.Unregister(previous, previous);
                previous.CancelActions();
                _actor = replacement;
                AgentTickManager.Instance.Register(replacement, replacement);
                Destroy(previous.gameObject);
                return true;
            }
            catch (System.Exception exception)
            {
                if (replacement != null) Destroy(replacement.gameObject);
                _actor = previous;
                Debug.LogError("펫 모델을 교체하지 못했습니다: " + exception.Message);
                return false;
            }
        }
        public bool TrySetSupport(string skillId)
        {
            if (!CanEdit(out _) || Catalog.skills.Find(skillId)?.supportAbility == null) return false;
            if (_save.SelectedSupportId == skillId) return true;
            ClearSupportEffects();
            if (_save.supportSlots.Count == 0) _save.supportSlots.Add(skillId);
            else _save.supportSlots[0] = skillId;
            _save.selectedSupportId = skillId; MarkChanged(); return true;
        }
        public bool TrySetAutoPickup(bool enabled)
        {
            if (!CanEdit(out _)) return false;
            _save.isAutoPickupEnabled = enabled; MarkChanged(); return true;
        }
        public PetUpgradeResult TryUpgrade(PetUpgradeRequest request)
        {
            if (!CanEdit(out _) || FindOwned(_save.selectedPetId) == null || _lastUpgradeFrame == Time.frameCount)
                return PetUpgradeResult.Rejected;
            var progress = _save.skills.Find(value => value.skillId == request.SkillId);
            if (progress == null || !PetUpgradeRules.CanUpgrade(Catalog.skills.Find(request.SkillId), progress.level, request))
                return PetUpgradeResult.Rejected;
            bool committed;
            _lastUpgradeFrame = Time.frameCount;
            _isUpgrading = true;
            try
            {
                committed = Svc.Inventory?.TrySpendGold(request.ExpectedCost, () =>
                { progress.level++; _revision++; HasUnsavedChanges = true; return true; }) == true;
            }
            finally { _isUpgrading = false; }
            if (!committed) return PetUpgradeResult.Rejected;
            bool saved = RetrySave(); Changed?.Invoke();
            return saved ? PetUpgradeResult.CommittedAndSaved : PetUpgradeResult.CommittedUnsaved;
        }
        public bool RetrySave()
        {
            if (_isUpgrading) return false;
            bool saved = SaveManager.Instance.TrySaveActiveSlot();
            HasUnsavedChanges = !saved; Changed?.Invoke(); return saved;
        }
        public void OnSaveCommitted()
        {
            if (_revision == _exportedRevision) HasUnsavedChanges = false;
            Changed?.Invoke();
        }
        private void MarkChanged() { _revision++; HasUnsavedChanges = true; Changed?.Invoke(); }
    }
}
