using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Animancer;
using UnityEditor;
using UnityEngine;
using UPlayGround.Animation;

namespace UPlayGround.EditorTools
{
    /// <summary>창의 전신 높이만 접지 기준으로 옮겨 KCC가 도약 높이를 소유하게 한다.</summary>
    public static class WeaponSignatureSpearHeightAuthoring
    {
        private const string MotionPath = "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Signature/Spear_Signature.asset";
        private const string SourcePath = "Assets/07.Animations/Player/Signature/Spear/Spear_Signature.fbx";
        private const string OutputPath = "Assets/07.Animations/Player/Signature/Spear/Spear_Signature_GroundedBody.anim";
        private const float FootHeight = .08f;

        private sealed class Samples
        {
            public float Scale;
            public readonly List<float> Times = new();
            public readonly List<float> Feet = new();
            public readonly List<Vector3[]> Relative = new();
        }

        public static string Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed)
                throw new InvalidOperationException("정상 편집 모드에서만 보정합니다.");
            var motion = AssetDatabase.LoadAssetAtPath<MotionSetAsset>(MotionPath);
            var source = AssetDatabase.LoadAllAssetsAtPath(SourcePath).OfType<AnimationClip>().Single(c => !c.name.StartsWith("__"));
            if (motion == null || motion.motionSet.motions.Count != 1)
                throw new InvalidOperationException("창 단일 클립 계약이 달라졌습니다.");
            var current = motion.motionSet.motions[0].motionClip;
            if (current != source && AssetDatabase.GetAssetPath(current) != OutputPath)
                throw new InvalidOperationException("다른 창 저작 클립은 자동 교체하지 않습니다.");
            var binding = AnimationUtility.GetCurveBindings(source).Single(b => b.type == typeof(Animator) && b.path == "" && b.propertyName == "RootT.y");
            var originalY = AnimationUtility.GetEditorCurve(source, binding);
            Samples before = Sample(motion, source);
            var derived = UnityEngine.Object.Instantiate(source);
            try
            {
                var keys = new Keyframe[before.Times.Count];
                for (int i = 0; i < keys.Length; i++)
                {
                    float time = before.Times[i];
                    keys[i] = new Keyframe(time, originalY.Evaluate(time) - (before.Feet[i] - FootHeight) / before.Scale);
                }
                var curve = new AnimationCurve(keys);
                for (int i = 0; i < keys.Length; i++)
                {
                    AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
                    AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
                }
                AnimationUtility.SetEditorCurve(derived, binding, curve);
                Samples after = Sample(motion, derived);
                float maxFootError = 0, maxPoseError = 0;
                for (int i = 0; i < after.Times.Count; i++)
                {
                    maxFootError = Mathf.Max(maxFootError, Mathf.Abs(after.Feet[i] - FootHeight));
                    for (int bone = 0; bone < after.Relative[i].Length; bone++)
                        maxPoseError = Mathf.Max(maxPoseError, Vector3.Distance(before.Relative[i][bone], after.Relative[i][bone]));
                }
                if (maxFootError > .015f || maxPoseError > .005f)
                    throw new InvalidOperationException("창 높이 검증 실패: 발 오차=" + maxFootError + ", 관절 상대벡터 오차=" + maxPoseError);
                Undo.IncrementCurrentGroup();
                int group = Undo.GetCurrentGroup();
                var output = AssetDatabase.LoadAssetAtPath<AnimationClip>(OutputPath);
                bool created = output == null;
                try
                {
                    if (created)
                    {
                        output = UnityEngine.Object.Instantiate(derived);
                        AssetDatabase.CreateAsset(output, OutputPath);
                        Undo.RegisterCreatedObjectUndo(output, "창 접지 파생 클립 생성");
                    }
                    else
                    {
                        Undo.RegisterCompleteObjectUndo(output, "창 전신 높이 보정");
                        EditorUtility.CopySerialized(derived, output);
                    }
                    output.name = Path.GetFileNameWithoutExtension(OutputPath);
                    Undo.RegisterCompleteObjectUndo(motion, "창 접지 클립 연결");
                    motion.motionSet.motions[0].motionClip = output;
                    EditorUtility.SetDirty(output); EditorUtility.SetDirty(motion);
                    AssetDatabase.SaveAssetIfDirty(output); AssetDatabase.SaveAssetIfDirty(motion);
                    Undo.CollapseUndoOperations(group);
                }
                catch
                {
                    Undo.RevertAllDownToGroup(group);
                    if (created && File.Exists(OutputPath)) AssetDatabase.DeleteAsset(OutputPath);
                    if (!created && output != null) AssetDatabase.SaveAssetIfDirty(output);
                    AssetDatabase.SaveAssetIfDirty(motion);
                    throw;
                }
                string report = "창 접지 120Hz: samples=" + after.Times.Count + ", 낮은 발 목표=" + FootHeight + ", 최대 발 오차=" + maxFootError + ", 최대 골반 상대벡터 오차=" + maxPoseError;
                File.WriteAllText("docs/work-status/weapon-signature/SPEAR_HEIGHT_RECEIPT.txt", report);
                return report;
            }
            finally { UnityEngine.Object.DestroyImmediate(derived); }
        }

        private static Samples Sample(MotionSetAsset original, AnimationClip clip)
        {
            var result = new Samples();
            var temporary = ScriptableObject.CreateInstance<MotionSetAsset>();
            EditorUtility.CopySerialized(original, temporary);
            temporary.motionSet.motions[0].motionClip = clip;
            var root = PrefabUtility.LoadPrefabContents("Assets/03.Prefabs/Actor/Player/Models/PlayerModel_Reine.prefab");
            MotionSetPlaybackController playback = null;
            AnimancerComponent player = null;
            try
            {
                foreach (var behavior in root.GetComponentsInChildren<MonoBehaviour>(true)) if (behavior != null) behavior.enabled = false;
                foreach (var constraint in root.GetComponentsInChildren<UnityEngine.Animations.ParentConstraint>(true)) constraint.enabled = false;
                root.SetActive(true);
                var animator = root.GetComponent<Animator>();
                animator.enabled = true; animator.applyRootMotion = true; animator.fireEvents = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; animator.runtimeAnimatorController = null;
                result.Scale = animator.humanScale;
                player = root.GetComponent<AnimancerComponent>() ?? root.AddComponent<AnimancerComponent>();
                player.enabled = true; player.Animator = animator;
                player.Graph.UpdateMode = UnityEngine.Playables.DirectorUpdateMode.Manual;
                playback = new MotionSetPlaybackController(player);
                if (!playback.Play(temporary)) throw new InvalidOperationException("창 높이 샘플 재생 실패");
                player.Graph.PauseGraph();
                var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
                var bones = new[] { HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot, HumanBodyBones.LeftHand, HumanBodyBones.RightHand }
                    .Select(animator.GetBoneTransform).ToArray();
                int frames = Mathf.CeilToInt(temporary.motionSet.TotalDuration * 120);
                for (int i = 0; i <= frames; i++)
                {
                    float time = Mathf.Min(i / 120f, temporary.motionSet.TotalDuration);
                    playback.SeekTime(time); player.Evaluate(0);
                    root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                    result.Times.Add(time);
                    result.Feet.Add(Mathf.Min(bones[0].position.y, bones[1].position.y));
                    result.Relative.Add(bones.Select(b => b.position - hips.position).ToArray());
                }
                return result;
            }
            finally
            {
                playback?.Dispose();
                if (player != null && player.IsGraphInitialized) player.Graph.Destroy();
                PrefabUtility.UnloadPrefabContents(root);
                UnityEngine.Object.DestroyImmediate(temporary);
            }
        }
    }
}
