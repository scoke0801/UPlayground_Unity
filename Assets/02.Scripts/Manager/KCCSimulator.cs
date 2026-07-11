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
    ///   대신 이 클래스의 FixedUpdate에서 motor를 LocalTimeScale별로 그룹핑하여
    ///   각 그룹에 보정된 deltaTime으로 Simulate를 1회씩 호출한다.
    ///   → KinematicCharacterSystem.cs 원본을 전혀 수정하지 않아도 된다.
    /// </summary>
    [DefaultExecutionOrder(-99)] // KinematicCharacterSystem(-100) 직후 실행
    public class KCCSimulator : MonoBehaviour
    {
        // LocalTimeScale이 1.0인 모터(정상 속도)를 별도 리스트로 관리할 필요 없이
        // 그룹핑 결과로 자연스럽게 처리된다.
        private readonly Dictionary<float, List<KinematicCharacterMotor>> _groups
            = new Dictionary<float, List<KinematicCharacterMotor>>();

        private readonly List<PhysicsMover> _emptyMovers = new List<PhysicsMover>();
        private readonly Dictionary<KinematicCharacterMotor, GameActor> _actorCache = new();
        private readonly List<KinematicCharacterMotor> _staleActorCacheKeys = new();
        private readonly List<float> _emptyGroupKeys = new();

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

            // ── motor를 LocalTimeScale 값으로 그룹핑 ──
            BuildGroups(motors);

            foreach (var (scale, group) in _groups)
            {
                float scaledDt = baseDt * scale;
                // Mover는 이미 처리했으므로 빈 리스트를 전달
                // → KCS.Simulate 내부에서 mover 루프가 0회 돌아 안전하다
                KinematicCharacterSystem.Simulate(scaledDt, group, _emptyMovers);
            }

            // ── PhysicsMover 위치 확정 ──
            for (int i = 0; i < movers.Count; i++)
            {
                var mover = movers[i];
                mover.Transform.SetPositionAndRotation(mover.TransientPosition, mover.TransientRotation);
                mover.Rigidbody.position = mover.TransientPosition;
                mover.Rigidbody.rotation = mover.TransientRotation;
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

        private void BuildGroups(List<KinematicCharacterMotor> motors)
        {
            // 리스트는 재사용하되 내용만 초기화
            foreach (var list in _groups.Values)
                list.Clear();

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

                if (!_groups.TryGetValue(scale, out var list))
                {
                    list = new List<KinematicCharacterMotor>();
                    _groups[scale] = list;
                }
                list.Add(motor);
            }

            // 비어있는 그룹 제거 (이전 프레임에서 생성됐지만 더 이상 없는 scale 값)
            _emptyGroupKeys.Clear();
            foreach (var kv in _groups)
                if (kv.Value.Count == 0) _emptyGroupKeys.Add(kv.Key);
            for (int i = 0; i < _emptyGroupKeys.Count; i++)
                _groups.Remove(_emptyGroupKeys[i]);
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
