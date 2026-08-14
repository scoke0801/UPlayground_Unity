using System;
using UnityEngine;

namespace UPlayGround.Data
{
    /// <summary>
    /// 대화 카메라의 샷(구도) 종류.
    /// Auto는 "노드가 지정하지 않음" — DialogueShotDirector가 규칙으로 결정한다.
    /// </summary>
    public enum DialogueShotType
    {
        Auto = 0,

        /// <summary>화자를 잡는 오버 더 숄더. 대화의 기본 구도.</summary>
        OverTheShoulderSpeaker = 1,

        /// <summary>청자를 잡는 오버 더 숄더. 화자 샷의 리버스 샷.</summary>
        OverTheShoulderListener = 2,

        /// <summary>화자 클로즈업. 감정 강조용.</summary>
        Closeup = 3,

        /// <summary>두 인물을 한 프레임에 담는 투샷.</summary>
        TwoShot = 4,

        /// <summary>두 인물과 주변을 함께 담는 와이드. establishing용.</summary>
        Wide = 5,

        /// <summary>지정한 인물의 반응을 잡는 리액션 샷.</summary>
        Reaction = 6
    }

    /// <summary>
    /// 이전 샷에서 현재 샷으로 넘어가는 방식.
    /// Auto는 DialogueShotDirector가 규칙으로 결정한다(대상 변경=Cut, 동일 대상=Blend, 진입=Establish).
    /// </summary>
    public enum DialogueShotTransition
    {
        Auto = 0,

        /// <summary>즉시 전환. cutInstantTime을 사용한다.</summary>
        Cut = 1,

        /// <summary>부드러운 보정. softBlendTime을 사용한다.</summary>
        Blend = 2,

        /// <summary>대화 진입/장면 전환용 느린 블렌드. establishBlendTime을 사용한다.</summary>
        Establish = 3
    }

    /// <summary>
    /// 샷 종류별 구도 프리셋.
    /// 별도 SO 자산으로 쪼개지 않고 DialogueCameraSettingsSO가 리스트로 소유한다
    /// (설정 에셋 하나만 Addressables에 등록되어 있어 자산·주소 추가 없이 저작 가능).
    /// 리스트에 항목이 없으면 DialogueCameraSettingsSO가 기존 구도와 동일한 기본값을 만들어 쓴다.
    /// </summary>
    [Serializable]
    public class DialogueShotPreset
    {
        public DialogueShotType shotType = DialogueShotType.OverTheShoulderSpeaker;

        [Tooltip("주시 대상 기준 카메라 방향. x=가상선 측면, y=높이, z=대상 뒤쪽(음수). 정규화 후 distance를 곱한다.")]
        public Vector3 shoulderOffset = new Vector3(0.45f, 1f, -2.8f);

        [Tooltip("주시 지점에서 카메라까지 거리(m).")]
        [Min(0.1f)] public float distance = 3.2f;

        [Range(10f, 90f)] public float fieldOfView = 45f;

        [Tooltip("주시 지점 보정(대상 발밑 기준). 보통 y만 사용해 가슴~머리 높이를 잡는다.")]
        public Vector3 lookAtOffset = new Vector3(0f, 1f, 0f);

        [Tooltip("두 인물의 중점을 잡고 둘 다 화면에 담는 구도인지. 투샷·와이드에 사용한다.")]
        public bool framesBothActors;

        [Tooltip("framesBothActors일 때 두 인물 간 거리 대비 최소 확보 거리 배수.")]
        [Min(0f)] public float separationFitScale = 1.15f;

        public DialogueShotPreset Clone()
        {
            return (DialogueShotPreset)MemberwiseClone();
        }
    }
}
