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
        public Color criticalColor = new Color(1f, 0.8f, 0f);    // 골드
        public Color missColor     = new Color(0.7f, 0.7f, 0.7f);

        [Header("Text Style — 플레이어 피격 (인커밍)")]
        public float playerDamageFontSize = 40f;
        public Color playerDamageColor    = new Color(0.9f, 0.2f, 0.2f); // 레드

        [Header("Text Style — 회복")]
        public float healFontSize        = 32f;
        public Color healColor           = new Color(0.3f, 1f, 0.4f);   // 밝은 그린 (플레이어 힐)
        public float monsterHealFontSize = 32f;
        public Color monsterHealColor    = new Color(0.6f, 1f, 0.3f);   // 황록색 (몬스터 힐)

        [Header("Motion")]
        [Tooltip("월드 오프셋 — 피격 지점 기준 시작 높이")]
        public float startHeight = 0.3f;
        [Tooltip("총 이동 높이")]
        public float riseHeight  = 1.2f;
        [Tooltip("전체 생존 시간")]
        public float lifetime    = 1.0f;

        [Tooltip("Y축 이동 커브 (x=normalized time, y=normalized height)")]
        public AnimationCurve riseCurve  = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Tooltip("알파 페이드 커브")]
        public AnimationCurve fadeCurve  = AnimationCurve.Linear(0.4f, 1f, 1f, 0f);

        [Tooltip("스케일 팝 커브")]
        public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 0.15f, 1);

        [Header("Spread — 여러 숫자가 겹치지 않도록 수평 분산")]
        public float spreadRadius = 0.25f;
    }
}
