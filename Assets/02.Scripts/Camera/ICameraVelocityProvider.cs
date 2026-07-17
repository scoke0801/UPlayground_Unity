using UnityEngine;

namespace UPlayGround.CameraSystem
{
    public interface ICameraVelocityProvider
    {
        Vector3 CameraVelocity { get; }
    }
}
