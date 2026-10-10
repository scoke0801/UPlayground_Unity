using System;
using System.IO;
using System.Linq;
using Animancer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UPlayGround.Animation;
using UPlayGround.Data.Actor;

namespace UPlayGround.Editor.P09Builder
{
    /// <summary>생성된 몬스터 모델의 Idle·공격 모션을 Play Mode에서 실제 재생해 손 추적을 검사한다.</summary>
    [InitializeOnLoad]
    public static class P09StaffMonsterPlaybackCheck
    {
        private const string SessionKey = "P09StaffMonsterPlaybackCheck";
        private const string Folder = "Assets/10.Datas/Actor/P09Progression/Generated";
        private static ActorDefinitionSO[] _definitions;
        private static GameObject _root;
        private static ParentConstraint _staff;
        private static AnimancerComponent _animancer;
        private static MotionSetPlaybackController _playback;
        private static Transform _finger;
        private static Quaternion _fingerRotation;
        private static int _actorIndex, _motionIndex, _sample, _frame, _gripLayer, _total;

        static P09StaffMonsterPlaybackCheck() => EditorApplication.playModeStateChanged += OnStateChanged;

        /// <summary>게임 서비스와 AI를 제외한 실제 생성 모델의 네이티브 애니메이션·제약을 검사한다.</summary>
        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/캐릭터/P09/스태프 몬스터 모델 재생 검증", priority = 1106)]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("컴파일이 끝난 편집 모드에서 실행하세요.");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
            string path = AssetDatabase.GenerateUniqueAssetPath("Assets/P09StaffMonsterPlayback.unity");
            EditorSceneManager.SaveScene(scene, path);
            if (!Application.isBatchMode) EditorSceneManager.CloseScene(scene, true);
            SessionState.SetString(SessionKey + ".Scene", path);
            SessionState.SetString(SessionKey + ".Previous", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            SessionState.SetBool(SessionKey, true);
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            EditorApplication.EnterPlaymode();
        }

        private static void OnStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(SessionKey, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                _definitions = AssetDatabase.FindAssets("t:ActorDefinitionSO", new[] { Folder })
                    .Select(AssetDatabase.GUIDToAssetPath).Where(p => p.Contains("_Staff_"))
                    .OrderBy(p => p).Select(AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>).ToArray();
                _actorIndex = _motionIndex = _total = 0;
                EditorApplication.update += Tick;
                try
                {
                    if (_definitions.Length != 14) throw new InvalidOperationException("스태프 몬스터 14종이 필요합니다.");
                    CreateCase();
                }
                catch (Exception ex) { Finish(ex.ToString()); }
            }
            if (state != PlayModeStateChange.EnteredEditMode) return;
            SessionState.SetBool(SessionKey, false);
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString(SessionKey + ".Previous", ""));
            if (Application.isBatchMode) EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(SessionState.GetString(SessionKey + ".Scene", ""));
            if (Application.isBatchMode) EditorApplication.Exit(SessionState.GetBool(SessionKey + ".Failed", false) ? 1 : 0);
        }

        private static void CreateCase()
        {
            var definition = _definitions[_actorIndex];
            var mapping = definition.prefab.GetComponentInChildren<ActorAnimator>(true).MotionSet;
            MotionSetAsset motion = _motionIndex == 0
                ? mapping.GetMotionSetAsset(UPlayGround.Data.Actor.Animation.MotionTags.Idle)
                : mapping.abilityMotions.Values.ElementAt(_motionIndex - 1);
            _root = new GameObject("스태프 몬스터 모델 검증");
            _root.SetActive(false);
            var model = UnityEngine.Object.Instantiate(definition.prefab, _root.transform);
            // 비활성 상태에서 서비스 의존 컴포넌트를 제거해 씬을 초기화하거나 전투를 시작하지 않는다.
            foreach (var component in model.GetComponentsInChildren<MonoBehaviour>(true).Reverse())
                if (component is not AnimancerComponent) UnityEngine.Object.DestroyImmediate(component);
            _root.SetActive(true);
            _staff = P09StaffAttachment.FindActive(model);
            if (_staff == null) throw new InvalidOperationException("스태프 외형 누락: " + definition.actorId);
            var animator = model.GetComponentInChildren<Animator>(true);
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            _animancer = model.GetComponentInChildren<AnimancerComponent>(true);
            _animancer.Animator = animator;
            _animancer.Graph.UpdateMode = DirectorUpdateMode.Manual;
            _playback = new MotionSetPlaybackController(_animancer);
            if (!_playback.Play(motion)) throw new InvalidOperationException("재생 실패: " + motion.name);
            _gripLayer = motion.motionSet.layers.Single(l => l.channelId == P09StaffGripAuthoring.Channel).animancerLayerIndex;
            _finger = animator.GetBoneTransform(HumanBodyBones.RightIndexProximal);
            _sample = 0;
            Sample();
        }

        private static void Sample()
        {
            _playback.SeekNormalized(_sample / 10f);
            _animancer.Evaluate(0f);
            _frame = Time.frameCount;
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying || Time.frameCount < _frame + 2) return;
            try
            {
                var source = _staff.GetSource(0).sourceTransform;
                float distance = Vector3.Distance(_staff.transform.position, source.TransformPoint(_staff.GetTranslationOffset(0)));
                float angle = Quaternion.Angle(_staff.transform.rotation, source.rotation * Quaternion.Euler(_staff.GetRotationOffset(0)));
                if (distance > .001f || angle > .1f || _animancer.Layers[_gripLayer].Weight < .99f)
                    throw new InvalidOperationException($"{_definitions[_actorIndex].actorId}, 모션 {_motionIndex}, 샘플 {_sample}: 손 추적 오차 {distance}m/{angle}도");
                if (_sample == 1) _fingerRotation = _finger.localRotation;
                if (_sample > 1 && Quaternion.Angle(_fingerRotation, _finger.localRotation) > .1f)
                    throw new InvalidOperationException("손가락 포즈 유실");
                _total++;
                if (++_sample < 10) { Sample(); return; }
                _playback.Dispose();
                _playback = null;
                UnityEngine.Object.DestroyImmediate(_root);
                if (++_motionIndex == 4) { _motionIndex = 0; _actorIndex++; }
                if (_actorIndex == _definitions.Length) { Finish(null); return; }
                CreateCase();
            }
            catch (Exception ex) { Finish(ex.ToString()); }
        }

        private static void Finish(string failure)
        {
            EditorApplication.update -= Tick;
            _playback?.Dispose();
            _playback = null;
            if (_root != null) UnityEngine.Object.DestroyImmediate(_root);
            Directory.CreateDirectory("Logs/P09StaffMonsters");
            File.WriteAllText("Logs/P09StaffMonsters/playback.txt", failure ??
                $"PASS: 몬스터 14종 × Idle/공격 4모션 × 10프레임 = {_total}샘플. 손 추적 1mm/0.1도 이내, 손가락 포즈 유지. 게임 서비스·BT 전투는 이 검사에 포함하지 않음.");
            SessionState.SetBool(SessionKey + ".Failed", failure != null);
            if (failure != null) Debug.LogError(failure);
            EditorApplication.ExitPlaymode();
        }
    }
}
