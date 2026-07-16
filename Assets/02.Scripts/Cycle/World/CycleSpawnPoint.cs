using System;
using UnityEngine;

namespace UPlayGround.Cycle
{
    [Flags]
    public enum CycleSpawnRole
    {
        None = 0,
        Player = 1 << 0,
        OuterBoss = 1 << 1,
        Respawn = 1 << 2,
    }

    public sealed class CycleSpawnPoint : MonoBehaviour
    {
        [SerializeField] private string _spawnId;
        [SerializeField] private CycleSpawnRole _allowedRoles;
        [SerializeField] private string _sectorId;
        [Min(0f), SerializeField] private float _safetyRadius = 10f;
        [SerializeField] private Transform _arrivalPoint;

        public string SpawnId => _spawnId;
        public CycleSpawnRole AllowedRoles => _allowedRoles;
        public string SectorId => _sectorId ?? string.Empty;
        public float SafetyRadius => _safetyRadius;
        public Vector3 Position => _arrivalPoint != null ? _arrivalPoint.position : transform.position;
        public Quaternion Rotation => _arrivalPoint != null ? _arrivalPoint.rotation : transform.rotation;
        public bool Allows(CycleSpawnRole role) => (_allowedRoles & role) != 0;

        private void OnDrawGizmos()
        {
            Gizmos.color = Allows(CycleSpawnRole.Player) ? Color.cyan : Allows(CycleSpawnRole.OuterBoss) ? Color.red : Color.green;
            Gizmos.DrawWireSphere(Position, 0.75f);
            if (Allows(CycleSpawnRole.Player) && _safetyRadius > 0f)
                Gizmos.DrawWireSphere(Position, _safetyRadius);
        }
    }

}
