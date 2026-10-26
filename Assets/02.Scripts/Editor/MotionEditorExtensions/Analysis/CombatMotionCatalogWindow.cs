using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.EditorTools;

namespace UPlayGround.Animation.Editor
{
    /// <summary>측정 사용처 검색, 안전한 포즈/궤적 비교, 기존 모션 에디터 이동을 제공한다.</summary>
    public sealed class CombatMotionCatalogWindow : EditorWindow
    {
        public const string ToolId = "UPlayGround/게임플레이/전투/전투 모션 분석";
        [SerializeField] private CombatMotionCatalog _catalog;
        private string _query = "";
        private bool _onlyUnreviewed, _onlyStale;
        private Vector2 _scroll;
        private int _selected, _sampleIndex, _compareIndex, _boundaryIndex;
        private CombatMotionSampler _sampler;
        private Camera _camera;
        private RenderTexture _texture;
        private bool _showTrajectory = true, _showHitboxes = true;
        private float _cameraYaw = 25;
        private float _cameraDistance = 3.5f;
        private bool _showOriginal;
        private int _originalClipIndex;
        private MotionSet _originalSet;
        private string _error;
        private readonly Dictionary<CombatMotionEntry, bool> _stale = new();

        /// <summary>통합 툴 런처에서 기존 카탈로그를 연다.</summary>
        [UPlaygroundTool(ToolId)]
        public static void Open()
        {
            var window = GetWindow<CombatMotionCatalogWindow>("전투 모션 분석");
            window.minSize = new Vector2(720, 640);
            if (window._catalog == null)
            {
                string guid = AssetDatabase.FindAssets("t:CombatMotionCatalog").FirstOrDefault();
                if (guid != null) window._catalog = AssetDatabase.LoadAssetAtPath<CombatMotionCatalog>(AssetDatabase.GUIDToAssetPath(guid));
            }
            window.RefreshStatus();
            window.Show();
        }

        /// <summary>선택 사용처를 기존 확장 패널에서 연다.</summary>
        public static void Open(CombatMotionCatalog catalog, int index)
        {
            Open();
            var window = GetWindow<CombatMotionCatalogWindow>();
            window._catalog = catalog;
            window.SelectEntry(index);
            window.RefreshStatus();
        }

        private CombatMotionEntry Entry => _catalog != null && _catalog.entries.Count > 0
            ? _catalog.entries[Mathf.Clamp(_selected, 0, _catalog.entries.Count - 1)] : null;

        private void OnEnable() => EditorApplication.projectChanged += RefreshStatus;
        private void OnDisable() { EditorApplication.projectChanged -= RefreshStatus; ReleasePreview(); }
        private void RefreshStatus()
        {
            _stale.Clear();
            if (_catalog == null) return;
            foreach (var entry in _catalog.entries)
            {
                try { _stale[entry] = entry.measurement == null || entry.measurement.fingerprint != CombatMotionAnalysis.Fingerprint(_catalog, entry); }
                catch { _stale[entry] = true; }
            }
            Repaint();
        }

