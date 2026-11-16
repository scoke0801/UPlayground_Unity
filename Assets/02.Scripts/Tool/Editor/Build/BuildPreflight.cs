using System;
using System.Collections.Generic;

namespace UPlayGround.Tool.Editor.Build
{
    /// <summary>
    /// 빌드 작업 전에 실패가 확정된 조건을 미리 걸러낸다. 에디터 API를 호출하지 않아 EditMode 테스트로 검증한다.
    /// </summary>
    public static class BuildPreflight
    {
        /// <summary>선택한 폴더의 기록·파일과 현재 프로필을 비교해 필요한 빌드를 안내한다.</summary>
        public static BuildAssessment Assess(BuildPreflightInput input)
        {
            var result = new BuildAssessment();
            var playerIssues = new List<BuildIssue>();
            CheckContentDeployTarget(input, playerIssues);
            result.PlayerNeedsBuild = playerIssues.Count > 0;
            foreach (BuildIssue issue in playerIssues) result.Reasons.Add(issue.Message);
            BuildManifest player = input.PlayerManifest;
            result.ContentNeedsBuild = player == null || string.IsNullOrEmpty(player.contentSourceHash)
                || player.contentSourceHash != input.CurrentContentSourceHash
                || string.IsNullOrEmpty(input.PlayerCatalogHash) || input.PlayerCatalogHash != player.catalogHash;
            if (result.ContentNeedsBuild)
                result.Reasons.Add("대상 폴더의 번들 기록이 없거나 콘텐츠 입력·카탈로그가 달라졌습니다.");
            if (result.PlayerNeedsBuild)
            {
                bool reuse = CanReuseCachedContent(input);
                result.RecommendedMode = result.ContentNeedsBuild || !reuse ? BuildManagerMode.Full : BuildManagerMode.PlayerOnly;
                if (!result.ContentNeedsBuild && !reuse)
                    result.Reasons.Add("재사용할 로컬 콘텐츠 빌드 기록이 없어 전체 빌드가 필요합니다.");
            }
            else if (result.ContentNeedsBuild) result.RecommendedMode = BuildManagerMode.ContentOnly;
            return result;
        }

        public static bool CanReuseCachedContent(BuildPreflightInput input)
        {
            BuildManifest content = input.CachedContentManifest;
            return input.CatalogExists && content != null && !string.IsNullOrEmpty(content.contentSourceHash)
                && content.contentSourceHash == input.CurrentContentSourceHash
                && content.unityVersion == input.ExpectedUnityVersion && content.buildTarget == input.ExpectedBuildTarget
                && content.scriptsHash == input.CurrentScriptsHash && content.catalogHash == input.CachedCatalogHash;
        }

        public static List<BuildIssue> Evaluate(BuildPreflightInput input)
        {
            var issues = new List<BuildIssue>();
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            CheckEditorState(input, issues);
            CheckScenes(input, issues);

            switch (input.Mode)
            {
                case BuildManagerMode.ContentOnly:
                    CheckContentDeployTarget(input, issues);
                    break;
                case BuildManagerMode.PlayerOnly:
                    CheckExistingContent(input, issues);
                    break;
            }

            return issues;
        }

        public static bool HasError(IReadOnlyList<BuildIssue> issues)
        {
            for (int i = 0; i < issues.Count; i++)
            {
                if (issues[i].Severity == BuildIssueSeverity.Error)
                    return true;
            }

            return false;
        }

        private static void CheckEditorState(BuildPreflightInput input, List<BuildIssue> issues)
        {
            if (input.IsPlayingOrChangingMode)
                issues.Add(Error("Play Mode를 종료한 뒤 빌드하세요."));
            if (input.HasUnsavedScene)
                issues.Add(Error("열린 씬의 변경 내용을 저장한 뒤 빌드하세요."));
            if (input.IsCompiling)
                issues.Add(Error("컴파일과 에셋 임포트가 끝난 뒤 빌드하세요."));
            if (!input.BuildTargetMatches)
                issues.Add(Error("활성 플랫폼이 프로필과 다릅니다. Unity에서 플랫폼을 먼저 전환하세요."));
            if (!input.IsSupportedTarget)
                issues.Add(Error("현재 빌드 매니저는 Windows 64비트 exe 출력만 지원합니다."));
        }

