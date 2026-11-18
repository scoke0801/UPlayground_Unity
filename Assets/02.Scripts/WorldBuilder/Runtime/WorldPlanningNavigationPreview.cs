using UnityEngine;
using UnityEngine.AI;

namespace UPlayGround.WorldBuilder.Runtime
{
    /// <summary>분리된 저작 검증 씬에서만 저장한 NavMesh를 등록·회수한다. 게임 월드 로더와 구분한다.</summary>
    public sealed class WorldPlanningNavigationPreview : MonoBehaviour
    {
        public NavMeshData navigation;
        public Vector3 entranceA, entranceB;
        private NavMeshDataInstance _instance;
        private void Awake() { if (navigation != null) _instance = NavMesh.AddNavMeshData(navigation); }
        private void OnDestroy() { if (_instance.valid) _instance.Remove(); }
    }
}
