using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.Components;

namespace UPlayGround.Editor.P09Builder
{
    /// <summary>외부 모델을 독립된 미리보기 씬에서 조립하고 새 프리팹으로 저장한다.</summary>
    public static class ExternalCharacterAssembler
    {
        public const string OutputFolder = "Assets/03.Prefabs/Characters/ExternalModels";

        /// <summary>원본 모델의 Humanoid와 스킨 연결을 검사한다.</summary>
        public static void ValidateSource(GameObject source)
        {
            if (source == null || !EditorUtility.IsPersistent(source))
                throw new InvalidOperationException("Project 창의 모델 프리팹을 지정하세요.");
            var animator = source.GetComponent<Animator>();
            if (animator == null || animator.avatar == null || !animator.avatar.isValid || !animator.avatar.isHuman)
                throw new InvalidOperationException("루트 Animator에 유효한 Humanoid Avatar가 필요합니다.");
            foreach (var transform in source.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) != 0)
                    throw new InvalidOperationException($"원본에 Missing Script가 있습니다: {transform.name}");
            foreach (var skin in source.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (skin.sharedMesh == null) throw new InvalidOperationException($"스킨 메시 누락: {skin.name}");
                if (skin.bones.Length != skin.sharedMesh.bindposes.Length)
                    throw new InvalidOperationException($"본과 bind pose 개수가 다릅니다: {skin.name}");
                foreach (var bone in skin.bones)
                    if (bone == null || !bone.IsChildOf(source.transform))
                        throw new InvalidOperationException($"원본 계층 밖의 스킨 본: {skin.name}");
            }
        }

        /// <summary>저장 없이 전체 조립을 검증한다.</summary>
        public static void Validate(ExternalCharacterProfile profile)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try { CreateInstance(profile, scene); }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        /// <summary>원본 메시·재질·표정을 복제하지 않고 인스턴스 연결만 구성한다.</summary>
        public static GameObject CreateInstance(ExternalCharacterProfile profile, Scene scene, int[] selections = null)
        {
            if (profile == null) throw new InvalidOperationException("외부 모델 프로필을 선택하세요.");
            ValidateSource(profile.sourcePrefab);
            if (profile.slots == null) throw new InvalidOperationException("슬롯 목록이 없습니다.");
            if (selections != null && selections.Length != profile.slots.Length)
                throw new InvalidOperationException("장비 선택 개수가 슬롯 개수와 다릅니다.");
            var root = (GameObject)PrefabUtility.InstantiatePrefab(profile.sourcePrefab, scene);
            try
            {
                PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                var animator = root.GetComponent<Animator>();
                var slots = new CharacterEquipmentVisuals.Slot[profile.slots.Length];
                for (int i = 0; i < slots.Length; i++) slots[i] = BuildSlot(profile.slots[i], root, animator);
                if (selections != null)
                    for (int i = 0; i < slots.Length; i++) slots[i].selectedIndex = selections[i];
                var visuals = root.GetComponent<CharacterEquipmentVisuals>();
                if (visuals == null) visuals = root.AddComponent<CharacterEquipmentVisuals>();
                visuals.Configure(slots);
                ApplyMaterials(profile, root);
                root.name = profile.name;
                return root;
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(root);
                throw;
            }
        }

