using UnityEngine;

namespace UPlayGround.Cycle
{
    // 파일명과 클래스명이 일치해야 씬 컴포넌트에 정식 MonoScript가 연결된다 (CycleSpawnPoint.cs에서 분리).
    public sealed class CentralBossSpawnPoint : MonoBehaviour
    {
        [SerializeField] private string _spawnId = "central_boss";
        [SerializeField] private Transform _arrivalPoint;
        public string SpawnId => _spawnId;
        public Vector3 Position => _arrivalPoint != null ? _arrivalPoint.position : transform.position;
        public Quaternion Rotation => _arrivalPoint != null ? _arrivalPoint.rotation : transform.rotation;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(Position, 1.25f);
        }
    }
}
