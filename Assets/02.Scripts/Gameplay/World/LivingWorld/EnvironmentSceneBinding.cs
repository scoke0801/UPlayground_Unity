using UnityEngine;
using UPlayGround.CameraSystem;
using UPlayGround.Manager;

namespace UPlayGround.Gameplay.World.LivingWorld
{
    /// <summary>현재 카메라와 전투 상태를 씬 환경의 관측점·집중 감쇠에 연결한다.</summary>
    [DisallowMultipleComponent]
    public sealed class EnvironmentSceneBinding : MonoBehaviour
    {
        [SerializeField] private EnvironmentController _environment;

        private void LateUpdate()
        {
            if (_environment == null) return;
            if (!Services.TryGet<ICameraViewService>(out var view))
            {
                _environment.BindObserver(null);
                _environment.SetFocus(false);
                return;
            }

            var camera = view.GetMainCamera();
            _environment.BindObserver(camera != null ? camera.transform : null);
            var player = Services.TryGet<IActorQueryService>(out var actors) ? actors.Player as PlayerActor : null;
            bool inCombat = player != null && player.GetCombat() != null && player.GetCombat().IsInCombat;
            _environment.SetFocus(inCombat || view.CurrentCameraMode != CameraModeType.InGame);
        }

        private void OnDisable()
        {
            if (_environment == null) return;
            _environment.BindObserver(null);
            _environment.SetFocus(false);
        }
    }
}
