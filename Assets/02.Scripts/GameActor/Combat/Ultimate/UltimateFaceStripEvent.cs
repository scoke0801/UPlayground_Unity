using System;
using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Manager;

namespace UPlayGround.Data
{
    /// <summary>전투 시간축에서 시작하고 실시간 UI 시계로 재생하는 궁극기 얼굴 표시.</summary>
    [Serializable]
    public sealed class UltimateFaceStripEvent : UltimateTimelineEvent
    {
        public Sprite faceOverride;
        public bool useAccentOverride;
        public Color accentOverride = Color.white;
        [Min(0f)] public float inDuration = 0.18f;
        [Min(0f)] public float hold = 1.4f;
        [Min(0f)] public float outDuration = 0.2f;
        [Min(0f)] public float faceDelay = 0.04f;
        [Range(0f, 0.02f)] public float breathScale = 0.015f;
        [Min(0f)] public float flashDuration = 0.1f;

        public override string DisplayName => "궁극기 얼굴 패널";

        public override void Execute(UltimateRuntimeContext context)
        {
            if (context?.Caster == null)
                return;
            Sprite face = faceOverride;
            if (face == null)
                face = Svc.Party?.GetCharacterDefinition(context.Caster.CharacterType)?.ultimateFaceSprite;
            if (face == null)
                return;

            Color accent = useAccentOverride ? accentOverride
                : (context.Asset?.weaponProfile != null ? context.Asset.weaponProfile.accentColor : Color.white);
            context.ShowFaceStrip(Svc.FaceStrip, new FaceStripRequest(face, accent,
                inDuration, hold, outDuration, faceDelay, breathScale, flashDuration));
        }

        // 실시간 퇴장과 전투 시간축의 Complete를 묶지 않는다. 실행 종료는 context가 정리한다.
    }
}
