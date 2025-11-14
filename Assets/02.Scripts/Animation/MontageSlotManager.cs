using System.Collections.Generic;
using UnityEngine;

namespace Animation
{
    /// <summary>
    /// 슬롯 그룹을 관리하는 싱글톤 매니저
    /// 언리얼의 Anim Slot Manager와 유사한 기능
    /// 같은 그룹의 슬롯에서 몽타쥬가 재생되면 이전 몽타쥬를 자동으로 중단
    /// </summary>
    public class MontageSlotManager : MonoBehaviour
    {
        private static MontageSlotManager _instance;
        public static MontageSlotManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("MontageSlotManager");
                    _instance = go.AddComponent<MontageSlotManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        [System.Serializable]
        public class SlotGroup
        {
            public string groupName = "DefaultGroup";
            public List<string> slots = new List<string> { "DefaultSlot" };
        }
        
        [SerializeField] private List<SlotGroup> slotGroups = new List<SlotGroup>();
        
        // 현재 각 슬롯에서 재생 중인 몽타쥬 플레이어 추적
        private Dictionary<string, MontagePlayer> activeSlotPlayers = new Dictionary<string, MontagePlayer>();
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeDefaultSlots();
        }
        
        private void InitializeDefaultSlots()
        {
            if (slotGroups.Count == 0)
            {
                slotGroups.Add(new SlotGroup
                {
                    groupName = "DefaultGroup",
                    slots = new List<string> { "DefaultSlot", "UpperBody", "LowerBody", "FullBody" }
                });
            }
        }
        
        /// <summary>
        /// 몽타쥬 재생 시 슬롯 그룹 체크 및 이전 몽타쥬 중단
        /// </summary>
        public void RegisterMontagePlayback(MontagePlayer player, string slotName)
        {
            // 같은 슬롯 그룹의 다른 몽타쥬들을 찾아서 중단
            string groupName = GetGroupNameForSlot(slotName);
            if (!string.IsNullOrEmpty(groupName))
            {
                var slotsInGroup = GetSlotsInGroup(groupName);
                foreach (var slot in slotsInGroup)
                {
                    if (activeSlotPlayers.TryGetValue(slot, out var activePlayer))
                    {
                        if (activePlayer != null && activePlayer != player && activePlayer.IsPlaying)
                        {
                            Debug.Log($"Stopping montage in slot '{slot}' due to new montage in same group '{groupName}'");
                            activePlayer.StopMontage();
                        }
                    }
                }
            }
            
            // 현재 플레이어 등록
            activeSlotPlayers[slotName] = player;
        }
        
        /// <summary>
        /// 몽타쥬 종료 시 슬롯 해제
        /// </summary>
        public void UnregisterMontagePlayback(string slotName)
        {
            activeSlotPlayers.Remove(slotName);
        }
        
        /// <summary>
        /// 슬롯이 속한 그룹 이름 가져오기
        /// </summary>
        public string GetGroupNameForSlot(string slotName)
        {
            foreach (var group in slotGroups)
            {
                if (group.slots.Contains(slotName))
                {
                    return group.groupName;
                }
            }
            return null;
        }
        
        /// <summary>
        /// 특정 그룹의 모든 슬롯 가져오기
        /// </summary>
        public List<string> GetSlotsInGroup(string groupName)
        {
            var group = slotGroups.Find(g => g.groupName == groupName);
            return group?.slots ?? new List<string>();
        }
        
        /// <summary>
        /// 새 슬롯 그룹 추가
        /// </summary>
        public void AddSlotGroup(string groupName)
        {
            if (slotGroups.Exists(g => g.groupName == groupName))
            {
                Debug.LogWarning($"Slot group '{groupName}' already exists!");
                return;
            }
            
            slotGroups.Add(new SlotGroup { groupName = groupName, slots = new List<string>() });
        }
        
        /// <summary>
        /// 그룹에 슬롯 추가
        /// </summary>
        public void AddSlotToGroup(string groupName, string slotName)
        {
            var group = slotGroups.Find(g => g.groupName == groupName);
            if (group == null)
            {
                Debug.LogWarning($"Slot group '{groupName}' not found!");
                return;
            }
            
            if (!group.slots.Contains(slotName))
            {
                group.slots.Add(slotName);
            }
        }
    }
}
