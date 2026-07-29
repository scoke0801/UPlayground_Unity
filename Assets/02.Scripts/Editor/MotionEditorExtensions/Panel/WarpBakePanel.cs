using System.Collections.Generic;
using System.Text;
using UPlayGround.Data.Event;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Animation.Editor
{
    public sealed class WarpBakePanel :
        IMotionEditorPanel,
        IMotionEditorPanelLifecycle
    {
        private sealed class Accumulator
        {
            public MotionEvent_MotionWarp Event;
            public float GlobalStart;
            public float GlobalEnd;
            public Vector3 Local;
            public float Path;
        }

        private readonly List<Accumulator> _accumulators = new();
        private IMotionEditorContext _context;
        private bool _active;
        private float _maxEnd;
        private float _previousCaptureDelta;
        private string _summary;

        public string Title => "워프 베이크";
        public int Order => 300;

        public bool IsAvailable(IMotionEditorContext context) =>
            context?.Asset != null &&
            context.Subject is IMotionPreviewRootMotion;

        public void OnGUI(IMotionEditorContext context)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(
                    "Warp 루트모션 베이크",
                    EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "Play Mode에서 MotionWarp 이벤트 구간의 실제 루트 변위와 경로 길이를 측정해 이벤트에 저장합니다.",
                    MessageType.Info);
                using (new EditorGUI.DisabledScope(
                           _active ||
                           !Application.isPlaying ||
                           context.Subject is not IMotionPreviewRootMotion))
                {
                    if (GUILayout.Button(
                            _active ? "베이크 중..." : "Bake Warp Root Motion",
                            GUILayout.Height(24f)))
                    {
                        Begin(context);
                    }
                }

                if (!string.IsNullOrEmpty(_summary))
                {
                    EditorGUILayout.LabelField("최근 결과", EditorStyles.miniBoldLabel);
                    EditorGUILayout.SelectableLabel(
                        _summary,
                        EditorStyles.textArea,
                        GUILayout.Height(
                            Mathf.Clamp(
                                _summary.Split('\n').Length * 15f + 6f,
                                24f,
                                140f)));
                }
            }
        }

        public void OnSceneGUI(IMotionEditorContext context)
        {
        }

        public void OnPlaybackStateChanged(
            IMotionEditorContext context,
            MotionPreviewPlaybackState state)
        {
            if (_active && state == MotionPreviewPlaybackState.Stopped &&
                context.PlaybackTime + 0.001f < _maxEnd)
            {
                Abort("재생 중단");
            }
        }

        public void OnEditorClosed(IMotionEditorContext context)
        {
            if (_active)
                Abort("에디터 종료");
        }

        private void Begin(IMotionEditorContext context)
        {
            MotionSet set = context.CurrentSet;
            if (set == null ||
                context.Subject is not IMotionPreviewRootMotion)
                return;

            _accumulators.Clear();
            float offset = 0f;
            if (set.motions != null)
            {
                foreach (Motion motion in set.motions)
                {
                    if (motion?.events != null)
                    {
                        foreach (MotionEventBase motionEvent in motion.events)
                        {
                            if (motionEvent is MotionEvent_MotionWarp warp)
                            {
                                _accumulators.Add(new Accumulator
                                {
                                    Event = warp,
                                    GlobalStart = offset + warp.startTime,
                                    GlobalEnd = offset + warp.endTime,
                                });
                            }
                        }
                    }
                    offset += motion?.Duration ?? 0f;
                }
            }

            if (set.globalEvents != null)
            {
                foreach (MotionEventBase motionEvent in set.globalEvents)
                {
                    if (motionEvent is MotionEvent_MotionWarp warp)
                    {
                        _accumulators.Add(new Accumulator
                        {
                            Event = warp,
                            GlobalStart = warp.startTime,
                            GlobalEnd = warp.endTime,
                        });
                    }
                }
            }

            if (_accumulators.Count == 0)
            {
                _summary = "MotionWarp 이벤트가 없습니다.";
                return;
            }

            _maxEnd = 0f;
            foreach (Accumulator accumulator in _accumulators)
                _maxEnd = Mathf.Max(_maxEnd, accumulator.GlobalEnd);

            context.RecordUndo("Warp Root Motion 베이크");
            context.Stop();
            context.SetPlaybackTime(0f);
            _context = context;
            _active = true;
            _summary = "베이크 중...";
            _previousCaptureDelta = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 120f;
            EditorApplication.update += Tick;
            context.Play();
        }

        private void Tick()
        {
            if (!_active || _context == null)
                return;
            if (!Application.isPlaying ||
                _context.Subject is not IMotionPreviewRootMotion rootMotion)
            {
                Abort("대상 또는 Play Mode 소실");
                return;
            }

            Vector3 delta = rootMotion.DeltaPosition;
            Vector3 horizontal = new(delta.x, 0f, delta.z);
            if (horizontal.sqrMagnitude > 1e-12f)
            {
                Quaternion inverse = Quaternion.Inverse(
                    _context.Subject.Root.transform.rotation);
                foreach (Accumulator accumulator in _accumulators)
                {
                    if (_context.PlaybackTime >= accumulator.GlobalStart &&
                        _context.PlaybackTime <= accumulator.GlobalEnd)
                    {
                        accumulator.Path += horizontal.magnitude;
                        accumulator.Local += inverse * horizontal;
                    }
                }
            }

            if (_context.PlaybackTime >= _maxEnd)
                Finish();
        }

        private void Finish()
        {
            var builder = new StringBuilder();
            foreach (Accumulator accumulator in _accumulators)
            {
                MotionEvent_MotionWarp warp = accumulator.Event;
                warp.bakedLocalTotal = accumulator.Local;
                warp.bakedPathLen = accumulator.Path;
                warp.bakedValid = accumulator.Path > 0.0001f;
                warp.bakedStartTime = warp.startTime;
                warp.bakedEndTime = warp.endTime;
                builder.AppendLine(
                    $"{warp.GetShortLabel()} [{accumulator.GlobalStart:F2}~{accumulator.GlobalEnd:F2}] " +
                    $"PathLen={accumulator.Path:F4} |Local|={accumulator.Local.magnitude:F4} " +
                    $"valid={warp.bakedValid}");
            }

            _summary = builder.ToString();
            EditorUtility.SetDirty(_context.Asset);
            AssetDatabase.SaveAssetIfDirty(_context.Asset);
            End();
            _context.Stop();
            _context.Repaint();
        }

        private void Abort(string reason)
        {
            _summary = $"베이크 중단({reason}) — 결과 미저장";
            End();
            _context?.Stop();
            _context?.Repaint();
        }

        private void End()
        {
            EditorApplication.update -= Tick;
            Time.captureDeltaTime = _previousCaptureDelta;
            _active = false;
        }
    }
}
