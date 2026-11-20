using UnityEngine;

namespace UPlayGround.WorldBuilder.Runtime
{
    /// <summary>씬 활성화 시 게임플레이 계층은 비활성 상태로 남는다.</summary>
    public sealed class WorldChunkContent : MonoBehaviour
    {
        public string worldGuid, revision, chunkId;
        public GameObject content;
        public Terrain terrain;
        public WorldBakedIdentity[] identities;
        public WaterVolume[] water;
        public WorldBridgeAccessGate[] gates;
        public Collider[] crossingBarriers;
        public int[] crossingIndices;
    }
}
