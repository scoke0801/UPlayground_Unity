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
        void Exit(in CinematicStageTicket ticket, CinematicStageExitReason reason);
    }
}
