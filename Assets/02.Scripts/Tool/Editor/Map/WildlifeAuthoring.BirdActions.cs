using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.World;
using UPlayGround.EditorTools;

namespace UPlayGround.Tool.Editor.Map
{
    /// <summary>작은 새의 고개 흔들기·쪼기·웅크렸다 뛰기 클립을 뼈대에서 직접 계산해 만든다.</summary>
    public static partial class WildlifeAuthoring
    {
        private const string BirdActionClipRoot = DataRoot + "/Clips/Songbird_";
        private const string BirdArmatureRoot = "Armature/Root";
        private const string BirdBodyPath = BirdArmatureRoot + "/Body";
        private const string BirdNeck1Path = BirdBodyPath + "/Neck1";
        private const string BirdNeck2Path = BirdNeck1Path + "/Neck2";
        private const string BirdNeck3Path = BirdNeck2Path + "/Neck3";
        private const string BirdHeadPath = BirdNeck3Path + "/Head";
        private const string BirdBeakPath = BirdHeadPath + "/Beak";
        private const string BirdHipLPath = BirdBodyPath + "/Hip.L";
        private const string BirdHipRPath = BirdBodyPath + "/Hip.R";
        private const string BirdFootLPath = BirdHipLPath + "/Thigh.L/Shin.L/Foot.L";
        private const string BirdFootRPath = BirdHipRPath + "/Thigh.R/Shin.R/Foot.R";
        private const float AxisProbeDegrees = 10f;
        private const float MaxFootDrift = 0.003f;

        /// <summary>한 시점의 자세. 목 각도는 아래로 숙이는 쪽이 양수, 요는 오른쪽이 양수다.</summary>
        private readonly struct BirdPoseKey
        {
            public readonly float Time, BodyPitch, NeckPitch, HeadYaw;
            public readonly bool IsGrounded;

            public BirdPoseKey(float time, float bodyPitch, float neckPitch, float headYaw, bool isGrounded = true)
            {
                Time = time; BodyPitch = bodyPitch; NeckPitch = neckPitch; HeadYaw = headYaw; IsGrounded = isGrounded;
            }
        }

        /// <summary>뼈 하나가 자세 값을 얼마나 나눠 받는지와 모델 고유의 회전축.</summary>
        private sealed class BirdDrivenBone
        {
            public string Path;
            public Transform Bone;
            public Quaternion BaseRotation;
            public Vector3 PitchAxis, YawAxis;
            public float PitchWeight, YawWeight;
        }

        private sealed class BirdActionRig
        {
            public GameObject Root;
            public AnimationClip Idle;
            public Transform Beak, ArmatureRoot, Body, HipL, HipR, FootL, FootR;
            public Vector3 ArmatureRootBasePosition, FeetBaseCenter;
            public float BeakBaseHeight;
            public Quaternion HipLBase, HipRBase;
            public BirdDrivenBone BodyBone;
            public readonly List<BirdDrivenBone> Neck = new();
        }

        // 고개 흔들기: 머리를 고정한 채 있다가 빠르게 돌리는 hold-thrust 리듬을 따른다.
        private static readonly BirdPoseKey[] BirdLookKeys =
        {
            new(0, 0, 0, 0), new(0.5f, 0, 0, 0), new(0.62f, 0, 5, 40), new(1.3f, 0, 5, 40),
            new(1.42f, 0, -3, -35), new(2.1f, 0, -3, -35), new(2.22f, 0, 0, 0), new(2.7f, 0, 0, 0),
            new(2.78f, 0, 6, 14), new(2.88f, 0, 0, 0), new(3.6f, 0, 0, 0),
        };

        // 쪼기: 몸을 살짝 기울이고 머리를 빠르게 내려 찍은 뒤 주위를 살피는 3회 반복.
        private static readonly BirdPoseKey[] BirdPeckKeys =
        {
            new(0, 0, 0, 0), new(0.35f, 0, 0, 0), new(0.45f, 10, 55, 0), new(0.52f, 10, 55, 0),
            new(0.68f, 2, 8, 0), new(0.9f, 2, 8, 0), new(1.0f, 10, 55, 0), new(1.07f, 10, 55, 0),
            new(1.22f, 1, 5, 10), new(1.5f, 1, 5, 10), new(1.58f, 10, 55, -8), new(1.64f, 10, 55, -8),
            new(1.8f, 0, 0, 0), new(2.4f, 0, 0, 0),
        };

