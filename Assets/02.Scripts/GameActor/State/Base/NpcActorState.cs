using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// NPC 전용 State 베이스.
    /// PlayerActorState / EnemyActorState 와 동일한 레이어 역할.
    /// NpcMovementController와 NpcActor 참조를 미리 캐싱합니다.
    /// </summary>
    public abstract class NpcActorState : GameActorState
    {
        protected NpcMovementController npcController;
        protected NpcActor npcActor;

        protected NpcActorState(NpcMovementController controller) : base(controller)
        {
            npcController = controller;
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            npcActor = gameActor as NpcActor;
        }
    }
}
