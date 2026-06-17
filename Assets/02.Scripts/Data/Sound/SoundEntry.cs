using System;
using UnityEngine;

namespace UPlayGround.Data.Sound
{
    [Serializable]
    public sealed class SoundEntry
    {
        public string key;
        public AudioClip clip;
        public SoundBusType bus = SoundBusType.SFX;
        public SoundDistanceMode distanceMode = SoundDistanceMode.Logarithmic3D;

        [Range(0f, 1f)] public float volume = 1f;
        public float pitchMin = 1f;
        public float pitchMax = 1f;

        public float minDistance = 1.5f;
        public float maxDistance = 24f;
        public AnimationCurve customRolloff;
        public bool preCullByMaxDistance = true;

        public float cooldown = 0f;
        public int maxSimultaneous = 4;
        [Range(0, 256)] public int priority = 128;
    }
}
