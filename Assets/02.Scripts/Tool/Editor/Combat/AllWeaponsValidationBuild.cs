using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.EditorTools;

namespace UPlayGround.Tool.Editor
{
    /// <summary>전투 구도 검증 하나와 로컬 리소스를 Windows Development Player로 패키징한다.</summary>
    public static class AllWeaponsValidationBuild
    {
        /// <summary>일반 게임의 씬 목록을 보존하고 시험장 전용 실행 파일을 만든다.</summary>
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Play Mode를 종료한 뒤 빌드하세요.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (!Application.isBatchMode && SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("열린 씬의 변경 내용을 저장한 뒤 빌드하세요.");
            }

            string directory = Path.GetFullPath("Builds/AllWeaponsApproach_20261021");
            Directory.CreateDirectory(directory);
            SceneSetup[] previousScenes = EditorSceneManager.GetSceneManagerSetup();
            EditorBuildSettingsScene[] previousBuildScenes = EditorBuildSettings.scenes;
            const string buildSettingsPath = "ProjectSettings/EditorBuildSettings.asset";
            byte[] previousBuildSettings = File.ReadAllBytes(buildSettingsPath);
            string previousProductName = PlayerSettings.productName;
            byte[] previousPlayerSettings = File.ReadAllBytes("ProjectSettings/ProjectSettings.asset");
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                throw new InvalidOperationException("Addressables 설정이 없습니다.");
            var previousCrcSettings = new List<(BundledAssetGroupSchema Schema, bool UseCrc)>();
            AddressableAssetSettings.PlayerBuildOption previousContentBuild = settings.BuildAddressablesWithPlayerBuild;
            bool previousThreaded = UnityEditor.Build.Pipeline.Utilities.ScriptableBuildPipeline.threadedArchiving;
            try
            {
                UnityEditor.Build.Pipeline.Utilities.ScriptableBuildPipeline.threadedArchiving = false;
                // lilToon 전처리도 BuildPlayerOptions가 아닌 전역 씬 목록을 사용한다.
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/01.Scenes/Test/CombatTestMap.unity", true) };
                EditorSceneManager.OpenScene("Assets/01.Scenes/Test/CombatTestMap.unity");
                PlayerSettings.productName = "UPlaygroundAllWeaponsApproachValidation_20261021";
                ConfigureLocalBundles(settings, previousCrcSettings);
                // 에디터의 AssetDatabase 재생 모드는 독립 실행 파일에서 사용할 수 없다.
                if (Array.IndexOf(Environment.GetCommandLineArgs(), "-reuseCombatContent") < 0)
                {
                    AddressableAssetSettings.BuildPlayerContent(out var contentResult);
                    if (!string.IsNullOrEmpty(contentResult.Error))
                        throw new InvalidOperationException("Addressables 빌드 실패: " + contentResult.Error);
                }
                settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;

                BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { "Assets/01.Scenes/Test/CombatTestMap.unity" },
                    locationPathName = Path.Combine(directory, "CombatEncounterValidation.exe"),
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.Development | BuildOptions.CompressWithLz4
                });
                BuildSummary summary = report.summary;
                string result = $"결과: {summary.result}\nUnity: {Application.unityVersion}\n" +
                    $"씬: {"Assets/01.Scenes/Test/CombatTestMap.unity"}\n플랫폼: {summary.platform}\n" +
                    $"Backend: {PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone)}\n옵션: {summary.options}\n오류: {summary.totalErrors}\n경고: {summary.totalWarnings}\n" +
                    $"크기: {summary.totalSize} bytes\n시간: {summary.totalTime}\n실행: {summary.outputPath}\n";
                File.WriteAllText(Path.Combine(directory, "BuildReport.txt"), result);
                if (summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("전투 구도 검증 Player 빌드 실패: " + result);
                Debug.Log("[AllWeaponsValidationBuild] " + result);
            }
            finally
            {
                UnityEditor.Build.Pipeline.Utilities.ScriptableBuildPipeline.threadedArchiving = previousThreaded;
                settings.BuildAddressablesWithPlayerBuild = previousContentBuild;
                foreach (var entry in previousCrcSettings)
                {
                    entry.Schema.UseAssetBundleCrc = entry.UseCrc;
                    AssetDatabase.SaveAssetIfDirty(entry.Schema);
                }
                AssetDatabase.SaveAssetIfDirty(settings);
                EditorBuildSettings.scenes = previousBuildScenes;
                File.WriteAllBytes(buildSettingsPath, previousBuildSettings);
                PlayerSettings.productName = previousProductName;
                AssetDatabase.SaveAssets();
                File.WriteAllBytes("ProjectSettings/ProjectSettings.asset", previousPlayerSettings);
                // 배치 에디터는 시작 시 열린 씬이 없으므로 복원할 구성이 없다.
                if (Array.Exists(previousScenes, scene => scene.isLoaded && scene.isActive && !string.IsNullOrEmpty(scene.path)))
                    EditorSceneManager.RestoreSceneManagerSetup(previousScenes);
                else if (!Application.isBatchMode)
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        private static void ConfigureLocalBundles(AddressableAssetSettings settings,
            List<(BundledAssetGroupSchema Schema, bool UseCrc)> previousSettings)
        {
            foreach (AddressableAssetGroup group in settings.groups)
            {
                BundledAssetGroupSchema schema = group != null ? group.GetSchema<BundledAssetGroupSchema>() : null;
                if (schema == null || !schema.IncludeInBuild)
                    continue;
                string loadPath = settings.profileSettings.GetValueById(settings.activeProfileId, schema.LoadPath.Id);
                if (!loadPath.StartsWith("{UnityEngine.AddressableAssets.Addressables.RuntimePath}", StringComparison.Ordinal))
                    continue;
                previousSettings.Add((schema, schema.UseAssetBundleCrc));
                // 설치에 포함한 LZ4 번들의 전체 CRC 검사는 작은 설정 하나를 읽어도 전체 번들을 해제해
                // 시작 제한시간을 소모한다. 네트워크 번들은 그대로 검사하며 원본 그룹 설정도 복원한다.
                schema.UseAssetBundleCrc = false;
            }
        }
    }
}


