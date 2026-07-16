using UnityEngine;

namespace UPlayGround.Cycle
{
    // 파일명과 클래스명이 일치해야 씬 컴포넌트에 정식 MonoScript가 연결된다 (CycleSpawnPoint.cs에서 분리).
    public sealed class CycleRespawnPoint : MonoBehaviour
    {
        [SerializeField] private string _respawnId;
        [SerializeField] private bool _isActive = true;
        [SerializeField] private Transform _arrivalPoint;
        public string RespawnId => _respawnId;
        public bool IsActive => _isActive;
        public Transform ArrivalPoint => _arrivalPoint != null ? _arrivalPoint : transform;
        public void SetActive(bool active) => _isActive = active;
    }
}
