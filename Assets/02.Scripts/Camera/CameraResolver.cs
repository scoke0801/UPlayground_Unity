using UnityEngine;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// CameraPose를 실제 Unity Camera/Pivot Transform에 반영한다.
    /// </summary>
    public sealed class CameraResolver
    {
        public void Apply(CameraPose pose, Camera mainCamera, Transform cameraPivot)
        {
            if (cameraPivot != null)
                cameraPivot.position = pose.PivotPosition;

            if (mainCamera == null)
                return;

            mainCamera.transform.position = pose.CameraPosition;
            mainCamera.transform.rotation = pose.CameraRotation;
            mainCamera.fieldOfView = pose.FieldOfView;
        }
    }
}
