#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.UI.Editor
{
    /// <summary>
    /// 설정 화면의 기존 전투 스위치 행을 복제해 같은 비주얼·게임패드 포커스 계약으로 진동 토글을 추가한다.
    /// 전체 설정 프리팹을 재생성하지 않아 다른 UI 저작 변경을 보존한다.
    /// </summary>
    public static class UICombatVibrationSetupTool
    {
        private const string PrefabPath =
            "Assets/03.Prefabs/UI/Scene/UI_Scene_SettingMenu.prefab";

        [UPlayGround.EditorTools.UPlaygroundTool(
            "UPlayGround/UI/설정/전투 진동 토글 적용")]
        public static void Apply()
        {
            if (!Application.isBatchMode && !EditorUtility.DisplayDialog(
                    "전투 진동 토글 적용",
                    "설정 화면의 기존 전투 스위치 스타일로 진동 토글을 추가합니다. 계속할까요?",
                    "적용",
                    "취소"))
                return;

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (root == null)
                throw new InvalidOperationException($"프리팹을 열지 못했습니다: {PrefabPath}");

            try
            {
                UISettingPageGamePlay page =
                    root.GetComponentInChildren<UISettingPageGamePlay>(true);
                if (page == null)
                    throw new InvalidOperationException("게임플레이 설정 페이지를 찾지 못했습니다.");

                Transform existing = FindDirectChild(page.transform, "Row_전투 진동");
                if (existing == null)
                {
                    Transform source = FindDirectChild(page.transform, "Row_타겟 보정");
                    if (source == null)
                        throw new InvalidOperationException("복제할 '타겟 보정' 스위치 행을 찾지 못했습니다.");

                    GameObject row = UnityEngine.Object.Instantiate(source.gameObject, page.transform);
                    row.name = "Row_전투 진동";
                    row.transform.SetSiblingIndex(source.GetSiblingIndex() + 1);

                    TMP_Text label = null;
                    foreach (TMP_Text candidate in row.GetComponentsInChildren<TMP_Text>(true))
                    {
                        if (candidate.gameObject.name != "Label")
                            continue;
                        label = candidate;
                        break;
                    }
                    if (label == null)
                        throw new InvalidOperationException("복제된 전투 진동 행의 Label을 찾지 못했습니다.");
                    label.text = "전투 진동";
                }

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"[UICombatVibrationSetup] 전투 진동 토글 적용 완료: {PrefabPath}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Transform FindDirectChild(Transform parent, string childName)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (string.Equals(child.name, childName, StringComparison.Ordinal))
                    return child;
            }
            return null;
        }
    }
}
#endif
