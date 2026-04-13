using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.Data.Combat
{
    /// <summary>
    /// 콤보 시퀀스에서 인식하는 입력 종류.
    /// 기존 ScriptableObject 직렬화 값 보존을 위해 enum 정수값을 고정한다.
    /// </summary>
    public enum ComboInputType
    {
        LightAttack = 0,   // L — 약공격 (좌클릭)
        HeavyAttack = 1,   // H — 강공격 (우클릭)
        Dodge       = 2,   // D — 회피
        Skill       = 3,   // S — 스킬
        Jump        = 4,   // J — 점프
    }

    /// <summary>콤보 시퀀스의 단일 입력 스텝</summary>
    [Serializable]
    public class ComboInputStep
    {
        [Tooltip("이 스텝에서 요구되는 입력 종류")]
        public ComboInputType inputType = ComboInputType.LightAttack;
    }

    /// <summary>
    /// 하나의 콤보 시퀀스 엔트리.
    /// 입력 패턴이 일치하고 태그 조건을 통과할 때 대응하는 공격을 실행한다.
    /// PlayerAttackDataSO.comboSequences 리스트에 등록한다.
    /// </summary>
    [Serializable]
    public class ComboSequenceEntry
    {
        [Tooltip("식별용 이름 (에디터 표시용)")]
        public string sequenceName = "New Combo";

        [Tooltip("입력 패턴. 왼쪽부터 순서대로 일치해야 한다.")]
        public List<ComboInputStep> inputSequence = new();

        [Header("Tag Conditions")]
        [Tooltip(
            "이 시퀀스를 사용하려면 Actor가 보유해야 하는 태그 (AND 조건).\n" +
            "GameplayTagId enum으로 지정 — Tag Registry Editor에서 정의한 태그만 사용 가능.")]
        public List<GameplayTagId> requiredTagIds = new();

        [Tooltip(
            "이 중 하나라도 보유하면 사용 불가 (블록 조건).\n" +
            "GameplayTagId enum으로 지정.")]
        public List<GameplayTagId> blockedTagIds = new();

        [Header("Skill Gauge")]
        [Tooltip("시퀀스 실행 시 차감할 스킬 게이지 슬롯 인덱스 (0-based).\n" +
                 "-1이면 게이지 비용 없음.\n" +
                 "게이지가 부족하면 이 시퀀스는 매칭되어도 실행되지 않는다.")]
        public int skillGaugeIndex = -1;

        [Header("Attack Data")]
        [Tooltip("시퀀스 매칭 시 실행할 공격 정보")]
        public PlayerAttackInfo attackInfo = new();

        [Tooltip("동일한 길이의 시퀀스가 여럿일 때 우선순위 (높을수록 먼저 체크)")]
        public int priority = 0;

        public bool IsEmpty => inputSequence == null || inputSequence.Count == 0;

        /// <summary>
        /// 이 시퀀스의 태그 조건을 GameplayTagContainer로 검사한다.
        /// requiredTagIds 전부 보유 AND blockedTagIds 하나도 없어야 통과.
        /// </summary>
        public bool CheckTagConditions(GameplayTagContainer container)
        {
            if (container == null)
                return requiredTagIds == null || requiredTagIds.Count == 0;

            if (requiredTagIds != null)
            {
                foreach (var id in requiredTagIds)
                {
                    if (id == GameplayTagId.None) continue;
                    if (!container.HasTag(id)) return false;
                }
            }

            if (blockedTagIds != null)
            {
                foreach (var id in blockedTagIds)
                {
                    if (id == GameplayTagId.None) continue;
                    if (container.HasTag(id)) return false;
                }
            }

            return true;
        }
    }
}
