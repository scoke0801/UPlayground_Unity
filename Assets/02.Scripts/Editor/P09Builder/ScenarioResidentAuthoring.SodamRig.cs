using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UPlayGround.Animation;
using UPlayGround.EditorTools;

namespace UPlayGround.Editor.P09Builder
{
    public static partial class ScenarioResidentAuthoring
    {
        /// <summary>임시 씬에서 소담의 목 회전과 기존 대기·보행 모션의 머리카락 추종을 검사한다.</summary>
        [UPlaygroundTool("UPlayGround/캐릭터/P09/소담 리그 검수")]
        public static async void ReviewSodamRig()
        {
            RequireLifeEditor();
            if (s_isElderRigReviewRunning) throw new InvalidOperationException("리그 검수가 이미 실행 중입니다.");
            s_isElderRigReviewRunning = true;
            EditorApplication.LockReloadAssemblies();
            EditorApplication.update += QueueElderRigUpdate;
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = default;
            int exitCode = 1;
            try
            {
                var recipe = JsonUtility.FromJson<SodamRecipe>(File.ReadAllText(SodamRecipePath));
                string folder = Output + "/Sodam/" + recipe.revision;
                string prefabPath = "Assets/03.Prefabs/Characters/ScenarioResidents/AppearanceReview/P09_Female_Sodam_" + recipe.revision + ".prefab";
                var saved = Require<GameObject>(prefabPath);
                CaptureSodamCopy(saved, folder + "/Face_Soft_0.png", true, 0);
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
                if (!Application.isBatchMode) SceneManager.SetActiveScene(previous);
                float maximumError = 0;
                foreach (int yaw in new[] { -35, 35 })
                {
                    var root = (GameObject)PrefabUtility.InstantiatePrefab(saved, scene);
                    try
                    {
                        var hair = root.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r => r.name == recipe.sculpt.hairRenderer);
                        LockSodamHairConstraint(hair);
                        await UpdateElderRigFrames();
                        var head = root.GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.Head);
                        Quaternion turn = Quaternion.AngleAxis(yaw, Vector3.up);
                        Quaternion expected = turn * hair.rootBone.rotation;
                        head.rotation = turn * head.rotation;
                        await UpdateElderRigFrames();
                        float error = Quaternion.Angle(expected, hair.rootBone.rotation);
                        maximumError = Mathf.Max(maximumError, error);
                        if (error > 0.5f) throw new InvalidOperationException("머리카락 회전 추종 실패: " + error);
                        Capture(root, folder + "/HeadTurn_" + yaw + ".png", true, 0, false, true);
                    }
                    finally { if (root != null) UnityEngine.Object.DestroyImmediate(root); }
                }
                foreach (string motion in new[] { "Npc_Female_Idle", "Npc_Walk" })
                {
                    var root = (GameObject)PrefabUtility.InstantiatePrefab(saved, scene);
                    PlayableGraph graph = default;
                    try
                    {
                        var animator = root.GetComponentInChildren<Animator>();
                        if (!animator.isHuman || !animator.avatar.isValid) throw new InvalidOperationException("Humanoid Avatar 누락");
                        animator.runtimeAnimatorController = null;
                        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                        var hair = root.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r => r.name == recipe.sculpt.hairRenderer);
                        LockSodamHairConstraint(hair);
                        await UpdateElderRigFrames();
                        var head = animator.GetBoneTransform(HumanBodyBones.Head);
                        Quaternion relativeHair = Quaternion.Inverse(head.rotation) * hair.rootBone.rotation;
                        var clip = Require<MotionSetAsset>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/" + motion + ".asset").motionSet.motions[0].motionClip;
                        graph = PlayableGraph.Create("소담 모션 검수");
                        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                        var playable = AnimationClipPlayable.Create(graph, clip);
                        var output = AnimationPlayableOutput.Create(graph, "소담", animator);
                        output.SetSourcePlayable(playable);
                        graph.Play();
                        foreach (float time in new[] { 0.2f, 0.5f, 0.8f })
                        {
                            playable.SetTime(clip.length * time);
                            graph.Evaluate(0);
                            await UpdateElderRigFrames();
                            float error = Quaternion.Angle(head.rotation * relativeHair, hair.rootBone.rotation);
                            maximumError = Mathf.Max(maximumError, error);
                            if (error > 0.5f) throw new InvalidOperationException("모션 중 머리카락 추종 실패: " + motion + " / " + error);
                            ValidateElderRoot(root);
                            CaptureSodamCopy(root, folder + "/" + motion + "_" + (int)(time * 100) + ".png", false, 0);
                        }
                    }
                    finally
                    {
                        if (graph.IsValid()) graph.Destroy();
                        if (root != null) UnityEngine.Object.DestroyImmediate(root);
                    }
                }
                File.WriteAllText(folder + "/rig-validation.txt", "에디터 실제 프레임 갱신: 목 좌우 35도·대기 3시점·보행 3시점 통과\nHumanoid Avatar 유효 / 머리카락 회전 최대 오차 "
                    + maximumError + "도 (허용 0.5도)\n원본 모션 데이터 변경 없음. Play Mode 및 Player Build 검증 아님.");
                exitCode = 0;
            }
            catch (Exception exception) { Debug.LogException(exception); }
            finally
            {
                if (scene.IsValid() && !Application.isBatchMode) EditorSceneManager.CloseScene(scene, true);
                EditorApplication.update -= QueueElderRigUpdate;
                EditorApplication.UnlockReloadAssemblies();
                s_isElderRigReviewRunning = false;
                if (Application.isBatchMode) EditorApplication.Exit(exitCode);
            }
        }

        private static void LockSodamHairConstraint(SkinnedMeshRenderer hair)
        {
            var constraint = hair.rootBone.GetComponent<ParentConstraint>();
            if (constraint == null || !constraint.constraintActive || !constraint.enabled)
                throw new InvalidOperationException("머리카락 제약이 비활성 상태입니다.");
            constraint.locked = true;
        }
    }
}
