#if UNITY_EDITOR
using System.Collections.Generic;
using UPlayGround.AI.CombatDecision;
using UPlayGround.AI.Debugging;
using UnityEngine.UIElements;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    public sealed class IntentScoreTimelineView : VisualElement
    {
        private readonly IMGUIContainer _container;
        private readonly List<IntentScoreSnapshot> _snapshots = new();
        private BehaviorTreeRunner _runner;
        private IntentScoreTimeline _timeline;
        private int _lastVersion = -1;
        private bool _showingReplay;

        public IntentScoreTimelineView()
        {
            style.flexGrow = 1;
            style.backgroundColor = BehaviorTreeEditorStyles.Panel;
            _container = new IMGUIContainer(Draw);
            _container.style.flexGrow = 1;
            Add(_container);
        }

        public void SetDebugRunner(BehaviorTreeRunner runner)
        {
            _runner = runner;
            _timeline = _runner != null ? _runner.GetComponent<IntentScoreTimeline>() : null;
            _lastVersion = -1;
            _showingReplay = false;
            RefreshIfNeeded(force: true);
        }

        public void SetReplay(EncounterReplay replay)
        {
            _showingReplay = replay != null;
            _lastVersion = -1;
            _snapshots.Clear();
            if (replay?.frames != null)
            {
                foreach (var frame in replay.frames)
                {
                    var scores = frame.scores ?? new float[0];
                    _snapshots.Add(new IntentScoreSnapshot(
                        frame.t,
                        frame.selectedIntent,
                        frame.lastIntent,
                        frame.consecutiveIntentCount,
                        GetScore(scores, 0),
                        GetScore(scores, 1),
                        GetScore(scores, 2),
                        GetScore(scores, 3),
                        GetScore(scores, 4),
                        GetScore(scores, 5),
                        GetScore(scores, 6),
                        GetScore(scores, 7),
                        GetScore(scores, 8),
                        frame.rhythmPhase,
                        frame.reason));
                }
            }
            _container.MarkDirtyRepaint();
        }

        public void RefreshIfNeeded(bool force = false)
        {
            if (_runner != null && _timeline == null)
                _timeline = _runner.GetComponent<IntentScoreTimeline>();

            if (_showingReplay && !force)
                return;

            var version = _timeline != null ? _timeline.Version : -1;
            if (!force && version == _lastVersion)
                return;

            _lastVersion = version;
            _snapshots.Clear();
            _timeline?.TryCopySnapshots(_snapshots);
            _container.MarkDirtyRepaint();
        }

        private void Draw()
        {
            IntentScoreTimelineRenderer.Draw(_snapshots);
        }

        private static float GetScore(float[] scores, int index)
            => scores != null && index >= 0 && index < scores.Length ? scores[index] : 0f;
    }
}
#endif
