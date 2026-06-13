using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UPlayGround.Editor
{
    /// <summary>
    /// 에디터 Play 모드에서만 URP 그림자 비용을 낮춰 GPU 부하를 줄이는 도구.
    /// 그림자는 섀도우맵 캐스케이드마다 지오메트리를 다시 그리므로 TPS 씬에서 GPU 최대 부하원이 되기 쉽다.
    /// Shadow Distance를 줄이면 섀도우맵에 들어가는 지오메트리가 급감하고, Cascade 수를 줄이면 그림자 렌더 패스가 줄어든다.
    ///
    /// 주의: UniversalRenderPipeline.asset은 디스크의 공유 에셋이다.
    /// 값만 바꾸고 복원하지 않으면 빌드 품질에 그대로 박힌다.
    /// 따라서 Play 진입 시 원본을 저장하고, 종료 시 반드시 원복 + Dirty 플래그를 정리한다.
    /// Editor 폴더에 있으므로 빌드에는 포함되지 않는다.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorPlayModeShadowReducer
    {
        private const string EnabledPrefKey = "UPlayGround.EditorShadowReducer.Enabled";
        private const string DistancePrefKey = "UPlayGround.EditorShadowReducer.Distance";
        private const string CascadePrefKey = "UPlayGround.EditorShadowReducer.Cascade";
        private const string MenuPath = "Tools/성능/에디터 Play 그림자 줄이기";
        private const float DefaultDistance = 30f;
        private const int DefaultCascade = 2;

        // Play 진입 전 원래 값(종료 시 복원용)
        private static float _previousDistance;
        private static int _previousCascade;
        private static bool _applied;
        // Apply 시점에 변형한 에셋 참조. Play 중 Quality Level이 바뀌어 활성 에셋이
        // 교체돼도, 실제로 건드린 에셋을 정확히 원복하기 위해 캐시한다.
        private static UniversalRenderPipelineAsset _appliedAsset;

        public static bool Enabled
        {
            get => EditorPrefs.GetBool(EnabledPrefKey, true);
            set => EditorPrefs.SetBool(EnabledPrefKey, value);
        }

        public static float Distance
        {
            get => EditorPrefs.GetFloat(DistancePrefKey, DefaultDistance);
            set => EditorPrefs.SetFloat(DistancePrefKey, Mathf.Max(1f, value));
        }

        public static int Cascade
        {
            get => EditorPrefs.GetInt(CascadePrefKey, DefaultCascade);
            set => EditorPrefs.SetInt(CascadePrefKey, Mathf.Clamp(value, 1, 4));
        }

        static EditorPlayModeShadowReducer()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static UniversalRenderPipelineAsset ActiveAsset =>
            UniversalRenderPipeline.asset;

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    if (!Enabled)
                        return;
                    Apply();
                    break;

                case PlayModeStateChange.ExitingPlayMode:
                    // 적용 여부와 무관하게 안전하게 원복
                    Restore();
                    break;
            }
        }

        private static void Apply()
        {
            var asset = ActiveAsset;
            if (asset == null)
            {
                Debug.LogWarning("[ShadowReducer] 활성 URP 에셋을 찾지 못해 적용을 건너뜀.");
                return;
            }

            _previousDistance = asset.shadowDistance;
            _previousCascade = asset.shadowCascadeCount;

            asset.shadowDistance = Distance;
            asset.shadowCascadeCount = Cascade;
            _applied = true;
            _appliedAsset = asset;

            Debug.Log($"[ShadowReducer] 에디터 Play 그림자 축소: 거리 {_previousDistance:0}→{asset.shadowDistance:0}, " +
                      $"캐스케이드 {_previousCascade}→{asset.shadowCascadeCount} — GPU 절약용");
        }

        private static void Restore()
        {
            if (!_applied)
                return;

            // Apply 당시 변형한 에셋을 그대로 복원한다(활성 에셋 재조회 금지).
            var asset = _appliedAsset;
            if (asset != null)
            {
                asset.shadowDistance = _previousDistance;
                asset.shadowCascadeCount = _previousCascade;
                // 런타임 변경으로 생긴 Dirty 플래그를 지워 실수로 디스크에 저장되는 것을 방지
                EditorUtility.ClearDirty(asset);
            }

            _applied = false;
            _appliedAsset = null;
        }

        // ── 메뉴: 켜기/끄기 토글 ──────────────────────────────────────────
        [MenuItem(MenuPath, false, 102)]
        private static void ToggleEnabled()
        {
            Enabled = !Enabled;

            // 이미 Play 중이면 즉시 반영
            if (EditorApplication.isPlaying)
            {
                if (Enabled)
                    Apply();
                else
                    Restore();
            }

            Debug.Log($"[ShadowReducer] 에디터 Play 그림자 줄이기 " +
                      $"{(Enabled ? $"켜짐 (거리 {Distance:0}, 캐스케이드 {Cascade})" : "꺼짐")}");
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleEnabledValidate()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;
        }
    }
}
