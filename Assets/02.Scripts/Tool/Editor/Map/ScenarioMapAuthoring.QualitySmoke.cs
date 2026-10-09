using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.Manager;

namespace UPlayGround.Tool.Editor.Map
{
    public static partial class ScenarioMapAuthoring
    {
        private static int ValidateQualityRuntime(PlayerActor player)
        {
            GameObject root = GameObject.Find("QualityInteractions");
            if (root == null) return 0;
            GatheringActor[] resources = root.GetComponentsInChildren<GatheringActor>(true);
            WorldStateManager world = WorldStateManager.Instance;
            InteractionRespawnManager persistence = InteractionRespawnManager.Instance;
            string mapId = UPlayGround.Manager.SceneManager.Instance.CurrentMapID;
            var before = new HashSet<string>();
            if (world.GetConsumedInteractables(mapId) != null)
                foreach (string guid in world.GetConsumedInteractables(mapId)) before.Add(guid);
            int validated = 0;
            try
            {
                foreach (GatheringActor resource in resources)
                {
                    resource.gameObject.SetActive(true);
                    resource.ResetForRespawn();
                    if (resource.GetData() == null || !resource.CanInteract())
                        throw new InvalidOperationException("채집 준비 실패: " + resource.name);
                    resource.Interact(player);
                    if (!resource.IsInteracting()) throw new InvalidOperationException("채집 시작 실패: " + resource.name);
                    resource.OnAnimationEvent(InteractionAnimEvent.OnHit,
                        new PlayerInteractionEvent { value = resource.GetData().hp });
                    string guid = resource.GetComponent<SceneEntityId>().Guid;
                    if (resource.gameObject.activeSelf || !world.IsInteractableConsumed(mapId, guid))
                        throw new InvalidOperationException("채집 보상/소모 기록 실패: " + resource.name);
                    // 같은 저장 상태를 다시 적용해 재진입 시 소모한 자원이 살아나지 않는지 확인한다.
                    resource.gameObject.SetActive(true);
                    resource.ResetForRespawn();
                    persistence.ApplyConsumedStatesToScene();
                    if (resource.gameObject.activeSelf)
                        throw new InvalidOperationException("채집 소모 상태 재적용 실패: " + resource.name);
                    validated++;
                }
            }
            finally
            {
                // 자동 검증의 월드 변경은 메모리에서 되돌리고 디스크 세이브는 호출하지 않는다.
                world.ClearConsumedInteractables(mapId);
                foreach (string guid in before) world.RecordConsumedInteractable(mapId, guid);
                foreach (GatheringActor resource in resources)
                {
                    resource.ResetForRespawn();
                    resource.gameObject.SetActive(true);
                }
                persistence.ApplyConsumedStatesToScene();
            }
            return validated;
        }
    }
}
