using System.Collections.Generic;
using KinematicCharacterController;
using UnityEngine;
using UPlayGround;
using UPlayGround.Manager;

namespace UPlayGround.Manager
{
    /// <summary>
    /// KinematicCharacterSystem의 AutoSimulation을 끄고,
    /// 우리가 직접 Simulate를 호출하여 액터별 LocalTimeScale을 deltaTime에 반영한다.
    ///
    /// 동작 원리:
    ///   KCS.Simulate(deltaTime, motors, movers) 는 모든 motor에 동일한 deltaTime을 넘긴다.
    ///   AutoSimulation = false 로 설정하면 KCS.FixedUpdate가 Simulate를 호출하지 않는다.
    ///   대신 이 클래스가 원본 motor 순서를 보존한 채 Phase 1은 base deltaTime,
    ///   Phase 2는 액터별 보정 deltaTime으로 직접 호출한다.
    ///   → KinematicCharacterSystem.cs 원본을 전혀 수정하지 않아도 된다.
    /// </summary>
    [DefaultExecutionOrder(-99)] // KinematicCharacterSystem(-100) 직후 실행
    public class KCCSimulator : MonoBehaviour
    {
        private readonly List<KinematicCharacterMotor> _activeMotors = new();
        private readonly Dictionary<KinematicCharacterMotor, float> _motorScales = new();
        private readonly Dictionary<KinematicCharacterMotor, GameActor> _actorCache = new();
        private readonly List<KinematicCharacterMotor> _staleActorCacheKeys = new();

        private void Awake()
        {
            DisableAutoSimulation();
        }

        private void OnEnable()
        {
            DisableAutoSimulation();
        }

        private void DisableAutoSimulation()
        {
            KinematicCharacterSystem.EnsureCreation();
            if (KinematicCharacterSystem.Settings != null)
            {
                // AutoSimulation을 끄면 KCS.FixedUpdate가 Simulate를 호출하지 않는다.
                // 이 컴포넌트가 대신 제어권을 가진다.
                KinematicCharacterSystem.Settings.AutoSimulation = false;
            }
        }

        private void OnDestroy()
        {
            // 플레이 중 스크립트 리컴파일로 파괴될 때 AutoSimulation을 다시 켜면
            // KinematicCharacterSystem과 KCCSimulator가 동시에 Simulate를 호출할 수 있다.
            if (!Application.isPlaying && KinematicCharacterSystem.Settings != null)
                KinematicCharacterSystem.Settings.AutoSimulation = true;
        }

        private void FixedUpdate()
        {
            float baseDt = Time.deltaTime;
            var motors  = KinematicCharacterSystem.CharacterMotors;
            var movers  = KinematicCharacterSystem.PhysicsMovers;

            PruneInvalidMotors(motors);

            if (KinematicCharacterSystem.Settings.Interpolate)
                KinematicCharacterSystem.PreSimulationInterpolationUpdate(baseDt);

            // ── PhysicsMover는 LocalTimeScale 개념이 없으므로 baseDt로 선처리 ──
            for (int i = 0; i < movers.Count; i++)
                movers[i].VelocityUpdate(baseDt);

            // 원본 CharacterMotors 순서를 보존한 단일 스텝 스냅샷.
            BuildActiveMotorSnapshot(motors);

            // KCC의 전역 시뮬레이션 장벽을 보존한다.
            // 모든 Motor Phase 1이 끝난 뒤 Mover 위치를 확정해야 플랫폼 이동량이
            // 같은 물리 스텝의 모든 캐릭터에 일관되게 전달된다.
            //
            // 주의: Phase 1은 LocalTimeScale이 적용되지 않은 baseDt로 돈다.
            // Phase 1이 호출하는 상태 콜백은 BeforeCharacterUpdate / PostGroundingUpdate 두 개뿐이고
            // 현재 모든 구현이 착지·이륙 같은 엣지 감지만 하며 deltaTime을 소비하지 않기 때문에 안전하다.
            // 이 콜백에서 타이머를 누적하면 히트스톱(LocalTimeScale) 중에도 정상 속도로 진행하므로,
            // 시간에 비례하는 로직은 반드시 Phase 2 쪽(UpdateVelocity/UpdateState)에 둘 것.
            for (int i = 0; i < _activeMotors.Count; i++)
            {
                _activeMotors[i].UpdatePhase1(baseDt);
            }

            // ── PhysicsMover 위치 확정 ──
            for (int i = 0; i < movers.Count; i++)
            {
                var mover = movers[i];
                mover.Transform.SetPositionAndRotation(mover.TransientPosition, mover.TransientRotation);
                mover.Rigidbody.position = mover.TransientPosition;
                mover.Rigidbody.rotation = mover.TransientRotation;
            }

            // Mover가 실제 위치로 이동한 뒤에만 모든 Motor Phase 2를 실행한다.
            for (int i = 0; i < _activeMotors.Count; i++)
            {
                var motor = _activeMotors[i];
                float scale = _motorScales[motor];
                float scaledDt = baseDt * scale;
                motor.UpdatePhase2(scaledDt);
                motor.Transform.SetPositionAndRotation(
                    motor.TransientPosition,
                    motor.TransientRotation);
            }

            if (KinematicCharacterSystem.Settings.Interpolate)
                KinematicCharacterSystem.PostSimulationInterpolationUpdate(baseDt);
        }

        private void PruneInvalidMotors(List<KinematicCharacterMotor> motors)
        {
            for (int i = motors.Count - 1; i >= 0; i--)
            {
                var motor = motors[i];
                if (motor == null)
                {
                    motors.RemoveAt(i);
                    continue;
                }

                if (motor.CharacterController == null)
                    motor.CharacterController = motor.GetComponent<ICharacterController>();

                if (motor.CharacterController == null)
                {
                    Debug.LogWarning($"[KCCSimulator] CharacterController가 없는 모터를 시뮬레이션 목록에서 제거합니다: {motor.name}", motor);
                    _actorCache.Remove(motor);
                    motors.RemoveAt(i);
                }
            }

            PruneActorCache();
        }

        private void BuildActiveMotorSnapshot(List<KinematicCharacterMotor> motors)
        {
            _activeMotors.Clear();
            _motorScales.Clear();

            for (int i = 0; i < motors.Count; i++)
            {
                var motor = motors[i];
                if (motor == null || !motor.isActiveAndEnabled || motor.CharacterController == null)
                    continue;

                if (!_actorCache.TryGetValue(motor, out var actor))
                {
                    actor = motor.GetComponent<GameActor>();
                    _actorCache.Add(motor, actor);
                }

                // 소수점 3자리 반올림: 0.05f / 0.1f 등 근사값이 다른 키로 분류되는 것 방지
                float scale = actor != null ? Mathf.Round(actor.LocalTimeScale * 1000f) / 1000f : 1f;
                _activeMotors.Add(motor);
                _motorScales.Add(motor, scale);
            }
        }

        private void PruneActorCache()
        {
            if (_actorCache.Count <= KinematicCharacterSystem.CharacterMotors.Count)
                return;

            _staleActorCacheKeys.Clear();
            foreach (var pair in _actorCache)
            {
                if (pair.Key == null || !KinematicCharacterSystem.CharacterMotors.Contains(pair.Key))
                    _staleActorCacheKeys.Add(pair.Key);
            }

            for (int i = 0; i < _staleActorCacheKeys.Count; i++)
                _actorCache.Remove(_staleActorCacheKeys[i]);
        }
    }
}
