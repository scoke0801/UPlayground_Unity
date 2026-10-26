using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.World;
using Object = UnityEngine.Object;

namespace UPlayGround.Content.Tests
{
    /// <summary>새의 접지 여유·장애물 검사와 외부 모델의 모션 연결을 검사한다.</summary>
    public sealed class WildlifeBirdTests
    {
        [Test]
        public void 발_원점의_착지_여유를_강제로_띄우지_않는다()
        {
            var profile = ScriptableObject.CreateInstance<WildlifeProfileSO>();
            try
            {
                profile.GroundClearance = 0.01f;
                typeof(WildlifeProfileSO).GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(profile, null);
                Assert.That(profile.GroundClearance, Is.EqualTo(0.01f));
            }
            finally { Object.DestroyImmediate(profile); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void 낮은_착지는_허용하고_실제_장애물은_막는다(bool hasWall)
        {
            var root = new GameObject("새 접지 검사");
            var floor = new GameObject("검사 지면", typeof(BoxCollider));
            var wall = new GameObject("검사 벽", typeof(BoxCollider));
            var profile = ScriptableObject.CreateInstance<WildlifeProfileSO>();
            try
            {
                Vector3 origin = new Vector3(-1000, 1000, -1000);
                floor.transform.position = origin + Vector3.down * 0.1f;
                floor.GetComponent<BoxCollider>().size = new Vector3(4, 0.2f, 4);
                wall.transform.position = origin + new Vector3(0, 0.5f, 0.3f);
                wall.GetComponent<BoxCollider>().size = new Vector3(2, 1, 0.1f);
                wall.SetActive(hasWall);
                root.transform.position = origin + Vector3.up * 0.2f;
                Physics.SyncTransforms();
                Type type = Type.GetType("UPlayGround.Gameplay.World.LivingWorld.WildlifeCreature, Assembly-CSharp", true);
                Component creature = root.AddComponent(type);
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                profile.IsFlying = profile.CanLand = true;
                type.GetField("_profile", flags).SetValue(creature, profile);
                Vector3 destination = origin + (hasWall ? new Vector3(0, 0.2f, 1) : Vector3.up * 0.01f);
                type.GetField("_target", flags).SetValue(creature, destination);
                Vector3 before = root.transform.position;
                type.GetMethod("MoveInAir", flags).Invoke(creature, new object[] { 1f });
                Assert.That(Vector3.Distance(root.transform.position, hasWall ? before : destination), Is.LessThan(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(floor);
                Object.DestroyImmediate(wall);
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void 새_클립의_본이_연결되고_접힌_날개의_폭이_비행보다_좁다()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Environment/Wildlife/Songbird.prefab");
            Assert.That(prefab, Is.Not.Null);
            var root = Object.Instantiate(prefab);
            var baked = new Mesh();
            try
            {
                var renderer = root.GetComponentInChildren<SkinnedMeshRenderer>();
                Assert.That(renderer.bones.Length, Is.EqualTo(renderer.sharedMesh.bindposes.Length));
                foreach (var bone in renderer.bones) Assert.That(bone, Is.Not.Null);
                float idleWidth = 0;
                foreach (string name in new[] { "Idle", "Fly" })
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/10.Datas/World/Wildlife/Clips/Songbird_" + name + ".anim");
                    Assert.That(clip, Is.Not.Null);
                    foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                        Assert.That(string.IsNullOrEmpty(binding.path) || root.transform.Find(binding.path) != null,
                            Is.True, name + ": " + binding.path);
                    // 비행의 중립 구간을 샘플링해 위로 접히는 순간의 폭과 혼동하지 않는다.
                    clip.SampleAnimation(root, name == "Fly" ? clip.length * 0.25f : 0);
                    int foldedShape = renderer.sharedMesh.GetBlendShapeIndex("Wings_Folded");
                    Assert.That(foldedShape, Is.GreaterThanOrEqualTo(0));
                    Assert.That(renderer.GetBlendShapeWeight(foldedShape), Is.EqualTo(name == "Idle" ? 100f : 0f).Within(0.01f), "날개 접기 모프");
                    // FBX의 단위 보정 스케일을 포함해 렌더러 로컬 좌표로 받은 뒤 월드로 변환한다.
                    renderer.BakeMesh(baked, true);
                    var bounds = new Bounds(root.transform.InverseTransformPoint(renderer.transform.TransformPoint(baked.vertices[0])), Vector3.zero);
                    foreach (var vertex in baked.vertices)
                        bounds.Encapsulate(root.transform.InverseTransformPoint(renderer.transform.TransformPoint(vertex)));
                    if (name == "Idle")
                    {
                        idleWidth = bounds.size.x;
                        Assert.That(bounds.min.y, Is.InRange(-0.005f, 0.015f), "발 원점");
                    }
                    else Assert.That(bounds.size.x, Is.GreaterThan(idleWidth * 1.5f), name);
                }
            }
            finally
            {
                Object.DestroyImmediate(baked);
                Object.DestroyImmediate(root);
            }
        }
    }
}
