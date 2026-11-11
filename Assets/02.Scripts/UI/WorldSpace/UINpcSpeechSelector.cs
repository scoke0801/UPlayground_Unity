using System;
using System.Collections.Generic;
using UPlayGround.Data.Actor.NPC;

namespace UPlayGround.UI
{
    /// <summary>행동·시간·문장 이력·주변 소재를 함께 검사해 반복이 적은 발화를 고른다.</summary>
    public sealed class UINpcSpeechSelector
    {
        /// <summary>NPC 한 명이 해당 목소리에서 실제 표시한 대사 이력이다.</summary>
        public sealed class History
        {
            internal readonly int[] counts;
            internal readonly float[] times;
            internal readonly Dictionary<string, float> topics = new();

            public History(int lineCount)
            {
                counts = new int[lineCount];
                times = new float[lineCount];
                for (int i = 0; i < times.Length; i++) times[i] = float.NegativeInfinity;
            }
        }

        private readonly NpcAmbientSpeechSO _settings;
        private readonly Random _random;
        private readonly Queue<(string id, float expires)> _recent = new();
        private readonly Dictionary<string, float> _recentIds = new();
        private readonly Dictionary<string, float> _topics = new();

        public UINpcSpeechSelector(NpcAmbientSpeechSO settings, int seed)
        { _settings = settings; _random = new Random(seed); }

        /// <summary>조건에 맞는 새 후보가 없으면 침묵하고, 선택된 경우에만 이력을 기록한다.</summary>
        public bool TryChoose(NpcAmbientSpeechSO.Voice voice, History history, NpcSpeechActivity activity,
            NpcSpeechPeriod period, float now, out NpcAmbientSpeechSO.Line line)
        {
            line = null;
            if (voice?.lines == null || history == null || history.counts.Length != voice.lines.Length) return false;
            // 충돌 반응처럼 작은 후보군도 시간이 지나면 다시 쓸 수 있어야 한다.
            while (_recent.Count > 0 && now >= _recent.Peek().expires) RemoveOldestRecent();
            float cooldown = activity == NpcSpeechActivity.Bumped ? _settings.reactionCooldown : _settings.lineCooldown;
            float topicCooldown = activity == NpcSpeechActivity.Bumped ? _settings.reactionCooldown : _settings.topicCooldown;
            int selected = -1, minCount = int.MaxValue, candidates = 0;
            for (int i = 0; i < voice.lines.Length; i++)
            {
                var candidate = voice.lines[i];
                if (candidate == null || string.IsNullOrWhiteSpace(candidate.text)
                    || string.IsNullOrWhiteSpace(candidate.id) || string.IsNullOrWhiteSpace(candidate.topic)
                    || !candidate.CanSpeak(activity, period)
                    || _recentIds.TryGetValue(candidate.id, out float expires) && now < expires
                    || now - history.times[i] < cooldown
                    || IsTopicRecent(history.topics, candidate.topic, now, topicCooldown) || IsTopicRecent(_topics, candidate.topic, now, topicCooldown)
                    || history.counts[i] > minCount) continue;
                if (history.counts[i] < minCount) { minCount = history.counts[i]; candidates = 0; }
                if (_random.Next(++candidates) == 0) selected = i;
            }
            if (selected < 0) return false;
            line = voice.lines[selected];
            history.counts[selected]++;
            history.times[selected] = now;
            history.topics[line.topic] = now;
            _topics[line.topic] = now;
            _recent.Enqueue((line.id, now + cooldown));
            _recentIds[line.id] = now + cooldown;
            while (_recent.Count > _settings.globalRecentLines) RemoveOldestRecent();
            return true;
        }

        private void RemoveOldestRecent()
        {
            var oldest = _recent.Dequeue();
            if (_recentIds.TryGetValue(oldest.id, out float expires) && expires == oldest.expires)
                _recentIds.Remove(oldest.id);
        }

        private static bool IsTopicRecent(Dictionary<string, float> topics, string topic, float now, float cooldown)
            => topics.TryGetValue(topic, out float last) && now - last < cooldown;
    }
}
