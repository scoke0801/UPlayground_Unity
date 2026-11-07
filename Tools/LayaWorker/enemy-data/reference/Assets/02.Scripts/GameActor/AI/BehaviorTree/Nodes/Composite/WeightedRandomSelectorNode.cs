using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>
    /// 자식별 가중치를 기반으로 한 번 픽하고, 실패하면 남은 자식 중에서 다시 픽한다.
    /// 매 Tick 재롤하지 않으므로 Running 자식이 안정적으로 끝까지 실행된다.
    /// 가중치 리스트 길이가 자식 수와 다르면 누락분은 1f로 패딩한다.
    /// </summary>
    public class WeightedRandomSelectorNode : BTCompositeNode
    {
        [SerializeField] private List<float> _weights = new();

        private readonly List<int> _remaining = new();
        private int _currentIndex = -1;

        public IReadOnlyList<float> Weights => _weights;

        public float GetWeight(int childIndex)
        {
            if (childIndex < 0)
                return 0f;

            return childIndex < _weights.Count ? Mathf.Max(0f, _weights[childIndex]) : 1f;
        }

        public void SetWeight(int childIndex, float weight)
        {
            if (childIndex < 0)
                return;

            while (_weights.Count <= childIndex)
                _weights.Add(1f);

            _weights[childIndex] = Mathf.Max(0f, weight);
        }

        protected override void OnStart()
        {
            BuildRemainingPool();
            _currentIndex = PickNext();
        }

        protected override BTStatus OnUpdate()
        {
            if (Children.Count == 0)
                return BTStatus.Failure;

            while (_currentIndex >= 0)
            {
                var child = Children[_currentIndex];
                if (child == null)
                {
                    _currentIndex = PickNext();
                    continue;
                }

                var status = child.Tick();
                if (status == BTStatus.Running)
                    return BTStatus.Running;

                if (status == BTStatus.Success)
                    return BTStatus.Success;

                // Failure인 경우 남은 풀에서 다시 픽
                _currentIndex = PickNext();
            }

            return BTStatus.Failure;
        }

        protected override void OnStop()
        {
            AbortRunningChildren();
            _remaining.Clear();
            _currentIndex = -1;
        }

        protected override void OnReset()
        {
            _remaining.Clear();
            _currentIndex = -1;
        }

        private void BuildRemainingPool()
        {
            _remaining.Clear();
            for (var i = 0; i < Children.Count; i++)
            {
                if (Children[i] != null)
                    _remaining.Add(i);
            }
        }

        private int PickNext()
        {
            if (_remaining.Count == 0)
                return -1;

            var totalWeight = 0f;
            foreach (var idx in _remaining)
                totalWeight += GetWeight(idx);

            if (totalWeight <= 0f)
            {
                // 모든 가중치가 0이면 균등 랜덤
                var pickIndex = Random.Range(0, _remaining.Count);
                var chosen = _remaining[pickIndex];
                _remaining.RemoveAt(pickIndex);
                return chosen;
            }

            var roll = Random.value * totalWeight;
            var cumulative = 0f;
            for (var i = 0; i < _remaining.Count; i++)
            {
                cumulative += GetWeight(_remaining[i]);
                if (roll <= cumulative)
                {
                    var chosen = _remaining[i];
                    _remaining.RemoveAt(i);
                    return chosen;
                }
            }

            var fallback = _remaining[_remaining.Count - 1];
            _remaining.RemoveAt(_remaining.Count - 1);
            return fallback;
        }
    }
}
