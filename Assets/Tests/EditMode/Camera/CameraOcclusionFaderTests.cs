using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UPlayGround.CameraSystem;
using UPlayGround.CameraSystem.Rendering;
using UPlayGround.Data;

namespace UPlayGround.CameraOcclusion.Tests
{
    /// <summary>카메라 가림 페이드의 분류·페이드 수명·프록시 정리·충돌 제외를 검증한다.</summary>
    public sealed class CameraOcclusionFaderTests
    {
        private const string ProxyLayerName = "CameraFadeProxy";

        private readonly List<GameObject> _spawned = new();
        private CameraSettings _settings;
        private int _proxyLayer;
        private int _solidLayer;

        [SetUp]
        public void SetUp()
        {
            _proxyLayer = LayerMask.NameToLayer(ProxyLayerName);
            Assert.GreaterOrEqual(_proxyLayer, 0, $"레이어 '{ProxyLayerName}'가 TagManager에 정의돼야 한다.");
            _solidLayer = LayerMask.NameToLayer("Water");
            _settings = ScriptableObject.CreateInstance<CameraSettings>();

            // 렌더러는 지연 생성되므로 접근해서 Feature가 등록되게 한다.
            _ = (GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset)?.scriptableRenderer;
            Assert.IsTrue(CameraOccluderFadeState.IsFeatureRegistered,
                "활성 렌더러에 CameraOccluderFadeFeature가 등록돼야 한다.");
        }

        [TearDown]
        public void TearDown()
        {
            CameraOccluderFadeState.Clear();
            foreach (GameObject go in _spawned)
                if (go != null) Object.DestroyImmediate(go);
            _spawned.Clear();
            Object.DestroyImmediate(_settings);
        }

        private CameraOcclusionFader CreateFader(Transform ignoredRoot = null)
            => new CameraOcclusionFader(_settings, ignoredRoot, ~0, 1 << _solidLayer, _proxyLayer);

        private GameObject CreateBox(string name, Vector3 position, Vector3 scale, bool withRenderer = true)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = scale;
            if (!withRenderer) Object.DestroyImmediate(go.GetComponent<MeshRenderer>());
            _spawned.Add(go);
            Physics.SyncTransforms();
            return go;
        }

        [Test]
        public void SkinnedRig_WithSiblingBodyCollider_FadesAndRestores()
        {
            using var fader = CreateFader();
            GameObject rig = CreateBox("Rig", new Vector3(0, 1.2f, -3), Vector3.one);
            Mesh mesh = rig.GetComponent<MeshFilter>().sharedMesh;
            Material material = rig.GetComponent<MeshRenderer>().sharedMaterial;
            Object.DestroyImmediate(rig.GetComponent<MeshRenderer>());
            rig.AddComponent<Animator>();
            var visual = new GameObject("Visual");
            visual.transform.SetParent(rig.transform, false);
            var source = visual.AddComponent<SkinnedMeshRenderer>();
            source.sharedMesh = mesh;
            source.sharedMaterial = material;
            source.rootBone = rig.transform;
            var body = new GameObject("BodyCollider");
            body.transform.SetParent(rig.transform, false);
            var collider = body.AddComponent<BoxCollider>();
            Physics.SyncTransforms();

            Assert.IsFalse(fader.IsBlocking(collider));
            Run(fader, 10);
            Assert.AreEqual(1, fader.ActiveGroupCount, "리그의 여러 충돌체는 한 그룹이어야 한다.");
            Assert.AreEqual(ShadowCastingMode.ShadowsOnly, source.shadowCastingMode);
            var proxy = visual.transform.GetChild(0).GetComponent<SkinnedMeshRenderer>();
            Assert.AreSame(mesh, proxy.sharedMesh);
            Assert.AreSame(source.rootBone, proxy.rootBone);
            source.enabled = false;
            fader.Advance(0.01f);
            Assert.IsFalse(proxy.enabled);
            fader.ReleaseAll();
            Assert.AreEqual(ShadowCastingMode.On, source.shadowCastingMode);
            Assert.AreEqual(0, visual.transform.childCount);
        }

        private static readonly Vector3 CameraPosition = new Vector3(0f, 1.2f, -8f);
        private static readonly Vector3 AimPoint = new Vector3(0f, 1.2f, 0f);

        private static void Run(CameraOcclusionFader fader, int frames, bool probe = true)
        {
            for (int i = 0; i < frames; i++)
            {
                if (probe) fader.Probe(CameraPosition, AimPoint);
                fader.Advance(0.05f);
            }
        }

