using UnityEngine;
using KinematicCharacterController;
using Animancer;
using System;

public class UnifiedPlayerController : MonoBehaviour, ICharacterController
{
    [Header("References")]
    public KinematicCharacterMotor Motor;
    public AnimancerComponent Animancer;

    [Header("Movement Animations")]
    public LinearMixerTransition Locomotion; // Idle-Walk-Run 믹서
    public ClipTransition JumpClip;

    [Header("Turn In Place Sets")]
    public TurnInPlaceSet StandTurns;   // Stand_Idle_Turn_..._InPlace 
    public TurnInPlaceSet WalkTurns;    // Walk_F_Turn_..._InPlace 
    public TurnInPlaceSet RunTurns;     // Run_F_Turn_..._InPlace 
    public TurnInPlaceSet SprintTurns;  // Sprint_F_Turn_..._InPlace

    [Header("Settings")]
    public float MaxRunSpeed = 5f;
    public float RotationSpeed = 15f;
    public float JumpSpeed = 7f;
    public float TurnThresholdAngle = 45f; // 이 각도 이상 차이나면 제자리 회전 실행
    
    [Header("Speed Thresholds")]
    public float WalkSpeedThreshold = 2.0f;
    public float RunSpeedThreshold = 5.0f;
    
    private Vector3 _moveInputVector;
    private Vector3 _lookInputVector;
    private bool _jumpRequested = false;
    private bool _isTurningInPlace = false;
    
    private Quaternion _targetRotation;
    private AnimancerState _currentTurnState;
    private Quaternion _startRotation; // 회전 시작 시점의 방향 저장

    private void Start()
    {
        // 중요: 인스펙터에 슬롯이 없으므로 코드에서 직접 연결합니다. 
        if (Motor != null) Motor.CharacterController = this;
        
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

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        // 1. 회전 애니메이션 재생 중일 때
        if (_isTurningInPlace)
        {
            // OnAnimatorMove에서 Root Motion을 처리하므로 여기서는 물리 회전 로직을 생략합니다.
            return;
        }
    
        float speed = Motor.Velocity.magnitude;

        if (_lookInputVector.sqrMagnitude > 0f)
        {
            float angle = Vector3.SignedAngle(Motor.CharacterForward, _lookInputVector, Motor.CharacterUp);
            float absAngle = Mathf.Abs(angle);

            // 이동 속도와 상관없이 각도 차이가 클 때 (Turn Threshold 이상) 호출
            if (absAngle > TurnThresholdAngle)
            {
                PlayRootMotionTurn(angle, speed);
                return;
            }
        }

        // 일반 부드러운 회전
        if (_lookInputVector.sqrMagnitude > 0f)
        {
            Quaternion targetRot = Quaternion.LookRotation(_lookInputVector, Motor.CharacterUp);
            currentRotation = Quaternion.Slerp(currentRotation, targetRot, 1f - Mathf.Exp(-RotationSpeed * deltaTime));
        }
    }
// 애니메이션의 이동/회전 정보를 KCC에 적용하는 핵심 메서드
    private void OnAnimatorMove()
    {
        if (_isTurningInPlace)
        {
            // 1. 애니메이션에서 계산된 회전 변화량 적용
            // Motor.RotateCharacter는 KCC 내부 TransientRotation을 안전하게 업데이트합니다.
            Motor.RotateCharacter(Animancer.Animator.deltaRotation * Motor.TransientRotation);

            // 2. 애니메이션에서 계산된 이동 변화량 적용
            // 일반 Turn 애니메이션에 포함된 전진/측면 이동 성분을 물리 위치에 더합니다.
            Motor.MoveCharacter(Motor.TransientPosition + Animancer.Animator.deltaPosition);
        }
    }

