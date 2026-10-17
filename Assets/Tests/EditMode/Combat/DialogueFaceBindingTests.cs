using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Dialogue;

namespace UPlayGround.Combat.Tests
{
    public sealed class DialogueFaceBindingTests
    {
        [TestCase("Arin")]
        [TestCase("H09")]
        [TestCase("Hwarin")]
        [TestCase("Lian")]
        [TestCase("Lili")]
        [TestCase("Myomyo")]
        [TestCase("MyoRyeong")]
        [TestCase("Raon")]
        [TestCase("Reine")]
        [TestCase("SeolA")]
        [TestCase("Sera")]
        [TestCase("YeonHoa")]
        [TestCase("Yura")]
        public void PlayerModelHasValidIdleBlinkBinding(string character)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/03.Prefabs/Actor/Player/Models/PlayerModel_{character}.prefab");
            Assert.NotNull(model, $"{character}: 플레이어 모델이 없다.");
            var data = model.GetComponent<CharacterModelData>();
            Assert.NotNull(data, $"{character}: CharacterModelData가 없다.");
            var face = data.dialogueFace;
            Assert.NotNull(face, $"{character}: 얼굴 바인딩이 없다.");
            Assert.NotNull(face.profile, $"{character}: 눈 깜빡임 프로필을 연결해야 한다.");
            Assert.NotNull(face.head, $"{character}: 머리 본을 연결해야 한다.");
            Assert.NotNull(face.eyelids, $"{character}: 눈꺼풀 메시를 연결해야 한다.");
            Assert.NotNull(face.eyelids.sharedMesh);
            var mesh = face.eyelids.sharedMesh;
            Assert.GreaterOrEqual(mesh.GetBlendShapeIndex(face.blinkShape), 0,
                $"{character}: {face.blinkShape} 눈 깜빡임 채널이 없다.");
            foreach (string expression in face.expressionShapes)
                Assert.GreaterOrEqual(mesh.GetBlendShapeIndex(expression), 0,
                    $"{character}: {expression} 표정 우선 채널이 없다.");
        }

