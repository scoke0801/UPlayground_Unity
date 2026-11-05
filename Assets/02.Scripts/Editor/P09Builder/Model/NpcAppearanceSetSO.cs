using System;
using UnityEngine;
using P09.Modular.Humanoid.Data;

namespace UPlayGround.Editor.P09Builder
{
    /// <summary>NPC 외형의 의상·성별별 허용 목록과 공통 빌드 설정을 보관한다.</summary>
    [CreateAssetMenu(fileName = "NpcAppearanceSet", menuName = "UPlayGround/액터/NPC 외형 조합")]
    public sealed class NpcAppearanceSetSO : ScriptableObject
    {
        [TextArea(3, 8)] public string description;
        public StatsAssignment npcStats = new StatsAssignment();
        public bool useMagicaCloth;
        public NpcFaceSkinPair[] maleFaces = Array.Empty<NpcFaceSkinPair>();
        public NpcFaceSkinPair[] femaleFaces = Array.Empty<NpcFaceSkinPair>();
        public NpcAppearanceRule[] rules = Array.Empty<NpcAppearanceRule>();
    }

    /// <summary>얼굴형과 피부색을 독립 축으로 늘리지 않고 한 묶음으로 지정한다.</summary>
    [Serializable]
    public sealed class NpcFaceSkinPair
    {
        public RendererEditPartData face;
        public ColorEditPartData skin;
    }

    /// <summary>헤어 컬러 하나에 눈 색 하나를 대응시킨다.</summary>
    [Serializable]
    public sealed class NpcHairEyePair
    {
        public HairColorEditPartData hair;
        public ColorEditPartData eye;
    }

    /// <summary>한 의상 세트·성별의 고정 부위와 두 개씩의 헤어 축을 지정한다.</summary>
    [Serializable]
    public sealed class NpcAppearanceRule
    {
        public int setId;
        public BuilderSex sex;
        public ArmorEditPartData arm;
        public ArmorEditPartData chest;
        public ArmorEditPartData waist;
        public ArmorEditPartData leg;
        public RendererEditPartData[] hairStyles = Array.Empty<RendererEditPartData>();
        public NpcHairEyePair[] colors = Array.Empty<NpcHairEyePair>();
        public RendererEditPartData facialHair;
        public int facialHairId;
        public BustSizeEditPartData bustSize;
    }
}
