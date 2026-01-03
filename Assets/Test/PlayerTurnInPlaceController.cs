using System;
using UnityEngine;
using KinematicCharacterController;
using Animancer;

public class PlayerTurnInPlaceController : MonoBehaviour, ICharacterController
{
    [Header("References")]
    public KinematicCharacterMotor Motor;
    public AnimancerComponent Animancer;

    [Header("Turn In Place Sets")]
    public TurnInPlaceSet StandIdleTurns;   // Stand_Idle_Turn_..._InPlace 
    public TurnInPlaceSet CrouchTurns;      // Crouch_Idle_Turn_..._InPlace [cite: 8, 9]
    public TurnInPlaceSet WalkTurns;        // Walk_F_Turn_..._InPlace [cite: 60]

    private Vector3 _lookInputVector;
    private bool _isTurningInPlace;
    public float TurnThresholdAngle = 30f; // 회전을 시작할 최소 각도

    private void Start()
    {
        if (Motor != null)
        {
            Motor.CharacterController = this;
        }
    }

    public void SetInputs(ref PlayerCharacterInputs inputs)
    {
        Vector3 moveInput = (inputs.CameraRotation * Vector3.forward * inputs.MoveAxisForward) +
                            (inputs.CameraRotation * Vector3.right * inputs.MoveAxisRight);

        if (moveInput.sqrMagnitude > 0f)
        {
            _lookInputVector = moveInput.normalized;
        }
    }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        // 1. 이미 애니메이션 회전 중이면 물리 회전 중단
        if (_isTurningInPlace) return;

        // 2. 캐릭터가 정지 상태인지 확인 (이동 중에는 일반 회전) [cite: 35, 42, 57]
        if (Motor.Velocity.magnitude < 0.1f && _lookInputVector.sqrMagnitude > 0f)
        {
            float angle = Vector3.SignedAngle(Motor.CharacterForward, _lookInputVector, Motor.CharacterUp);

            if (Mathf.Abs(angle) > TurnThresholdAngle)
            {
                PlayMatchingTurn(angle);
                return;
            }
        }

        // 일반적인 부드러운 회전 로직 (이동 중일 때)
        if (_lookInputVector.sqrMagnitude > 0f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_lookInputVector, Motor.CharacterUp);
            currentRotation = Quaternion.Slerp(currentRotation, targetRotation, 1f - Mathf.Exp(-15f * deltaTime));
        }
    }

    private void PlayMatchingTurn(float angle)
    {
        _isTurningInPlace = true;
        float absAngle = Mathf.Abs(angle);
        TurnInPlaceSet currentSet = StandIdleTurns; // 상태에 따라 분기 가능 (Crouch 등)

        ClipTransition selectedClip = null;

        // 각도에 따른 애니메이션 선택 로직
        if (absAngle > 155f) selectedClip = currentSet.Turn180;
        else if (absAngle > 65f) selectedClip = (angle < 0) ? currentSet.Left90 : currentSet.Right90;
        else if (absAngle > 30f) selectedClip = (angle < 0) ? currentSet.Left45 : currentSet.Right45;

        if (selectedClip != null && selectedClip.Clip != null)
        {
            var state = Animancer.Play(selectedClip);
            // 애니메이션이 끝나면 회전 상태 해제
            if (state.Events(this, out AnimancerEvent.Sequence events))
            {
                events.OnEnd = ()=> _isTurningInPlace = false;
            }
        }
        else
        {
            _isTurningInPlace = false;
        }
    }

    // --- ICharacterController 필수 구현부 (생략 가능 시 생략, 위 답변 참조) ---
    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime) 
    {
        // 제자리 회전 중에는 이동 속도를 0으로 고정하거나 제한 가능
        if (_isTurningInPlace) currentVelocity = Vector3.zero;
        // ... 기존 이동 로직 ...
    }
    public void BeforeCharacterUpdate(float deltaTime) {}
    public void PostCharacterUpdate(float deltaTime) {}
    public void AfterCharacterUpdate(float deltaTime) {}
    public void PostGroundingUpdate(float deltaTime) {}
    public bool IsColliderValidForCollisions(Collider coll) => true;
    public bool IsRelevantForCamera(Transform cameraTransform) => true;
    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) {}
    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) {}
    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) {}
    public void OnDiscreteCollisionDetected(Collider hitCollider) {}
}