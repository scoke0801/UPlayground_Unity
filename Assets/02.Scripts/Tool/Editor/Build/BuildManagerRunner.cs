using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Stopwatch = System.Diagnostics.Stopwatch;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace UPlayGround.Tool.Editor.Build
{
    /// <summary>빌드 매니저의 실행 결과.</summary>
    public sealed class BuildManagerResult
    {
        public bool Success;
        public string Summary;
        public BuildReport Report;
        public TimeSpan Elapsed;
        public List<BuildIssue> Issues = new();
    }

    /// <summary>
    /// 프로필·모드에 따라 콘텐츠 빌드, 플레이어 빌드, 콘텐츠 교체를 수행한다.
    /// 사용자 지시 없이 호출하지 않는다. Player·Addressables 빌드는 실행 비용이 크고 AGENTS.md가 별도 지시를 요구한다.
    /// </summary>
    public static class BuildManagerRunner
    {
        /// <summary>기존 검증 도구의 출력·씬·옵션을 유지하면서 공통 점검과 콘텐츠 빌드를 거친다.</summary>
        public static BuildReport RunValidation(BuildPlayerOptions options, BuildManagerMode mode = BuildManagerMode.Full)
        {
            if (mode == BuildManagerMode.ContentOnly)
                throw new ArgumentException("검증 Player 진입점에서는 콘텐츠만 모드를 사용할 수 없습니다.", nameof(mode));
            var profile = ScriptableObject.CreateInstance<BuildManagerProfileSO>();
            try
            {
                string directory = Path.GetDirectoryName(Path.GetFullPath(options.locationPathName));
                profile.outputRoot = Path.GetDirectoryName(directory);
                profile.profileName = Path.GetFileName(directory);
                profile.executableName = Path.GetFileNameWithoutExtension(options.locationPathName);
                profile.buildTarget = options.target;
                profile.developmentBuild = (options.options & BuildOptions.Development) != 0;
                profile.buildOptions = options.options & ~BuildOptions.Development;
                foreach (string path in options.scenes)
                {
                    var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                    if (scene == null) throw new ArgumentException("씬 파일이 없습니다: " + path);
                    profile.scenes.Add(scene);
                }
                BuildManagerResult result = Run(profile, mode, allowStaleContent: false);
                // 기존 진입점이 실패한 Player의 개별 보고서도 기록할 수 있게 결과를 돌려준다.
                if (!result.Success && result.Report != null && result.Report.summary.result != BuildResult.Succeeded)
                    return result.Report;
                if (!result.Success)
                    throw new InvalidOperationException(result.Summary + "\n" + string.Join("\n", result.Issues.ConvertAll(issue => issue.Message)));
                return result.Report;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        /// <summary>현재 에디터 상태를 읽어 사전 점검 입력을 만든다.</summary>
        public static BuildPreflightInput CreateInput(BuildManagerProfileSO profile, BuildManagerMode mode, bool allowStaleContent)
        {
            string outputDirectory = profile.GetOutputDirectory();
            string executable = BuildManagerFiles.GetExecutablePath(outputDirectory, profile.executableName);
            string catalogDirectory = Addressables.BuildPath;
            string cachedCatalogHash = BuildManagerFiles.ComputeCatalogHash(catalogDirectory);

            return new BuildPreflightInput
            {
                Mode = mode,
                IsPlayingOrChangingMode = EditorApplication.isPlayingOrWillChangePlaymode,
                HasUnsavedScene = HasDirtyScene(),
                IsCompiling = EditorApplication.isCompiling || EditorApplication.isUpdating,
                ExpectedUnityVersion = Application.unityVersion,
                ExpectedBuildTarget = profile.buildTarget.ToString(),
                ExpectedExecutableName = profile.executableName,
                CurrentScriptsHash = ComputePlayerScriptsHash(),
                CurrentPlayerSettingsHash = BuildManagerFingerprint.ComputePlayerSettings(profile),
                CurrentContentSourceHash = BuildManagerFingerprint.ComputeContent(AddressableAssetSettingsDefaultObject.Settings),
                BuildTargetMatches = EditorUserBuildSettings.activeBuildTarget == profile.buildTarget,
                IsSupportedTarget = profile.buildTarget == BuildTarget.StandaloneWindows64,
                Scenes = profile.GetScenePaths(),
                SceneExists = path => File.Exists(path),
                PlayerManifest = BuildManagerFiles.ReadManifest(outputDirectory),
                PlayerAaFolderExists = Directory.Exists(BuildManagerFiles.GetPlayerAaDirectory(executable)),
                PlayerExecutableExists = File.Exists(executable),
                PlayerCatalogHash = BuildManagerFiles.ComputeCatalogHash(BuildManagerFiles.GetPlayerAaDirectory(executable)),
                CachedContentManifest = BuildManagerFiles.ReadManifest(BuildManagerFingerprint.GetContentRecordDirectory(profile.buildTarget)),
                CachedCatalogHash = cachedCatalogHash,
                CatalogExists = !string.IsNullOrEmpty(cachedCatalogHash),
                CatalogWriteUtc = GetCatalogWriteUtc(catalogDirectory),
                LatestContentSourceUtc = GetLatestContentSourceUtc(),
                AllowStaleContent = allowStaleContent,
            };
        }

        public static BuildManagerResult Run(BuildManagerProfileSO profile, BuildManagerMode mode, bool allowStaleContent)
        {
            var timing = new BuildTiming();
            var result = new BuildManagerResult();
            if (profile == null)
            {
                result.Summary = "빌드 프로필이 지정되지 않았습니다.";
                return result;
            }

            if (EditorUserBuildSettings.activeBuildTarget != profile.buildTarget)
            {
                result.Summary = $"활성 빌드 대상({EditorUserBuildSettings.activeBuildTarget})이 프로필({profile.buildTarget})과 다릅니다. 플랫폼을 먼저 전환하세요.";
                return result;
            }

            BuildPreflightInput input = CreateInput(profile, mode, allowStaleContent);
            result.Issues = BuildPreflight.Evaluate(input);
            if (BuildPreflight.HasError(result.Issues))
            {
                result.Summary = "사전 점검에서 막혔습니다.";
                return result;
            }

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                result.Summary = "Addressables 설정이 없습니다.";
                return result;
            }

            EditorBuildSettingsScene[] previousScenes = EditorBuildSettings.scenes;
            const string buildSettingsPath = "ProjectSettings/EditorBuildSettings.asset";
            bool buildsPlayer = mode != BuildManagerMode.ContentOnly;
            bool replacesScenes = buildsPlayer && !MatchesBuildScenes(previousScenes, input.Scenes);
            byte[] previousBuildSettings = File.ReadAllBytes(buildSettingsPath);
            AddressableAssetSettings.PlayerBuildOption previousOption = settings.BuildAddressablesWithPlayerBuild;
            bool overridesAddressables = buildsPlayer && previousOption != AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;
            timing.Begin("빌드 설정 준비");
            try
            {
                // lilToon 전처리가 BuildPlayerOptions가 아닌 전역 씬 목록을 읽으므로 빌드 중에만 교체한다.
                if (replacesScenes)
                    EditorBuildSettings.scenes = ToBuildScenes(input.Scenes);
                // 콘텐츠는 이 도구가 명시적으로 만든다. 플레이어 빌드가 환경설정에 따라 다시 만들지 않게 막는다.
                if (overridesAddressables)
                    settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;

                RunMode(profile, mode, input, result, timing);
            }
            catch (Exception exception)
            {
                result.Success = false;
                result.Summary = "빌드 실패: " + exception.Message;
                Debug.LogException(exception);
            }
            finally
            {
                timing.Begin("빌드 설정 복원");
                if (settings.BuildAddressablesWithPlayerBuild != previousOption)
                    settings.BuildAddressablesWithPlayerBuild = previousOption;
                if (!EditorBuildSettings.scenes.Select(scene => (scene.path, scene.enabled))
                        .SequenceEqual(previousScenes.Select(scene => (scene.path, scene.enabled))))
                    EditorBuildSettings.scenes = previousScenes;
                if (!File.Exists(buildSettingsPath) || !File.ReadAllBytes(buildSettingsPath).SequenceEqual(previousBuildSettings))
                    File.WriteAllBytes(buildSettingsPath, previousBuildSettings);
                AssetDatabase.SaveAssetIfDirty(settings);
            }

            timing.Finish(profile, mode, result);
            return result;
        }

        private static void RunMode(BuildManagerProfileSO profile, BuildManagerMode mode, BuildPreflightInput input, BuildManagerResult result, BuildTiming timing)
        {
            string outputDirectory = profile.GetOutputDirectory();
            string executable = BuildManagerFiles.GetExecutablePath(outputDirectory, profile.executableName);

            if (mode != BuildManagerMode.PlayerOnly)
            {
                timing.Begin("Addressables 빌드");
                BuildContent();
                timing.Begin("콘텐츠 빌드 기록 저장");
                BuildManagerFiles.WriteManifest(BuildManagerFingerprint.GetContentRecordDirectory(profile.buildTarget), new BuildManifest
                {
                    unityVersion = input.ExpectedUnityVersion, buildTarget = input.ExpectedBuildTarget,
                    scriptsHash = input.CurrentScriptsHash, contentSourceHash = input.CurrentContentSourceHash,
                    catalogHash = BuildManagerFiles.ComputeCatalogHash(Addressables.BuildPath),
                    contentBuiltAtUtc = DateTime.UtcNow.ToString("o"),
                });
            }

            if (mode == BuildManagerMode.ContentOnly)
            {
                timing.Begin("콘텐츠 배포");
                BuildManagerFiles.DeployContent(Addressables.BuildPath, BuildManagerFiles.GetPlayerAaDirectory(executable));
                BuildManifest manifest = input.PlayerManifest;
                manifest.contentBuiltAtUtc = DateTime.UtcNow.ToString("o");
                manifest.catalogHash = BuildManagerFiles.ComputeCatalogHash(Addressables.BuildPath);
                manifest.contentSourceHash = input.CurrentContentSourceHash;
                BuildManagerFiles.WriteManifest(outputDirectory, manifest);
                result.Success = true;
                result.Summary = "콘텐츠를 기존 플레이어에 반영했습니다. 플레이어는 다시 빌드하지 않았습니다.";
                return;
            }

            timing.Begin("Player 빌드");
            BuildReport report = BuildPlayer(profile, input.Scenes, executable);
            result.Report = report;
            timing.Begin("Player 빌드 기록 저장");
            BuildSummary summary = report.summary;
            string text = $"결과: {summary.result}\nUnity: {Application.unityVersion}\n플랫폼: {summary.platform}\n" +
                          $"오류: {summary.totalErrors}\n경고: {summary.totalWarnings}\n크기: {summary.totalSize} bytes\n시간: {summary.totalTime}\n실행: {summary.outputPath}\n";
            Directory.CreateDirectory(outputDirectory);
            File.WriteAllText(Path.Combine(outputDirectory, "BuildReport.txt"), text);
            if (summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Player 빌드 실패:\n" + text);

            WritePlayerManifest(profile, input, outputDirectory);
            result.Success = true;
            result.Summary = mode == BuildManagerMode.Full ? "콘텐츠와 플레이어를 빌드했습니다." : "기존 콘텐츠로 플레이어를 빌드했습니다.";
        }

        private sealed class BuildTiming
        {
            private readonly DateTime _startedAt = DateTime.UtcNow;
            private readonly Stopwatch _clock = Stopwatch.StartNew();
            private readonly StringBuilder _steps = new();
            private string _stage = "사전 점검";
            private TimeSpan _stageStarted;

            public void Begin(string stage)
            {
                RecordCurrentStage();
                _stage = stage;
            }

            private void RecordCurrentStage()
            {
                TimeSpan now = _clock.Elapsed;
                _steps.AppendLine($"{_stage}: {now - _stageStarted:c}");
                _stageStarted = now;
            }

            public void Finish(BuildManagerProfileSO profile, BuildManagerMode mode, BuildManagerResult result)
            {
                _clock.Stop();
                RecordCurrentStage();
                result.Elapsed = _clock.Elapsed;
                var text = new StringBuilder();
                text.AppendLine($"{_startedAt:O} | {profile.profileName} | {mode} | {profile.buildTarget} | {profile.GetBuildOptions()}");
                text.AppendLine($"결과: {(result.Success ? "성공" : "실패")} | 총 시간: {result.Elapsed:c}");
                text.Append(_steps);
                if (!result.Success)
                    text.AppendLine(result.Summary);
                if (result.Report != null)
                {
                    text.AppendLine("Unity Player 세부 단계 (하위 단계는 상위 시간에 포함)");
                    foreach (BuildStep step in result.Report.steps)
                        text.Append(' ', Math.Max(0, step.depth) * 2).AppendLine($"{step.name}: {step.duration:c}");
                }
                text.AppendLine();

                try
                {
                    string directory = profile.GetOutputDirectory();
                    Directory.CreateDirectory(directory);
                    File.AppendAllText(Path.Combine(directory, "BuildTimings.log"), text.ToString());
                }
                catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
                {
                    Debug.LogWarning("빌드 시간 기록을 저장하지 못했습니다: " + exception.Message);
                }
            }
        }

        private static void BuildContent()
        {
            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult contentResult);
            if (!string.IsNullOrEmpty(contentResult.Error))
                throw new InvalidOperationException("Addressables 빌드 실패: " + contentResult.Error);
        }

        private static BuildReport BuildPlayer(BuildManagerProfileSO profile, IReadOnlyList<string> scenes, string executable)
        {
            var sceneArray = new string[scenes.Count];
            for (int i = 0; i < scenes.Count; i++)
                sceneArray[i] = scenes[i];

            BuildOptions options = profile.GetBuildOptions();

            return BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = sceneArray,
                locationPathName = executable,
                target = profile.buildTarget,
                options = options,
            });
        }

        private static void WritePlayerManifest(BuildManagerProfileSO profile, BuildPreflightInput input, string outputDirectory)
        {
            string catalogDirectory = Addressables.BuildPath;
            var scenes = new string[input.Scenes.Count];
            for (int i = 0; i < scenes.Length; i++)
                scenes[i] = input.Scenes[i];

            BuildManagerFiles.WriteManifest(outputDirectory, new BuildManifest
            {
                profileName = profile.profileName,
                executableName = profile.executableName,
                unityVersion = input.ExpectedUnityVersion,
                buildTarget = input.ExpectedBuildTarget,
                scriptsHash = input.CurrentScriptsHash,
                playerSettingsHash = input.CurrentPlayerSettingsHash,
                contentSourceHash = GetBuiltContentSourceHash(input),
                scenes = scenes,
                playerBuiltAtUtc = DateTime.UtcNow.ToString("o"),
                contentBuiltAtUtc = GetCatalogWriteUtc(catalogDirectory)?.ToString("o") ?? string.Empty,
                catalogHash = BuildManagerFiles.ComputeCatalogHash(catalogDirectory),
            });
        }

        private static string GetBuiltContentSourceHash(BuildPreflightInput input) => input.Mode == BuildManagerMode.PlayerOnly
            ? input.CachedContentManifest?.contentSourceHash ?? string.Empty : input.CurrentContentSourceHash;

        /// <summary>플레이어에 들어가는 어셈블리(테스트 제외)의 컴파일 결과 해시. 에디터 전용 코드 변경은 반영하지 않는다.</summary>
        public static string ComputePlayerScriptsHash()
        {
            var paths = new List<string>();
            foreach (Assembly assembly in CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies))
                paths.Add(assembly.outputPath);
            return BuildManagerFiles.ComputeFilesHash(paths);
        }

        private static DateTime? GetCatalogWriteUtc(string directory)
        {
            if (!Directory.Exists(directory))
                return null;
            DateTime? latest = null;
            foreach (string file in Directory.GetFiles(directory, "catalog*"))
            {
                DateTime written = File.GetLastWriteTimeUtc(file);
                if (!latest.HasValue || written > latest.Value)
                    latest = written;
            }

            return latest;
        }

        /// <summary>주소 지정 에셋 원본 파일의 최신 수정 시각. 프리팹이 참조하는 하위 에셋 변경은 잡지 못하는 근사치다.</summary>
        private static DateTime? GetLatestContentSourceUtc()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                return null;

            DateTime? latest = null;
            // 그룹 이동·묶음 방식 변경도 기존 카탈로그 재사용을 막아야 한다.
            foreach (string guid in AssetDatabase.FindAssets("", new[] { "Assets/AddressableAssetsData/AssetGroups" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase) && File.Exists(path))
                {
                    DateTime written = File.GetLastWriteTimeUtc(path);
                    if (!latest.HasValue || written > latest.Value) latest = written;
                }
            }
            foreach (AddressableAssetGroup group in settings.groups)
            {
                if (group == null)
                    continue;
                foreach (AddressableAssetEntry entry in group.entries)
                {
                    if (!File.Exists(entry.AssetPath))
                        continue;
                    DateTime written = File.GetLastWriteTimeUtc(entry.AssetPath);
                    if (!latest.HasValue || written > latest.Value)
                        latest = written;
                }
            }

            return latest;
        }

        private static bool MatchesBuildScenes(EditorBuildSettingsScene[] current, IReadOnlyList<string> paths)
        {
            if (current.Length != paths.Count)
                return false;
            for (int i = 0; i < current.Length; i++)
            {
                if (!current[i].enabled || current[i].path != paths[i])
                    return false;
            }
            return true;
        }

        private static EditorBuildSettingsScene[] ToBuildScenes(IReadOnlyList<string> paths)
        {
            var scenes = new EditorBuildSettingsScene[paths.Count];
            for (int i = 0; i < paths.Count; i++)
                scenes[i] = new EditorBuildSettingsScene(paths[i], true);
            return scenes;
        }

        private static bool HasDirtyScene()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).isDirty)
                    return true;
            }

            return false;
        }
    }
}
