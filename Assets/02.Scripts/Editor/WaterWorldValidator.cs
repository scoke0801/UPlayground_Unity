using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UPlayGround.Gameplay.World;

namespace UPlayGround.EditorTools
{
    /// <summary>광학 소유권·높이장·파도 오차·강수 마스크·GPU 예산을 저작 단계에서 검사한다.</summary>
    public static class WaterWorldValidator
    {
        [UPlaygroundTool("Water/ValidateLoaded")]
        public static void ValidateLoaded()
        {
            List<string> errors = InspectLoaded(out var warnings);
            foreach (string warning in warnings) Debug.LogWarning(warning);
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
            Debug.Log("수역 구조 검사 통과. 입력·아트·성능 인수는 별도입니다.");
        }

        /// <summary>실행 중인 월드를 수정하지 않고 오류와 지원 제한을 반환한다.</summary>
        public static List<string> InspectLoaded(out List<string> warnings)
        {
            var errors = new List<string>();
            warnings = new List<string>();
            var surfaces = new HashSet<UPlayGround.Components.FootstepWaterSurface>();
            var regions = UnderwaterRegion.ActiveRegions;
            int cells = 0, dry = 0;
            foreach (var region in regions)
            {
                if (region == null || !region.isActiveAndEnabled) continue;
                var surface = region.Surface;
                if (surface == null || region.Profile == null || surface.surfaceMesh == null)
                { errors.Add(region.name + ": 수면·프로필·메시 참조 누락"); continue; }
                if (!surfaces.Add(surface)) errors.Add(region.name + ": 같은 수면에 중복 UnderwaterRegion");
                region.RefreshBinding();
                Renderer renderer = region.SurfaceRenderer;
                Material material = renderer != null ? renderer.sharedMaterial : null;
                if (material == null || material.FindPass("WaterSurfaceInfo") < 0)
                    errors.Add(region.name + ": WaterSurfaceInfo 패스 누락");
                else if (material.GetFloat("_ShoreFade") > 0)
                    warnings.Add(region.name + ": UV 후퇴가 있는 수면은 메시/체적 경계 일치를 별도로 확인해야 합니다.");
                if (Vector3.Dot(surface.surfaceMesh.transform.up, Vector3.up) < 0.9999f)
                    errors.Add(region.name + ": 회전된 로컬 수직축은 기존 CPU 높이장 샘플러 지원 밖");
                if (region.Profile.extinction.x < 0 || region.Profile.extinction.y < 0 || region.Profile.extinction.z < 0)
                    errors.Add(region.name + ": 음수 흡수 계수");
                if (!surface.useVolumeBounds) warnings.Add(region.name + ": 수직 체적이 무제한입니다. 다른 높이/동굴 저작 시 체적과 건조 구역을 지정해야 합니다.");
                dry += surface.dryVolumes != null ? surface.dryVolumes.Length : 0;
                cells += region.PrismTriangleCount;
                if (surface.dynamics != null)
                {
                    if (surface.dynamics.MaximumSlope > 0.35f) errors.Add(region.name + ": 큰 파도 경사 0.35 초과");
                    float estimate = EstimateWaveInterpolationError(surface);
                    if (estimate > 0.02f) errors.Add(region.name + ": CPU/GPU 보간 오차 상한 " + estimate.ToString("F4") + "m. 시각 메시를 더 세분화해야 합니다.");
                    if (region.RainIntensity > 0f && region.rainExposure == null) warnings.Add(region.name + ": 노출 마스크가 없어 비 파문이 비활성입니다.");
                }
                if (region.rainExposure != null && region.rainExposure.isDataSRGB) errors.Add(region.name + ": 강수 마스크는 선형 데이터여야 합니다.");
            }
            if (surfaces.Count > WaterGpuRecords.MaxBodies || cells > WaterGpuRecords.MaxCells || dry > WaterGpuRecords.MaxDryVolumes)
                errors.Add($"수역 예산 초과: Body {surfaces.Count}/16, Cell {cells}/512, Dry {dry}/128");
            var pipeline = new SerializedObject(GraphicsSettings.currentRenderPipeline);
            int rendererIndex = pipeline.FindProperty("m_DefaultRendererIndex").intValue;
            var rendererData = pipeline.FindProperty("m_RendererDataList").GetArrayElementAtIndex(rendererIndex).objectReferenceValue as ScriptableRendererData;
            int featureCount = 0;
            foreach (var feature in rendererData.rendererFeatures)
                if (feature is UnderwaterRendererFeature && feature.isActive) featureCount++;
            if (featureCount != 1) errors.Add("활성 렌더러의 광학 Feature 수가 1이 아닙니다: " + featureCount);
            foreach (var link in WaterfallLink.ActiveLinks)
            {
                if (link == null || !link.TryGetComponent<Renderer>(out var ribbon)) continue;
                if (ribbon.sharedMaterial == null || ribbon.sharedMaterial.FindPass("DepthNormalsOnly") < 0)
                    errors.Add(link.name + ": Deferred/Depth Priming용 DepthNormalsOnly 패스 누락");
            }
            foreach (string path in new[] { "Assets/06.Shaders/LakeWater.shader", "Assets/06.Shaders/LakeUnderwater.shader",
                "Assets/06.Shaders/Water/WaterSurfaceInfo.shader", "Assets/06.Shaders/Water/WaterSceneResolve.shader",
                "Assets/06.Shaders/Water/WaterAwareParticle.shader", "Assets/06.Shaders/Water/ToonWaterfall.shader" })
            {
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                if (shader == null) { errors.Add(path + ": 셰이더 누락"); continue; }
                foreach (var message in ShaderUtil.GetShaderMessages(shader))
                    if (message.severity.ToString() == "Error") errors.Add(path + ":" + message.line + " " + message.message);
            }
            warnings.Add("수면 뒤 임의 AlphaBlend·다중 자유수면 반사/굴절·XR은 V1 지원 밖입니다. 광학 구간 32개를 넘으면 자홍색 오류 표시를 냅니다.");
            return errors;
        }

        /// <summary>사인파 Hessian과 시각 메시 최대 변으로 선형 삼각형 보간 오차를 보수적으로 제한한다.</summary>
        public static float EstimateWaveInterpolationError(UPlayGround.Components.FootstepWaterSurface surface)
        {
            if (surface.dynamics == null) return 0f;
            Mesh mesh = surface.surfaceMesh.sharedMesh;
            Vector3[] vertices = mesh.vertices;
            int[] indices = mesh.triangles;
            float maximumEdgeSquared = 0f;
            Matrix4x4 matrix = surface.surfaceMesh.transform.localToWorldMatrix;
            for (int i = 0; i < indices.Length; i += 3)
            {
                Vector3 a = matrix.MultiplyPoint3x4(vertices[indices[i]]);
                Vector3 b = matrix.MultiplyPoint3x4(vertices[indices[i + 1]]);
                Vector3 c = matrix.MultiplyPoint3x4(vertices[indices[i + 2]]);
                maximumEdgeSquared = Mathf.Max(maximumEdgeSquared, (b - a).sqrMagnitude);
                maximumEdgeSquared = Mathf.Max(maximumEdgeSquared, (c - a).sqrMagnitude);
                maximumEdgeSquared = Mathf.Max(maximumEdgeSquared, (b - c).sqrMagnitude);
            }
            Vector4 first = surface.dynamics.firstWave.GetParameters(), second = surface.dynamics.secondWave.GetParameters();
            float curvature = first.z * (first.x * first.x + first.y * first.y)
                + second.z * (second.x * second.x + second.y * second.y);
            return curvature * maximumEdgeSquared / 6f;
        }
    }
}
