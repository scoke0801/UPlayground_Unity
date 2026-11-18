using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using UPlayGround.WorldBuilder.Editor;
using UPlayGround.WorldBuilder.Runtime;

namespace UPlayGround.WorldBuilder.Tests
{
    public sealed class WorldPlanningTraversalTests
    {
        [UnityTest] public IEnumerator T13_AgentTraversesTwoEntrancesAndBridgeWithReadinessGate() => Traverse(false);
        [UnityTest] public IEnumerator T13_512mValidationSectorTraversesActualSavedBridge() => Traverse(true);
        private IEnumerator Traverse(bool useFullFixture)
        {
            SessionState.SetString("WB_P04_TraversalFolder", ""); SessionState.SetString("WB_P04_TraversalData", "");
            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>()) if (window.GetType().FullName == "UPlayGround.Tool.Editor.Build.BuildManagerWindow")
            { SessionState.SetString("WB_P04_BuildWindow", window.GetType().AssemblyQualifiedName); window.Close(); }
            if (useFullFixture)
                EditorSceneManager.OpenScene(WorldPlanningValidationFixture.ScenePath, OpenSceneMode.Single);
            else
            {
                string id = Guid.NewGuid().ToString("N"); string folder = "Assets/WorldBuilder/Worlds/" + id, data = "Assets/10.Datas/WorldBuilder/Test_" + id;
                SessionState.SetString("WB_P04_TraversalFolder", folder); SessionState.SetString("WB_P04_TraversalData", data);
                var definition = WorldPlanningValidationFixture.Create(id, folder, data, false, NewSceneMode.Single);
                WorldPlanningValidationFixture.BuildNavigation(definition); Assert.That(EditorSceneManager.SaveScene(SceneManager.GetActiveScene()));
            }
            string scenePath = SceneManager.GetActiveScene().path; SessionState.SetString("WB_P04_ExpectedPlayScene", scenePath);
            Assert.That(UnityEngine.Object.FindFirstObjectByType<WorldBridgeAccessGate>(), Is.Not.Null, "Play 진입 전 교량 누락");
            SessionState.SetString("WB_P04_PreviousPlayStart", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene)); SessionState.SetBool("WB_P04_PlayStartSaved", true);
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            bool expectReload = !EditorSettings.enterPlayModeOptionsEnabled || (EditorSettings.enterPlayModeOptions & EnterPlayModeOptions.DisableDomainReload) == 0;
            yield return new EnterPlayMode(expectReload);
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(SessionState.GetString("WB_P04_ExpectedPlayScene", "")), "실행 씬이 검증 대상과 다릅니다.");
            var preview = UnityEngine.Object.FindFirstObjectByType<WorldPlanningNavigationPreview>(); var gate = UnityEngine.Object.FindFirstObjectByType<WorldBridgeAccessGate>();
            Assert.That(preview, Is.Not.Null); Assert.That(gate, Is.Not.Null, "Play 진입 후 교량 누락");
            Assert.That(gate.CanEnter, Is.False); gate.SetEndpointReady(0, true); Assert.That(gate.CanEnter, Is.False);
            foreach (var barrier in gate.EntryBarriers) Assert.That(barrier.enabled);
            gate.SetEndpointReady(1, true); Assert.That(gate.CanEnter); foreach (var barrier in gate.EntryBarriers) Assert.That(barrier.enabled, Is.False);
            yield return new WaitForSeconds(.3f);
            Assert.That(NavMesh.SamplePosition(preview.entranceA, out var start, 2, NavMesh.AllAreas)); Assert.That(NavMesh.SamplePosition(preview.entranceB, out var goal, 2, NavMesh.AllAreas));
            var actor = new GameObject("P04 실제 NavMeshAgent 검증"); actor.transform.position = start.position; var agent = actor.AddComponent<NavMeshAgent>();
            agent.radius = .4f; agent.height = 1.8f; agent.speed = 30; agent.acceleration = 100; agent.angularSpeed = 720; agent.stoppingDistance = .15f;
            Assert.That(agent.Warp(start.position)); Assert.That(agent.SetDestination(goal.position)); bool crossed = false; float until = Time.realtimeSinceStartup + 25;
            Vector3 previous = agent.transform.position; bool full = preview.entranceA.x > 128; float bridgeX = full ? 512 : 64, bridgeZ = full ? 250 : 64;
            while (Time.realtimeSinceStartup < until)
            {
                yield return null; Vector3 current = agent.transform.position;
                if ((previous.x - bridgeX) * (current.x - bridgeX) <= 0 && Mathf.Abs(current.x - previous.x) > .00001f)
                {
                    var crossing = Vector3.Lerp(previous, current, (bridgeX - previous.x) / (current.x - previous.x));
                    if (Mathf.Abs(crossing.z - bridgeZ) < 3 && crossing.y > 11.8f)
                    {
                        Assert.That(Physics.Raycast(crossing + Vector3.up, Vector3.down, out var hit, 2)); Assert.That(hit.point.y, Is.EqualTo(12).Within(.1)); crossed = true;
                    }
                }
                previous = current; if (!agent.pathPending && Vector3.Distance(current, goal.position) < .5f) break;
            }
            Assert.That(agent.pathStatus, Is.EqualTo(NavMeshPathStatus.PathComplete)); Assert.That(Vector3.Distance(agent.transform.position, goal.position), Is.LessThan(.5f)); Assert.That(crossed, "실제 교량 상판을 횡단해야 합니다.");
            gate.enabled = false; Assert.That(gate.CanEnter, Is.False); foreach (var barrier in gate.EntryBarriers) Assert.That(barrier.enabled);
            gate.enabled = true; Assert.That(gate.CanEnter, Is.False);
            Debug.Log("[WorldBuilder P04 T13] 구역=" + (full ? "512m" : "128m") + " 교량 Collider 지지 횡단=true 도착오차=" + Vector3.Distance(agent.transform.position, goal.position));
            yield return new ExitPlayMode();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (Application.isPlaying) yield return new ExitPlayMode();
            if (SessionState.GetBool("WB_P04_PlayStartSaved", false))
            {
                string previous = SessionState.GetString("WB_P04_PreviousPlayStart", ""); EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previous) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
                SessionState.EraseBool("WB_P04_PlayStartSaved"); SessionState.EraseString("WB_P04_PreviousPlayStart");
            }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            string folder = SessionState.GetString("WB_P04_TraversalFolder", ""), data = SessionState.GetString("WB_P04_TraversalData", "");
            if (folder.StartsWith("Assets/WorldBuilder/Worlds/")) AssetDatabase.DeleteAsset(folder);
            if (data.StartsWith("Assets/10.Datas/WorldBuilder/Test_")) AssetDatabase.DeleteAsset(data);
            SessionState.EraseString("WB_P04_TraversalFolder"); SessionState.EraseString("WB_P04_TraversalData"); SessionState.EraseString("WB_P04_ExpectedPlayScene");
            string windowType = SessionState.GetString("WB_P04_BuildWindow", ""); SessionState.EraseString("WB_P04_BuildWindow"); if (!string.IsNullOrEmpty(windowType)) EditorWindow.GetWindow(Type.GetType(windowType));
        }
    }
}
