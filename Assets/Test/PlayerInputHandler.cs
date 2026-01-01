using UnityEngine;

public class PlayerInputHandler : MonoBehaviour
{
    public MyPlayerController Controller;
    public Camera PlayerCamera;

    private void Update()
    {
        if (Controller == null) return;

        PlayerCharacterInputs inputs = new PlayerCharacterInputs();
        inputs.MoveAxisForward = Input.GetAxisRaw("Vertical");
        inputs.MoveAxisRight = Input.GetAxisRaw("Horizontal");
        inputs.CameraRotation = PlayerCamera != null ? PlayerCamera.transform.rotation : Quaternion.identity;
        inputs.JumpDown = Input.GetKeyDown(KeyCode.Space);

        Controller.SetInputs(ref inputs);
    }
}