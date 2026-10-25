#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Animancer;
using UnityEngine;
using UnityEngine.Animations;

namespace UPlayGround.CombatValidation
{
    /// <summary>격리된 무기 검증 씬에서 실제 Animator와 Constraint의 프레임 결과를 기록한다.</summary>
    public sealed class WeaponAnimationPlayerProbe : MonoBehaviour
    {
        [SerializeField] private AnimancerComponent _animancer;
        [SerializeField] private ParentConstraint _attachment;
        [SerializeField] private AnimationClip[] _clips;
        [SerializeField] private AnimationClip _idle;

        private readonly List<string> _results = new();
        private int _failures;
        private string _output;

        private IEnumerator Start()
        {
            _output = Path.Combine(Application.temporaryCachePath, "FrankWeaponRetarget");
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++)
                if (args[i] == "-weaponProbeOutput") _output = args[i + 1];
            Directory.CreateDirectory(_output);
            Application.runInBackground = true;
            Application.targetFrameRate = 60;
            Application.logMessageReceived += CaptureError;

            Transform weapon = _attachment.transform;
            Transform source = _attachment.GetSource(0).sourceTransform;
            Vector3 restScale = weapon.localScale;
            Quaternion restRotation = source.localRotation;
            Vector3 restPosition = source.localPosition;
            _animancer.Animator.fireEvents = false;
            _animancer.Animator.applyRootMotion = false;
            yield return null;
            foreach (AnimationClip clip in _clips)
            {
                AnimancerState state = _animancer.Play(clip);
                state.Speed = 0;
                float maxDistance = 0;
                float maxAngle = 0;
                float maxScale = 0;
                float displacement = 0;
                Vector3 firstPosition = source.position;
                int frames = Mathf.CeilToInt(clip.length * 60);
                for (int frame = 0; frame <= frames; frame++)
                {
                    state.Time = Mathf.Min(frame / 60f, clip.length);
                    yield return new WaitForEndOfFrame();
                    maxDistance = Mathf.Max(maxDistance, Vector3.Distance(weapon.position, source.position));
                    maxAngle = Mathf.Max(maxAngle, Quaternion.Angle(weapon.rotation, source.rotation));
                    maxScale = Mathf.Max(maxScale, weapon.localScale.x);
                    displacement = Mathf.Max(displacement, Vector3.Distance(firstPosition, source.position));
                    if (frame == frames / 3 || frame == frames * 2 / 3)
                        Capture(clip.name + "_" + frame);
                }
                Check(clip.name + ".constraint", maxDistance < 0.001f && maxAngle < 0.1f,
                    $"최대 위치 오차={maxDistance:F6}m, 회전 오차={maxAngle:F4}도");
                Check(clip.name + ".motion", displacement > 0.05f, $"무기 이동={displacement:F3}m");
                if (clip == _clips[3])
                    Check("skill-scale", maxScale > 2.9f && maxScale < 3.1f, $"칼날 확대={maxScale:F4}");

                Vector3 pausedPosition = weapon.position;
                float pausedTime = state.Time;
                for (int frame = 0; frame < 5; frame++) yield return new WaitForEndOfFrame();
                Check(clip.name + ".pause", Vector3.Distance(pausedPosition, weapon.position) < 0.0001f &&
                    Mathf.Abs(state.Time - pausedTime) < 0.0001f, "재생 정지 시 몸과 무기 동기화");

                AnimancerState idle = _animancer.Play(_idle, 0.1f);
                idle.Time = 0;
                idle.Speed = 1;
                yield return new WaitForSeconds(0.2f);
                Check(clip.name + ".reset", Vector3.Distance(weapon.localScale, restScale) < 0.001f &&
                    Vector3.Distance(source.localPosition, restPosition) < 0.001f &&
                    Quaternion.Angle(source.localRotation, restRotation) < 0.1f, "크로스페이드 후 기본 무기 자세 복원");
            }
            _results.Add("Failures=" + _failures);
            File.WriteAllLines(Path.Combine(_output, "player-validation.txt"), _results);
            Application.logMessageReceived -= CaptureError;
            if (!Application.isEditor) Application.Quit(_failures == 0 ? 0 : 1);
        }

        private void Check(string id, bool passed, string detail)
        {
            if (!passed) _failures++;
            _results.Add($"{(passed ? "PASS" : "FAIL")} {id} {detail}");
            File.WriteAllLines(Path.Combine(_output, "player-progress.txt"), _results);
        }

        private void CaptureError(string message, string stackTrace, LogType type)
        {
            if (type is not (LogType.Error or LogType.Exception or LogType.Assert)) return;
            _failures++;
            _results.Add("ERROR " + message + "\n" + stackTrace);
        }

        private void Capture(string name)
        {
            // Player 화면을 저장하므로 렌더러의 수동 포즈와 실행 프레임을 혼동하지 않는다.
            ScreenCapture.CaptureScreenshot(Path.Combine(_output, name + ".png"));
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= CaptureError;
        }
    }
}
#endif
