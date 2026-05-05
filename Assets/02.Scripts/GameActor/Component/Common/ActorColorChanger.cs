using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace UPlayGround.Component
{
    /// <summary>
    /// GameActor에 부착하여 메테리얼 색상을 변경하기 위한 컴포넌트
    /// </summary>
    public class ActorColorChanger : MonoBehaviour
    {
        // 각 파츠의 정보를 저장하기 위한 구조체
        private struct RendererInfo
        {
            public Renderer renderer;
            public int colorPropertyId; // 해당 렌더러가 사용하는 프로퍼티 ID (_Color 또는 _BaseColor)
            public Color originalColor;
        }

        private List<RendererInfo> rendererInfos = new List<RendererInfo>();
        private MaterialPropertyBlock propBlock;
        
        private static readonly int LilToonColorId = Shader.PropertyToID("_Color");
        private static readonly int UrpColorId = Shader.PropertyToID("_BaseColor");
    
        [Header("Settings")]
        [SerializeField] private Color hitColor = Color.red;
        [SerializeField] private float flashDuration = 0.15f;

        void Awake()
        {
            propBlock = new MaterialPropertyBlock();
            
            // 처음에 모든 파츠와 그 색상을 저장합니다.
            InitializeRendererData();
        }

        // 모든 자식의 렌더러를 찾아 원래 색상을 리스트에 저장합니다.
        public void InitializeRendererData()
        {
            if (propBlock == null)
                propBlock = new MaterialPropertyBlock();

            StopAllCoroutines();
            RestoreTrackedRenderers();

            rendererInfos.Clear();
            Renderer[] renderers = GetComponentsInChildren<Renderer>();

            foreach (var r in renderers)
            {
                if (r.sharedMaterial == null) continue;
                if (r.CompareTag("Weapon")) continue;  // 추가

                string shaderName = r.sharedMaterial.shader.name;
                int targetId = -1;

                // 셰이더 이름에 "lilToon"이 포함되어 있는지 먼저 확인
                if (shaderName.Contains("lilToon"))
                {
                    // lilToon은 _Color가 메인 컬러 프로퍼티입니다.
                    targetId = LilToonColorId;
                    hitColor = new Color(0.8f, 0, 0);
                }
                // 그 외의 경우(URP Lit 등)는 _BaseColor를 우선적으로 사용
                else if (r.sharedMaterial.HasProperty(UrpColorId))
                {
                    targetId = UrpColorId;
                }
                // 만약 위 둘 다 해당되지 않지만 구형 셰이더 대응이 필요한 경우
                else if (r.sharedMaterial.HasProperty(LilToonColorId))
                {
                    targetId = LilToonColorId;
                }

                // 관리 대상 프로퍼티를 찾은 경우에만 리스트에 추가
                if (targetId != -1)
                {
                    var info = new RendererInfo
                    {
                        renderer = r,
                        colorPropertyId = targetId,
                        originalColor = r.sharedMaterial.GetColor(targetId)
                    };

                    rendererInfos.Add(info);
                    SetRendererColor(info, info.originalColor);
                }
            }
        }

        /// <summary>
        /// 하위 렌더러 파츠가 변경되었을 때 호출 필요
        /// </summary>
        public void RefreshRenderers()
        {
            InitializeRendererData();
        }

        /// <summary>
        /// 피격 시 사용
        /// </summary>
        public void OnHit(float duration = 0.0f)
        {
            if (rendererInfos.Count == 0) return;

            if(duration > 0f)
                flashDuration = duration;
            
            StopAllCoroutines();
            StartCoroutine(FlashRoutine());
        }

        IEnumerator FlashRoutine()
        {
            // 모든 파츠를 피격 색상으로 변경
            SetAllColors(hitColor, useOriginal: false);

            yield return new WaitForSeconds(flashDuration);

            // 각 파츠가 가지고 있던 원래 색상으로 복구
            SetAllColors(Color.white, useOriginal: true);
        }

        private void SetAllColors(Color targetColor, bool useOriginal)
        {
            foreach (var info in rendererInfos)
            {
                if (info.renderer == null) continue;

                Color finalColor = useOriginal ? info.originalColor : targetColor;
                SetRendererColor(info, finalColor);
            }
        }

        private void RestoreTrackedRenderers()
        {
            foreach (var info in rendererInfos)
            {
                if (info.renderer == null) continue;
                SetRendererColor(info, info.originalColor);
            }
        }

        private void SetRendererColor(RendererInfo info, Color color)
        {
            info.renderer.GetPropertyBlock(propBlock);
            propBlock.SetColor(info.colorPropertyId, color);
            info.renderer.SetPropertyBlock(propBlock);
        }
    }
}
