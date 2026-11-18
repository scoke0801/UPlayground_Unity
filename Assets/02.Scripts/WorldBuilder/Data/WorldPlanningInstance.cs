using UnityEngine;
using UPlayGround.WorldBuilder.Core;

namespace UPlayGround.WorldBuilder.Data
{
    /// <summary>이름과 InstanceID 대신 원본 레코드로 구조물의 정체성을 유지한다.</summary>
    public sealed class WorldPlanningInstance : MonoBehaviour
    {
        public WorldDefinition definition;
        public string planGuid;
        public PlacementRecord record;
    }
}
