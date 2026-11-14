using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.Item;
using UPlayGround.UI;

namespace UPlayGround.EditorTools.Tests
{
    /// <summary>드롭 모델 선택과 이름표 연결선의 좌표·수명을 검사한다.</summary>
    public sealed class GroundDropPresentationTests
    {
        [TestCase(true, false, false, true, false)]
        [TestCase(false, true, false, true, false)]
        [TestCase(false, true, true, false, false)]
        [TestCase(false, false, false, false, false)]
        [TestCase(true, true, true, true, false)]
        [TestCase(false, true, false, true, true)]
        public void 드롭은_명시모델과_장착모델을_우선하고_고유무기는_메달을_유지한다(
            bool explicitModel, bool equippedModel, bool builtIn, bool expectedModel, bool skinned)
        {
            var item = ScriptableObject.CreateInstance<EquipmentSO>();
            var settings = ScriptableObject.CreateInstance<GroundDropSettingsSO>();
            var source = skinned
                ? new GameObject("모델 원본", typeof(SkinnedMeshRenderer))
                : new GameObject("모델 원본", typeof(MeshFilter), typeof(MeshRenderer));
            var mesh = new Mesh();
            mesh.vertices = new[] { Vector3.zero, Vector3.right, Vector3.up };
            mesh.triangles = new[] { 0, 1, 2 };
            if (skinned) source.GetComponent<SkinnedMeshRenderer>().sharedMesh = mesh;
            else source.GetComponent<MeshFilter>().sharedMesh = mesh;
            var root = new GameObject("드롭 검사");
            try
            {
                item.worldPrefab = explicitModel ? source : null;
                item.equipmentPrefab = equippedModel ? source : null;
                item.visualMode = builtIn ? EquipmentVisualMode.CharacterBuiltIn : EquipmentVisualMode.Prefab;
                var view = root.AddComponent<GroundDropRepresentation>();
                typeof(GroundDropRepresentation).GetField("_settings", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(view, settings);
                typeof(GroundDropRepresentation).GetMethod("BuildVisual", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(view, new object[] { item });
                var model = root.transform.Find("Visual/Token/WorldModel");
                Assert.That(model != null, Is.EqualTo(expectedModel));
                Assert.That(root.transform.Find("Visual/Token/Plate") != null, Is.EqualTo(!expectedModel));
                if (model != null)
                {
                    Assert.That(model.localScale.x, Is.EqualTo(settings.tokenSize).Within(0.0001f));
                    Assert.That(model.localPosition.x, Is.EqualTo(-settings.tokenSize * 0.5f).Within(0.0001f));
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(source);
                UnityEngine.Object.DestroyImmediate(mesh);
                UnityEngine.Object.DestroyImmediate(item);
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void 연결선은_이름표_뒤에서_실제목표로_기울고_숨김을_따른다()
        {
            var parent = new GameObject("이름표 검사", typeof(RectTransform));
            try
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/UI/WorldSpace/UIItemDropLabel.prefab");
                var label = UnityEngine.Object.Instantiate(prefab, parent.transform).GetComponent<UIItemDropLabel>();
                label.Rect.anchoredPosition = new Vector2(50f, 100f);
                label.Rect.localScale = Vector3.one * 0.8f;
                label.GetComponent<CanvasGroup>().alpha = 0.5f;
                label.ConfigureTail(1.25f, Color.white, 0.32f);
                label.SetTailTarget(new Vector2(30f, -100f));
                var line = parent.GetComponentInChildren<UIDropLabelConnector>();
                Assert.That(line, Is.Not.Null);
                Assert.That(line.transform.GetSiblingIndex(), Is.Zero);
                Assert.That(line.raycastTarget, Is.False);
                Assert.That(line.color.a, Is.EqualTo(0.16f).Within(0.0001f));
                var tail = (RectTransform)line.transform;
                Vector3 target = label.Rect.TransformPoint(new Vector2(30f, -100f));
                Assert.That(Vector3.Cross(target - tail.position, -tail.up).magnitude, Is.LessThan(0.0001f));
                label.SetTailTarget(Vector2.zero);
                Assert.That(line.gameObject.activeSelf, Is.False);
                label.SetTailTarget(new Vector2(0f, -100f));
                Assert.That(line.gameObject.activeSelf, Is.True);
                label.gameObject.SetActive(false);
                // EditMode에서는 일반 MonoBehaviour의 비활성 콜백이 자동 실행되지 않는다.
                typeof(UIItemDropLabel).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(label, null);
                Assert.That(line.gameObject.activeSelf, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(parent); }
        }

        [Test]
        public void 연결선_메시는_양옆이_투명하고_끝으로_갈수록_가늘어진다()
        {
            var root = new GameObject("연결선 메시 검사", typeof(RectTransform), typeof(UIDropLabelConnector));
            try
            {
                var graphic = root.GetComponent<UIDropLabelConnector>();
                graphic.rectTransform.sizeDelta = new Vector2(2f, 60f);
                graphic.color = Color.white;
                using (var vertices = new VertexHelper())
                {
                    typeof(UIDropLabelConnector).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic,
                        null, new[] { typeof(VertexHelper) }, null)
                        .Invoke(graphic, new object[] { vertices });
                    Assert.That(vertices.currentVertCount, Is.EqualTo(8));
                    var edge = new UIVertex();
                    var start = new UIVertex();
                    var end = new UIVertex();
                    vertices.PopulateUIVertex(ref edge, 0);
                    vertices.PopulateUIVertex(ref start, 1);
                    vertices.PopulateUIVertex(ref end, 5);
                    Assert.That(edge.color.a, Is.Zero);
                    Assert.That(end.color.a, Is.LessThan(start.color.a));
                    Assert.That(Mathf.Abs(end.position.x), Is.LessThan(Mathf.Abs(start.position.x)));
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
