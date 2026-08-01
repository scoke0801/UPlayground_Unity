#if UNITY_EDITOR
using System;
using UnityEditor;

namespace UPlayGround.Tool.Editor.Map
{
    /// <summary>
    /// 배치 작업 하나를 단일 Undo 그룹으로 묶는 스코프.
    /// <see cref="Complete"/>를 호출하고 종료하면 그룹을 collapse 해 'Ctrl+Z 1회 = 배치 1회'를 보장한다.
    /// Complete 없이 종료되면(예외/조기 return) 그룹 전체를 되돌려 부분 적용 상태를 남기지 않는다.
    /// </summary>
    public sealed class PlacementUndoScope : IDisposable
    {
        private readonly int _group;
        private bool _completed;
        private bool _disposed;

        public PlacementUndoScope(string label)
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(label);
            _group = Undo.GetCurrentGroup();
        }

        /// <summary>작업이 성공적으로 끝났음을 표시한다. 호출하지 않으면 Dispose 시 롤백된다.</summary>
        public void Complete()
        {
            _completed = true;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            if (!_completed)
            {
                Undo.RevertAllDownToGroup(_group);
                return;
            }

            Undo.CollapseUndoOperations(_group);
        }
    }
}
#endif
