using UnityEngine;

namespace UPlayGround.Animation.Editor
{
    public interface IMotionPreviewSubjectBinder
    {
        int Priority { get; }
        IMotionPreviewSubject TryBind(GameObject root);
    }
}
