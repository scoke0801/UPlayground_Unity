using UnityEngine;

namespace UPlayGround.Data.Config
{
    /// <summary>NPC 외곽선의 화면 크기 기준 거리와 동일 표면의 경량 셰이더를 지정한다.</summary>
    [CreateAssetMenu(fileName = "NpcOutlineLodSettings", menuName = "UPlayGround/Rendering/NPC Outline LOD")]
    public sealed class NpcOutlineLodSettingsSO : ScriptableObject
    {
        public bool enableOutlineLod = true;
        [Min(1f)] public float restoreDistance = 30f;
        [Min(1f)] public float reduceDistance = 35f;
        [Range(1f, 179f)] public float referenceFieldOfView = 60f;
        [Tooltip("lilToon 기본 Opaque. 직접 참조하여 Player 셰이더 포함을 유지한다.")]
        public Shader opaqueShader;
        [Tooltip("Hidden/lilToonCutout. 알파 클립과 그림자는 원본과 동일하게 유지한다.")]
        public Shader cutoutShader;

        /// <summary>확대 화면에서는 가까운 거리로 평가하고, 경계 구간에서는 이전 상태를 유지한다.</summary>
        public bool ShouldReduce(bool isReduced, float distanceSquared, float fieldOfView)
        {
            if (!enableOutlineLod)
                return false;

            float projectionScale = Mathf.Tan(Mathf.Clamp(fieldOfView, 1f, 179f) * Mathf.Deg2Rad * 0.5f)
                                    / Mathf.Tan(Mathf.Clamp(referenceFieldOfView, 1f, 179f) * Mathf.Deg2Rad * 0.5f);
            float threshold = isReduced ? Mathf.Max(1f, restoreDistance)
                : Mathf.Max(restoreDistance + 0.01f, reduceDistance);
            return distanceSquared * projectionScale * projectionScale > threshold * threshold;
        }

        private void OnValidate()
        {
            restoreDistance = Mathf.Max(1f, restoreDistance);
            reduceDistance = Mathf.Max(restoreDistance + 0.01f, reduceDistance);
        }
    }
}
