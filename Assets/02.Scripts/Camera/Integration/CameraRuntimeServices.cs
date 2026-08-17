using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround.Data.EnumType;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// 카메라가 게임 설정에서 소비하는 최소 사용자 옵션.
    /// 프로젝트의 SettingsData 같은 구체 설정 모델을 Camera 모듈에 노출하지 않는다.
    /// </summary>
    public readonly struct CameraUserPreferences
    {
        public readonly bool IsAvailable;
        public readonly float SensitivityX;
        public readonly float SensitivityY;
        public readonly bool InvertY;
        public readonly bool ScreenShakeEnabled;
        public readonly float CameraShakeScale;
        public readonly bool AimAssistEnabled;
        public readonly float AutoCorrectionScale;
        public readonly float SequenceIntensity;

        public CameraUserPreferences(
            bool isAvailable,
            float sensitivityX,
            float sensitivityY,
            bool invertY,
            bool screenShakeEnabled,
            float cameraShakeScale,
            bool aimAssistEnabled,
            float autoCorrectionScale,
            float sequenceIntensity)
        {
            IsAvailable = isAvailable;
            SensitivityX = sensitivityX;
            SensitivityY = sensitivityY;
            InvertY = invertY;
            ScreenShakeEnabled = screenShakeEnabled;
            CameraShakeScale = cameraShakeScale;
            AimAssistEnabled = aimAssistEnabled;
            AutoCorrectionScale = autoCorrectionScale;
            SequenceIntensity = sequenceIntensity;
        }

        public static CameraUserPreferences Default => new CameraUserPreferences(
            false,
            1f,
            1f,
            false,
            true,
            1f,
            true,
            1f,
            1f);
    }

    /// <summary>
    /// 카메라가 락온·킬캠·군중 거리 계산에 사용하는 최소 대상 정보.
    /// GameActor/IWorldActor 같은 프로젝트 전용 타입은 조립 계층의 어댑터가 변환한다.
    /// </summary>
    public readonly struct CameraTargetInfo
    {
        public readonly Transform Root;
        public readonly bool IsAlive;
        public readonly bool IsMonster;
        public readonly bool IsHostileToPlayer;
        public readonly MonsterActorGrade Grade;

        public CameraTargetInfo(
            Transform root,
            bool isAlive,
            bool isMonster,
            bool isHostileToPlayer,
            MonsterActorGrade grade)
        {
            Root = root;
            IsAlive = isAlive;
            IsMonster = isMonster;
            IsHostileToPlayer = isHostileToPlayer;
            Grade = grade;
        }
    }

    /// <summary>
    /// Camera 모듈이 호스트 프로젝트에 요구하는 런타임 포트.
    /// 다른 프로젝트로 이식할 때는 이 인터페이스만 해당 프로젝트 방식으로 구현한다.
    /// </summary>
    public interface ICameraRuntimeAdapter
    {
        UniTask<T> LoadAssetAsync<T>(
            string key,
            string owner,
            CancellationToken cancellationToken = default)
            where T : UnityEngine.Object;

        bool IsGameplayInputActive { get; }
        bool IsPlayerActionInputSuppressed { get; }
        bool TryGetPlayerAction(string actionName, out InputAction action);
        void RegisterPlayerAction(
            string actionName,
            Action<InputAction.CallbackContext> performed);
        void UnregisterPlayerAction(
            string actionName,
            Action<InputAction.CallbackContext> performed);
        void SetPlayerActionInputSuppressed(bool suppressed);
        void ClearBufferedInput();
        bool IsTargetInputSuppressed(Transform target);
        void SetTargetInputSuppressed(Transform target, bool suppressed);

        CameraUserPreferences UserPreferences { get; }

        Transform ActivePlayer { get; }
        Transform FindActor(string actorId);
        bool TryResolveTarget(Component candidate, out CameraTargetInfo target);
        bool TryGetSocket(Transform target, ActorSocketType socketType, out Transform socket);
        void NotifyLockOnChanged(Transform target, bool lockedOn);

        int RequestTimeScale(float scale);
        void ReleaseTimeScale(int requestId);
        void ExecuteHitStop(float duration, float timeScale);
        void StopHitStop();
        void NotifyKillCamStarted(Transform victim);
    }

    /// <summary>
    /// 선택 기능을 구현하지 않아도 Camera 모듈을 조립할 수 있게 하는 안전 기본 구현.
    /// 필수 기능만 오버라이드하는 호스트 어댑터의 기반으로 사용한다.
    /// </summary>
    public abstract class CameraRuntimeAdapterBase : ICameraRuntimeAdapter
    {
        public virtual UniTask<T> LoadAssetAsync<T>(
            string key,
            string owner,
            CancellationToken cancellationToken = default)
            where T : UnityEngine.Object
            => UniTask.FromResult<T>(null);

        public virtual bool IsGameplayInputActive => true;
        public virtual bool IsPlayerActionInputSuppressed => false;
        public virtual bool TryGetPlayerAction(string actionName, out InputAction action)
        {
            action = null;
            return false;
        }

        public virtual void RegisterPlayerAction(
            string actionName,
            Action<InputAction.CallbackContext> performed)
        {
        }

        public virtual void UnregisterPlayerAction(
            string actionName,
            Action<InputAction.CallbackContext> performed)
        {
        }

        public virtual void SetPlayerActionInputSuppressed(bool suppressed)
        {
        }

        public virtual void ClearBufferedInput()
        {
        }

        public virtual bool IsTargetInputSuppressed(Transform target) => false;
        public virtual void SetTargetInputSuppressed(Transform target, bool suppressed)
        {
        }

        public virtual CameraUserPreferences UserPreferences => CameraUserPreferences.Default;

        public virtual Transform ActivePlayer => null;
        public virtual Transform FindActor(string actorId) => null;

        public virtual bool TryResolveTarget(Component candidate, out CameraTargetInfo target)
        {
            target = default;
            return false;
        }

        public virtual bool TryGetSocket(
            Transform target,
            ActorSocketType socketType,
            out Transform socket)
        {
            socket = null;
            return false;
        }

        public virtual void NotifyLockOnChanged(Transform target, bool lockedOn)
        {
        }

        public virtual int RequestTimeScale(float scale) => -1;
        public virtual void ReleaseTimeScale(int requestId)
        {
        }

        public virtual void ExecuteHitStop(float duration, float timeScale)
        {
        }

        public virtual void StopHitStop()
        {
        }

        public virtual void NotifyKillCamStarted(Transform victim)
        {
        }
    }

    /// <summary>
    /// Camera 내부에서만 사용하는 호스트 프로젝트 연결점.
    /// UPlayground 구현은 Assembly-CSharp 조립 계층에서 Configure로 주입한다.
    /// </summary>
    public static class CameraRuntimeServices
    {
        public const string LookAction = "Look";
        public const string ZoomAction = "Zoom";
        public const string LockOnAction = "LockOn";
        public const string LockOnSwitchLeftAction = "LockOnSwitchLeft";
        public const string LockOnSwitchRightAction = "LockOnSwitchRight";

        private sealed class NullCameraRuntimeAdapter : CameraRuntimeAdapterBase
        {
        }

        private static readonly ICameraRuntimeAdapter NullAdapter = new NullCameraRuntimeAdapter();
        private static ICameraRuntimeAdapter _adapter = NullAdapter;

        public static ICameraRuntimeAdapter Adapter => _adapter;
        public static bool IsConfigured => !ReferenceEquals(_adapter, NullAdapter);

        public static void Configure(ICameraRuntimeAdapter adapter)
        {
            _adapter = adapter ?? NullAdapter;
        }

        public static void Reset()
        {
            _adapter = NullAdapter;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnSubsystemRegistration()
        {
            Reset();
        }
    }
}
