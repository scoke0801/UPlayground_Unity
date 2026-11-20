using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KinematicCharacterController;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UPlayGround.WorldBuilder.Data;
using UPlayGround.WorldBuilder.Editor;
using UPlayGround.WorldBuilder.Runtime;
using Object = UnityEngine.Object;

namespace UPlayGround.WorldBuilder.Tests
{
    public sealed class WorldCaveTraversalTests
    {
        private const string StartSceneKey = "WorldBuilder.P06.PreviousPlayStart";
        private const string OpenSceneKey = "WorldBuilder.P06.PreviousActiveScene";
        private const string BuildWindowKey = "WorldBuilder.P06.PreviousBuildWindow";

        [UnityTest]
        public IEnumerator T14_PlayerMotorTraversesSavedPortalsAndAllModuleShapes()
        {
            P06TestSceneScope.Capture(OpenSceneKey);
            SessionState.SetString(StartSceneKey, AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                var type = window.GetType();
                if (type.FullName != "UPlayGround.Tool.Editor.Build.BuildManagerWindow") continue;
                SessionState.SetString(BuildWindowKey, type.AssemblyQualifiedName);
                window.Close();
            }
            EditorSceneManager.OpenScene(WorldEcologyValidationFixture.ScenePath, OpenSceneMode.Single);
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(WorldEcologyValidationFixture.ScenePath);
            bool domainReload = !EditorSettings.enterPlayModeOptionsEnabled ||
                (EditorSettings.enterPlayModeOptions & EnterPlayModeOptions.DisableDomainReload) == 0;

            yield return new EnterPlayMode(domainReload);

            var preview = Object.FindFirstObjectByType<WorldEcologyPreview>();
            Assert.That(preview, Is.Not.Null);
            var source = preview.definition.Ecology;
            var player = source.playerPrefab.GetComponentInChildren<KinematicCharacterMotor>(true);
            var driver = new Driver();
            var motor = CreateMotor(player, driver);
            var motors = new List<KinematicCharacterMotor> { motor };
            var movers = new List<PhysicsMover>();
            var grounded = new HashSet<Collider>();
            const float tick = .02f;

            float Walk(string route, Vector3 start, params Vector3[] waypoints)
            {
                motor.SetPositionAndRotation(start, Quaternion.identity);
                motor.BaseVelocity = Vector3.zero;
                driver.Destination = start;
                Physics.SyncTransforms();
                for (int i = 0; i < 20; i++) KinematicCharacterSystem.Simulate(tick, motors, movers);
                grounded.Clear();
                float lowest = float.PositiveInfinity;
                foreach (var goal in waypoints)
                {
                    int limit = 1600;
                    while (HorizontalDistance(motor.TransientPosition, goal) > .13f && limit-- > 0)
                    {
                        driver.Destination = goal;
                        KinematicCharacterSystem.Simulate(tick, motors, movers);
                        if (motor.GroundingStatus.IsStableOnGround) grounded.Add(motor.GroundingStatus.GroundCollider);
                        lowest = Mathf.Min(lowest, motor.TransientPosition.y);
                    }
                    Assert.That(HorizontalDistance(motor.TransientPosition, goal), Is.AtMost(.13f),
                        route + " · 구간 " + goal + " · 현재 " + motor.TransientPosition);
                    Assert.That(motor.GroundingStatus.IsStableOnGround, Is.True, route + " · " + goal);
                }
                Assert.That(lowest, Is.GreaterThan(.8f), "동굴 바닥 아래로 낙하");
                Debug.Log($"[P06 KCC] {route} · 최저 Y={lowest:F3} · 도착={motor.TransientPosition} · 지지 Collider={grounded.Count}");
                return lowest;
            }

            var exterior = new List<WorldCavePortal>();
            foreach (var portal in preview.portals)
                if (portal.position.y > 8) exterior.Add(portal);
            exterior.Sort((a, b) => a.position.x.CompareTo(b.position.x));
            Assert.That(exterior.Count, Is.EqualTo(2));
            Vector3 west = exterior[0].position + exterior[0].outward * 2;
            Vector3 east = exterior[1].position + exterior[1].outward * 2;
            Vector3 middle = (west + east) * .5f;
            Assert.That(Walk("저장 씬 서→동", west + Vector3.up * .2f, middle, east), Is.LessThan(2));
            Assert.That(grounded, Has.Some.TypeOf<TerrainCollider>());
            Assert.That(grounded, Has.Some.TypeOf<MeshCollider>());
            Assert.That(Walk("저장 씬 동→서", east + Vector3.up * .2f, middle, west), Is.LessThan(2));

            // 남쪽 모듈 전시장은 저장 씬의 지형 표면 위에 배치하고 같은 카탈로그 충돌을 검증한다.
            var branch = Instantiate(source, CaveModuleKind.Branch, 200, 35, 200, 0);
            var turn = Instantiate(source, CaveModuleKind.Bend, 208, 35, 200, 90);
            var room = Instantiate(source, CaveModuleKind.Room, 200, 35, 209, 0);
            var cap = Instantiate(source, CaveModuleKind.EndCap, 208, 35, 193.5f, 180);
            Vector3 center = branch.transform.position;
            var south = center + Vector3.back * 2;
            Walk("분기→곡선→끝막이→분기→방", south + Vector3.up * .18f,
                center, turn.transform.position, cap.transform.position, turn.transform.position, center, room.transform.position);
            foreach (var module in new[] { branch, turn, cap, room })
                Assert.That(grounded, Has.Member(module.floorColliders[0]), module.name + " 바닥을 실제로 밟아야 합니다.");
            Walk("끝막이 충돌 준비", turn.transform.position + Vector3.up * .18f, cap.transform.position);
            driver.Destination = cap.transform.position + Vector3.back * 5;
            for (int i = 0; i < 100; i++) KinematicCharacterSystem.Simulate(tick, motors, movers);
            Assert.That(motor.TransientPosition.z, Is.InRange(cap.transform.position.z - 2, cap.transform.position.z - 1.5f));
            Assert.That(motor.GroundingStatus.GroundCollider, Is.SameAs(cap.floorColliders[0]));
            Debug.Log($"[P06 KCC] 끝막이 벽 통과 차단 · 정지={motor.TransientPosition}");

            yield return new ExitPlayMode();
        }

