using System;
using UnityEngine;
using UPlayGround.Data.Sound;

namespace UPlayGround.Data.Actor
{
    public enum FootstepKind { Walk, Run, Landing }

    /// <summary>바닥과 동작별 발 디딤 소리와 환경 입자를 저작한다.</summary>
    [CreateAssetMenu(fileName = "FootstepSurface_", menuName = "UPlayGround/환경/Footstep Surface")]
    public sealed class FootstepSurfaceProfileSO : ScriptableObject
    {
        [Serializable]
        public sealed class Response
        {
            public SoundEntrySO[] sounds = Array.Empty<SoundEntrySO>();
            public GameObject effect;
            public GameObject secondaryEffect;
            [Range(0f, 1f)] public float volume = 0.7f;
            [Min(0f)] public float scale = 1f;
        }

        public Response walk = new();
        public Response run = new();
        public Response landing = new();

        /// <summary>현재 디딤의 표현 데이터를 반환한다.</summary>
        public Response GetResponse(FootstepKind kind) => kind switch
        {
            FootstepKind.Run => run,
            FootstepKind.Landing => landing,
            _ => walk
        };
    }
}
