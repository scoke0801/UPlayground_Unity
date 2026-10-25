using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Components;

namespace UPlayGround.Animation.Editor
{
    /// <summary>기존 모션 에디터의 타임라인과 Scene 프리뷰에 측정 결과를 겹친다.</summary>
    public sealed class CombatMotionAnalysisPanel : IMotionEditorPanel
    {
        private CombatMotionCatalog _catalog;
        private CombatMotionEntry _entry;
        private bool _show = true;
        private string _fingerprint;
        private string _overlayKey;
        private List<MotionSetDrawer.OverlayTrack> _overlayTracks;
        private bool _overlayPending;
        private double _nextCheck;
        public string Title => "전투 모션 분석";
        public int Order => 350;

        public bool IsAvailable(IMotionEditorContext context) => context?.Asset != null;

        public void OnGUI(IMotionEditorContext context)
        {
            if (_catalog == null)
            {
                string guid = AssetDatabase.FindAssets("t:CombatMotionCatalog").FirstOrDefault();
                if (guid != null) _catalog = AssetDatabase.LoadAssetAtPath<CombatMotionCatalog>(AssetDatabase.GUIDToAssetPath(guid));
            }
            _catalog = (CombatMotionCatalog)EditorGUILayout.ObjectField("측정 묶음", _catalog, typeof(CombatMotionCatalog), false);
            _show = EditorGUILayout.Toggle("궤적 오버레이", _show);
            _entry = _catalog?.entries.FirstOrDefault(e => e.measurement?.motionSet.path == AssetDatabase.GetAssetPath(context.Asset));
            if (_entry == null)
            {
                if (Event.current.type == EventType.Layout) QueueOverlay(context);
                EditorGUILayout.HelpBox("이 MotionSet의 측정 결과가 없습니다.", MessageType.Info);
                return;
            }
            if (EditorApplication.timeSinceStartup >= _nextCheck)
            {
                _nextCheck = EditorApplication.timeSinceStartup + 1;
                try { _fingerprint = CombatMotionAnalysis.Fingerprint(_catalog, _entry); }
                catch { _fingerprint = null; }
            }
            bool stale = _entry.measurement.fingerprint != _fingerprint;
            EditorGUILayout.LabelField(stale ? "오래된 결과: 재분석 필요" : "측정 조건 최신 / 시각 검수는 별도");
            if (GUILayout.Button("분석 목록·안전한 포즈 비교 열기")) CombatMotionCatalogWindow.Open(_catalog, _catalog.entries.IndexOf(_entry));
            if (Event.current.type == EventType.Layout) QueueOverlay(context);
        }

        private void QueueOverlay(IMotionEditorContext context)
        {
            var asset = context.Asset;
            string key = $"{asset.GetInstanceID()}|{_catalog?.GetInstanceID()}|{_entry?.measurement?.generatedUtc}|{_show}";
            if (_overlayKey != key)
            {
                _overlayKey = key;
                _overlayTracks = new List<MotionSetDrawer.OverlayTrack>();
                foreach (string kind in new[] { "Hit", "Cancel", "Combo", "Telegraph" })
                {
                    var track = new MotionSetDrawer.OverlayTrack { label = "측정 " + kind, color = kind == "Hit" ? Color.red : Color.cyan };
                    if (_show && _entry?.measurement != null)
                        foreach (var window in _entry.measurement.windows.Where(w => w.kind == kind))
                            track.spans.Add(new MotionSetDrawer.OverlaySpan { start = window.start, end = window.end,
                                label = window.actions ?? $"P{window.hitPhaseIndex}", dashed = kind == "Cancel" });
                    _overlayTracks.Add(track);
                }
            }
            if (_overlayPending) return;
            _overlayPending = true;
            // IMGUIContainer의 레이아웃 도중 UI Toolkit 계층을 갱신하면 예외가 발생한다.
            EditorApplication.CallbackFunction apply = null;
            apply = () =>
            {
                EditorApplication.update -= apply;
                _overlayPending = false;
                if ((context is Object host && host == null) || context.Asset != asset) return;
                context.SetOverlayTracks("측정 타이밍", _overlayTracks);
            };
            EditorApplication.update += apply;
        }

        public void OnSceneGUI(IMotionEditorContext context)
        {
            if (!_show || _entry?.measurement == null || _entry.measurement.fingerprint != _fingerprint || context.Subject?.Animancer == null) return;
            var model = context.Subject.Animancer.GetComponent<CharacterModelData>();
            var expected = _catalog.modelPrefab.GetComponent<CharacterModelData>();
            if (model == null || model.Definition != expected.Definition || context.Subject.Animancer.Animator.avatar != _catalog.modelPrefab.GetComponent<Animator>().avatar) return;
            var result = _entry.measurement;
            var sample = result.samples.OrderBy(s => Mathf.Abs(s.time - context.PlaybackTime)).FirstOrDefault();
            if (sample == null) return;
            Transform root = model.transform;
            Matrix4x4 old = Handles.matrix;
            Color color = Handles.color;
            Handles.matrix = Matrix4x4.TRS(root.position, root.rotation, Vector3.one) * Matrix4x4.TRS(sample.rootPosition, sample.rootRotation, Vector3.one).inverse;
            for (int i = 1; i < result.samples.Count; i++)
            {
                var before = result.samples[i - 1]; var after = result.samples[i];
                Handles.color = result.windows.Any(w => w.kind == "Hit" && after.time >= w.start && after.time <= w.end) ? Color.red : Color.yellow;
                Handles.DrawLine(before.bladeTip, after.bladeTip);
            }
            Handles.color = Color.white;
            Handles.DrawLine(sample.bladeBase, sample.bladeTip);
            Handles.matrix = old; Handles.color = color;
        }

        public void OnPlaybackStateChanged(IMotionEditorContext context, MotionPreviewPlaybackState state) { }
    }
}
