using UnityEngine;
using UPlayGround.WorldBuilder.Core;

namespace UPlayGround.WorldBuilder.Data
{
    /// <summary>미리보기 인스턴스의 영속 정체성을 이름·InstanceID와 분리한다.</summary>
    [DisallowMultipleComponent]
    public sealed class WorldPlacementInstance : MonoBehaviour
    {
        public WorldDefinition definition;
        public PlacementRecord record;
    }
}
