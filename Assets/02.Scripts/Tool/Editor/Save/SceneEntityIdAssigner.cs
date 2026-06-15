#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.Component;
using UPlayGround.Tool.Editor;

namespace UPlayGround.Tool.Editor.Save
{
    /// <summary>
    /// 열린 씬의 모든 MonsterActor에 SceneEntityId를 부착하고 GUID를 보정한다.
    /// 몬스터 처치 영속화(WorldStateManager)의 안정적 식별자를 일괄 발급하는 용도.
    ///
    /// - SceneEntityId가 없으면 추가하고 새 GUID 발급
    /// - GUID가 비었거나 다른 인스턴스와 중복되면 새 GUID로 보정
    /// </summary>
    public static class SceneEntityIdAssigner
    {
        [MenuItem("UPlayGround/World/배치 몬스터 SceneEntityId 일괄 부여", priority = UPlaygroundMenuPriority.WorldMap)]
        private static void AssignToOpenScenes()
        {
            var monsters = Object.FindObjectsByType<MonsterActor>(FindObjectsSortMode.None);
            if (monsters.Length == 0)
            {
                EditorUtility.DisplayDialog("SceneEntityId 부여", "열린 씬에 MonsterActor가 없습니다.", "확인");
                return;
            }

            var seenGuids = new HashSet<string>();
            int added = 0, fixedDup = 0, kept = 0;
            var dirtyScenes = new HashSet<Scene>();

            foreach (var monster in monsters)
            {
                if (monster == null) continue;

                var entityId = monster.GetComponent<SceneEntityId>();
                if (entityId == null)
                {
                    entityId = Undo.AddComponent<SceneEntityId>(monster.gameObject);
                    entityId.EditorSetGuid(System.Guid.NewGuid().ToString("N"));
                    added++;
                    MarkDirty(entityId, dirtyScenes);
                    seenGuids.Add(entityId.Guid);
                    continue;
                }

                if (!entityId.HasGuid || seenGuids.Contains(entityId.Guid))
                {
                    Undo.RecordObject(entityId, "Fix SceneEntityId GUID");
                    entityId.EditorSetGuid(System.Guid.NewGuid().ToString("N"));
                    fixedDup++;
                    MarkDirty(entityId, dirtyScenes);
                }
                else
                {
                    kept++;
                }
                seenGuids.Add(entityId.Guid);
            }

            foreach (var scene in dirtyScenes)
                EditorSceneManager.MarkSceneDirty(scene);

            Debug.Log($"[SceneEntityIdAssigner] 완료 — 신규 부착 {added}, 중복/공백 보정 {fixedDup}, 유지 {kept} (총 {monsters.Length})");
            EditorUtility.DisplayDialog("SceneEntityId 부여",
                $"신규 부착: {added}\n중복/공백 보정: {fixedDup}\n유지: {kept}\n총 몬스터: {monsters.Length}\n\n씬을 저장하세요.", "확인");
        }

        private static void MarkDirty(SceneEntityId entityId, HashSet<Scene> dirtyScenes)
        {
            EditorUtility.SetDirty(entityId);
            if (entityId.gameObject.scene.IsValid())
                dirtyScenes.Add(entityId.gameObject.scene);
        }
    }
}
#endif