        internal static KinematicCharacterMotor CreateMotor(KinematicCharacterMotor source, Driver driver)
        {
            var actor = new GameObject("P06 플레이어 KCC 검증");
            actor.SetActive(false);
            actor.layer = source.gameObject.layer;
            var collider = actor.AddComponent<CapsuleCollider>();
            EditorUtility.CopySerialized(source.Capsule, collider);
            var motor = actor.AddComponent<KinematicCharacterMotor>();
            EditorUtility.CopySerialized(source, motor);
            motor.Capsule = collider;
            motor.CharacterController = driver;
            actor.SetActive(true);
            // 실제 모터를 고정 간격으로 실행하므로 자동 등록에서만 빼 둔다.
            KinematicCharacterSystem.UnregisterCharacterMotor(motor);
            Assert.That(collider.radius, Is.EqualTo(source.Capsule.radius));
            Assert.That(collider.height, Is.EqualTo(source.Capsule.height));
            Assert.That(motor.MaxStepHeight, Is.EqualTo(source.MaxStepHeight));
            Assert.That(motor.MaxStableSlopeAngle, Is.EqualTo(source.MaxStableSlopeAngle));
            driver.Motor = motor;
            return motor;
        }

        private static WorldCaveModule Instantiate(WorldEcology source, CaveModuleKind kind, float x, float y, float z, float yaw)
        {
            var preset = source.caveCatalog.Find(value => value.kind == kind);
            return Object.Instantiate(preset.prefab, new Vector3(x, y, z), Quaternion.Euler(0, yaw, 0)).GetComponent<WorldCaveModule>();
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

        [UnityTearDown]
        public IEnumerator RestoreEditor()
        {
            if (Application.isPlaying) yield return new ExitPlayMode();
            if (!P06TestSceneScope.HasCaptured(OpenSceneKey)) yield break;
            string previousStart = SessionState.GetString(StartSceneKey, "");
            EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previousStart) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(previousStart);
            P06TestSceneScope.Restore(OpenSceneKey);
            SessionState.EraseString(StartSceneKey);
            string buildWindow = SessionState.GetString(BuildWindowKey, "");
            SessionState.EraseString(BuildWindowKey);
            if (!string.IsNullOrEmpty(buildWindow)) EditorWindow.GetWindow(Type.GetType(buildWindow, true));
        }

