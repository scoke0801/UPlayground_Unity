using UPlayGround.State;

namespace UPlayGround.MovementController
{
    /// <summary>
    /// NPC 전용 MovementController.
    /// EnemyMovementController와 동일한 구조로, 시작 상태만 NpcIdleState로 다릅니다.
    /// </summary>
    public class NpcMovementController : ActorMovementController
    {
        protected override void Start()
        {
            base.Start();
            TransitionToState(new NpcIdleState(this));
        }
    }
}
