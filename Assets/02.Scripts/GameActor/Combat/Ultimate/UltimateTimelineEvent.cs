using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UPlayGround.CameraSystem;
using UPlayGround.Data.Sound;
using UPlayGround.Manager;
using UPlayGround.Components;
using UPlayGround.Data.Cinematic;

namespace UPlayGround.Data
{
    public enum UltimateTimelineAnchor
    {
        Caster,
        PrimaryTarget,
        World
    }

    [Serializable]
    public abstract class UltimateTimelineEvent
    {
        [Min(0f)] public float startTime;
        [Min(0f)] public float duration;

        public float EndTime => startTime + duration;
        public virtual string DisplayName => GetType().Name;

        public abstract void Execute(UltimateRuntimeContext context);
        public virtual void Complete(UltimateRuntimeContext context) { }

        protected static Transform ResolveAnchor(
            UltimateRuntimeContext context,
            UltimateTimelineAnchor anchor)
        {
            return anchor switch
            {
                UltimateTimelineAnchor.PrimaryTarget => context?.PrimaryTarget,
                UltimateTimelineAnchor.Caster => context?.Caster != null
                    ? context.Caster.transform
                    : null,
                _ => null
            };
        }
    }

    [Serializable, MovedFrom(true, sourceAssembly: "Assembly-CSharp")]
    public sealed class UltimateSpawnVfxEvent : UltimateTimelineEvent
    {
        public GameObject prefab;
        public UltimateTimelineAnchor anchor = UltimateTimelineAnchor.Caster;
        public Vector3 worldPosition;
        public Vector3 localOffset;
        public Vector3 rotationOffset;
        public bool parentToAnchor;
        public bool destroyOnComplete = true;

        [NonSerialized] private GameObject _instance;

        public override string DisplayName => "VFX 생성";

        public override void Execute(UltimateRuntimeContext context)
        {
            if (prefab == null)
                return;

            Transform target = ResolveAnchor(context, anchor);
            Vector3 position = target != null
                ? target.TransformPoint(localOffset)
                : worldPosition + localOffset;
            Quaternion rotation = (target != null ? target.rotation : Quaternion.identity)
                                  * Quaternion.Euler(rotationOffset);

            ICinematicStageService stageService = Svc.CinematicStage;
            CinematicStageTicket stageTicket = context?.StageTicket ?? default;
            bool useStage = stageTicket.IsValid
                            && stageService != null
                            && stageService.ActiveTicket == stageTicket;
            if (useStage)
            {
                position = stageService.StageTransform.MultiplyPoint3x4(position);
                rotation = stageService.StageTransform.rotation * rotation;
            }

            _instance = UnityEngine.Object.Instantiate(
                prefab,
                position,
                rotation,
                parentToAnchor && !useStage ? target : null);

            if (useStage)
            {
                CinematicStageRuntimeUtility.SetLayerRecursively(
                    _instance,
                    "UltimateVFX");
                stageService.RegisterTransient(stageTicket, _instance);
            }
        }

        public override void Complete(UltimateRuntimeContext context)
        {
            if (destroyOnComplete && _instance != null)
                UnityEngine.Object.Destroy(_instance);
            _instance = null;
        }
    }

    [Serializable, MovedFrom(true, sourceAssembly: "Assembly-CSharp")]
    public sealed class UltimateSoundEvent : UltimateTimelineEvent
    {
        public AudioClip clip;
        public SoundBusType bus = SoundBusType.SFX;
        public UltimateTimelineAnchor anchor = UltimateTimelineAnchor.Caster;
        [Range(0f, 1f)] public float volume = 1f;
        public bool spatial = true;

        public override string DisplayName => bus == SoundBusType.Voice ? "보이스" : "사운드";

        public override void Execute(UltimateRuntimeContext context)
        {
            Transform target = ResolveAnchor(context, anchor);
            Vector3? position = spatial && target != null ? target.position : null;
            Svc.Sound?.PlayClip(clip, bus, position, volume);
        }
    }

    [Serializable, MovedFrom(true, sourceAssembly: "Assembly-CSharp")]
    public sealed class UltimateTimeScaleEvent : UltimateTimelineEvent
    {
        [Range(0.01f, 1f)] public float timeScale = 0.2f;
        [NonSerialized] private int _requestId = -1;

        public override string DisplayName => "타임스케일";

        public override void Execute(UltimateRuntimeContext context)
        {
            Complete(context);

            if (duration > 0f && Svc.GameTime != null)
                _requestId = Svc.GameTime.Request(timeScale);
        }

