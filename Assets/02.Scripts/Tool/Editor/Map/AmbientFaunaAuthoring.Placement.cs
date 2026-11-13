using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Gameplay.World.LivingWorld;

namespace UPlayGround.Tool.Editor.Map
{
    public static partial class AmbientFaunaAuthoring
    {
        /// <summary>고정 seed의 표면·체적 후보에서 안전한 비행 공간만 저장한다.</summary>
        public static Vector4[] BakePlacement(AmbientFaunaPlacement placement)
        {
            if (!placement.TryValidate(out string error)) throw new InvalidOperationException(error);
            Physics.SyncTransforms();
            Undo.RecordObject(placement, "환경 생물 표면 베이크");
            var random = new System.Random(placement.Seed);
            var anchors = new List<Vector4>();
            int valid = 0, inside = 0;
            for (int i = 0; i < placement.Samples; i++)
            {
                float x = (float)random.NextDouble() * 2 - 1;
                float y = (float)random.NextDouble() * 2 - 1;
                float z = (float)random.NextDouble() * 2 - 1;
                Vector3 local = placement.Shape == FaunaPlacementShape.Box
                    ? Vector3.Scale(new Vector3(x, y, z), placement.Size * 0.5f)
                    : new Vector3(x, y, z) * placement.Radius;
                if (placement.Shape == FaunaPlacementShape.Sphere && local.sqrMagnitude > placement.Radius * placement.Radius) continue;
                inside++;
                Vector3 world = placement.transform.TransformPoint(local);
                if (placement.UseSurface)
                {
                    float height = placement.Shape == FaunaPlacementShape.Box ? placement.Size.y : placement.Radius * 2;
                    Vector3 top = placement.transform.TransformPoint(new Vector3(local.x, height * 0.5f, local.z));
                    var hit = Physics.RaycastAll(top, Vector3.down, height, placement.SurfaceMask, QueryTriggerInteraction.Ignore)
                        .Where(h => h.collider.attachedRigidbody == null && h.collider.GetComponentInParent<GameActor>() == null
                            && h.collider.GetComponentInParent<WildlifeCreature>() == null)
                        .OrderBy(h => h.distance).FirstOrDefault();
                    if (hit.collider == null || Vector3.Angle(hit.normal, Vector3.up) > placement.MaximumSlope) continue;
                    world = hit.point + Vector3.up * placement.FlightHeight;
                    local = placement.transform.InverseTransformPoint(world);
                    if (placement.Shape == FaunaPlacementShape.Box && (Mathf.Abs(local.y) + placement.Clearance > placement.Size.y * 0.5f)) continue;
                }
                if (placement.Shape == FaunaPlacementShape.Sphere && local.magnitude + placement.Clearance > placement.Radius) continue;
                if (placement.Shape == FaunaPlacementShape.Box &&
                    (Mathf.Abs(local.x) + placement.Clearance > placement.Size.x * 0.5f
                     || Mathf.Abs(local.z) + placement.Clearance > placement.Size.z * 0.5f
                     || Mathf.Abs(local.y) + placement.Clearance > placement.Size.y * 0.5f)) continue;
                if (placement.IntersectsExclusion(world, placement.Clearance)) continue;
                if (Physics.OverlapSphere(world, placement.Clearance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                    .Any(c => c.attachedRigidbody == null && c.GetComponentInParent<GameActor>() == null
                        && c.GetComponentInParent<WildlifeCreature>() == null)) continue;
                valid++;
                if (anchors.Any(a => (new Vector3(a.x, a.y, a.z) - local).sqrMagnitude < placement.MinimumSpacing * placement.MinimumSpacing)) continue;
                anchors.Add(new Vector4(local.x, local.y, local.z, placement.Clearance));
            }
            if (anchors.Count == 0) throw new InvalidOperationException(placement.name + ": 유효한 안전 비행 앵커가 0개입니다.");
            float measure = placement.Shape == FaunaPlacementShape.Box
                ? (placement.DensityMode == FaunaDensityMode.Surface ? placement.Size.x * placement.Size.z : placement.Size.x * placement.Size.y * placement.Size.z)
                : (placement.DensityMode == FaunaDensityMode.Surface ? Mathf.PI * placement.Radius * placement.Radius : 4f / 3 * Mathf.PI * Mathf.Pow(placement.Radius, 3));
            placement.ValidMeasure = measure * valid / Mathf.Max(1f, inside);
            placement.AnchorCount = anchors.Count;
            placement.SettingsHash = placement.ComputeSettingsHash();
            placement.GeometryHash = ComputeGeometryHash(placement);
            placement.BakeVersion = 1;
            var group = placement.GetComponent<AmbientFaunaGroup>();
            if (placement.DensityMode != FaunaDensityMode.ManualCount)
            {
                var serialized = new SerializedObject(group);
                int count = Mathf.Min(placement.MaximumCount, Mathf.FloorToInt(placement.ValidMeasure * placement.Density));
                if (count < 1) throw new InvalidOperationException("유효 면적·체적의 목표 개체 수가 0입니다.");
                serialized.FindProperty("_capacity").intValue = Mathf.Min(count, 4096);
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorUtility.SetDirty(placement);
            return anchors.ToArray();
        }

        /// <summary>주변 지오메트리와 의존 에셋 변경을 Editor에서 검출한다.</summary>
        public static string ComputeGeometryHash(AmbientFaunaPlacement placement)
        {
            Physics.SyncTransforms();
            float radius = placement.Shape == FaunaPlacementShape.Box ? placement.Size.magnitude : placement.Radius;
            var sources = Physics.OverlapSphere(placement.transform.position, radius, placement.SurfaceMask, QueryTriggerInteraction.Ignore)
                .Where(c => c.attachedRigidbody == null && c.GetComponentInParent<GameActor>() == null && c.GetComponentInParent<WildlifeCreature>() == null)
                .OrderBy(c => GlobalObjectId.GetGlobalObjectIdSlow(c).ToString());
            string value = "";
            foreach (var collider in sources)
            {
                value += GlobalObjectId.GetGlobalObjectIdSlow(collider) + EditorJsonUtility.ToJson(collider)
                    + collider.transform.localToWorldMatrix;
                UnityEngine.Object data = collider is MeshCollider mesh ? mesh.sharedMesh
                    : collider is TerrainCollider terrain ? terrain.terrainData : null;
                string path = AssetDatabase.GetAssetPath(data);
                if (!string.IsNullOrEmpty(path)) value += AssetDatabase.GetAssetDependencyHash(path);
            }
            return Hash128.Compute(value).ToString();
        }
    }

    /// <summary>베이크 상태와 실제 예약·출력 추정을 같은 Inspector에 표시한다.</summary>
    [CustomEditor(typeof(AmbientFaunaPlacement))]
    public sealed class AmbientFaunaPlacementEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var placement = (AmbientFaunaPlacement)target;
            bool stale = placement.BakeVersion != 1 || placement.SettingsHash != placement.ComputeSettingsHash()
                || placement.GeometryHash != AmbientFaunaAuthoring.ComputeGeometryHash(placement);
            EditorGUILayout.HelpBox(stale ? "베이크가 오래됐거나 미수행입니다. 다시 베이크하세요."
                : $"앵커 {placement.AnchorCount} / 유효 면적·체적 {placement.ValidMeasure:F2}", stale ? MessageType.Warning : MessageType.Info);
            if (!Application.isPlaying && GUILayout.Button("선택한 환경 생물 베이크")) AmbientFaunaAuthoring.BakeSelectedHabitats();
            var group = placement.GetComponent<AmbientFaunaGroup>();
            EditorGUILayout.LabelField("실제 할당 / 상한", $"{group.AllocatedCapacity} / {group.Capacity}");
            EditorGUILayout.LabelField("GPU buffer bytes", group.BufferBytes.ToString());
            EditorGUILayout.LabelField("표시 밀도 / 예산 선택", $"{group.DisplayDensity:F2} / {group.IsBudgetEligible}");
            EditorGUILayout.LabelField("화면 점유 추정", AmbientFaunaHabitat.EstimatedScreenFraction.ToString("P4"));
        }
    }

