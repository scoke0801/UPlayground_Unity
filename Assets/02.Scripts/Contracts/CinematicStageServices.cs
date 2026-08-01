using System;
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

    public readonly struct CinematicStageRequest
    {
        public CinematicStageRequest(
            CinematicStageSO stage,
            UnityEngine.Object owner,
            GameObject caster,
            Transform casterModelRoot,
            GameObject target = null,
            Transform targetModelRoot = null)
        {
            Stage = stage;
            Owner = owner;
            Caster = caster;
            CasterModelRoot = casterModelRoot;
            Target = target;
            TargetModelRoot = targetModelRoot;
        }

        public CinematicStageSO Stage { get; }
        public UnityEngine.Object Owner { get; }
        public GameObject Caster { get; }
        public Transform CasterModelRoot { get; }
        public GameObject Target { get; }
        public Transform TargetModelRoot { get; }
    }

    public interface ICinematicStageService : IGameService
    {
        bool IsActive { get; }
        CinematicStageTicket ActiveTicket { get; }
        Matrix4x4 StageTransform { get; }
        bool TryEnter(in CinematicStageRequest request, out CinematicStageTicket ticket);
        void RegisterTransient(in CinematicStageTicket ticket, GameObject instance);
        void Exit(in CinematicStageTicket ticket, CinematicStageExitReason reason);
    }
}
