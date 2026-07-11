using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace UPlayGround.Tool.Editor
{
    /// <summary>
    /// lilToon은 빌드 직전(lilToonBuildProcessor.OnPreprocessBuild) 셰이더 최적화로 사용되지 않는 기능을
    /// #define에서 제거한다. 이때 사용 여부 판정은 "빌드 씬에서 참조된 머티리얼"만 스캔하므로
    /// (lilToonSetting.WalkAllSceneReferencedAssets), Resources에만 존재하고 런타임 Resources.Load로만
    /// 로드되는 LilToonDissolveKeepAlive.mat 은 스캔에 잡히지 않는다.
    /// 그 결과 LIL_FEATURE_DISSOLVE 가 빌드에서 컴파일아웃되어 DissolveController 의 lilToon 디졸브가
    /// "에디터에선 동작하지만 빌드에선 동작하지 않는" 증상이 발생한다.
    ///
    /// 이 유틸리티는 빌드 씬(Boot)에 keep-alive 머티리얼을 참조하는 "비활성" GameObject 를 1개 배치해
    /// lilToon 최적화 스캔이 _DissolveParams.x != 기본값 인 머티리얼을 발견하도록 만든다.
    /// (lilToonSetting.SetupShaderSettingFromMaterial: _DissolveParams.x 가 기본값과 다르면 LIL_FEATURE_DISSOLVE 유지)
    /// 비활성이라 실제 렌더링/성능 영향은 없고, dissolve 기능만 빌드에 유지되므로 빌드 크기 영향도 최소다.
    /// </summary>
    public static class LilToonDissolveKeepAliveSetup
    {
        // Boot 씬 (EditorBuildSettings 의 첫 빌드 씬). lilToon 스캔은 모든 빌드 씬을 순회하므로 하나에만 있으면 충분.
        private const string BootScenePath = "Assets/01.Scenes/GameLogic/Boot.unity";
        private const string KeepAliveMaterialPath = "Assets/Resources/Rendering/LilToonDissolveKeepAlive.mat";
        private const string KeepAliveObjectName = "[lilToonDissolveKeepAlive]";

        [MenuItem("Tools/Dissolve/lilToon Dissolve KeepAlive 보장", false, 0)]
        public static void EnsureKeepAlive()
        {
            var keepAliveMat = AssetDatabase.LoadAssetAtPath<Material>(KeepAliveMaterialPath);
            if (keepAliveMat == null)
            {
                EditorUtility.DisplayDialog("실패",
                    $"keep-alive 머티리얼을 찾을 수 없습니다.\n{KeepAliveMaterialPath}", "확인");
                return;
            }

            if (!keepAliveMat.HasProperty("_DissolveParams") ||
                Mathf.Approximately(keepAliveMat.GetVector("_DissolveParams").x, 0f))
            {
                bool fix = EditorUtility.DisplayDialog("경고",
                    "keep-alive 머티리얼의 _DissolveParams.x 가 기본값(0)입니다.\n" +
                    "이 값이 0이면 lilToon 최적화가 dissolve 를 유지하지 않습니다.\n" +
                    "_DissolveParams.x 를 3(Position 모드)으로 설정할까요?", "설정", "취소");
                if (!fix) return;
                var p = keepAliveMat.GetVector("_DissolveParams");
                p.x = 3f;
                keepAliveMat.SetVector("_DissolveParams", p);
                EditorUtility.SetDirty(keepAliveMat);
                AssetDatabase.SaveAssets();
            }

            // 작업 전 현재 씬 구성 저장 후 복원
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var previousSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                var scene = EditorSceneManager.OpenScene(BootScenePath, OpenSceneMode.Single);
                if (!scene.IsValid())
                {
                    EditorUtility.DisplayDialog("실패", $"Boot 씬을 열 수 없습니다.\n{BootScenePath}", "확인");
                    return;
                }

                var existing = FindKeepAliveObject(scene);
                bool created = existing == null;
                if (created)
                {
                    existing = new GameObject(KeepAliveObjectName);
                    SceneManager.MoveGameObjectToScene(existing, scene);
                }

                var meshRenderer = existing.GetComponent<MeshRenderer>();
                if (meshRenderer == null)
                    meshRenderer = existing.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterial = keepAliveMat;

                // 비활성: 실제 렌더링/성능 영향 제거. 비활성 루트도 lilToon 스캔(GetRootGameObjects)에 포함된다.
                existing.SetActive(false);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);

                Debug.Log($"[LilToonDissolveKeepAlive] {(created ? "생성" : "갱신")} 완료 — " +
                          $"'{KeepAliveObjectName}' (비활성) 이 Boot 씬에서 keep-alive 머티리얼을 참조합니다. " +
                          "이제 빌드 시 LIL_FEATURE_DISSOLVE 가 유지됩니다.");
                EditorUtility.DisplayDialog("완료",
                    $"Boot 씬에 keep-alive 오브젝트를 {(created ? "생성" : "갱신")}했습니다.\n" +
                    "빌드 시 lilToon dissolve 가 유지됩니다.", "확인");
            }
            finally
            {
                if (previousSetup != null && previousSetup.Length > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }
        }

        private static GameObject FindKeepAliveObject(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == KeepAliveObjectName)
                    return root;
            }
            return null;
        }
    }
}
