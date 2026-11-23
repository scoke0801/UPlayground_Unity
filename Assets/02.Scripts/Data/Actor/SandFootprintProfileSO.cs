using UnityEngine;

namespace UPlayGround.Data.Actor
{
    [CreateAssetMenu(menuName = "UPlayGround/환경/모래 발자국 프로필")]
    public sealed class SandFootprintProfileSO : ScriptableObject
    {
        public enum Quality { Standard, Low, Extended }

        public Quality quality;
        [Min(0.1f)] public float halfLife = 20f;
        [Min(0f)] public float windDecay = 1f;
        [Min(0f)] public float depression = 0.008f;
        [Min(0f)] public float rim = 0.0035f;
        [Range(0f, 1f)] public float colorContrast = 0.19f;
        [Range(0f, 1f)] public float smoothness = 0.06f;
        [Min(0f)] public float npcRadius = 9f;
        [Range(0, 8)] public int npcStampsPerFrame = 4;
        public Vector2 walk = new(0.75f, 0.62f);
        public Vector2 run = new(0.91f, 0.82f);
        public Vector2 landing = new(1f, 1f);

        public Vector2 Pressure(FootstepKind kind) => kind switch
        {
            FootstepKind.Landing => landing,
            FootstepKind.Run => run,
            _ => walk
        };
    }
}
