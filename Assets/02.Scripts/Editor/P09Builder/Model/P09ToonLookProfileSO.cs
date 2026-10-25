using System;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Editor.P09Builder
{
    /// <summary>원본 재질과 프로젝트 소유 톤 재질의 연결을 보관하는 빌더 전용 프로파일.</summary>
    [CreateAssetMenu(fileName = "P09_ToonLook", menuName = "UPlayGround/도구/P09 Toon Look")]
    public sealed class P09ToonLookProfileSO : ScriptableObject
    {
        /// <summary>원본 재질과 렌더러 조건별 교체 재질.</summary>
        [Serializable]
        public sealed class MaterialBinding
        {
            [Tooltip("비우면 모든 렌더러에 적용. 얼굴처럼 별도 톤이 필요한 메시 접두사를 지정합니다.")]
            public string rendererPrefix;
            public Material source;
            public Material replacement;
        }

        public MaterialBinding[] bindings = Array.Empty<MaterialBinding>();

        /// <summary>저장 전에 모든 연결을 검사하여 원본 에셋 변경과 잘못된 셰이더 교체를 막는다.</summary>
        public void ValidateBindings()
        {
            if (bindings == null || bindings.Length == 0)
                throw new InvalidOperationException("톤 프로파일의 머티리얼 연결이 비어 있습니다.");
            for (int i = 0; i < bindings.Length; i++)
            {
                var binding = bindings[i];
                if (binding == null || binding.source == null || binding.replacement == null)
                    throw new InvalidOperationException($"톤 프로파일 연결 {i}: 머티리얼이 누락되었습니다.");
                string path = AssetDatabase.GetAssetPath(binding.replacement);
                if (!path.StartsWith("Assets/", StringComparison.Ordinal)
                    || path.StartsWith("Assets/ExternalAssets/", StringComparison.Ordinal)
                    || binding.source == binding.replacement || binding.source.shader == null
                    || binding.source.shader != binding.replacement.shader)
                    throw new InvalidOperationException($"톤 프로파일 연결 {i}: 같은 셰이더의 프로젝트 소유 복제본을 지정하세요.");
                for (int j = 0; j < i; j++)
                {
                    if (bindings[j].source == binding.source
                        && (bindings[j].rendererPrefix ?? "") == (binding.rendererPrefix ?? ""))
                        throw new InvalidOperationException($"톤 프로파일 연결 {i}: 같은 원본과 렌더러 조건이 중복됩니다.");
                    if (bindings[j].replacement == binding.replacement && bindings[j].source != binding.source)
                        throw new InvalidOperationException($"톤 프로파일 연결 {i}: 교체 재질의 원본을 유일하게 결정할 수 없습니다.");
                    if (bindings[j].replacement == binding.source || bindings[j].source == binding.replacement)
                        throw new InvalidOperationException($"톤 프로파일 연결 {i}: 교체 재질을 다른 연결의 원본으로 사용할 수 없습니다.");
                }
            }
        }

        /// <summary>얼굴 전용 조건을 공용 조건보다 우선하여 재질을 찾는다.</summary>
        public Material Resolve(string rendererName, Material current)
        {
            // 이미 공용 피부로 교체된 슬롯도 얼굴 조건을 다시 평가할 수 있어야 한다.
            Material source = current;
            foreach (var binding in bindings)
                if (current == binding.replacement)
                {
                    source = binding.source;
                    break;
                }
            MaterialBinding selected = null;
            int specificity = -1;
            foreach (var binding in bindings)
            {
                if (source != binding.source) continue;
                string prefix = binding.rendererPrefix ?? "";
                if (prefix.Length <= specificity || !rendererName.StartsWith(prefix, StringComparison.Ordinal)) continue;
                selected = binding;
                specificity = prefix.Length;
            }
            return selected != null ? selected.replacement : current;
        }

        /// <summary>텍스처·프리팹 구조를 바꾸지 않고 렌더러의 공유 재질만 교체한다.</summary>
        public int Apply(GameObject root)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            ValidateBindings();
            int count = 0;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material replacement = Resolve(renderer.name, materials[i]);
                    if (replacement == materials[i]) continue;
                    materials[i] = replacement;
                    changed = true;
                    count++;
                }
                if (changed) renderer.sharedMaterials = materials;
            }
            return count;
        }
    }
}
