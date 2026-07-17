using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Path
{
    /// <summary>
    /// ItemSO 데이터베이스
    /// 
    /// </summary>
    [CreateAssetMenu(fileName = "CameraShakeDatabase", menuName = "UPlayGround/데이터베이스/Camera Shake")]
    public class CameraShakeDatabase : ScriptableObject
    {
        [SerializeField] private List<CameraShakeData> allItems = new List<CameraShakeData>();

        private Dictionary<string, CameraShakeData> itemDictionary;

        public IReadOnlyList<CameraShakeData> AllItems => allItems;

        // 초기화 (게임 시작 시 호출)
        public void Initialize()
        {
            itemDictionary = new Dictionary<string, CameraShakeData>();

            foreach (var item in allItems)
            {
                if (item != null && !itemDictionary.ContainsKey(item.key))
                {
                    itemDictionary.Add(item.key, item);
                }
            }
        }

        public CameraShakeData GetShakeData(CameraShakeIdType key) => GetShakeData(key.ToKey());

        public CameraShakeData GetShakeData(string key)
        {
            if (itemDictionary == null)
                Initialize();

            return itemDictionary.TryGetValue(key, out var item) ? item : null;
        }
    }
}