        internal sealed class Driver : ICharacterController
        {
            public KinematicCharacterMotor Motor;
            public Vector3 Destination;

            public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
            {
                var offset = Destination - Motor.TransientPosition;
                offset.y = 0;
                var next = Vector3.ClampMagnitude(offset / deltaTime, 3.8f);
                if (Motor.GroundingStatus.IsStableOnGround)
                    currentVelocity = Motor.GetDirectionTangentToSurface(next, Motor.GroundingStatus.GroundNormal) * next.magnitude;
                else
                    currentVelocity = next + Vector3.up * (currentVelocity.y + Physics.gravity.y * deltaTime);
            }

            public void UpdateRotation(ref Quaternion rotation, float dt) { }
            public void BeforeCharacterUpdate(float dt) { }
            public void PostGroundingUpdate(float dt) { }
            public void AfterCharacterUpdate(float dt) { }
            public bool IsColliderValidForCollisions(Collider collider) => true;
            public void OnGroundHit(Collider collider, Vector3 normal, Vector3 point, ref HitStabilityReport report) { }
            public void OnMovementHit(Collider collider, Vector3 normal, Vector3 point, ref HitStabilityReport report) { }
            public void ProcessHitStabilityReport(Collider collider, Vector3 normal, Vector3 point, Vector3 position, Quaternion rotation, ref HitStabilityReport report) { }
            public void OnDiscreteCollisionDetected(Collider collider) { }
        }
    }

    internal static class P06TestSceneScope
    {
        [Serializable]
        private sealed class Setup
        {
            public string[] paths;
            public bool[] loaded, active;
        }

        internal static void Capture(string key)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
                Assert.That(SceneManager.GetSceneAt(i).isDirty, Is.False, "열린 씬을 먼저 저장하세요.");
            var setup = EditorSceneManager.GetSceneManagerSetup();
            Assert.That(setup.Length == 1 || setup.All(scene => !string.IsNullOrEmpty(scene.path)),
                "가산 씬 구성은 모두 저장되어 있어야 합니다.");
            SessionState.SetString(key, JsonUtility.ToJson(new Setup {
                paths = setup.Select(scene => scene.path).ToArray(),
                loaded = setup.Select(scene => scene.isLoaded).ToArray(),
                active = setup.Select(scene => scene.isActive).ToArray()
            }));
        }

        internal static bool HasCaptured(string key) => !string.IsNullOrEmpty(SessionState.GetString(key, ""));

        internal static void Restore(string key)
        {
            if (!HasCaptured(key)) return;
            var setup = JsonUtility.FromJson<Setup>(SessionState.GetString(key, ""));
            if (setup.paths.Length == 0 || setup.paths.Any(string.IsNullOrEmpty))
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            else
            {
                var scenes = new SceneSetup[setup.paths.Length];
                for (int i = 0; i < scenes.Length; i++)
                    scenes[i] = new SceneSetup { path = setup.paths[i], isLoaded = setup.loaded[i], isActive = setup.active[i] };
                EditorSceneManager.RestoreSceneManagerSetup(scenes);
            }
            SessionState.EraseString(key);
        }
    }
}
