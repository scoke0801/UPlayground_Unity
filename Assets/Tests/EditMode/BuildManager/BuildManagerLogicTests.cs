using System;
using System.IO;
using NUnit.Framework;
using UPlayGround.Tool.Editor.Build;

namespace UPlayGround.BuildManager.Tests
{
    public sealed class BuildManagerLogicTests
    {
        private static readonly string[] s_scenes = { "Assets/A.unity", "Assets/B.unity" };

        private static BuildManifest Manifest()
        {
            return new BuildManifest
            {
                unityVersion = "6000.3.21f1", buildTarget = "StandaloneWindows64",
                scriptsHash = "H1", scenes = s_scenes,
                playerSettingsHash = "P1", contentSourceHash = "C1", catalogHash = "CAT",
            };
        }

        private static BuildPreflightInput Input(BuildManagerMode mode)
        {
            return new BuildPreflightInput
            {
                Mode = mode, ExpectedUnityVersion = "6000.3.21f1", ExpectedBuildTarget = "StandaloneWindows64",
                CurrentScriptsHash = "H1", Scenes = s_scenes, PlayerManifest = Manifest(),
                PlayerAaFolderExists = true, CatalogExists = true,
                CurrentPlayerSettingsHash = "P1", CurrentContentSourceHash = "C1",
                PlayerCatalogHash = "CAT", CachedCatalogHash = "CAT", CachedContentManifest = Manifest(),
            };
        }

        [Test]
        public void Assessment_SeparatesPlayerAndContentChanges()
        {
            var input = Input(BuildManagerMode.Full);
            Assert.IsNull(BuildPreflight.Assess(input).RecommendedMode);
            input.CurrentPlayerSettingsHash = "P2";
            Assert.AreEqual(BuildManagerMode.PlayerOnly, BuildPreflight.Assess(input).RecommendedMode);
            input.CurrentContentSourceHash = "C2";
            Assert.AreEqual(BuildManagerMode.Full, BuildPreflight.Assess(input).RecommendedMode);
            input.CurrentPlayerSettingsHash = "P1";
            Assert.AreEqual(BuildManagerMode.ContentOnly, BuildPreflight.Assess(input).RecommendedMode);
        }

        [Test]
        public void Assessment_RequiresFullForLegacyOrMissingOutput()
        {
            var input = Input(BuildManagerMode.Full);
            input.PlayerManifest.playerSettingsHash = null;
            input.PlayerManifest.contentSourceHash = null;
            Assert.AreEqual(BuildManagerMode.Full, BuildPreflight.Assess(input).RecommendedMode);
            input.PlayerManifest = null;
            Assert.AreEqual(BuildManagerMode.Full, BuildPreflight.Assess(input).RecommendedMode);
        }

        [Test]
        public void Assessment_MissingExeRequiresPlayerAndCorruptCatalogRequiresContent()
        {
            var input = Input(BuildManagerMode.Full);
            input.PlayerExecutableExists = false;
            Assert.AreEqual(BuildManagerMode.PlayerOnly, BuildPreflight.Assess(input).RecommendedMode);
            input.PlayerExecutableExists = true;
            input.PlayerCatalogHash = "OTHER";
            Assert.AreEqual(BuildManagerMode.ContentOnly, BuildPreflight.Assess(input).RecommendedMode);
        }

        [Test]
        public void Assessment_UnusableLibraryCachePreventsPlayerOnlyRecommendation()
        {
            var input = Input(BuildManagerMode.Full);
            input.CurrentPlayerSettingsHash = "P2";
            input.CachedContentManifest = null;
            Assert.AreEqual(BuildManagerMode.Full, BuildPreflight.Assess(input).RecommendedMode);
            input.Mode = BuildManagerMode.PlayerOnly;
            Assert.IsTrue(BuildPreflight.HasError(BuildPreflight.Evaluate(input)));
        }

        [Test]
        public void Assessment_ChangedExecutableNameCannotReuseAnotherPlayerRecord()
        {
            var input = Input(BuildManagerMode.ContentOnly);
            input.ExpectedExecutableName = "OtherGame";
            Assert.IsTrue(BuildPreflight.HasError(BuildPreflight.Evaluate(input)));
            Assert.IsTrue(BuildPreflight.Assess(input).PlayerNeedsBuild);
        }

