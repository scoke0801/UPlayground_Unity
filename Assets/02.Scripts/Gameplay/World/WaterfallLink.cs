using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Components;

namespace UPlayGround.Gameplay.World
{
    /// <summary>폭포 ribbon·상하류 수면·지속 낙하 파문·미스트의 연결만 소유한다. 낙구 물살은 WaterfallCurrent가 맡는다.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class WaterfallLink : MonoBehaviour
    {
        public FootstepWaterSurface source;
        public FootstepWaterSurface target;
        public Transform impact;
        public ParticleSystem mist;
        [Min(0.01f)] public float impactInterval = 0.6f;
        [Min(0f)] public float impactStrength = 1.5f;
        private static readonly List<WaterfallLink> Links = new();
        public static IReadOnlyList<WaterfallLink> ActiveLinks => Links;

        private void OnEnable()
        {
            if (!Links.Contains(this)) Links.Add(this);
            if (Application.isPlaying && mist != null) mist.Play();
        }

        private void OnDisable()
        {
            Links.Remove(this);
            if (mist != null) mist.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
