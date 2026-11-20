using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Tool.Editor.WorldToon
{
    /// <summary>Foliage 프리셋 적용 전 메시 바람 데이터와 렌더 경로를 검사한다. 원본 메시는 수정하지 않는다.</summary>
    public static class WorldToonFoliageValidator
    {
        /// <summary>바람 가중치(정점 색 R)와 렌더 경로를 검사해 경고 목록을 반환한다. 비어 있으면 FoliageNear 후보다.</summary>
        public static List<string> Validate(Renderer renderer, float windAmplitude)
        {
            var warnings = new List<string>();
            Mesh mesh = ResolveMesh(renderer);
            if (mesh == null)
            {
                warnings.Add("WT-FOLIAGE-001: 메시 없음");
                return warnings;
            }
            if (renderer is SkinnedMeshRenderer)
                warnings.Add("WT-FOLIAGE-001: 스키닝 식생은 v1 경로 아님");

            if (!mesh.isReadable)
            {
                warnings.Add("WT-FOLIAGE-001: 메시 Read/Write 꺼짐 — 정점 색 확인 불가(바람 없는 FoliageFar만 허용)");
                return warnings;
            }
            InspectWindWeights(mesh, warnings);

            // 바람 최대 변위만큼 컬링 bounds 여유가 없으면 화면 가장자리에서 조기 소거될 수 있다.
            float smallestExtent = Mathf.Min(mesh.bounds.extents.x, mesh.bounds.extents.z);
            if (windAmplitude > smallestExtent)
                warnings.Add($"WT-FOLIAGE-001: 바람 진폭 {windAmplitude:0.###}m가 메시 수평 반경 {smallestExtent:0.###}m보다 큼 — bounds 검증 필요");
            return warnings;
        }

        private static Mesh ResolveMesh(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned)
                return skinned.sharedMesh;
            var filter = renderer != null ? renderer.GetComponent<MeshFilter>() : null;
            return filter != null ? filter.sharedMesh : null;
        }

        // 정점 색이 없거나 R이 균일하면 뿌리까지 흔들리므로 자동으로 1로 간주하지 않는다.
        private static void InspectWindWeights(Mesh mesh, List<string> warnings)
        {
            var colors = new List<Color32>();
            mesh.GetColors(colors);
            if (colors.Count == 0)
            {
                warnings.Add("WT-FOLIAGE-001: 정점 색 없음 — 바람 가중치(R) 부재");
                return;
            }
            byte min = 255;
            byte max = 0;
            foreach (Color32 color in colors)
            {
                if (color.r < min) min = color.r;
                if (color.r > max) max = color.r;
            }
            if (max - min < 32)
                warnings.Add($"WT-FOLIAGE-001: 정점 색 R 범위 {min}~{max} — 뿌리 고정 마스크로 보기 어려움");
        }
    }
}
