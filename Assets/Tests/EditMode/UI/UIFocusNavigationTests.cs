using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace UPlayGround.UI.Tests
{
    public sealed class UIFocusNavigationTests
    {
        private readonly List<GameObject> _objects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject gameObject in _objects)
            {
                if (gameObject != null)
                    Object.DestroyImmediate(gameObject);
            }
            _objects.Clear();
        }

        [Test]
        public void ConfigureVertical_비활성항목을건너뛴다()
        {
            Button first = CreateButton("First");
            Button disabled = CreateButton("Disabled");
            Button last = CreateButton("Last");
            disabled.interactable = false;

            UIFocusNavigation.ConfigureVertical(new Selectable[]
            {
                first,
                disabled,
                last
            });

            Assert.That(first.navigation.mode, Is.EqualTo(Navigation.Mode.Explicit));
            Assert.That(first.navigation.selectOnDown, Is.SameAs(last));
            Assert.That(last.navigation.selectOnUp, Is.SameAs(first));
        }

        [Test]
        public void ConfigureHorizontal_순환옵션이면양끝을연결한다()
        {
            Button first = CreateButton("First");
            Button middle = CreateButton("Middle");
            Button last = CreateButton("Last");

            UIFocusNavigation.ConfigureHorizontal(new Selectable[]
            {
                first,
                middle,
                last
            }, wrap: true);

            Assert.That(first.navigation.selectOnLeft, Is.SameAs(last));
            Assert.That(last.navigation.selectOnRight, Is.SameAs(first));
            Assert.That(middle.navigation.selectOnLeft, Is.SameAs(first));
            Assert.That(middle.navigation.selectOnRight, Is.SameAs(last));
        }

        [Test]
        public void ConfigureGrid_열수에맞춰상하좌우를연결한다()
        {
            Button[] buttons =
            {
                CreateButton("0"),
                CreateButton("1"),
                CreateButton("2"),
                CreateButton("3"),
                CreateButton("4")
            };

            UIFocusNavigation.ConfigureGrid(buttons, columns: 3);

            Assert.That(buttons[0].navigation.selectOnRight, Is.SameAs(buttons[1]));
            Assert.That(buttons[0].navigation.selectOnDown, Is.SameAs(buttons[3]));
            Assert.That(buttons[2].navigation.selectOnRight, Is.Null);
            Assert.That(buttons[4].navigation.selectOnUp, Is.SameAs(buttons[1]));
            Assert.That(buttons[4].navigation.selectOnRight, Is.Null);
        }

        private Button CreateButton(string name)
        {
            var gameObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            _objects.Add(gameObject);
            return gameObject.GetComponent<Button>();
        }
    }
}
