using System.Collections.Generic;
using UPlayGround.Data.Event;
using UnityEngine;

namespace UPlayGround.Animation.Editor
{
    public interface IMotionEditorContext
    {
        MotionSetAsset Asset { get; }
        MotionSet CurrentSet { get; }
        Motion CurrentMotion { get; }
        MotionEventBase SelectedEvent { get; }
        IMotionPreviewSubject Subject { get; }
        IMotionSetCatalog Catalog { get; }
        string SelectedSlotId { get; }
        float PlaybackTime { get; }
        bool IsPlaying { get; }

        void Repaint();
        void RecordUndo(string label);
        bool SelectSlot(string slotId);
        bool TrySampleRootMotion(
            float fromTime,
            float toTime,
            out Vector3 deltaPosition,
            out Quaternion deltaRotation);
        void SetPlaybackTime(float time);
        void Play();
        void Stop();
        void SetOverlayTracks(
            string groupTitle,
            List<MotionSetDrawer.OverlayTrack> tracks);
    }

    public enum MotionPreviewPlaybackState
    {
        Stopped,
        Playing,
        Paused,
    }
}
