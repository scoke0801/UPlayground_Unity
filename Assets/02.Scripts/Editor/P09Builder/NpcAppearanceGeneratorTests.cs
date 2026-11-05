#if UNITY_INCLUDE_TESTS
using System;
using System.Linq;
using NUnit.Framework;
using P09.Modular.Humanoid.Data;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Editor.P09Builder.Tests
{
    /// <summary>조합 계약과 저장·실패 경계의 GUID 및 내용 보존을 검증한다.</summary>
    public sealed class NpcAppearanceGeneratorTests
    {
        private const string TestRoot = "Assets/__NpcAppearanceTests";
        private const string Output = TestRoot + "/Presets";
        private NpcAppearanceSetSO source;

        [TestCase(BuilderSex.Male)]
        [TestCase(BuilderSex.Female)]
        public void 무기없는_NPC는_기존_성별_NPC_모션을_연결한다(BuilderSex sex)
        {
            var root = new GameObject("NPC 모션 연결 테스트");
            try
            {
                var animator = root.AddComponent<UPlayGround.Animation.ActorAnimator>();
                var config = new CharacterBuildConfig { ActorKind = BuilderActorKind.Npc, Sex = sex };
                new ApplyWeaponStep().Execute(new BuildContext(config) { RootInstance = root });
                Assert.That(animator.MotionSet, Is.SameAs(ApplyWeaponStep.LoadNpcMotionSet(sex)));
                foreach (var slot in NpcAppearanceImprovement.Slots)
                    Assert.That(animator.HasMotion(slot), Is.True, slot.ToString());
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void 외부_NPC는_명시한_모션_소스를_보존한다()
        {
            var root = new GameObject("외부 NPC 모션 테스트");
            var motion = ScriptableObject.CreateInstance<UPlayGround.Data.Actor.Animation.ActorAnimationMotionSet>();
            try
            {
                var animator = root.AddComponent<UPlayGround.Animation.ActorAnimator>();
                var config = new CharacterBuildConfig { ActorKind = BuilderActorKind.Npc, ExternalModelPrefab = root, ExternalMotionSet = motion };
                new ApplyWeaponStep().Execute(new BuildContext(config) { RootInstance = root });
                Assert.That(animator.MotionSet, Is.SameAs(motion));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(motion); }
        }

        [SetUp]
        public void SetUp()
        {
            source = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<NpcAppearanceSetSO>(NpcAppearanceGenerator.RulesPath));
            Assert.That(source, Is.Not.Null);
            Assert.That(AssetDatabase.IsValidFolder(TestRoot), Is.False, "다른 테스트의 임시 자산을 삭제하지 않습니다.");
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.StartAssetEditing();
            try { AssetDatabase.DeleteAsset(TestRoot); }
            finally { AssetDatabase.StopAssetEditing(); }
            UnityEngine.Object.DestroyImmediate(source);
        }

        [Test]
        public void 조합은_288개이며_각_세트와_성별마다_12개이고_중복이_없다()
        {
            var configs = NpcAppearanceGenerator.CreateConfigs(source);
            Assert.That(configs.Count, Is.EqualTo(288));
            var groups = configs.GroupBy(c => ((IEditPartData)c.ArmorSelections.Get(BuilderArmorSlot.Arm)).ContentId + "/" + c.Sex).ToArray();
            Assert.That(groups.Length, Is.EqualTo(24));
            Assert.That(groups.All(g => g.Count() == 12), Is.True);
            Assert.That(configs.Select(NpcAppearanceGenerator.GetAppearanceSignature).Distinct().Count(), Is.EqualTo(288));
            foreach (var config in configs)
            {
                Assert.That(config.Validate(), Is.Empty);
                Assert.That(config.ActorKind, Is.EqualTo(BuilderActorKind.Npc));
                Assert.That(config.ArmorSelections.Get(BuilderArmorSlot.Head), Is.Null);
                var ids = new[] { BuilderArmorSlot.Arm, BuilderArmorSlot.Chest, BuilderArmorSlot.Waist, BuilderArmorSlot.Leg }
                    .Select(slot => ((IEditPartData)config.ArmorSelections.Get(slot)).ContentId).Distinct().ToArray();
                Assert.That(ids.Length, Is.EqualTo(1));
                Assert.That(ids[0], Is.InRange(1, 12));
                Assert.That(config.UseWeaponGroup, Is.False);
                Assert.That(config.WeaponGroupSo, Is.Null);
                if (config.Sex == BuilderSex.Male) Assert.That(config.BustSizeSo, Is.Null);
                else
                {
                    Assert.That(config.FacialHairSo, Is.Null);
                    Assert.That(config.FacialHairId, Is.Zero);
                }
            }
        }

        [Test]
        public void 저장된_프리셋이_288개이며_재실행은_GUID와_내용을_보존한다()
        {
            var existing = AssetDatabase.FindAssets("t:BuildPreset", new[] { NpcAppearanceGenerator.PresetFolder });
            Assert.That(existing.Length, Is.EqualTo(288));
            var generated = NpcAppearanceGenerator.Generate(source, Output, null);
            var paths = generated.Select(AssetDatabase.GetAssetPath).ToArray();
            var guids = paths.Select(AssetDatabase.AssetPathToGUID).ToArray();
            var json = generated.Select(EditorJsonUtility.ToJson).ToArray();
            Assert.That(generated.All(p => !EditorUtility.IsDirty(p)), Is.True, "생성 결과의 저장이 끝나야 합니다.");
            Assert.That(generated.All(p => AssetDatabase.GetLabels(p).Contains("P09.GeneratedNpcAppearance")), Is.True);
            NpcAppearanceGenerator.Generate(source, Output, null);
            Assert.That(AssetDatabase.FindAssets("t:BuildPreset", new[] { Output }).Length, Is.EqualTo(288));
            Assert.That(paths.Select(AssetDatabase.AssetPathToGUID), Is.EqualTo(guids));
            Assert.That(paths.Select(p => EditorJsonUtility.ToJson(AssetDatabase.LoadAssetAtPath<BuildPreset>(p))), Is.EqualTo(json));
            var expected = NpcAppearanceGenerator.CreateConfigs(source).Select(JsonUtility.ToJson).ToArray();
            var actual = existing.Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p, StringComparer.Ordinal)
                .Select(p => JsonUtility.ToJson(AssetDatabase.LoadAssetAtPath<BuildPreset>(p).config)).ToArray();
            Assert.That(actual.OrderBy(s => s), Is.EqualTo(expected.OrderBy(s => s)));
        }

        [TestCase(1)]
        [TestCase(145)]
        [TestCase(288)]
        public void 신규_생성_중간에_실패하면_이번_폴더와_에셋이_남지_않는다(int failAt)
        {
            Assert.Throws<ApplicationException>(() => NpcAppearanceGenerator.Generate(source, Output,
                count => { if (count == failAt) throw new ApplicationException("실패 주입"); }));
            Assert.That(AssetDatabase.IsValidFolder(TestRoot), Is.False);
        }

        [Test]
        public void 갱신_중_실패하면_기존_GUID와_내용을_복원한다()
        {
            var assets = NpcAppearanceGenerator.Generate(source, Output, null);
            var paths = assets.Select(AssetDatabase.GetAssetPath).ToArray();
            var guids = paths.Select(AssetDatabase.AssetPathToGUID).ToArray();
            var before = assets.Select(EditorJsonUtility.ToJson).ToArray();
            source.description = "실패 시 남아서는 안 되는 변경";
            Assert.Throws<ApplicationException>(() => NpcAppearanceGenerator.Generate(source, Output,
                count => { if (count == 145) throw new ApplicationException("실패 주입"); }));
            Assert.That(paths.Select(AssetDatabase.AssetPathToGUID), Is.EqualTo(guids));
            AssetDatabase.StartAssetEditing();
            try { foreach (string path in paths) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate); }
            finally { AssetDatabase.StopAssetEditing(); }
            Assert.That(paths.Select(p => EditorJsonUtility.ToJson(AssetDatabase.LoadAssetAtPath<BuildPreset>(p))), Is.EqualTo(before));
        }

        [Test]
        public void 유효성_실패는_출력에_아무것도_쓰지_않는다()
        {
            source.npcStats.npcHp = -1;
            var exception = Assert.Throws<InvalidOperationException>(() => NpcAppearanceGenerator.Generate(source, Output, null));
            Assert.That(exception.Message.Split('\n').Length, Is.EqualTo(288));
            Assert.That(AssetDatabase.IsValidFolder(TestRoot), Is.False);
        }

        [Test]
        public void 다른_자산과_저장되지_않은_변경은_덮어쓰지_않는다()
        {
            PathConfig.EnsureFolderExists(Output);
            string path = Output + "/NPC_Gen_S01_M_01_BuildPreset.asset";
            var foreign = ScriptableObject.CreateInstance<BuildPreset>();
            foreign.description = "다른 도구의 자산";
            AssetDatabase.CreateAsset(foreign, path);
            AssetDatabase.SaveAssetIfDirty(foreign);
            string guid = AssetDatabase.AssetPathToGUID(path);
            Assert.Throws<InvalidOperationException>(() => NpcAppearanceGenerator.Generate(source, Output, null));
            Assert.That(foreign.description, Is.EqualTo("다른 도구의 자산"));
            Assert.That(AssetDatabase.AssetPathToGUID(path), Is.EqualTo(guid));
            AssetDatabase.DeleteAsset(path);
            var generated = NpcAppearanceGenerator.Generate(source, Output, null);
            generated[0].description = "저장하지 않은 사용자 편집";
            EditorUtility.SetDirty(generated[0]);
            Assert.Throws<InvalidOperationException>(() => NpcAppearanceGenerator.Generate(source, Output, null));
            Assert.That(generated[0].description, Is.EqualTo("저장하지 않은 사용자 편집"));
        }

        [Test]
        public void 중복_헤어와_대머리와_다른_성별의_피부는_거부한다()
        {
            source.rules[0].hairStyles[1] = source.rules[0].hairStyles[0];
            Assert.Throws<InvalidOperationException>(() => NpcAppearanceGenerator.CreateConfigs(source));
            var catalog = new P09AssetCatalog(); catalog.Refresh();
            source.rules[0].hairStyles[1] = (RendererEditPartData)catalog.HairStyles.Single(s => ((IEditPartData)s).ContentId == 14);
            Assert.Throws<InvalidOperationException>(() => NpcAppearanceGenerator.CreateConfigs(source));
            source.maleFaces[0].skin = source.femaleFaces[0].skin;
            Assert.Throws<InvalidOperationException>(() => NpcAppearanceGenerator.CreateConfigs(source));
        }

        [TestCase(BuilderSex.Male)]
        [TestCase(BuilderSex.Female)]
        public void 피부색은_동일_ID의_다른_성별_대신_선택한_SO를_적용한다(BuilderSex sex)
        {
            var root = new GameObject("피부 테스트");
            var child = GameObject.CreatePrimitive(PrimitiveType.Cube);
            child.transform.SetParent(root.transform);
            var renderer = child.GetComponent<Renderer>();
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "P09_Test_Skin" };
            renderer.sharedMaterial = material;
            try
            {
                var catalog = new P09AssetCatalog(); catalog.Refresh();
                var skin = sex == BuilderSex.Male ? source.maleFaces[0].skin : source.femaleFaces[0].skin;
                var config = new CharacterBuildConfig { ActorKind = BuilderActorKind.Npc, Sex = sex, SkinColorSo = skin };
                AppearanceApplier.Apply(root, config, catalog);
                Assert.That(renderer.sharedMaterial, Is.SameAs(skin.Material));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(material); }
        }
    }
}
#endif