        private void OnGUI()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorGUILayout.HelpBox("전투 모션 분석은 Edit Mode에서 사용할 수 있습니다. Play Mode 종료 후 다시 열어 주세요.", MessageType.Info);
                return;
            }
            var catalog = (CombatMotionCatalog)EditorGUILayout.ObjectField("측정 묶음", _catalog, typeof(CombatMotionCatalog), false);
            if (catalog != _catalog) { _catalog = catalog; SelectEntry(0); RefreshStatus(); }
            if (_catalog == null) { EditorGUILayout.HelpBox("프로젝트의 CombatMotionCatalog를 지정하세요.", MessageType.Info); return; }
            using (new EditorGUILayout.HorizontalScope())
            {
                _query = EditorGUILayout.TextField("키·태그·동작 검색", _query);
                _onlyUnreviewed = GUILayout.Toggle(_onlyUnreviewed, "미검수", GUILayout.Width(65));
                _onlyStale = GUILayout.Toggle(_onlyStale, "오래됨", GUILayout.Width(65));
                if (GUILayout.Button("원본 변경 확인", GUILayout.Width(105))) RefreshStatus();
                if (GUILayout.Button("측정 설정", GUILayout.Width(75))) Selection.activeObject = _catalog;
                if (GUILayout.Button("경계 검증", GUILayout.Width(75))) Run(() => {
                    CombatMotionAnalysisValidation.Run(_catalog);
                    ShowNotification(new GUIContent("검증 통과: Logs/CombatMotionAnalysis/validation.json")); });
            }
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if (!string.IsNullOrEmpty(_error)) EditorGUILayout.HelpBox(_error, MessageType.Error);
            DrawEntries();
            if (Entry != null) DrawEntry();
            EditorGUILayout.EndScrollView();
        }

        private void DrawEntries()
        {
            for (int i = 0; i < _catalog.entries.Count; i++)
            {
                var entry = _catalog.entries[i];
                var measurement = entry.measurement;
                bool stale = !_stale.TryGetValue(entry, out bool value) || value;
                string label = $"{entry.ability?.abilityId} · {entry.usage} · {measurement?.suggestedDirection} · {measurement?.suggestedMovement} · {entry.manualTags}";
                if ((_onlyStale && !stale) || (_onlyUnreviewed && entry.isReviewed) ||
                    label.IndexOf(_query, StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (GUILayout.Toggle(_selected == i, $"{(stale ? "[오래됨/미측정]" : "[측정됨]")} {(entry.isReviewed ? "[검수]" : "[미검수]")} {label}", "Button"))
                    if (_selected != i) SelectEntry(i);
            }
        }

        private void DrawEntry()
        {
            var entry = Entry;
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("선택 재분석")) Run(() => {
                    var measured = CombatMotionAnalysis.Measure(_catalog, entry);
                    Undo.RecordObject(_catalog, "선택 모션 재분석"); entry.measurement = measured;
                    EditorUtility.SetDirty(_catalog); AssetDatabase.SaveAssetIfDirty(_catalog); SelectEntry(_selected); RefreshStatus(); });
                if (GUILayout.Button("전체 재분석")) Run(() => { CombatMotionAnalysis.Regenerate(_catalog); SelectEntry(_selected); RefreshStatus(); });
                if (GUILayout.Button("기존 모션 프리뷰")) Run(() => MotionSetEditorWindow.Open(CombatMotionAnalysis.Resolve(_catalog, entry, out _, out _, out _)));
                if (GUILayout.Button("MotionSet 선택")) Run(() => Selection.activeObject = CombatMotionAnalysis.Resolve(_catalog, entry, out _, out _, out _));
                if (GUILayout.Button("Ability 선택")) Selection.activeObject = entry.ability;
                if (GUILayout.Button("JSON 내보내기")) Run(() => {
                    string path = EditorUtility.SaveFilePanel("측정 JSON", "ArtSource/CombatMotionCatalog", _catalog.name + ".json", "json");
                    if (!string.IsNullOrEmpty(path)) CombatMotionAnalysis.Export(_catalog, path); });
            }
            if (GUILayout.Button("런타임 타격 데이터 베이크 (전체 공격)"))
                Run(() => { CombatMotionImpactBaker.Bake(_catalog); SelectEntry(_selected); RefreshStatus(); });
            EditorGUI.BeginChangeCheck();
            string tags = EditorGUILayout.TextField("수동 태그", entry.manualTags);
            bool reviewed = EditorGUILayout.Toggle("수동 검수 완료", entry.isReviewed);
            string notes = EditorGUILayout.TextField("검수 메모", entry.reviewNotes);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_catalog, "모션 수동 검수");
                entry.manualTags = tags; entry.isReviewed = reviewed; entry.reviewNotes = notes;
                EditorUtility.SetDirty(_catalog); AssetDatabase.SaveAssetIfDirty(_catalog);
            }
            var result = entry.measurement;
            if (result == null || result.samples.Count == 0) return;
            EditorGUILayout.LabelField($"{result.motionKey} · {result.duration:F3}s · 루트 {result.rootDisplacement:F3}m / 경로 {result.rootPathLength:F3}m / 회전 {result.rootYaw:F1}°");
            EditorGUILayout.LabelField($"첫 판정 전 {result.beforeFirstHit:F3}s · 마지막 판정 뒤 길이 {result.afterLastHit:F3}s · 최고 칼끝 속도 {result.peakTipSpeed:F2}m/s @ {result.peakTipTime:F3}s");
            if (_stale.TryGetValue(entry, out bool stale) && stale)
                EditorGUILayout.HelpBox("원본/설정이 바뀌었습니다. 저장된 궤적과 현재 포즈는 일치하지 않을 수 있습니다. 재분석하세요.", MessageType.Warning);
            using (new EditorGUILayout.HorizontalScope())
            {
                _showTrajectory = GUILayout.Toggle(_showTrajectory, "무기 궤적");
                _showHitboxes = GUILayout.Toggle(_showHitboxes, "활성 HitBox");
                _cameraYaw = EditorGUILayout.Slider("시점", _cameraYaw, -180, 180);
                _cameraDistance = EditorGUILayout.Slider("거리", _cameraDistance, 1.5f, 8);
            }
            bool original = EditorGUILayout.Toggle("원본 Clip 전체 1배속", _showOriginal);
            if (original != _showOriginal) { _showOriginal = original; _sampleIndex = 0; }
            var display = result;
            if (_showOriginal)
            {
                int index = EditorGUILayout.Popup("원본 Clip", _originalClipIndex, result.clips.Select(c => c.name + " / " + c.layer).ToArray());
                if (index != _originalClipIndex) { _originalClipIndex = index; _originalSet = null; _sampleIndex = 0; }
                var clip = result.clips[_originalClipIndex];
                if (_originalSet == null)
                {
                    _originalSet = MotionSet.CreateAuthored("원본 Clip 비교");
                    var source = AssetDatabase.LoadAllAssetsAtPath(clip.source.path).OfType<AnimationClip>().First(c => CombatMotionAnalysis.Identify(c).localFileId == clip.source.localFileId);
                    _originalSet.motions.Add(new Motion { motionClip = source });
                }
                display = new CombatMotionMeasurement { samples = clip.originalClipSamples, duration = _originalSet.TotalDuration };
            }
            _sampleIndex = EditorGUILayout.IntSlider("표본", _sampleIndex, 0, display.samples.Count - 1);
            var sample = display.samples[_sampleIndex];
            EditorGUILayout.LabelField($"{(_showOriginal ? "원본" : "저작 타임라인")} {sample.time:F4}s / Clip {sample.clipTime:F4}s · 노랑=칼끝, 청록=중간, 흰색=밑동, 빨강=타격");
            Rect preview = GUILayoutUtility.GetRect(400, 350, GUILayout.ExpandWidth(true));
            Run(() => DrawPreview(preview, display, sample));
            if (!_showOriginal) { DrawTimeline(result); DrawComparison(result); }
            foreach (var window in result.windows.Where(w => w.kind == "Hit"))
                EditorGUILayout.LabelField($"P{window.hitPhaseIndex} {window.start:F3}–{window.end:F3}s · 간격 {window.gapFromPrevious:F3}s · {window.direction} · 높이 {window.boundsMin.y:F2}–{window.boundsMax.y:F2}m · XZ 크기 {(window.boundsMax - window.boundsMin).x:F2}×{(window.boundsMax - window.boundsMin).z:F2}m");
            foreach (string diagnostic in result.diagnostics) EditorGUILayout.HelpBox(diagnostic, MessageType.Info);
            EditorGUILayout.HelpBox(string.Join("\n", result.limitations), MessageType.None);
        }

        private void SelectEntry(int index) { ReleasePreview(); _selected = index; _sampleIndex = 0; _boundaryIndex = 0; _originalClipIndex = 0; _originalSet = null; _error = null; }
        private void Run(Action action)
        {
            try { action(); }
            catch (ExitGUIException) { throw; }
            catch (Exception exception) { _error = exception.Message; Repaint(); }
        }

        private void EnsurePreview()
        {
            if (_sampler != null) return;
            _sampler = new CombatMotionSampler(_catalog);
            var cameraObject = new GameObject("분석 카메라", typeof(Camera));
            SceneManager.MoveGameObjectToScene(cameraObject, _sampler.Root.scene);
            _camera = cameraObject.GetComponent<Camera>();
            _camera.enabled = false; _camera.scene = _sampler.Root.scene;
            _camera.clearFlags = CameraClearFlags.SolidColor; _camera.backgroundColor = new Color(0.12f, 0.13f, 0.14f);
            _camera.nearClipPlane = 0.01f; _camera.farClipPlane = 100; _camera.fieldOfView = 40;
            var lightObject = new GameObject("분석 조명", typeof(Light));
            SceneManager.MoveGameObjectToScene(lightObject, _sampler.Root.scene);
            var light = lightObject.GetComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.3f;
            lightObject.transform.rotation = Quaternion.Euler(40, -30, 0);
        }

        private void DrawPreview(Rect rect, CombatMotionMeasurement result, MotionPoseSample sample)
        {
            if (Event.current.type != EventType.Repaint) return;
            EnsurePreview();
            _sampler.ShowPose(_showOriginal ? _originalSet : CombatMotionAnalysis.Resolve(_catalog, Entry, out _, out _, out _).motionSet, sample);
            Vector3 focus = sample.rootPosition + Vector3.up;
            _camera.transform.position = focus + Quaternion.Euler(12, _cameraYaw, 0) * new Vector3(0, 0, -_cameraDistance);
            _camera.transform.LookAt(focus);
            int width = Mathf.Max(1, (int)rect.width), height = Mathf.Max(1, (int)rect.height);
            if (_texture == null || _texture.width != width || _texture.height != height)
            {
                if (_texture != null) DestroyImmediate(_texture);
                _texture = new RenderTexture(width, height, 24);
            }
            _camera.targetTexture = _texture; _camera.aspect = width / (float)height; _camera.Render();
            GUI.DrawTexture(rect, _texture, ScaleMode.StretchToFill, false);
            Handles.BeginGUI();
            if (_showTrajectory)
                for (int i = 1; i < result.samples.Count; i++)
                {
                    var before = result.samples[i - 1]; var after = result.samples[i];
                    bool hit = result.windows.Any(w => w.kind == "Hit" && after.time >= w.start && after.time <= w.end);
                    DrawLine(rect, before.bladeTip, after.bladeTip, hit ? Color.red : Color.yellow);
                    DrawLine(rect, before.bladeMiddle, after.bladeMiddle, Color.cyan);
                    DrawLine(rect, before.bladeBase, after.bladeBase, Color.gray);
                }
            DrawLine(rect, sample.bladeBase, sample.bladeTip, Color.white, 3);
            if (_showHitboxes)
            {
                var groups = result.windows.Where(w => w.kind == "Hit" && sample.time >= w.start && sample.time <= w.end).SelectMany(w => w.groups).ToHashSet();
                foreach (var box in sample.hitboxes.Where(b => groups.Contains(b.group))) DrawBounds(rect, CombatMotionSampler.GetBounds(box));
            }
            Handles.EndGUI();
            GUI.Label(new Rect(rect.x + 8, rect.y + 8, rect.width - 16, 20), $"재현 칼끝 오차: {_sampler.GetTipError(sample) * 1000:F2} mm / HitBox 표시는 표본 AABB");
        }

        private void DrawLine(Rect rect, Vector3 from, Vector3 to, Color color, float width = 1)
        {
            Vector3 a = _camera.WorldToViewportPoint(from), b = _camera.WorldToViewportPoint(to);
            if (a.z <= 0 || b.z <= 0 || a.x < 0 || a.x > 1 || b.x < 0 || b.x > 1 || a.y < 0 || a.y > 1 || b.y < 0 || b.y > 1) return;
            Handles.color = color;
            Handles.DrawAAPolyLine(width, new Vector3(rect.x + a.x * rect.width, rect.yMax - a.y * rect.height),
                new Vector3(rect.x + b.x * rect.width, rect.yMax - b.y * rect.height));
        }

        private void DrawBounds(Rect rect, Bounds bounds)
        {
            var corners = new Vector3[8];
            for (int i = 0; i < 8; i++) corners[i] = new Vector3((i & 1) == 0 ? bounds.min.x : bounds.max.x,
                (i & 2) == 0 ? bounds.min.y : bounds.max.y, (i & 4) == 0 ? bounds.min.z : bounds.max.z);
            for (int i = 0; i < 8; i++)
                for (int axis = 0; axis < 3; axis++)
                    if ((i & (1 << axis)) == 0) DrawLine(rect, corners[i], corners[i | (1 << axis)], Color.red, 2);
        }

        private void DrawTimeline(CombatMotionMeasurement result)
        {
            foreach (string kind in new[] { "Hit", "Combo", "Cancel", "Telegraph" })
            {
                Rect rect = GUILayoutUtility.GetRect(400, 22, GUILayout.ExpandWidth(true));
                GUI.Label(new Rect(rect.x, rect.y, 75, 22), kind);
                rect.xMin += 80; EditorGUI.DrawRect(rect, new Color(0.18f, 0.18f, 0.18f));
                foreach (var window in result.windows.Where(w => w.kind == kind))
                {
                    var span = new Rect(rect.x + window.start / result.duration * rect.width, rect.y,
                        Mathf.Max(2, (window.end - window.start) / result.duration * rect.width), rect.height);
                    EditorGUI.DrawRect(span, kind == "Hit" ? new Color(0.7f, 0.2f, 0.2f) : new Color(0.2f, 0.4f, 0.6f));
                    GUI.Label(span, new GUIContent(window.actions ?? $"P{window.hitPhaseIndex}", $"{window.start:F3}–{window.end:F3}s {window.source}"));
                }
                float cursor = result.samples[_sampleIndex].time / result.duration;
                EditorGUI.DrawRect(new Rect(rect.x + cursor * rect.width, rect.y, 2, rect.height), Color.white);
            }
        }

        private void DrawComparison(CombatMotionMeasurement result)
        {
            if (result.boundaries.Count == 0) return;
            _boundaryIndex = EditorGUILayout.Popup("비교 출발 경계", Mathf.Clamp(_boundaryIndex, 0, result.boundaries.Count - 1), result.boundaries.Select(p => $"{p.time:F3}s {p.label}").ToArray());
            _compareIndex = EditorGUILayout.Popup("다음 모션 시작 자세", Mathf.Clamp(_compareIndex, 0, _catalog.entries.Count - 1), _catalog.entries.Select(e => e.ability?.abilityId ?? "없음").ToArray());
            var target = _catalog.entries[_compareIndex].measurement?.boundaries.FirstOrDefault();
            if (target == null) return;
            var source = result.boundaries[_boundaryIndex];
            var pairs = source.joints.Join(target.joints, a => a.bone, b => b.bone, (a, b) => new { a, b }).ToArray();
            if (pairs.Length == 0) return;
            float rms = Mathf.Sqrt(pairs.Average(p => (p.a.position - p.b.position).sqrMagnitude));
            float angle = pairs.Average(p => Quaternion.Angle(p.a.rotation, p.b.rotation));
            EditorGUILayout.LabelField($"루트 상대 관절 {pairs.Length}개: 위치 RMS {rms * 1000:F1}mm / 평균 회전 차 {angle:F1}° · 검토용 비교, 콤보 자동 변경 없음");
            if (GUILayout.Button("비교 경계 포즈 보기")) _sampleIndex = result.samples.FindIndex(s => Mathf.Abs(s.time - source.time) < 0.00001f);
        }

        private void ReleasePreview()
        {
            _sampler?.Dispose(); _sampler = null; _camera = null;
            if (_texture != null) DestroyImmediate(_texture); _texture = null;
        }
    }
}
