using UnityEngine;
using UPlayGround.Data.World;

namespace UPlayGround.Gameplay.World.LivingWorld
{
    public sealed partial class AmbientFaunaHabitat
    {
        [SerializeField] private AmbientFaunaQualitySO _quality;
        [SerializeField, Range(0, 2)] private int _qualityTier = 1;
        [SerializeField] private bool _followSettingsQuality = true;
        private static float _nextBudgetTime;
        private static int _budgetFrame = -1;
        public static UnityEngine.Camera BudgetCamera { get; internal set; }
        public static float EstimatedScreenFraction { get; private set; }

        /// <summary>실행 중 품질 변경도 같은 예약 상한으로 처리한다.</summary>
        public void SetQualityTier(int tier) { _followSettingsQuality = false; _qualityTier = Mathf.Clamp(tier, 0, 2); _nextBudgetTime = 0; }

        private static void SelectBudget()
        {
            if (_budgetFrame == Time.frameCount || Time.time < _nextBudgetTime) return;
            _budgetFrame = Time.frameCount;
            float interval = 0.2f, screenLimit = 1;
            for (int i = 0; i < Hosts.Count; i++)
            {
                var host = Hosts[i];
                if (host._quality == null || !host.isActiveAndEnabled || !host._isRegionActive) continue;
                interval = Mathf.Max(0.05f, host._quality.EvaluationSeconds);
                screenLimit = Mathf.Min(screenLimit, host._quality.GetTier(host._qualityTier).ScreenFraction);
            }
            _nextBudgetTime = Time.time + interval;
            ReadGlobalBudget(out _, out _, out int capacityLimit, out int groupLimit);
            var groups = AmbientFaunaRendererFeature.Groups;
            // 작은 등록 목록만 제자리 정렬하여 프레임별 임시 배열을 만들지 않는다.
            for (int i = 1; i < groups.Count; i++)
            {
                var group = groups[i];
                int j = i - 1;
                while (j >= 0 && GetPriority(group) > GetPriority(groups[j])) { groups[j + 1] = groups[j]; j--; }
                groups[j + 1] = group;
            }
            int capacity = 0, count = 0;
            EstimatedScreenFraction = 0;
            for (int i = 0; i < groups.Count; i++)
            {
                var group = groups[i];
                if (group == null || group.Profile == null) continue;
                group.CheckBakedGeometry();
                float occupancy = EstimateOccupancy(group);
                bool wants = group.IsReady && group.CanActivateForBudget(BudgetCamera)
                    && group.GetHabitatDensity() > 0.001f;
                bool allowed = wants && count < groupLimit && group.Capacity <= capacityLimit - capacity
                    && EstimatedScreenFraction + occupancy <= screenLimit;
                group.IsBudgetEligible = allowed;
                if (!allowed) continue;
                capacity += group.Capacity; count++;
                EstimatedScreenFraction += occupancy;
            }
        }

        private static float GetPriority(AmbientFaunaGroup group)
        {
            if (group == null || group.Profile == null) return float.MinValue;
            float distance = BudgetCamera != null ? group.WorldBounds.SqrDistance(BudgetCamera.transform.position) : 0;
            return group.Profile.Priority * 10000f - distance;
        }

        private static float EstimateOccupancy(AmbientFaunaGroup group)
        {
            if (BudgetCamera == null) return 0;
            float distance = Mathf.Max(group.Profile.NearFade.y, Mathf.Sqrt(group.WorldBounds.SqrDistance(BudgetCamera.transform.position)));
            float height = BudgetCamera.orthographic ? 2 * BudgetCamera.orthographicSize
                : 2 * distance * Mathf.Tan(BudgetCamera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            float diameter = group.Profile.SizeRange.y * group.Profile.Mesh.bounds.size.magnitude;
            return group.Capacity * diameter * diameter / Mathf.Max(0.01f, height * height * BudgetCamera.aspect);
        }
    }
}
