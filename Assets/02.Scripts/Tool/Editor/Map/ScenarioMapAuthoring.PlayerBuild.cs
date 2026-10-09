using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UPlayGround.EditorTools;

namespace UPlayGround.Tool.Editor.Map
{
    public static partial class ScenarioMapAuthoring
    {
        [Serializable] private sealed class PlayerBuildReport
        {
            public string result;
            public string unityVersion;
            public string executable;
            public string[] scenes;
            public int errors;
            public int warnings;
            public long bytes;
            public double seconds;
        }

        /// <summary>등록된 게임 씬과 새 맵을 Windows Development Player로 빌드 검증한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/맵/시나리오 맵 플레이어 빌드 검증")]
        public static void BuildScenarioPlayer()
        {
            RequireEditMode();
            Layout layout = ReadLayout();
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(layout.scenePath);
            Validate();
            var scenes = new System.Collections.Generic.List<string>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
                if (scene.enabled) scenes.Add(scene.path);
            if (!scenes.Contains(layout.scenePath)) throw new InvalidOperationException("새 맵을 빌드 씬 목록에 먼저 연결하세요.");
            Directory.CreateDirectory(ReportDirectory + "/Player");
            string executable = Path.GetFullPath(ReportDirectory + "/Player/ScenarioMap.exe");
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = executable,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development | BuildOptions.CompressWithLz4
            });
            BuildSummary summary = report.summary;
            var result = new PlayerBuildReport
            {
                result = summary.result.ToString(), unityVersion = Application.unityVersion,
                executable = executable, scenes = scenes.ToArray(), errors = (int)summary.totalErrors,
                warnings = (int)summary.totalWarnings, bytes = (long)summary.totalSize, seconds = summary.totalTime.TotalSeconds
            };
            File.WriteAllText(ReportDirectory + "/PlayerBuild.json", JsonUtility.ToJson(result, true));
            if (summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Player Build 실패: " + summary.result);
            Debug.Log($"[ScenarioMap] Windows Player Build 통과: 오류 {summary.totalErrors}, 경고 {summary.totalWarnings}");
        }
    }
}
