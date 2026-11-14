using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.EditorTools
{
    /// <summary>원본 계산을 보존한 환경 반응 셰이더 파생을 생성한다.</summary>
    public static class VegetationReactionShaderAuthoring
    {
        public const string OutputFolder = "Assets/06.Shaders/Environment/Vegetation";
        private const string Include = "Assets/06.Shaders/Environment/VegetationReaction.hlsl";
        private const string Properties = @"
        [HideInInspector] _EnvironmentReactionSupported(""환경 반응 계약"", Float) = 1
        [HideInInspector] _EnvState0(""젖음 그을림 손상 연소"", Vector) = (0,0,0,0)
        [HideInInspector] _EnvState1(""열"", Vector) = (0,0,0,0)
        [HideInInspector] _FuelCoverage(""잔여 연료"", Float) = 1
        [Toggle] _AllowFuelCoverage(""연료 소진 소거"", Float) = 0
        _WetTint(""젖음 색"", Color) = (0.55,0.65,0.7,1)
        _CharTint(""그을림 색"", Color) = (0.12,0.10,0.08,1)
        _FireColor(""연소 발광"", Color) = (2,0.4,0.03,1)
        [HideInInspector] _EnvironmentCellMask(""지형 상태"", 2D) = ""black"" {}
        [HideInInspector] _EnvironmentCellBounds(""지형 범위"", Vector) = (0,0,1,1)
        [HideInInspector] _UseEnvironmentCells(""지형 상태 사용"", Float) = 0
";
        private const string Fields = @"
float _EnvironmentReactionSupported;
float4 _EnvState0, _EnvState1;
float _FuelCoverage, _AllowFuelCoverage;
float4 _WetTint, _CharTint, _FireColor;
float4 _EnvironmentCellBounds;
float _UseEnvironmentCells;
";

        /// <summary>구매 원본을 수정하지 않고 원본 경로의 GUID로 파생 셰이더를 식별한다.</summary>
        public static Shader Generate(Shader source)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode가 필요합니다.");
            string path = AssetDatabase.GetAssetPath(source);
            string guid = AssetDatabase.AssetPathToGUID(path);
            if (source == null || string.IsNullOrEmpty(guid)) throw new ArgumentException("실제 셰이더 원본이 필요합니다.");
            Directory.CreateDirectory(OutputFolder);
            string target = OutputFolder + "/" + guid + ".shader";
            if (source.name == "Universal Render Pipeline/Lit") return GenerateLit(path, guid, target);
            string text = path.EndsWith(".shadergraph", StringComparison.OrdinalIgnoreCase) ? GenerateGraph(path) : File.ReadAllText(path);
            text = Regex.Replace(text, "Shader\\s+\"[^\"]+\"", "Shader \"UPlayGround/Environment/Vegetation/" + guid + "\"", RegexOptions.None, TimeSpan.FromSeconds(1));
            text = Regex.Replace(text, @"(Properties\s*\{)", "$1\n" + Properties, RegexOptions.None, TimeSpan.FromSeconds(1));
            text = text.Replace("CBUFFER_START(UnityPerMaterial)", "CBUFFER_START(UnityPerMaterial)\n" + Fields);
            text = text.Replace("CBUFFER_END", "CBUFFER_END\n#include \"" + Include + "\"");
            if (path.EndsWith(".shadergraph", StringComparison.OrdinalIgnoreCase)) text = PatchGraph(text);
            else if (text.Contains("surfaceData.albedo = rgb;"))
                text = text.Replace("// Alpha clipping", "ApplyVegetationReaction(input.positionWS, surfaceData.albedo, surfaceData.emission);\nClipVegetationFuel(input.positionWS);\n// Alpha clipping");
            else if (text.Contains("float3 BaseColor = vColor27.rgb;")) text = PatchGrass(text);
            else if (text.Contains("float3 Color = CustomLighting379.rgb;"))
            {
                text = text.Replace("float3 Color = CustomLighting379.rgb;", "float3 Color = CustomLighting379.rgb;\nfloat3 reactionEmission = 0;\nApplyVegetationReaction(WorldPosition, Color, reactionEmission);\nColor += reactionEmission;");
            }
            else throw new InvalidOperationException("미지원 셰이더: " + path);
            File.WriteAllText(target, text);
            AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<Shader>(target);
        }

        private static string GenerateGraph(string path)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType("UnityEditor.ShaderGraph.ShaderGraphImporter");
                if (type == null) continue;
                foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
                    if (method.Name == "GetShaderText" && method.GetParameters().Length == 4)
                        return (string)method.Invoke(null, new object[] { path, null, null, null });
            }
            throw new InvalidOperationException("설치된 ShaderGraph 생성기를 찾지 못했습니다.");
        }

        private static string PatchGraph(string text)
        {
            return Regex.Replace(text, @"struct SurfaceDescriptionInputs\s*\{.*?SurfaceDescription SurfaceDescriptionFunction\(SurfaceDescriptionInputs IN\)\s*\{.*?return surface;", match =>
            {
                string body = match.Value;
                string inputs = body.Substring(0, body.IndexOf("};", StringComparison.Ordinal));
                string position = inputs.Contains(" WorldSpacePosition;") ? "IN.WorldSpacePosition" : "TransformObjectToWorld(float3(0,0,0))";
                string color = body.Contains("surface.BaseColor =") ? "surface.BaseColor" : "reactionUnusedColor";
                string emission = body.Contains("surface.Emission =") ? "surface.Emission" : "reactionUnusedEmission";
                string prefix = "float3 reactionUnusedColor = 0, reactionUnusedEmission = 0;\n";
                string apply = "ClipVegetationFuel(" + position + ");\n";
                if (color != "reactionUnusedColor" || emission != "reactionUnusedEmission")
                    apply += "ApplyVegetationReaction(" + position + "," + color + "," + emission + ");\n";
                return body.Replace("return surface;", prefix + apply + "return surface;");
            }, RegexOptions.Singleline, TimeSpan.FromSeconds(2));
        }

        private static string PatchGrass(string text)
        {
            text = Regex.Replace(text, @"(float3 Emission = 0;)", "$1\nApplyVegetationReaction(WorldPosition, BaseColor, Emission);", RegexOptions.None, TimeSpan.FromSeconds(1));
            return Regex.Replace(text, @"(float Alpha = staticSwitch182;)", "$1\nClipVegetationFuel(WorldPosition);", RegexOptions.None, TimeSpan.FromSeconds(1));
        }

        private static Shader GenerateLit(string sourcePath, string guid, string target)
        {
            string folder = Path.GetDirectoryName(sourcePath).Replace('\\', '/');
            string inputPath = OutputFolder + "/" + guid + "Input.hlsl";
            string passPath = OutputFolder + "/" + guid + "Forward.hlsl";
            string gbufferPath = OutputFolder + "/" + guid + "GBuffer.hlsl";
            string input = File.ReadAllText(folder + "/LitInput.hlsl");
            input = input.Replace("CBUFFER_START(UnityPerMaterial)", "CBUFFER_START(UnityPerMaterial)\n" + Fields);
            input = input.Replace("CBUFFER_END", "CBUFFER_END\n#include \"" + Include + "\"");
            File.WriteAllText(inputPath, input);
            string forward = File.ReadAllText(folder + "/LitForwardPass.hlsl");
            forward = forward.Replace("InputData inputData;", "ApplyVegetationReaction(input.positionWS, surfaceData.albedo, surfaceData.emission);\nClipVegetationFuel(input.positionWS);\nInputData inputData;");
            File.WriteAllText(passPath, forward);
            string gbuffer = File.ReadAllText(folder + "/LitGBufferPass.hlsl");
            gbuffer = gbuffer.Replace("InputData inputData;", "ApplyVegetationReaction(input.positionWS, surfaceData.albedo, surfaceData.emission);\nInputData inputData;");
            File.WriteAllText(gbufferPath, gbuffer);
            string shader = File.ReadAllText(sourcePath);
            shader = shader.Replace("Shader \"Universal Render Pipeline/Lit\"", "Shader \"UPlayGround/Environment/Vegetation/" + guid + "\"");
            shader = Regex.Replace(shader, @"(Properties\s*\{)", "$1\n" + Properties);
            shader = shader.Replace("Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl", inputPath);
            shader = shader.Replace("Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl", passPath);
            shader = shader.Replace("Packages/com.unity.render-pipelines.universal/Shaders/LitGBufferPass.hlsl", gbufferPath);
            File.WriteAllText(target, shader);
            AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<Shader>(target);
        }
    }
}