        [Test]
        public void ContentOnly_RejectsPlayerSettingsChangeAndMissingExe()
        {
            var input = Input(BuildManagerMode.ContentOnly);
            input.CurrentPlayerSettingsHash = "DEV_CHANGED";
            Assert.IsTrue(BuildPreflight.HasError(BuildPreflight.Evaluate(input)));
            input.CurrentPlayerSettingsHash = "P1";
            input.PlayerExecutableExists = false;
            Assert.IsTrue(BuildPreflight.HasError(BuildPreflight.Evaluate(input)));
        }

        [Test]
        public void Preflight_RejectsEmptyDuplicateScenesAndUnsupportedTarget()
        {
            var input = Input(BuildManagerMode.Full);
            input.Scenes = new[] { "", "Assets/A.unity", "Assets/A.unity" };
            input.IsSupportedTarget = false;
            Assert.AreEqual(3, BuildPreflight.Evaluate(input).Count);
        }

        [Test]
        public void Profile_UsesExactFolderAndDevelopmentToggleAsSource()
        {
            var profile = UnityEngine.ScriptableObject.CreateInstance<BuildManagerProfileSO>();
            try
            {
                profile.outputDirectory = Path.Combine(Path.GetTempPath(), "ChosenBuild");
                Assert.AreEqual(Path.GetFullPath(profile.outputDirectory), profile.GetOutputDirectory());
                profile.buildOptions |= UnityEditor.BuildOptions.Development;
                Assert.IsFalse((profile.GetBuildOptions() & UnityEditor.BuildOptions.Development) != 0);
                profile.developmentBuild = true;
                Assert.IsTrue((profile.GetBuildOptions() & UnityEditor.BuildOptions.Development) != 0);
                profile.scenes.Add(null);
                Assert.AreEqual(string.Empty, profile.GetScenePaths()[0]);
            }
            finally { UnityEngine.Object.DestroyImmediate(profile); }
        }

        [TestCase("../Other")]
        [TestCase("Game.exe")]
        [TestCase("")]
        public void ExecutableName_RejectsInvalidNames(string name)
        {
            Assert.Throws<ArgumentException>(() => BuildManagerFiles.GetExecutablePath("Builds", name));
        }

        [Test]
        public void Fingerprint_IsStableAndTracksDevelopmentOption()
        {
            var profile = UnityEngine.ScriptableObject.CreateInstance<BuildManagerProfileSO>();
            try
            {
                string release = BuildManagerFingerprint.ComputePlayerSettings(profile);
                Assert.AreEqual(release, BuildManagerFingerprint.ComputePlayerSettings(profile));
                profile.developmentBuild = true;
                Assert.AreNotEqual(release, BuildManagerFingerprint.ComputePlayerSettings(profile));
                var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
                Assert.AreEqual(BuildManagerFingerprint.ComputeContent(settings), BuildManagerFingerprint.ComputeContent(settings));
            }
            finally { UnityEngine.Object.DestroyImmediate(profile); }
        }

        [Test]
        public void Preflight_Blocks_CompilationAndUnsavedScenes()
        {
            var input = Input(BuildManagerMode.Full);
            input.IsCompiling = true;
            input.HasUnsavedScene = true;
            Assert.AreEqual(2, BuildPreflight.Evaluate(input).Count);
        }

        [Test]
        public void ValidationEntry_RejectsContentOnlyBeforeBuilding()
        {
            Assert.Throws<ArgumentException>(() => BuildManagerRunner.RunValidation(default, BuildManagerMode.ContentOnly));
        }

        [Test]
        public void ValidationEntry_RejectsMissingSceneBeforeBuilding()
        {
            Assert.Throws<ArgumentException>(() => BuildManagerRunner.RunValidation(new UnityEditor.BuildPlayerOptions
            {
                scenes = new[] { "Assets/MissingBuildManagerTest.unity" },
                locationPathName = "Builds/Unused/Game.exe",
                target = UnityEditor.BuildTarget.StandaloneWindows64,
            }));
            Assert.IsFalse(Directory.Exists("Builds/Unused"));
        }

