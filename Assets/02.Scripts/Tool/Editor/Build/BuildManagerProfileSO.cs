using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Tool.Editor.Build
{
    /// <summary>
    /// 빌드 한 종류의 씬 목록과 옵션을 담는다. 전역 Build Settings를 손으로 고치지 않고 이 에셋을 정본으로 쓴다.
    /// lilToon 전처리가 전역 씬 목록을 읽으므로 빌드 중에만 목록을 교체하고 끝나면 복원한다.
    /// </summary>
    [CreateAssetMenu(fileName = "BuildProfile", menuName = "UPlayGround/Build/Profile")]
    public sealed class BuildManagerProfileSO : ScriptableObject
    {
        [Tooltip("출력 폴더 이름이자 프로필 식별자. 같은 이름의 폴더에 덮어쓴다.")]
        public string profileName = "Release";

        [Tooltip("프로젝트 루트 기준 출력 폴더.")]
        public string outputRoot = "Builds";

        [Tooltip("실제 빌드 폴더. 비어 있으면 기존 outputRoot/profileName 규칙을 사용한다.")]
        public string outputDirectory;

        [Tooltip("확장자 없는 실행 파일 이름.")]
        public string executableName = "UPlayground";

        [Tooltip("빌드에 포함할 씬. 첫 항목이 시작 씬이다.")]
        public List<SceneAsset> scenes = new();

        public BuildTarget buildTarget = BuildTarget.StandaloneWindows64;
        public bool developmentBuild;
        public BuildOptions buildOptions = BuildOptions.CompressWithLz4;

        /// <summary>새 폴더 지정과 기존 프로필의 경로 규칙을 함께 지원한다.</summary>
        public string GetOutputDirectory() => string.IsNullOrWhiteSpace(outputDirectory)
            ? BuildManagerFiles.GetOutputDirectory(outputRoot, profileName)
            : System.IO.Path.GetFullPath(outputDirectory);

        /// <summary>Development 여부는 창에 노출한 토글을 정본으로 사용한다.</summary>
        public BuildOptions GetBuildOptions() => (buildOptions & ~BuildOptions.Development)
            | (developmentBuild ? BuildOptions.Development : BuildOptions.None);

        public IReadOnlyList<string> GetScenePaths()
        {
            var paths = new List<string>(scenes.Count);
            foreach (SceneAsset scene in scenes)
            {
                paths.Add(scene != null ? AssetDatabase.GetAssetPath(scene) : string.Empty);
            }

            return paths;
        }
    }
}
