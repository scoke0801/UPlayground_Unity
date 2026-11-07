using System;
using UnityEditor;
using UnityEngine;
using UPlayGround.EditorTools;

namespace UPlayGround.Tool.Editor
{
    /// <summary>
    /// 풀·식물·나뭇잎 머티리얼을 액터 접촉을 지원하는 파생 셰이더로 교체한다.
    /// ExternalAssets는 저장소에 포함되지 않아 머티리얼 변경이 공유되지 않으므로, 각 작업 환경에서 이 도구로 재적용한다.
    /// </summary>
    public static class FoliageInteractionMaterialSwapTool
    {
        private static readonly (string source, string derived)[] ShaderPairs =
        {
            ("Polyart/Dreamscape/URP/Grass", "UPlayGround/Foliage/Grass"),
            ("Polyart/Dreamscape/Foliage/Grass", "UPlayGround/Foliage/GrassGraph"),
            ("Polyart/Dreamscape/Foliage/Foliage", "UPlayGround/Foliage/PlantGraph"),
            ("Polyart/Dreamscape/Foliage/Leaves", "UPlayGround/Foliage/LeavesGraph"),
            ("Idyllic Fantasy Nature/Vegetation", "UPlayGround/Foliage/IdyllicVegetationGraph"),
        };

        [UPlaygroundTool("Foliage/풀 밟기 셰이더 적용")]
        public static void Apply()
        {
            var sources = new Shader[ShaderPairs.Length];
            var derivedShaders = new Shader[ShaderPairs.Length];
            for (int i = 0; i < ShaderPairs.Length; i++)
            {
                sources[i] = Shader.Find(ShaderPairs[i].source);
                derivedShaders[i] = Shader.Find(ShaderPairs[i].derived);
                if (sources[i] == null || derivedShaders[i] == null)
                    throw new InvalidOperationException("셰이더를 찾지 못했습니다: " + ShaderPairs[i].source + " / " + ShaderPairs[i].derived);
            }

            int swapped = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Material"))
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                // Material Variant는 부모의 셰이더를 상속하므로 직접 교체하지 않는다.
                if (material == null || material.isVariant)
                    continue;

                int pairIndex = Array.IndexOf(sources, material.shader);
                if (pairIndex < 0)
                    continue;

                Undo.RecordObject(material, "풀 밟기 셰이더 적용");
                material.shader = derivedShaders[pairIndex];
                EditorUtility.SetDirty(material);
                swapped++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[FoliageInteractionMaterialSwapTool] 교체한 머티리얼 수: " + swapped);
        }
    }
}
