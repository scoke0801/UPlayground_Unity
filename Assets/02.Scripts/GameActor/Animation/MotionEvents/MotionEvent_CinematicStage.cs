using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UPlayGround.Data.Cinematic;
using UPlayGround.Manager;

namespace UPlayGround.Data.Event
{
    /// <summary>일반 MotionSet 구간에서 시전자 클론 연출 무대를 점유한다.</summary>
    [Serializable]
    [MovedFrom(true, sourceAssembly: "Assembly-CSharp")]
    [MotionEventDescriptor(
        "Cinematic Stage",
        "Camera",
        0,
        "실제 액터는 제자리에 둔 채 렌더 전용 클론 무대를 시작합니다.",
        "cinematic",
        "stage",
        "연출",
        "무대")]
    public sealed class MotionEvent_CinematicStage : MotionEventBase
    {
        public CinematicStageSO stage;
        private CinematicStageTicket _ticket;

        public override string GetDisplayName() => "Cinematic Stage";

        public override string GetShortLabel() => stage != null
            ? $"Cinematic Stage: {stage.name}"
            : "Cinematic Stage: 미지정";

        public override void Execute(GameObject target)
        {
            if (_ticket.IsValid)
                Svc.CinematicStage?.Exit(
                    _ticket,
                    CinematicStageExitReason.Replaced);
            _ticket = default;

            CinematicStageRuntimeUtility.TryEnter(
                stage,
                target,
                target,
                null,
                out _ticket);
        }

        public override void OnCompleteEvent(GameObject target)
        {
            if (!_ticket.IsValid)
                return;

            Svc.CinematicStage?.Exit(
                _ticket,
                CinematicStageExitReason.Completed);
            _ticket = default;
        }
    }
}