        public override void Complete(UltimateRuntimeContext context)
        {
            if (_requestId < 0)
                return;

            Svc.GameTime?.Release(_requestId);
            _requestId = -1;
        }
    }

    [Serializable, MovedFrom(true, sourceAssembly: "Assembly-CSharp")]
    public sealed class UltimateCameraEffectEvent : UltimateTimelineEvent
    {
        public List<CameraEffectData> effects = new();
        [NonSerialized] private readonly List<ICameraEffect> _handles = new();

        public override string DisplayName => "카메라 효과";

        public override void Execute(UltimateRuntimeContext context)
        {
            _handles.Clear();
            if (CameraManager.Instance == null)
                return;

            foreach (CameraEffectData effect in effects)
            {
                if (effect != null)
                    _handles.Add(CameraManager.Instance.PlayEffect(effect));
            }
        }

        public override void Complete(UltimateRuntimeContext context)
        {
            foreach (ICameraEffect handle in _handles)
                CameraManager.Instance?.StopEffect(handle);
            _handles.Clear();
        }
    }

    [Serializable, MovedFrom(true, sourceAssembly: "Assembly-CSharp")]
    public sealed class UltimateCameraShakeEvent : UltimateTimelineEvent
    {
        public CameraShakeData shake;
        [Min(0f)] public float strength = 1f;
        public UltimateTimelineAnchor anchor = UltimateTimelineAnchor.PrimaryTarget;

        public override string DisplayName => "카메라 흔들림";

        public override void Execute(UltimateRuntimeContext context)
        {
            Transform target = ResolveAnchor(context, anchor);
            CameraManager.Instance?.StartShake(
                shake,
                Vector3.zero,
                strength,
                target != null ? target.position : null);
        }
    }

    [Serializable, MovedFrom(true, sourceAssembly: "Assembly-CSharp")]
    public sealed class UltimateDamageWindowEvent : UltimateTimelineEvent
    {
        public override string DisplayName => "데미지 윈도우";

        public override void Execute(UltimateRuntimeContext context)
        {
            context?.Caster?.GetCombat()?.SetEnableCollision(true);
        }

        public override void Complete(UltimateRuntimeContext context)
        {
            context?.Caster?.GetCombat()?.SetEnableCollision(false);
        }
    }

    [Serializable, MovedFrom(true, sourceAssembly: "Assembly-CSharp")]
    public sealed class UltimateCustomCallbackEvent : UltimateTimelineEvent
    {
        public string callbackName;
        public bool requireReceiver;

        public override string DisplayName => "커스텀 콜백";

        public override void Execute(UltimateRuntimeContext context)
        {
            if (context?.Caster == null || string.IsNullOrWhiteSpace(callbackName))
                return;

            context.Caster.SendMessage(
                callbackName,
                context,
                requireReceiver
                    ? SendMessageOptions.RequireReceiver
                    : SendMessageOptions.DontRequireReceiver);
        }
    }

    [Serializable]
    public sealed class UltimateStageEnterEvent : UltimateTimelineEvent
    {
        [Tooltip("비워두면 UltimateSequenceAsset의 연출 스테이지를 사용합니다.")]
        public CinematicStageSO stageOverride;

        public override string DisplayName => "연출 스테이지 진입";

        public override void Execute(UltimateRuntimeContext context)
        {
            if (context?.Caster == null || context.StageTicket.IsValid)
                return;

            CinematicStageSO stage = stageOverride != null
                ? stageOverride
                : context.Asset?.cinematicStage?.stage;
            GameObject target = context.PrimaryTarget != null
                ? (context.PrimaryTarget.GetComponentInParent<GameActor>()?.gameObject
                   ?? context.PrimaryTarget.gameObject)
                : null;
            if (CinematicStageRuntimeUtility.TryEnter(
                    stage,
                    context.Caster,
                    context.Caster.gameObject,
                    target,
                    out CinematicStageTicket ticket))
            {
                context.StageTicket = ticket;
            }
        }

        public override void Complete(UltimateRuntimeContext context)
        {
            if (duration <= 0f || context == null || !context.StageTicket.IsValid)
                return;

            Svc.CinematicStage?.Exit(
                context.StageTicket,
                CinematicStageExitReason.Completed);
            context.StageTicket = default;
        }
    }

    [Serializable]
    public sealed class UltimateStageExitEvent : UltimateTimelineEvent
    {
        public override string DisplayName => "연출 스테이지 복귀";

        public override void Execute(UltimateRuntimeContext context)
        {
            if (context == null || !context.StageTicket.IsValid)
                return;

            Svc.CinematicStage?.Exit(
                context.StageTicket,
                CinematicStageExitReason.Completed);
            context.StageTicket = default;
        }
    }
}
