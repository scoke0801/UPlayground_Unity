using System.Collections.Generic;
using MagicaCloth2;
using UnityEngine;
using UnityEngine.Animations;
using UPlayGround.Simulation;

namespace UPlayGround.Manager
{
    /// <summary>액터의 거리 정지 상태에 본 제약과 옷감 카메라 컬링을 연결한다.</summary>
    [DisallowMultipleComponent]
    public sealed class ActorSecondaryMotionSimulation : MonoBehaviour
    {
        private readonly List<Behaviour> _behaviours = new();
        private readonly List<Behaviour> _pausedConstraints = new();
        private readonly List<ClothCullingState> _clothStates = new();
        private ActorSimulationParticipant _participant;
        private bool _isSuspended;

        /// <summary>등록된 액터의 현재 정지 상태부터 동기화한다.</summary>
        public void Bind(ActorSimulationParticipant participant)
        {
            Unbind();
            _participant = participant;
            if (isActiveAndEnabled)
                ApplyState();
        }

        /// <summary>시뮬레이션 등록 해제 시 이 컴포넌트가 변경한 상태만 복구한다.</summary>
        public void Unbind()
        {
            Restore();
            _participant = null;
        }

        private void OnEnable()
        {
            ActorSimulationParticipant.AnyStateChanged += HandleStateChanged;
            ApplyState();
        }

        private void OnDisable()
        {
            ActorSimulationParticipant.AnyStateChanged -= HandleStateChanged;
            Restore();
        }

        private void HandleStateChanged(GameActor actor, ActorSimulationState state)
        {
            if (_participant != null && _participant.Actor == actor)
                ApplyState();
        }

        private void ApplyState()
        {
            bool shouldSuspend = _participant != null && _participant.IsSuspended;
            if (_isSuspended == shouldSuspend)
                return;
            if (!shouldSuspend)
            {
                Restore();
                return;
            }

            _isSuspended = true;
            // 모델 교체를 반영하되 매 프레임 계층 탐색은 하지 않는다.
            GetComponentsInChildren(true, _behaviours);
            for (int i = 0; i < _behaviours.Count; i++)
            {
                Behaviour behaviour = _behaviours[i];
                if (behaviour is IConstraint && behaviour.enabled)
                {
                    _pausedConstraints.Add(behaviour);
                    behaviour.enabled = false;
                }
                else if (behaviour is MagicaCloth cloth && cloth.enabled)
                {
                    _clothStates.Add(new ClothCullingState(cloth));
                    // enabled 해제는 MeshCloth를 원본 메시로 되돌린다. Keep은 화면 밖에서 자세를 보존한다.
                    cloth.SerializeData.cullingSettings.cameraCullingMode = CullingSettings.CameraCullingMode.Keep;
                    cloth.SetParameterChange();
                }
            }
            _behaviours.Clear();
        }

        private void Restore()
        {
            for (int i = 0; i < _pausedConstraints.Count; i++)
            {
                if (_pausedConstraints[i] != null)
                    _pausedConstraints[i].enabled = true;
            }
            _pausedConstraints.Clear();

            for (int i = 0; i < _clothStates.Count; i++)
                _clothStates[i].Restore();
            _clothStates.Clear();
            _isSuspended = false;
        }

        private readonly struct ClothCullingState
        {
            private readonly MagicaCloth _cloth;
            private readonly CullingSettings.CameraCullingMode _mode;

            public ClothCullingState(MagicaCloth cloth)
            {
                _cloth = cloth;
                _mode = cloth.SerializeData.cullingSettings.cameraCullingMode;
            }

            public void Restore()
            {
                if (_cloth == null)
                    return;
                _cloth.SerializeData.cullingSettings.cameraCullingMode = _mode;
                _cloth.SetParameterChange();
            }
        }
    }
}
