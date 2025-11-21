using UnityEngine;

namespace Game.FSM
{
    public class PlayerBrain : CharacterBrain
    {
        protected override void HandleInput()
        {
            // 플레이어: 키보드/마우스 입력 -> 변수에 할당
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            
            // 카메라 기준 방향 보정 로직을 여기에 넣으세요
            // (예: Vector3 dir = Camera.main.transform.TransformDirection(new Vector3(h, 0, v))...)
            
            // 부모 클래스의 프로퍼티 값을 세팅 (InputDirection은 protected set 이나 public set 필요)
            SetInputDirection(new Vector3(h, 0, v).normalized); 

            // 공격 입력
            if (Input.GetMouseButtonDown(0)) SetAttackInput(AttackInputType.Light);
            else if (Input.GetMouseButtonDown(1)) SetAttackInput(AttackInputType.Heavy);
            
            // 점프/회피
            SetJumpInput(Input.GetKeyDown(KeyCode.Space));
            SetDodgeInput(Input.GetKeyDown(KeyCode.LeftShift));
        }
        
        // *CharacterBrain에 protected Set 메서드들을 추가해줘야 합니다.
    }
}