using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround.Data.Path;
using UPlayGround.InputDefine;

namespace UPlayGround.UI.Tests
{
    /// <summary>전역 FPS 표시의 단축키 연결, 켜짐 상태 추종, 씬 중복 배치 방지를 검사한다.</summary>
    public sealed class FrameRateOverlayTests
    {
        private const string InputActionsPath = "Assets/Resources/Input/PlayerInputActions.inputactions";
        private const string PrefabDatabasePath = "Assets/10.Datas/Path/UIPrefabDatabase.asset";
        private const string FrameRatePrefabPath = "Assets/03.Prefabs/UI/HUD/UIFrameRateDisplay.prefab";
        private const string AutoShowScriptPath = "Assets/02.Scripts/UI/Common/UIFrameRateAutoShow.cs";
        private const string FrameRateDisplayKey = "FrameRateDisplay";
        private const string CombatTestMapPath = "Assets/01.Scenes/Test/CombatTestMap.unity";

        [TearDown]
        public void ResetVisibility() => UIFrameRateOverlay.SetVisible(false);

        [Test]
        public void FPS_토글은_UI맵에_F12_기본_바인딩으로_연결된다()
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            Assert.That(asset, Is.Not.Null);

            InputAction action = asset.FindActionMap(InputMapNames.UI, true)
                .FindAction(UIAction.ToggleFrameRate, true);
            Assert.That(action.type, Is.EqualTo(InputActionType.Button));
            Assert.That(action.bindings.Select(binding => binding.path), Does.Contain("<Keyboard>/f12"));
        }

        [Test]
        public void UI_프리팹_DB는_전역_FPS_프리팹을_등록한다()
        {
            var database = AssetDatabase.LoadAssetAtPath<UIPrefabDatabase>(PrefabDatabasePath);
            Assert.That(database, Is.Not.Null);
            database.Initialize();

            GameObject prefab = database.GetPrefab(FrameRateDisplayKey);
            Assert.That(prefab, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(prefab), Is.EqualTo(FrameRatePrefabPath));
            Assert.That(prefab.GetComponent<UIFrameRateDisplay>(), Is.Not.Null);
        }

        [Test]
        public void 오버레이는_켜짐_상태를_따르고_해제_후에는_상태_변경에_반응하지_않는다()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FrameRatePrefabPath);
            var parent = new GameObject("FrameRateOverlayTestRoot");
            UIFrameRateOverlay overlay = null;
            try
            {
                UIFrameRateOverlay.SetVisible(false);
                overlay = new UIFrameRateOverlay(prefab, parent.transform);
                GameObject instance = parent.transform.GetChild(0).gameObject;
                Assert.That(instance.activeSelf, Is.False, "꺼진 상태로 만들면 비활성이어야 한다");

                UIFrameRateOverlay.ToggleVisible();
                Assert.That(UIFrameRateOverlay.IsVisible, Is.True);
                Assert.That(instance.activeSelf, Is.True);

                UIFrameRateOverlay.ToggleVisible();
                Assert.That(instance.activeSelf, Is.False);

                overlay.Dispose();
                overlay = null;
                Assert.That(parent.transform.childCount, Is.Zero);
                Assert.DoesNotThrow(() => UIFrameRateOverlay.SetVisible(true));
            }
            finally
            {
                overlay?.Dispose();
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void 이미_켜진_상태에서_만든_오버레이는_바로_보인다()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FrameRatePrefabPath);
            var parent = new GameObject("FrameRateOverlayTestRoot");
            UIFrameRateOverlay overlay = null;
            try
            {
                // 시험장 표식이 UIManager 초기화보다 먼저 켜는 순서를 재현한다.
                UIFrameRateOverlay.SetVisible(true);
                overlay = new UIFrameRateOverlay(prefab, parent.transform);
                Assert.That(parent.transform.GetChild(0).gameObject.activeSelf, Is.True);
            }
            finally
            {
                overlay?.Dispose();
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void 어떤_씬도_FPS_프리팹을_직접_두지_않는다()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets" }))
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(guid);
                Assert.That(AssetDatabase.GetDependencies(scenePath, false), Does.Not.Contain(FrameRatePrefabPath),
                    $"{scenePath}: 씬에 둔 FPS 프리팹은 전역 표시와 겹쳐 그려진다. UIFrameRateAutoShow 표식을 쓴다");
            }
        }

        [Test]
        public void 전투_시험장은_시작할_때_FPS를_켠다()
        {
            Assert.That(AssetDatabase.GetDependencies(CombatTestMapPath, false), Does.Contain(AutoShowScriptPath));
        }
    }
}