        // 호핑: 0.14초 웅크림 → 0.22초 도약 → 착지 흡수. 시간 길이는 프로필의 HopWindupSeconds·HopSeconds와 맞춘다.
        private static readonly BirdPoseKey[] BirdHopKeys =
        {
            new(0, 0, 0, 0), new(0.14f, 16, 12, 0), new(0.2f, 16, 12, 0),
            new(0.28f, -12, -14, 0, false), new(0.36f, -6, -8, 0, false),
            new(0.38f, 8, 6, 0), new(0.55f, 2, 2, 0), new(0.7f, 0, 0, 0),
        };

        /// <summary>고개 흔들기·쪼기·호핑 클립을 만들고 Songbird 프로필에 연결한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/환경 생물/새 동작 클립 생성")]
        public static void BuildSongbirdActionClips()
        {
            if (Application.isPlaying) throw new InvalidOperationException("편집 모드에서 클립을 만들어야 합니다.");
            var idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(BirdActionClipRoot + "Idle.anim");
            var profile = AssetDatabase.LoadAssetAtPath<WildlifeProfileSO>(DataRoot + "/Songbird.asset");
            if (idle == null || profile == null) throw new InvalidOperationException("Songbird 프로필 또는 Idle 클립이 없습니다.");
            var root = PrefabUtility.LoadPrefabContents(PrefabRoot + "/Songbird.prefab");
            try
            {
                var rig = CreateBirdActionRig(root, idle);
                profile.Observe = SaveBirdActionClip("Look", BuildBirdActionClip(rig, "Look", BirdLookKeys, true));
                profile.Feed = SaveBirdActionClip("Peck", BuildBirdActionClip(rig, "Peck", BirdPeckKeys, true));
                profile.Hop = SaveBirdActionClip("Hop", BuildBirdActionClip(rig, "Hop", BirdHopKeys, false));
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssets();
                Debug.Log("새 동작 클립(Look/Peck/Hop)을 만들어 Songbird 프로필에 연결했습니다.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static BirdActionRig CreateBirdActionRig(GameObject root, AnimationClip idle)
        {
            var rig = new BirdActionRig { Root = root, Idle = idle };
            idle.SampleAnimation(root, 0);
            rig.Beak = FindBirdBone(root, BirdBeakPath);
            rig.ArmatureRoot = FindBirdBone(root, BirdArmatureRoot);
            rig.Body = FindBirdBone(root, BirdBodyPath);
            rig.HipL = FindBirdBone(root, BirdHipLPath);
            rig.HipR = FindBirdBone(root, BirdHipRPath);
            rig.FootL = FindBirdBone(root, BirdFootLPath);
            rig.FootR = FindBirdBone(root, BirdFootRPath);
            rig.ArmatureRootBasePosition = rig.ArmatureRoot.localPosition;
            rig.HipLBase = rig.HipL.localRotation;
            rig.HipRBase = rig.HipR.localRotation;
            rig.FeetBaseCenter = GetBirdFeetCenter(rig);
            rig.BeakBaseHeight = root.transform.InverseTransformPoint(rig.Beak.position).y;
            rig.BodyBone = CreateDrivenBone(rig, BirdBodyPath, 1, 0);
            rig.Neck.Add(CreateDrivenBone(rig, BirdNeck1Path, 0.15f, 0));
            rig.Neck.Add(CreateDrivenBone(rig, BirdNeck2Path, 0.3f, 0.25f));
            rig.Neck.Add(CreateDrivenBone(rig, BirdNeck3Path, 0.3f, 0.3f));
            rig.Neck.Add(CreateDrivenBone(rig, BirdHeadPath, 0.25f, 0.45f));
            return rig;
        }

        private static Transform FindBirdBone(GameObject root, string path)
        {
            var bone = root.transform.Find(path);
            if (bone == null) throw new InvalidOperationException("새 뼈대에 " + path + " 가 없습니다.");
            return bone;
        }

        private static BirdDrivenBone CreateDrivenBone(BirdActionRig rig, string path, float pitchWeight, float yawWeight)
        {
            var driven = new BirdDrivenBone
            {
                Path = path, Bone = FindBirdBone(rig.Root, path), PitchWeight = pitchWeight, YawWeight = yawWeight,
            };
            driven.BaseRotation = driven.Bone.localRotation;
            DetectBirdAxes(rig, driven);
            return driven;
        }

        /// <summary>뼈마다 로컬 축이 달라, 부리 끝이 아래로 가는 축(피치)과 옆으로 가는 축(요)을 실측해 고른다.</summary>
        private static void DetectBirdAxes(BirdActionRig rig, BirdDrivenBone driven)
        {
            Vector3[] axes = { Vector3.right, Vector3.up, Vector3.forward };
            Vector3 baseTip = rig.Root.transform.InverseTransformPoint(rig.Beak.position);
            float bestPitch = float.MinValue;
            var pitchAxis = Vector3.right;
            var tipOffsets = new Vector3[axes.Length * 2];
            for (int i = 0; i < tipOffsets.Length; i++)
            {
                Vector3 axis = axes[i / 2] * (i % 2 == 0 ? 1 : -1);
                driven.Bone.localRotation = driven.BaseRotation * Quaternion.AngleAxis(AxisProbeDegrees, axis);
                tipOffsets[i] = rig.Root.transform.InverseTransformPoint(rig.Beak.position) - baseTip;
                if (-tipOffsets[i].y > bestPitch) { bestPitch = -tipOffsets[i].y; pitchAxis = axis; }
            }
            float bestYaw = 0;
            var yawAxis = Vector3.up;
            for (int i = 0; i < tipOffsets.Length; i++)
            {
                Vector3 axis = axes[i / 2] * (i % 2 == 0 ? 1 : -1);
                if (Mathf.Abs(Vector3.Dot(axis, pitchAxis)) > 0.5f) continue;
                if (tipOffsets[i].x > bestYaw) { bestYaw = tipOffsets[i].x; yawAxis = axis; }
            }
            driven.Bone.localRotation = driven.BaseRotation;
            if (bestPitch <= 0.0005f) throw new InvalidOperationException(driven.Path + ": 부리를 아래로 움직이는 회전축을 찾지 못했습니다.");
            driven.PitchAxis = pitchAxis;
            driven.YawAxis = yawAxis;
        }

        private static AnimationClip BuildBirdActionClip(BirdActionRig rig, string name, BirdPoseKey[] keys, bool loop)
        {
            float length = keys[keys.Length - 1].Time;
            var clip = new AnimationClip { name = "Songbird_" + name, frameRate = 30 };
            // 사용하지 않는 뼈는 Idle 첫 자세로 고정해 전환 시 다른 클립의 값이 남지 않게 한다.
            foreach (var binding in AnimationUtility.GetCurveBindings(rig.Idle))
            {
                float value = AnimationUtility.GetEditorCurve(rig.Idle, binding).Evaluate(0);
                AnimationUtility.SetEditorCurve(clip, binding,
                    new AnimationCurve(new Keyframe(0, value), new Keyframe(length, value)));
            }
            var rotations = new Dictionary<string, Quaternion[]>();
            var rootPositions = new Vector3[keys.Length];
            var driven = new List<BirdDrivenBone> { rig.BodyBone };
            driven.AddRange(rig.Neck);
            foreach (var bone in driven) rotations[bone.Path] = new Quaternion[keys.Length];
            rotations[BirdHipLPath] = new Quaternion[keys.Length];
            rotations[BirdHipRPath] = new Quaternion[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                ApplyBirdPose(rig, keys[i]);
                foreach (var bone in driven) rotations[bone.Path][i] = bone.Bone.localRotation;
                rotations[BirdHipLPath][i] = rig.HipL.localRotation;
                rotations[BirdHipRPath][i] = rig.HipR.localRotation;
                rootPositions[i] = rig.ArmatureRoot.localPosition;
                ValidateBirdPose(rig, name, keys[i]);
            }
            foreach (var pair in rotations) WriteBirdRotationCurves(clip, pair.Key, keys, pair.Value);
            WriteBirdPositionCurves(clip, BirdArmatureRoot, keys, rootPositions);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            settings.startTime = 0;
            settings.stopTime = length;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            rig.Idle.SampleAnimation(rig.Root, 0);
            return clip;
        }

        /// <summary>자세를 뼈대에 적용한다. 몸을 기울이면 다리가 딸려가므로 엉덩이를 반대로 돌리고 발 중심을 제자리로 되돌린다.</summary>
        private static void ApplyBirdPose(BirdActionRig rig, BirdPoseKey key)
        {
            rig.Idle.SampleAnimation(rig.Root, 0);
            rig.ArmatureRoot.localPosition = rig.ArmatureRootBasePosition;
            var bodyDelta = Quaternion.AngleAxis(key.BodyPitch, rig.BodyBone.PitchAxis);
            rig.BodyBone.Bone.localRotation = rig.BodyBone.BaseRotation * bodyDelta;
            rig.HipL.localRotation = Quaternion.Inverse(bodyDelta) * rig.HipLBase;
            rig.HipR.localRotation = Quaternion.Inverse(bodyDelta) * rig.HipRBase;
            foreach (var bone in rig.Neck)
            {
                bone.Bone.localRotation = bone.BaseRotation
                    * Quaternion.AngleAxis(key.HeadYaw * bone.YawWeight, bone.YawAxis)
                    * Quaternion.AngleAxis(key.NeckPitch * bone.PitchWeight, bone.PitchAxis);
            }
            if (!key.IsGrounded) return;
            rig.ArmatureRoot.position += rig.FeetBaseCenter - GetBirdFeetCenter(rig);
        }

        private static Vector3 GetBirdFeetCenter(BirdActionRig rig) =>
            (rig.FootL.position + rig.FootR.position) * 0.5f;

        private static void ValidateBirdPose(BirdActionRig rig, string clipName, BirdPoseKey key)
        {
            if (key.IsGrounded && (GetBirdFeetCenter(rig) - rig.FeetBaseCenter).magnitude > MaxFootDrift)
                throw new InvalidOperationException(clipName + " " + key.Time + "초: 발이 제자리에서 벗어났습니다.");
            if (key.NeckPitch < 40) return;
            float beakDrop = rig.BeakBaseHeight - rig.Root.transform.InverseTransformPoint(rig.Beak.position).y;
            Debug.Log(clipName + " " + key.Time + "초 부리 하강 " + beakDrop.ToString("F4"));
            if (beakDrop <= 0.001f) throw new InvalidOperationException(clipName + " " + key.Time + "초: 고개가 숙여지지 않았습니다.");
        }

        private static void WriteBirdRotationCurves(AnimationClip clip, string path, BirdPoseKey[] keys, Quaternion[] values)
        {
            string[] components = { "x", "y", "z", "w" };
            var curves = new AnimationCurve[4];
            for (int c = 0; c < 4; c++) curves[c] = new AnimationCurve();
            Quaternion previous = values[0];
            for (int i = 0; i < keys.Length; i++)
            {
                Quaternion value = values[i];
                // 이웃 키와 반구를 맞춰 보간이 반대 방향으로 돌지 않게 한다.
                if (Quaternion.Dot(previous, value) < 0) value = new Quaternion(-value.x, -value.y, -value.z, -value.w);
                previous = value;
                curves[0].AddKey(new Keyframe(keys[i].Time, value.x, 0, 0));
                curves[1].AddKey(new Keyframe(keys[i].Time, value.y, 0, 0));
                curves[2].AddKey(new Keyframe(keys[i].Time, value.z, 0, 0));
                curves[3].AddKey(new Keyframe(keys[i].Time, value.w, 0, 0));
            }
            for (int c = 0; c < 4; c++)
                AnimationUtility.SetEditorCurve(clip,
                    EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation." + components[c]), curves[c]);
        }

        private static void WriteBirdPositionCurves(AnimationClip clip, string path, BirdPoseKey[] keys, Vector3[] values)
        {
            string[] components = { "x", "y", "z" };
            for (int c = 0; c < 3; c++)
            {
                var curve = new AnimationCurve();
                for (int i = 0; i < keys.Length; i++)
                    curve.AddKey(new Keyframe(keys[i].Time, values[i][c], 0, 0));
                AnimationUtility.SetEditorCurve(clip,
                    EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition." + components[c]), curve);
            }
        }

        /// <summary>기존 에셋이 있으면 GUID를 보존한 채 내용만 교체한다.</summary>
        private static AnimationClip SaveBirdActionClip(string name, AnimationClip generated)
        {
            string path = BirdActionClipRoot + name + ".anim";
            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(generated, path);
                return generated;
            }
            EditorUtility.CopySerialized(generated, existing);
            existing.name = "Songbird_" + name;
            EditorUtility.SetDirty(existing);
            return existing;
        }
    }
}