        [TestCase("2adabc03c19a2e946af1b9405cd10832", "OpenWorld.StonegateCitadel.Content")]
        [TestCase("33f5aff4b0dcbfe42b1c73ebd8c42142", "OpenWorld.SaltwindHarbor.Content")]
        [TestCase("ade6ea8759d8ac442901edbe4288c6c6", "OpenWorld.DroughtVillage.Content")]
        public void RegionKeys_KeepSceneGuidAndSeparateLocalBundle(string guid, string address)
        {
            var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
            var entry = settings.FindAssetEntry(guid);
            Assert.NotNull(entry);
            Assert.AreEqual(address, entry.address);
            Assert.AreEqual("Zones_InGame", entry.parentGroup.Name);
            Assert.IsTrue(entry.AssetPath.EndsWith(".unity"));
            var schema = entry.parentGroup.GetSchema<UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema>();
            Assert.AreEqual(UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema.BundlePackingMode.PackSeparately, schema.BundleMode);
            StringAssert.Contains("RuntimePath", schema.LoadPath.GetValue(settings));
            Assert.IsTrue(schema.IncludeInBuild);
            Assert.IsTrue(schema.IncludeAddressInCatalog);
        }

        [Test]
        public void FreshnessCheck_IncludesGroupSettings()
        {
            var profile = UnityEngine.ScriptableObject.CreateInstance<BuildManagerProfileSO>();
            try
            {
                var input = BuildManagerRunner.CreateInput(profile, BuildManagerMode.PlayerOnly, false);
                Assert.That(input.LatestContentSourceUtc, Is.GreaterThanOrEqualTo(File.GetLastWriteTimeUtc("Assets/AddressableAssetsData/AssetGroups/Zones_InGame.asset")));
            }
            finally { UnityEngine.Object.DestroyImmediate(profile); }
        }

        [Test]
        public void ContentOnly_StaysAllowed_WhenNothingButContentChanged()
        {
            Assert.IsFalse(BuildPreflight.HasError(BuildPreflight.Evaluate(Input(BuildManagerMode.ContentOnly))));
        }

        [Test]
        public void ContentOnly_IsBlocked_WhenScriptsChanged()
        {
            BuildPreflightInput input = Input(BuildManagerMode.ContentOnly);
            input.CurrentScriptsHash = "H2";
            Assert.IsTrue(BuildPreflight.HasError(BuildPreflight.Evaluate(input)));
        }

        [Test]
        public void ContentOnly_IsBlocked_WhenSceneListChanged()
        {
            BuildPreflightInput input = Input(BuildManagerMode.ContentOnly);
            input.Scenes = new[] { "Assets/A.unity" };
            Assert.IsTrue(BuildPreflight.HasError(BuildPreflight.Evaluate(input)));
        }

        [Test]
        public void ContentOnly_IsBlocked_WhenPlayerWasNeverBuilt()
        {
            BuildPreflightInput input = Input(BuildManagerMode.ContentOnly);
            input.PlayerManifest = null;
            Assert.IsTrue(BuildPreflight.HasError(BuildPreflight.Evaluate(input)));
        }

        [Test]
        public void PlayerOnly_IsBlocked_WhenCatalogIsMissing()
        {
            BuildPreflightInput input = Input(BuildManagerMode.PlayerOnly);
            input.CatalogExists = false;
            Assert.IsTrue(BuildPreflight.HasError(BuildPreflight.Evaluate(input)));
        }

        [Test]
        public void PlayerOnly_StaleContent_IsErrorUnlessAllowed()
        {
            BuildPreflightInput input = Input(BuildManagerMode.PlayerOnly);
            input.CatalogWriteUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            input.LatestContentSourceUtc = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
            Assert.IsTrue(BuildPreflight.HasError(BuildPreflight.Evaluate(input)));

            input.AllowStaleContent = true;
            Assert.IsFalse(BuildPreflight.HasError(BuildPreflight.Evaluate(input)));
        }

