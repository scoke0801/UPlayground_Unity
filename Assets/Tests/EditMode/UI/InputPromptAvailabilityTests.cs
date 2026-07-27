using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UPlayGround.InputDefine;
using UPlayGround.UI.InputPrompt;

namespace UPlayGround.UI.Tests
{
    public sealed class InputPromptAvailabilityTests
    {
        [Test]
        public void HasBindingFor_현재장치바인딩만찾는다()
        {
            using var action = new InputAction("PageNext");
            action.AddBinding("<Gamepad>/rightTrigger");

            Assert.IsTrue(InputPromptAvailability.HasBindingFor(
                action,
                ActiveInputDevice.Gamepad));
            Assert.IsFalse(InputPromptAvailability.HasBindingFor(
                action,
                ActiveInputDevice.KeyboardMouse));
        }

        [Test]
        public void HasBindingFor_키보드와마우스를같은장치군으로취급한다()
        {
            using var action = new InputAction("PointAndCancel");
            action.AddBinding("<Keyboard>/escape");
            action.AddBinding("<Mouse>/rightButton");

            Assert.IsTrue(InputPromptAvailability.HasBindingFor(
                action,
                ActiveInputDevice.KeyboardMouse));
            Assert.IsFalse(InputPromptAvailability.HasBindingFor(
                action,
                ActiveInputDevice.Gamepad));
        }

        [Test]
        public void HasBindingFor_복합바인딩의첫유효파트로장치를판정한다()
        {
            using var action = new InputAction("Chord");
            action.AddCompositeBinding("OneModifier")
                .With("Modifier", "<Gamepad>/leftShoulder")
                .With("Binding", "<Gamepad>/buttonEast");

            Assert.IsTrue(InputPromptAvailability.HasBindingFor(
                action,
                ActiveInputDevice.Gamepad));
            Assert.IsFalse(InputPromptAvailability.HasBindingFor(
                action,
                ActiveInputDevice.KeyboardMouse));
        }

        [Test]
        public void HasBindingFor_빈오버라이드는미바인딩으로처리한다()
        {
            using var action = new InputAction("Removed");
            action.AddBinding("<Keyboard>/escape");
            action.ApplyBindingOverride(0, string.Empty);

            Assert.IsFalse(InputPromptAvailability.HasBindingFor(
                action,
                ActiveInputDevice.KeyboardMouse));
        }

