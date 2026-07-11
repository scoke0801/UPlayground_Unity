using System.Collections;
using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.Components
{
    public sealed class UltimatePlacementContext
    {
        private GameActor _caster;
        private GameActor _target;
        private Vector3 _casterInitialPosition;
        private Quaternion _casterInitialRotation;
        private Vector3 _targetInitialPosition;
        private Quaternion _targetInitialRotation;
        private bool _restorePositions;
        private bool _captured;

        public IEnumerator Apply(
            UltimateRuntimeContext runtimeContext,
            UltimatePlacementSettings settings)
        {
            if (runtimeContext?.Caster == null || settings == null)
                yield break;

            _caster = runtimeContext.Caster;
            _target = runtimeContext.PrimaryTarget != null
                ? runtimeContext.PrimaryTarget.GetComponent<GameActor>()
                  ?? runtimeContext.PrimaryTarget.GetComponentInParent<GameActor>()
                : null;
            _restorePositions = settings.restorePositionsOnFinish;

            CaptureInitialPoses();

            Vector3 casterDestination = _casterInitialPosition;
            Quaternion casterRotation = _casterInitialRotation;
            Vector3 targetDestination = _targetInitialPosition;
            Quaternion targetRotation = _targetInitialRotation;

            if (_target != null)
            {
                if (settings.warpCaster)
                    casterDestination = _targetInitialPosition
                                        + _targetInitialRotation * settings.casterOffsetFromTarget;

                if (settings.warpPrimaryTarget)
                    targetDestination = _casterInitialPosition
                                        + _casterInitialRotation * settings.targetOffsetFromCaster;

                if (settings.faceTarget)
                {
                    Vector3 casterToTarget = targetDestination - casterDestination;
                    casterToTarget.y = 0f;
                    if (casterToTarget.sqrMagnitude > 0.0001f)
                    {
                        casterRotation = Quaternion.LookRotation(casterToTarget.normalized, Vector3.up);
                        targetRotation = Quaternion.LookRotation(-casterToTarget.normalized, Vector3.up);
                    }
                }
            }

            float duration = Mathf.Max(0f, settings.placementBlendDuration);
            if (duration <= 0f)
            {
                SetPose(_caster, casterDestination, casterRotation);
                if (_target != null)
                    SetPose(_target, targetDestination, targetRotation);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                SetPose(
                    _caster,
                    Vector3.Lerp(_casterInitialPosition, casterDestination, t),
                    Quaternion.Slerp(_casterInitialRotation, casterRotation, t));

                if (_target != null)
                {
                    SetPose(
                        _target,
                        Vector3.Lerp(_targetInitialPosition, targetDestination, t),
                        Quaternion.Slerp(_targetInitialRotation, targetRotation, t));
                }

                yield return null;
            }
        }

        public void Restore()
        {
            if (!_captured)
                return;

            if (_restorePositions)
            {
                SetPose(_caster, _casterInitialPosition, _casterInitialRotation);
                if (_target != null)
                    SetPose(_target, _targetInitialPosition, _targetInitialRotation);
            }

            _caster = null;
            _target = null;
            _captured = false;
            _restorePositions = false;
        }

        private void CaptureInitialPoses()
        {
            _casterInitialPosition = _caster.transform.position;
            _casterInitialRotation = _caster.transform.rotation;

            if (_target != null)
            {
                _targetInitialPosition = _target.transform.position;
                _targetInitialRotation = _target.transform.rotation;
            }

            _captured = true;
        }

        private static void SetPose(GameActor actor, Vector3 position, Quaternion rotation)
        {
            if (actor == null)
                return;

            if (actor.ActorController?.Motor != null)
                actor.ActorController.Motor.SetPositionAndRotation(position, rotation);
            else
                actor.transform.SetPositionAndRotation(position, rotation);
        }
    }
}
