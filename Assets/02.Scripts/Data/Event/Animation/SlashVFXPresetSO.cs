using UnityEngine;

namespace UPlayGround.Data.Event
{
    [CreateAssetMenu(menuName = "UPlayGround/VFX/Slash Preset", fileName = "SlashVFXPreset")]
    public sealed class SlashVFXPresetSO : ScriptableObject
    {
        public GameObject vfxPrefab;
        public string basePointName = "Blade_Base";
        public string tipPointName = "Blade_Tip";
        public Vector3 positionOffset;
        public Vector3 rotationOffset;
        public Vector3 scaleMultiplier = Vector3.one;
        public float destroyDelay = 2f;
    }
}