        [Test]
        public void 복합바인딩_폴백문자열에모든파트를보존한다()
        {
            using var action = new InputAction("QuickSlot");
            action.AddCompositeBinding("OneModifier")
                .With("Modifier", "<Gamepad>/leftShoulder")
                .With("Binding", "<Gamepad>/dpad/up");

            InputGlyphResult result = InputGlyphResolver.ResolveAction(
                action,
                ActiveInputDevice.Gamepad,
                GamepadBrand.Generic,
                null);

            Assert.AreEqual(2, result.Count);
            Assert.That(result.GetDisplayText(" + "), Does.Contain(" + "));
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.Parts[0].Text));
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.Parts[1].Text));
        }

        [Test]
        public void ResetScrollToTop_관성을지우고최상단으로이동한다()
        {
            var root = new GameObject("ScrollRect", typeof(RectTransform), typeof(ScrollRect));
            try
            {
                var viewport = new GameObject("Viewport", typeof(RectTransform));
                viewport.transform.SetParent(root.transform, false);
                var content = new GameObject("Content", typeof(RectTransform));
                content.transform.SetParent(viewport.transform, false);

                var scrollRect = root.GetComponent<ScrollRect>();
                scrollRect.viewport = viewport.GetComponent<RectTransform>();
                scrollRect.content = content.GetComponent<RectTransform>();
                scrollRect.viewport.sizeDelta = new Vector2(100f, 100f);
                scrollRect.content.sizeDelta = new Vector2(100f, 300f);
                scrollRect.vertical = true;
                scrollRect.velocity = new Vector2(0f, -300f);
                scrollRect.verticalNormalizedPosition = 0f;

                UIFocusNavigation.ResetScrollToTop(scrollRect);

                Assert.AreEqual(1f, scrollRect.verticalNormalizedPosition);
                Assert.AreEqual(Vector2.zero, scrollRect.velocity);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [TestCase(80f, true)]
        [TestCase(-80f, false)]
        public void ScrollRect선택추적_벗어난방향으로정규화위치를보정한다(
            float targetY,
            bool expectsIncrease)
        {
            var viewportObject = new GameObject("Viewport", typeof(RectTransform));
            try
            {
                RectTransform viewport = viewportObject.GetComponent<RectTransform>();
                viewport.sizeDelta = new Vector2(100f, 100f);

                var contentObject = new GameObject("Content", typeof(RectTransform));
                contentObject.transform.SetParent(viewport, false);
                RectTransform content = contentObject.GetComponent<RectTransform>();
                content.sizeDelta = new Vector2(100f, 300f);

                var targetObject = new GameObject("Target", typeof(RectTransform));
                targetObject.transform.SetParent(content, false);
                RectTransform target = targetObject.GetComponent<RectTransform>();
                target.sizeDelta = new Vector2(20f, 20f);
                target.anchoredPosition = new Vector2(0f, targetY);

                var scrollRect = viewportObject.AddComponent<ScrollRect>();
                scrollRect.viewport = viewport;
                scrollRect.content = content;
                scrollRect.horizontal = false;
                scrollRect.vertical = true;

                float current = scrollRect.verticalNormalizedPosition;
                Vector2 result = UIFocusScope.CalculateNormalizedPositionFor(
                    scrollRect,
                    target,
                    0f);

                if (expectsIncrease)
                    Assert.Greater(result.y, current);
                else
                    Assert.Less(result.y, current);
            }
            finally
            {
                Object.DestroyImmediate(viewportObject);
            }
        }

        [Test]
        public void 같은선택에서수동스크롤한위치를자동추적이되돌리지않는다()
        {
            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
            var scopeObject = new GameObject(
                "Scope",
                typeof(RectTransform),
                typeof(UIFocusScope));
            UIFocusScope scope = scopeObject.GetComponent<UIFocusScope>();
            try
            {
                EventSystem eventSystem = eventSystemObject.GetComponent<EventSystem>();
                MethodInfo enableEventSystem = typeof(EventSystem).GetMethod(
                    "OnEnable",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(enableEventSystem);
                enableEventSystem.Invoke(eventSystem, null);
                Assert.AreSame(eventSystem, EventSystem.current);

                var viewportObject = new GameObject("Viewport", typeof(RectTransform));
                viewportObject.transform.SetParent(scopeObject.transform, false);
                RectTransform viewport = viewportObject.GetComponent<RectTransform>();
                viewport.sizeDelta = new Vector2(100f, 100f);

                var contentObject = new GameObject("Content", typeof(RectTransform));
                contentObject.transform.SetParent(viewport, false);
                RectTransform content = contentObject.GetComponent<RectTransform>();
                content.sizeDelta = new Vector2(100f, 300f);

                var targetObject = new GameObject(
                    "Target",
                    typeof(RectTransform),
                    typeof(Selectable));
                targetObject.transform.SetParent(content, false);
                RectTransform target = targetObject.GetComponent<RectTransform>();
                target.sizeDelta = new Vector2(20f, 20f);

                var scrollRect = viewportObject.AddComponent<ScrollRect>();
                scrollRect.viewport = viewport;
                scrollRect.content = content;
                scrollRect.horizontal = false;
                scrollRect.vertical = true;

                EventSystem.current.SetSelectedGameObject(targetObject);
                scope.ActivateScope();

                MethodInfo track = typeof(UIFocusScope).GetMethod(
                    "TrackSelectionIntoView",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo hasTarget = typeof(UIFocusScope).GetField(
                    "_hasScrollTarget",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(track);
                Assert.NotNull(hasTarget);

                track.Invoke(scope, null);
                Assert.IsFalse((bool)hasTarget.GetValue(scope));

                scrollRect.verticalNormalizedPosition = 0f;
                float manualPosition = scrollRect.verticalNormalizedPosition;
                track.Invoke(scope, null);

                Assert.AreEqual(manualPosition, scrollRect.verticalNormalizedPosition);
                Assert.IsFalse((bool)hasTarget.GetValue(scope));
            }
            finally
            {
                scope.DeactivateScope();
                Object.DestroyImmediate(scopeObject);
                Object.DestroyImmediate(eventSystemObject);
            }
        }
    }
}
