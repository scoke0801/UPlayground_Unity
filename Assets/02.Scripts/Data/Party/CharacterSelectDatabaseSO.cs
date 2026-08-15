using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Party
{
    /// <summary>
    /// 신규 게임 캐릭터 선택 화면(UI_Scene_CharacterSelect)에 노출할 캐릭터 목록.
    /// PartyConfig 와는 분리된 표시용 데이터 — 선택 화면 구성만 담당한다.
    /// (실제 신규 게임 시작 시 PartyConfig 반영은 추후 별도 연동.)
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterSelectDatabase", menuName = "UPlayGround/파티/캐릭터 선택 목록")]
    public class CharacterSelectDatabaseSO : ScriptableObject
    {
        [System.Serializable]
        public class Entry
        {
            public CharacterActorType characterType = CharacterActorType.None;

            [Tooltip("카드/상세에 표시할 이름. 비우면 enum 이름을 사용한다.")]
            public string displayName;

            [Tooltip("상세 패널 한 줄 소개.")]
            [TextArea(1, 3)]
            public string tagline;

            [Tooltip("카드/대형 초상화에 사용할 스프라이트.")]
            public Sprite portrait;

            [Tooltip("무기 + 무기 효과 정보 데이터.")]
            public WeaponEffectSO weaponEffect;

            [Tooltip("잠금(비활성) 여부. 잠긴 카드는 선택할 수 없고 흐리게 + 자물쇠 표시된다.")]
            public bool locked;
        }

        public List<Entry> entries = new();
    }
}
