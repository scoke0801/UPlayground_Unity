using System;
using UnityEngine;

namespace UPlayGround
{
    /// <summary>
    /// 대화 연출 동안 액터의 자율 행동(배회·전투 판단)을 멈추고 시선을 상대에게 고정하는 계약.
    /// 대화를 누가 시작했는지(상호작용 / FlowGraph / 스토리 트리거)와 무관하게 같은 연출을 보장하려면
    /// 대화 계층이 참여자 액터에게 직접 홀드를 걸어야 한다 — 상호작용 경로만 Talk 상태로 들어가면
    /// FlowGraph로 시작된 대화에서 대상이 배회를 계속해 카메라 프레임을 벗어난다.
    /// </summary>
    public interface IDialogueStageActor
    {
        /// <summary>대화 홀드를 건다. 반환 리스를 Dispose하면 해제되며 중첩 홀드를 허용한다.</summary>
        IDisposable BeginDialogueStage(Transform lookTarget);

        /// <summary>홀드 중 시선 대상을 갱신한다. null이면 플레이어를 본다.</summary>
        void SetDialogueStageLookTarget(Transform lookTarget);
    }
}
