using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.Core.ReactiveEnvironment;
using UPlayGround.Data.ReactiveEnvironment;
using UPlayGround.Gameplay.World.ReactiveEnvironment;
using UPlayGround.Manager;

namespace UPlayGround.EditorTools.Tests
{
    /// <summary>소화 프로필·연출 수명·풀 예산을 현재 에셋으로 검사한다.</summary>
    public sealed class ReactiveEnvironmentPresentationTests
    {
        [TestCase(0.1f, 15f)]
        [TestCase(0.25f, 15f)]
        [TestCase(0.5f, 10f)]
        [TestCase(1f, 3f)]
        public void ExposedFarmTreeExtinguishesBeforeRainDeadline(float rain, float deadline)
        {
            var profile = AssetDatabase.LoadAssetAtPath<ReactiveEnvironmentDefinitionSO>("Assets/10.Datas/Environment/Profiles/FarmTree.asset");
            var reaction = NewBurning(profile.reaction);
            for (float time = 0; time < deadline && reaction.State.combustion == EnvironmentCombustion.Burning; time += 0.2f)
                reaction.Tick(0.2f, rain, 1);
            Assert.That(reaction.State.combustion, Is.EqualTo(EnvironmentCombustion.Cooling));
            Assert.That(reaction.State.fuel, Is.GreaterThan(0), "연료 소진을 비 소화 통과로 세지 않는다");
            Assert.That(reaction.State.wetness, Is.GreaterThan(0));
            Assert.That(reaction.State.charAmount, Is.GreaterThan(0));
        }

        [TestCase(0f, 1f)]
        [TestCase(1f, 0f)]
        public void DryOrShelteredTreeKeepsBurning(float rain, float exposure)
        {
            var profile = AssetDatabase.LoadAssetAtPath<ReactiveEnvironmentDefinitionSO>("Assets/10.Datas/Environment/Profiles/FarmTree.asset");
            var reaction = NewBurning(profile.reaction);
            for (int i = 0; i < 50; i++) reaction.Tick(0.2f, rain, exposure);
            Assert.That(reaction.State.combustion, Is.EqualTo(EnvironmentCombustion.Burning));
        }

