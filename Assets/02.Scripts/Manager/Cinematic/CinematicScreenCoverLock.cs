using UnityEngine;

namespace UPlayGround.Manager.Cinematic
{
    /// <summary>
    /// 화면이 덮여 있는 동안 플레이어 조작을 잠그고, 자신이 바꾼 상태만 되돌린다.
    /// 암전 중에도 입력이 살아 있으면 플레이어는 보이지 않는 채로 계속 이동·공격하다가
    /// 화면이 걷힌 순간 자기 위치와 카메라 방향을 잃는다.
    /// </summary>
    /// <remarks>
    /// 잠금은 화면 덮기 자체에 귀속된다. "화면이 가려졌는데 조작은 살아 있는" 상태를
    /// 저작으로 만들 수 있게 두면 그건 옵션이 아니라 버그이므로 요청 데이터로 뽑지 않는다.
    /// </remarks>
    internal sealed class CinematicScreenCoverLock
    {
        private IPlayerInputSuppressible _player;
        private bool _ownsPlayerInputLock;
        private bool _ownsActionInputLock;
        private bool _ownsCameraInputLock;
        private bool _isAcquired;

        public void Acquire()
        {
            if (_isAcquired)
                return;

            _isAcquired = true;

            // 플레이어 상태 머신 정지 + 이동 입력 초기화. 이미 잠겨 있으면(궁극기 등)
            // 소유권을 잡지 않아 바깥 잠금을 우리가 풀어버리지 않는다.
            _player = Svc.ActorQuery?.Player as IPlayerInputSuppressible;
            if (_player is { IsInputSuppressed: false })
            {
                _player.SetInputSuppressed(true);
                _ownsPlayerInputLock = true;
            }

            // 상태 머신을 세워도 InputManager는 같은 프레임 입력을 공유 InputBuffer에 계속 쌓는다.
            // 입력 레이어에서도 막지 않으면 암전 동안 눌린 공격이 화면이 걷히는 순간 한꺼번에 터진다.
            IInputService input = Svc.Input;
            if (input is { IsPlayerActionInputSuppressed: false })
            {
                input.SetPlayerActionInputSuppressed(true);
                _ownsActionInputLock = true;
            }

            // 보이지 않는 동안 시점이 돌아가면 걷힌 화면이 들어갈 때와 다른 방향을 보게 된다.
            if (CameraManager.Instance != null && !CameraManager.Instance.IsInputLocked())
            {
                CameraManager.Instance.SetInputLock(true);
                _ownsCameraInputLock = true;
            }
        }

        public void Release()
        {
            if (!_isAcquired)
                return;

            if (_ownsCameraInputLock && CameraManager.Instance != null)
                CameraManager.Instance.SetInputLock(false);

            if (_ownsActionInputLock)
            {
                IInputService input = Svc.Input;
                if (input != null)
                {
                    input.SetPlayerActionInputSuppressed(false);
                    // 암전 동안 쌓인 선입력은 플레이어가 화면을 보고 넣은 것이 아니므로 버린다.
                    input.InputBuffer?.Clear();
                }
            }

            // 인터페이스 참조는 Unity의 파괴 판정을 타지 않으므로 UnityEngine.Object로 확인한다.
            // (씬 전환으로 플레이어가 파괴된 채 화면 덮기가 끝나는 경로가 있다.)
            if (_ownsPlayerInputLock
                && _player is UnityEngine.Object playerObject
                && playerObject != null)
            {
                _player.SetInputSuppressed(false);
            }

            _player = null;
            _ownsPlayerInputLock = false;
            _ownsActionInputLock = false;
            _ownsCameraInputLock = false;
            _isAcquired = false;
        }
    }
}
