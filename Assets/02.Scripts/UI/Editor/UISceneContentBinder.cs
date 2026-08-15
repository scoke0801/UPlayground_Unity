using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.UI.EditorTools
{
    /// <summary>
    /// UI_SceneBase 파생 Scene UI 프리팹의 <c>_sceneContent</c>(열기/닫기 슬라이드 대상)를
    /// 기존 프리팹을 재생성하지 않고 "그 필드만" 연결하는 비파괴 바인더.
    ///
    /// - 각 UI 전용 프리팹 빌더는 자식 계층 전체를 회색 초안으로 재생성(ClearChildren)하므로,
    ///   Unity에서 다듬은 색/폰트/스프라이트를 보존하려면 재실행 대신 이 툴로 필드만 연결한다.
    /// - "현재 구조가 빌더와 다른" 경우에도 동작하도록, 후보 이름 검색 → 폴백 휴리스틱 순으로
    ///   슬라이드시킬 메인 패널 RectTransform을 찾는다.
    /// - UI_Scene_Map처럼 슬라이드시킬 단일 창이 없는 전체 화면 UI는 대상에서 제외(루트 페이드 전용).
    /// - 기본은 이미 연결된 프리팹을 건너뛴다(idempotent). 강제 재연결 메뉴는 별도 제공.
    /// </summary>
    public static class UISceneContentBinder
    {
        // 대상 프리팹 경로와, 슬라이드시킬 메인 패널로 우선 선택할 자식 이름 후보.
        // (UI_Scene_Map은 전체 화면 맵이라 페이드 전용 → 목록에서 제외한다.)
        private static readonly (string path, string[] panelNames)[] Targets =
        {
            ("Assets/03.Prefabs/UI/Scene/Inventory/UI_Scene_Inventory.prefab", new[] { "Window" }),
            ("Assets/03.Prefabs/UI/Scene/Quest/UI_Scene_QuestMenu.prefab",     new[] { "Window" }),
            ("Assets/03.Prefabs/UI/Scene/UI_Scene_SettingMenu.prefab",         new[] { "Panel", "Window" }),
            ("Assets/03.Prefabs/UI/Scene/Party/UI_Scene_PartyMenu.prefab",     new[] { "Window" }),
            ("Assets/03.Prefabs/UI/Scene/Craft/UI_Scene_CraftMenu.prefab",     new[] { "Window" }),
        };

        // 폴백 시 메인 패널로 보기 어려운(배경/딤/입력차단) 자식 이름 조각.
        private static readonly string[] OverlayNameHints =
        {
            "dim", "background", "bg", "overlay", "backdrop", "blocker", "raycast", "scrim"
        };

        public static void BindMissing() => Run(force: false);

        public static void BindForce() => Run(force: true);

        private static void Run(bool force)
        {
            int bound = 0, skipped = 0, failed = 0;
            var log = new System.Text.StringBuilder();

            foreach (var (path, panelNames) in Targets)
            {
                if (!System.IO.File.Exists(path))
                {
                    log.AppendLine($"  · [없음] {path}");
                    failed++;
                    continue;
                }

                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var scene = root.GetComponent<UI_SceneBase>();
                    if (scene == null)
                    {
                        log.AppendLine($"  · [스킵] UI_SceneBase 없음: {path}");
                        skipped++;
                        continue;
                    }

                    var so = new SerializedObject(scene);
                    var prop = so.FindProperty("_sceneContent");
                    if (prop == null)
                    {
                        log.AppendLine($"  · [실패] _sceneContent 프로퍼티 없음: {path}");
                        failed++;
                        continue;
                    }

                    if (prop.objectReferenceValue != null && !force)
                    {
                        log.AppendLine($"  · [유지] 이미 연결됨({prop.objectReferenceValue.name}): {System.IO.Path.GetFileName(path)}");
                        skipped++;
                        continue;
                    }

                    RectTransform panel = FindPanel(root.transform, panelNames);
                    if (panel == null)
                    {
                        log.AppendLine($"  · [실패] 슬라이드 패널 후보를 찾지 못함: {path}");
                        failed++;
                        continue;
                    }

                    prop.objectReferenceValue = panel;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    log.AppendLine($"  · [연결] {System.IO.Path.GetFileName(path)} → \"{panel.name}\"");
                    bound++;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[SceneContentBinder] 완료 — 연결 {bound}, 유지/스킵 {skipped}, 실패 {failed}\n{log}");
        }

        /// <summary>
        /// 슬라이드시킬 메인 패널 RectTransform 탐색:
        /// 1) 직속 자식에서 후보 이름과 일치하는 것
        /// 2) 하위 전체에서 후보 이름과 일치하는 것
        /// 3) 배경/딤으로 보이지 않는 첫 직속 자식(폴백)
        /// </summary>
        private static RectTransform FindPanel(Transform root, string[] panelNames)
        {
            // 1) 직속 자식 우선(이름 일치)
            foreach (Transform child in root)
            {
                if (panelNames.Any(n => string.Equals(child.name, n, System.StringComparison.OrdinalIgnoreCase)))
                {
                    var rt = child as RectTransform;
                    if (rt != null) return rt;
                }
            }

            // 2) 하위 전체(이름 일치)
            foreach (var rt in root.GetComponentsInChildren<RectTransform>(true))
            {
                if (rt.transform == root) continue;
                if (panelNames.Any(n => string.Equals(rt.name, n, System.StringComparison.OrdinalIgnoreCase)))
                    return rt;
            }

            // 3) 폴백: 배경/딤이 아닌 첫 직속 자식
            foreach (Transform child in root)
            {
                var rt = child as RectTransform;
                if (rt == null) continue;
                string lower = child.name.ToLowerInvariant();
                if (OverlayNameHints.Any(h => lower.Contains(h))) continue;
                return rt;
            }

            return null;
        }
    }
}
