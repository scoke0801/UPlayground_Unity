using System;
using System.Collections;
using System.Collections.Generic;
using UPlayGround.Data.EnumType;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using UPlayGround.Manager;
using UPlayGround.Data.Party;

namespace UPlayGround.UI.Tests
{
    /// <summary>성장 경로의 메시, 셰이더 및 저작된 아이콘 참조를 검증한다.</summary>
    public sealed class SkillTreeVisualTests
    {
        [UnityTest]
        public IEnumerator 실제_성장화면의_곡선은_일시정지중에도_흐른다()
        {
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);
            yield return new EnterPlayMode();
            IUIPartyService service = DispatchProxy.Create<IUIPartyService, SkillTreePreviewParty>();
            var preview = (SkillTreePreviewParty)(object)service;
            preview.Tree = AssetDatabase.LoadAssetAtPath<CharacterSkillTreeSO>(
                "Assets/10.Datas/Party/SkillTree/CharacterSkillTree_Reine.asset");
            Services.Register(service);
            var cameraObject = new GameObject("성장 화면 검증 카메라", typeof(UnityEngine.Camera));
            var camera = cameraObject.GetComponent<UnityEngine.Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            var texture = new RenderTexture(1920, 1080, 24);
            camera.targetTexture = texture;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03.Prefabs/UI/Scene/Growth/UI_Scene_SkillTree.prefab");
            GameObject root = UnityEngine.Object.Instantiate(prefab);
            var view = root.GetComponent<UI_Scene_SkillTree>();
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            float originalTimeScale = Time.timeScale;
            try
            {
                view.Configure(preview.Tree.characterType);
                Invoke(view, "StartFlow");
                Invoke(view, "RebuildAll");
                yield return null;
                Transform tabs = root.transform.Find("SkillTreeRuntimeRoot/Window/CharacterTabs");
                Assert.That(tabs.Find("Character_H09"), Is.Null);
                Assert.That(tabs.childCount, Is.EqualTo(1));
                Invoke(view, "SelectCharacterTab", 0);
                Assert.That(tabs.Find($"Character_{preview.Tree.characterType}"), Is.Not.Null);
                var curves = root.GetComponentsInChildren<UISkillTreeCurve>();
                Assert.That(curves.Length, Is.GreaterThan(0));
                Material material = curves[0].material;
                float before = material.GetFloat("_UnscaledTime");
                Time.timeScale = 0f;
                yield return new WaitForSecondsRealtime(0.15f);
                Assert.That(material.GetFloat("_UnscaledTime"), Is.GreaterThan(before));
                Assert.That(root.GetComponentsInChildren<TMPro.TextMeshProUGUI>()
                    .Any(text => text.name == "Glyph"), Is.False);
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = texture;
                var capture = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
                capture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                capture.Apply();
                Directory.CreateDirectory("output/SkillTreeVisuals");
                File.WriteAllBytes("output/SkillTreeVisuals/SkillTree-1920x1080.png", capture.EncodeToPNG());
                UnityEngine.Object.Destroy(capture);
                RenderTexture.active = previous;
                Invoke(view, "StopFlow");
                float stopped = material.GetFloat("_UnscaledTime");
                yield return new WaitForSecondsRealtime(0.05f);
                Assert.That(material.GetFloat("_UnscaledTime"), Is.EqualTo(stopped));
            }
            finally
            {
                Time.timeScale = originalTimeScale;
                UnityEngine.Object.Destroy(root);
                UnityEngine.Object.Destroy(cameraObject);
                texture.Release();
                UnityEngine.Object.Destroy(texture);
                Services.Unregister(service);
            }
            yield return new ExitPlayMode();
        }

