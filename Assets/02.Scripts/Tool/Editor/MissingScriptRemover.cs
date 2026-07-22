using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Tool.Editor
{
    public static class MissingScriptRemover
    {
        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/유틸/Missing Script 정리/선택 오브젝트 하위 전체")]
        private static void RemoveMissingScriptsFromSelection()
        {
            var selected = Selection.gameObjects;
            if (selected == null || selected.Length == 0)
            {
                EditorUtility.DisplayDialog("Missing Script 제거", "Hierarchy에서 오브젝트를 선택하세요.", "확인");
                return;
            }

            int totalRemoved = 0;
            var processed = new HashSet<GameObject>();

            foreach (var root in selected)
            {
                var all = root.GetComponentsInChildren<Transform>(true);
                foreach (var t in all)
                {
                    if (!processed.Add(t.gameObject))
                        continue;

                    int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                    if (count > 0)
                    {
                        Undo.RegisterCompleteObjectUndo(t.gameObject, "Missing Script 제거");
                        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                        totalRemoved += count;
                        EditorUtility.SetDirty(t.gameObject);
                    }
                }
            }

            if (totalRemoved > 0)
            {
                EditorUtility.DisplayDialog(
                    "Missing Script 제거 완료",
                    $"총 {totalRemoved}개의 Missing Script를 제거했습니다.",
                    "확인"
                );
                Debug.Log($"[MissingScriptRemover] {totalRemoved}개 제거 완료.");
            }
            else
            {
                EditorUtility.DisplayDialog("Missing Script 제거", "Missing Script가 없습니다.", "확인");
            }
        }

        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/유틸/Missing Script 정리/선택 오브젝트 하위 전체", true)]
        private static bool ValidateRemoveMissingScripts()
        {
            return Selection.gameObjects != null && Selection.gameObjects.Length > 0;
        }

        // 우클릭 컨텍스트 메뉴 (Hierarchy에서 직접 접근)
        [MenuItem("GameObject/Missing Script 제거 (하위 포함)", false, 0)]
        private static void RemoveMissingScriptsContext()
        {
            RemoveMissingScriptsFromSelection();
        }

        [MenuItem("GameObject/Missing Script 제거 (하위 포함)", true)]
        private static bool ValidateRemoveMissingScriptsContext()
        {
            return Selection.gameObjects != null && Selection.gameObjects.Length > 0;
        }
    }
}
