using UnityEngine;
namespace UPlayGround.Dialogue
{
    /// <summary>모델 본과 분리된 시선 범위·추종 속도·깜빡임 설정.</summary>
    [CreateAssetMenu(menuName = "UPlayGround/대화/Face Profile")]
    public sealed class DialogueFaceProfileSO : ScriptableObject
    {
        [Range(0, 80)] public float headYaw = 40;
        [Range(0, 60)] public float headUp = 20;
        [Range(0, 60)] public float headDown = 25;
        [Range(0, 30)] public float eyeYaw = 12;
        [Range(0, 30)] public float eyePitch = 10;
        [Min(0.01f)] public float headSpeed = 5;
        [Min(0.01f)] public float eyeSpeed = 12;
        [Min(0.01f)] public float minimumDistance = 0.45f;
        [Min(0.01f)] public float outsideFadeDegrees = 35;
        public Vector2 blinkInterval = new(2.5f, 6f);
        [Min(0.01f)] public float blinkDuration = 0.16f;
        [Range(0, 100)] public float blinkWeight = 100;
    }
}
