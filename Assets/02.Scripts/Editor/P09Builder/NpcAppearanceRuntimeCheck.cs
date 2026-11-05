using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data.Actor;
using UPlayGround.Dialogue;
using UPlayGround.EditorTools;
using UPlayGround.Manager;

namespace UPlayGround.Editor.P09Builder
{
    /// <summary>정상 Boot 뒤 실제 정의 ID 스폰과 임시 데이터의 짧은 재생 경로를 검사한다.</summary>
    public static class NpcAppearanceRuntimeCheck
    {
        /// <summary>대표 CSV의 24개 ID를 한 종씩 스폰하고 원본을 저장하지 않고 제거한다.</summary>
        [UPlaygroundTool("p09.npc-appearance.runtime-smoke")]
        public static void Run()
        {
            if (!EditorApplication.isPlaying || !Services.TryGet<IActorSpawnService>(out var spawn)
                || !Services.TryGet<IActorObjectService>(out var objects) || objects.Player == null
                || !Services.TryGet<IDialogueService>(out var dialogue) || dialogue.IsDialogueActive)
                throw new InvalidOperationException("정상 Boot·플레이어·스폰·대화 서비스 및 진행 중 대화 없음이 필요합니다.");
            string output = NpcAppearanceImprovement.Output;
            Directory.CreateDirectory(output);
            var ids = File.ReadAllLines("output/NpcAppearanceReview/representatives.csv").Skip(1)
                .Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Split(',')[0]).ToArray();
            if (ids.Length != 24 || ids.Distinct().Count() != 24) throw new InvalidOperationException("대표 CSV 계약 불일치");
            var rows = new List<string> { "시작 " + DateTime.Now.ToString("O"), "API 강제 재생 검사. 자연 배회·화면·실물 입력 검증과 구분." };
            foreach (string id in ids)
            {
                NpcActor actor = null;
                NpcActorSO temporaryData = null;
                DialogueGraphSO graph = null;
                DialogueNodeSO end = null;
                try
                {
                    actor = spawn.SpawnActor(id, objects.Player.transform.position + objects.Player.transform.forward * 3, Quaternion.identity) as NpcActor;
                    if (actor == null || actor.Definition == null || actor.Definition.actorId != id) throw new InvalidOperationException(id + ": 정의 스폰 실패");
                    if (actor.CanInteract()) throw new InvalidOperationException(id + ": 원본 콘텐츠 없는 상호작용이 허용됨");
                    var original = actor.GetData() as NpcActorSO;
                    foreach (var slot in NpcAppearanceImprovement.Slots)
                    {
                        if (!actor.Animator.HasMotion(slot)) throw new InvalidOperationException(id + ": 모션 해석 실패 " + slot);
                        var state = actor.Animator.PlayMotion(slot, 0);
                        if (state == null) throw new InvalidOperationException(id + ": 재생 실패 " + slot);
                        actor.Animator.GetAnimancerComponent().Evaluate(.1f);
                        if (actor.Animator.CurrentMotionAsset != actor.Animator.MotionSet.GetMotionSetAsset(slot) || state.Clip == null)
                            throw new InvalidOperationException(id + ": 실제 재생 소스 불일치 " + slot);
                        rows.Add(id + "|" + slot + "|" + state.Clip.name + "|time=" + state.Time);
                    }
                    // 즉시 종료 노드로 진입 계약만 확인한다. UI·대사·Talk 상태 관찰은 사용자 시험이다.
                    graph = ScriptableObject.CreateInstance<DialogueGraphSO>(); graph.graphId = "P09_Temporary_Entry"; graph.startNodeId = "End";
                    end = ScriptableObject.CreateInstance<DialogueNodeSO>(); end.nodeId = "End"; end.nodeType = NodeType.End;
                    graph.nodes.Add(end);
                    if (!graph.TryValidatePlayback(out string error)) throw new InvalidOperationException(error);
                    temporaryData = UnityEngine.Object.Instantiate(original); temporaryData.dialogueGraph = graph;
                    var serialized = new SerializedObject(actor); serialized.FindProperty("_data").objectReferenceValue = temporaryData; serialized.ApplyModifiedPropertiesWithoutUndo();
                    if (!actor.CanInteract()) throw new InvalidOperationException(id + ": 임시 유효 그래프 진입 거부");
                    actor.Interact(objects.Player);
                    if (actor.IsInteracting() || dialogue.IsDialogueActive) throw new InvalidOperationException(id + ": 즉시 종료 뒤 상호작용 잔존");
                    serialized.Update(); serialized.FindProperty("_data").objectReferenceValue = original; serialized.ApplyModifiedPropertiesWithoutUndo();
                    rows.Add(id + "|ID 스폰·Idle/Walk/Talk 강제 재생·원본 거부·임시 End 그래프 진입/종료 통과");
                }
                catch (Exception error)
                {
                    rows.Add(id + "|실패|" + error); throw;
                }
                finally
                {
                    if (actor != null && actor.IsInteracting()) actor.StopInteract();
                    if (actor != null) UnityEngine.Object.DestroyImmediate(actor.gameObject);
                    if (temporaryData != null) UnityEngine.Object.DestroyImmediate(temporaryData);
                    if (graph != null) UnityEngine.Object.DestroyImmediate(graph);
                    if (end != null) UnityEngine.Object.DestroyImmediate(end);
                    File.WriteAllLines(output + "/runtime-smoke.txt", rows);
                }
            }
            File.AppendAllText(output + "/runtime-smoke.txt", "24종 API 검사 완료. 사용자 시각·조작 검수 대기.\n");
        }
    }
}
