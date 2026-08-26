using System;
using UnityEngine;
using UPlayGround.Data.Reward;

namespace UPlayGround.Manager
{
    /// <summary>골드·경험치·아이템 보상을 사전 검증한 뒤 한 경로에서 지급한다.</summary>
    public sealed class RewardManager : BaseManager<RewardManager>, IManager, IRewardService
    {
        public event Action<RewardGrantReceipt> OnRewardGranted;

        public void Init()
        {
        }

        public void AfterInit()
        {
        }

        public void Dispose()
        {
            OnRewardGranted = null;
        }

        public void OnUpdate()
        {
        }

        public void OnFixedUpdate()
        {
        }

        public void OnLateUpdate()
        {
        }

        public void OnSceneChanged(string sceneType)
        {
        }

        /// <summary>보상 데이터와 현재 서비스 상태를 변경 없이 검사한다.</summary>
        public RewardGrantResult CanGrant(RewardData reward, RewardGrantTarget target)
        {
            if (reward == null || reward.Validate() != RewardDataValidationResult.Valid)
                return RewardGrantResult.InvalidData;

            IInventoryService inventory = Svc.Inventory;
            IItemService itemService = Svc.Item;
            IPartyService party = Svc.Party;

            RewardGrantResult currencyResult = ValidateCurrency(reward.gold, inventory);
            if (currencyResult != RewardGrantResult.Success)
                return currencyResult;

            RewardGrantResult experienceResult = ValidateExperience(reward.exp, target, party);
            if (experienceResult != RewardGrantResult.Success)
                return experienceResult;

            return ValidateItems(reward, inventory, itemService);
        }

        /// <summary>검증을 통과한 보상을 지급하고 성공 영수증 이벤트를 발행한다.</summary>
        public RewardGrantResult TryGrant(RewardData reward, RewardGrantTarget target)
        {
            RewardGrantResult validation = CanGrant(reward, target);
            if (validation != RewardGrantResult.Success)
                return validation;

            IInventoryService inventory = Svc.Inventory;
            IPartyService party = Svc.Party;

            int itemCount = reward.items?.Count ?? 0;
            for (int i = 0; i < itemCount; i++)
            {
                ItemRewardData item = reward.items[i];
                if (!inventory.TryAddItem(item.itemId, item.count))
                    return RewardGrantResult.ApplyFailed;
            }

            if (reward.gold > 0 && !inventory.TryAddGold(reward.gold))
                return RewardGrantResult.ApplyFailed;

            if (reward.exp > 0)
            {
                if (target.ExperienceRecipient == RewardExperienceRecipient.Character)
                    party.AddExp(target.CharacterType, reward.exp);
                else
                    party.AwardBattleExp(reward.exp);
            }

            PublishRewardGranted(new RewardGrantReceipt(reward, target));
            return RewardGrantResult.Success;
        }

        private void PublishRewardGranted(RewardGrantReceipt receipt)
        {
            Delegate[] subscribers = OnRewardGranted?.GetInvocationList();
            if (subscribers == null)
                return;

            for (int i = 0; i < subscribers.Length; i++)
            {
                try
                {
                    ((Action<RewardGrantReceipt>)subscribers[i]).Invoke(receipt);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        private static RewardGrantResult ValidateCurrency(
            int gold,
            IInventoryService inventory)
        {
            if (gold <= 0)
                return RewardGrantResult.Success;
            if (inventory == null)
                return RewardGrantResult.ServiceUnavailable;
            return inventory.Gold <= int.MaxValue - gold
                ? RewardGrantResult.Success
                : RewardGrantResult.CapacityExceeded;
        }

        private static RewardGrantResult ValidateExperience(
            long experience,
            RewardGrantTarget target,
            IPartyService party)
        {
            if (experience <= 0)
                return RewardGrantResult.Success;
            if (party == null)
                return RewardGrantResult.ServiceUnavailable;
            if (target.ExperienceRecipient != RewardExperienceRecipient.Character)
                return RewardGrantResult.Success;
            if (target.CharacterType == global::UPlayGround.Data.EnumType.CharacterActorType.None
                || !party.IsCharacterUnlocked(target.CharacterType))
            {
                return RewardGrantResult.InvalidRecipient;
            }

            return party.IsMaxLevel(target.CharacterType)
                ? RewardGrantResult.RecipientCannotGainExperience
                : RewardGrantResult.Success;
        }

        private static RewardGrantResult ValidateItems(
            RewardData reward,
            IInventoryService inventory,
            IItemService itemService)
        {
            if (reward.items == null || reward.items.Count == 0)
                return RewardGrantResult.Success;
            if (inventory == null || itemService == null || !itemService.IsItemDBLoaded)
                return RewardGrantResult.ServiceUnavailable;

            for (int i = 0; i < reward.items.Count; i++)
            {
                ItemRewardData item = reward.items[i];
                if (itemService.GetItemData(item.itemId) == null)
                    return RewardGrantResult.InvalidItem;
                if (!inventory.CanAddItem(item.itemId, item.count))
                    return RewardGrantResult.CapacityExceeded;
            }

            return RewardGrantResult.Success;
        }
    }
}