        [Test]
        public void ActiveRenderer_ExcludesProxyLayerFromMainPassesAndHostsFeature()
        {
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            Assert.IsNotNull(pipeline, "URP 파이프라인이 활성이어야 한다.");

            var serialized = new UnityEditor.SerializedObject(pipeline);
            UnityEditor.SerializedProperty list = serialized.FindProperty("m_RendererDataList");
            Assert.Greater(list.arraySize, 0);

            for (int i = 0; i < list.arraySize; i++)
            {
                var data = list.GetArrayElementAtIndex(i).objectReferenceValue as UniversalRendererData;
                if (data == null) continue;

                bool hasFeature = false;
                foreach (ScriptableRendererFeature feature in data.rendererFeatures)
                    hasFeature |= feature is CameraOccluderFadeFeature;
                if (!hasFeature) continue;

                var rendererData = new UnityEditor.SerializedObject(data);
                foreach (string maskName in new[] { "m_PrepassLayerMask", "m_OpaqueLayerMask", "m_TransparentLayerMask" })
                {
                    long bits = rendererData.FindProperty(maskName).FindPropertyRelative("m_Bits").longValue;
                    Assert.AreEqual(0L, bits & (1L << _proxyLayer),
                        $"{data.name}의 {maskName}에서 '{ProxyLayerName}' 레이어를 제외해야 한다. 그러지 않으면 프록시가 메인 패스에 그려져 벽이 불투명하게 남는다.");
                }
            }
        }

        [Test]
        public void IsBlocking_ClassifiesByLayerShapeAndRenderer()
        {
            using var fader = CreateFader();
            GameObject wall = CreateBox("Wall", new Vector3(0, 1.5f, -3), new Vector3(6, 4, 0.3f));
            GameObject slab = CreateBox("Slab", new Vector3(0, -0.15f, 0), new Vector3(20, 0.3f, 20));
            GameObject smallFlatProp = CreateBox("SmallFlatProp", new Vector3(4, 0.1f, 4), new Vector3(0.9f, 0.2f, 0.9f));
            GameObject barrier = CreateBox("InvisibleBarrier", new Vector3(5, 1.5f, -3), new Vector3(6, 4, 0.3f), withRenderer: false);
            GameObject water = CreateBox("SolidLayer", new Vector3(-5, 1.5f, -3), new Vector3(6, 4, 0.3f));
            water.layer = _solidLayer;

            Assert.IsFalse(fader.IsBlocking(wall.GetComponent<Collider>()), "렌더러가 있는 벽은 Fade여야 한다.");
            Assert.IsTrue(fader.IsBlocking(slab.GetComponent<Collider>()), "얇고 넓은 바닥형은 Solid여야 한다.");
            Assert.IsFalse(fader.IsBlocking(smallFlatProp.GetComponent<Collider>()), "작고 얇은 소품은 바닥이 아니므로 Fade여야 한다.");
            Assert.IsTrue(fader.IsBlocking(barrier.GetComponent<Collider>()), "렌더러 없는 콜라이더는 Solid여야 한다.");
            Assert.IsTrue(fader.IsBlocking(water.GetComponent<Collider>()), "Solid 레이어는 Solid여야 한다.");
        }

        [Test]
        public void IsBlocking_AuthoringOverrideWinsOverAutoRules()
        {
            using var fader = CreateFader();
            GameObject forcedSolid = CreateBox("ForcedSolid", new Vector3(0, 1.5f, -3), new Vector3(6, 4, 0.3f));
            SetMode(forcedSolid, OccluderMode.Solid);
            GameObject forcedFadeSlab = CreateBox("ForcedFadeSlab", new Vector3(0, -0.15f, 10), new Vector3(20, 0.3f, 20));
            SetMode(forcedFadeSlab, OccluderMode.Fade);
            GameObject ignored = CreateBox("Ignored", new Vector3(8, 1.5f, -3), new Vector3(6, 4, 0.3f));
            SetMode(ignored, OccluderMode.Ignore);

            Assert.IsTrue(fader.IsBlocking(forcedSolid.GetComponent<Collider>()));
            Assert.IsFalse(fader.IsBlocking(forcedFadeSlab.GetComponent<Collider>()));
            Assert.IsFalse(fader.IsBlocking(ignored.GetComponent<Collider>()));
        }

