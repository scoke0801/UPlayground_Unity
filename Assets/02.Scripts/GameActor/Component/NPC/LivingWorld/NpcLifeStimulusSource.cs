using System;
using UnityEngine;

namespace UPlayGround.Components.LivingWorld
{
    /// <summary>실제 채집 충격을 같은 논리 맵의 제한된 주민에게 국소 소리로 전달한다.</summary>
    [RequireComponent(typeof(GatheringActor))]
    public sealed class NpcLifeStimulusSource : MonoBehaviour
    {
        [SerializeField] private string _mapId;
        [SerializeField] private NpcLifeController[] _listeners = Array.Empty<NpcLifeController>();
        [Min(1), SerializeField] private int _maxResponses = 2;
        [Min(0.1f), SerializeField] private float _lifetime = 2f;
        [Range(0f, 1f), SerializeField] private float _importance = 0.5f;
        private GatheringActor _source;
        private object _producer;
        private long _revision;

        private void OnEnable()
        {
            _source = GetComponent<GatheringActor>();
            _producer = new object();
            _revision = 0;
            _source.ImpactOccurred += OnImpact;
            for (int i = 0; i < _listeners.Length; i++) _listeners[i]?.BindStimulus(_producer);
        }

        private void OnImpact(Vector3 position)
        {
            long revision = ++_revision;
            int responses = 0;
            int start = _listeners.Length == 0 ? 0 : (int)(revision % _listeners.Length);
            for (int offset = 0; offset < _listeners.Length && responses < _maxResponses; offset++)
            {
                var listener = _listeners[(start + offset) % _listeners.Length];
                if (listener != null && listener.ConsumeStimulus(_producer, _mapId, revision, position,
                    Time.time, Time.time + _lifetime, _importance)) responses++;
            }
        }

        private void OnDisable()
        {
            if (_source != null) _source.ImpactOccurred -= OnImpact;
            for (int i = 0; i < _listeners.Length; i++) _listeners[i]?.ReleaseStimulus(_producer);
            _producer = null;
        }
    }
}
