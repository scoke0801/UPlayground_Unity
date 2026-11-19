using System;
using UnityEngine;
using UPlayGround.WorldBuilder.Data;

namespace UPlayGround.WorldBuilder.Runtime
{
    /// <summary>월드 하나의 폭포 emitter를 묶어 거리 감쇠와 근접 소리 중복 제한을 적용한다.</summary>
    public sealed class WorldWaterfallEffects : MonoBehaviour
    {
        [Serializable]
        public sealed class Emitter
        {
            public string guid;
            public Transform landing;
            public ParticleSystem mist, spray;
            public AudioSource audio;
            public ParticleSystemRenderer mistRenderer, sprayRenderer;
            public Bounds mistLocalBounds, sprayLocalBounds;
        }
        public WaterfallPreset preset;
        public Camera observer;
        public Emitter[] emitters = Array.Empty<Emitter>();
        private void OnEnable()
        {
            foreach (var emitter in emitters)
            {
                if (emitter.mistRenderer != null) emitter.mistRenderer.localBounds = emitter.mistLocalBounds;
                if (emitter.sprayRenderer != null) emitter.sprayRenderer.localBounds = emitter.sprayLocalBounds;
            }
            if (Application.isPlaying) Tick();
        }
        private void Update() => Tick();
        /// <summary>카메라가 없으면 재생을 중지하고, 같은 거리에서는 GUID 순으로 소리를 선택한다.</summary>
        public void Tick()
        {
            if (preset == null) return;
            for (int i = 0; i < emitters.Length; i++)
            {
                var emitter = emitters[i]; if (emitter.landing == null) continue;
                float distance = observer != null ? Vector3.Distance(observer.transform.position, emitter.landing.position) : float.PositiveInfinity;
                float weight = observer != null ? 1 - Mathf.InverseLerp(preset.nearDistance, preset.farDistance, distance) : 0;
                SetRate(emitter.mist, preset.mistRate * weight); SetRate(emitter.spray, preset.sprayRate * weight);
                int rank = 0;
                for (int j = 0; j < emitters.Length; j++)
                {
                    if (i == j || observer == null || emitters[j].landing == null || Vector3.Distance(emitter.landing.position, emitters[j].landing.position) > preset.soundClusterRadius) continue;
                    float other = Vector3.Distance(observer.transform.position, emitters[j].landing.position);
                    if (other < distance || (other == distance && string.CompareOrdinal(emitters[j].guid, emitter.guid) < 0)) rank++;
                }
                if (emitter.audio != null)
                {
                    emitter.audio.volume = rank < preset.maxNearbySounds ? preset.soundVolume * weight : 0;
                    if (emitter.audio.volume > 0 && !emitter.audio.isPlaying) emitter.audio.Play();
                    if (emitter.audio.volume == 0 && emitter.audio.isPlaying) emitter.audio.Stop();
                }
            }
        }
        private static void SetRate(ParticleSystem particles, float rate)
        {
            if (particles == null) return; var emission = particles.emission; emission.rateOverTime = rate;
            if (rate > 0 && !particles.isPlaying) particles.Play();
            if (rate == 0 && particles.isPlaying) particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
