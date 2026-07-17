#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    public partial class BehaviorTreeEditorWindow
    {
        // 트레이스는 노드 기록마다 Version이 올라 노드 viz(0.15s)와 같은 빈도로 리스트를 재구성하면
        // 비용이 크다. 별도 저빈도로 throttle하고, Label을 풀링/재사용해 매 갱신 전체 재생성을 막는다.
        private const double TraceViewRefreshInterval = 0.4d;
        private const int TraceViewMaxRows = 60;
        private double _nextTraceViewRefreshTime;
        private readonly List<Label> _traceRowPool = new();
        private Label _traceEmptyLabel;

        private void UpdateBreadcrumb(bool debugActive)
        {
            if (_breadcrumbBar == null)
                return;

            if (!debugActive || _debugRunner?.RuntimeTree?.RootNode == null)
            {
                _breadcrumbBar.style.display = DisplayStyle.None;
                _lastBreadcrumbTick = int.MinValue;
                return;
            }

            var tickKey = _debugRunner.DebugTrace?.CurrentTick ?? 0;
            if (tickKey == _lastBreadcrumbTick && _breadcrumbBar.style.display == DisplayStyle.Flex)
                return;

            _lastBreadcrumbTick = tickKey;

            var runtimePath = BuildBreadcrumbPath(_debugRunner.RuntimeTree.RootNode);
            _breadcrumbBar.Clear();

            if (runtimePath.Count == 0)
            {
                _breadcrumbBar.style.display = DisplayStyle.None;
                return;
            }

            _breadcrumbBar.style.display = DisplayStyle.Flex;
            for (var i = 0; i < runtimePath.Count; i++)
            {
                var node = runtimePath[i];
                var label = new Label(node.DisplayName)
                {
                    tooltip = $"{node.GetType().Name}\nGuid: {node.Guid}"
                };
                label.style.color = i == runtimePath.Count - 1
                    ? new Color(0.48f, 0.96f, 0.56f)
                    : new Color(0.78f, 0.85f, 0.92f);
                label.style.unityFontStyleAndWeight = i == runtimePath.Count - 1 ? FontStyle.Bold : FontStyle.Normal;
                label.style.fontSize = 11f;
                label.style.paddingLeft = 4f;
                label.style.paddingRight = 4f;
                var guid = node.Guid;
                label.RegisterCallback<MouseDownEvent>(evt =>
                {
                    if (evt.button != 0)
                        return;
                    _graphView?.FocusNodeByGuid(guid);
                    evt.StopPropagation();
                });
                _breadcrumbBar.Add(label);

                if (i < runtimePath.Count - 1)
                {
                    var sep = new Label("›");
                    sep.style.color = new Color(0.40f, 0.40f, 0.50f);
                    sep.style.unityFontStyleAndWeight = FontStyle.Bold;
                    sep.style.fontSize = 13f;
                    sep.style.paddingLeft = 2f;
                    sep.style.paddingRight = 2f;
                    _breadcrumbBar.Add(sep);
                }
            }
        }

        private static List<BTNode> BuildBreadcrumbPath(BTNode root)
        {
            var path = new List<BTNode>();
            var current = root;
            var depth = 0;
            while (current != null && current.IsRunning && depth < BreadcrumbMaxDepth)
            {
                path.Add(current);
                BTNode nextRunning = null;
                foreach (var child in current.Children)
                {
                    if (child == null || !child.IsRunning)
                        continue;
                    nextRunning = child;
                    break;
                }

                if (nextRunning == null)
                    break;
                current = nextRunning;
                depth++;
            }

            return path;
        }

        private void OnEditorUpdate()
        {
            if (_graphView == null)
                return;

            var debugActive = Application.isPlaying && _debugRunner != null && _debugRunner.DebugMode;
            var trace = debugActive ? _debugRunner.DebugTrace : null;
            var traceVersion = trace?.Version ?? -1;
            var traceTick = trace?.CurrentTick ?? -1;
            var debugStateChanged = HasDebugStateChanged();
            var traceChanged = traceVersion != _lastTraceVersion || traceTick != _lastTraceTick;
            var graphNeedsClear = _debugGraphWasActive && !debugActive;
            var now = EditorApplication.timeSinceStartup;

            if (!debugActive && !graphNeedsClear && !debugStateChanged)
                return;

            if (debugActive && _debugRunner.State == BehaviorTreeRunnerState.Running && !debugStateChanged && now < _nextDebugRefreshTime)
                return;

            if (debugActive && !traceChanged && !debugStateChanged)
            {
                if (_debugRunner.State != BehaviorTreeRunnerState.Running)
                    return;
            }

            _nextDebugRefreshTime = now + DebugRefreshInterval;
            _lastTraceVersion = traceVersion;
            _lastTraceTick = traceTick;
            _debugGraphWasActive = debugActive;

            var runtimeTree = debugActive ? _debugRunner.RuntimeTree : null;
            if (debugActive || graphNeedsClear || traceChanged)
            {
                // 디버그 종료(graphNeedsClear) 시에는 캐시를 무시하고 전 노드/엣지를 강제로 Idle로 되돌린다.
                _graphView.UpdateDebugState(runtimeTree, trace, force: graphNeedsClear);
                if (_miniMapView != null && _miniMapToggle?.value == true)
                    _miniMapView.MarkDirtyRepaint();
                if (_activeTab == PropertyTab.Variables)
                    _blackboardView?.MarkDirtyRepaint();
                if (_activeTab == PropertyTab.Timeline)
                    _timelineView?.RefreshIfNeeded();
            }
            FocusBreakpointNodeIfNeeded();
            UpdateBreadcrumb(debugActive);
            RefreshDebugState();
            RefreshTraceView(traceVersion);
        }

        private void FocusBreakpointNodeIfNeeded()
        {
            if (!Application.isPlaying || _debugRunner == null || _debugRunner.State != BehaviorTreeRunnerState.Paused)
            {
                _lastFocusedPauseGuid = null;
                return;
            }

            var pauseNode = _debugRunner.PauseRequestedBy;
            if (pauseNode == null || string.IsNullOrWhiteSpace(pauseNode.Guid) || pauseNode.Guid == _lastFocusedPauseGuid)
                return;

            if (_graphView != null && _graphView.FocusNodeByGuid(pauseNode.Guid))
                _lastFocusedPauseGuid = pauseNode.Guid;
        }

        private bool HasDebugStateChanged()
        {
            if (_debugRunner == null)
                return _lastDebugState != (BehaviorTreeRunnerState)(-1) ||
                       _lastExecutionStatus != (BTStatus)(-1) ||
                       _lastDebugMode;

            return _lastDebugState != _debugRunner.State ||
                   _lastExecutionStatus != _debugRunner.ExecutionStatus ||
                   _lastDebugMode != _debugRunner.DebugMode;
        }

        private void RefreshDebugState()
        {
            if (_debugStateLabel == null)
                return;

            if (_debugRunner == null)
            {
                _debugStateLabel.text = "No Runner";
                _debugStateLabel.style.backgroundColor = new Color(0.30f, 0.30f, 0.30f);
                if (_runtimeBanner != null)
                    _runtimeBanner.style.display = DisplayStyle.None;
                _lastDebugState = (BehaviorTreeRunnerState)(-1);
                _lastExecutionStatus = (BTStatus)(-1);
                _lastDebugMode = false;
                return;
            }

            _debugStateLabel.text = $"{_debugRunner.State}  |  {_debugRunner.ExecutionStatus}";
            _debugStateLabel.style.backgroundColor = _debugRunner.State switch
            {
                BehaviorTreeRunnerState.Running => new Color(0.18f, 0.50f, 0.28f),
                BehaviorTreeRunnerState.Paused => new Color(0.72f, 0.45f, 0.12f),
                _ => new Color(0.30f, 0.30f, 0.30f)
            };
            if (_runtimeBanner != null)
                _runtimeBanner.style.display = Application.isPlaying && _debugRunner.DebugMode
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            _lastDebugState = _debugRunner.State;
            _lastExecutionStatus = _debugRunner.ExecutionStatus;
            _lastDebugMode = _debugRunner.DebugMode;
        }

        private void RefreshTraceView(int traceVersion)
        {
            if (_traceBox == null || _activeTab != PropertyTab.Trace)
                return;

            // 실행 중에는 Version이 틱마다 수십 번 올라 노드 viz와 같은 빈도로 갱신하면 사람이 읽지도 못하고
            // 비용만 크다. 일시정지/스텝 중이 아니면 저빈도로 throttle한다(첫 호출은 즉시 렌더).
            bool running = _debugRunner != null && _debugRunner.State == BehaviorTreeRunnerState.Running;
            var now = EditorApplication.timeSinceStartup;
            if (running && _lastTraceViewVersion >= 0 && now < _nextTraceViewRefreshTime)
                return;

            if (_lastTraceViewVersion == traceVersion)
                return;

            _lastTraceViewVersion = traceVersion;
            _nextTraceViewRefreshTime = now + TraceViewRefreshInterval;

            var trace = _debugRunner != null && _debugRunner.DebugMode ? _debugRunner.DebugTrace : null;
            if (trace == null || trace.Records.Count == 0)
            {
                ShowTraceEmpty(true);
                for (int i = 0; i < _traceRowPool.Count; i++)
                    _traceRowPool[i].style.display = DisplayStyle.None;
                return;
            }

            ShowTraceEmpty(false);

            // 가장 최근 TraceViewMaxRows개만 표시한다. Queue는 오래된→최신 순이라 앞쪽 (Count-N)개를 건너뛴다.
            int total = trace.Records.Count;
            int skip = Mathf.Max(0, total - TraceViewMaxRows);
            int visible = total - skip;
            EnsureTraceRowCount(visible);

            int rowIndex = 0;
            int idx = 0;
            foreach (var record in trace.Records)
            {
                if (idx++ < skip)
                    continue;

                var row = _traceRowPool[rowIndex++];
                row.style.display = DisplayStyle.Flex;
                row.text = $"#{record.Tick} {record.EventType,-16} {record.Status,-7} {record.NodeName} [{ShortGuid(record.NodeGuid)}] {record.Detail}";
                row.style.color = GetTraceColor(record);
                row.userData = record.NodeGuid;
            }

            for (int i = rowIndex; i < _traceRowPool.Count; i++)
                _traceRowPool[i].style.display = DisplayStyle.None;
        }

        // Label을 풀에서 재사용한다. 매 갱신마다 새 Label/콜백/문자열을 생성·파괴하던 비용을 제거한다.
        private void EnsureTraceRowCount(int count)
        {
            while (_traceRowPool.Count < count)
            {
                var row = new Label
                {
                    tooltip = "클릭하면 해당 노드로 이동합니다."
                };
                row.style.fontSize = 10f;
                row.style.whiteSpace = WhiteSpace.Normal;
                row.style.marginBottom = 2f;
                row.style.paddingLeft = 4f;
                row.style.paddingRight = 4f;
                row.RegisterCallback<MouseDownEvent>(OnTraceRowClicked);
                _traceRowPool.Add(row);
                _traceBox.Add(row);
            }
        }

        private void OnTraceRowClicked(MouseDownEvent evt)
        {
            if (evt.button != 0 || evt.currentTarget is not Label row || row.userData is not string guid)
                return;

            _graphView?.FocusNodeByGuid(guid);
            evt.StopPropagation();
        }

        private void ShowTraceEmpty(bool show)
        {
            if (_traceEmptyLabel == null)
            {
                _traceEmptyLabel = new Label("Play Mode에서 Debug Runner를 지정하면 최근 Tick Trace가 표시됩니다.");
                _traceEmptyLabel.style.color = new Color(0.72f, 0.72f, 0.72f);
                _traceEmptyLabel.style.whiteSpace = WhiteSpace.Normal;
                _traceBox.Insert(0, _traceEmptyLabel);
            }

            _traceEmptyLabel.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static string ShortGuid(string guid)
        {
            if (string.IsNullOrWhiteSpace(guid))
                return "none";

            return guid.Length > 8 ? guid.Substring(0, 8) : guid;
        }

        private static Color GetTraceColor(BehaviorTreeDebugTraceRecord record)
        {
            if (record.EventType == "Breakpoint" || record.EventType == "ConditionalAbort")
                return new Color(1f, 0.72f, 0.28f);

            return record.Status switch
            {
                BTStatus.Success => new Color(0.46f, 0.86f, 0.52f),
                BTStatus.Failure => new Color(0.95f, 0.38f, 0.34f),
                BTStatus.Running => new Color(0.95f, 0.72f, 0.24f),
                _ => new Color(0.78f, 0.78f, 0.78f)
            };
        }
    }
}
#endif
