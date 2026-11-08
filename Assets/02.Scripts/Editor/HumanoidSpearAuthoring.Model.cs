using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Combat;
using UPlayGround.Data.Actor;

namespace UPlayGround.Editor
{
    /// <summary>원본 창 리그와 실제 메시를 사용하는 독립 일반 창병을 저작한다.</summary>
    public static partial class HumanoidSpearAuthoring
    {
        public const string SourceDefinitionPath = "Assets/10.Datas/Actor/DataBase/Enemy_Random_M_GreatSword_001_ActorDef.asset";
        public const string SourceRigPath = "Assets/ExternalAssets/AnimationOnly/Frank_Slash_Pack/Assets/Meshes/Frank_RPG_Spear_Unity.FBX";
        public const string OutputRoot = "Assets/10.Datas/Actor/HumanoidSpear";
        public const string ClipRoot = "Assets/07.Animations/Monster/FieldSpear";
        public const string PrefabPath = "Assets/03.Prefabs/Actor/Monster/Humanoid/MonsterActor_Field_Spear_001.prefab";
        public const string DefinitionPath = OutputRoot + "/Enemy_Field_Spear_001.asset";
        public const string SetPath = "Assets/10.Datas/Ability/Actor/GradeVariation/Spear/AbilitySet_Spear_Normal.asset";
        public const string RecipePath = OutputRoot + "/SpearRecipe.json";
        private const string LogRoot = "Logs/HumanoidSpear";

        private static T Load<T>(string path) where T : UnityEngine.Object =>
            AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("원본 누락: " + path);

