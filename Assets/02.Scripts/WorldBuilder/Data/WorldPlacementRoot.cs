using UnityEngine;

namespace UPlayGround.WorldBuilder.Data
{
    /// <summary>배치 미리보기의 소유 월드를 명시한다.</summary>
    [DisallowMultipleComponent]
    public sealed class WorldPlacementRoot : MonoBehaviour
    {
        public WorldDefinition definition;
    }
}
