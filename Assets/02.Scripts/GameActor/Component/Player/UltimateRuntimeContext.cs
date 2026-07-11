using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.Components
{
    public sealed class UltimateRuntimeContext
    {
        public PlayerActor Caster { get; internal set; }
        public Transform PrimaryTarget { get; internal set; }
        public List<Transform> Targets { get; } = new();
        public UltimateSequenceAsset Asset { get; internal set; }
        public float ElapsedTime { get; internal set; }
        public bool IsInterrupted { get; internal set; }
    }
}
