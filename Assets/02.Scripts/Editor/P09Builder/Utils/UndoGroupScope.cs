using System;
using UnityEditor;

namespace UPlayGround.Editor.P09Builder
{
    public sealed class UndoGroupScope : IDisposable
    {
        public int GroupId { get; }
        public string GroupName { get; }

        public UndoGroupScope(string groupName)
        {
            GroupName = groupName;
            Undo.IncrementCurrentGroup();
            GroupId = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(groupName);
        }

        public void Collapse()
        {
            Undo.CollapseUndoOperations(GroupId);
        }

        public void Dispose()
        {
        }
    }
}
