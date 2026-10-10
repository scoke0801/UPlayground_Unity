using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Animancer;
using UPlayGround.Animation;

namespace UPlayGround.Editor.P09Builder
{
    /// <summary>네이티브 ParentConstraint가 재생 중 손·등 소스를 추적하는지 검증한다.</summary>
    [InitializeOnLoad]
    public static class P09StaffPlayModeCheck
    {
        private const string SessionKey = "P09StaffPlayModeCheck.Active";
        public const string ToolId = "UPlayGround/캐릭터/P09/스태프 Play Mode 검증";
        private static GameObject _root;
        private static ParentConstraint _staff;
        private static AnimancerComponent _animancer;
        private static MotionSetPlaybackController _playback;
        private static int _gripLayerIndex;
        private static Transform _indexFinger;
        private static Quaternion _gripRotation;
        private static int _sample;
        private static int _frame;
        private static int _case;
        private static readonly string[] Motions = { "Idle", "Attack_1", "HeavyAttack_1", "Skill_1", "Skill_4" };

        static P09StaffPlayModeCheck()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        /// <summary>임시 빈 씬에서 손·등 추적을 확인하고 원래 편집 씬으로 돌아온다.</summary>
        [UPlayGround.EditorTools.UPlaygroundTool(ToolId, priority = 1104)]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("편집 모드에서 컴파일 완료 후 실행하세요.");
            // 사용자의 씬을 닫지 않고, Play Mode 시작 씬만 임시 빈 씬으로 지정한다.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
            string path = AssetDatabase.GenerateUniqueAssetPath("Assets/P09StaffRuntimeCheck.unity");
            EditorSceneManager.SaveScene(scene, path);
            if (!Application.isBatchMode) EditorSceneManager.CloseScene(scene, true);
            SessionState.SetString(SessionKey + ".Scene", path);
            SessionState.SetString(SessionKey + ".Previous", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            SessionState.SetBool(SessionKey, true);
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(SessionKey, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                _case = 0;
                EditorApplication.update += Tick;
                try { CreateCase(); }
                catch (Exception ex) { Finish(ex.ToString()); }
            }
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                string failure = SessionState.GetString(SessionKey + ".Failure", string.Empty);
                SessionState.SetBool(SessionKey, false);
                EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString(SessionKey + ".Previous", string.Empty));
                if (Application.isBatchMode) EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                AssetDatabase.DeleteAsset(SessionState.GetString(SessionKey + ".Scene", string.Empty));
                if (Application.isBatchMode) EditorApplication.Exit(string.IsNullOrEmpty(failure) ? 0 : 1);
            }
        }

        private static void CreateCase()
        {
            var sex = _case < Motions.Length ? BuilderSex.Male : BuilderSex.Female;
            var catalog = new P09AssetCatalog();
            catalog.Refresh();
            _root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PathConfig.GetBasePrefabPath(sex, false)));
            ApplyWeaponStep.Apply(_root, new CharacterBuildConfig { Sex = sex, StaffSo = catalog.Staves[0] }, catalog);
            _staff = P09StaffAttachment.FindActive(_root);
            var animator = _root.GetComponentInChildren<Animator>();
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var motion = AssetDatabase.LoadAssetAtPath<MotionSetAsset>(P09StaffAttachment.MotionRoot + Motions[_case % Motions.Length] + ".asset");
            _animancer = animator.GetComponent<AnimancerComponent>();
            if (_animancer == null) _animancer = animator.gameObject.AddComponent<AnimancerComponent>();
            _animancer.Animator = animator;
            _animancer.Graph.UpdateMode = DirectorUpdateMode.Manual;
            _playback = new MotionSetPlaybackController(_animancer);
            if (!_playback.Play(motion)) throw new InvalidOperationException("MotionSet 재생 실패");
            _gripLayerIndex = motion.motionSet.layers.Single(layer => layer.channelId == P09StaffGripAuthoring.Channel).animancerLayerIndex;
            _indexFinger = animator.GetBoneTransform(HumanBodyBones.RightIndexProximal);
            _sample = 0;
            SetSample();
        }

        private static void SetSample()
        {
            bool drawn = _sample < 10;
            var hand = _staff.GetSource(0); hand.weight = drawn ? 1f : 0f; _staff.SetSource(0, hand);
            var back = _staff.GetSource(1); back.weight = drawn ? 0f : 1f; _staff.SetSource(1, back);
            _playback.SeekNormalized((_sample % 10) / 10f);
            _animancer.Evaluate(0f);
            _frame = Time.frameCount;
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying || Time.frameCount < _frame + 2) return;
            try
            {
                if (_animancer.Layers[_gripLayerIndex].Weight < .99f)
                    throw new InvalidOperationException("손가락 레이어 가중치 유실");
                if (_sample == 1) _gripRotation = _indexFinger.localRotation;
                if (_sample > 1 && _sample % 10 != 0 && Quaternion.Angle(_indexFinger.localRotation, _gripRotation) > .1f)
                    throw new InvalidOperationException("클립별 손가락 쥐기 자세 불일치");
                int index = _sample < 10 ? 0 : 1;
                var source = _staff.GetSource(index).sourceTransform;
                float distance = Vector3.Distance(_staff.transform.position, source.TransformPoint(_staff.GetTranslationOffset(index)));
                float angle = Quaternion.Angle(_staff.transform.rotation, source.rotation * Quaternion.Euler(_staff.GetRotationOffset(index)));
                if (distance > .001f || angle > .1f)
                    throw new InvalidOperationException($"case={_case}, sample={_sample}, socket={index}, distance={distance}, angle={angle}");
                if (++_sample < 20) { SetSample(); return; }
                _playback.Dispose();
                UnityEngine.Object.DestroyImmediate(_root);
                if (++_case < Motions.Length * 2) { CreateCase(); return; }
                Finish(null);
            }
            catch (Exception ex) { Finish(ex.ToString()); }
        }

        private static void Finish(string failure)
        {
            EditorApplication.update -= Tick;
            _playback?.Dispose();
            if (_root != null) UnityEngine.Object.DestroyImmediate(_root);
            Directory.CreateDirectory("Logs/P09Staff");
            File.WriteAllText("Logs/P09Staff/playmode.txt", failure ?? "PASS: 2 sexes x 5 motions x 10 samples x 2 sockets = 200 native constraint samples via MotionSetPlaybackController. Position < 1 mm, rotation < 0.1 degrees; right-finger layer weight and pose stable.");
            SessionState.SetString(SessionKey + ".Failure", failure ?? string.Empty);
            if (failure != null) Debug.LogError(failure);
            else Debug.Log("P09 스태프 Play Mode 손·등 소켓 200샘플 검증 통과");
            EditorApplication.ExitPlaymode();
        }
    }
}