        [Test]
        public void Preflight_Blocks_PlayModeAndMissingScene()
        {
            BuildPreflightInput input = Input(BuildManagerMode.Full);
            input.IsPlayingOrChangingMode = true;
            input.SceneExists = _ => false;
            Assert.GreaterOrEqual(BuildPreflight.Evaluate(input).Count, 3);
        }

        [Test]
        public void PlayerAaDirectory_FollowsExecutableName()
        {
            string aa = BuildManagerFiles.GetPlayerAaDirectory(Path.Combine("out", "Game.exe"));
            Assert.AreEqual(Path.Combine("out", "Game_Data", "StreamingAssets", "aa"), aa);
        }

        [Test]
        public void DeployContent_KeepsOldBundles_WhenCatalogCopyFails()
        {
            string root = Path.Combine(Path.GetTempPath(), "BuildManagerTest_" + Guid.NewGuid().ToString("N"));
            string source = Path.Combine(root, "src");
            string destination = Path.Combine(root, "dst");
            try
            {
                Directory.CreateDirectory(Path.Combine(source, "Win"));
                Directory.CreateDirectory(Path.Combine(destination, "Win"));
                Directory.CreateDirectory(Path.Combine(destination, "catalog.bin"));
                File.WriteAllText(Path.Combine(source, "Win", "new.bundle"), "n");
                File.WriteAllText(Path.Combine(source, "catalog.bin"), "newcat");
                File.WriteAllText(Path.Combine(destination, "Win", "old.bundle"), "o");

                Assert.Catch(() => BuildManagerFiles.DeployContent(source, destination));
                Assert.IsTrue(File.Exists(Path.Combine(destination, "Win", "old.bundle")));
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        [Test]
        public void DeployContent_ReplacesBundlesAndRemovesOrphans()
        {
            string root = Path.Combine(Path.GetTempPath(), "BuildManagerTest_" + Guid.NewGuid().ToString("N"));
            string source = Path.Combine(root, "src");
            string destination = Path.Combine(root, "dst");
            try
            {
                Directory.CreateDirectory(Path.Combine(source, "Win"));
                Directory.CreateDirectory(Path.Combine(destination, "Win"));
                File.WriteAllText(Path.Combine(source, "Win", "new.bundle"), "n");
                File.WriteAllText(Path.Combine(source, "catalog.bin"), "newcat");
                File.WriteAllText(Path.Combine(source, "settings.json"), "newset");
                File.WriteAllText(Path.Combine(destination, "Win", "old.bundle"), "o");
                Directory.CreateDirectory(Path.Combine(destination, "RemovedGroup"));
                File.WriteAllText(Path.Combine(destination, "RemovedGroup", "orphan.bundle"), "o");
                File.WriteAllText(Path.Combine(destination, "catalog.bin"), "oldcat");

                BuildManagerFiles.DeployContent(source, destination);

                Assert.IsTrue(File.Exists(Path.Combine(destination, "Win", "new.bundle")));
                Assert.IsFalse(File.Exists(Path.Combine(destination, "Win", "old.bundle")));
                Assert.IsFalse(File.Exists(Path.Combine(destination, "RemovedGroup", "orphan.bundle")));
                Assert.AreEqual("newcat", File.ReadAllText(Path.Combine(destination, "catalog.bin")));
                Assert.AreEqual("newset", File.ReadAllText(Path.Combine(destination, "settings.json")));
            }
            finally
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, true);
            }
        }

        [Test]
        public void FilesHash_ChangesWithContent()
        {
            string file = Path.Combine(Path.GetTempPath(), "BuildManagerHash_" + Guid.NewGuid().ToString("N") + ".dll");
            try
            {
                File.WriteAllText(file, "a");
                string first = BuildManagerFiles.ComputeFilesHash(new[] { file });
                File.WriteAllText(file, "b");
                Assert.AreNotEqual(first, BuildManagerFiles.ComputeFilesHash(new[] { file }));
            }
            finally
            {
                File.Delete(file);
            }
        }
    }
}