    private void PlayRootMotionTurn(float angle, float currentSpeed)
    {
        _isTurningInPlace = true;

        // 현재 속도에 맞는 세트 선택 (Stand, Walk, Run, Sprint)
        TurnInPlaceSet selectedSet = GetSetBySpeed(currentSpeed);
    
        // [중요] InPlace가 아닌 '일반 Turn' 애니메이션 클립을 가져와야 합니다.
        ClipTransition clip = GetTurnClipFromSet(selectedSet, angle);

        if (clip != null)
        {
            Animancer.Animator.applyRootMotion = true;
            _currentTurnState = Animancer.Play(clip);
            _currentTurnState.OwnedEvents.OnEnd = () => 
            {
                Animancer.Animator.applyRootMotion = false;
                _isTurningInPlace = false;
                _currentTurnState = null;
                _lookInputVector = Vector3.zero;
            };
        }
        else _isTurningInPlace = false;
    }
    private void PlayTurnInPlace(float angle, float currentSpeed)
    {
        _startRotation = Motor.TransientRotation;
        _targetRotation = Quaternion.Euler(0, angle, 0) * _startRotation;
        _isTurningInPlace = true;

        // [핵심] 현재 속도에 따라 애니메이션 세트 선택
        TurnInPlaceSet selectedSet = GetSetBySpeed(currentSpeed);
        ClipTransition clip = GetTurnClipFromSet(selectedSet, angle);

        if (clip != null)
        {
            _currentTurnState = Animancer.Play(clip);
            _currentTurnState.OwnedEvents.OnEnd = () => {
                _isTurningInPlace = false;
                _currentTurnState = null;
            };
        }
        else _isTurningInPlace = false;
    }
    private TurnInPlaceSet GetSetBySpeed(float speed)
    {
        if (speed < 0.1f) return StandTurns;
        if (speed <= WalkSpeedThreshold) return WalkTurns;
        if (speed <= RunSpeedThreshold) return RunTurns;
        return SprintTurns;
    }

    private ClipTransition GetTurnClipFromSet(TurnInPlaceSet set, float angle)
    {
        float absAngle = Mathf.Abs(angle);
        bool isLeft = angle < 0;

        if (absAngle > 150f) return set.Turn180;
        if (absAngle > 110f) return isLeft ? set.Left135 : set.Right135;
        if (absAngle > 70f) return isLeft ? set.Left90 : set.Right90;
        return isLeft ? set.Left45 : set.Right45;
    }

    private ClipTransition GetTurnClip(float angle)
    {
        ClipTransition clip = null;
        float absAngle = Mathf.Abs(angle);

        if (absAngle > 135f) clip = StandTurns.Turn180;
        else if (absAngle > 70f) clip = (angle < 0) ? StandTurns.Left90 : StandTurns.Right90;
        else clip = (angle < 0) ? StandTurns.Left45 : StandTurns.Right45;
        return clip;
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        if (Motor.GroundingStatus.IsStableOnGround)
        {
            float targetSpeed = _moveInputVector.magnitude * MaxRunSpeed;
            currentVelocity = Vector3.Lerp(currentVelocity, _moveInputVector * targetSpeed, 1f - Mathf.Exp(-20f * deltaTime));

            if (_jumpRequested)
            {
                currentVelocity += Motor.CharacterUp * JumpSpeed;
                _jumpRequested = false;
                Motor.ForceUnground();
            }
        }
        else
        {
            currentVelocity += Vector3.down * 20f * deltaTime;
        }

        if (_isTurningInPlace) currentVelocity = Vector3.zero;
        UpdateAnimations();
    }

    private void UpdateAnimations()
    {
        if (!Motor.GroundingStatus.IsStableOnGround)
        {
            Animancer.Play(JumpClip, 0.1f);
            return;
        }

        if (!_isTurningInPlace)
        {
            Animancer.Play(Locomotion, 0.25f);
            Locomotion.State.Parameter = Vector3.ProjectOnPlane(Motor.Velocity, Motor.CharacterUp).magnitude;
        }
    }

    #region ICharacterController 필수 구현
    public void BeforeCharacterUpdate(float deltaTime) { }
    public void PostCharacterUpdate(float deltaTime) { }
    public void AfterCharacterUpdate(float deltaTime) { }
    public void PostGroundingUpdate(float deltaTime) { }
    public bool IsColliderValidForCollisions(Collider coll) => true; // 
    public bool IsRelevantForCamera(Transform cameraTransform) => true;
    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) { }
    public void OnDiscreteCollisionDetected(Collider hitCollider) { }
    #endregion
}