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
    [CreateAssetMenu(fileName = "MapConfigDatabase", menuName = "UPlayGround/UI/Map Config Database")]
    public class MapConfigDatabaseSO : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            [Tooltip("SceneContext.MapID 와 동일한 문자열")]
            public string             mapId;
            public MinimapIconConfigSO config;

            [Tooltip("좌하단 지역 정보 패널 / 상단 브레드크럼에 표시할 지역 메타데이터")]
            public MapRegionInfoSO     regionInfo;
        }

        [SerializeField] private List<Entry> _entries = new();

        /// <summary> 등록된 모든 엔트리 (지역 선택 UI 등에서 순회용). </summary>
        public IReadOnlyList<Entry> Entries => _entries;

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

        /// <summary>
        /// mapId 에 해당하는 지역 메타데이터를 반환. 없으면 null (호출측 인스펙터 기본값으로 폴백).
        /// </summary>
        public MapRegionInfoSO GetRegionInfo(string mapId)
        {
            if (string.IsNullOrEmpty(mapId)) return null;

            foreach (var entry in _entries)
                if (entry.mapId == mapId) return entry.regionInfo;

            return null;
        }
    }
}
