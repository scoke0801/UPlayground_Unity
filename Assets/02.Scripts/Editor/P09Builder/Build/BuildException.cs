using System;

namespace Game.Editor.P09Builder
{
    public sealed class BuildException : Exception
    {
        public BuildException(string message) : base(message) { }
        public BuildException(string message, Exception inner) : base(message, inner) { }
    }
}
