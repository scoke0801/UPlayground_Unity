using UnityEngine;

namespace UPlayGround.WorldBuilder.Data
{
    /// <summary>폭포 연결 조건과 근/원거리 연출 예산을 SO로 공유한다.</summary>
    [CreateAssetMenu(menuName = "UPlayGround/WorldBuilder/Waterfall Preset")]
    public sealed class WaterfallPreset : ScriptableObject
    {
        public float minimumDrop = 6, poolMargin = 1, socketTolerance = .05f;
        public Material curtainMaterial, foamMaterial, particleMaterial;
        public AudioClip audioClip;
        public float nearDistance = 12, farDistance = 100, soundClusterRadius = 24, soundVolume = .5f;
        public int maxNearbySounds = 1;
        public float mistRate = 18, sprayRate = 35, particleLifetime = 2, particleSpeed = 3, mistSize = 3, spraySize = .15f;
        public Vector3 vfxBoundsPadding = new Vector3(12, 8, 12);
    }
}
