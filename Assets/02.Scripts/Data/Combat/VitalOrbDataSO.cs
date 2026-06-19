using UnityEngine;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Combat
{
    [CreateAssetMenu(fileName = "VitalOrbData", menuName = "UPlayGround/전투/VitalOrb Data")]
    public class VitalOrbDataSO : ScriptableObject
    {
        [Header("등급 & 회복")]
        public VitalOrbGrade grade = VitalOrbGrade.B;
        [Range(0f, 1f)]  public float healAmount   = 0.03f; // 최대 HP 비율
        [Range(0f, 100f)] public float gaugeAmount  = 8f;

        [Header("자동 습득")]
        public float collectRadius   = 2.0f;
        public float attractDelay    = 0.15f;
        public float minAttractSpeed = 10.0f;
        public float maxAttractSpeed = 28.0f;
        public float attractSpeed    = 28.0f;
        public float collectDistance = 0.3f;
        public float lifetime        = 12.0f;

        [Header("부유 애니메이션")]
        public float floatAmplitude = 0.05f;
        public float floatSpeed     = 0.05f;

        [Header("이펙트 / 사운드")]
        public string spawnParticleName   = "";
        public string collectParticleName = "";
        public string collectSoundName    = "";
    }
}
