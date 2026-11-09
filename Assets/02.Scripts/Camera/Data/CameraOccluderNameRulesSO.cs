using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.CameraSystem;

namespace UPlayGround.Data
{
    /// <summary>
    /// 오브젝트 이름으로 카메라 가림 처리(Solid/Fade/지붕 제공)를 지정하는 규칙 목록.
    /// 씬마다 건물이 프리팹 인스턴스이기도 하고 풀린 키트 부품이기도 하며, 월드 씬은 소스 씬에서 재생성되므로
    /// 프리팹·씬에 컴포넌트를 심지 않고 이 에셋 하나를 분류기가 런타임에 읽는다.
    /// 우선순위: 오브젝트에 직접 붙인 CameraOccluder → 이 규칙 → 자동 판정.
    /// </summary>
    [CreateAssetMenu(fileName = "CameraOccluderNameRules", menuName = "UPlayGround/카메라/Occluder Name Rules")]
    public sealed class CameraOccluderNameRulesSO : ScriptableObject
    {
        /// <summary>이름 하나에 대한 지정.</summary>
        [Serializable]
        public struct Rule
        {
            [Tooltip("콜라이더 오브젝트 또는 그 조상 이름과 비교할 문자열.")]
            public string namePattern;

            [Tooltip("켜면 이름이 패턴으로 시작하면 일치한다. 끄면 이름이 정확히 같거나 '패턴 (n)', '패턴(Clone)'처럼 뒤에 공백·괄호가 붙은 경우만 일치한다.")]
            public bool isPrefix;

            [Tooltip("Auto: 렌더러가 있으면 Fade. Solid: 카메라 암이 막힘(지붕 없는 구조물·성벽·걷는 면).")]
            public OccluderMode mode;

            [Tooltip("지붕·천장 콜라이더에 켠다. 카메라 실내 가드가 이 아래로 카메라가 들어가는 것을 막는다.")]
            public bool coversInterior;

            [Tooltip("지정 근거. 측정 결과나 판단 이유를 남긴다.")]
            public string note;
        }

        [SerializeField] private List<Rule> _rules = new();

        /// <summary>지정 목록. 앞에 있는 규칙이 먼저 일치한다.</summary>
        public IReadOnlyList<Rule> Rules => _rules;

        /// <summary>오브젝트 이름이 규칙과 일치하는지 판정한다.</summary>
        public static bool Matches(in Rule rule, string objectName)
        {
            string pattern = rule.namePattern;
            if (string.IsNullOrEmpty(pattern) || string.IsNullOrEmpty(objectName)) return false;
            if (!objectName.StartsWith(pattern, StringComparison.Ordinal)) return false;
            if (rule.isPrefix || objectName.Length == pattern.Length) return true;

            char next = objectName[pattern.Length];
            return next == ' ' || next == '(';
        }

        /// <summary>이름과 일치하는 첫 규칙을 찾는다.</summary>
        public bool TryMatch(string objectName, out Rule match)
        {
            for (int i = 0; i < _rules.Count; i++)
            {
                if (!Matches(_rules[i], objectName)) continue;
                match = _rules[i];
                return true;
            }
            match = default;
            return false;
        }

#if UNITY_EDITOR
        /// <summary>에디터 도구와 테스트가 목록을 채울 때만 사용한다.</summary>
        public void SetRules(List<Rule> rules) => _rules = rules;
#endif
    }
}
