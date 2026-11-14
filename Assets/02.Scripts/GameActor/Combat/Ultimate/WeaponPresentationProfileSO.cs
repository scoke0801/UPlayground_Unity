using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Cinematic;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data
{
    public enum PresentationBeat { Anticipation, Entry, Development, Finish, Recovery }

    /// <summary>비트 위치는 모션 길이 비율, 이벤트 startTime은 비트 시작 이후의 초다.</summary>
    [Serializable]
    public sealed class WeaponPresentationBeat
    {
        public PresentationBeat beat;
        [Range(0f, 1f)] public float normalizedTime;
        [SerializeReference] public List<UltimateTimelineEvent> eventTemplates = new();
    }

    /// <summary>에디터에서 복사해 쓰는 무기별 연출 원본. 런타임은 생성한 events만 실행한다.</summary>
    [CreateAssetMenu(fileName = "WeaponPresentation_", menuName = "UPlayGround/전투/Weapon Presentation Profile")]
    public sealed class WeaponPresentationProfileSO : ScriptableObject
    {
        public WeaponType weaponType;
        public CinematicStageTier defaultTier = CinematicStageTier.CameraOnly;
        public Color accentColor = Color.white;
        [Tooltip("카메라 저작 후보. 생성기는 기존 카메라와 Stage를 자동 교체하지 않습니다.")]
        public List<CameraSnapshotProfile> cameraPresets = new();
        [Tooltip("타이밍 불변 적용에서는 사용하지 않는 프리즈 저작 제안값입니다.")]
        [Min(0f)] public float suggestedFreezeDuration;
        [TextArea] public string motionReview;
        public List<WeaponPresentationBeat> beats = new();

        /// <summary>누락·중복 비트와 전투 동작 이벤트가 섞인 템플릿을 거절한다.</summary>
        public bool IsValid(out string error)
        {
            error = string.Empty;
            if (weaponType == WeaponType.NoWeapon || weaponType == WeaponType.Arrow
                || !Enum.IsDefined(typeof(WeaponType), weaponType))
                error = "궁극기 주무기 타입이 아닙니다.";
            else if (beats == null || beats.Count != 5)
                error = "예고·진입·전개·피니시·복귀 5비트가 필요합니다.";
            else if (!IsFinite(suggestedFreezeDuration) || suggestedFreezeDuration < 0f)
                error = "프리즈 제안값이 유효하지 않습니다.";
            if (error.Length > 0)
                return false;

            float previous = -1f;
            for (int i = 0; i < beats.Count; i++)
            {
                WeaponPresentationBeat entry = beats[i];
                if (entry == null || (int)entry.beat != i || !IsFinite(entry.normalizedTime)
                    || entry.normalizedTime < 0f || entry.normalizedTime > 1f
                    || entry.normalizedTime <= previous || entry.eventTemplates == null)
                {
                    error = "비트 순서·위치·목록이 유효하지 않습니다.";
                    return false;
                }
                previous = entry.normalizedTime;
                foreach (UltimateTimelineEvent template in entry.eventTemplates)
                {
                    if (!IsPresentationEvent(template) || !IsFinite(template.startTime)
                        || !IsFinite(template.duration) || template.startTime < 0f || template.duration < 0f)
                    {
                        error = "템플릿에는 유효한 얼굴·VFX·사운드·흔들림만 허용합니다.";
                        return false;
                    }
                    if (template is UltimateFaceStripEvent face
                        && (!IsFinite(face.inDuration) || !IsFinite(face.hold) || !IsFinite(face.outDuration)
                            || !IsFinite(face.faceDelay) || !IsFinite(face.breathScale) || !IsFinite(face.flashDuration)
                            || face.inDuration < 0f || face.hold < 0f || face.outDuration < 0f
                            || face.faceDelay < 0f || face.breathScale < 0f || face.breathScale > 0.02f
                            || face.flashDuration < 0f))
                    {
                        error = "얼굴 패널 표현값이 유효하지 않습니다.";
                        return false;
                    }
                    if (template is UltimateSpawnVfxEvent vfx && vfx.destroyOnComplete && vfx.duration <= 0f)
                    {
                        error = "정리가 필요한 VFX에는 양수 duration이 필요합니다.";
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>전투·시간·카메라 소유권을 바꾸는 이벤트는 생성 대상에서 제외한다.</summary>
        public static bool IsPresentationEvent(UltimateTimelineEvent item)
        {
            Type type = item?.GetType();
            return type == typeof(UltimateFaceStripEvent) || type == typeof(UltimateCameraShakeEvent)
                || type == typeof(UltimateSpawnVfxEvent) || type == typeof(UltimateSoundEvent);
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
