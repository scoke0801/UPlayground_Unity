using System.Collections.Generic;
using UPlayGround.Data.Event;
using UnityEngine;

namespace UPlayGround.Animation.Editor
{
    public interface IMotionSetCatalog
    {
        Object SourceAsset { get; }
        IReadOnlyList<MotionSetSlot> Slots { get; }
        IReadOnlyList<MotionSetSlot> AssignableSlots { get; }
        MotionSetAsset Resolve(string slotId);
        bool Assign(string slotId, MotionSetAsset asset);
        MotionSetAsset CreateAndAssign(string slotId, string directory);
        void Refresh();
    }

    /// <summary>
    /// 프리뷰 대상 없이 에셋 카탈로그만 열었을 때도 선택 가능한 카탈로그 축.
    /// 예: PlayerActorAnimationMotionSet의 무기 타입.
    /// </summary>
    public interface IMotionSetCatalogVariants
    {
        IReadOnlyList<MotionPreviewAxis> Axes { get; }
        string GetSelected(string axisId);
        bool Select(string axisId, string optionId);
    }

    public readonly struct MotionSetSlot
    {
        public MotionSetSlot(string slotId, string displayName, string groupLabel)
        {
            SlotId = slotId;
            DisplayName = displayName;
            GroupLabel = groupLabel;
        }

        public string SlotId { get; }
        public string DisplayName { get; }
        public string GroupLabel { get; }
    }
}
