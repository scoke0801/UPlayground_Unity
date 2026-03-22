using UnityEngine;

namespace UPlayGround.Data.UI
{
    [CreateAssetMenu(fileName = "DamageFloaterConfig", menuName = "UPlayGround/UI/DamageFloater Config")]
    public class DamageFloaterConfigSO : ScriptableObject
    {
        [Header("Pool")]
        [Tooltip("씬 시작 시 미리 생성할 풀 크기")]
        public int initialPoolSize = 20;

        [Header("Text Style — 플레이어 공격 (아웃고잉)")]
        public float normalFontSize   = 36f;
        public float criticalFontSize = 52f;
        public Color normalColor   = Color.white;
        public Color criticalColor = new Color(1f, 0.8f, 0f);
        public Color missColor     = new Color(0.7f, 0.7f, 0.7f);

        [Header("Text Style — 플레이어 피격 (인커밍)")]
        public float playerDamageFontSize = 40f;
        public Color playerDamageColor    = new Color(0.9f, 0.2f, 0.2f);

        [Header("Text Style — 회복")]
        public float healFontSize        = 32f;
        public Color healColor           = new Color(0.3f, 1f, 0.4f);
        public float monsterHealFontSize = 32f;
        public Color monsterHealColor    = new Color(0.6f, 1f, 0.3f);

        [Header("Motion — 이동 / 페이드")]
        [Tooltip("피격 지점 기준 시작 높이")]
        public float startHeight = 0.3f;
        [Tooltip("총 이동 높이")]
        public float riseHeight  = 1.2f;
        [Tooltip("전체 생존 시간")]
        public float lifetime    = 1.0f;

        [Tooltip("Y축 이동 커브 (x=normalized time, y=normalized height)")]
        public AnimationCurve riseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Tooltip("알파 페이드 커브")]
        public AnimationCurve fadeCurve = AnimationCurve.Linear(0.4f, 1f, 1f, 0f);

        [Header("Scale 애니메이션")]
        [Tooltip("등장 팝(overshoot) 피크 스케일. 1보다 크게 설정하면 튀어오르는 느낌")]
        public float scalePopPeak    = 1.3f;
        [Tooltip("팝 완료까지 걸리는 시간 비율 (0~1). 예: 0.12 = lifetime의 12%")]
        [Range(0.05f, 0.4f)]
        public float scalePopEndT    = 0.12f;
        [Tooltip("유지 구간 끝 시간 비율. 이후부터 축소 시작")]
        [Range(0.1f, 0.9f)]
        public float scaleShrinkStartT = 0.7f;
        [Tooltip("축소 완료 스케일 (0에 가까울수록 완전히 사라짐)")]
        [Range(0f, 0.5f)]
        public float scaleShrinkEndValue = 0.1f;

        [Header("Spread — 수평 분산")]
        public float spreadRadius = 0.25f;
    }
}
