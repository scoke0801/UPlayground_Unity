using UnityEngine.UIElements;

namespace UPlayGround.Animation.Editor.UIToolkit.Timeline
{
    /// <summary>
    /// 타임라인의 모든 기하 렌더를 담당하는 단일 캔버스.
    /// 텍스트는 접근성과 테마 대응을 위해 일반 Label 자식으로 유지한다.
    /// </summary>
    internal sealed class TimelineTrackElement : VisualElement
    {
        readonly TimelineView _owner;

        public TimelineTrackElement(TimelineView owner)
        {
            _owner = owner;
            name = "motion-timeline-canvas";
            pickingMode = PickingMode.Position;
            AddToClassList("up-timeline-canvas");
            generateVisualContent += context => _owner.GenerateTimeline(context);
        }
    }
}
