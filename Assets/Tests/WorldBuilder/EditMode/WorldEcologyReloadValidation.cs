using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.WorldBuilder.Data;
using UPlayGround.WorldBuilder.Editor;

namespace UPlayGround.WorldBuilder.Tests
{
    /// <summary>실제 스크립트 리로드 전후를 같은 임시 월드에서 대조한다.</summary>
    public static class WorldEcologyReloadValidation
    {
        private const string ScopeKey = "WorldBuilder.P06.SculptReload.SceneSetup";
        private static readonly string DomainId = Guid.NewGuid().ToString("N");

        [Serializable]
        private sealed class Receipt
        {
            public string world, previousScene, domain, session, revision, native;
        }

        public static string Prepare(string receiptPath)
        {
            P06TestSceneScope.Capture(ScopeKey);
            string id = Guid.NewGuid().ToString("N");
            string previousScene = SceneManager.GetActiveScene().path;
            try
            {
                var definition = WorldWaterValidationFixture.Create(id, WorldPath(id), DataPath(id), false, NewSceneMode.Single);
                var ecology = WorldEcologyValidationFixture.CreateInput(definition, DataPath(id), false);
                WorldEcologyAuthoring.Regenerate(definition, ecology);
                Assert.That(EditorSceneManager.SaveScene(SceneManager.GetActiveScene()), Is.True);
                using var session = new WorldTerrainAuthoringSession(definition, WorldBuilderAuthoringAssets.FindTerrains(definition));
                session.BeginSculpt();
                var tile = session.Terrain.Terrains[0, 0].terrainData;
                float value = tile.GetHeights(8, 8, 1, 1)[0, 0];
                tile.SetHeights(8, 8, new[,] { { value + .00025f } });
                EditorUtility.SetDirty(tile);
                AssetDatabase.SaveAssetIfDirty(tile);
                var receipt = new Receipt {
                    world = id, previousScene = previousScene, domain = DomainId, session = definition.ActiveSession.sessionGuid,
                    revision = definition.SourceRevision, native = session.Terrain.Read().ComputeHash()
                };
                string json = JsonUtility.ToJson(receipt, true);
                File.WriteAllText(receiptPath, json);
                return json;
            }
            catch
            {
                Cleanup(id, previousScene);
                throw;
            }
        }

        public static string VerifyAndCleanup(string receiptPath)
        {
            var expected = JsonUtility.FromJson<Receipt>(File.ReadAllText(receiptPath));
            Assert.That(Guid.TryParseExact(expected.world, "N", out _), Is.True);
            try
            {
                Assert.That(Guid.TryParseExact(expected.domain, "N", out _), Is.True);
                Assert.That(DomainId, Is.Not.EqualTo(expected.domain));
                Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(WorldPath(expected.world) + "/P05Validation.unity"));
                var definition = AssetDatabase.LoadAssetAtPath<WorldDefinition>(DataPath(expected.world) + "/Definition.asset");
                Assert.That(definition.Ecology, Is.Not.Null);
                Assert.That(definition.ActiveSession?.sessionGuid, Is.EqualTo(expected.session));
                Assert.That(definition.SourceRevision, Is.EqualTo(expected.revision));
                using var authoring = new WorldTerrainAuthoringSession(definition, WorldBuilderAuthoringAssets.FindTerrains(definition));
                Assert.That(authoring.Terrain.Read().ComputeHash(), Is.EqualTo(expected.native));
                Assert.That(definition.AppliedTerrainHash, Is.Not.EqualTo(expected.native));
                int operations = definition.Operations.Count;
                authoring.CaptureSculpt();
                Assert.That(definition.ActiveSession, Is.Null);
                Assert.That(definition.Operations.Count, Is.EqualTo(operations + 1));
                Assert.That(authoring.Terrain.Read().ComputeHash(), Is.EqualTo(expected.native));
                Assert.That(definition.SourceRevision, Is.EqualTo(expected.revision));
                return $"도메인 {expected.domain} → {DomainId}, Sculpt {expected.session}, 연산 {operations} → {operations + 1}, native SHA {expected.native}";
            }
            finally { Cleanup(expected.world, expected.previousScene); }
        }

        private static void Cleanup(string id, string previousScene)
        {
            if (P06TestSceneScope.HasCaptured(ScopeKey))
                P06TestSceneScope.Restore(ScopeKey);
            else if (!string.IsNullOrEmpty(previousScene) && AssetDatabase.LoadAssetAtPath<SceneAsset>(previousScene) != null)
                EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);
            else
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(WorldPath(id));
            AssetDatabase.DeleteAsset(DataPath(id));
        }

        private static string WorldPath(string id) => "Assets/WorldBuilder/Worlds/" + id;
        private static string DataPath(string id) => "Assets/10.Datas/WorldBuilder/Test_" + id;
    }
}
