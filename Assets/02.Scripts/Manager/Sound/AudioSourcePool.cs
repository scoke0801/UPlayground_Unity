using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Manager
{
    internal sealed class AudioSourcePool
    {
        private readonly Queue<AudioSource> _inactive = new();
        private readonly Transform _root;
        private readonly string _sourceName;

        public AudioSourcePool(Transform root, string sourceName, int initialCount)
        {
            _root = root;
            _sourceName = sourceName;

            for (int i = 0; i < initialCount; i++)
                _inactive.Enqueue(CreateSource());
        }

        public AudioSource Rent()
        {
            var source = _inactive.Count > 0 ? _inactive.Dequeue() : CreateSource();
            source.gameObject.SetActive(true);
            return source;
        }

        public void Return(AudioSource source)
        {
            if (source == null)
                return;

            source.Stop();
            source.clip = null;
            source.outputAudioMixerGroup = null;
            source.loop = false;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.pitch = 1f;
            source.volume = 1f;
            source.priority = 128;
            source.ignoreListenerPause = false;
            source.transform.SetParent(_root, false);
            source.transform.localPosition = Vector3.zero;
            source.gameObject.SetActive(false);
            _inactive.Enqueue(source);
        }

        private AudioSource CreateSource()
        {
            var obj = new GameObject(_sourceName);
            obj.transform.SetParent(_root, false);
            obj.SetActive(false);

            var source = obj.AddComponent<AudioSource>();
            source.playOnAwake = false;
            return source;
        }
    }
}
