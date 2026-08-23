using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace UPlayGround.Data.Item
{
    [System.Serializable]
    public class ItemDropList
    {
        public ItemSO itemData;

        [Range(0.0f, 100.0f)] public float rate;

        [Min(1)] public int minimumDropCount = 1;

        [Min(1)] public int maximumDropCount = 1;

    }

    [System.Serializable]
    public class WeightedItemDropEntry
    {
        public ItemSO itemData;

        [Min(0.0f)] public float weight = 1.0f;

        [Min(1)] public int minimumDropCount = 1;

        [Min(1)] public int maximumDropCount = 1;

    }

    [System.Serializable]
    public class WeightedItemDropGroup
    {
        [Tooltip("에디터와 검증 로그에서 구분할 안정적인 그룹 이름")]
        public string groupId = "group";

        [Min(1)] public int rolls = 1;

        [Tooltip("아이템을 주지 않는 결과의 가중치. 0이면 유효 후보 중 하나가 반드시 선택된다.")]
        [Min(0.0f)] public float noDropWeight;

        [Tooltip("꺼져 있으면 같은 아이템은 한 그룹의 여러 회차에서 한 번만 선택된다.")]
        public bool allowDuplicateItems;

        public System.Collections.Generic.List<WeightedItemDropEntry> entries = new();
    }
}
