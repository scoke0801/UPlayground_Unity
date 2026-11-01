using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Ability.Core;

namespace UPlayGround.UI.Tests
{
    /// <summary>필살기 에너지 레일의 프리팹 연결, 채움 경계, 눈금 점등, 준비 표시를 검증합니다.</summary>
    public sealed class UltimateEnergyRailTests
    {
        private const string Prefab = "Assets/03.Prefabs/UI/HUD/UI_HUD_PlayerInfo.prefab";
        private GameObject _hud;
        private UIUltimateEnergyRail _rail;

        [SetUp]
        public void SetUp()
        {
            _hud = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
            _rail = _hud.GetComponentInChildren<UIUltimateEnergyRail>(true);
        }

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(_hud);

        [Test]
        public void HUD_레일의_필수_참조가_연결되어_있다()
        {
            Assert.That(_rail, Is.Not.Null);
            var data = new SerializedObject(_rail);
            foreach (string field in new[] { "_fillArea", "_energyFill", "_trailFill", "_head", "_readyGlow", "_group", "_sparks", "_keyPrompt", "_sealMark" })
                Assert.That(data.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
            Assert.That(data.FindProperty("_glints").arraySize, Is.EqualTo(2));
            var energy = (Image)data.FindProperty("_energyFill").objectReferenceValue;
            Assert.That(energy.material.shader.name, Is.EqualTo("UPlayGround/UI/UltimateRailEnergyCanvas"));
            var sparks = (Graphic)data.FindProperty("_sparks").objectReferenceValue;
            Assert.That(sparks.material.shader.name, Is.EqualTo("UPlayGround/UI/UltimateRailSparksCanvas"));
            var prompt = ((RectTransform)data.FindProperty("_keyPrompt").objectReferenceValue)
                .GetComponentInChildren<UPlayGround.UI.InputPrompt.UIInputPromptIcon>(true);
            Assert.That(prompt, Is.Not.Null, "발동 키는 장치별 글리프 시스템으로 표시한다");
            var markers = data.FindProperty("_markers");
            Assert.That(markers.arraySize, Is.GreaterThanOrEqualTo(1));
            for (int i = 0; i < markers.arraySize; i++)
            {
                Assert.That(markers.GetArrayElementAtIndex(i).FindPropertyRelative("root").objectReferenceValue, Is.Not.Null);
                Assert.That(markers.GetArrayElementAtIndex(i).FindPropertyRelative("lit").objectReferenceValue, Is.Not.Null);
            }
            var fill = (Image)data.FindProperty("_energyFill").objectReferenceValue;
            Assert.That(fill.type, Is.EqualTo(Image.Type.Filled));
            Assert.That(fill.fillMethod, Is.EqualTo(Image.FillMethod.Horizontal));
        }

        [TestCase(70f, 100f, .7f)]
        [TestCase(-5f, 100f, 0f)]
        [TestCase(250f, 100f, 1f)]
        [TestCase(50f, 0f, 0f)]
        [TestCase(float.NaN, 100f, 0f)]
        public void 채움은_정규화된_자원량을_따른다(float current, float maximum, float expected)
        {
            _rail.SetResource(current, maximum, immediate: true);
            Assert.That(EnergyFill().fillAmount, Is.EqualTo(expected).Within(.0001f));
        }

        [Test]
        public void 속성_색은_레일과_불티에_적용되고_기존_투명도는_유지된다()
        {
            var data = new SerializedObject(_rail);
            var energy = (Graphic)data.FindProperty("_energyFill").objectReferenceValue;
            var sparks = (Graphic)data.FindProperty("_sparks").objectReferenceValue;
            float energyAlpha = energy.color.a;
            float sparksAlpha = sparks.color.a;
            _rail.SetElementColor(Color.cyan);
            Assert.That(energy.color, Is.EqualTo(new Color(0f, 1f, 1f, energyAlpha)));
            Assert.That(sparks.color, Is.EqualTo(new Color(0f, 1f, 1f, sparksAlpha)));
            _rail.SetElementColor(Color.red);
            Assert.That(energy.color, Is.EqualTo(new Color(1f, 0f, 0f, energyAlpha)));
        }

        [Test]
        public void 잔량과_같은_잔상은_에너지_윤곽을_덮지_않는다()
        {
            var trail = (Image)new SerializedObject(_rail).FindProperty("_trailFill").objectReferenceValue;
            _rail.SetResource(35f, 100f, immediate: true);
            Assert.That(trail.enabled, Is.False);
            _rail.SetResource(100f, 100f, immediate: true);
            Assert.That(trail.enabled, Is.False);
        }

        [Test]
        public void 눈금은_넘어선_구간만_점등한다()
        {
            var markers = new SerializedObject(_rail).FindProperty("_markers");
            int count = markers.arraySize;
            float firstRatio = 1f / (count + 1);
            _rail.SetResource(firstRatio * 100f - 1f, 100f, immediate: true);
            for (int i = 0; i < count; i++)
                Assert.That(Lit(markers, i).activeSelf, Is.False, $"marker {i}");
            _rail.SetResource(100f, 100f, immediate: true);
            for (int i = 0; i < count; i++)
                Assert.That(Lit(markers, i).activeSelf, Is.True, $"marker {i}");
            _rail.SetResource(firstRatio * 100f + 1f, 100f, immediate: true);
            Assert.That(Lit(markers, 0).activeSelf, Is.True);
            if (count > 1) Assert.That(Lit(markers, 1).activeSelf, Is.False);
        }

        [Test]
        public void 준비_연출은_일시_제한에_유지되고_자원_소비시_꺼진다()
        {
            var data = new SerializedObject(_rail);
            var glow = ((Graphic)data.FindProperty("_readyGlow").objectReferenceValue).gameObject;
            var group = (CanvasGroup)data.FindProperty("_group").objectReferenceValue;
            var prompt = ((RectTransform)data.FindProperty("_keyPrompt").objectReferenceValue).gameObject;
            _rail.SetResource(100f, 100f, immediate: true);
            _rail.SetAbilityState(true, State(true, AbilityActivationResult.Success));
            Assert.That(glow.activeSelf, Is.True);
            Assert.That(prompt.activeSelf, Is.True, "준비 완료 시 레일 끝에 발동 키를 안내한다");
            Assert.That(group.alpha, Is.EqualTo(1f));
            _rail.SetAbilityState(true, State(false, AbilityActivationResult.BlockedByTag));
            Assert.That(glow.activeSelf, Is.True);
            Assert.That(prompt.activeSelf, Is.True);
            Assert.That(group.alpha, Is.EqualTo(1f));
            _rail.SetResource(50f, 100f, immediate: true);
            _rail.SetAbilityState(true, State(false, AbilityActivationResult.InsufficientResource));
            Assert.That(glow.activeSelf, Is.False);
            Assert.That(prompt.activeSelf, Is.False, "발동 후에는 키 안내가 즉시 사라진다");
            Assert.That(group.alpha, Is.EqualTo(1f), "자원 부족은 채움량으로 전달한다");
            _rail.SetAbilityState(true, State(false, AbilityActivationResult.BlockedByTag));
            Assert.That(group.alpha, Is.LessThan(1f));
        }

        [Test]
        public void 미습득_필살기도_공용_스킬_게이지는_표시한다()
        {
            var data = new SerializedObject(_rail);
            var fillArea = ((RectTransform)data.FindProperty("_fillArea").objectReferenceValue).gameObject;
            var seal = (GameObject)data.FindProperty("_sealMark").objectReferenceValue;
            var prompt = ((RectTransform)data.FindProperty("_keyPrompt").objectReferenceValue).gameObject;
            _rail.SetResource(100f, 100f, immediate: true);
            _rail.SetAbilityState(true, State(false, AbilityActivationResult.Locked));
            Assert.That(fillArea.activeSelf, Is.True, "T/E가 사용하는 공용 게이지는 R 잠금 중에도 표시한다");
            Assert.That(seal.activeSelf, Is.False);
            Assert.That(prompt.activeSelf, Is.False);
            _rail.SetAbilityState(true, State(true, AbilityActivationResult.Success));
            Assert.That(fillArea.activeSelf, Is.True, "습득하면 쌓아 둔 충전량이 바로 보인다");
            Assert.That(seal.activeSelf, Is.False);
            Assert.That(prompt.activeSelf, Is.True);
        }

        private Image EnergyFill() =>
            (Image)new SerializedObject(_rail).FindProperty("_energyFill").objectReferenceValue;

        private static GameObject Lit(SerializedProperty markers, int index) =>
            ((Graphic)markers.GetArrayElementAtIndex(index).FindPropertyRelative("lit").objectReferenceValue).gameObject;

        private static AbilitySlotViewState State(bool ready, AbilityActivationResult reason) =>
            new AbilitySlotViewState("test", true, true, ready, reason, 100f, 100f, 0f, 5f, string.Empty);
    }
}
