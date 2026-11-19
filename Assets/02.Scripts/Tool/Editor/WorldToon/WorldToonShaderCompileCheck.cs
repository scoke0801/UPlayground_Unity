using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UPlayGround.EditorTools;

namespace UPlayGround.Tool.Editor.WorldToon
{
    /// <summary>
    /// WorldToon 셰이더의 대표 키워드 조합을 D3D 대상으로 실제 컴파일한다.
    /// 검사한 조합에 대해서만 의미가 있으며 Player 빌드의 변형 보존(스트리핑)을 증명하지 않는다.
    /// </summary>
    public static class WorldToonShaderCompileCheck
    {
        private const string ToolId = "UPlayGround/검증/WorldToon/셰이더 컴파일 검사";

        // 프리셋 local 키워드 × 프로젝트에서 실제로 켜지는 URP 키워드(그림자·Forward+·SSAO·DBuffer·Fog·라이트맵·인스턴싱).
        private static readonly string[][] Combos =
        {
            new string[0],
            new[] { "_MAIN_LIGHT_SHADOWS_CASCADE", "_ADDITIONAL_LIGHTS", "_CLUSTER_LIGHT_LOOP", "_WT_ADDITIONAL_LIGHTS", "_SCREEN_SPACE_OCCLUSION", "_DBUFFER_MRT3", "_SHADOWS_SOFT", "_ADDITIONAL_LIGHT_SHADOWS", "_LIGHT_LAYERS", "_LIGHT_COOKIES", "FOG_EXP2" },
            new[] { "_MAIN_LIGHT_SHADOWS_CASCADE", "_ADDITIONAL_LIGHTS", "_WT_ADDITIONAL_LIGHTS", "_ALPHATEST_ON", "LOD_FADE_CROSSFADE", "LIGHTMAP_ON", "SHADOWS_SHADOWMASK", "LIGHTMAP_SHADOW_MIXING", "FOG_LINEAR", "_RECEIVE_SHADOWS_OFF", "_WT_WIND" },
            new[] { "_ADDITIONAL_LIGHTS_VERTEX", "_WT_ADDITIONAL_LIGHTS", "DYNAMICLIGHTMAP_ON", "DEBUG_DISPLAY", "INSTANCING_ON", "_WRITE_RENDERING_LAYERS", "_DBUFFER_MRT1", "_WT_WIND", "_ALPHATEST_ON" },
            new[] { "_ALPHATEST_ON", "INSTANCING_ON", "_CASTING_PUNCTUAL_LIGHT_SHADOW", "EDITOR_VISUALIZATION", "_ADD_PRECOMPUTED_VELOCITY", "_WT_WIND", "_ALPHATEST_ON" },
            new[] { "_TERRAIN_INSTANCED_PERPIXEL_NORMAL", "INSTANCING_ON", "_ALPHATEST_ON", "_MAIN_LIGHT_SHADOWS_CASCADE", "_SCREEN_SPACE_OCCLUSION", "_DBUFFER_MRT3", "_CLUSTER_LIGHT_LOOP", "_ADDITIONAL_LIGHTS" },
        };

        private static readonly string[] ShaderPaths =
        {
            "Assets/06.Shaders/WorldToon/WorldToonMesh.shader",
            "Assets/06.Shaders/WorldToon/WorldToonFoliage.shader",
            "Assets/06.Shaders/WorldToon/WorldToonTerrain4.shader",
            "Assets/06.Shaders/WorldToon/Hidden/WorldToonTerrainBaseMap.shader",
        };

        public const string ReportPath = "docs/work-status/worldtoon/logs/shader-compile-check.txt";

        [UPlaygroundTool(ToolId)]
        private static void RunFromCatalog()
        {
            RunDeferred();
        }

        /// <summary>
        /// 컴파일이 수십 초 걸려 MCP 호출 제한을 넘기므로 다음 Editor 갱신에서 실행하고 결과를 파일로 남긴다.
        /// </summary>
        public static void RunDeferred()
        {
            EditorApplication.delayCall += () =>
            {
                string result = Run();
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ReportPath));
                System.IO.File.WriteAllText(ReportPath, $"{System.DateTime.Now:yyyy-MM-dd HH:mm:ss}\n{result}");
                Debug.Log("[WorldToon] 셰이더 컴파일 검사 " + result.Split('\n')[0]);
            };
        }

        /// <summary>존재하는 WorldToon 셰이더 전부를 검사하고 사람이 읽을 보고문을 반환한다. 첫 줄은 PASS/FAIL 요약이다.</summary>
        public static string Run()
        {
            var report = new StringBuilder();
            int compiled = 0;
            int failed = 0;
            foreach (string path in ShaderPaths)
            {
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                if (shader == null)
                    continue;
                CheckShader(shader, report, ref compiled, ref failed);
            }
            string summary = failed == 0 ? $"PASS compiled={compiled}" : $"FAIL failed={failed} compiled={compiled}";
            return summary + "\n" + report;
        }

        private static void CheckShader(Shader shader, StringBuilder report, ref int compiled, ref int failed)
        {
            if (ShaderUtil.ShaderHasError(shader))
            {
                failed++;
                report.AppendLine($"{shader.name}: import 오류");
                foreach (ShaderMessage message in ShaderUtil.GetShaderMessages(shader))
                    report.AppendLine($"   {message.severity} {message.message} @{message.file}:{message.line}");
                return;
            }

            ShaderData data = ShaderUtil.GetShaderData(shader);
            for (int subIndex = 0; subIndex < data.SubshaderCount; subIndex++)
            {
                ShaderData.Subshader subshader = data.GetSerializedSubshader(subIndex);
                for (int passIndex = 0; passIndex < subshader.PassCount; passIndex++)
                    CheckPass(shader, subshader.GetPass(passIndex), report, ref compiled, ref failed);
            }
            report.AppendLine($"{shader.name}: passes 검사 완료");
        }

        private static void CheckPass(Shader shader, ShaderData.Pass pass, StringBuilder report, ref int compiled, ref int failed)
        {
            var seen = new HashSet<string>();
            foreach (string[] combo in Combos)
            {
                foreach (ShaderType stage in new[] { ShaderType.Vertex, ShaderType.Fragment })
                {
                    if (!pass.HasShaderStage(stage))
                        continue;
                    ShaderData.VariantCompileInfo result = pass.CompileVariant(stage, combo, ShaderCompilerPlatform.D3D, BuildTarget.StandaloneWindows64);
                    compiled++;
                    if (result.Success && result.Messages.Length == 0)
                        continue;
                    foreach (ShaderMessage message in result.Messages)
                    {
                        string line = $"{shader.name}/{pass.Name} {stage}: {message.severity} {message.message} @{message.file}:{message.line}";
                        if (seen.Add(line))
                            report.AppendLine("   " + line);
                    }
                    if (!result.Success)
                        failed++;
                }
            }
        }
    }
}
