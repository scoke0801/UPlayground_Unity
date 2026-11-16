using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace UPlayGround.Tool.Editor.Build
{
    /// <summary>빌드 산출물 경로 규칙과 지문 파일 입출력.</summary>
    public static class BuildManagerFiles
    {
        public const string ManifestFileName = "BuildManager.manifest.json";
        private const string AddressablesLinkFolder = "AddressablesLink";

        public static string GetOutputDirectory(string outputRoot, string profileName)
        {
            return Path.GetFullPath(Path.Combine(outputRoot, profileName));
        }

        public static string GetExecutablePath(string outputDirectory, string executableName)
        {
            if (string.IsNullOrWhiteSpace(executableName) || executableName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                || executableName == "." || executableName == ".." || executableName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("exe 이름은 경로와 .exe 확장자 없이 입력하세요.", nameof(executableName));
            return Path.Combine(outputDirectory, executableName + ".exe");
        }

        /// <summary>플레이어 실행 파일 기준의 번들 폴더(StreamingAssets/aa). Unity는 데이터 폴더를 실행 파일 이름으로 만든다.</summary>
        public static string GetPlayerAaDirectory(string executablePath)
        {
            string directory = Path.GetDirectoryName(executablePath) ?? string.Empty;
            string dataFolder = Path.GetFileNameWithoutExtension(executablePath) + "_Data";
            return Path.Combine(directory, dataFolder, "StreamingAssets", "aa");
        }

        public static string GetManifestPath(string outputDirectory)
        {
            return Path.Combine(outputDirectory, ManifestFileName);
        }

        public static BuildManifest ReadManifest(string outputDirectory)
        {
            string path = GetManifestPath(outputDirectory);
            if (!File.Exists(path))
                return null;
            try
            {
                return JsonUtility.FromJson<BuildManifest>(File.ReadAllText(path));
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        public static void WriteManifest(string outputDirectory, BuildManifest manifest)
        {
            Directory.CreateDirectory(outputDirectory);
            File.WriteAllText(GetManifestPath(outputDirectory), JsonUtility.ToJson(manifest, true));
        }

        /// <summary>파일 이름 순으로 이름과 내용을 함께 해시한다. 파일이 없으면 빈 문자열이 아니라 'missing' 표식으로 반영한다.</summary>
        public static string ComputeFilesHash(IEnumerable<string> filePaths)
        {
            var sorted = new List<string>(filePaths);
            sorted.Sort(StringComparer.Ordinal);
            using var sha = SHA256.Create();
            foreach (string path in sorted)
            {
                byte[] name = Encoding.UTF8.GetBytes(Path.GetFileName(path));
                sha.TransformBlock(name, 0, name.Length, null, 0);
                byte[] content = File.Exists(path) ? File.ReadAllBytes(path) : Encoding.UTF8.GetBytes("missing");
                sha.TransformBlock(content, 0, content.Length, null, 0);
            }

            sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return BitConverter.ToString(sha.Hash).Replace("-", string.Empty);
        }

        /// <summary>
        /// 콘텐츠 빌드 결과를 플레이어의 aa 폴더에 반영한다.
        /// 번들을 먼저 복사하고 카탈로그·설정을 마지막에 덮어써, 중간에 실패해도 새 카탈로그가 없는 번들을 가리키지 않게 한다.
        /// </summary>
        public static void DeployContent(string sourceDirectory, string destinationAaDirectory)
        {
            if (!Directory.Exists(sourceDirectory))
                throw new DirectoryNotFoundException("콘텐츠 빌드 결과 폴더가 없습니다: " + sourceDirectory);
            Directory.CreateDirectory(destinationAaDirectory);

            foreach (string sourceSub in Directory.GetDirectories(sourceDirectory))
            {
                string folderName = Path.GetFileName(sourceSub);
                if (folderName == AddressablesLinkFolder)
                    continue;
                MirrorBundleFolder(sourceSub, Path.Combine(destinationAaDirectory, folderName));
            }

            foreach (string sourceFile in Directory.GetFiles(sourceDirectory))
            {
                string name = Path.GetFileName(sourceFile);
                if (IsCatalogOrSettings(name))
                    continue;
                File.Copy(sourceFile, Path.Combine(destinationAaDirectory, name), true);
            }

            foreach (string sourceFile in Directory.GetFiles(sourceDirectory))
            {
                string name = Path.GetFileName(sourceFile);
                if (IsCatalogOrSettings(name) && name != "settings.json")
                    File.Copy(sourceFile, Path.Combine(destinationAaDirectory, name), true);
            }

            string settings = Path.Combine(sourceDirectory, "settings.json");
            if (File.Exists(settings))
                File.Copy(settings, Path.Combine(destinationAaDirectory, "settings.json"), true);

            // 그룹 재편으로 사라진 번들도 카탈로그·설정 반영이 모두 끝난 뒤에만 지운다.
            foreach (string existing in Directory.GetFiles(destinationAaDirectory, "*.bundle", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(destinationAaDirectory, existing);
                if (!File.Exists(Path.Combine(sourceDirectory, relative)))
                    File.Delete(existing);
            }
        }

        public static string ComputeCatalogHash(string directory)
        {
            if (!Directory.Exists(directory))
                return string.Empty;
            var catalogs = new List<string>();
            foreach (string file in Directory.GetFiles(directory))
            {
                if (Path.GetFileName(file).StartsWith("catalog", StringComparison.OrdinalIgnoreCase))
                    catalogs.Add(file);
            }

            return catalogs.Count == 0 ? string.Empty : ComputeFilesHash(catalogs);
        }

        private static bool IsCatalogOrSettings(string fileName)
        {
            return fileName.StartsWith("catalog", StringComparison.OrdinalIgnoreCase) || fileName == "settings.json";
        }

        private static void MirrorBundleFolder(string sourceFolder, string destinationFolder)
        {
            Directory.CreateDirectory(destinationFolder);
            foreach (string sourceFile in Directory.GetFiles(sourceFolder))
            {
                string name = Path.GetFileName(sourceFile);
                File.Copy(sourceFile, Path.Combine(destinationFolder, name), true);
            }
        }
    }
}
