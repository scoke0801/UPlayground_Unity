using UnityEngine;

namespace UPlayGround.Particle
{
    public class ActorDestroyParticle : MonoBehaviour
    {
        private ParticleSystem _particleSystem;

        void Awake()
        {
            _particleSystem = GetComponent<ParticleSystem>();
        }
        
        public void OnParticle(MeshRenderer meshRenderer)
        {
            UpdateParticleMesh(meshRenderer);
            _particleSystem.Play();
        }

        private void UpdateParticleMesh(MeshRenderer meshRenderer)
        {
            var shape = _particleSystem.shape;
            shape.meshRenderer = meshRenderer;
        }
    }
}