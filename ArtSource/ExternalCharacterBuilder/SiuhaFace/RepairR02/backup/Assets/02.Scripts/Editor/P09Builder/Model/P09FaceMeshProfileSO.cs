using System;
using UnityEngine;
using UnityEngine.Animations;

namespace UPlayGround.Editor.P09Builder
{
    /// <summary>P09 본과 목 경계에 맞춰 저작한 얼굴을 헤어·의상 선택과 독립적으로 적용한다.</summary>
    public sealed class P09FaceMeshProfileSO : ScriptableObject
    {
        /// <summary>두피를 변형하지 않고 헤어 리그의 머리 기준 부착 위치를 지정한다.</summary>
        [Serializable]
        public sealed class HairAttachment
        {
            public string constraintPath;
            [Tooltip("머리 본 로컬 좌표의 절대 위치 오프셋(m). 반복 적용해도 누적되지 않습니다.")]
            public Vector3 positionOffset;
            [Tooltip("헤어 부착 본의 로컬 배율. P09에서는 X가 앞뒤 폭이며 얼굴·두피에는 적용하지 않습니다.")]
            public Vector3 localScale = Vector3.one;
        }

        public BuilderSex sex = BuilderSex.Female;
        public Mesh mesh;
        public Material[] materials = Array.Empty<Material>();
        public string[] boneNames = Array.Empty<string>();
        public string[] rendererPaths = Array.Empty<string>();
        public HairAttachment[] hairAttachments = Array.Empty<HairAttachment>();

        /// <summary>메시와 스킨 계약을 저장 전에 검사한다.</summary>
        public void ValidateProfile()
        {
            if (mesh == null || materials == null || materials.Length != mesh.subMeshCount)
                throw new InvalidOperationException("교체 얼굴의 메시·재질 슬롯이 일치하지 않습니다.");
            foreach (var material in materials)
                if (material == null) throw new InvalidOperationException("교체 얼굴 재질이 누락되었습니다.");
            if (boneNames == null || boneNames.Length != mesh.bindposes.Length || boneNames.Length == 0)
                throw new InvalidOperationException("교체 얼굴의 본·바인드 포즈가 일치하지 않습니다.");
            if (rendererPaths == null || rendererPaths.Length == 0)
                throw new InvalidOperationException("교체할 P09 얼굴 경로가 없습니다.");
            if (hairAttachments == null)
                throw new InvalidOperationException("헤어 부착 설정이 없습니다.");
            foreach (var attachment in hairAttachments)
                if (attachment == null || string.IsNullOrWhiteSpace(attachment.constraintPath)
                    || float.IsNaN(attachment.positionOffset.sqrMagnitude)
                    || float.IsInfinity(attachment.positionOffset.sqrMagnitude)
                    || float.IsNaN(attachment.localScale.sqrMagnitude)
                    || float.IsInfinity(attachment.localScale.sqrMagnitude)
                    || attachment.localScale.x <= 0 || attachment.localScale.y <= 0 || attachment.localScale.z <= 0)
                    throw new InvalidOperationException("헤어 부착 경로·위치·배율이 유효하지 않습니다.");
        }

        /// <summary>모든 대상의 본 순서를 확인한 뒤 얼굴 메시와 재질을 함께 교체한다.</summary>
        public void Apply(GameObject root)
        {
            ValidateProfile();
            if (root == null) throw new ArgumentNullException(nameof(root));
            var targets = new SkinnedMeshRenderer[rendererPaths.Length];
            for (int i = 0; i < targets.Length; i++)
            {
                var target = root.transform.Find(rendererPaths[i]);
                var skin = target != null ? target.GetComponent<SkinnedMeshRenderer>() : null;
                if (skin == null || skin.bones.Length != boneNames.Length)
                    throw new InvalidOperationException("교체 얼굴의 P09 렌더러·본 수 불일치: " + rendererPaths[i]);
                for (int bone = 0; bone < boneNames.Length; bone++)
                    if (skin.bones[bone] == null || skin.bones[bone].name != boneNames[bone])
                        throw new InvalidOperationException("교체 얼굴의 P09 본 순서 불일치: " + rendererPaths[i]);
                targets[i] = skin;
            }
            var constraints = new ParentConstraint[hairAttachments.Length];
            for (int i = 0; i < constraints.Length; i++)
            {
                var target = root.transform.Find(hairAttachments[i].constraintPath);
                var constraint = target != null ? target.GetComponent<ParentConstraint>() : null;
                if (constraint == null || constraint.sourceCount != 1
                    || constraint.GetSource(0).sourceTransform == null)
                    throw new InvalidOperationException("헤어 부착 제약·본 불일치: " + hairAttachments[i].constraintPath);
                constraints[i] = constraint;
            }
            // 하나라도 잘못된 연결이면 앞쪽 얼굴만 교체되는 부분 적용을 막는다.
            foreach (var skin in targets)
            {
                skin.sharedMesh = mesh;
                skin.sharedMaterials = materials;
                for (int i = 0; i < mesh.blendShapeCount; i++) skin.SetBlendShapeWeight(i, 0);
            }
            for (int i = 0; i < constraints.Length; i++)
            {
                var constraint = constraints[i];
                constraint.SetTranslationOffset(0, hairAttachments[i].positionOffset);
                constraint.transform.localScale = hairAttachments[i].localScale;
                constraint.locked = true;
                // Editor 미리보기에서도 다음 PlayerLoop 평가를 기다리지 않고 같은 위치를 보여준다.
                constraint.transform.position = constraint.GetSource(0).sourceTransform
                    .TransformPoint(hairAttachments[i].positionOffset);
            }
        }
    }
}
