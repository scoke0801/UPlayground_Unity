using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Manager;

namespace UPlayGround.Components
{
    public sealed class UltimateRuntimeContext
    {
        public PlayerActor Caster { get; internal set; }
        public Transform PrimaryTarget { get; internal set; }
        public List<Transform> Targets { get; } = new();
        public UltimateSequenceAsset Asset { get; internal set; }
        public float ElapsedTime { get; internal set; }
        public bool IsInterrupted { get; internal set; }
        public CinematicStageTicket StageTicket { get; internal set; }

        private IFaceStripPresenter _faceStripPresenter;
        private long _faceStripToken;

        /// <summary>이 실행의 패널만 소유하고 동일 에셋의 다른 실행과 상태를 공유하지 않는다.</summary>
        public void ShowFaceStrip(IFaceStripPresenter presenter, FaceStripRequest request)
        {
            ReleaseFaceStrip();
            if (presenter == null)
                return;
            _faceStripPresenter = presenter;
            _faceStripToken = presenter.Show(request);
        }

        /// <summary>이전 실행의 종료가 새 패널을 숨기지 않도록 발급한 토큰만 반납한다.</summary>
        public void ReleaseFaceStrip()
        {
            _faceStripPresenter?.Hide(_faceStripToken);
            _faceStripPresenter = null;
            _faceStripToken = 0;
        }
    }
}
