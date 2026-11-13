using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Manager
{
    /// <summary>기존 FX 갱신 수명에서 지속 입자를 대여하고 자연 소멸 뒤 재사용한다.</summary>
    public interface IParticleEffectService : IGameService
    {
        ParticleSystem RentParticles(GameObject prefab, int maximum);
        void ReturnParticles(ParticleSystem particles, bool clear);
    }

    public partial class GameObjectManager : IParticleEffectService
    {
        private sealed class ParticleSlot
        {
            public GameObject prefab;
            public ParticleSystem particles;
            public bool leased, draining;
        }
        private readonly List<ParticleSlot> _particleSlots = new();

        /// <summary>효과별 상한에 도달하면 논리 상태와 무관하게 시각 요청만 생략한다.</summary>
        public ParticleSystem RentParticles(GameObject prefab, int maximum)
        {
            if (prefab == null || maximum <= 0) return null;
            int count = 0;
            foreach (var slot in _particleSlots)
            {
                if (slot.prefab != prefab || slot.particles == null) continue;
                count++;
                if (slot.leased || slot.draining) continue;
                slot.leased = true;
                slot.particles.gameObject.SetActive(true);
                return slot.particles;
            }
            if (count >= maximum) return null;
            var instance = Instantiate(prefab, transform);
            var particles = instance.GetComponent<ParticleSystem>();
            if (particles == null) { Destroy(instance); return null; }
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _particleSlots.Add(new ParticleSlot { prefab = prefab, particles = particles, leased = true });
            return particles;
        }

        /// <summary>언로드는 즉시 비우고 소화는 기존 입자를 자연 종료한다.</summary>
        public void ReturnParticles(ParticleSystem particles, bool clear)
        {
            foreach (var slot in _particleSlots)
            {
                if (slot.particles != particles || !slot.leased) continue;
                slot.leased = false; slot.draining = !clear;
                particles.Stop(true, clear ? ParticleSystemStopBehavior.StopEmittingAndClear : ParticleSystemStopBehavior.StopEmitting);
                if (clear) particles.gameObject.SetActive(false);
                return;
            }
        }

        private void UpdateParticleReturns()
        {
            foreach (var slot in _particleSlots)
            {
                if (!slot.draining || slot.particles == null || slot.particles.IsAlive(true)) continue;
                slot.draining = false;
                slot.particles.gameObject.SetActive(false);
            }
        }

        private void DisposeParticleSlots()
        {
            foreach (var slot in _particleSlots) if (slot.particles != null) Destroy(slot.particles.gameObject);
            _particleSlots.Clear();
        }
    }
}