    /// <summary>서식지의 실제 용량·그룹·공격 구독과 누락 자극을 표시한다.</summary>
    [CustomEditor(typeof(AmbientFaunaHabitat))]
    public sealed class AmbientFaunaHabitatEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var habitat = (AmbientFaunaHabitat)target;
            EditorGUILayout.LabelField("예약 Capacity / 그룹", $"{habitat.AllocatedCapacity} / {habitat.AllocatedGroups}");
            EditorGUILayout.LabelField("지역 활성 / 공격 구독", $"{habitat.IsRegionActive} / {habitat.CombatSubscriptionCount}");
            EditorGUILayout.LabelField("지형 이벤트 구독", habitat.TerrainSubscriptionCount.ToString());
            EditorGUILayout.LabelField("자극 초과·교체", habitat.DroppedStimuli.ToString());
            EditorGUILayout.LabelField("화면 점유 추정 (정확한 픽셀 수 아님)", AmbientFaunaHabitat.EstimatedScreenFraction.ToString("P4"));
            if (GUILayout.Button("현재 씬 서식지 검증")) Debug.Log(AmbientFaunaAuthoring.ValidateActiveWorld());
        }
    }
    /// <summary>수동 앵커 그룹도 기하 무효화와 실제 GPU 자원을 직접 확인할 수 있게 한다.</summary>
    [CustomEditor(typeof(AmbientFaunaGroup))]
    public sealed class AmbientFaunaGroupEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var group = (AmbientFaunaGroup)target;
            EditorGUILayout.LabelField("상태 / 실제 할당", $"{group.State} / {group.AllocatedCapacity}");
            EditorGUILayout.LabelField("GPU buffer bytes", group.BufferBytes.ToString());
            EditorGUILayout.LabelField("표시 밀도 / 예산 선택", $"{group.DisplayDensity:F2} / {group.IsBudgetEligible}");
            if (group.IsGeometryInvalidated)
                EditorGUILayout.HelpBox("주변 지형·구조물이 바뀌어 중단했습니다. 편집 모드에서 다시 베이크하세요.", MessageType.Warning);
        }
    }
}
