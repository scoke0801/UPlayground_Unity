using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Data.Rendering
{
    [CreateAssetMenu(fileName = "LilToonDissolveShaderConversionProfile", menuName = "UPlayGround/렌더링/LilToon Dissolve Conversion")]
    public class LilToonDissolveShaderConversionProfile : ScriptableObject
    {
        [Serializable]
        public class ShaderConversionRule
        {
            public Shader sourceShader;
            public Shader cutoutShader;
            public float transparentMode = 1f;
            public bool keepSourceShader;
        }

        [SerializeField] private List<ShaderConversionRule> _rules = new List<ShaderConversionRule>();

        public bool TryGetRule(Shader sourceShader, out ShaderConversionRule rule)
        {
            rule = null;
            if (sourceShader == null || _rules == null)
                return false;

            foreach (var candidate in _rules)
            {
                if (candidate == null || candidate.sourceShader == null)
                    continue;

                if (candidate.sourceShader == sourceShader)
                {
                    rule = candidate;
                    return true;
                }
            }

            return false;
        }

        private void OnValidate()
        {
            if (_rules == null)
                return;

            foreach (var rule in _rules)
            {
                if (rule == null)
                    continue;

                rule.transparentMode = Mathf.Max(0f, rule.transparentMode);
            }
        }
    }
}
