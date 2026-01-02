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
        // 1. 제자리 회전 중일 때 (Slerp 동기화 로직)
        if (_isTurningInPlace && _currentTurnState != null)
        {
            float progress = Mathf.Clamp01(_currentTurnState.NormalizedTime);
            currentRotation = Quaternion.Slerp(_startRotation, _targetRotation, progress);

            if (progress >= 0.99f)
            {
                _isTurningInPlace = false;
                _currentTurnState = null;
                Animancer.Play(Locomotion, 0.2f); 
            }
            return;
        }
        
        float speed = Motor.Velocity.magnitude;

        // 2. 급격한 방향 전환 시 Turn In Place 트리거 (정지 혹은 이동 중 급회전)
        if (_lookInputVector.sqrMagnitude > 0f)
        {
            float angle = Vector3.SignedAngle(Motor.CharacterForward, _lookInputVector, Motor.CharacterUp);
            float absAngle = Mathf.Abs(angle);
            // 정지 상태이거나, 이동 중이라도 각도가 급격히(예: 135도 이상) 변할 때 실행
            bool isStationaryTurn = speed < 0.1f && absAngle > TurnThresholdAngle;
            bool isQuickTurn = speed >= 0.1f && absAngle > 40; // 이동 중 급회전(Pivot) 조건

            if (isStationaryTurn || isQuickTurn)
            {
                PlayTurnInPlace(angle, speed);
                return;
            }
        }

        // 3. 일반 부드러운 회전
        if (_lookInputVector.sqrMagnitude > 0f)
        {
            Quaternion targetRot = Quaternion.LookRotation(_lookInputVector, Motor.CharacterUp);
            currentRotation = Quaternion.Slerp(currentRotation, targetRot, 1f - Mathf.Exp(-RotationSpeed * deltaTime));
        }
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