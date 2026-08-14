using System;
using UnityEditor;

namespace UPlayGround.Editor.P09Builder
{
    public sealed class UndoGroupScope : IDisposable
    {
        private bool _reverted;

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
            if (!_reverted)
                Undo.CollapseUndoOperations(GroupId);
        }

        public void Revert()
        {
            if (_reverted)
                return;

            Undo.FlushUndoRecordObjects();
            Undo.RevertAllDownToGroup(GroupId);
            _reverted = true;
        }

        public void Dispose()
        {
        }
    }
}
