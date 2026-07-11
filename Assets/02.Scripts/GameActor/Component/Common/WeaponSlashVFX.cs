using UnityEngine;

namespace UPlayGround.Components
{
    public sealed class WeaponSlashVFX : MonoBehaviour
    {
        [SerializeField] private UPlayGround.Particle.WeaponSlashVfxSpawner spawner;

        public void SpawnSlash()
        {
            if (spawner == null)
                spawner = GetComponentInChildren<UPlayGround.Particle.WeaponSlashVfxSpawner>(true);

            if (spawner == null)
            {
                Debug.LogWarning($"{nameof(WeaponSlashVFX)}: Missing {nameof(UPlayGround.Particle.WeaponSlashVfxSpawner)}.", this);
                return;
            }

            spawner.SpawnSlash();
        }
    }
}
