using UnityEngine;
using UPlayGround.Data.Cycle;
using UPlayGround.Manager;

namespace UPlayGround.Cycle
{
    public sealed class BossAssistBootstrap : MonoBehaviour
    {
        [SerializeField] private BossAssistDatabaseSO _database;
        private void Start() => BossAssistManager.Instance?.Configure(_database);
    }
}
