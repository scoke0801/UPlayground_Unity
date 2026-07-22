using System;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Actor.Animation
{
    [CreateAssetMenu(fileName = "MotionReference_", menuName = "UPlayGround/애니메이션/Motion Reference")]
    public sealed class MotionReferenceSO : ScriptableObject
    {
        [Serializable]
        public struct WeaponOverride
        {
            public WeaponType weaponType;
            public MotionSetAsset motion;
        }

        [Tooltip("무기별 오버라이드가 없을 때 재생할 기본 모션입니다.")]
        public MotionSetAsset defaultMotion;

        [Tooltip("같은 무기 타입이 여러 번 있으면 앞에 있는 유효한 모션을 사용합니다.")]
        public WeaponOverride[] weaponOverrides = Array.Empty<WeaponOverride>();

        public bool HasAnyMotion
        {
            get
            {
                if (defaultMotion != null)
                    return true;
                if (weaponOverrides == null)
                    return false;
                for (int i = 0; i < weaponOverrides.Length; i++)
                    if (weaponOverrides[i].motion != null)
                        return true;
                return false;
            }
        }

        public MotionSetAsset Resolve(WeaponType weaponType)
        {
            if (weaponOverrides != null)
            {
                for (int i = 0; i < weaponOverrides.Length; i++)
                {
                    WeaponOverride candidate = weaponOverrides[i];
                    if (candidate.weaponType == weaponType && candidate.motion != null)
                        return candidate.motion;
                }
            }

            return defaultMotion;
        }
    }
}
