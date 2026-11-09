using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UPlayGround.CameraSystem;
using UPlayGround.Data;

namespace UPlayGround.CameraOcclusion.Tests
{
    /// <summary>카메라 가림 이름 규칙의 일치 규칙, 분류기 연동, 저작 데이터 연결을 검증한다.</summary>
    public sealed class CameraOccluderNameRulesTests
    {
        private const string RulesAssetPath = "Assets/10.Datas/Camera/CameraOccluderNameRules.asset";
        private const string SettingsAssetPath = "Assets/10.Datas/Camera/CameraSettings.asset";

        private readonly List<GameObject> _spawned = new();
        private CameraSettings _settings;
        private CameraOccluderNameRulesSO _rules;
        private int _proxyLayer;

        private static CameraOccluderNameRulesSO.Rule MakeRule(string pattern, bool isPrefix, OccluderMode mode, bool cover = false)
            => new() { namePattern = pattern, isPrefix = isPrefix, mode = mode, coversInterior = cover };

        [SetUp]
        public void SetUp()
        {
            _proxyLayer = LayerMask.NameToLayer("CameraFadeProxy");
            Assert.GreaterOrEqual(_proxyLayer, 0);
            _ = (GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset)?.scriptableRenderer;

            _rules = ScriptableObject.CreateInstance<CameraOccluderNameRulesSO>();
            _settings = ScriptableObject.CreateInstance<CameraSettings>();
            _settings.occlusionNameRules = _rules;
        }

        [TearDown]
        public void TearDown()
        {
            CameraOccluderFadeState.Clear();
            foreach (GameObject go in _spawned)
                if (go != null) Object.DestroyImmediate(go);
            _spawned.Clear();
            Object.DestroyImmediate(_settings);
            Object.DestroyImmediate(_rules);
        }

        private CameraOcclusionFader CreateFader()
            => new CameraOcclusionFader(_settings, null, ~0, 0, _proxyLayer);

        private GameObject CreateBox(string name, Vector3 position, Vector3 scale, Transform parent = null)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            _spawned.Add(go);
            Physics.SyncTransforms();
            return go;
        }

        [Test]
        public void Matches_ExactPatternAcceptsOnlyWholeNameOrInstanceSuffix()
        {
            var rule = MakeRule("LakeHouse_A", false, OccluderMode.Auto);

            Assert.IsTrue(CameraOccluderNameRulesSO.Matches(rule, "LakeHouse_A"));
            Assert.IsTrue(CameraOccluderNameRulesSO.Matches(rule, "LakeHouse_A (3)"));
            Assert.IsTrue(CameraOccluderNameRulesSO.Matches(rule, "LakeHouse_A(Clone)"));
            Assert.IsFalse(CameraOccluderNameRulesSO.Matches(rule, "LakeHouse_AB"), "이름이 더 긴 다른 오브젝트와 섞이면 안 된다.");
            Assert.IsFalse(CameraOccluderNameRulesSO.Matches(rule, "Other_LakeHouse_A"));
            Assert.IsFalse(CameraOccluderNameRulesSO.Matches(rule, ""));
        }

        [Test]
        public void Matches_PrefixPatternAcceptsAnySuffix()
        {
            var rule = MakeRule("TGV_Roof_", true, OccluderMode.Auto);

            Assert.IsTrue(CameraOccluderNameRulesSO.Matches(rule, "TGV_Roof_Thatch_01"));
            Assert.IsFalse(CameraOccluderNameRulesSO.Matches(rule, "TGV_Wall_01"));
        }

        [Test]
        public void TryMatch_FirstMatchingRuleWins()
        {
            _rules.SetRules(new List<CameraOccluderNameRulesSO.Rule>
            {
                MakeRule("Castle_Wall_Special", false, OccluderMode.Fade),
                MakeRule("Castle_Wall_", true, OccluderMode.Solid),
            });

            Assert.IsTrue(_rules.TryMatch("Castle_Wall_Special", out var special));
            Assert.AreEqual(OccluderMode.Fade, special.mode);
            Assert.IsTrue(_rules.TryMatch("Castle_Wall_Straight", out var general));
            Assert.AreEqual(OccluderMode.Solid, general.mode);
        }

