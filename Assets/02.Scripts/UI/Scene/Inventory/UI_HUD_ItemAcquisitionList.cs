using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Item;

namespace UPlayGround.UI
{
    /// <summary>짧은 시간 안에 들어온 같은 아이템을 하나의 획득 알림으로 병합한다.</summary>
    public class UI_HUD_ItemAcquisitionList : UI_Base
    {
        [SerializeField] private UI_HUD_ItemAcquisitionEntry _itemEntry;
        [SerializeField] private Transform _content;
        [Min(0f)] [SerializeField] private float _mergeWindow = 0.45f;

        // 장비처럼 스택되지 않는 아이템이 한꺼번에 떨어지면 항목이 순식간에 쌓여
        // 목록 영역을 넘어간다. 넘친 항목은 화면 밖으로 나가므로 오래된 것부터 밀어낸다.
        [Min(1)] [SerializeField] private int _maxVisibleEntries = 5;

        private readonly Dictionary<int, UI_HUD_ItemAcquisitionEntry> _mergeTargets = new();
        private readonly Dictionary<int, float> _mergeUpdateTimes = new();
        private readonly List<UI_HUD_ItemAcquisitionEntry> _activeEntries = new();

        /// <summary>아이템 획득을 표시하고 병합 시간 안의 동일 아이템이면 수량만 갱신한다.</summary>
        public void SetItem(ItemSO itemData, int count)
        {
            if (itemData == null || _itemEntry == null || _content == null)
                return;

            int addedCount = Mathf.Max(1, count);
            float now = Time.unscaledTime;
            if (TryMerge(itemData.itemId, addedCount, now))
                return;

            TrimOverflow();

            UI_HUD_ItemAcquisitionEntry entry = Instantiate(_itemEntry, _content);
            entry.gameObject.SetActive(true);
            entry.Init(itemData, addedCount, OnEntryExpired);

            _activeEntries.Add(entry);
            _mergeTargets[itemData.itemId] = entry;
            _mergeUpdateTimes[itemData.itemId] = now;
        }

        private bool TryMerge(int itemId, int count, float now)
        {
            if (!_mergeTargets.TryGetValue(itemId, out UI_HUD_ItemAcquisitionEntry entry))
                return false;

            if (entry == null || now - _mergeUpdateTimes[itemId] > _mergeWindow)
                return false;

            entry.AddCount(count);
            _mergeUpdateTimes[itemId] = now;
            return true;
        }

        /// <summary>표시 한도를 넘기기 전에 가장 오래된 항목부터 퇴장시킨다.</summary>
        private void TrimOverflow()
        {
            while (_activeEntries.Count >= _maxVisibleEntries)
            {
                UI_HUD_ItemAcquisitionEntry oldest = _activeEntries[0];
                if (oldest == null)
                {
                    _activeEntries.RemoveAt(0);
                    continue;
                }

                // 즉시 퇴장은 콜백으로 목록에서 스스로 빠지므로 여기서 직접 제거하지 않는다.
                _activeEntries.RemoveAt(0);
                RemoveMergeTarget(oldest);
                oldest.ExpireImmediately();
            }
        }

        private void OnEntryExpired(UI_HUD_ItemAcquisitionEntry entry)
        {
            if (entry == null)
                return;

            _activeEntries.Remove(entry);
            RemoveMergeTarget(entry);
        }

        private void RemoveMergeTarget(UI_HUD_ItemAcquisitionEntry entry)
        {
            int expiredKey = 0;
            bool hasExpiredKey = false;
            foreach (KeyValuePair<int, UI_HUD_ItemAcquisitionEntry> pair in _mergeTargets)
            {
                if (pair.Value != entry)
                    continue;

                expiredKey = pair.Key;
                hasExpiredKey = true;
                break;
            }

            if (!hasExpiredKey)
                return;

            _mergeTargets.Remove(expiredKey);
            _mergeUpdateTimes.Remove(expiredKey);
        }

        protected override void OnDispose()
        {
            _mergeTargets.Clear();
            _mergeUpdateTimes.Clear();
            _activeEntries.Clear();
            base.OnDispose();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _mergeWindow = Mathf.Max(0f, _mergeWindow);
            _maxVisibleEntries = Mathf.Max(1, _maxVisibleEntries);
        }
#endif
    }
}
