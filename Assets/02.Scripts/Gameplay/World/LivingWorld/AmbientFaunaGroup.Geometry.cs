using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Gameplay.World.LivingWorld
{
    public sealed partial class AmbientFaunaGroup
    {
        [Serializable]
        private sealed class GeometryWitness
        {
            public Collider Source;
            public Matrix4x4 Matrix;
            public Bounds Bounds;
            public Mesh Mesh;
            public TerrainData Terrain;
        }

        [SerializeField, HideInInspector] private GeometryWitness[] _geometryWitnesses = Array.Empty<GeometryWitness>();
        private bool _geometryInvalidated;
        public bool IsGeometryInvalidated => _geometryInvalidated;

        private void CaptureBakedGeometry()
        {
            _geometryInvalidated = false;
            if (_anchors.Length == 0) { _geometryWitnesses = Array.Empty<GeometryWitness>(); return; }
            Physics.SyncTransforms();
            Bounds bounds = TransformBounds(CalculateLocalBounds(_anchors, _profile != null ? _profile.SizeRange.y : 0), transform.localToWorldMatrix);
            var records = new List<GeometryWitness>();
            foreach (var source in Physics.OverlapSphere(bounds.center, bounds.extents.magnitude,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (source.gameObject.scene != gameObject.scene || source.attachedRigidbody != null || source.GetComponentInParent<GameActor>() != null
                    || source.GetComponentInParent<WildlifeCreature>() != null) continue;
                records.Add(new GeometryWitness { Source = source, Matrix = source.transform.localToWorldMatrix,
                    Bounds = source.bounds, Mesh = source is MeshCollider mesh ? mesh.sharedMesh : null,
                    Terrain = source is TerrainCollider terrain ? terrain.terrainData : null });
            }
            _geometryWitnesses = records.ToArray();
        }

        private bool HasUnchangedGeometry()
        {
            if (_geometryInvalidated || _geometryWitnesses == null) return false;
            for (int i = 0; i < _geometryWitnesses.Length; i++)
            {
                var record = _geometryWitnesses[i];
                var source = record.Source;
                if (source == null || !source.enabled || !source.gameObject.activeInHierarchy) return false;
                if (source is MeshCollider mesh && mesh.sharedMesh != record.Mesh) return false;
                if (source is TerrainCollider terrain && terrain.terrainData != record.Terrain) return false;
                Matrix4x4 matrix = source.transform.localToWorldMatrix;
                for (int element = 0; element < 16; element++)
                    if (!Data.World.AmbientFaunaProfileSO.IsFinite(matrix[element])
                        || Mathf.Abs(matrix[element] - record.Matrix[element]) > 0.001f) return false;
                Bounds bounds = source.bounds;
                if ((bounds.center - record.Bounds.center).sqrMagnitude > 0.000001f
                    || (bounds.size - record.Bounds.size).sqrMagnitude > 0.000001f) return false;
            }
            return true;
        }

        /// <summary>저작한 정적 충돌체 변경을 예산 갱신 주기에 검사하고 오래된 군집을 중단한다.</summary>
        public void CheckBakedGeometry()
        {
            if (IsReady && !HasUnchangedGeometry()) InvalidateGeometry();
        }

        /// <summary>지형 변형이나 외부 구조물 변경 뒤 재베이크 전까지 자원·fallback 소유권을 반환한다.</summary>
        public void InvalidateGeometry()
        {
            if (_geometryInvalidated) return;
            _geometryInvalidated = true;
            ReleaseGpu(); RestoreFallbacks();
            IsReady = false;
            IsBudgetEligible = false;
            State = AmbientFaunaGroupState.Faulted;
        }
    }
}
