using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KinematicCharacterController;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using UPlayGround.WorldBuilder.Data;
using UPlayGround.WorldBuilder.Editor;
using UPlayGround.WorldBuilder.Runtime;

namespace UPlayGround.WorldBuilder.Tests
{
    public sealed class WorldBakeTraversalTests
    {
        private const string Previous = "WorldBuilder.P07.PreviousScene";
        private const string PreviousStart = "WorldBuilder.P07.PreviousPlayStart";
        private const string PreviousWindow = "WorldBuilder.P07.PreviousBuildWindow";

        [UnityTest]
        public IEnumerator T17_PublishedLinksFollowBothEndpointsAcrossBridgeAndPortals()
        {
            var published = WorldBakePipeline.ReadPublished(WorldBakeValidationFixture.WorldId);
            Assert.That(published, Is.Not.Null, "P07 검증 월드의 revision을 먼저 발행하세요.");
            var catalog = WorldBakePipeline.ReadRevision(published.worldGuid, published.current, reopen: true);
            var layout = AssetDatabase.LoadAssetAtPath<WorldNavigationLayout>(WorldBakeValidationFixture.DataFolder + "/NavigationLayout.asset");
            var authoredPortals = layout.crossings.Where(c => !string.IsNullOrEmpty(c.portalId)).OrderBy(c => c.from.x).ToArray();
            P06TestSceneScope.Capture(Previous);
            SessionState.SetString(PreviousStart, AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (window.GetType().FullName != "UPlayGround.Tool.Editor.Build.BuildManagerWindow") continue;
                SessionState.SetString(PreviousWindow, window.GetType().AssemblyQualifiedName);
                window.Close();
            }
            EditorSceneManager.OpenScene(catalog.scenePath, OpenSceneMode.Single);
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(catalog.scenePath);
            bool domainReload = !EditorSettings.enterPlayModeOptionsEnabled ||
                (EditorSettings.enterPlayModeOptions & EnterPlayModeOptions.DisableDomainReload) == 0;
            yield return new EnterPlayMode(domainReload);

            var navigation = UnityEngine.Object.FindFirstObjectByType<WorldBakedNavigation>();
            Assert.That(navigation, Is.Not.Null);
            catalog = navigation.catalog;
            var filter = new NavMeshQueryFilter { agentTypeID = catalog.agentTypeId, areaMask = NavMesh.AllAreas };
            var path = new NavMeshPath();
            bool Reach(Vector3 from, Vector3 to) => NavMesh.CalculatePath(from, to, filter, path) && path.status == NavMeshPathStatus.PathComplete;

            foreach (var route in catalog.routes)
                Assert.That(Reach(route.from, route.to), Is.True, route.id);

            for (int index = 0; index < catalog.crossings.Length; index++)
            {
                var crossing = catalog.crossings[index];
                var gate = navigation.crossingGates[index];
                foreach (var surface in catalog.surfaces)
                    navigation.SetSurfaceReady(surface.id, false, false);
                navigation.SetSurfaceReady(crossing.fromSurface, true, true);
                navigation.SetSurfaceReady(crossing.toSurface, true, false);
                yield return null;
                Assert.That(navigation.IsLinkReady(index), Is.False, crossing.id);
                Assert.That(Reach(crossing.from, crossing.to), Is.False, crossing.id);
                if (gate != null)
                {
                    Assert.That(gate.CanEnter, Is.False);
                    Assert.That(gate.EntryBarriers.All(barrier => barrier.enabled), Is.True);
                }

                navigation.SetSurfaceReady(crossing.toSurface, true, true);
                yield return null;
                Assert.That(navigation.IsLinkReady(index), Is.True, crossing.id);
                Assert.That(Reach(crossing.from, crossing.to), Is.True, crossing.id);
                if (gate != null)
                {
                    Assert.That(gate.CanEnter, Is.True);
                    Assert.That(gate.EntryBarriers.All(barrier => !barrier.enabled), Is.True);
                }

                navigation.SetSurfaceReady(crossing.fromSurface, false, true);
                yield return null;
                Assert.That(navigation.IsLinkReady(index), Is.False, crossing.id);
                Assert.That(Reach(crossing.from, crossing.to), Is.False, crossing.id);
                if (gate != null) Assert.That(gate.CanEnter, Is.False);
                Debug.Log($"[P07 T17] {crossing.id} · {crossing.fromSurface} → {crossing.toSurface} · 닫힘/열림/닫힘");
            }

            foreach (var surface in catalog.surfaces) navigation.SetSurfaceReady(surface.id, true, true);
            yield return null;
            var driver = new WorldCaveTraversalTests.Driver();
            var player = layout.playerPrefab.GetComponentInChildren<KinematicCharacterMotor>(true);
            var motor = WorldCaveTraversalTests.CreateMotor(player, driver);
            motor.gameObject.name = "P07 플레이어 KCC 검증";
            var motors = new List<KinematicCharacterMotor> { motor };
            var movers = new List<PhysicsMover>();
            const float step = .02f;

            var authoredBridge = layout.crossings.Single(c => !string.IsNullOrEmpty(c.bridgeId));
            SetStart(authoredBridge.from - Vector3.right * .25f);
            var supports = new HashSet<Collider>();
            Walk(authoredBridge.to + Vector3.right * .25f);
            Assert.That(supports.Any(c => c.GetComponentInParent<WorldBridgeAccessGate>() != null), Is.True);
            Debug.Log($"[P07 KCC] 저장 revision 교량 서→동 · 도착 {motor.TransientPosition}");
            supports.Clear();
            var west = authoredPortals.First().from - Vector3.right * .5f;
            var east = authoredPortals.Last().from + Vector3.right * .5f;
            var middle = (west + east) * .5f;
            var water = UnityEngine.Object.FindFirstObjectByType<WorldWaterQuery>();
            var dryMarker = UnityEngine.Object.FindFirstObjectByType<WorldBakedWaterExclusions>();
            var inside = new Vector3(middle.x, 1.4f, middle.z);
            Assert.That(dryMarker.volumes.Any(volume => volume.Contains(inside)), Is.True);
            Assert.That(water.TrySample(inside, out _), Is.False);
            Assert.That(water.TrySample(new Vector3(middle.x, 8, middle.z), out _), Is.True);
            Debug.Log("[P07 P05/P06] 같은 XZ의 동굴 내부 제외와 지상 수계 유지");
            SetStart(west);
            Walk(middle);
            Walk(east);
            Assert.That(supports.Any(c => c is TerrainCollider), Is.True);
            Assert.That(supports.Any(c => c is MeshCollider && c.GetComponentInParent<WorldCaveModule>() != null), Is.True);
            Debug.Log($"[P07 KCC] 저장 revision 동굴 서→동 · 지지 Collider {supports.Count}개 · 도착 {motor.TransientPosition}");
            yield return new ExitPlayMode();

            void SetStart(Vector3 point)
            {
                motor.SetPositionAndRotation(point + Vector3.up * .3f, Quaternion.identity);
                motor.BaseVelocity = Vector3.zero;
                driver.Destination = point;
                Physics.SyncTransforms();
                for (int i = 0; i < 25; i++) KinematicCharacterSystem.Simulate(step, motors, movers);
            }

            void Walk(Vector3 target)
            {
                int budget = 1600;
                driver.Destination = target;
                Vector3 HorizontalDelta() => Vector3.ProjectOnPlane(motor.TransientPosition - target, Vector3.up);
                while (HorizontalDelta().sqrMagnitude > .017f && budget-- > 0)
                {
                    KinematicCharacterSystem.Simulate(step, motors, movers);
                    if (motor.GroundingStatus.IsStableOnGround) supports.Add(motor.GroundingStatus.GroundCollider);
                }
                Assert.That(HorizontalDelta().sqrMagnitude, Is.LessThanOrEqualTo(.017f), target.ToString());
                Assert.That(motor.GroundingStatus.IsStableOnGround, Is.True, target.ToString());
            }
        }

        [UnityTearDown]
        public IEnumerator RestoreEditor()
        {
            if (Application.isPlaying) yield return new ExitPlayMode();
            if (!P06TestSceneScope.HasCaptured(Previous)) yield break;
            string previousStart = SessionState.GetString(PreviousStart, "");
            EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previousStart) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(previousStart);
            P06TestSceneScope.Restore(Previous);
            SessionState.EraseString(PreviousStart);
            string window = SessionState.GetString(PreviousWindow, "");
            SessionState.EraseString(PreviousWindow);
            if (!string.IsNullOrEmpty(window)) EditorWindow.GetWindow(Type.GetType(window, true));
        }
    }
}