        /// <summary>동일 경로 덮어쓰기 없이 새 모델 프리팹을 만든다.</summary>
        public static GameObject Build(ExternalCharacterProfile profile, string path, int[] selections = null)
        {
            if (string.IsNullOrWhiteSpace(path) || !path.StartsWith(OutputFolder + "/", StringComparison.Ordinal) ||
                !path.EndsWith(".prefab", StringComparison.Ordinal) || path.Contains("..") || path.Contains("\\"))
                throw new InvalidOperationException($"저장 위치는 {OutputFolder} 아래의 새 .prefab이어야 합니다.");
            if (File.Exists(path) || AssetDatabase.LoadMainAssetAtPath(path) != null)
                throw new InvalidOperationException("기존 프리팹을 덮어쓰지 않습니다. 새 이름을 지정하세요.");
            var scene = EditorSceneManager.NewPreviewScene();
            bool committed = false;
            try
            {
                var root = CreateInstance(profile, scene, selections);
                root.name = Path.GetFileNameWithoutExtension(path);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                AssetDatabase.Refresh();
                var result = PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
                if (!success || result == null) throw new InvalidOperationException("외부 모델 프리팹 저장에 실패했습니다.");
                committed = true;
                return result;
            }
            finally
            {
                if (!committed && File.Exists(path)) AssetDatabase.DeleteAsset(path);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        private static CharacterEquipmentVisuals.Slot BuildSlot(ExternalCharacterProfile.EquipmentSlot source, GameObject root, Animator animator)
        {
            if (source == null || source.variants == null) throw new InvalidOperationException("장비 슬롯 정의 누락");
            var slot = new CharacterEquipmentVisuals.Slot
            {
                id = source.id, selectedIndex = source.defaultIndex,
                variants = new CharacterEquipmentVisuals.Variant[source.variants.Length]
            };
            for (int i = 0; i < slot.variants.Length; i++)
            {
                var variant = source.variants[i];
                if (variant == null || variant.rendererPaths == null) throw new InvalidOperationException("장비 외형 정의 누락");
                var renderers = new List<Renderer>();
                foreach (string path in variant.rendererPaths)
                {
                    var renderer = ResolveRenderer(root, path);
                    renderers.Add(renderer);
                    for (var node = renderer.transform; node != root.transform; node = node.parent)
                        node.gameObject.SetActive(true);
                }
                if (variant.attachmentPrefab != null)
                    AddAttachment(variant, animator, renderers);
                slot.variants[i] = new CharacterEquipmentVisuals.Variant { id = variant.id, renderers = renderers.ToArray() };
            }
            return slot;
        }

        private static void ApplyMaterials(ExternalCharacterProfile profile, GameObject root)
        {
            var replacements = new Dictionary<Material, Material>();
            foreach (var entry in profile.materialReplacements ?? Array.Empty<ExternalCharacterProfile.MaterialReplacement>())
            {
                if (entry == null || entry.source == null || entry.replacement == null || replacements.ContainsKey(entry.source))
                    throw new InvalidOperationException("재질 교체 연결이 누락되거나 중복됩니다.");
                string path = AssetDatabase.GetAssetPath(entry.replacement);
                if (!path.StartsWith("Assets/", StringComparison.Ordinal) || path.StartsWith("Assets/ExternalAssets/", StringComparison.Ordinal))
                    throw new InvalidOperationException("교체 재질은 프로젝트 소유 복제본을 지정하세요.");
                replacements.Add(entry.source, entry.replacement);
            }
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] != null && replacements.TryGetValue(materials[i], out var replacement)) materials[i] = replacement;
                    if (materials[i] == null || materials[i].shader == null || materials[i].shader.name == "Hidden/InternalErrorShader" || !materials[i].shader.isSupported)
                        throw new InvalidOperationException($"사용할 수 없는 재질: {renderer.name}, 슬롯 {i}. 프로젝트 재질 교체를 지정하세요.");
                }
                renderer.sharedMaterials = materials;
            }
        }

        private static Renderer ResolveRenderer(GameObject root, string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new InvalidOperationException("빈 렌더러 경로는 허용하지 않습니다.");
            Renderer result = null;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (AnimationUtility.CalculateTransformPath(renderer.transform, root.transform) != path) continue;
                if (result != null) throw new InvalidOperationException($"렌더러 경로가 모호합니다: {path}");
                result = renderer;
            }
            return result != null ? result : throw new InvalidOperationException($"렌더러 경로를 찾지 못했습니다: {path}");
        }

        private static void AddAttachment(ExternalCharacterProfile.EquipmentVariant variant, Animator animator, List<Renderer> renderers)
        {
            if ((int)variant.attachmentBone < 0 || variant.attachmentBone >= HumanBodyBones.LastBone)
                throw new InvalidOperationException("장신구 부착 본이 잘못되었습니다.");
            if (variant.localScale.x <= 0 || variant.localScale.y <= 0 || variant.localScale.z <= 0)
                throw new InvalidOperationException("장신구 크기는 양수여야 합니다.");
            if (variant.attachmentPrefab.GetComponentInChildren<SkinnedMeshRenderer>(true) != null)
                throw new InvalidOperationException("리깅된 의상은 같은 원본 모델의 렌더러 경로로 등록하세요. 다른 체형의 본을 자동 교체하지 않습니다.");
            var bone = animator.GetBoneTransform(variant.attachmentBone);
            if (bone == null) throw new InvalidOperationException($"부착 본 누락: {variant.attachmentBone}");
            var attachment = UnityEngine.Object.Instantiate(variant.attachmentPrefab, bone, false);
            attachment.transform.localPosition = variant.localPosition;
            // FBX 루트의 단위·축 보정을 유지한 뒤 슬롯의 피팅 값을 더한다.
            attachment.transform.localRotation = Quaternion.Euler(variant.localEulerAngles) * variant.attachmentPrefab.transform.localRotation;
            attachment.transform.localScale = Vector3.Scale(variant.attachmentPrefab.transform.localScale, variant.localScale);
            var attachedRenderers = attachment.GetComponentsInChildren<Renderer>(true);
            if (attachedRenderers.Length == 0) throw new InvalidOperationException("장신구에 렌더러가 없습니다.");
            renderers.AddRange(attachedRenderers);
        }
    }
}
