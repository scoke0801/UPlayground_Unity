using UnityEngine;
using UPlayGround.WorldBuilder.Data;

namespace UPlayGround.WorldBuilder.Runtime
{
    /// <summary>발행된 수역의 동굴 건조 공간을 저작 원본과 분리한다.</summary>
    public sealed class WorldBakedWaterExclusions : MonoBehaviour
    {
        public CaveDryVolume[] volumes;
    }
}

