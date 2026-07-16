using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// 대상 모델을 일시적으로 숨기는 이벤트
    /// startTime에 Renderer를 끄고, endTime에 다시 켬
    /// SetActive는 MonoBehaviour를 비활성화시켜 UpdateTimeline이 멈추므로 절대 사용 금지
    /// </summary>
    [Serializable]
    [MotionEventMeta("HideTarget", Category = "Utility", CategoryOrder = 40,
        Description = "대상을 숨기거나 표시합니다.",
        Aliases = new[] { "hide", "visible", "렌더", "숨김" },
        Icon = "👁", Color = new[] { 0.60f, 0.60f, 0.65f })]
    [MovedFrom(true, sourceAssembly: "Assembly-CSharp")]
    public class HideTargetEvent : MotionEventBase
    {
        // 숨길 자식 오브젝트 이름. 비어있으면 target의 모든 Renderer를 토글
        public string targetObjectName;

        public override string GetDisplayName() => "Hide Target";

        public override string GetShortLabel()
        {
            return string.IsNullOrEmpty(targetObjectName)
                ? "Hide: (All Renderers)"
                : $"Hide: {targetObjectName}";
        }

        public override void Execute(GameObject target) => SetVisible(target, false);

        public override void OnCompleteEvent(GameObject target) => SetVisible(target, true);

        private void SetVisible(GameObject target, bool visible)
        {
            if (target == null) return;

            if (string.IsNullOrEmpty(targetObjectName))
            {
                // 이름 미지정 → 루트 기준 모든 Renderer 토글
                foreach (var r in target.GetComponentsInChildren<Renderer>())
                    r.enabled = visible;
                return;
            }

            Transform found = FindTransformByName(target.transform, targetObjectName);
            if (found == null)
            {
                Debug.LogWarning($"[HideTargetEvent] '{targetObjectName}' 오브젝트를 찾을 수 없습니다.");
                return;
            }

            foreach (var r in found.GetComponentsInChildren<Renderer>())
                r.enabled = visible;
        }

        private Transform FindTransformByName(Transform parent, string name)
        {
            foreach (Transform child in parent.GetComponentsInChildren<Transform>())
            {
                if (child.name == name) return child;
            }
            return null;
        }
    }
}