        [Test]
        public void IdleBlinkWithoutLookTargetFreezesWithZeroDeltaAndClearsOnExit()
        {
            var root = new GameObject("root");
            var head = new GameObject("head");
            var profile = ScriptableObject.CreateInstance<DialogueFaceProfileSO>();
            var mesh = new Mesh();
            try
            {
                head.transform.SetParent(root.transform);
                head.transform.localRotation = Quaternion.Euler(0, 17, 0);
                Quaternion originalRotation = head.transform.localRotation;
                mesh.vertices = new[] { Vector3.zero };
                mesh.AddBlendShapeFrame("blink", 100, new[] { Vector3.up }, new Vector3[1], new Vector3[1]);
                var renderer = root.AddComponent<SkinnedMeshRenderer>();
                renderer.sharedMesh = mesh;
                profile.blinkInterval = new Vector2(0.01f, 0.01f);
                profile.blinkDuration = 0.2f;
                var face = new DialogueFaceBinding
                {
                    head = head.transform, eyelids = renderer, blinkShape = "blink", profile = profile
                };
                face.Initialize(root.transform);
                face.Apply(null, false, true, false, 0.02f);
                face.Restore();
                face.Apply(null, false, true, false, 0.05f);
                float weight = renderer.GetBlendShapeWeight(0);
                Assert.Greater(weight, 0f, "대화 상대 없이도 Idle 눈 깜빡임이 실행되어야 한다.");
                Assert.Less(Quaternion.Angle(originalRotation, head.transform.localRotation), 0.01f);

                face.Restore();
                face.Apply(null, false, true, false, 0f);
                Assert.AreEqual(weight, renderer.GetBlendShapeWeight(0), 0.001f,
                    "게임 시간이 멈추면 눈 깜빡임도 같은 위치에 머물러야 한다.");
                face.Clear();
                Assert.AreEqual(0f, renderer.GetBlendShapeWeight(0),
                    "Idle 종료 시 눈꺼풀 보정이 남으면 안 된다.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(head);
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void HwarinModelHasValidHeadEyesAndBlinkChannel()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Actor/Player/Models/PlayerModel_Hwarin.prefab");
            var face = model.GetComponent<CharacterModelData>().dialogueFace;
            Assert.NotNull(face.profile);
            Assert.NotNull(face.head);
            Assert.NotNull(face.leftEye);
            Assert.NotNull(face.rightEye);
            Assert.NotNull(face.eyelids);
            Assert.GreaterOrEqual(face.eyelids.sharedMesh.GetBlendShapeIndex(face.blinkShape), 0);
        }

        [TestCase("Assets/03.Prefabs/Actor/Monster/Humanoid/MonsterActor_Hwarin_2Handed.prefab")]
        [TestCase("Assets/03.Prefabs/Actor/Monster/MonsterActor_Hwarin.prefab")]
        public void HwarinStandInHasFaceBinding(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var actor = prefab.GetComponent<MonsterActor>();
            Assert.IsInstanceOf<IDialogueFaceActor>(actor);
            var serialized = new SerializedObject(actor);
            var binding = serialized.FindProperty("_dialogueFace");
            Assert.NotNull(binding.FindPropertyRelative("profile").objectReferenceValue);
            Assert.NotNull(binding.FindPropertyRelative("head").objectReferenceValue);
            var renderer = (SkinnedMeshRenderer)binding.FindPropertyRelative("eyelids").objectReferenceValue;
            Assert.GreaterOrEqual(renderer.sharedMesh.GetBlendShapeIndex("Eye_Blink"), 0);
        }

        [TestCase(0f)]
        [TestCase(90f)]
        public void EyeDirectionUsesBindPoseInsteadOfAuthoredEyeRotation(float meshAxisRotation)
        {
            var root = new GameObject("root");
            var target = new GameObject("target");
            var head = new GameObject("head");
            var left = new GameObject("left");
            var right = new GameObject("right");
            var profile = ScriptableObject.CreateInstance<DialogueFaceProfileSO>();
            var mesh = new Mesh();
            try
            {
                head.transform.SetParent(root.transform);
                left.transform.SetParent(head.transform);
                right.transform.SetParent(head.transform);
                var renderer = root.AddComponent<SkinnedMeshRenderer>();
                renderer.bones = new[] { head.transform, left.transform, right.transform };
                var meshToBone = Matrix4x4.Rotate(Quaternion.Euler(meshAxisRotation, 0, 0));
                mesh.bindposes = new[] { meshToBone, meshToBone, meshToBone };
                renderer.sharedMesh = mesh;
                left.transform.localRotation = right.transform.localRotation = Quaternion.Euler(0, 17, 0);
                var face = new DialogueFaceBinding { head = head.transform, leftEye = left.transform,
                    rightEye = right.transform, eyelids = renderer, profile = profile };
                root.transform.rotation = Quaternion.Euler(0, 63, 0);
                face.Initialize(root.transform);
                target.transform.position = root.transform.forward * 3;
                for (int i = 0; i < 120; i++)
                {
                    face.Restore();
                    face.Apply(target.transform, false, false, true, 1f / 60);
                }
                Assert.Less(Vector3.Angle(head.transform.forward, root.transform.forward), 0.1f,
                    "메시 임포트 축 때문에 머리 방향이 바뀌면 안 된다.");
                Assert.Less(Vector3.Angle(left.transform.forward, root.transform.forward), 0.1f);
                Assert.Less(Vector3.Angle(right.transform.forward, root.transform.forward), 0.1f);
                face.Clear();
                Assert.Less(Quaternion.Angle(left.transform.localRotation, Quaternion.Euler(0, 17, 0)), 0.1f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void RestoreDoesNotEraseExternalExpressionAndClearReleasesOwnPose()
        {
            var root = new GameObject("root");
            var target = new GameObject("target");
            var head = new GameObject("head");
            var profile = ScriptableObject.CreateInstance<DialogueFaceProfileSO>();
            var mesh = new Mesh();
            try
            {
                head.transform.SetParent(root.transform);
                mesh.vertices = new[] { Vector3.zero };
                mesh.AddBlendShapeFrame("blink", 100, new[] { Vector3.up }, new Vector3[1], new Vector3[1]);
                var renderer = root.AddComponent<SkinnedMeshRenderer>();
                renderer.sharedMesh = mesh;
                profile.blinkInterval = new Vector2(0.01f, 0.01f);
                var face = new DialogueFaceBinding { head = head.transform, eyelids = renderer, blinkShape = "blink", profile = profile };
                face.Initialize(root.transform);
                target.transform.position = new Vector3(1, 0, 3);
                face.Apply(target.transform, false, false, false, 0.02f);
                face.Restore();
                face.Apply(target.transform, false, false, false, 0.04f);
                Assert.Greater(renderer.GetBlendShapeWeight(0), 0);
                renderer.SetBlendShapeWeight(0, 73);
                face.Clear();
                Assert.AreEqual(73, renderer.GetBlendShapeWeight(0));
                Assert.Less(Quaternion.Angle(Quaternion.identity, head.transform.localRotation), 0.01f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(head);
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(mesh);
            }
        }
    }
}
