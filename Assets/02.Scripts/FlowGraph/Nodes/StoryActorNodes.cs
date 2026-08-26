using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Manager;

namespace UPlayGround.FlowGraph
{
    /// <summary>스토리 진행용 액터를 ID로 한 번만 생성하고 결과를 분기한다.</summary>
    [FlowNodeMenu("스토리/Spawn Actor", Summary = "Actor ID에 해당하는 스토리 액터를 중복 없이 생성합니다.", Keywords = new[] { "actor", "spawn", "보스", "생성" })]
    [Serializable]
    public sealed class SpawnStoryActorNode : FlowNode
    {
        public const string ActorIdPort = "ActorId";
        public const string SpawnedPort = "Spawned";
        public const string FailedPort = "Failed";

        [Tooltip("ActorId 데이터 포트가 연결되지 않았을 때 사용할 Actor ID.")]
        public string actorId;
        public Vector3 position;
        public Vector3 eulerAngles;
        [Min(0f)] public float serviceReadyTimeout = 10f;

        public override IEnumerable<FlowPortDef> Ports
        {
            get
            {
                yield return FlowPortDef.Input();
                yield return FlowPortDef.DataInput<string>(ActorIdPort, displayName: "Actor ID");
                yield return FlowPortDef.Output(SpawnedPort, optional: true);
                yield return FlowPortDef.Output(FailedPort, optional: true);
            }
        }

        public override IEnumerator Execute(FlowToken token)
        {
            string resolvedActorId = ResolveActorId(token);
            if (string.IsNullOrWhiteSpace(resolvedActorId))
            {
                token.Emit(FailedPort);
                yield break;
            }

            IWorldActor existingActor = Svc.ActorQuery?.FindActor(resolvedActorId);
            if (IsUnityObjectAlive(existingActor))
            {
                token.Emit(SpawnedPort);
                yield break;
            }

            float deadline = Time.unscaledTime + serviceReadyTimeout;
            IActorSpawnService spawnService = null;
            while (!token.Context.Cancelled
                   && (!Services.TryGet(out spawnService) || !spawnService.IsReady)
                   && Time.unscaledTime < deadline)
            {
                yield return null;
            }

            if (token.Context.Cancelled)
                yield break;

            IWorldActor spawnedActor = spawnService?.SpawnActor(
                resolvedActorId,
                position,
                Quaternion.Euler(eulerAngles));
            token.Emit(IsUnityObjectAlive(spawnedActor) ? SpawnedPort : FailedPort);
        }

        private string ResolveActorId(FlowToken token)
        {
            return token.Graph.TryEvaluateDataInput(
                       token.Context,
                       this,
                       ActorIdPort,
                       out string connectedActorId)
                   && !string.IsNullOrWhiteSpace(connectedActorId)
                ? connectedActorId
                : actorId;
        }

        private static bool IsUnityObjectAlive(IWorldActor actor)
        {
            return actor != null
                   && (actor is not UnityEngine.Object unityObject || unityObject != null);
        }
    }

    /// <summary>지정 Actor ID의 액터가 실제로 등장한 뒤 쓰러질 때까지 대기한다.</summary>
    [FlowNodeMenu("스토리/Wait Actor Defeated", Summary = "스토리 액터의 등장과 전투 종료를 안전하게 기다립니다.", Keywords = new[] { "actor", "defeat", "보스", "대기" })]
    [Serializable]
    public sealed class WaitStoryActorDefeatedNode : FlowNode
    {
        public const string ActorIdPort = "ActorId";
        public const string DefeatedPort = "Defeated";
        public const string FailedPort = "Failed";

        [Tooltip("ActorId 데이터 포트가 연결되지 않았을 때 사용할 Actor ID.")]
        public string actorId;
        [Min(0f)] public float discoveryTimeout = 10f;

        public override IEnumerable<FlowPortDef> Ports
        {
            get
            {
                yield return FlowPortDef.Input();
                yield return FlowPortDef.DataInput<string>(ActorIdPort, displayName: "Actor ID");
                yield return FlowPortDef.Output(DefeatedPort, optional: true);
                yield return FlowPortDef.Output(FailedPort, optional: true);
            }
        }

        public override IEnumerator Execute(FlowToken token)
        {
            string resolvedActorId = ResolveActorId(token);
            if (string.IsNullOrWhiteSpace(resolvedActorId))
            {
                token.Emit(FailedPort);
                yield break;
            }

            float discoveryDeadline = Time.unscaledTime + discoveryTimeout;
            bool hasObservedActor = false;
            while (!token.Context.Cancelled)
            {
                IWorldActor actor = Svc.ActorQuery?.FindActor(resolvedActorId);
                bool actorExists = IsUnityObjectAlive(actor);
                if (actorExists)
                {
                    hasObservedActor = true;
                    if (!actor.IsAlive)
                    {
                        token.Emit(DefeatedPort);
                        yield break;
                    }
                }
                else if (hasObservedActor)
                {
                    token.Emit(DefeatedPort);
                    yield break;
                }
                else if (Time.unscaledTime >= discoveryDeadline)
                {
                    token.Emit(FailedPort);
                    yield break;
                }

                yield return null;
            }
        }

        private string ResolveActorId(FlowToken token)
        {
            return token.Graph.TryEvaluateDataInput(
                       token.Context,
                       this,
                       ActorIdPort,
                       out string connectedActorId)
                   && !string.IsNullOrWhiteSpace(connectedActorId)
                ? connectedActorId
                : actorId;
        }

        private static bool IsUnityObjectAlive(IWorldActor actor)
        {
            return actor != null
                   && (actor is not UnityEngine.Object unityObject || unityObject != null);
        }
    }
}
