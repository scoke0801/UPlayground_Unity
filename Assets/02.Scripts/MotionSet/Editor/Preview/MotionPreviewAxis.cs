using System;
using System.Collections.Generic;

namespace UPlayGround.Animation.Editor
{
    [Serializable]
    public sealed class MotionPreviewAxis
    {
        public string Id;
        public string DisplayName;
        public IReadOnlyList<MotionPreviewAxisOption> Options;
        public bool AffectsCatalog;
    }

    [Serializable]
    public sealed class MotionPreviewAxisOption
    {
        public string Id;
        public string DisplayName;
    }
}
