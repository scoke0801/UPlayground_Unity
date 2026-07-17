using System;
using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Item;
using UPlayGround.Data.Path;
using UPlayGround.UI;

namespace UPlayGround.Manager
{
    /// <summary>
    /// 액터가 월드 오브젝트 시스템에 요청하는 기능만 노출한다.
    /// </summary>
    public interface IActorObjectService : IGameService
    {
        PlayerActor Player { get; }
        IActorInteractionService InteractionHandler { get; }

        bool CanInteract();
        void RegisterActor(GameActor actor);
        void UnregisterActor(GameActor actor);
        void RegisterFXInstance(GameObject instance, float lifeTime);
        GameObject ShowFX(
            FXKeyType key,
            Vector3 position,
            Quaternion rotation = default,
            Transform parent = null,
            float duration = 5f);
        GameObject ShowFX(
            string key,
            Vector3 position,
            Quaternion rotation = default,
            Transform parent = null,
            float duration = 5f);
        GameObject CreateWeapon(int itemKey);
        void SpawnItem(ItemInstance itemInstance, Vector3 position);
    }

    public interface IActorInteractionService
    {
        IInteractable CurrentClosestInteractable { get; }
        bool IsInteractionProgressActive { get; }
        float InteractionProgress { get; }

        void StartInteraction();
        void StopInteraction();
        void SetWaitEvent(Action callback);
    }

    public interface IActorHpBarView
    {
        void UpdateHealth(float current, float max);
        void UpdatePoise(float current, float max);
        void UpdateBreakGauge(float current, float max);
        void SetBreakGaugeEmptyUiActive(bool active);
        void Release();
    }

    public interface IActorDangerRingView
    {
        void CompleteNow();
        void Release();
    }

    public interface IActorBreakInteractionView
    {
        void Release();
    }

    /// <summary>
    /// 액터가 화면에 표현을 요청하는 의도 기반 UI 계약.
    /// </summary>
    public interface IActorUIService : IGameService
    {
        IActorHpBarView CreateHpBar(GameActor actor);
        IActorDangerRingView CreateDangerRing(GameActor actor, EnemyAttackInfo skill, float duration);
        IActorBreakInteractionView CreateBreakInteraction(GameActor actor);
        void ShowDamageFloater(Vector3 worldPos, float damage, FloatStyle style = FloatStyle.Normal);
        void ShowDamageFloaterLabel(Vector3 worldPos, string label, FloatStyle style = FloatStyle.Normal);
        void ShowDamageFloaterHeal(Vector3 worldPos, float amount, FloatStyle style = FloatStyle.Heal);
        bool HideHud(UIKeyType key);
        void ShowHud(UIKeyType key);
        void ShowItemAcquisition(ItemSO item);
        void RefreshInventoryIfVisible();
        void ShowInteractionBoard(InteractableActorSO data, float current, float max);
        void UpdateInteractionBoard(float current, float max);
        void ShowRestGrowth();
        void ShowRespawn(Action<float> onSpotRevive, Action onPortalRevive);
    }

    public interface IActorCombatService : IGameService
    {
        bool IsActorHitStopping(GameActor actor);
        void ExecuteActorHitStop(GameActor actor, float duration, float timeScale = 0.1f);
        void ExecuteLocalImpact(
            GameActor attacker,
            GameActor victim,
            float duration,
            float localTimeScale = 0.1f,
            bool includeAttacker = true,
            float victimTimeScale = -1f);
        void ExecuteHitStop(float duration, float timeScale = 0.1f);
        void ExecutePlayerDeathHitStop();
        void ResetActorHitStop();
        void TrySpawnVitalOrb(VitalOrbTrigger trigger, Vector3 position);
        void TrySpawnVitalOrbByPolicy(
            VitalOrbTrigger trigger,
            Vector3 position,
            float probability,
            int count,
            float healScale);
        float GetCounterWindowDuration(DefenseSuccessType type);
        void PlayDefenseSuccess(
            DefenseSuccessType type,
            PlayerActor player,
            GameActor attacker,
            AttackData incomingAttack,
            Vector3 position,
            string fxKey = null);
        void PlayDashEvade(
            PlayerActor player,
            GameActor attacker,
            AttackData incomingAttack,
            Vector3 position);
        void StopDefenseFeedbackForCounterAttack(GameActor counterActor);
    }

    public interface IActorSpawnTrackingService : IGameService
    {
        void RegisterActor(GameActor actor, string actorIdOverride = null);
    }

    public interface IInteractionPersistenceService : IGameService
    {
        bool TryConsume(GatheringActor actor);
        bool TryConsume(DropItemActor actor);
    }

    public interface ICycleRemainsService : IGameService
    {
        bool TryAddUnsettledMaterial(int itemId, int count);
        bool HandlePartyWipe(Vector3 deathPosition, Quaternion deathRotation);
    }

    public interface IMonsterLifecycleService : IGameService
    {
        void RecordDeath(MonsterActor monster, string guid);
    }

    public interface IQuestProgressService : IGameService
    {
        void NotifyMonsterKill(string actorId);
    }

    public interface IRecipeProgressService : IGameService
    {
        void NotifyMonsterKill(string actorId);
    }

    public interface ICheatStateService : IGameService
    {
        bool IsAlwaysParryEnabled { get; }
    }

    public interface ICycleExitService : IGameService
    {
        bool RequestExit();
    }

    public interface ISceneTransitionService : IGameService
    {
        void LoadScene(string sceneName, string arrivalId);
    }

    public static class ActorSvc
    {
        public static IActorObjectService Objects => Services.Get<IActorObjectService>();
        public static IActorUIService UI => Services.Get<IActorUIService>();
        public static IActorCombatService Combat => Services.Get<IActorCombatService>();
        public static IActorSpawnTrackingService SpawnTracking => Services.Get<IActorSpawnTrackingService>();
        public static IInteractionPersistenceService InteractionPersistence =>
            Services.Get<IInteractionPersistenceService>();
        public static ICycleRemainsService CycleRemains => Services.Get<ICycleRemainsService>();
        public static IMonsterLifecycleService MonsterLifecycle => Services.Get<IMonsterLifecycleService>();
        public static IQuestProgressService QuestProgress => Services.Get<IQuestProgressService>();
        public static IRecipeProgressService RecipeProgress => Services.Get<IRecipeProgressService>();
        public static ICheatStateService CheatState => Services.Get<ICheatStateService>();
        public static ICycleExitService CycleExit => Services.Get<ICycleExitService>();
        public static ISceneTransitionService SceneTransition => Services.Get<ISceneTransitionService>();
    }
}
