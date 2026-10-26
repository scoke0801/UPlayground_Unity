using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UPlayGround.Content.Tests
{
    /// <summary>입자 일괄 실내 판정이 회전·이동·재바인딩 후에도 개별 판정과 일치하는지 검사한다.</summary>
    public sealed class EnvironmentEffectsTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void 실내_입자만_제거하고_보호구역의_변경을_즉시_반영한다(bool inactive)
        {
            var root = new GameObject("환경 효과 검사");
            var shelterObject = new GameObject("보호 구역", typeof(BoxCollider));
            try
            {
                Type type = Type.GetType("UPlayGround.Gameplay.World.LivingWorld.EnvironmentEffects, Assembly-CSharp", true);
                Component effects = root.AddComponent(type);
                var particles = root.AddComponent<ParticleSystem>();
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = particles.main;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                particles.Play(false);
                particles.Pause(false);
                BoxCollider shelter = shelterObject.GetComponent<BoxCollider>();
                shelter.center = new Vector3(0.3f, 0.2f, -0.4f);
                shelter.size = new Vector3(2, 3, 4);
                shelterObject.transform.rotation = Quaternion.Euler(15, 41, 23);
                shelterObject.transform.localScale = new Vector3(2, 0.7f, 1.3f);
                shelterObject.SetActive(!inactive);
                MethodInfo bind = type.GetMethod("BindShelters");
                MethodInfo isSheltered = type.GetMethod("IsSheltered");
                MethodInfo cull = type.GetMethod("Cull", BindingFlags.Instance | BindingFlags.NonPublic);
                bind.Invoke(effects, new object[] { new[] { shelter, null }, (LayerMask)0 });

                for (int phase = 0; phase < 3; phase++)
                {
                    if (phase == 1) shelterObject.transform.position = new Vector3(7, 4, -2);
                    if (phase == 2) bind.Invoke(effects, new object[] { Array.Empty<BoxCollider>(), (LayerMask)0 });
                    Vector3 center = shelterObject.transform.TransformPoint(shelter.center);
                    Vector3[] positions = { center, center + Vector3.up * 100, center + Vector3.right * 0.1f };
                    var samples = positions.Select(p => new ParticleSystem.Particle
                    {
                        position = p, startLifetime = 10, remainingLifetime = 10, startSize = 1
                    }).ToArray();
                    Vector3[] expected = positions.Where(p => !(bool)isSheltered.Invoke(effects, new object[] { p })).ToArray();
                    particles.SetParticles(samples, samples.Length);
                    Assert.That(particles.particleCount, Is.EqualTo(samples.Length), "검사 입자 준비");
                    cull.Invoke(effects, new object[] { particles, new ParticleSystem.Particle[8] });
                    var remaining = new ParticleSystem.Particle[8];
                    int count = particles.GetParticles(remaining);
                    Vector3[] actual = remaining.Take(count).Where(p => p.remainingLifetime > 0).Select(p => p.position).ToArray();
                    Assert.That(actual, Is.EquivalentTo(expected), "단계 " + phase);
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(shelterObject);
            }
        }
    }
}
