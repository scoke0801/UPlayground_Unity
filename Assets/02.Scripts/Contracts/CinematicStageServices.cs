using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Cinematic;

namespace UPlayGround.Manager
{
    public readonly struct CinematicStageTicket : IEquatable<CinematicStageTicket>
    {
        private readonly ulong _value;

        public CinematicStageTicket(ulong value)
        {
            _value = value;
        }

        public bool IsValid => _value != 0;
        public bool Equals(CinematicStageTicket other) => _value == other._value;
        public override bool Equals(object obj) =>
            obj is CinematicStageTicket other && Equals(other);
        public override int GetHashCode() => _value.GetHashCode();
        public static bool operator ==(CinematicStageTicket left, CinematicStageTicket right) =>
            left.Equals(right);
        public static bool operator !=(CinematicStageTicket left, CinematicStageTicket right) =>
            !left.Equals(right);
    }

    /// <summary>연출 무대가 표현할 대상 액터 하나와 그 Model 루트다.</summary>
    public readonly struct CinematicStageTarget
    {
        public CinematicStageTarget(GameObject actor, Transform modelRoot)
        {
            Actor = actor;
            ModelRoot = modelRoot;
        }

        public GameObject Actor { get; }
        public Transform ModelRoot { get; }
        public bool IsValid => Actor != null;
    }

    public readonly struct CinematicStageRequest
    {
        private static readonly CinematicStageTarget[] NoTargets =
            Array.Empty<CinematicStageTarget>();

        private readonly IReadOnlyList<CinematicStageTarget> _targets;

        public CinematicStageRequest(
            CinematicStageSO stage,
            UnityEngine.Object owner,
            GameObject caster,
            Transform casterModelRoot,
            GameObject target = null,
            Transform targetModelRoot = null)
            : this(
                stage,
                owner,
                caster,
                casterModelRoot,
                target != null
                    ? new[] { new CinematicStageTarget(target, targetModelRoot) }
                    : NoTargets)
        {
        }

        public CinematicStageRequest(
            CinematicStageSO stage,
            UnityEngine.Object owner,
            GameObject caster,
            Transform casterModelRoot,
            IReadOnlyList<CinematicStageTarget> targets)
        {
            Stage = stage;
            Owner = owner;
            Caster = caster;
            CasterModelRoot = casterModelRoot;
            _targets = targets;
        }

        public CinematicStageSO Stage { get; }
        public UnityEngine.Object Owner { get; }
        public GameObject Caster { get; }
        public Transform CasterModelRoot { get; }

        /// <summary>무대에 함께 옮길 대상 전체다. 첫 항목이 주 대상이다.</summary>
        public IReadOnlyList<CinematicStageTarget> Targets => _targets ?? NoTargets;

        /// <summary>무대 배치와 크기 분류의 기준이 되는 주 대상이다.</summary>
        public GameObject Target => Targets.Count > 0 ? Targets[0].Actor : null;

        public Transform TargetModelRoot => Targets.Count > 0 ? Targets[0].ModelRoot : null;
    }

    /// <summary>
    /// 무대 진입 없이 화면만 덮었다가 걷어내는 전환 요청이다.
    /// 스폰·순간이동·상태 전환처럼 "보이면 버그로 읽히는" 순간을 가리는 데 쓴다.
    /// </summary>
    public readonly struct ScreenCoverRequest
    {
        public ScreenCoverRequest(
            CinematicStageTransitionType type,
            float coverSeconds,
            float holdSeconds,
            float revealSeconds,
            Action onCovered = null,
            Action onCompleted = null)
        {
            Type = type;
            CoverSeconds = coverSeconds;
            HoldSeconds = holdSeconds;
            RevealSeconds = revealSeconds;
            OnCovered = onCovered;
            OnCompleted = onCompleted;
        }

        public CinematicStageTransitionType Type { get; }

        /// <summary>화면을 덮는 데 걸리는 시간.</summary>
        public float CoverSeconds { get; }

        /// <summary>완전히 덮인 상태를 유지하는 시간. 가릴 처리가 여러 프레임 걸릴 때 쓴다.</summary>
        public float HoldSeconds { get; }

        /// <summary>화면을 다시 걷어내는 데 걸리는 시간.</summary>
        public float RevealSeconds { get; }

        /// <summary>화면이 완전히 덮인 순간. 가려야 할 처리를 여기서 실행한다.</summary>
        public Action OnCovered { get; }

        /// <summary>전환이 모두 끝난 순간. 중단·실패로 끝나도 반드시 호출된다.</summary>
        public Action OnCompleted { get; }

        public bool IsValid =>
            Type != CinematicStageTransitionType.None
            && (CoverSeconds > 0f || RevealSeconds > 0f);
    }

    public interface ICinematicStageService : IGameService
    {
        bool IsActive { get; }
        CinematicStageTicket ActiveTicket { get; }
        Matrix4x4 StageTransform { get; }
        bool TryEnter(in CinematicStageRequest request, out CinematicStageTicket ticket);
        bool TryResolvePresentationTransform(
            Transform source,
            out Transform presentation);
        void RegisterTransient(in CinematicStageTicket ticket, GameObject instance);
        void ShowLetterbox(UltimateLetterboxSettings settings);
        void HideLetterbox(float duration);

        /// <summary>
        /// 화면 전체를 덮었다가 걷어내는 전환만 재생한다.
        /// 다른 전환이 이미 화면을 쓰고 있으면 false를 반환하며, 이 경우 콜백은 호출되지 않는다.
        /// </summary>
        bool TryPlayScreenCover(in ScreenCoverRequest request);
        void Exit(in CinematicStageTicket ticket, CinematicStageExitReason reason);
    }
}
