using UnityEngine;

namespace Game.FSM
{
    public class PlayerBrain : CharacterBrain
    {
        private Camera _cachedCamera;

        protected virtual void Awake()
        {
            base.Awake();
            
            _cachedCamera = Camera.main;
        }
        protected override void HandleInput()
        {
            // 플레이어: 키보드/마우스 입력 -> 변수에 할당
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical"); 
            
            // 카메라 기준 방향 보정
            Transform cameraTransform = _cachedCamera.transform;
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;
    
            // Y축 성분 제거 (평면 이동만)
            forward.y = 0;
            right.y = 0;
    
            forward.Normalize();
            right.Normalize();
    
            // 카메라 기준 이동 방향 계산
            Vector3 moveDirection = (forward * v + right * h).normalized;
            SetInputDirection(moveDirection);

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