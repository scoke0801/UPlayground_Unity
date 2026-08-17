using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround.CameraSystem;
using UPlayGround.Combat;
using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.InputDefine;

namespace UPlayGround.Manager
{
    /// <summary>
    /// 독립 Camera 런타임 포트를 UPlayground의 매니저·서비스 구성에 연결한다.
    /// 다른 프로젝트는 이 파일 대신 해당 프로젝트 전용 어댑터를 제공하면 된다.
    /// </summary>
    public sealed class UPlayGroundCameraRuntimeAdapter : CameraRuntimeAdapterBase
    {
        public override UniTask<T> LoadAssetAsync<T>(
            string key,
            string owner,
            CancellationToken cancellationToken = default)
        {
            IAssetService asset = Svc.Asset;
            return asset != null
                ? asset.LoadGlobalAsync<T>(key, owner, cancellationToken)
                : UniTask.FromResult<T>(null);
        }

        public override bool IsGameplayInputActive =>
            Svc.Input is { CurrentLayer: InputLayer.Level_0 };

        public override bool IsPlayerActionInputSuppressed =>
            Svc.Input?.IsPlayerActionInputSuppressed ?? false;

        public override bool TryGetPlayerAction(string actionName, out InputAction action)
        {
            IInputService input = Svc.Input;
            if (input != null)
                return input.GetAction(InputMapNames.PlayerAction, actionName, out action);

            action = null;
            return false;
        }

        public override void RegisterPlayerAction(
            string actionName,
            Action<InputAction.CallbackContext> performed)
        {
            Svc.Input?.RegisterInputEvent(
                InputMapNames.PlayerAction,
                actionName,
                null,
                performed,
                null,
                null,
                null,
                InputLayer.Level_1);
        }

        public override void UnregisterPlayerAction(
            string actionName,
            Action<InputAction.CallbackContext> performed)
        {
            Svc.Input?.UnRegisterInputEvent(
                InputMapNames.PlayerAction,
                actionName,
                null,
                performed,
                null);
        }

        public override void SetPlayerActionInputSuppressed(bool suppressed)
        {
            Svc.Input?.SetPlayerActionInputSuppressed(suppressed);
        }

        public override void ClearBufferedInput()
        {
            Svc.Input?.InputBuffer?.Clear();
        }

        public override bool IsTargetInputSuppressed(Transform target)
        {
            IPlayerInputSuppressible suppressible = ResolveInputSuppressible(target);
            return suppressible?.IsInputSuppressed ?? false;
        }

        public override void SetTargetInputSuppressed(Transform target, bool suppressed)
        {
            ResolveInputSuppressible(target)?.SetInputSuppressed(suppressed);
        }

        public override CameraUserPreferences UserPreferences
        {
            get
            {
                ISettingsService settings = Svc.Settings;
                if (settings is not { IsLoaded: true } || settings.Data == null)
                    return CameraUserPreferences.Default;

                var data = settings.Data;
                return new CameraUserPreferences(
                    true,
                    Mathf.Clamp(data.sensitivityX, 1, 10) / 5f,
                    Mathf.Clamp(data.sensitivityY, 1, 10) / 5f,
                    data.invertY,
                    data.screenShake,
                    data.cameraShakeScale,
                    data.aimAssist,
                    data.combatCameraAutoCorrection,
                    data.combatCameraSequenceIntensity);
            }
        }

        public override Transform ActivePlayer => Svc.ActorQuery?.PlayerTransform;

        public override Transform FindActor(string actorId)
        {
            return Svc.ActorQuery?.FindActor(actorId)?.Transform;
        }

        public override bool TryResolveTarget(Component candidate, out CameraTargetInfo target)
        {
            IWorldActor actor = ResolveWorldActor(candidate);
            if (actor == null)
            {
                target = default;
                return false;
            }

            target = new CameraTargetInfo(
                actor.Transform,
                actor.IsAlive,
                (actor.ActorType & ActorType.Monster) != 0,
                IsHostileToActivePlayer(actor),
                actor.Grade);
            return true;
        }

        private static bool IsHostileToActivePlayer(IWorldActor target)
        {
            IWorldActor player = Svc.ActorQuery?.Player;
            return player is ICombatAffiliationView playerAffiliation
                   && target is ICombatAffiliationView targetAffiliation
                   && CombatRelationUtility.CanTarget(
                       playerAffiliation,
                       targetAffiliation);
        }

        public override bool TryGetSocket(
            Transform target,
            ActorSocketType socketType,
            out Transform socket)
        {
            IWorldActor actor = ResolveWorldActor(target);
            if (actor != null)
                return actor.TryGetSocket(socketType, out socket);

            socket = null;
            return false;
        }

        public override void NotifyLockOnChanged(Transform target, bool lockedOn)
        {
            IWorldActor actor = ResolveWorldActor(target);
            if (lockedOn)
                actor?.LockOn();
            else
                actor?.UnLockOn();
        }

        public override int RequestTimeScale(float scale)
        {
            return Svc.GameTime?.Request(scale) ?? -1;
        }

        public override void ReleaseTimeScale(int requestId)
        {
            Svc.GameTime?.Release(requestId);
        }

        public override void ExecuteHitStop(float duration, float timeScale)
        {
            Svc.HitStop?.Execute(duration, timeScale);
        }

        public override void StopHitStop()
        {
            Svc.HitStop?.Stop();
        }

        public override void NotifyKillCamStarted(Transform victim)
        {
            if (victim != null)
                Svc.VitalOrb?.TrySpawn(VitalOrbTrigger.KillKillCam, victim.position);
        }

        private static IWorldActor ResolveWorldActor(Component candidate)
        {
            if (candidate == null)
                return null;

            return candidate.GetComponent<IWorldActor>()
                   ?? candidate.GetComponentInParent<IWorldActor>();
        }

        private static IPlayerInputSuppressible ResolveInputSuppressible(Transform target)
        {
            return target != null
                ? target.GetComponentInParent<IPlayerInputSuppressible>()
                : null;
        }
    }
}
