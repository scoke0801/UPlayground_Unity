using UnityEngine;
using UPlayGround.Manager;

namespace UPlayGround.CameraEffects
{
    public readonly struct CameraEffectContext
    {
        public CameraManager Manager { get; }
        public Camera Camera { get; }
        public Transform Target { get; }
        public float BaseDistance { get; }
        public float BaseFov { get; }

        public CameraEffectContext(CameraManager manager, Camera camera, Transform target, float baseDistance, float baseFov)
        {
            Manager = manager;
            Camera = camera;
            Target = target;
            BaseDistance = baseDistance;
            BaseFov = baseFov;
        }
    }
}
