using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Pet;
using UPlayGround.Data.Ability;

namespace UPlayGround.Ability.Tests
{
    public sealed class PetV2AssetTests
    {
        [Test]
        public void 앉기_자세의_중앙_접촉면이_날개_보강대에_닿는다()
        {
            var definition = AssetDatabase.LoadAssetAtPath<PetDefinitionSO>("Assets/10.Datas/Pet/Monkey.asset");
            var glider = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Traversal/PlayerGlider.prefab");
            var seat = glider.GetComponent<global::UPlayGround.Components.PlayerGliderView>().PetSeatAnchor;
            var model = UnityEngine.Object.Instantiate(definition.previewPrefab);
            var mesh = new Mesh();
            try
            {
                var clip = definition.seatedMotion.motionSet.motions[0].motionClip;
                clip.SampleAnimation(model, clip.length);
                model.transform.localScale = Vector3.one * .25f;
                var renderer = model.GetComponentInChildren<SkinnedMeshRenderer>();
                renderer.BakeMesh(mesh);
                float bodyBottom = float.PositiveInfinity;
                foreach (var vertex in mesh.vertices)
                {
                    Vector3 point = renderer.transform.TransformPoint(vertex) - model.transform.position;
                    if (Mathf.Abs(point.x) < .06f && Mathf.Abs(point.z) < .06f)
                        bodyBottom = Mathf.Min(bodyBottom, point.y);
                }
                float surface = float.NegativeInfinity;
                foreach (var filter in seat.parent.GetComponentsInChildren<MeshFilter>())
                    foreach (var vertex in filter.sharedMesh.vertices)
                    {
                        Vector3 point = seat.parent.InverseTransformPoint(filter.transform.TransformPoint(vertex));
                        if (Mathf.Abs(point.x - seat.localPosition.x) < .03f && Mathf.Abs(point.z - seat.localPosition.z) < .03f)
                            surface = Mathf.Max(surface, point.y);
                    }
                Assert.That(float.IsInfinity(bodyBottom) || float.IsInfinity(surface), Is.False);
                Assert.That(seat.localPosition.y + bodyBottom - surface, Is.InRange(-.01f, .02f));
            }
            finally { UnityEngine.Object.DestroyImmediate(mesh); UnityEngine.Object.DestroyImmediate(model); }
        }

        [Test]
        public void 도착_FX는_등록된_비반복_효과이며_펫_크기로_축소한다()
        {
            var definition = AssetDatabase.LoadAssetAtPath<PetDefinitionSO>("Assets/10.Datas/Pet/Monkey.asset");
            var database = AssetDatabase.LoadAssetAtPath<global::UPlayGround.Data.Path.FXPrefabDatabase>("Assets/10.Datas/Path/FXPrefabDatabase.asset");
            database.Initialize();
            var prefab = database.GetPrefab(definition.arrivalFxKey);
            Assert.That(prefab, Is.Not.Null);
            Assert.That(definition.arrivalFxScale, Is.InRange(.01f, .3f));
            Assert.That(definition.arrivalFxDuration, Is.GreaterThan(0f));
            foreach (var particle in prefab.GetComponentsInChildren<ParticleSystem>(true))
            {
                Assert.That(particle.main.loop, Is.False);
                Assert.That(particle.main.scalingMode, Is.EqualTo(ParticleSystemScalingMode.Hierarchy));
            }
        }