        [Test]
        public void RepeatedSnapshotsDoNotRestartEmittersOrExtendResidualSmoke()
        {
            var profile = ScriptableObject.CreateInstance<EnvironmentPresentationProfileSO>();
            var prefab = new GameObject("입자 원본"); prefab.AddComponent<ParticleSystem>();
            var service = new Effects();
            Assert.That(Services.TryGet<IParticleEffectService>(out _), Is.False);
            Services.Register(service);
            var presentation = new EnvironmentCombustionPresentation(profile);
            try
            {
                profile.flame = profile.smoke = prefab;
                var burning = new EnvironmentReactionState { heat = 1, fuel = 20, combustion = EnvironmentCombustion.Burning };
                presentation.Apply(burning); presentation.Tick(0.2f, Vector3.zero, Vector2.zero);
                Assert.That(service.rents, Is.EqualTo(2));
                for (int i = 0; i < 20; i++) { burning.revision++; presentation.Apply(burning); presentation.Tick(0.2f, Vector3.zero, Vector2.zero); }
                Assert.That(service.rents, Is.EqualTo(2));
                float fullSize = service.First.main.startSize.constantMax;
                burning.heat = 0.4f; burning.wetness = 0.5f;
                presentation.Apply(burning); presentation.Tick(0.2f, Vector3.zero, Vector2.zero);
                Assert.That(service.First.main.startSize.constantMax, Is.LessThan(fullSize), "비/물 약화는 입자 높이와 크기도 줄인다");
                burning.combustion = EnvironmentCombustion.Cooling;
                for (int i = 0; i < 50; i++) { presentation.Apply(burning); presentation.Tick(0.2f, Vector3.zero, Vector2.zero); }
                Assert.That(presentation.HasFlame, Is.False);
                Assert.That(presentation.HasSmoke, Is.False, "Cooling 반복 적용으로 잔연 무한 연장 금지");
                Assert.That(service.returns, Is.EqualTo(2));
                presentation.Release(); presentation.Release();
                Assert.That(service.returns, Is.EqualTo(2));
                burning.combustion = EnvironmentCombustion.Burning;
                presentation.Apply(burning); presentation.Tick(0.2f, Vector3.zero, Vector2.zero);
                Assert.That(presentation.HasFlame, Is.True);
                presentation.Release();
                Assert.That(service.clearReturns, Is.EqualTo(2));
            }
            finally
            {
                presentation.Release(); Services.Unregister(service); service.Dispose();
                UnityEngine.Object.DestroyImmediate(prefab); UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void ColdRestorationDoesNotEmitAndBudgetExhaustionDoesNotChangeSnapshot()
        {
            var profile = ScriptableObject.CreateInstance<EnvironmentPresentationProfileSO>();
            profile.flame = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Environment/Reactive/Effects/PF_EnvironmentFlame.prefab");
            profile.smoke = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Environment/Reactive/Effects/PF_EnvironmentSmoke.prefab");
            var presentation = new EnvironmentCombustionPresentation(profile);
            var service = new Effects { exhausted = true };
            Services.Register(service);
            try
            {
                var cold = new EnvironmentReactionState { charAmount = 1, combustion = EnvironmentCombustion.Cooling };
                presentation.Apply(cold); presentation.Tick(0.2f, Vector3.zero, Vector2.zero);
                Assert.That(service.rents, Is.Zero);
                cold.heat = 1; cold.fuel = 1; cold.combustion = EnvironmentCombustion.Burning;
                presentation.Apply(cold); presentation.Tick(0.2f, Vector3.zero, Vector2.zero);
                Assert.That(presentation.HasFlame, Is.False);
                Assert.That(service.rents, Is.EqualTo(2), "실제 효과 참조로 고갈 경로까지 도달한다");
                Assert.That(cold.combustion, Is.EqualTo(EnvironmentCombustion.Burning));
            }
            finally { presentation.Release(); Services.Unregister(service); service.Dispose(); UnityEngine.Object.DestroyImmediate(profile); }
        }

        [Test]
        public void FarmTreeLeavesPreserveAtlasAndDisableCracksCoverageAndGlowInEveryLod()
        {
            for (int i = 1; i <= 2; i++)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>($"Assets/11.Material/EnvironmentReactive/FarmTree_{i}.mat");
                Assert.That(material.GetFloat("_SurfaceRole"), Is.EqualTo(1));
                Assert.That(material.GetFloat("_AllowFuelCoverage"), Is.Zero);
                Assert.That(material.GetFloat("_CrackOpacity"), Is.Zero);
                Assert.That(material.GetFloat("_GlowScale"), Is.Zero);
                Assert.That(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(material.mainTexture)), Is.EqualTo("2a63c1abe64a2bc4187173538afedab8"));
            }
        }