        private static GameObject CreateModel()
        {
            var definition = Load<ActorDefinitionSO>(SourceDefinitionPath);
            var root = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(definition.prefab));
            try
            {
                root.name = "MonsterActor_Field_Spear_001";
                root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                var animator = root.GetComponentInChildren<Animator>(true);
                var original = Load<GameObject>(SourceRigPath);
                var sourceHand = original.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.RightHand);
                var targetHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                GetHandConversion(root, out var alignment, out float scale);
                if (targetHand.Find("R_Hand_Weapon") != null) throw new InvalidOperationException("무기 추가 본 경로 충돌");
                var rig = UnityEngine.Object.Instantiate(sourceHand.Find("R_Hand_Weapon").gameObject, targetHand, false);
                rig.name = "R_Hand_Weapon";
                rig.transform.localPosition = alignment * rig.transform.localPosition * scale;
                rig.transform.localRotation = alignment * rig.transform.localRotation;
                rig.transform.localScale *= scale;
                var weaponRoot = root.transform.Find("Weapon");
                if (weaponRoot == null) throw new InvalidOperationException("원본 장비 루트 누락");
                foreach (Transform child in weaponRoot) child.gameObject.SetActive(false);
                foreach (var oldHit in root.GetComponentsInChildren<CombatHitbox>(true).Where(h => h.GroupId == "MainWeapon").ToArray())
                    UnityEngine.Object.DestroyImmediate(oldHit.gameObject);
                var sourceRenderer = original.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r => r.name == "Weapon_Spear");
                var weapon = UnityEngine.Object.Instantiate(sourceRenderer.gameObject, root.transform, false);
                weapon.name = "Weapon_Spear";
                var renderer = weapon.GetComponent<SkinnedMeshRenderer>();
                renderer.bones = sourceRenderer.bones.Select(b => rig.transform.Find(RelativePath(b, sourceHand.Find("R_Hand_Weapon")))).ToArray();
                if (renderer.bones.Any(b => b == null)) throw new InvalidOperationException("스킨 무기 본 매핑 누락");
                renderer.rootBone = rig.transform.Find("Spear_Root");
                renderer.updateWhenOffscreen = true;
                AddHitboxes(renderer);
                return root;
            }
            catch { PrefabUtility.UnloadPrefabContents(root); throw; }
        }

        private static string RelativePath(Transform child, Transform parent) => AnimationUtility.CalculateTransformPath(child, parent);

        private static void AddHitboxes(SkinnedMeshRenderer renderer)
        {
            var mesh = renderer.sharedMesh;
            var vertices = mesh.vertices;
            var weights = mesh.boneWeights;
            for (int bone = 0; bone < renderer.bones.Length; bone++)
            {
                var points = new List<Vector3>();
                for (int i = 0; i < vertices.Length; i++)
                    if (weights[i].boneIndex0 == bone && weights[i].weight0 > 0.5f)
                        points.Add(mesh.bindposes[bone].MultiplyPoint3x4(vertices[i]));
                if (points.Count == 0) throw new InvalidOperationException("무기 형상 근거 누락");
                var bounds = new Bounds(points[0], Vector3.zero);
                foreach (var point in points) bounds.Encapsulate(point);
                var hit = new GameObject("[HitBox] MainWeapon");
                hit.transform.SetParent(renderer.bones[bone], false);
                var shape = hit.AddComponent<BoxCollider>();
                shape.center = bounds.center;
                shape.size = bounds.size;
                hit.AddComponent<CombatHitbox>().Configure("MainWeapon", shape, true, 0.15f, 8);
            }
        }

        private static AnimationClip CreateClip(AnimationClip source, GameObject target)
        {
            var copy = UnityEngine.Object.Instantiate(source);
            copy.name = "Spear_" + System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(source)) + "_" + source.name;
            string sourcePath = AssetDatabase.GetAssetPath(source);
            var preview = AssetDatabase.LoadAllAssetsAtPath(sourcePath).OfType<AnimationClip>().FirstOrDefault(c => c.name.StartsWith("__preview__"));
            string targetHand = RelativePath(target.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.RightHand), target.transform);
            const string marker = "/R_Hand_Weapon";
            GetHandConversion(target, out var alignment, out float scale);
            bool hasWeaponCurves = preview != null && AnimationUtility.GetCurveBindings(preview).Any(b => b.path.Contains(marker + "/Spear_Root"));
            if (hasWeaponCurves)
            {
                foreach (var binding in AnimationUtility.GetCurveBindings(preview).Where(b => b.path.Contains(marker)))
                {
                    // 원본 클립의 미사용 Slash FX 보조 경로는 실제 창 스킨 리그에 포함되지 않는다.
                    if (binding.path.Contains("/R_Slash_FX_Bone")) continue;
                    var destination = binding;
                    destination.path = targetHand + binding.path.Substring(binding.path.IndexOf(marker, StringComparison.Ordinal));
                    if (target.transform.Find(destination.path) == null) throw new InvalidOperationException("창 추가 본 경로 누락: " + destination.path);
                    AnimationUtility.SetEditorCurve(copy, destination, AnimationUtility.GetEditorCurve(preview, binding));
                }
                string originalPath = AnimationUtility.GetCurveBindings(preview).First(b => b.path.EndsWith(marker)).path;
                string targetPath = targetHand + marker;
                ConvertCurves(preview, copy, originalPath, targetPath, "m_LocalPosition", 3, v => alignment * new Vector3(v.x, v.y, v.z) * scale);
                ConvertCurves(preview, copy, originalPath, targetPath, "m_LocalRotation", 4, v => { var q = alignment * new Quaternion(v.x, v.y, v.z, v.w); return new Vector4(q.x, q.y, q.z, q.w); });
                ConvertCurves(preview, copy, originalPath, targetPath, "m_LocalScale", 3, v => v * scale);
            }
            else
            {
                // 다른 무기 커브가 없는 상태 모션에서도 직전 공격의 추가 본 자세를 남기지 않는다.
                var original = Load<GameObject>(SourceRigPath);
                var sourceRig = original.GetComponentsInChildren<Transform>(true).Single(t => t.name == "R_Hand_Weapon");
                foreach (var bone in sourceRig.GetComponentsInChildren<Transform>(true))
                {
                    string path = targetHand + "/R_Hand_Weapon" + (bone == sourceRig ? "" : "/" + RelativePath(bone, sourceRig));
                    var position = bone == sourceRig ? alignment * bone.localPosition * scale : bone.localPosition;
                    var rotation = bone == sourceRig ? alignment * bone.localRotation : bone.localRotation;
                    var size = bone == sourceRig ? bone.localScale * scale : bone.localScale;
                    var values = new[] { position.x, position.y, position.z, rotation.x, rotation.y, rotation.z, rotation.w, size.x, size.y, size.z };
                    var properties = new[] { "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z", "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w", "m_LocalScale.x", "m_LocalScale.y", "m_LocalScale.z" };
                    for (int i = 0; i < values.Length; i++)
                        AnimationUtility.SetEditorCurve(copy, EditorCurveBinding.FloatCurve(path, typeof(Transform), properties[i]), AnimationCurve.Constant(0, source.length, values[i]));
                }
            }
            copy.EnsureQuaternionContinuity();
            TrimWeaponCurves(copy, source.length);
            foreach (var binding in AnimationUtility.GetCurveBindings(source).Where(b => b.type == typeof(Animator)))
                AnimationUtility.SetEditorCurve(copy, binding, AnimationUtility.GetEditorCurve(source, binding));
            AnimationUtility.SetAnimationClipSettings(copy, AnimationUtility.GetAnimationClipSettings(source));
            return copy;
        }

        private static void TrimWeaponCurves(AnimationClip clip, float duration)
        {
            foreach (var binding in AnimationUtility.GetCurveBindings(clip).Where(b => b.type == typeof(Transform) && b.path.Contains("/R_Hand_Weapon")))
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                var keys = curve.keys;
                if (keys.Length == 0 || keys[keys.Length - 1].time <= duration) continue;
                if (keys.Any(k => k.weightedMode != WeightedMode.None)) throw new InvalidOperationException("가중 곡선 끝점 분할 검수 필요");
                var retained = keys.Where(k => k.time <= duration).ToList();
                if (retained.Count == 0) throw new InvalidOperationException("클립 구간 내 추가 본 키 없음");
                if (retained[retained.Count - 1].time < duration)
                {
                    var a = retained[retained.Count - 1];
                    var b = keys.First(k => k.time > duration);
                    float dt = b.time - a.time;
                    float u = (duration - a.time) / dt;
                    float tangent = float.IsInfinity(a.outTangent) || float.IsInfinity(b.inTangent) ? 0f :
                        ((6*u*u - 6*u)*a.value + (3*u*u - 4*u + 1)*dt*a.outTangent + (-6*u*u + 6*u)*b.value + (3*u*u - 2*u)*dt*b.inTangent) / dt;
                    // Hermite 구간을 같은 값·미분의 끝점에서 나눠 사용 구간의 궤적을 보존한다.
                    retained.Add(new Keyframe(duration, curve.Evaluate(duration), tangent, tangent));
                }
                curve.keys = retained.ToArray();
                AnimationUtility.SetEditorCurve(clip, binding, curve);
            }
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.stopTime = settings.startTime + duration;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
        }

        private static Quaternion? _handAlignment;
        private static float _weaponScale;

        private static void GetHandConversion(GameObject target, out Quaternion alignment, out float scale)
        {
            if (_handAlignment.HasValue) { alignment = _handAlignment.Value; scale = _weaponScale; return; }
            var source = UnityEngine.Object.Instantiate(Load<GameObject>(SourceRigPath));
            try
            {
                source.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                var a = source.GetComponent<Animator>();
                var b = target.GetComponent<Animator>();
                using (var sa = new HumanPoseHandler(a.avatar, source.transform))
                using (var sb = new HumanPoseHandler(b.avatar, target.transform))
                {
                    var pa = new HumanPose(); var pb = new HumanPose();
                    sa.GetHumanPose(ref pa); sb.GetHumanPose(ref pb);
                    Array.Clear(pa.muscles, 0, pa.muscles.Length); Array.Clear(pb.muscles, 0, pb.muscles.Length);
                    pa.bodyRotation = pb.bodyRotation = Quaternion.identity;
                    sa.SetHumanPose(ref pa); sb.SetHumanPose(ref pb);
                    alignment = Quaternion.Inverse(b.GetBoneTransform(HumanBodyBones.RightHand).rotation) * a.GetBoneTransform(HumanBodyBones.RightHand).rotation;
                    scale = b.humanScale / a.humanScale;
                    _handAlignment = alignment; _weaponScale = scale;
                }
                b.Rebind();
            }
            finally { UnityEngine.Object.DestroyImmediate(source); }
        }

        private static void ConvertCurves(AnimationClip source, AnimationClip target, string sourcePath, string targetPath, string property, int count, Func<Vector4, Vector4> convert)
        {
            const string axes = "xyzw";
            var curves = Enumerable.Range(0, count).Select(i => AnimationUtility.GetEditorCurve(source, EditorCurveBinding.FloatCurve(sourcePath, typeof(Transform), property + "." + axes[i]))).ToArray();
            if (curves.Any(c => c == null)) throw new InvalidOperationException("추가 본 커브 채널 누락");
            var transformed = Enumerable.Range(0, count).Select(_ => new Keyframe[curves[0].length]).ToArray();
            for (int k = 0; k < curves[0].length; k++)
            {
                var value = Vector4.zero; var input = Vector4.zero; var output = Vector4.zero;
                for (int axis = 0; axis < count; axis++)
                {
                    if (curves[axis].length != curves[0].length || Mathf.Abs(curves[axis].keys[k].time - curves[0].keys[k].time) > 0.00001f)
                        throw new InvalidOperationException("추가 본 커브 시간축 불일치");
                    var key = curves[axis].keys[k]; value[axis] = key.value; input[axis] = key.inTangent; output[axis] = key.outTangent;
                }
                value = convert(value); input = convert(input); output = convert(output);
                for (int axis = 0; axis < count; axis++)
                {
                    var key = curves[axis].keys[k]; key.value = value[axis]; key.inTangent = input[axis]; key.outTangent = output[axis]; transformed[axis][k] = key;
                }
            }
            for (int axis = 0; axis < count; axis++)
                AnimationUtility.SetEditorCurve(target, EditorCurveBinding.FloatCurve(targetPath, typeof(Transform), property + "." + axes[axis]), new AnimationCurve(transformed[axis]) { preWrapMode = curves[axis].preWrapMode, postWrapMode = curves[axis].postWrapMode });
        }
    }
}
