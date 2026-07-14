using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Party
{
    /// <summary>
    /// 무기와 그 무기가 부여하는 효과 정보를 담는 데이터.
    /// 캐릭터 선택 화면 등에서 "무기 + 무기에 따른 효과"를 표시하기 위한 단일 소스.
    /// (스탯/전투 로직과는 분리된, 표시/설계용 데이터. 추후 실제 효과 적용 로직과 연결 예정.)
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponEffect", menuName = "UPlayGround/파티/무기 효과")]
    public class WeaponEffectSO : ScriptableObject
    {
        [System.Serializable]
        public struct EffectEntry
        {
            [Tooltip("효과 이름")]
            public string title;

            [Tooltip("효과 설명")]
            [TextArea(2, 4)]
            public string description;

            [Tooltip("효과 아이콘(선택)")]
            public Sprite icon;
        }

        [Header("Weapon")]
        public WeaponType weaponType = WeaponType.NoWeapon;

        [Tooltip("무기 표시 이름. 비우면 WeaponType 한글 표기를 사용한다.")]
        public string weaponDisplayName;

        public Sprite weaponIcon;

        [TextArea(2, 4)]
        public string weaponDescription;

        [Header("Effects")]
        [Tooltip("이 무기가 부여하는 효과 목록.")]
        public List<EffectEntry> effects = new();

        /// <summary> 표시용 무기 이름. 오버라이드가 없으면 WeaponType 한글 표기. </summary>
        public string ResolveWeaponName()
            => string.IsNullOrEmpty(weaponDisplayName) ? weaponType.ToDisplayString() : weaponDisplayName;
    }
}
