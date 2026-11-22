using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UPlayGround.Data.Rendering;
using UPlayGround.Gameplay.World.ToonFog;

namespace UPlayGround.Tool.Editor.ToonFog
{
    [CustomEditor(typeof(ToonFogProfile)), CanEditMultipleObjects]
    internal sealed class ToonFogProfileInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (targets.Length != 1) return;
            var profile = (ToonFogProfile)target;
            if (!profile.IsValid)
                EditorGUILayout.HelpBox("밀도, 최대 평가 거리, 팔레트 구간 및 높이 스케일을 확인하세요.", MessageType.Error);
            else
                EditorGUILayout.LabelField("거리 안개로 절반 투과까지의 추가 거리",
                    profile.distanceDensity > 0 && profile.fogEnabled ? $"{Mathf.Log(2) / profile.distanceDensity:0.#} m" : "무한대");
        }
    }

    [CustomEditor(typeof(ToonFogRegion)), CanEditMultipleObjects]
    internal sealed class ToonFogRegionInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            foreach (var item in targets)
                if (item is ToonFogRegion region && !region.TryGetAxes(out _, out _, out _, out _))
                    EditorGUILayout.HelpBox($"{region.name}의 Region에는 전단 없는 상자 또는 균일 스케일 구체가 필요합니다.", MessageType.Error);
        }
    }

    [CustomEditor(typeof(ToonFogLocalVolume)), CanEditMultipleObjects]
    internal sealed class ToonFogVolumeInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            foreach (var item in targets)
                if (item is ToonFogLocalVolume volume && !volume.TryGetGeometry(out _, out _))
                    EditorGUILayout.HelpBox($"{volume.name}의 국소 부피 변환을 역변환할 수 없습니다.", MessageType.Error);
        }
    }

    [CustomEditor(typeof(ToonFogWorld))]
    internal sealed class ToonFogWorldInspector : UnityEditor.Editor
    {
        private Camera _camera;
        private ToonFogRendererFeature _feature;

        private void OnEnable()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
            if (renderer != null)
                foreach (var feature in renderer.rendererFeatures)
                    if (feature is ToonFogRendererFeature fog) { _feature = fog; break; }
        }

        public override void OnInspectorGUI()
        {
            if (DrawDefaultInspector()) SceneView.RepaintAll();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("카메라 진단", EditorStyles.boldLabel);
            _camera = (Camera)EditorGUILayout.ObjectField("대상 카메라", _camera, typeof(Camera), true);
            Camera observed = _camera != null ? _camera : SceneView.lastActiveSceneView?.camera;
            if (_feature == null || !_feature.isActive)
            {
                EditorGUILayout.HelpBox("PC Renderer의 Toon Fog 기능을 확인하세요.", MessageType.Info);
                return;
            }
            if (_feature.TryGetDiagnostics(observed, out var result))
            {
                EditorGUILayout.LabelField("카메라 ID", observed.GetInstanceID().ToString());
                EditorGUILayout.LabelField("상태", result.Status.ToString());
                EditorGUILayout.LabelField("기본 프로필", result.Profile != null ? result.Profile.name : "없음");
                EditorGUILayout.LabelField("등록 / 후보 / 활성 / 상한 제외",
                    $"{result.Registered} / {result.Candidates} / {result.Active} / {result.Rejected}");
                EditorGUILayout.LabelField("지역 기여 / 상한 제외", $"{Mathf.Min(result.Regions, 4)} / {Mathf.Max(0, result.Regions - 4)}");
            }
            if (GUILayout.Button("다시 읽기")) Repaint();
        }
    }
}
