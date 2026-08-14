#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Actor;

namespace UPlayGround.Editor.P09Builder.Tests
{
    public sealed class PathConfigTransactionTests
    {
        private const string TestFolder = "Assets/__P09BuilderTransactionTests";
        private const string TestAssetPath = TestFolder + "/Stable.asset";

        [SetUp]
        public void SetUp()
        {
            AssetDatabase.DeleteAsset(TestFolder);
            PathConfig.EnsureFolderExists(TestFolder);
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TestFolder);
            AssetDatabase.Refresh();
        }

        [Test]
        public void 기존_에셋_갱신_중_실패하면_GUID와_내용을_복구한다()
        {
            var original = ScriptableObject.CreateInstance<NpcActorSO>();
            original.actorName = "before";
            AssetDatabase.CreateAsset(original, TestAssetPath);
            AssetDatabase.SaveAssets();
            string originalGuid = AssetDatabase.AssetPathToGUID(TestAssetPath);

            var context = new BuildContext(null);
            var undo = new UndoGroupScope("P09 Builder Transaction Test");
            try
            {
                var replacement = ScriptableObject.CreateInstance<NpcActorSO>();
                replacement.actorName = "after";
                NpcActorSO updated = PathConfig.CreateOrUpdateAsset(
                    replacement,
                    TestFolder,
                    "Stable",
                    out string updatedPath,
                    out bool created,
                    context);

                Assert.That(created, Is.False);
                Assert.That(updatedPath, Is.EqualTo(TestAssetPath));
                Assert.That(updated, Is.SameAs(original));
                Assert.That(updated.actorName, Is.EqualTo("after"));

                // 실제 파이프라인 catch와 같은 순서로 중간 단계 실패를 복구한다.
                undo.Revert();
                context.RestoreStagedAssetBackups();
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(TestAssetPath, ImportAssetOptions.ForceUpdate);

                NpcActorSO restored = AssetDatabase.LoadAssetAtPath<NpcActorSO>(TestAssetPath);
                Assert.That(AssetDatabase.AssetPathToGUID(TestAssetPath), Is.EqualTo(originalGuid));
                Assert.That(restored, Is.Not.Null);
                Assert.That(restored.actorName, Is.EqualTo("before"));
            }
            finally
            {
                context.DiscardStagedAssetBackups();
                undo.Collapse();
                undo.Dispose();
            }
        }
    }

}
#endif
