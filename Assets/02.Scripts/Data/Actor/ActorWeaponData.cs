

using UnityEngine;
using UnityEngine.Serialization;
using UPlayGround.Data.Enum;

namespace UPlayGround.Data.Actor
{
    [System.Serializable]
    public class WeaponData
    {
        [FormerlySerializedAs("equipment")] public WeaponType weaponType;
        public string weaponKey;
        
        // todo
        
    }
}