        private static void CheckScenes(BuildPreflightInput input, List<BuildIssue> issues)
        {
            if (input.Scenes == null || input.Scenes.Count == 0)
            {
                issues.Add(Error("프로필의 씬 목록이 비어 있습니다."));
                return;
            }

            var unique = new HashSet<string>(StringComparer.Ordinal);
            foreach (string scene in input.Scenes)
            {
                if (string.IsNullOrEmpty(scene) || !input.SceneExists(scene))
                    issues.Add(Error($"씬 파일이 없습니다: {scene}"));
                if (!unique.Add(scene)) issues.Add(Error($"씬이 중복되었습니다: {scene}"));
            }
        }

        private static void CheckContentDeployTarget(BuildPreflightInput input, List<BuildIssue> issues)
        {
            BuildManifest player = input.PlayerManifest;
            if (player == null)
            {
                issues.Add(Error("대상 플레이어의 빌드 기록이 없습니다. 전체 빌드를 먼저 실행하세요."));
                return;
            }

            if (!input.PlayerAaFolderExists)
                issues.Add(Error("대상 플레이어에 StreamingAssets/aa 폴더가 없습니다. 전체 빌드를 먼저 실행하세요."));
            if (!input.PlayerExecutableExists)
                issues.Add(Error("선택한 폴더에 지정한 exe가 없습니다. 플레이어 빌드가 필요합니다."));
            if (player.executableName != input.ExpectedExecutableName)
                issues.Add(Error("빌드 기록의 exe 이름이 현재 설정과 다릅니다. exe를 다시 빌드하세요."));
            if (player.unityVersion != input.ExpectedUnityVersion)
                issues.Add(Error($"Unity 버전이 다릅니다: 플레이어 {player.unityVersion}, 현재 {input.ExpectedUnityVersion}. exe 재빌드가 필요합니다."));
            if (player.buildTarget != input.ExpectedBuildTarget)
                issues.Add(Error($"빌드 대상이 다릅니다: 플레이어 {player.buildTarget}, 현재 {input.ExpectedBuildTarget}."));
            if (player.scriptsHash != input.CurrentScriptsHash)
                issues.Add(Error("플레이어 빌드 이후 코드가 바뀌었습니다. 번들만 교체할 수 없으며 exe 재빌드가 필요합니다."));
            if (!SameScenes(player.scenes, input.Scenes))
                issues.Add(Error("플레이어 빌드 이후 씬 목록이나 순서가 바뀌었습니다. exe 재빌드가 필요합니다."));
            if (string.IsNullOrEmpty(player.playerSettingsHash) || player.playerSettingsHash != input.CurrentPlayerSettingsHash)
                issues.Add(Error("Dev 옵션·씬 내용·Player 설정이 달라졌거나 비교 기록이 없습니다. exe를 다시 빌드하세요."));
        }

        private static void CheckExistingContent(BuildPreflightInput input, List<BuildIssue> issues)
        {
            if (!input.CatalogExists)
            {
                issues.Add(Error("콘텐츠 카탈로그가 없습니다. 이 상태로 플레이어를 만들면 실행 시 Addressables 로드가 실패합니다. 콘텐츠를 먼저 빌드하세요."));
                return;
            }

            if (!CanReuseCachedContent(input))
            {
                var severity = input.AllowStaleContent ? BuildIssueSeverity.Warning : BuildIssueSeverity.Error;
                issues.Add(new BuildIssue(severity, "캐시된 콘텐츠의 입력·코드·대상·카탈로그 기록이 현재와 다릅니다. 전체 빌드를 권장합니다."));
            }

            if (input.CatalogWriteUtc.HasValue && input.LatestContentSourceUtc.HasValue
                && input.CatalogWriteUtc.Value < input.LatestContentSourceUtc.Value)
            {
                var severity = input.AllowStaleContent ? BuildIssueSeverity.Warning : BuildIssueSeverity.Error;
                issues.Add(new BuildIssue(severity,
                    "마지막 콘텐츠 빌드 이후 주소 지정 에셋이 수정됐습니다. 콘텐츠를 다시 빌드하거나 '오래된 콘텐츠 허용'을 켜세요."));
            }
        }

        private static bool SameScenes(IReadOnlyList<string> recorded, IReadOnlyList<string> current)
        {
            if (recorded == null || current == null || recorded.Count != current.Count)
                return false;
            for (int i = 0; i < recorded.Count; i++)
            {
                if (!string.Equals(recorded[i], current[i], StringComparison.Ordinal))
                    return false;
            }

            return true;
        }

        private static BuildIssue Error(string message)
        {
            return new BuildIssue(BuildIssueSeverity.Error, message);
        }
    }
}
