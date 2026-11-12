using UnityEditor;
using UnityEngine;
using UPlayGround.EditorTools;
using UPlayGround.Gameplay.World.LivingWorld;

namespace UPlayGround.Tool.Editor.Map
{
    /// <summary>실제 월드의 관측점·지붕·집중 감쇠를 그대로 쓰는 날씨 미리보기 창.</summary>
    public sealed class WeatherPreviewWindow : EditorWindow
    {
        private EnvironmentController _environment;
        private bool _hasPreview;
        private float _rain, _snow, _storm, _rainCloud, _cloud = 0.9f;
        private Vector2 _wind = new(0.3f, 0.1f);

        /// <summary>반복 저작용 날씨 미리보기 창을 연다.</summary>
        [UPlaygroundTool("World/날씨 미리보기")]
        public static void Open() => GetWindow<WeatherPreviewWindow>("날씨 미리보기");

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("Play Mode에서 월드의 날씨만 미리 봅니다. 창을 닫거나 시간표로 복귀하면 해제됩니다. 밤하늘은 기존 시각 치트로 밤을 선택하세요.", MessageType.Info);
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                EditorGUI.BeginChangeCheck();
                var target = (EnvironmentController)EditorGUILayout.ObjectField("환경 컨트롤러", _environment, typeof(EnvironmentController), true);
                if (EditorGUI.EndChangeCheck()) { ReleasePreview(); _environment = target; }
                if (GUILayout.Button("현재 월드 환경 찾기"))
                {
                    ReleasePreview();
                    _environment = FindFirstObjectByType<EnvironmentController>();
                }
                _rain = EditorGUILayout.Slider("비", _rain, 0, 1);
                _snow = EditorGUILayout.Slider("눈", _snow, 0, 1);
                _storm = EditorGUILayout.Slider("폭풍", _storm, 0, 1);
                _cloud = EditorGUILayout.Slider("흐림", _cloud, 0, 1);
                _rainCloud = EditorGUILayout.Slider("비구름 (강수 예고)", _rainCloud, 0, 1);
                _wind = Vector2.ClampMagnitude(EditorGUILayout.Vector2Field("바람", _wind), 1);
                using (new EditorGUI.DisabledScope(_environment == null))
                {
                    if (GUILayout.Button("맑음")) { _snow = _rain = _storm = _rainCloud = 0; _cloud = 0.1f; ApplyPreview(); }
                    if (GUILayout.Button("비 오기 직전")) { _snow = _rain = _storm = 0; _rainCloud = 1; _cloud = 0.98f; ApplyPreview(); }
                    if (GUILayout.Button("눈보라")) { _snow = 1; _rain = _storm = _rainCloud = 0; _cloud = 1; ApplyPreview(); }
                    if (GUILayout.Button("뇌설")) { _snow = 0.85f; _rain = 0; _storm = 1; _cloud = 1; ApplyPreview(); }
                    if (GUILayout.Button("뇌우")) { _snow = 0; _rain = 0.7f; _storm = 1; _cloud = 1; ApplyPreview(); }
                    if (GUILayout.Button("현재 값 적용")) ApplyPreview();
                    if (GUILayout.Button("시간표로 복귀")) ReleasePreview();
                }
            }
            if (_environment != null)
                EditorGUILayout.LabelField(_hasPreview ? "미리보기 적용 중" : "시간표 사용 중",
                    $"눈 {_environment.SnowIntensity:F2} / 섬광 {_environment.LightningIntensity:F2}");
        }

        private void ApplyPreview()
        {
            if (_environment == null || !Application.isPlaying) return;
            _environment.SetWeatherPreview(new EnvironmentSchedule.Sample
            {
                PresetId = "EditorPreview", Rain = _rain, Snow = _snow, Storm = _storm,
                Cloud = _cloud, Wetness = _rain, Wind = _wind, RainCloud = _rainCloud
            });
            _hasPreview = true;
        }

        private void ReleasePreview()
        {
            if (_hasPreview && _environment != null) _environment.ClearWeatherPreview();
            _hasPreview = false;
        }

        private void OnDisable() => ReleasePreview();
        private void OnInspectorUpdate() => Repaint();
    }
}
