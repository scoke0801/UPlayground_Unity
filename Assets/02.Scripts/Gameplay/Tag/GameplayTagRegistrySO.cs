using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Gameplay.Tag
{
    /// <summary>
    /// 프로젝트에서 사용하는 GameplayTag 하나의 정의.
    /// </summary>
    [Serializable]
    public class GameplayTagDefinition
    {
        [Tooltip("계층형 태그 이름. '.'으로 계층 구분. 예) State.Combat.Sprint")]
        public string tagName = "";

        [Tooltip("생성될 enum 멤버 이름. 비워두면 tagName의 '.'을 '_'로 치환해 자동 생성. 예) State_Combat_Sprint")]
        public string enumName = "";

        [Tooltip("에디터 표시용 설명")]
        public string description = "";

        [Tooltip("에디터 시각화 색상")]
        public Color color = new Color(0.4f, 0.8f, 1.0f);

        // ── 헬퍼 ──────────────────────────────────────────────────────
        /// <summary>enumName이 비어 있으면 tagName에서 자동 생성한 값을 반환한다.</summary>
        public string GetEffectiveEnumName() =>
            string.IsNullOrWhiteSpace(enumName)
                ? tagName.Replace('.', '_').Replace(' ', '_')
                : enumName;

        public bool IsValid() => !string.IsNullOrWhiteSpace(tagName);
    }

    /// <summary>
    /// 프로젝트 전역 GameplayTag 정의 목록.
    /// 이 SO를 기반으로 GameplayTagRegistryEditorWindow가 코드를 자동 생성한다.
    ///
    /// 생성 방법: Assets 우클릭 → Create → UPlayGround → GameplayTag → Tag Registry
    /// </summary>
    [CreateAssetMenu(
        fileName = "GameplayTagRegistry",
        menuName = "UPlayGround/게임플레이 태그/Registry")]
    public class GameplayTagRegistrySO : ScriptableObject
    {
        [Tooltip("프로젝트에서 사용하는 모든 GameplayTag 정의 목록")]
        public List<GameplayTagDefinition> tags = new();

        // ── 기본값 초기화 ─────────────────────────────────────────────
        /// <summary>프로젝트 기본 태그로 목록을 채운다. 에디터 창의 "기본 태그로 초기화" 버튼에서 호출.</summary>
        public void ResetToDefaults()
        {
            tags.Clear();
            AddDefault("State.Move",               "State_Move",               "이동 중",          new Color(0.4f, 0.9f, 0.4f));
            AddDefault("State.Sprint",              "State_Sprint",             "전력 질주 중",      new Color(0.2f, 1.0f, 0.2f));
            AddDefault("State.Dash",                "State_Dash",               "대시 중",          new Color(0.3f, 0.7f, 1.0f));
            AddDefault("State.Jump",                "State_Jump",               "점프 입력 진입",    new Color(0.5f, 0.8f, 1.0f));
            AddDefault("State.Airborne",            "State_Airborne",           "공중 상태",         new Color(0.6f, 0.9f, 1.0f));
            AddDefault("State.Crouching",           "State_Crouching",          "웅크리는 중",       new Color(0.8f, 0.7f, 0.3f));
            AddDefault("State.Dodge",               "State_Dodge",              "회피 중",          new Color(0.9f, 0.6f, 0.2f));
            AddDefault("State.Combat",              "State_Combat",             "전투 상태 (부모)",  new Color(1.0f, 0.4f, 0.4f));
            AddDefault("State.Combat.Attack",       "State_Combat_Attack",      "공격 중",          new Color(1.0f, 0.3f, 0.3f));
            AddDefault("State.Combat.Guard",        "State_Combat_Guard",       "가드 중",          new Color(0.9f, 0.5f, 0.2f));
            AddDefault("State.Combat.Charge",       "State_Combat_Charge",      "차지 중",          new Color(1.0f, 0.8f, 0.1f));
            AddDefault("State.Combat.DashAttack",   "State_Combat_DashAttack",  "대시 공격 중",     new Color(1.0f, 0.5f, 0.5f));
            AddDefault("State.Combat.JumpAttack",   "State_Combat_JumpAttack",  "점프 공격 중",     new Color(1.0f, 0.5f, 0.7f));
            AddDefault("State.Hit",                 "State_Hit",                "피격 중",          new Color(1.0f, 0.2f, 0.2f));
            AddDefault("State.Death",               "State_Death",              "사망",             new Color(0.5f, 0.5f, 0.5f));
            AddDefault("State.Grabbed",             "State_Grabbed",            "잡힌 상태",         new Color(0.7f, 0.3f, 0.7f));
            AddDefault("State.Interaction",         "State_Interaction",        "상호작용 중",       new Color(0.4f, 0.9f, 0.7f));
            AddDefault("Combo.Light",               "Combo_Light",              "콤보: 약 공격 입력됨", new Color(0.3f, 0.6f, 1.0f));
            AddDefault("Combo.Heavy",               "Combo_Heavy",              "콤보: 강 공격 입력됨", new Color(1.0f, 0.5f, 0.2f));
        }

        private void AddDefault(string tagName, string enumName, string desc, Color color)
        {
            tags.Add(new GameplayTagDefinition
            {
                tagName     = tagName,
                enumName    = enumName,
                description = desc,
                color       = color,
            });
        }
    }
}
