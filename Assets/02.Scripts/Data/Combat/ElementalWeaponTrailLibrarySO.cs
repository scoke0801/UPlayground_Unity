using UnityEngine;

namespace UPlayGround.Data.Combat
{
    /// <summary>전투 속성과 INab WeaponTrail 프리팹의 프로젝트 공용 매핑.</summary>
    [CreateAssetMenu(
        fileName = "ElementalWeaponTrailLibrary",
        menuName = "UPlayGround/전투/속성 Weapon Trail 라이브러리")]
    public sealed class ElementalWeaponTrailLibrarySO : ScriptableObject
    {
        public const string ResourcesPath =
            "Combat/ElementalWeaponTrailLibrary";

        [SerializeField] private GameObject _none;
        [SerializeField] private GameObject _fire;
        [SerializeField] private GameObject _water;
        [SerializeField] private GameObject _nature;
        [SerializeField] private GameObject _light;
        [SerializeField] private GameObject _dark;

        public GameObject GetPrefab(CombatElement element) =>
            element switch
            {
                CombatElement.Fire => _fire,
                CombatElement.Water => _water,
                CombatElement.Nature => _nature,
                CombatElement.Light => _light,
                CombatElement.Dark => _dark,
                _ => _none,
            };
    }
}
