using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.SceneManagement;
using UPlayGround.EditorTools;

namespace UPlayGround.Editor.P09Builder
{
    public static partial class ScenarioResidentAuthoring
    {
        private static bool s_isElderRigReviewRunning;

        /// <summary>에디터의 실제 제약 갱신 후 원본과 노년 모델의 목 회전을 비교한다.</summary>
        [UPlaygroundTool("UPlayGround/캐릭터/P09/노년 리그 검수")]
        public static async void ReviewElderRig()
        {
            if (s_isElderRigReviewRunning || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("편집 모드에서 검수가 끝난 뒤 실행하세요.");
            s_isElderRigReviewRunning = true;
            EditorApplication.LockReloadAssemblies();
            EditorApplication.update += QueueElderRigUpdate;
            Scene previousScene = SceneManager.GetActiveScene();
            Scene reviewScene = default;
            int exitCode = 1;
            try
            {
                var recipe = JsonUtility.FromJson<ElderRecipe>(File.ReadAllText(ElderRecipePath));
                string folder = Output + "/ElderAppearance_" + recipe.revision;
                string prefabPath = "Assets/03.Prefabs/Characters/ScenarioResidents/AppearanceReview/P09_Male_Elder_" + recipe.revision + ".prefab";
                var saved = Require<GameObject>(prefabPath);
                // 저장하지 않는 일반 씬에서 실제 에디터 프레임의 제약 갱신을 검사한다.
                reviewScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                    Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
                if (!Application.isBatchMode) SceneManager.SetActiveScene(previousScene);
                foreach (bool baseline in new[] { true, false })
                    foreach (int yaw in new[] { -35, 35 })
                    {
                        var root = (GameObject)PrefabUtility.InstantiatePrefab(baseline ? Require<GameObject>(SkinReviewPrefabPath) : saved, reviewScene);
                        try
                        {
                            var hair = root.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r => r.name == recipe.hairRenderer);
                            var constraint = hair.rootBone.GetComponent<ParentConstraint>();
                            if (constraint == null || !constraint.constraintActive || !constraint.enabled)
                                throw new InvalidOperationException("머리카락 ParentConstraint가 활성 상태가 아닙니다.");
                            // 원본은 편집용 Lock이 꺼져 있다. 검사 인스턴스만 잠가 런타임처럼 추종시킨다.
                            constraint.locked = true;
                            await UpdateElderRigFrames();
                            Transform head = root.GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.Head);
                            Quaternion turn = Quaternion.AngleAxis(yaw, Vector3.up);
                            Quaternion expectedHair = turn * hair.rootBone.rotation;
                            head.rotation = turn * head.rotation;
                            await UpdateElderRigFrames();
                            float error = Quaternion.Angle(expectedHair, hair.rootBone.rotation);
                            if (error > 0.5f) throw new InvalidOperationException("머리카락 회전 추종 실패: " + error);
                            string prefix = baseline ? "Before_HeadTurn_" : "HeadTurn_";
                            Capture(root, folder + "/" + prefix + yaw + ".png", true, 0);
                        }
                        finally { if (root != null) UnityEngine.Object.DestroyImmediate(root); }
                    }
                File.WriteAllText(folder + "/rig-validation.txt", "원본·노년 프리팹의 좌우 35도 목 회전: ParentConstraint 실제 갱신 후 머리카락 회전 오차 0.5도 이내. 에디터 검사이며 Play Mode 검사는 아님.");
                exitCode = 0;
            }
            catch (Exception exception) { Debug.LogException(exception); }
            finally
            {
                if (reviewScene.IsValid() && !Application.isBatchMode) EditorSceneManager.CloseScene(reviewScene, true);
                EditorApplication.update -= QueueElderRigUpdate;
                EditorApplication.UnlockReloadAssemblies();
                s_isElderRigReviewRunning = false;
                if (Application.isBatchMode) EditorApplication.Exit(exitCode);
            }
        }

        private static async Task UpdateElderRigFrames()
        {
            // 같은 executeMethod 프레임에서 촬영하면 별도 헤어 스켈레톤의 제약이 아직 갱신되지 않는다.
            for (int i = 0; i < 10; i++)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                await Task.Delay(50);
            }
        }

        private static void QueueElderRigUpdate()
        {
            // 배치 편집 모드에서도 지연 작업의 Unity 동기화 컨텍스트를 처리할 프레임이 필요하다.
            EditorApplication.QueuePlayerLoopUpdate();
        }
    }
}
