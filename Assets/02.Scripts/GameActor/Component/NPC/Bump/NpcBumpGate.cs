using UPlayGround.Manager;

namespace UPlayGround.Components
{
    /// <summary>
    /// 스토리·대화·연출이 진행 중인지 판정한다. 이 동안에는 어떤 NPC도 부딪혀 밀리면 안 된다.
    /// 연출은 NPC 위치와 자세를 직접 잡으므로 충돌 반응이 끼어들면 장면이 깨진다.
    /// </summary>
    public static class NpcBumpGate
    {
        public static bool IsStoryPresentationActive()
        {
            if (Svc.Dialogue != null && Svc.Dialogue.IsDialogueActive)
                return true;

            CameraManager camera = CameraManager.Instance;
            return camera != null
                   && (camera.IsInputLocked()
                       || camera.IsDialogueCameraRecordingActive()
                       || camera.IsCameraSnapshotSequenceActive());
        }
    }
}
