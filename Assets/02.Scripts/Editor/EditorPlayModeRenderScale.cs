using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UPlayGround.Editor
{
    /// <summary>
    /// 에디터 Play 모드에서는 URP Render Scale을 1.0으로 고정하는 도구.
    /// TPS처럼 화면을 가득 채우는 게임은 픽셀 수가 곧 GPU 비용이라 체감이 가장 크다.
    ///
    /// 주의: UniversalRenderPipeline.asset은 디스크에 있는 공유 에셋이다.
    /// 값만 바꾸고 복원하지 않으면 에디터용 Render Scale이 그대로 빌드에 박힐 수 있다.
    /// 따라서 Play 진입 시 원본을 저장하고, 종료 시 반드시 원복 + Dirty 플래그를 정리한다.
    /// Editor 폴더에 있으므로 빌드에는 포함되지 않는다.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorPlayModeRenderScale
    {
        private const float EditorRenderScale = 1f;

        private static float _previousRenderScale;
        private static bool _applied;
        // Apply 시점에 변형한 에셋 참조. Play 중 Quality Level이 바뀌어 활성 에셋이
        // 교체돼도, 실제로 건드린 에셋을 정확히 원복하기 위해 캐시한다.
        private static UniversalRenderPipelineAsset _appliedAsset;

        static EditorPlayModeRenderScale()
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
                Debug.LogWarning("[RenderScale] 활성 URP 에셋을 찾지 못해 적용을 건너뜀.");
                return;
            }

            _previousRenderScale = asset.renderScale;
            asset.renderScale = EditorRenderScale;
            _applied = true;
            _appliedAsset = asset;

            Debug.Log($"[RenderScale] 에디터 Play Render Scale 적용: {asset.renderScale:0.##} " +
                      $"(원본 {_previousRenderScale:0.##}) — GPU 절약용");
        }

        private static void Restore()
        {
            if (!_applied)
                return;

            // Apply 당시 변형한 에셋을 그대로 복원한다(활성 에셋 재조회 금지).
            var asset = _appliedAsset;
            if (asset != null)
            {
                asset.renderScale = _previousRenderScale;
                // 런타임 변경으로 생긴 Dirty 플래그를 지워 실수로 디스크에 저장되는 것을 방지
                EditorUtility.ClearDirty(asset);
            }

            _applied = false;
            _appliedAsset = null;
        }

    }
}
