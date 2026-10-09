using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.Manager;

namespace UPlayGround.Tool.Editor.Map
{
    public static partial class ScenarioMapAuthoring
    {
        [Serializable] private sealed class WorldRuntimeReport
        {
            public int monsters;
            public int gatheringNodes;
            public int fishingNodes;
            public int portalTransfers;
            public int restPoints;
            public bool succeeded;
            public List<string> errors = new();
        }

        private static void ValidateWorldRuntime(PlayerActor player)
        {
            GameObject root = GameObject.Find("WorldExpansionActors");
            if (root == null) return;
            var report = new WorldRuntimeReport();
            var world = JsonUtility.FromJson<WorldLayout>(File.ReadAllText(WorldPath));
            var persistence = InteractionRespawnManager.Instance;
            var state = WorldStateManager.Instance;
            string map = UPlayGround.Manager.SceneManager.Instance.CurrentMapID;
            var consumed = new HashSet<string>();
            if (state.GetConsumedInteractables(map) != null)
                foreach (string id in state.GetConsumedInteractables(map)) consumed.Add(id);
            Vector3 previousPosition = player.transform.position;
            Quaternion previousRotation = player.transform.rotation;
            var flags = new Dictionary<string, bool>();
            try
            {
                foreach (MonsterActor monster in root.GetComponentsInChildren<MonsterActor>(true))
                {
                    if (!monster.gameObject.activeInHierarchy || monster.AbilitySystem == null)
                        report.errors.Add("몬스터 런타임 초기화 실패: " + monster.name);
                    if (monster.GetComponent<SceneEntityId>().Guid != monster.name)
                        report.errors.Add("저장 후 몬스터 ID 변경: " + monster.name);
                    report.monsters++;
                }
                if (report.monsters != world.monsters.Length) report.errors.Add("런타임 몬스터 배치 수 불일치");
                foreach (RestPointActor rest in root.GetComponentsInChildren<RestPointActor>())
                {
                    if (rest.GetData() == null || !rest.CanInteract()) throw new InvalidOperationException("휴식 준비 실패: " + rest.name);
                    rest.Interact(player);
                    var screen = UIManager.Instance.GetUI<UPlayGround.UI.UI_Scene_SkillTree>(UPlayGround.UI.UI_Scene_SkillTree.UIKey);
                    if (rest.IsInteracting() || screen == null || !screen.gameObject.activeInHierarchy)
                        throw new InvalidOperationException("휴식 완료/성장 화면 열기 실패: " + rest.name);
                    UIManager.Instance.HideUI(UPlayGround.UI.UI_Scene_SkillTree.UIKey);
                    report.restPoints++;
                }
                foreach (GatheringActor resource in root.GetComponentsInChildren<GatheringActor>(true))
                {
                    resource.gameObject.SetActive(true);
                    resource.ResetForRespawn();
                    var data = resource.GetData();
                    if (data == null || !resource.CanInteract()) throw new InvalidOperationException("상호작용 준비 실패: " + resource.name);
                    resource.Interact(player);
                    if (data.fishingDepleteCatchCount > 0)
                    {
                        for (int i = 0; i < data.fishingDepleteCatchCount; i++)
                            resource.OnAnimationEvent(InteractionAnimEvent.CatchFish, new PlayerInteractionEvent { value = 1 });
                        report.fishingNodes++;
                    }
                    else
                    {
                        resource.OnAnimationEvent(InteractionAnimEvent.OnHit, new PlayerInteractionEvent { value = data.hp });
                        report.gatheringNodes++;
                    }
                    string id = resource.GetComponent<SceneEntityId>().Guid;
                    bool isDepleted = data.interactionObjectType == InteractionObjectType.FISHING_ZONE
                        ? !resource.CanInteract() : !resource.gameObject.activeSelf;
                    if (!isDepleted || !state.IsInteractableConsumed(map, id))
                        throw new InvalidOperationException("소모 상태 기록 실패: " + resource.name);
                    resource.ResetForRespawn();
                    resource.gameObject.SetActive(true);
                    persistence.ApplyConsumedStatesToScene();
                    isDepleted = data.interactionObjectType == InteractionObjectType.FISHING_ZONE
                        ? !resource.CanInteract() : !resource.gameObject.activeSelf;
                    if (!isDepleted) throw new InvalidOperationException("소모 상태 복원 실패: " + resource.name);
                }
                foreach (WorldPortal entry in world.portals)
                {
                    PortalActor portal = root.transform.Find(entry.id).GetComponentInChildren<PortalActor>();
                    string flag = PortalActivationState.GetFlagKey(entry.id);
                    flags.Add(flag, Svc.Flags.GetFlag(flag));
                    if (!portal.IsActivated) portal.Interact(player);
                    if (!portal.IsActivated) throw new InvalidOperationException("포탈 해금 실패: " + entry.label);
                    portal.SendMessage("OnTriggerEnter", player.ActorController.Motor.Capsule, SendMessageOptions.RequireReceiver);
                    Vector3 expected = root.transform.Find(entry.destinationId + "_Arrival").position;
                    if (Vector3.Distance(player.ActorController.Motor.TransientPosition, expected) > 0.2f)
                        throw new InvalidOperationException("포탈 이동 실패: " + entry.label);
                    report.portalTransfers++;
                }
            }
            catch (Exception exception) { report.errors.Add(exception.Message); }
            finally
            {
                player.ActorController.Motor.SetPositionAndRotation(previousPosition, previousRotation);
                CameraManager.Instance?.SnapToTarget(previousPosition);
                foreach (var flag in flags) Svc.Flags.SetFlag(flag.Key, flag.Value);
                state.ClearConsumedInteractables(map);
                foreach (string id in consumed) state.RecordConsumedInteractable(map, id);
                foreach (GatheringActor resource in root.GetComponentsInChildren<GatheringActor>(true))
                {
                    resource.ResetForRespawn();
                    resource.gameObject.SetActive(true);
                }
                persistence.ApplyConsumedStatesToScene();
            }
            report.succeeded = report.errors.Count == 0;
            File.WriteAllText(ReportDirectory + "/WorldPlayMode.json", JsonUtility.ToJson(report, true));
            if (!report.succeeded) throw new InvalidOperationException(string.Join("\n", report.errors));
        }
    }
}
