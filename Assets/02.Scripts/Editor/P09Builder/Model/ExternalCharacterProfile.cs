using System;
using UnityEngine;

namespace UPlayGround.Editor.P09Builder
{
    /// <summary>외부 모델의 원본 외형, 장비 그룹과 Humanoid 부착 위치를 정의한다.</summary>
    [CreateAssetMenu(menuName = "UPlayGround/캐릭터/외부 모델 장비 프로필")]
    public sealed class ExternalCharacterProfile : ScriptableObject
    {
        public GameObject sourcePrefab;
        [TextArea] public string authoringNotes;
        public EquipmentSlot[] slots = Array.Empty<EquipmentSlot>();
        public MaterialReplacement[] materialReplacements = Array.Empty<MaterialReplacement>();

        [Serializable]
        public sealed class MaterialReplacement
        {
            public Material source;
            public Material replacement;
        }

        [Serializable]
        public sealed class EquipmentSlot
        {
            public string id;
            public int defaultIndex;
            public EquipmentVariant[] variants = Array.Empty<EquipmentVariant>();
        }

        [Serializable]
        public sealed class EquipmentVariant
        {
            public string id;
            [Tooltip("원본 프리팹 루트 기준 Renderer 경로. 몸 가림 메시도 같은 외형에 포함합니다.")]
            public string[] rendererPaths = Array.Empty<string>();
            [Tooltip("리깅된 의상은 원본 rendererPaths를 사용합니다. 이 필드는 강체 장신구용입니다.")]
            public GameObject attachmentPrefab;
            public HumanBodyBones attachmentBone = HumanBodyBones.Hips;
            public Vector3 localPosition;
            public Vector3 localEulerAngles;
            public Vector3 localScale = Vector3.one;
        }
    }
}