        private static void MarkCover(GameObject go)
        {
            CameraOccluder occluder = go.GetComponent<CameraOccluder>() ?? go.AddComponent<CameraOccluder>();
            typeof(CameraOccluder).GetField("_coversInterior", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(occluder, true);
        }

        private static void SetMode(GameObject go, OccluderMode mode)
        {
            CameraOccluder occluder = go.GetComponent<CameraOccluder>() ?? go.AddComponent<CameraOccluder>();
            typeof(CameraOccluder).GetField("_mode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(occluder, mode);
        }

        [Test]
        public void Probe_FadesWallBehindProxyThenRestoresOriginal()
        {
            using var fader = CreateFader();
            GameObject wall = CreateBox("Wall", new Vector3(0, 1.5f, -3), new Vector3(6, 4, 0.3f));
            MeshRenderer renderer = wall.GetComponent<MeshRenderer>();
            ShadowCastingMode originalMode = renderer.shadowCastingMode;

            Run(fader, 40);

            Assert.AreEqual(1, fader.ActiveGroupCount);
            Assert.AreEqual(ShadowCastingMode.ShadowsOnly, renderer.shadowCastingMode, "원본은 그림자만 남겨야 한다.");
            Assert.AreEqual(1, wall.transform.childCount, "프록시가 하나 생겨야 한다.");
            Assert.AreEqual(_proxyLayer, wall.transform.GetChild(0).gameObject.layer);
            Assert.AreEqual(1, CameraOccluderFadeState.ActiveSlotMask);
            int minLevel = Mathf.RoundToInt(_settings.occlusionMinVisibility * CameraOccluderFadeState.Levels);
            Assert.AreEqual(minLevel, CameraOccluderFadeState.GetLevel(0));

            fader.ReleaseAll();

            Assert.AreEqual(originalMode, renderer.shadowCastingMode);
            Assert.AreEqual(0, wall.transform.childCount, "프록시가 남으면 안 된다.");
            Assert.AreEqual(0, CameraOccluderFadeState.ActiveSlotMask);
        }

        [Test]
        public void Advance_ReleasesGroupAfterLineOfSightClearsAndFadeInCompletes()
        {
            using var fader = CreateFader();
            GameObject wall = CreateBox("Wall", new Vector3(0, 1.5f, -3), new Vector3(6, 4, 0.3f));
            Run(fader, 20);
            Assert.AreEqual(1, fader.ActiveGroupCount);

            // 시야선 조사가 끊기면 유예 후 복귀하고, 복귀가 끝나면 프록시를 거둔다.
            Run(fader, 60, probe: false);

            Assert.AreEqual(0, fader.ActiveGroupCount);
            Assert.AreEqual(0, wall.transform.childCount);
            Assert.AreEqual(ShadowCastingMode.On, wall.GetComponent<MeshRenderer>().shadowCastingMode);
        }

        [Test]
        public void Probe_IgnoresIgnoredRootHierarchy()
        {
            GameObject player = CreateBox("Player", new Vector3(0, 1.5f, -3), new Vector3(6, 4, 0.3f));
            using var fader = CreateFader(player.transform);

            Run(fader, 20);

            Assert.AreEqual(0, fader.ActiveGroupCount, "플레이어 계층은 페이드 대상이 아니다.");
        }

        [Test]
        public void Advance_RespectsMaxGroupsLimit()
        {
            _settings.occlusionMaxGroups = 1;
            using var fader = CreateFader();
            CreateBox("WallNear", new Vector3(0, 1.5f, -3), new Vector3(6, 4, 0.3f));
            CreateBox("WallFar", new Vector3(0, 1.5f, -5), new Vector3(6, 4, 0.3f));

            Run(fader, 20);

            Assert.AreEqual(1, fader.ActiveGroupCount);
        }

        [Test]
        public void Advance_ReleasesEverythingWhenDisabledAtRuntime()
        {
            using var fader = CreateFader();
            GameObject wall = CreateBox("Wall", new Vector3(0, 1.5f, -3), new Vector3(6, 4, 0.3f));
            Run(fader, 20);
            Assert.AreEqual(1, fader.ActiveGroupCount);

            _settings.enableOcclusionFade = false;
            fader.Advance(0.05f);

            Assert.AreEqual(0, fader.ActiveGroupCount);
            Assert.AreEqual(0, wall.transform.childCount);
            Assert.IsTrue(fader.IsBlocking(wall.GetComponent<Collider>()), "끄면 기존처럼 모든 충돌체가 카메라를 막아야 한다.");
        }

        [Test]
        public void Dispose_RemovesAllProxies()
        {
            var fader = CreateFader();
            GameObject wall = CreateBox("Wall", new Vector3(0, 1.5f, -3), new Vector3(6, 4, 0.3f));
            Run(fader, 20);

            fader.Dispose();

            Assert.AreEqual(0, wall.transform.childCount);
            Assert.AreEqual(ShadowCastingMode.On, wall.GetComponent<MeshRenderer>().shadowCastingMode);
        }

        [Test]
        public void LimitInterior_ShortensArmWhenCameraWouldEndUnderRoofAndPlayerIsOutside()
        {
            using var fader = CreateFader();
            // 지붕은 z -10~0 구간을 덮는다. 플레이어(피벗)는 z=5에서 지붕 밖, 카메라 끝점은 z=-3으로 지붕 아래.
            MarkCover(CreateBox("Roof", new Vector3(0, 4f, -5f), new Vector3(10, 0.3f, 10)));
            var pivot = new Vector3(0, 1.2f, 5f);

            float limited = fader.LimitInterior(pivot, Vector3.back, 8f);

            Assert.Less(limited, 6f, "지붕 아래로 들어가는 암은 줄어야 한다.");
            Assert.Greater(limited, 4f, "지붕 가장자리 근처까지는 유지해야 한다.");
        }

        [Test]
        public void LimitInterior_DoesNotLimitWhenPlayerIsAlsoUnderRoofOrCameraEndsInOpenAir()
        {
            using var fader = CreateFader();
            MarkCover(CreateBox("Roof", new Vector3(0, 4f, -5f), new Vector3(10, 0.3f, 10)));

            float playerInside = fader.LimitInterior(new Vector3(0, 1.2f, -5f), Vector3.back, 3f);
            Assert.AreEqual(3f, playerInside, 0.001f, "플레이어도 실내면 제한하지 않는다.");

            float endsOutside = fader.LimitInterior(new Vector3(0, 1.2f, 5f), Vector3.back, 3f);
            Assert.AreEqual(3f, endsOutside, 0.001f, "끝점이 열린 공간이면 제한하지 않는다.");

            float throughToFarSide = fader.LimitInterior(new Vector3(0, 1.2f, 5f), Vector3.back, 16f);
            Assert.AreEqual(16f, throughToFarSide, 0.001f, "건물 뒤쪽 열린 공간으로 나가는 것은 허용한다.");
        }

        [Test]
        public void LimitInterior_IgnoresColliderNotMarkedAsCover()
        {
            using var fader = CreateFader();
            // 나무 캐노피처럼 표시되지 않은 위쪽 콜라이더는 지붕이 아니다.
            CreateBox("Canopy", new Vector3(0, 4f, -5f), new Vector3(10, 0.3f, 10));

            Assert.AreEqual(8f, fader.LimitInterior(new Vector3(0, 1.2f, 5f), Vector3.back, 8f), 0.001f);
        }

        [Test]
        public void LimitInterior_IsDisabledBySetting()
        {
            _settings.enableOcclusionInteriorGuard = false;
            using var fader = CreateFader();
            MarkCover(CreateBox("Roof", new Vector3(0, 4f, -5f), new Vector3(10, 0.3f, 10)));

            Assert.AreEqual(8f, fader.LimitInterior(new Vector3(0, 1.2f, 5f), Vector3.back, 8f), 0.001f);
        }

        [Test]
        public void CameraCollision_PassesThroughFadeWallButStopsAtSolidSlab()
        {
            using var fader = CreateFader();
            var collision = new CameraCollision(_settings, null, ~0, 7f, fader);
            try
            {
                GameObject wall = CreateBox("Wall", new Vector3(0, 1.5f, -3), new Vector3(6, 4, 0.3f));
                float throughWall = collision.GetAvailableDistance(AimPoint, Vector3.back, 7f);
                Assert.AreEqual(7f, throughWall, 0.01f, "Fade 벽은 카메라 암을 접지 않는다.");

                Object.DestroyImmediate(wall);
                Physics.SyncTransforms();
                CreateBox("SolidWall", new Vector3(0, 1.5f, -3), new Vector3(6, 4, 0.3f), withRenderer: false);
                float stopped = collision.GetAvailableDistance(AimPoint, Vector3.back, 7f);
                Assert.Less(stopped, 3f, "렌더러 없는 벽은 Solid로 암을 접어야 한다.");
            }
            finally
            {
                collision.Dispose();
            }
        }
    }
}