        [Test]
        public void Classifier_AppliesRuleFromColliderOrAncestorName()
        {
            _rules.SetRules(new List<CameraOccluderNameRulesSO.Rule>
            {
                MakeRule("BigWall", false, OccluderMode.Solid),
            });
            using var fader = CreateFader();

            GameObject root = CreateBox("BigWall (1)", new Vector3(0, 1.5f, -3), new Vector3(6, 4, 0.3f));
            GameObject child = CreateBox("Mesh", new Vector3(0, 1.5f, -3), new Vector3(2, 2, 0.3f), root.transform);
            GameObject unrelated = CreateBox("Fence", new Vector3(10, 1.5f, -3), new Vector3(6, 4, 0.3f));

            Assert.IsTrue(fader.IsBlocking(root.GetComponent<Collider>()), "이름 규칙 Solid는 카메라를 막아야 한다.");
            Assert.IsTrue(fader.IsBlocking(child.GetComponent<Collider>()), "조상 이름의 규칙도 적용돼야 한다.");
            Assert.IsFalse(fader.IsBlocking(unrelated.GetComponent<Collider>()), "규칙에 없는 오브젝트는 자동 판정(Fade)이다.");
        }

        [Test]
        public void Classifier_CoverRuleEnablesInteriorGuard()
        {
            _rules.SetRules(new List<CameraOccluderNameRulesSO.Rule>
            {
                MakeRule("Roof_", true, OccluderMode.Auto, cover: true),
            });
            using var fader = CreateFader();
            CreateBox("Roof_A", new Vector3(0, 4f, -5f), new Vector3(10, 0.3f, 10));

            float limited = fader.LimitInterior(new Vector3(0, 1.2f, 5f), Vector3.back, 8f);

            Assert.Less(limited, 6f, "지붕 제공 규칙이 있는 콜라이더 아래로는 암이 들어가지 못한다.");
        }

        [Test]
        public void Classifier_UnmarkedRoofDoesNotLimitArm()
        {
            using var fader = CreateFader();
            CreateBox("Roof_A", new Vector3(0, 4f, -5f), new Vector3(10, 0.3f, 10));

            float limited = fader.LimitInterior(new Vector3(0, 1.2f, 5f), Vector3.back, 8f);

            Assert.AreEqual(8f, limited, 0.001f, "규칙이 없으면 지붕으로 인정하지 않는다(나무 캐노피 오탐 방지).");
        }

        [Test]
        public void Classifier_AuthoringComponentWinsOverNameRule()
        {
            _rules.SetRules(new List<CameraOccluderNameRulesSO.Rule>
            {
                MakeRule("Wall", true, OccluderMode.Solid),
            });
            using var fader = CreateFader();
            GameObject wall = CreateBox("Wall_A", new Vector3(0, 1.5f, -3), new Vector3(6, 4, 0.3f));
            var occluder = wall.AddComponent<CameraOccluder>();
            var mode = typeof(CameraOccluder).GetField("_mode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            mode.SetValue(occluder, OccluderMode.Fade);

            Assert.IsFalse(fader.IsBlocking(wall.GetComponent<Collider>()), "직접 붙인 CameraOccluder가 이름 규칙보다 우선이다.");
        }

        [Test]
        public void AuthoredRulesAsset_HasUniqueNonEmptyPatternsAndIsAssignedToCameraSettings()
        {
            var rules = AssetDatabase.LoadAssetAtPath<CameraOccluderNameRulesSO>(RulesAssetPath);
            Assert.IsNotNull(rules, $"{RulesAssetPath}가 있어야 한다.");
            Assert.Greater(rules.Rules.Count, 0);

            var seen = new HashSet<string>();
            foreach (var rule in rules.Rules)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(rule.namePattern), "빈 패턴 규칙이 있다.");
                Assert.IsTrue(seen.Add(rule.namePattern + "|" + rule.isPrefix), $"중복 규칙: {rule.namePattern}");
            }

            var settings = AssetDatabase.LoadAssetAtPath<CameraSettings>(SettingsAssetPath);
            Assert.IsNotNull(settings);
            Assert.AreSame(rules, settings.occlusionNameRules, "CameraSettings가 이름 규칙 에셋을 참조해야 한다.");
        }
    }
}
