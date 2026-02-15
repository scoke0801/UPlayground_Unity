using System;
using UnityEngine;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// 사운드 재생 이벤트
    /// </summary>
    [Serializable]
    public class PlaySoundEvent : MotionEventBase
    {
        public AudioClip audioClip;
        [Range(0f, 1f)] public float volume = 1f;
        public bool is3D = true;

        public override string GetDisplayName() => "Sound";

        public override string GetShortLabel()
        {
            if (audioClip != null)
                return $"Sound: {audioClip.name}";
            return "Sound: (None)";
        }

        public override void Execute(GameObject target)
        {
            if (audioClip == null) return;

            if (is3D)
                AudioSource.PlayClipAtPoint(audioClip, target.transform.position, volume);
            else
                Debug.Log($"Play 2D Sound: {audioClip.name}");
        }

        public override void OnCompleteEvent(GameObject target)
        {
        }
    }
}