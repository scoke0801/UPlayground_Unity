using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Animancer;
using UnityEditor;
using UnityEngine;
using UPlayGround.Animation;

namespace UPlayGround.Editor
{
    public static partial class HumanoidSpearAuthoring
    {
        /// <summary>저장하지 않은 새 병사 사본에서 실제 창 표면·손 접촉과 루트 이동을 측정한다.</summary>
        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/전투/신규 창병 모션 조사")]
        public static string Probe(bool reference = false)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed)
                throw new InvalidOperationException("컴파일이 끝난 Edit Mode 필요");
            Directory.CreateDirectory(LogRoot);
            var root = reference ? UnityEngine.Object.Instantiate(Load<GameObject>(SourceRigPath)) : CreateModel();
            var temporary = new List<AnimationClip>();
            var preview = new PreviewRenderUtility();
            var mesh = new Mesh();
            var rows = new List<string> { "motion,time,tipX,tipY,tipZ,tipSpeed,leftSurfaceDistance,rightSurfaceDistance,rootX,rootZ" };
            try
            {
                var animator = PrepareSampling(root);
                var renderer = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r => r.name == "Weapon_Spear");
                if (reference) AddHitboxes(renderer);
                var left = animator.GetBoneTransform(HumanBodyBones.LeftMiddleProximal);
                var right = animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal);
                var player = root.GetComponent<AnimancerComponent>() ?? root.AddComponent<AnimancerComponent>();
                player.Animator = animator; player.enabled = true;
                preview.AddSingleGO(root);
                foreach (string guid in AssetDatabase.FindAssets("t:MotionSetAsset", new[] { "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Humanoid/Spear" }))
                {
                    var asset = Load<MotionSetAsset>(AssetDatabase.GUIDToAssetPath(guid));
                    if (!System.Text.RegularExpressions.Regex.IsMatch(asset.name, @"^Humanoid_Spear_(Attack|HeavyAttack|Skill)_\d+$")) continue;
                    var part = asset.motionSet.motions.Single();
                    var clip = reference ? CreateReferenceClip(part.motionClip) : CreateClip(part.motionClip, root); temporary.Add(clip);
                    var state = player.Play(clip);
                    player.Graph.PauseGraph(); state.Time = 0; player.Evaluate(0);
                    Vector3 position = Vector3.zero; Quaternion rotation = Quaternion.identity;
                    root.transform.SetPositionAndRotation(position, rotation);
                    Vector3 previous = Vector3.zero;
                    for (int step = 0; step <= Mathf.CeilToInt(part.Duration * 60); step++)
                    {
                        float time = Mathf.Min(step / 60f, part.Duration);
                        if (step > 0)
                        {
                            player.Evaluate(time - (step - 1) / 60f);
                            position += animator.deltaPosition; rotation *= animator.deltaRotation;
                            root.transform.SetPositionAndRotation(position, rotation);
                        }
                        renderer.BakeMesh(mesh);
                        EvaluateConstraints(root);
                        var vertices = mesh.vertices;
                        var triangles = mesh.triangles;
                        var bladeShape = renderer.bones[2].GetComponentInChildren<BoxCollider>();
                        Vector3 tip = renderer.bones[2].TransformPoint(bladeShape.center + Vector3.left * (bladeShape.size.x * 0.5f));
                        float speed = step == 0 ? 0 : (tip - previous).magnitude * 60;
                        previous = tip;
                        float ld = SurfaceDistance(renderer.transform.InverseTransformPoint(left.position), vertices, triangles);
                        float rd = SurfaceDistance(renderer.transform.InverseTransformPoint(right.position), vertices, triangles);
                        rows.Add(string.Join(",", new[] { asset.name, time.ToString("F4"), tip.x.ToString("F4"), tip.y.ToString("F4"), tip.z.ToString("F4"), speed.ToString("F4"), ld.ToString("F4"), rd.ToString("F4"), position.x.ToString("F4"), position.z.ToString("F4") }));
                        if ((asset.name == "Humanoid_Spear_Attack_1" && step == 18) || (asset.name == "Humanoid_Spear_HeavyAttack_8" && step == 30))
                            RenderProbe(preview, root, LogRoot + "/" + (reference ? "Reference_" : "") + asset.name + ".png");
                    }
                }
            }
            finally
            {
                foreach (var p in root.GetComponentsInChildren<AnimancerComponent>(true)) if (p.IsGraphInitialized) p.Graph.Destroy();
                foreach (var clip in temporary) UnityEngine.Object.DestroyImmediate(clip);
                UnityEngine.Object.DestroyImmediate(mesh);
                if (reference) UnityEngine.Object.DestroyImmediate(root); else PrefabUtility.UnloadPrefabContents(root);
                preview.Cleanup();
                File.WriteAllLines(LogRoot + (reference ? "/reference-probe.csv" : "/motion-probe.csv"), rows);
            }
            return "표본 " + (rows.Count - 1) + ": " + LogRoot;
        }

        private static AnimationClip CreateReferenceClip(AnimationClip source)
        {
            var result = UnityEngine.Object.Instantiate(source);
            var preview = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(source)).OfType<AnimationClip>().First(c => c.name.StartsWith("__preview__"));
            foreach (var binding in AnimationUtility.GetCurveBindings(preview).Where(b => b.path.Contains("/R_Hand_Weapon")))
                AnimationUtility.SetEditorCurve(result, binding, AnimationUtility.GetEditorCurve(preview, binding));
            return result;
        }

        private static void EvaluateConstraints(GameObject root)
        {
            foreach (var c in root.GetComponentsInChildren<UnityEngine.Animations.ParentConstraint>(true))
            {
                if (!c.constraintActive || !c.gameObject.activeInHierarchy) continue;
                for (int i = 0; i < c.sourceCount; i++)
                {
                    var source = c.GetSource(i);
                    if (source.weight < 0.99f || source.sourceTransform == null) continue;
                    c.transform.SetPositionAndRotation(source.sourceTransform.TransformPoint(c.GetTranslationOffset(i)), source.sourceTransform.rotation * Quaternion.Euler(c.GetRotationOffset(i)));
                    break;
                }
            }
        }

        private static Animator PrepareSampling(GameObject root)
        {
            foreach (var b in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (b == null) continue;
                b.enabled = false;
                if (b is ActorAnimator) UnityEngine.Object.DestroyImmediate(b);
            }
            foreach (var particle in root.GetComponentsInChildren<ParticleSystem>(true)) particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            foreach (var constraint in root.GetComponentsInChildren<UnityEngine.Animations.ParentConstraint>(true)) constraint.enabled = false;
            root.SetActive(true);
            var animator = root.GetComponent<Animator>();
            animator.enabled = true; animator.fireEvents = false; animator.applyRootMotion = true;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; animator.runtimeAnimatorController = null;
            return animator;
        }

        private static void RenderProbe(PreviewRenderUtility preview, GameObject root, string path)
        {
            var center = root.transform.position + Vector3.up;
            preview.camera.transform.position = center + new Vector3(3, 1.5f, 3.5f);
            preview.camera.transform.LookAt(center); preview.camera.fieldOfView = 40;
            preview.camera.nearClipPlane = 0.01f; preview.camera.farClipPlane = 30;
            preview.camera.clearFlags = CameraClearFlags.SolidColor; preview.camera.backgroundColor = new Color(.16f, .18f, .2f);
            preview.lights[0].intensity = 1.4f; preview.lights[0].transform.rotation = Quaternion.Euler(40, 35, 0);
            preview.lights[1].intensity = .7f; preview.ambientColor = Color.gray;
            preview.BeginStaticPreview(new Rect(0, 0, 1000, 1000)); preview.Render(true, true);
            var texture = preview.EndStaticPreview(); File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
        }

        private static float SurfaceDistance(Vector3 p, Vector3[] vertices, int[] triangles)
        {
            float distance = float.PositiveInfinity;
            for (int t = 0; t < triangles.Length; t += 3)
                distance = Mathf.Min(distance, (p - ClosestOnTriangle(p, vertices[triangles[t]], vertices[triangles[t + 1]], vertices[triangles[t + 2]])).sqrMagnitude);
            return Mathf.Sqrt(distance);
        }

        private static Vector3 ClosestOnTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 ab = b - a, ac = c - a, ap = p - a;
            float d1 = Vector3.Dot(ab, ap), d2 = Vector3.Dot(ac, ap);
            if (d1 <= 0 && d2 <= 0) return a;
            Vector3 bp = p - b; float d3 = Vector3.Dot(ab, bp), d4 = Vector3.Dot(ac, bp);
            if (d3 >= 0 && d4 <= d3) return b;
            float vc = d1 * d4 - d3 * d2;
            if (vc <= 0 && d1 >= 0 && d3 <= 0) return a + ab * (d1 / (d1 - d3));
            Vector3 cp = p - c; float d5 = Vector3.Dot(ab, cp), d6 = Vector3.Dot(ac, cp);
            if (d6 >= 0 && d5 <= d6) return c;
            float vb = d5 * d2 - d1 * d6;
            if (vb <= 0 && d2 >= 0 && d6 <= 0) return a + ac * (d2 / (d2 - d6));
            float va = d3 * d6 - d5 * d4;
            if (va <= 0 && d4 - d3 >= 0 && d5 - d6 >= 0) return b + (c - b) * ((d4 - d3) / ((d4 - d3) + d5 - d6));
            float denominator = 1 / (va + vb + vc);
            return a + ab * (vb * denominator) + ac * (vc * denominator);
        }
    }
}
