using UnityEngine;

namespace UPlayGround.Animation
{
    /// <summary>
    /// MotionEventExecutor가 모델(자식 오브젝트)에 붙었을 때 이벤트 타깃으로 사용할
    /// 루트 GameObject를 제공한다. 모션 이벤트들은 타깃에서 액터 컴포넌트를 찾으므로,
    /// 액터 루트(예: GameActor)가 이 인터페이스를 구현해야 부모로 올바르게 해석된다.
    /// </summary>
    public interface IMotionEventTargetRoot
    {
        GameObject EventTargetRoot { get; }
    }
}
