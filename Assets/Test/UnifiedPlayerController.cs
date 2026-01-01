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

    [Header("Turn In Place Animations")]
    public TurnInPlaceSet StandTurns;

    [Header("Settings")]
    public float MaxRunSpeed = 5f;
    public float RotationSpeed = 15f;
    public float JumpSpeed = 7f;
    public float TurnThresholdAngle = 45f; // 이 각도 이상 차이나면 제자리 회전 실행

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
        if (_isTurningInPlace && _currentTurnState != null)
        {
            // [웹 베스트 프랙티스] 애니메이션 재생 진행도(0~1)를 가져옵니다.
            float progress = Mathf.Clamp01(_currentTurnState.NormalizedTime);

            // [핵심] 시작 각도에서 목표 각도까지 애니메이션 속도에 맞춰 물리 회전을 수행합니다.
            // 이를 통해 애니메이션이 끝나는 순간 물리 회전도 자연스럽게 완료됩니다.
            currentRotation = Quaternion.Slerp(_startRotation, _targetRotation, progress);

            // 애니메이션이 거의 완료(99%)되면 상태를 해제합니다.
            if (progress >= 0.99f)
            {
                _isTurningInPlace = false;
                _currentTurnState = null;
                Animancer.Play(Locomotion, 0.2f); // Idle로 부드러운 전환
            }
            return;
        }
        
// [상태 2] 캐릭터가 멈춰 있는지 확인 (Motor.Velocity 사용)
        // KCC Motor의 Velocity magnitude가 매우 낮을 때 정지 상태로 간주합니다.
        float speed = Motor.Velocity.magnitude;

        if (speed < 0.1f && _lookInputVector.sqrMagnitude > 0f)
        {
            // 현재 캐릭터의 정면(CharacterForward)과 입력 벡터 사이의 각도 계산 
            float angle = Vector3.SignedAngle(Motor.CharacterForward, _lookInputVector, Motor.CharacterUp);

            // 설정한 임계값(예: 45도)보다 각도가 클 때만 제자리 회전 실행
            if (Mathf.Abs(angle) > TurnThresholdAngle)
            {
                // 여기서 호출합니다!
                PlayTurnInPlace(angle);
                return;
            }
        }

        // [상태 3] 이동 중일 때의 일반적인 부드러운 회전
        if (_lookInputVector.sqrMagnitude > 0f)
        {
            Quaternion targetRot = Quaternion.LookRotation(_lookInputVector, Motor.CharacterUp);
            currentRotation = Quaternion.Slerp(currentRotation, targetRot, 1f - Mathf.Exp(-RotationSpeed * deltaTime));
        }
    }

    private void PlayTurnInPlace(float angle)
    {
        // 시작 방향과 목표 방향을 미리 저장합니다.
        _startRotation = Motor.TransientRotation;
        _targetRotation = Quaternion.Euler(0, angle, 0) * _startRotation;
    
        _isTurningInPlace = true;
        ClipTransition clip = GetTurnClip(angle);

        if (clip != null)
        {
            _currentTurnState = Animancer.Play(clip);
            // OnEnd 이벤트는 이제 안전장치 역할만 합니다.
            _currentTurnState.OwnedEvents.OnEnd = () => {
                _isTurningInPlace = false;
                _currentTurnState = null;
            };
        }
        
        // _isTurningInPlace = true;
        //
        // if (clip != null)
        // {
        //     var state = Animancer.Play(clip);
        //     if (state.Events(this, out AnimancerEvent.Sequence events))
        //     {
        //         events.OnEnd = () =>
        //         {
        //             _isTurningInPlace = false;
        //             Motor.SetRotation(Quaternion.LookRotation(_lookInputVector, Motor.CharacterUp));
        //             _lookInputVector = Vector3.zero;
        //         };
        //     }
        // }
        // else _isTurningInPlace = false;
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

        if (_isTurningInPlace)
        {
            currentVelocity = Vector3.zero;
        }
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