        [Test]
        public void AuthoredEffectsHaveUrpMaterialsNoAutoplayAndNoGameplayWriters()
        {
            foreach (string name in new[] { "Flame", "Smoke", "LandingDust", "WoodChips", "FallingLeaves" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/03.Prefabs/Environment/Reactive/Effects/PF_Environment{name}.prefab");
                Assert.That(prefab, Is.Not.Null);
                Assert.That(prefab.GetComponentsInChildren<MonoBehaviour>(true), Is.Empty);
                Assert.That(prefab.GetComponentsInChildren<Collider>(true), Is.Empty);
                foreach (var ps in prefab.GetComponentsInChildren<ParticleSystem>(true))
                {
                    Assert.That(ps.main.playOnAwake, Is.False);
                    Assert.That(ps.collision.enabled, Is.False);
                    Assert.That(ps.GetComponent<ParticleSystemRenderer>().sharedMaterial.shader.name, Does.StartWith("Universal Render Pipeline/"));
                    Assert.That(ps.main.maxParticles, Is.LessThanOrEqualTo(96));
                }
            }
        }

        [TestCase(22f)]
        [TestCase(45f)]
        [TestCase(70f)]
        public void AcceleratedFallChecksIntermediateAnglesAndStopsAtSafePose(float blockerAngle)
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            var recipe = ScriptableObject.CreateInstance<EnvironmentDestructionRecipeSO>();
            try
            {
                var root = new GameObject("낙하 검사"); root.transform.position = new Vector3(30000, 4000, 30000);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
                var pivot = new GameObject("피벗").transform; pivot.SetParent(root.transform, false);
                var destruction = root.AddComponent<ReactiveEnvironmentDestruction>();
                typeof(ReactiveEnvironmentDestruction).GetField("_fallPivot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(destruction, pivot);
                recipe.mode = EnvironmentDestructionMode.DirectionalFall; recipe.fallAcceleration = 2.2f;
                recipe.fallSeconds = 1; recipe.trunkLength = 2; recipe.trunkRadius = 0.05f; recipe.fallBlockerLayers = 1;
                var state = new UPlayGround.Data.Save.EnvironmentDestructionSaveState
                { initialized = true, isFalling = true, direction = new UPlayGround.Data.Save.SerializableVector3(Vector3.right) };
                destruction.Apply(recipe, state, true);
                var blocker = new GameObject("중간 각도 장애물");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(blocker, scene);
                blocker.transform.position = root.transform.position + new Vector3(Mathf.Sin(blockerAngle * Mathf.Deg2Rad), Mathf.Cos(blockerAngle * Mathf.Deg2Rad), 0) * 2;
                blocker.AddComponent<BoxCollider>().size = Vector3.one * 0.03f;
                Physics.SyncTransforms();
                destruction.FixedTick(1);
                Assert.That(state.isFalling, Is.False);
                Assert.That(state.progress, Is.GreaterThan(0));
                Assert.That(state.progress * recipe.fallAngle, Is.LessThan(blockerAngle + recipe.sweepStepDegrees));
                Assert.That(state.progress, Is.LessThan(1), "빠른 후반 구간에서도 장애물을 건너뛰지 않는다");
                Quaternion safePose = pivot.localRotation;
                destruction.FixedTick(1);
                Assert.That(pivot.localRotation, Is.EqualTo(safePose));
            }
            finally
            {
                UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
                UnityEngine.Object.DestroyImmediate(recipe);
            }
        }

        [Test]
        public void ExistingFxManagerReusesClearedLeaseAndHonorsPrefabBudget()
        {
            var root = new GameObject("FX 수명 검사"); root.SetActive(false);
            var prefab = new GameObject("효과 원본"); prefab.SetActive(false); prefab.AddComponent<ParticleSystem>();
            try
            {
                var manager = root.AddComponent<GameObjectManager>();
                var first = manager.RentParticles(prefab, 1);
                Assert.That(first, Is.Not.Null);
                Assert.That(manager.RentParticles(prefab, 1), Is.Null);
                manager.ReturnParticles(first, true);
                var second = manager.RentParticles(prefab, 1);
                Assert.That(second, Is.SameAs(first));
                manager.ReturnParticles(second, true); manager.ReturnParticles(second, true);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(prefab); }
        }

        [Test]
        public void AuthoredEmittersKeepCompatibleVelocityCurvesAcrossWindChanges()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            var root = new GameObject("실제 FX 풀 검사");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            var manager = root.AddComponent<GameObjectManager>();
            Assert.That(Services.TryGet<IParticleEffectService>(out _), Is.False);
            Services.Register(manager);
            var presentation = new EnvironmentCombustionPresentation(AssetDatabase.LoadAssetAtPath<EnvironmentPresentationProfileSO>(
                "Assets/10.Datas/Environment/Profiles/TreePresentation.asset"));
            try
            {
                presentation.Apply(new EnvironmentReactionState { heat = 1, fuel = 20, combustion = EnvironmentCombustion.Burning });
                presentation.Tick(0.2f, Vector3.zero, new Vector2(1, -1));
                presentation.Tick(0.2f, Vector3.zero, new Vector2(-1, 1));
                foreach (var particles in root.GetComponentsInChildren<ParticleSystem>()) particles.Simulate(0.5f, true, false);
                Assert.That(presentation.HasFlame && presentation.HasSmoke, Is.True);
                UnityEngine.TestTools.LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                presentation.Release(); Services.Unregister(manager);
                UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [Test]
        public void OffscreenBudgetOnlyReleasesVisualLeases()
        {
            var profile = ScriptableObject.CreateInstance<EnvironmentPresentationProfileSO>();
            var cameraRoot = new GameObject("화면 예산 카메라");
            profile.flame = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Environment/Reactive/Effects/PF_EnvironmentFlame.prefab");
            profile.smoke = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Environment/Reactive/Effects/PF_EnvironmentSmoke.prefab");
            var camera = cameraRoot.AddComponent<Camera>();
            var view = new CameraView(camera);
            var effects = new Effects();
            Services.Register(view); Services.Register(effects);
            var presentation = new EnvironmentCombustionPresentation(profile);
            try
            {
                var state = new EnvironmentReactionState { heat = 1, fuel = 10, combustion = EnvironmentCombustion.Burning };
                presentation.Apply(state);
                presentation.Tick(0.2f, Vector3.back * 5, Vector2.zero);
                Assert.That(effects.rents, Is.Zero);
                presentation.Tick(0.2f, Vector3.forward * 5, Vector2.zero);
                Assert.That(effects.rents, Is.EqualTo(2));
                camera.transform.position = Vector3.back * (profile.maximumDistance + 1);
                presentation.Tick(0.2f, Vector3.forward * 5, Vector2.zero);
                Assert.That(effects.returns, Is.EqualTo(2), "화면 안이어도 카메라 거리 예산 밖이면 효과만 반환한다.");
                camera.transform.position = Vector3.zero;
                presentation.Tick(0.2f, Vector3.forward * 5, Vector2.zero);
                Assert.That(effects.rents, Is.EqualTo(4), "카메라 재접근 시 기존 연소 snapshot으로 효과를 다시 대여한다.");
                presentation.Tick(0.2f, Vector3.back * 5, Vector2.zero);
                Assert.That(effects.returns, Is.EqualTo(4));
                Assert.That(state.combustion, Is.EqualTo(EnvironmentCombustion.Burning));
                Assert.That(state.fuel, Is.EqualTo(10));
            }
            finally
            {
                presentation.Release(); Services.Unregister(view); Services.Unregister(effects); effects.Dispose();
                UnityEngine.Object.DestroyImmediate(cameraRoot); UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        private sealed class CameraView : ICameraViewService
        {
            private readonly Camera _camera;
            public CameraView(Camera camera) => _camera = camera;
            public UPlayGround.CameraSystem.CameraModeType CurrentCameraMode => default;
            public Camera GetMainCamera() => _camera;
        }

        private static EnvironmentReaction NewBurning(EnvironmentReactionParameters p) => new(p,
            new EnvironmentReactionState { fuel = p.initialFuel, durability = p.maxDurability, heat = 1, combustion = EnvironmentCombustion.Burning });

        private sealed class Effects : IParticleEffectService
        {
            public int rents, returns, clearReturns;
            public bool exhausted;
            private readonly List<GameObject> _instances = new();
            public ParticleSystem First => _instances[0].GetComponent<ParticleSystem>();
            public ParticleSystem RentParticles(GameObject prefab, int maximum)
            {
                rents++;
                if (exhausted) return null;
                var instance = new GameObject("대여 효과"); _instances.Add(instance);
                return instance.AddComponent<ParticleSystem>();
            }
            public void ReturnParticles(ParticleSystem particles, bool clear) { returns++; if (clear) clearReturns++; }
            public void Dispose() { foreach (var instance in _instances) UnityEngine.Object.DestroyImmediate(instance); }
        }
    }
}