        [Test]
        public void 메뉴_중단은_펫을_보인_채_대기시키고_숨김_중단은_숨긴다()
        {
            var petObject = new GameObject("PetVisibility");
            var playerObject = new GameObject("PetVisibilityOwner");
            var definition = ScriptableObject.CreateInstance<PetDefinitionSO>();
            var profile = ScriptableObject.CreateInstance<PetFollowProfileSO>();
            try
            {
                var visual = new GameObject("Visual"); visual.transform.SetParent(petObject.transform, false);
                var presentation = petObject.AddComponent<global::UPlayGround.Components.Pet.PetPresentation>();
                var serialized = new SerializedObject(presentation);
                serialized.FindProperty("_visualRoot").objectReferenceValue = visual;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var movement = petObject.AddComponent<global::UPlayGround.MovementController.PetMovementController>();
                var actor = petObject.AddComponent<PetActor>();
                var player = playerObject.AddComponent<PlayerActor>();
                definition.followProfile = profile;
                var brain = new global::UPlayGround.Components.Pet.PetBrain(actor, player, movement, definition, null, presentation);
                presentation.SetVisible(true);
                brain.SetSuspended(true, hide: false);
                Assert.That(visual.activeSelf, Is.True);
                brain.SetSuspended(true, hide: true);
                Assert.That(visual.activeSelf, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(petObject);
                UnityEngine.Object.DestroyImmediate(playerObject);
                UnityEngine.Object.DestroyImmediate(definition);
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }
        [Test]
        public void 모든_스킬_레벨의_회수범위는_일반복귀거리보다_작다()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PetCatalogSO>("Assets/10.Datas/Pet/PetCatalog.asset");
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.skills.skills.Count, Is.EqualTo(5));
            foreach (var model in catalog.definitions)
                foreach (var level in catalog.skills.Find(PetSkillCatalogSO.PickupRange).levels)
                    Assert.That(level.explorationValue + catalog.skills.lootLeashMargin, Is.LessThan(model.followProfile.returnDistance));
            foreach (var skill in catalog.skills.skills)
            {
                Assert.That(skill.levels.Count, Is.EqualTo(5));
                if (skill.supportAbility == null) continue;
                foreach (var level in skill.levels)
                {
                    Assert.That(level.ability.cooldown.cooldownGroupId, Is.EqualTo(skill.supportAbility.cooldown.cooldownGroupId));
                    Assert.That(level.ability.cooldown.cooldownGroupId, Is.Not.EqualTo("Pet.Support"));
                    Assert.That(level.ability.cooldown.durationSeconds, Is.EqualTo(skill.triggerPolicy.internalCooldown));
                    Assert.That(skill.triggerPolicy.requiresCanSupport, Is.True);
                    Assert.That(level.ability.cooldown.scope, Is.EqualTo(AbilityCooldownScope.PlayerActor));
                    Assert.That(level.Effect.maxStackCount, Is.EqualTo(1));
                }
            }
        }
        [Test]
        public void 미리보기에는_게임액터나_서비스실행_컴포넌트가_없다()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PetCatalogSO>("Assets/10.Datas/Pet/PetCatalog.asset");
            foreach (var model in catalog.definitions)
            {
                Assert.That(model.previewPrefab, Is.Not.Null);
                Assert.That(model.previewIdle, Is.Not.Null);
                Assert.That(model.seatedMotion.motionSet.IsValid(), Is.True);
                foreach (var component in model.previewPrefab.GetComponentsInChildren<MonoBehaviour>(true))
                    Assert.That(component, Is.TypeOf<Animancer.AnimancerComponent>());
            }
        }
        [Test]
        public void 프레임은_슬라이스_경계를_가지며_모든_아이콘은_투명_Sprite다()
        {
            var skills = AssetDatabase.LoadAssetAtPath<PetSkillCatalogSO>("Assets/10.Datas/Pet/PetSkills.asset");
            Assert.That(skills.menuFrame.border.sqrMagnitude, Is.GreaterThan(0f));
            Assert.That(skills.buttonPlate.border.sqrMagnitude, Is.GreaterThan(0f));
            foreach (var skill in skills.skills)
            {
                Assert.That(skill.icon, Is.Not.Null);
                var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(skill.icon));
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
                Assert.That(importer.alphaIsTransparency, Is.True);
                Assert.That(importer.maxTextureSize, Is.LessThanOrEqualTo(256));
            }
        }
    }
}
