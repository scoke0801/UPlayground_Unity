using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Manager;

namespace UPlayGround.Ability.PlayModeTests
{
    /// <summary>착탄 FX 재사용·중단이 다른 카메라 효과를 정리하지 않는지 검사한다.</summary>
    public sealed class SignatureImpactCameraTests
    {
        [Test]
        public void 착탄FX는_재활성화마다_한번재생하고_자기핸들만_해제한다()
        {
            Services.TryGet<ICameraViewService>(out var previous);
            var cameraObject = new GameObject("격리 카메라"); cameraObject.SetActive(false);
            var fx = new GameObject("착탄 FX"); fx.SetActive(false);
            Type cameraType = Type.GetType("UPlayGround.Manager.CameraManager, UPlayGround.Camera", throwOnError: true);
            Type managerType = Type.GetType("UPlayGround.CameraEffectManager, UPlayGround.Camera", throwOnError: true);
            Type dataType = Type.GetType("UPlayGround.Data.FOVCameraEffectData, UPlayGround.Camera", throwOnError: true);
            Type baseDataType = Type.GetType("UPlayGround.Data.CameraEffectData, UPlayGround.Camera", throwOnError: true);
            var data = ScriptableObject.CreateInstance(dataType);
            Component camera = null;
            ICameraViewService cameraService = null;
            try
            {
                camera = cameraObject.AddComponent(cameraType);
                cameraType.GetField("_effectManager", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(camera, Activator.CreateInstance(managerType, new object[] { null }));
                if (previous != null) Services.Unregister(previous);
                cameraService = (ICameraViewService)camera;
                Services.Register(cameraService);
                object other = cameraType.GetMethod("PlayEffect", new[] { baseDataType }).Invoke(camera, new object[] { data });
                var cue = fx.AddComponent<CameraEffectOnSpawn>();
                ((IList)typeof(CameraEffectOnSpawn).GetField("effects").GetValue(cue)).Add(data);
                fx.SetActive(true);
                var handles = (IList)typeof(CameraEffectOnSpawn)
                    .GetField("_handles", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(cue);
                Assert.That(handles, Has.Count.EqualTo(1));
                object first = handles[0];
                fx.SetActive(false);
                Assert.That(IsFinished(first), Is.True);
                Assert.That(IsFinished(other), Is.False);
                fx.SetActive(true);
                Assert.That(handles, Has.Count.EqualTo(1));
                Assert.That(handles[0], Is.Not.SameAs(first));
                fx.SetActive(false);
                Assert.That(IsFinished(other), Is.False);
            }
            finally
            {
                if (cameraService != null) Services.Unregister(cameraService);
                UnityEngine.Object.DestroyImmediate(fx);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(data);
                if (previous != null) Services.Register(previous);
            }
        }

        private static bool IsFinished(object effect)
            => (bool)effect.GetType().GetProperty("IsFinished").GetValue(effect);
    }
}
