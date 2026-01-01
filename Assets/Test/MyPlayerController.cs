using UnityEngine;
using KinematicCharacterController;
using Animancer;

public class MyPlayerController : MonoBehaviour, ICharacterController
{
    [Header("References")]
    public KinematicCharacterMotor Motor;
    public AnimancerComponent Animancer;

    [Header("Animations (v8 Transition)")]
    // 인스펙터에서 Idle, Walk, Run을 설정할 수 있는 믹서
    public LinearMixerTransition Locomotion;
    public AnimationClip JumpClip;

    [Header("Movement Settings")]
    public float MaxRunSpeed = 5f;
    public float Acceleration = 20f;
    public float RotationSpeed = 15f;
    public float Gravity = 20f;
    public float JumpSpeed = 7f;

    private Vector3 _moveInputVector;
    private Vector3 _lookInputVector;
    private bool _jumpRequested = false;

    private void Start()
    {
        Motor.CharacterController = this;
        
        // 시작 시 믹서 재생 (인스펙터에서 설정된 값 기준)
        Animancer.Play(Locomotion);
    }

    public void SetInputs(ref PlayerCharacterInputs inputs)
    {
        Vector3 moveInput = (inputs.CameraRotation * Vector3.forward * inputs.MoveAxisForward) +
                            (inputs.CameraRotation * Vector3.right * inputs.MoveAxisRight);

        _moveInputVector = Vector3.ProjectOnPlane(moveInput, Motor.CharacterUp).normalized * Mathf.Clamp01(new Vector2(inputs.MoveAxisRight, inputs.MoveAxisForward).magnitude);

        if (_moveInputVector.sqrMagnitude > 0f) _lookInputVector = _moveInputVector;
        if (inputs.JumpDown) _jumpRequested = true;
    }

    private void UpdateAnimations()
    {
        if (!Motor.GroundingStatus.IsStableOnGround)
        {
            // 공중 상태일 때 점프 애니메이션 (Fade 0.1초)
            if (JumpClip != null) Animancer.Play(JumpClip, 0.1f);
            return;
        }

        // 지상 이동 중일 때 믹서 재생 및 파라미터 업데이트
        Animancer.Play(Locomotion, 0.25f);
        
        float currentSpeed = Vector3.ProjectOnPlane(Motor.Velocity, Motor.CharacterUp).magnitude;
        
        // v8 방식: Transition의 State에 직접 파라미터 전달
        Locomotion.State.Parameter = currentSpeed;
    }

    #region KCC Callbacks (물리 로직)

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        if (_lookInputVector.sqrMagnitude > 0f && RotationSpeed > 0f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_lookInputVector, Motor.CharacterUp);
            currentRotation = Quaternion.Slerp(currentRotation, targetRotation, 1f - Mathf.Exp(-RotationSpeed * deltaTime));
        }
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        if (Motor.GroundingStatus.IsStableOnGround)
        {
            float targetSpeed = _moveInputVector.magnitude * MaxRunSpeed;
            Vector3 targetVelocity = _moveInputVector * targetSpeed;
            currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, 1f - Mathf.Exp(-Acceleration * deltaTime));

            if (_jumpRequested)
            {
                currentVelocity += Motor.CharacterUp * JumpSpeed;
                _jumpRequested = false;
                Motor.ForceUnground();
            }
        }
        else
        {
            currentVelocity += Vector3.down * Gravity * deltaTime;
        }

        UpdateAnimations();
    }
    #endregion

    #region ICharacterController 필수 구현 (에러 해결)
    public void BeforeCharacterUpdate(float deltaTime) { }
    public void PostCharacterUpdate(float deltaTime) { }
    public void AfterCharacterUpdate(float deltaTime) { }
    public void PostGroundingUpdate(float deltaTime) { }
    public bool IsColliderValidForCollisions(Collider coll) => true;
    public bool IsRelevantForCamera(Transform cameraTransform) => true;
    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) { }
    public void OnDiscreteCollisionDetected(Collider hitCollider) { }
    #endregion
}