        private static void Invoke(UI_Scene_SkillTree view, string method) =>
            typeof(UI_Scene_SkillTree).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, null);

        private static void Invoke(UI_Scene_SkillTree view, string method, int index) =>
            typeof(UI_Scene_SkillTree).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(view, new object[] { index });

        [Test]
        public void 모든_성장경로는_분류안에서_위에서_아래로_이어진다()
        {
            MethodInfo layout = typeof(UI_Scene_SkillTree).GetMethod("BuildHierarchyPositions",
                BindingFlags.Static | BindingFlags.NonPublic);
            string[] trees = AssetDatabase.FindAssets("t:CharacterSkillTreeSO");
            Assert.That(trees.Length, Is.GreaterThan(0));
            foreach (string guid in trees)
            {
                var tree = AssetDatabase.LoadAssetAtPath<CharacterSkillTreeSO>(AssetDatabase.GUIDToAssetPath(guid));
                var positions = new Dictionary<string, Vector2>();
                layout.Invoke(null, new object[] { tree.nodes, new Vector2(1052f, 960f), positions });
                foreach (SkillNodeDefinition node in tree.nodes)
                {
                    Assert.That(node.categoryName, Is.Not.Null.And.Not.Empty, tree.name);
                    Vector2 position = positions[node.NormalizedId];
                    Assert.That(Mathf.Abs(position.x) + 77f, Is.LessThan(526f), node.NormalizedId);
                    Assert.That(Mathf.Abs(position.y) + 88f, Is.LessThan(480f), node.NormalizedId);
                    foreach (string required in node.requiredNodeIds)
                        Assert.That(positions[required].y - position.y, Is.GreaterThan(176f), node.NormalizedId);
                    foreach (SkillNodeDefinition other in tree.nodes)
                    {
                        if (other == node) continue;
                        Vector2 delta = position - positions[other.NormalizedId];
                        Assert.That(Mathf.Abs(delta.x) >= 154f || Mathf.Abs(delta.y) >= 176f,
                            Is.True, $"{tree.name}: {node.NormalizedId} / {other.NormalizedId}");
                    }
                }
            }
        }

        [Test]
        public void Canvas그래프와_프리팹의_표시리소스가_연결된다()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/06.Shaders/UI/SkillTreeFlowCanvas.shadergraph");
            Assert.That(shader, Is.Not.Null);
            Assert.That(ShaderUtil.ShaderHasError(shader), Is.False);
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/UI/SkillTreeFlow.mat");
            Assert.That(material.shader, Is.SameAs(shader));
            Assert.That(material.HasProperty("_UnscaledTime"), Is.True);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03.Prefabs/UI/Scene/Growth/UI_Scene_SkillTree.prefab");
            var serialized = new SerializedObject(prefab.GetComponent<UI_Scene_SkillTree>());
            Assert.That(serialized.FindProperty("_flowMaterial").objectReferenceValue, Is.SameAs(material));
            Assert.That(serialized.FindProperty("_fallbackNodeIcon").objectReferenceValue, Is.Not.Null);
        }

        [Test]
        public void 모든_성장노드에_실제_스프라이트가_있다()
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath("Assets/04.Images/UI/SkillIcon/SkillTreeCelIcons.png")
                .OfType<Sprite>().ToArray();
            Assert.That(sprites.Length, Is.EqualTo(12));
            foreach (string guid in AssetDatabase.FindAssets("t:CharacterSkillTreeSO"))
            {
                var tree = AssetDatabase.LoadAssetAtPath<CharacterSkillTreeSO>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (SkillNodeDefinition node in tree.nodes)
                    Assert.That(node.icon, Is.Not.Null, tree.name + ": " + node.NormalizedId);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void 곡선의_양끝과_이동방향은_노드좌표를_따른다(bool canFlow)
        {
            var go = new GameObject("곡선 검증", typeof(RectTransform));
            ((RectTransform)go.transform).sizeDelta = new Vector2(800f, 600f);
            using var vertices = new VertexHelper();
            try
            {
                var curve = go.AddComponent<UISkillTreeCurve>();
                Vector2 start = new(-100f, -120f);
                Vector2 end = new(130f, 180f);
                curve.Configure(start, end, 12f, 0.5f, 40, canFlow, Vector2.right);
                typeof(UISkillTreeCurve).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic,
                        null, new[] { typeof(VertexHelper) }, null)
                    .Invoke(curve, new object[] { vertices });
                Assert.That(vertices.currentVertCount, Is.EqualTo(82));
                UIVertex left = default, right = default;
                float previousDistance = -1f;
                for (int i = 0; i < vertices.currentVertCount; i += 2)
                {
                    vertices.PopulateUIVertex(ref left, i);
                    vertices.PopulateUIVertex(ref right, i + 1);
                    Assert.That(left.uv0.x, Is.GreaterThanOrEqualTo(previousDistance));
                    Assert.That(left.uv0.z, Is.EqualTo(canFlow ? 1f : 0f));
                    Assert.That(Vector3.Distance(left.position, right.position), Is.EqualTo(12f).Within(0.001f));
                    Vector2 center = (left.position + right.position) * 0.5f;
                    if (i == 0) Assert.That(Vector2.Distance(center, start), Is.LessThan(0.001f));
                    if (i == vertices.currentVertCount - 2)
                        Assert.That(Vector2.Distance(center, end), Is.LessThan(0.001f));
                    previousDistance = left.uv0.x;
                }
                Assert.That(previousDistance, Is.GreaterThan(Vector2.Distance(start, end)));
                Assert.That(curve.raycastTarget, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
    }

    /// <summary>저장 데이터를 변경하지 않고 실제 UI의 표시 상태를 제공한다.</summary>
    public class SkillTreePreviewParty : DispatchProxy
    {
        public CharacterSkillTreeSO Tree;

        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            switch (targetMethod.Name)
            {
                case "GetSkillTree": return (CharacterActorType)args[0] == Tree.characterType ? Tree : null;
                case "get_PartyMemberDataSO": return AssetDatabase.LoadAssetAtPath<PartyMemberDataSO>(
                    "Assets/10.Datas/Party/PartyMemberData.asset");
                case "get_Roster": return new[] { CharacterActorType.H09, Tree.characterType };
                case "get_ActiveCharacterType": return Tree.characterType;
                case "GetLevel": return 18;
                case "GetAvailableSkillPoints": return 7;
                case "GetSkillNodeRank": return Rank((string)args[1]);
                case "HasSpentSkillNodes": return true;
                case "CanTakeSkillNode":
                    var node = Tree.FindNode((string)args[1]);
                    bool available = node.requiredNodeIds.All(id => Rank(id) > 0);
                    args[2] = available ? SkillNodeBlockReason.None : SkillNodeBlockReason.MissingPrerequisite;
                    return available;
            }
            Type result = targetMethod.ReturnType;
            return result == typeof(void) || !result.IsValueType ? null : Activator.CreateInstance(result);
        }

        private int Rank(string id) => Tree.FindNode(id).requiredNodeIds.Count == 0 ? 1 : 0;
    }
}
