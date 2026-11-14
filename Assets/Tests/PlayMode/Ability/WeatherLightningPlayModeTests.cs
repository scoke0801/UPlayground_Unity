#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UPlayGround.Components;
using UPlayGround.Data.ReactiveEnvironment;
using UPlayGround.Data.Save;

namespace UPlayGround.Ability.PlayModeTests
{
    /// <summary>실제 물리 첫 표면·접점 연출·중앙 열 입력을 짧은 독립 씬에서 검사한다.</summary>
    public sealed class WeatherLightningPlayModeTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, Hidden).SetValue(target, value);
        private static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(target, args);

        [UnityTest]
        public IEnumerator GroundStrikeSharesVisualPointAndRoofBlocksLowerTarget()
        {
            var scene = SceneManager.CreateScene("낙뢰 독립 검사");
            var ownership = Runtime("UPlayGround.Manager.World.RegionOwnership");
            ownership.GetMethod("Register").Invoke(null, new object[] { scene, "LightningTest" });
            var root = new GameObject("낙뢰 검사");
            SceneManager.MoveGameObjectToScene(root, scene);
            var worldObject = new GameObject("낙뢰 검사 매니저");
            var definition = ScriptableObject.CreateInstance<ReactiveEnvironmentDefinitionSO>();
            var profile = ScriptableObject.CreateInstance<EnvironmentLightningProfileSO>();
            Mesh waterMesh = null;
            try
            {
                var world = worldObject.AddComponent(Runtime("UPlayGround.Manager.WorldStateManager"));
                var weatherObject = new GameObject("날씨 입력 fixture");
                weatherObject.transform.SetParent(root.transform); weatherObject.SetActive(false);
                var weather = weatherObject.AddComponent(Runtime("UPlayGround.Gameplay.World.LivingWorld.EnvironmentController"));
                definition.definitionId = "test.lightning.tree";
                var reaction = typeof(ReactiveEnvironmentDefinitionSO).GetField("reaction").GetValue(definition);
                reaction.GetType().GetField("canBreak").SetValue(reaction, false);
                var origin = new Vector3(10000f, 10000f, 10000f);
                var targetObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                targetObject.name = "가연성 표면"; targetObject.transform.SetParent(root.transform);
                targetObject.SetActive(false); targetObject.layer = 29;
                targetObject.transform.position = origin;
                targetObject.AddComponent<SceneEntityId>().RuntimeSetGuid("lightning-target");
                var target = targetObject.AddComponent(Runtime("UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentObject"));
                var sample = new GameObject("표면 샘플").transform;
                sample.SetParent(targetObject.transform, false); sample.localPosition = Vector3.up * 0.6f;
                Set(target, "_definition", definition); Set(target, "_weather", weather);
                Set(target, "_rainSamples", new[] { sample }); Set(target, "_heatSamples", new[] { sample });
                Set(target, "_hitColliders", new[] { targetObject.GetComponent<Collider>() });
                targetObject.SetActive(true);
                Assert.That((bool)Call(world, "RegisterEnvironment", target), Is.True);
                profile.profileId = "test.weather.lightning";
                profile.areas = new[] { new EnvironmentLightningArea { sourceBounds = new Bounds(origin + Vector3.up * 10f, new Vector3(20f, 0f, 20f)) } };
                profile.surfaceLayers = 1 << 29; profile.blockerLayers = 1 << 29;
                Physics.SyncTransforms();
                object[] request = { "LightningTest", profile, 0, origin + Vector3.up * 10f, 1d, null };
                Assert.That((bool)world.GetType().GetMethod("TryRequestLightning").Invoke(world, request), Is.True);
                var strike = (EnvironmentLightningStrike)request[5];
                Assert.That(strike.Point.y, Is.EqualTo(origin.y + 0.5f).Within(0.001f));
                var save = new WorldStateSaveData(); Call(world, "ExportEnvironment", save);
                Assert.That(save.reactiveEnvironment[0].pendingHeat, Is.EqualTo(profile.heatImpulse));
                Call(world, "TickEnvironment", 0.2f);
                var state = target.GetType().GetProperty("State").GetValue(target);
                Assert.That(state.GetType().GetField("combustion").GetValue(state).ToString(), Is.EqualTo("Burning"));

                var effectsObject = new GameObject("번개 표현"); effectsObject.transform.SetParent(root.transform);
                var effects = effectsObject.AddComponent(Runtime("UPlayGround.Gameplay.World.LivingWorld.EnvironmentEffects"));
                var line = new GameObject("본선").AddComponent<LineRenderer>(); line.transform.SetParent(effectsObject.transform);
                Set(effects, "_lightningBolt", line);
                var light = new GameObject("접점 섬광").AddComponent<Light>(); light.transform.SetParent(effectsObject.transform);
                Set(effects, "_lightningLight", light);
                Call(effects, "Initialize"); Call(effects, "SetWorldLightning", true);
                Call(effects, "PresentLightning", strike, origin + Vector3.right * 343f, 1f);
                Assert.That(line.GetPosition(line.positionCount - 1), Is.EqualTo(strike.Point));
                float delay = (float)effects.GetType().GetField("_thunderCountdown", Hidden).GetValue(effects);
                Assert.That(delay, Is.EqualTo(Vector3.Distance(origin + Vector3.right * 343f, strike.Point) / 343f).Within(0.0001f));
                Call(effects, "SetWorldLightning", false);
                Call(effects, "ApplyStorm", 0f, origin, true, false, 1f, 1f);
                Assert.That(light.transform.position, Is.EqualTo(strike.Point + new Vector3(0f, 25f, 0f)),
                    "미리보기 모드 전환도 이미 확정한 실제 섬광의 접점을 바꾸면 안 된다.");

                var roof = GameObject.CreatePrimitive(PrimitiveType.Cube); roof.name = "첫 타격 지붕";
                roof.transform.SetParent(root.transform); roof.layer = 29;
                roof.transform.position = origin + Vector3.up * 2f; roof.transform.localScale = new Vector3(4f, 0.2f, 4f);
                Physics.SyncTransforms(); request[4] = 2d;
                Assert.That((bool)world.GetType().GetMethod("TryRequestLightning").Invoke(world, request), Is.True);
                var roofStrike = (EnvironmentLightningStrike)request[5];
                Assert.That(roofStrike.Point.y, Is.EqualTo(origin.y + 2.1f).Within(0.002f));
                save = new WorldStateSaveData(); Call(world, "ExportEnvironment", save);
                Assert.That(save.reactiveEnvironment[0].pendingHeat, Is.Zero, "지붕 아래 대상에 관통 열을 주면 안 된다.");
                Assert.That(save.environmentLightning[0].committedSequence, Is.EqualTo(2ul));
                roof.SetActive(false);
                var waterObject = new GameObject("수면 첫 접점"); waterObject.transform.SetParent(root.transform);
                waterObject.transform.position = origin + Vector3.up;
                var filter = waterObject.AddComponent<MeshFilter>();
                var mesh = waterMesh = new Mesh();
                mesh.vertices = new[] { new Vector3(-2,0,-2), new Vector3(2,0,-2), new Vector3(2,0,2), new Vector3(-2,0,2) };
                mesh.triangles = new[] { 0,2,1,0,3,2 }; mesh.RecalculateBounds();
                filter.sharedMesh = mesh;
                var water = waterObject.AddComponent<FootstepWaterSurface>(); water.surfaceMesh = filter; water.RefreshMesh();
                request[4] = 3d;
                Assert.That((bool)world.GetType().GetMethod("TryRequestLightning").Invoke(world, request), Is.True);
                var waterStrike = (EnvironmentLightningStrike)request[5];
                Assert.That(waterStrike.IsWaterSurface, Is.True);
                Assert.That(waterStrike.Point.y, Is.EqualTo(origin.y + 1f).Within(0.002f));
                Assert.That(waterStrike.Heat, Is.Zero);
                save = new WorldStateSaveData(); Call(world, "ExportEnvironment", save);
                Assert.That(save.reactiveEnvironment[0].pendingHeat, Is.Zero);

                // 수면은 바닥 Collider·발소리 우선순위·조회 시작점의 체적 소속에 의존하지 않는다.
                targetObject.GetComponent<Collider>().enabled = false;
                water.useVolumeBounds = true;
                water.volumeBounds = new Bounds(Vector3.down, new Vector3(4, 2, 4));
                var lowerWaterObject = new GameObject("우선순위가 높은 아래 수면");
                lowerWaterObject.transform.SetParent(root.transform);
                lowerWaterObject.transform.position = origin + Vector3.up * 0.7f;
                var lowerFilter = lowerWaterObject.AddComponent<MeshFilter>(); lowerFilter.sharedMesh = mesh;
                var lowerWater = lowerWaterObject.AddComponent<FootstepWaterSurface>();
                lowerWater.surfaceMesh = lowerFilter; lowerWater.priority = 100; lowerWater.RefreshMesh();
                Physics.SyncTransforms(); request[4] = 3.1d;
                Assert.That((bool)world.GetType().GetMethod("TryRequestLightning").Invoke(world, request), Is.True);
                var noFloorStrike = (EnvironmentLightningStrike)request[5];
                Assert.That(noFloorStrike.IsWaterSurface, Is.True);
                Assert.That(noFloorStrike.Point.y, Is.EqualTo(origin.y + 1f).Within(0.002f));
                Assert.That(noFloorStrike.Heat, Is.Zero);
                roof.SetActive(true); Physics.SyncTransforms(); request[4] = 3.2d;
                Assert.That((bool)world.GetType().GetMethod("TryRequestLightning").Invoke(world, request), Is.True);
                Assert.That(((EnvironmentLightningStrike)request[5]).IsWaterSurface, Is.False);
                Assert.That(((EnvironmentLightningStrike)request[5]).Point.y, Is.EqualTo(origin.y + 2.1f).Within(0.002f));
                roof.SetActive(false);
                object[] advance = { "LightningTest", profile, 4d, 1f, null };
                Assert.That((bool)world.GetType().GetMethod("TryAdvanceLightning").Invoke(world, advance), Is.False);
                advance[2] = 1000d;
                Assert.That((bool)world.GetType().GetMethod("TryAdvanceLightning").Invoke(world, advance), Is.False);
                yield return null;
            }
            finally
            {
                ownership.GetMethod("Unregister").Invoke(null, new object[] { scene });
                UnityEngine.Object.Destroy(root); UnityEngine.Object.Destroy(worldObject);
                UnityEngine.Object.Destroy(definition); UnityEngine.Object.Destroy(profile);
                if (waterMesh != null) UnityEngine.Object.Destroy(waterMesh);
                SceneManager.UnloadSceneAsync(scene);
            }
            yield return null;
        }
    }
}
#endif
