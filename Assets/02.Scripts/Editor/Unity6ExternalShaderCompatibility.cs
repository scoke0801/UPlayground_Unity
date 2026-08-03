using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UPlayGround.Editor
{
    /// <summary>
    /// Unity 6 / URP 17 이전 버전에서 생성된 외부 셰이더의 호환 코드를 보정한다.
    /// ExternalAssets는 Git에서 제외되므로 에셋을 다시 설치한 환경에서도 빌드 전에 보정해야 한다.
    /// </summary>
    [InitializeOnLoad]
    public sealed class Unity6ExternalShaderCompatibility : IPreprocessBuildWithReport
    {
        private const string ExternalAssetsRoot = "Assets/ExternalAssets";
        private const string IdyllicVegetationGraphPath =
            "Assets/ExternalAssets/Environment/Idyllic Fantasy Nature/Shader/Vegetation.shadergraph";

        private static readonly Regex LegacyRenderingLayerCall = new(
            @"EncodeMeshRenderingLayer\s*\(\s*renderingLayers\s*\)",
            RegexOptions.Compiled);

        private static readonly Regex ObsoleteRenderingLayerVariable = new(
            @"(?m)^[ \t]*uint renderingLayers = GetMeshRenderingLayer\(\);\r?\n" +
            @"(?=[ \t]*outRenderingLayers = float4\(\s*EncodeMeshRenderingLayer\(\))",
            RegexOptions.Compiled);

        private static readonly Regex DuplicateTerrainAlphaTestPragma = new(
            @"(?m)^[ \t]*#pragma multi_compile_local __ _ALPHATEST_ON\r?\n",
            RegexOptions.Compiled);

        static Unity6ExternalShaderCompatibility()
        {
            EditorApplication.delayCall += PatchForUnity6;
        }

        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            PatchForUnity6Internal();
        }

        [MenuItem("Tools/Validation/Unity 6 외부 셰이더 호환 패치")]
        public static void PatchForUnity6()
        {
#if !UNITY_6000_0_OR_NEWER
            return;
#else
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall -= PatchForUnity6;
                EditorApplication.delayCall += PatchForUnity6;
                return;
            }

            PatchForUnity6Internal();
#endif
        }

#if UNITY_6000_0_OR_NEWER
        private static void PatchForUnity6Internal()
        {
            string externalRoot = Path.Combine(Application.dataPath, "ExternalAssets");
            if (!Directory.Exists(externalRoot))
                return;

            var changedAssetPaths = new List<string>();
            foreach (string shaderPath in Directory.EnumerateFiles(externalRoot, "*.shader", SearchOption.AllDirectories))
            {
                string source = File.ReadAllText(shaderPath);
                string patched = LegacyRenderingLayerCall.Replace(source, "EncodeMeshRenderingLayer()");
                patched = ObsoleteRenderingLayerVariable.Replace(patched, string.Empty);

                if (shaderPath.EndsWith("TGV_CustomTerrain.shader", StringComparison.OrdinalIgnoreCase))
                    patched = DuplicateTerrainAlphaTestPragma.Replace(patched, string.Empty);

                if (patched == source)
                    continue;

                WriteUtf8PreservingBom(shaderPath, patched);
                changedAssetPaths.Add(ToAssetPath(shaderPath));
            }

            PatchIdyllicVegetationGraph(changedAssetPaths);

            foreach (string assetPath in changedAssetPaths)
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            if (changedAssetPaths.Count > 0)
                Debug.Log($"[Unity6ExternalShaderCompatibility] 외부 셰이더 {changedAssetPaths.Count}개를 URP 17 API에 맞게 보정했습니다.");
        }
#endif

        private static void PatchIdyllicVegetationGraph(List<string> changedAssetPaths)
        {
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", IdyllicVegetationGraphPath));
            if (!File.Exists(fullPath))
                return;

            string source = File.ReadAllText(fullPath);
            string patched = source
                .Replace("\"_MAIN_LIGHT_SHADOWS_CASCADE\"", "\"_IFN_LEGACY_MAIN_LIGHT_SHADOWS_CASCADE\"")
                .Replace("\"_MAIN_LIGHT_SHADOWS\"", "\"_IFN_LEGACY_MAIN_LIGHT_SHADOWS\"");

            if (patched == source)
                return;

            WriteUtf8PreservingBom(fullPath, patched);
            changedAssetPaths.Add(IdyllicVegetationGraphPath);
        }

        private static string ToAssetPath(string fullPath)
        {
            string normalizedAssetsPath = Application.dataPath.Replace('\\', '/');
            string normalizedFullPath = fullPath.Replace('\\', '/');
            return "Assets" + normalizedFullPath.Substring(normalizedAssetsPath.Length);
        }

        private static void WriteUtf8PreservingBom(string path, string content)
        {
            byte[] bytes = File.ReadAllBytes(path);
            bool hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
            File.WriteAllText(path, content, new UTF8Encoding(hasBom));
        }
    }
}
