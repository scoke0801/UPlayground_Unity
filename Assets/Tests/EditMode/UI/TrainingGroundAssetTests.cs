using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.UI;

namespace UPlayGround.UI.Tests
{
    /// <summary>훈련장 선택 목록과 프리팹 배선이 실제 임포트된 에셋에서 유효한지 확인한다.</summary>
    public sealed class TrainingGroundAssetTests
    {
        private const string ConfigPath = "Assets/10.Datas/UI/TrainingGroundConfig.asset";

        [Test]
        public void 씬에_설정이_없어도_화면의_설정으로_소환_세션을_준비한다()
        {
            var root = new GameObject("TrainingSessionTest");
            root.SetActive(false);
            try
            {
                var type = System.Type.GetType("UPlayGround.Training.TrainingGroundSession, Assembly-CSharp", true);
                var session = (IUITrainingService)root.AddComponent(type);
                var config = AssetDatabase.LoadAssetAtPath<TrainingGroundConfigSO>(ConfigPath);
                Assert.That(config, Is.Not.Null);
                Assert.That(session.Config, Is.Null);
                session.Configure(config);
                Assert.That(session.Config, Is.SameAs(config));
                Assert.Throws<System.ArgumentNullException>(() => session.Configure(null));
                Assert.That(session.Config, Is.SameAs(config));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void 모든_난이도는_동일한_무기와_유효한_상대에_연결된다()
        {
            var config = AssetDatabase.LoadAssetAtPath<TrainingGroundConfigSO>(ConfigPath);
            Assert.That(config, Is.Not.Null);
            Assert.That(config.tiers, Is.Not.Empty);
            Assert.That(config.formation.Length, Is.EqualTo(config.weaponLabels.Length));
            foreach (var tier in config.tiers)
            {
                Assert.That(tier.members.Length, Is.EqualTo(config.weaponLabels.Length), tier.label);
                Assert.That(tier.members.Distinct().Count(), Is.EqualTo(tier.members.Length), tier.label);
                foreach (var member in tier.members)
                {
                    Assert.That(member, Is.Not.Null, tier.label);
                    Assert.That(member.prefab, Is.Not.Null, member.name);
                    Assert.That(member.EffectiveAbilitySet, Is.Not.Null, member.name);
                }
            }
            Assert.That(EditorBuildSettings.scenes.Any(scene => scene.enabled
                && System.IO.Path.GetFileNameWithoutExtension(scene.path) == config.sceneName), Is.True);
        }

        [Test]
        public void 훈련_화면의_조작_참조가_누락되지_않는다()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03.Prefabs/UI/Scene/UI_Scene_TrainingGround.prefab");
            Assert.That(prefab, Is.Not.Null);
            var screen = prefab.GetComponent<UI_Scene_TrainingGround>();
            Assert.That(screen, Is.Not.Null);
            var serialized = new SerializedObject(screen);
            foreach (string field in new[] { "_config", "_sceneContent", "_modeButton", "_tierButton",
                "_weaponButton", "_bossButton", "_distanceDownButton", "_distanceUpButton", "_distanceText",
                "_statusText", "_invincibleButton", "_stopButton", "_partyButton", "_targetButton",
                "_attackButton", "_startButton", "_restartButton", "_returnButton", "_closeButton", "_formationPanel" })
                Assert.That(serialized.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
            foreach (string field in new[] { "_lateralButtons", "_forwardButtons" })
            {
                var array = serialized.FindProperty(field);
                Assert.That(array.arraySize, Is.EqualTo(6));
                for (int i = 0; i < array.arraySize; i++)
                    Assert.That(array.GetArrayElementAtIndex(i).objectReferenceValue, Is.Not.Null, field);
            }
            foreach (var transform in prefab.GetComponentsInChildren<Transform>(true))
                Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject), Is.Zero);
        }

        [Test]
        public void 게임_메뉴는_훈련장_설정과_버튼을_갖는다()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03.Prefabs/UI/HUD/UI_Scene_MenuPanel.prefab");
            var serialized = new SerializedObject(prefab.GetComponent<UI_Scene_MenuPanel>());
            Assert.That(serialized.FindProperty("_trainingButton").objectReferenceValue, Is.Not.Null);
            Assert.That(serialized.FindProperty("_trainingConfig").objectReferenceValue,
                Is.EqualTo(AssetDatabase.LoadAssetAtPath<TrainingGroundConfigSO>(ConfigPath)));
        }
    }
}
