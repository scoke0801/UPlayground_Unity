using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Data.UI
{
    /// <summary>
    /// MapID → MinimapIconConfigSO 매핑 테이블.
    ///
    /// SceneContext.MapID 와 일치하는 항목을 찾아 미니맵·전체맵 UI에 공급한다.
    /// Assets/10.Datas/ 에 하나만 생성하고 UI_Minimap / UI_Map 인스펙터에 할당할 것.
    /// </summary>
    [CreateAssetMenu(fileName = "MapConfigDatabase", menuName = "UPlayGround/UI/MapConfigDatabase")]
    public class MapConfigDatabaseSO : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            [Tooltip("SceneContext.MapID 와 동일한 문자열")]
            public string             mapId;
            public MinimapIconConfigSO config;
        }

        [SerializeField] private List<Entry> _entries = new();

        /// <summary>
        /// mapId 에 해당하는 Config를 반환. 없으면 null.
        /// </summary>
        public MinimapIconConfigSO GetConfig(string mapId)
        {
            if (string.IsNullOrEmpty(mapId)) return null;

            foreach (var entry in _entries)
                if (entry.mapId == mapId) return entry.config;

            Debug.LogWarning($"[MapConfigDatabase] MapID '{mapId}'에 해당하는 Config를 찾을 수 없습니다.");
            return null;
        }
    }
}
