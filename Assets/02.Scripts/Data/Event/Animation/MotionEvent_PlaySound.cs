using System;
using UnityEngine;
using UPlayGround.Data.Sound;
using UPlayGround.Manager;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// 사운드 재생 이벤트
    /// </summary>
    [Serializable]
    [MotionEventMeta("PlaySound", Category = "VFX / SFX", CategoryOrder = 10,
        Description = "오디오 클립을 재생합니다.",
        Aliases = new[] { "audio", "sfx", "sound", "소리", "사운드" },
        Icon = "♪", Color = new[] { 0.65f, 0.55f, 1.00f })]
    public class PlaySoundEvent : MotionEventBase
    {
        public string soundKey = "";
        public AudioClip audioClip;
        [Range(0f, 1f)] public float volume = 1f;
        public bool is3D = true;

        public override string GetDisplayName() => "Sound";

        public override string GetShortLabel()
        {
            if (!string.IsNullOrWhiteSpace(soundKey))
                return $"Sound: {soundKey}";
            if (audioClip != null)
                return $"Sound: {audioClip.name}";
            return "Sound: (None)";
        }

        public override void Execute(GameObject target)
        {
            Vector3? position = is3D && target != null
                ? target.transform.position
                : null;

            if (!string.IsNullOrWhiteSpace(soundKey))
            {
                SoundManager.Instance?.Play(soundKey, position, volume);
                return;
            }

            if (audioClip == null) return;

            SoundManager.Instance?.PlayClip(audioClip, SoundBusType.SFX, position, volume);
        }

        public override void OnCompleteEvent(GameObject target)
        {
        }
    }
}
