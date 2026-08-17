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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnEnterPlayMode() => Clear();

        /// <summary>서비스가 구현한 모든 게임 서비스 계약을 단일 구현으로 등록한다.</summary>
        public static void Register(IGameService service)
        {
            if (service == null)
                return;
            if (!IsServiceAlive(service))
                throw new ArgumentException("파괴된 Unity 객체는 서비스로 등록할 수 없습니다.", nameof(service));

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

                if (TryGetRegisteredService(contract, out IGameService registered))
                {
                    if (ReferenceEquals(registered, service))
                        continue;

                    throw new InvalidOperationException(
                        $"[Services] 서비스 계약이 중복 등록되었습니다: {contract.FullName}, " +
                        $"기존={registered.GetType().FullName}, 신규={serviceType.FullName}");
                }
            }

            for (int i = 0; i < interfaces.Length; i++)
            {
                Type contract = interfaces[i];
                if (contract == typeof(IGameService)
                    || !typeof(IGameService).IsAssignableFrom(contract))
                {
                    continue;
                }

                if (!Registry.ContainsKey(contract))
                    Registry.Add(contract, service);
                MissingBindingWarnings.Remove(contract);
            }
        }

        /// <summary>지정한 서비스 인스턴스가 소유한 계약 바인딩만 해제한다.</summary>
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

        /// <summary>등록된 서비스 계약을 조회하고 누락 시 최초 한 번 경고한다.</summary>
        public static T Get<T>() where T : class, IGameService
        {
            if (ManagerLifecycle.ApplicationIsQuitting)
                return null;

            Type contract = typeof(T);
            if (TryGetRegisteredService(contract, out IGameService service))
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

            if (!TryGetRegisteredService(typeof(T), out IGameService registered))
                return false;

            service = registered as T;
            return service != null;
        }

        /// <summary>플레이 세션에 남은 모든 서비스 바인딩과 누락 경고 상태를 초기화한다.</summary>
        public static void Clear()
        {
            Registry.Clear();
            MissingBindingWarnings.Clear();
        }

        private static bool TryGetRegisteredService(Type contract, out IGameService service)
        {
            if (!Registry.TryGetValue(contract, out service))
                return false;
            if (IsServiceAlive(service))
                return true;

            Registry.Remove(contract);
            service = null;
            return false;
        }

        private static bool IsServiceAlive(IGameService service) =>
            service != null
            && (service is not UnityEngine.Object unityObject || unityObject != null);
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
        public static ICombatRelationService CombatRelations =>
            Services.Get<ICombatRelationService>();
        public static IRecruitmentEncounterService RecruitmentEncounters =>
            Services.Get<IRecruitmentEncounterService>();
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
