using UnityEngine;

namespace UPlayGround.Data.Actor
{
    /// <summary>비전투 NPC가 주변 전투에 보이는 거리별 반응(Watch/Cower/Flee)의 수치 묶음.</summary>
    [CreateAssetMenu(fileName = "NpcCombatReaction_", menuName = "UPlayGround/액터/NPC 전투 반응")]
    public sealed class NpcCombatReactionProfileSO : ScriptableObject
    {
        [Header("반응 반경 (m) — 어그로 중인 몬스터까지의 수평 거리")]
        [Tooltip("이 거리 안이면 하던 일을 멈추고 위협 쪽을 바라본다.")]
        [Min(0f)] public float watchRadius = 25f;

        [Tooltip("이 거리 안이면 몸을 낮추고 웅크린다.")]
        [Min(0f)] public float cowerRadius = 15f;

        [Tooltip("이 거리 안이면 위협 반대쪽으로 달아난다.")]
        [Min(0f)] public float fleeRadius = 8f;

        [Header("안정화")]
        [Tooltip("이미 해당 단계에 있을 때 반경을 이 배율만큼 넓혀 경계에서 단계가 깜빡이는 것을 막는다.")]
        [Min(1f)] public float releaseRadiusScale = 1.25f;

        [Tooltip("위협이 사라진 뒤 일과로 돌아가기까지 반응을 유지하는 시간(초).")]
        [Min(0f)] public float releaseHoldSeconds = 3f;

        [Tooltip("반응 개시 지연의 주민별 무작위 범위(초). 모두가 같은 프레임에 반응하지 않게 한다.")]
        public Vector2 onsetDelayRange = new Vector2(0.1f, 0.6f);

        [Header("Flee")]
        [Tooltip("위협으로부터 이 거리 이상 벌어지면 달리기를 멈추고 웅크린다.")]
        [Min(0f)] public float fleeSafeDistance = 30f;

        [Tooltip("MaxRunMoveSpeed 대비 도주 속도 배율.")]
        [Range(0.1f, 1.5f)] public float fleeSpeedMultiplier = 0.9f;

        [Tooltip("이 시간 동안 거의 이동하지 못하면 막힌 것으로 보고 웅크린다(초).")]
        [Min(0.1f)] public float fleeStuckSeconds = 1.2f;

        private void OnValidate()
        {
            fleeRadius = Mathf.Min(fleeRadius, cowerRadius);
            cowerRadius = Mathf.Min(cowerRadius, watchRadius);
            if (onsetDelayRange.y < onsetDelayRange.x)
                onsetDelayRange.y = onsetDelayRange.x;
        }
    }
}
