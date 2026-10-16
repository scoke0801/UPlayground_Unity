using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Content.Tests
{
    /// <summary>공유 모델의 클립 오염 방지와 성장 효과 등록을 검사한다.</summary>
    public sealed class SharedCombatEffectsTests
    {
        [TestCase("Raon")]
        [TestCase("Hwarin")]
        public void 공유_모델은_인스턴스_클립_이벤트를_생성하지_않는다(string character)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03.Prefabs/Actor/Player/Models/PlayerModel_" + character + ".prefab");
            Assert.That(model, Is.Not.Null);
            int count = 0;
            foreach (var component in model.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null || component.GetType().FullName != "INab.Common.WeaponTrailEffect") continue;
                count++;
                var settings = new SerializedObject(component);
                Assert.That(settings.FindProperty("trailUsageType").enumValueIndex, Is.Zero);
                Assert.That(settings.FindProperty("lineTipTransform").objectReferenceValue, Is.Not.Null);
                Assert.That(settings.FindProperty("lineBottomTransform").objectReferenceValue, Is.Not.Null);
            }
            Assert.That(count, Is.GreaterThan(0));
        }

        [Test]
        public void 레벨업은_회복과_구분되는_단발_효과를_갖는다()
        {
            var database = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/10.Datas/Path/FXPrefabDatabase.asset");
            var settings = new SerializedObject(database);
            var entries = settings.FindProperty("prefabs");
            Assert.That(entries, Is.Not.Null);
            GameObject levelUp = null;
            GameObject heal = null;
            for (int i = 0; i < entries.arraySize; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                string key = entry.FindPropertyRelative("key").stringValue;
                var prefab = entry.FindPropertyRelative("prefab").objectReferenceValue as GameObject;
                if (key == "LevelUp") levelUp = prefab;
                if (key == "PlayerHeal") heal = prefab;
            }
            Assert.That(levelUp, Is.Not.Null);
            Assert.That(levelUp, Is.Not.EqualTo(heal));
            var particles = levelUp.GetComponentsInChildren<ParticleSystem>(true);
            Assert.That(particles.Length, Is.GreaterThan(0));
            foreach (var particle in particles) Assert.That(particle.main.loop, Is.False, particle.name);
        }
    }
}
