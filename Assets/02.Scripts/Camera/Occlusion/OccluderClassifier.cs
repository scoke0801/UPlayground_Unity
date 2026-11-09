using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem
{
    /// <summary>카메라 장애물의 처리 분류.</summary>
    internal enum OccluderKind
    {
        Solid,
        Fade,
        Ignore,
    }

    /// <summary>
    /// 콜라이더를 Solid/Fade/Ignore로 분류하고 결과를 캐시한다.
    /// 판정 순서: 저작 컴포넌트 → 지형·Solid 레이어 → 페이드할 렌더러 유무 → 바닥형 형태.
    /// 확신이 없으면 항상 Solid로 떨어져 기존 카메라 충돌 동작을 유지한다.
    /// </summary>
    internal sealed class OccluderClassifier
    {
        internal readonly struct Entry
        {
            public readonly OccluderKind Kind;
            public readonly OccluderGroup Group;

            /// <summary>실내 가드가 이 콜라이더를 지붕·천장으로 인정하는지 여부(저작 데이터로만 켜진다).</summary>
            public readonly bool ProvidesCover;

            public Entry(OccluderKind kind, OccluderGroup group, bool providesCover = false)
            {
                Kind = kind;
                Group = group;
                ProvidesCover = providesCover;
            }
        }

        /// <summary>이름 규칙을 찾기 위해 콜라이더 오브젝트에서 올라갈 최대 조상 단계.</summary>
        private const int MaxNameRuleDepth = 12;

        private static readonly Entry SolidEntry = new(OccluderKind.Solid, null);
        private static readonly Entry IgnoreEntry = new(OccluderKind.Ignore, null);

        private readonly CameraSettings _settings;
        private readonly int _solidLayerMask;
        private readonly int _proxyLayer;
        private readonly Dictionary<int, Entry> _entries = new();
        private readonly Dictionary<int, OccluderGroup> _groups = new();
        private readonly List<Renderer> _rendererBuffer = new();

        public OccluderClassifier(CameraSettings settings, int solidLayerMask, int proxyLayer)
        {
            _settings = settings;
            _solidLayerMask = solidLayerMask;
            _proxyLayer = proxyLayer;
        }

        /// <summary>콜라이더의 분류 결과를 반환한다. 최초 1회만 계산한다.</summary>
        public Entry Classify(Collider collider)
        {
            if (collider == null) return SolidEntry;
            int id = collider.GetInstanceID();
            if (_entries.TryGetValue(id, out Entry cached)) return cached;

            Entry entry = Build(collider);
            _entries[id] = entry;
            return entry;
        }

        /// <summary>분류 캐시를 비운다. 그룹의 머티리얼 복원은 호출자가 먼저 처리한다.</summary>
        public void Clear()
        {
            _entries.Clear();
            _groups.Clear();
        }

        private Entry Build(Collider collider)
        {
            // 우선순위: 직접 붙인 CameraOccluder → 이름 규칙 → 자동 판정.
            CameraOccluder authoring = collider.GetComponentInParent<CameraOccluder>();
            OccluderMode mode = OccluderMode.Auto;
            bool providesCover = false;
            if (authoring != null)
            {
                mode = authoring.Mode;
                providesCover = authoring.CoversInterior;
            }
            else if (TryMatchNameRule(collider, out CameraOccluderNameRulesSO.Rule rule))
            {
                mode = rule.mode;
                providesCover = rule.coversInterior;
            }

            if (mode == OccluderMode.Ignore) return IgnoreEntry;
            if (mode == OccluderMode.Solid) return CreateSolidEntry(providesCover);
            if (mode == OccluderMode.Auto && IsAutoSolid(collider)) return CreateSolidEntry(providesCover);

            Transform root = authoring != null && authoring.FadeWholeHierarchy
                ? authoring.transform
                : collider.transform;
            // 몸 충돌체와 메시가 형제인 리그도 같은 페이드 그룹으로 묶는다.
            if (authoring == null)
            {
                Animator animator = collider.GetComponentInParent<Animator>();
                if (animator != null) root = animator.transform;
            }
            OccluderGroup group = ResolveGroup(root, collider);
            return group == null
                ? CreateSolidEntry(providesCover)
                : new Entry(OccluderKind.Fade, group, providesCover);
        }

        /// <summary>콜라이더 오브젝트부터 조상 방향으로 올라가며 이름 규칙과 일치하는 첫 지정을 찾는다.</summary>
        private bool TryMatchNameRule(Collider collider, out CameraOccluderNameRulesSO.Rule rule)
        {
            CameraOccluderNameRulesSO rules = _settings.occlusionNameRules;
            rule = default;
            if (rules == null) return false;

            Transform current = collider.transform;
            for (int depth = 0; current != null && depth < MaxNameRuleDepth; depth++, current = current.parent)
                if (rules.TryMatch(current.name, out rule)) return true;
            return false;
        }

        private static Entry CreateSolidEntry(bool providesCover)
            => providesCover ? new Entry(OccluderKind.Solid, null, true) : SolidEntry;

        private bool IsAutoSolid(Collider collider)
        {
            if (collider is TerrainCollider) return true;
            if (((1 << collider.gameObject.layer) & _solidLayerMask) != 0) return true;
            return IsFlatSlab(collider.bounds.size);
        }

        private bool IsFlatSlab(Vector3 size)
        {
            float maxThickness = Mathf.Max(0f, _settings.occlusionFlatSolidMaxThickness);
            if (size.y > maxThickness) return false;
            if (Mathf.Max(size.x, size.z) < _settings.occlusionFlatSolidMinExtent) return false;
            return Mathf.Min(size.x, size.z) >= size.y * 4f;
        }

        private OccluderGroup ResolveGroup(Transform root, Collider collider)
        {
            int rootId = root.GetInstanceID();
            if (_groups.TryGetValue(rootId, out OccluderGroup existing)) return existing;

            CollectRenderers(root, collider);
            int limit = Mathf.Max(1, _settings.occlusionMaxRenderersPerGroup);
            OccluderGroup group = _rendererBuffer.Count == 0 || _rendererBuffer.Count > limit
                ? null
                : new OccluderGroup(_rendererBuffer.ToArray());
            _groups[rootId] = group;
            return group;
        }

        private void CollectRenderers(Transform root, Collider collider)
        {
            _rendererBuffer.Clear();
            root.GetComponentsInChildren<Renderer>(true, _rendererBuffer);
            for (int i = _rendererBuffer.Count - 1; i >= 0; i--)
                if (!IsFadeableRenderer(_rendererBuffer[i])) _rendererBuffer.RemoveAt(i);

            if (_rendererBuffer.Count > 0) return;

            // 콜라이더만 자식에 있고 메시는 부모에 있는 구성을 지원한다.
            Renderer parentRenderer = collider.GetComponentInParent<Renderer>();
            if (parentRenderer != null && IsFadeableRenderer(parentRenderer))
                _rendererBuffer.Add(parentRenderer);
        }

        /// <summary>정적·스킨드 메시를 지원하고 파티클·그림자 전용은 제외한다.</summary>
        private bool IsFadeableRenderer(Renderer renderer)
        {
            if (renderer == null || renderer.gameObject.layer == _proxyLayer) return false;
            if (renderer.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly) return false;
            if (renderer is SkinnedMeshRenderer skinned)
            {
                if (skinned.sharedMesh == null) return false;
            }
            else if (renderer is not MeshRenderer || !renderer.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null)
                return false;
            return renderer.sharedMaterials.Length > 0;
        }
    }
}
