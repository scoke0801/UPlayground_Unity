using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Manager
{
    /// <summary>GameManager가 수명주기를 관리하며 서비스 레지스트리에 노출하는 계약의 마커.</summary>
    public interface IGameService
    {
    }

    /// <summary>
    /// 구체 매니저 타입을 하위 모듈에 노출하지 않는 런타임 서비스 레지스트리.
    /// </summary>
    public static class Services
    {
        private static readonly Dictionary<Type, IGameService> Registry = new();
        private static readonly HashSet<Type> MissingBindingWarnings = new();

        public static void Register(IGameService service)
        {
            if (service == null)
                return;

            Type serviceType = service.GetType();
            Type[] interfaces = serviceType.GetInterfaces();
            for (int i = 0; i < interfaces.Length; i++)
            {
                Type contract = interfaces[i];
                if (contract == typeof(IGameService)
                    || !typeof(IGameService).IsAssignableFrom(contract))
                {
                    continue;
                }

                Registry[contract] = service;
                MissingBindingWarnings.Remove(contract);
            }
        }

        public static void Unregister(IGameService service)
        {
            if (service == null)
                return;

            Type[] interfaces = service.GetType().GetInterfaces();
            for (int i = 0; i < interfaces.Length; i++)
            {
                Type contract = interfaces[i];
                if (Registry.TryGetValue(contract, out IGameService registered)
                    && ReferenceEquals(registered, service))
                {
                    Registry.Remove(contract);
                }
            }
        }

        public static T Get<T>() where T : class, IGameService
        {
            // 앱/플레이 종료 중에는 매니저가 순서 보장 없이 파괴된다. 인터페이스 참조는
            // 파괴된 매니저(fake-null)를 감지할 수 없으므로, BaseManager<T>.Instance와
            // 동일하게 종료 중에는 null을 반환해 파괴된 객체 접근을 차단한다.
            if (ManagerLifecycle.ApplicationIsQuitting)
                return null;

            Type contract = typeof(T);
            if (Registry.TryGetValue(contract, out IGameService service))
                return service as T;

            if (MissingBindingWarnings.Add(contract))
                Debug.LogWarning($"[Services] 등록되지 않은 서비스 계약 요청: {contract.FullName}");

            return null;
        }

        /// <summary>
        /// 등록 여부가 정상적인 분기인 종료/비동기 경계에서 경고 없이 서비스를 조회한다.
        /// </summary>
        public static bool TryGet<T>(out T service) where T : class, IGameService
        {
            service = null;
            if (ManagerLifecycle.ApplicationIsQuitting)
                return false;

            if (!Registry.TryGetValue(typeof(T), out IGameService registered))
                return false;

            // 계약이 인터페이스라 제네릭 비교로는 파괴된 MonoBehaviour(fake null)를 걸러내지 못한다.
            // 씬 전환/종료 경계가 이 API의 주 용도이므로 여기서 직접 판별한다.
            if (registered is UnityEngine.Object unityObject && unityObject == null)
                return false;

            service = registered as T;
            return service != null;
        }

        public static void Clear()
        {
            Registry.Clear();
            MissingBindingWarnings.Clear();
        }
    }

    /// <summary>자주 쓰는 서비스 계약의 짧은 접근점.</summary>
    public static class Svc
    {
        public static IInputService Input => Services.Get<IInputService>();
        public static IHitStopService HitStop => Services.Get<IHitStopService>();
        public static IVitalOrbService VitalOrb => Services.Get<IVitalOrbService>();
        public static ISettingsService Settings => Services.Get<ISettingsService>();
        public static IGameTimeService GameTime => Services.Get<IGameTimeService>();
        public static IElementRandomSeedService ElementRandom =>
            Services.Get<IElementRandomSeedService>();
        public static ICameraViewService Camera => Services.Get<ICameraViewService>();
        public static IAssetService Asset => Services.Get<IAssetService>();
        public static IActorQueryService ActorQuery => Services.Get<IActorQueryService>();
        public static IGameEventObservable Events => Services.Get<IGameEventObservable>();
        public static IGameEventPublisher EventPublisher => Services.Get<IGameEventPublisher>();
        public static IPartyService Party => Services.Get<IPartyService>();
        public static IPassiveModifierReader Passives => Services.Get<IPassiveModifierReader>();
        public static IMonsterCodexService MonsterCodex =>
            Services.Get<IMonsterCodexService>();
        public static IMonsterCodexReader MonsterCodexReader =>
            Services.Get<IMonsterCodexReader>();
        public static IInventoryService Inventory => Services.Get<IInventoryService>();
        public static IItemService Item => Services.Get<IItemService>();
        public static IDialogueService Dialogue => Services.Get<IDialogueService>();
        public static ISoundService Sound => Services.Get<ISoundService>();
        public static IProjectileService Projectile => Services.Get<IProjectileService>();
        public static IGlobalFlagService Flags => Services.Get<IGlobalFlagService>();
        public static IQuestFlowService QuestFlow => Services.Get<IQuestFlowService>();
        public static IStoryFlowService StoryFlow => Services.Get<IStoryFlowService>();
        public static IFlowGraphService FlowGraph => Services.Get<IFlowGraphService>();
        public static ICinematicStageService CinematicStage =>
            Services.Get<ICinematicStageService>();
    }
}
