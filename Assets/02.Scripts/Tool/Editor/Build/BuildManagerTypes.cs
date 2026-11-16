using System;
using System.Collections.Generic;

namespace UPlayGround.Tool.Editor.Build
{
    /// <summary>빌드 매니저가 수행하는 작업 종류.</summary>
    public enum BuildManagerMode
    {
        /// <summary>콘텐츠(번들)를 새로 만든 뒤 플레이어를 빌드한다.</summary>
        Full,

        /// <summary>콘텐츠만 새로 만들어 기존 플레이어의 번들 폴더에 덮어쓴다. 코드·씬 목록이 같을 때만 허용한다.</summary>
        ContentOnly,

        /// <summary>마지막 콘텐츠 빌드 결과를 그대로 사용해 플레이어만 빌드한다.</summary>
        PlayerOnly,
    }

    public enum BuildIssueSeverity
    {
        Warning,
        Error,
    }

    /// <summary>사전 점검에서 발견한 문제 하나.</summary>
    public readonly struct BuildIssue
    {
        public readonly BuildIssueSeverity Severity;
        public readonly string Message;

        public BuildIssue(BuildIssueSeverity severity, string message)
        {
            Severity = severity;
            Message = message;
        }
    }

    /// <summary>플레이어 빌드 시점에 기록하는 지문. 콘텐츠만 교체해도 되는지 판정하는 근거다.</summary>
    [Serializable]
    public sealed class BuildManifest
    {
        public string profileName;
        public string executableName;
        public string unityVersion;
        public string buildTarget;
        public string scriptsHash;
        public string[] scenes = Array.Empty<string>();
        public string playerBuiltAtUtc;
        public string contentBuiltAtUtc;
        public string catalogHash;
        public string playerSettingsHash;
        public string contentSourceHash;
    }

    /// <summary>사전 점검에 필요한 입력. 에디터 상태를 읽는 일은 호출자가 맡아 점검 로직을 순수하게 유지한다.</summary>
    public sealed class BuildPreflightInput
    {
        public BuildManagerMode Mode;
        public bool IsPlayingOrChangingMode;
        public bool HasUnsavedScene;
        public bool IsCompiling;
        public string ExpectedUnityVersion;
        public string ExpectedBuildTarget;
        public string ExpectedExecutableName;
        public string CurrentScriptsHash;
        public string CurrentPlayerSettingsHash;
        public string CurrentContentSourceHash;
        public bool BuildTargetMatches = true;
        public bool IsSupportedTarget = true;
        public IReadOnlyList<string> Scenes = Array.Empty<string>();
        public Func<string, bool> SceneExists = _ => true;

        /// <summary>대상 플레이어의 지문. 플레이어 빌드 기록이 없으면 null.</summary>
        public BuildManifest PlayerManifest;
        public bool PlayerAaFolderExists;
        public bool PlayerExecutableExists = true;
        public string PlayerCatalogHash;
        public BuildManifest CachedContentManifest;
        public string CachedCatalogHash;

        public bool CatalogExists;
        public DateTime? CatalogWriteUtc;

        /// <summary>주소 지정 에셋 원본의 가장 최근 수정 시각. 계산하지 못했으면 null.</summary>
        public DateTime? LatestContentSourceUtc;

        /// <summary>콘텐츠가 오래돼도 PlayerOnly를 허용한다.</summary>
        public bool AllowStaleContent;
    }

    /// <summary>선택한 출력 폴더와 현재 입력의 차이를 빌드 종류로 나눈다.</summary>
    public sealed class BuildAssessment
    {
        public bool PlayerNeedsBuild;
        public bool ContentNeedsBuild;
        public BuildManagerMode? RecommendedMode;
        public List<string> Reasons = new();
    }
}
