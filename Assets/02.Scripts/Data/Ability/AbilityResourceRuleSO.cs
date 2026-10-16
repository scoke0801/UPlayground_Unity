using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Data.Ability
{
    /// <summary>캐릭터별 Ability 자원(Forte·Concerto 등) 증감 규칙 묶음.</summary>
    // 파일명과 클래스명이 일치해야 MonoScript가 에셋에 연결된다 (AbilityDefinitions.cs에서 분리).
    [CreateAssetMenu(
        fileName = "AbilityResourceRules_",
        menuName = "UPlayGround/Ability/Resource Rules")]
    public sealed class AbilityResourceRuleSO : ScriptableObject
    {
        public List<AbilityResourceRule> rules = new();
    }
}
