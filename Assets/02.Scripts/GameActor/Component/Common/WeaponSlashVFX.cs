using UnityEngine;

namespace UPlayGround.Component
{
    public sealed class WeaponSlashVFX : MonoBehaviour
    {
        [SerializeField] private FX.WeaponSlashVfxSpawner spawner;

        public void SpawnSlash()
        {
            if (spawner == null)
                spawner = GetComponentInChildren<FX.WeaponSlashVfxSpawner>(true);

            if (spawner == null)
            {
                Debug.LogWarning($"{nameof(WeaponSlashVFX)}: Missing {nameof(FX.WeaponSlashVfxSpawner)}.", this);
                return;
            }

            spawner.SpawnSlash();
        }
    }
}
