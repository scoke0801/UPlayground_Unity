using UnityEngine;
using UPlayGround.Data.Sound;

namespace UPlayGround.Manager
{
    internal sealed class ActiveSoundHandle
    {
        public AudioSource source;
        public AudioSourcePool pool;
        public string key;
        public SoundBusType bus;
    }
}
