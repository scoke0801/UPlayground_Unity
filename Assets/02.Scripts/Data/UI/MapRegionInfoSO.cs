using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Data.UI
{
    /// <summary>
    /// 맵 화면 좌하단 "지역 정보" 패널에 표시할 지역 메타데이터.
    /// 맵(씬)별로 하나씩 만들어 UI_Map의 _regionInfo(또는 MapConfigDatabaseSO)에 연결한다.
    /// </summary>
    [CreateAssetMenu(fileName = "MapRegionInfo", menuName = "UPlayGround/Map/Region Info")]
    public class MapRegionInfoSO : ScriptableObject
    {
        /// <summary>
        /// 브라우즈 모드(다른 지역 미리보기)에서 지도 위에 표시할 파스트트래블 포탈.
        /// 대상 지역이 로드돼 있지 않으므로 런타임 마커가 아닌 데이터로 저작한다.
        /// worldPosition은 대상 씬의 포탈 월드 좌표(PortalActor.transform.position 복사)를 넣으면,
        /// 대상 지역 Config의 WorldToMapImagePos로 지도상 위치가 계산된다.
        /// </summary>
        [Serializable]
        public struct PortalEntry
        {
            [Tooltip("포탈 표시 이름 (확인 팝업/툴팁에 사용)")]
            public string label;

            [Tooltip("대상 씬 내 포탈의 월드 좌표. 지도상 아이콘 위치 계산에 사용 (XZ 평면)")]
            public Vector3 worldPosition;

            [Tooltip("이동할 씬 이름 (SceneName 상수와 일치)")]
            public string targetSceneName;

            [Tooltip("도착 지점 식별자. 대상 씬의 SceneArrivalPoint.Id 와 일치. 비우면 씬 기본 스폰.")]
            public string arrivalId;
        }

        [Header("이름")]
        public string continentName;   // 예: 벨리안 대륙
        public string regionName;       // 예: 그레이우드 평원

        [Header("권장 레벨")]
        public int recommendedLevelMin;
        public int recommendedLevelMax;

        [Header("설명 / 썸네일")]
        [TextArea] public string description;
        public Sprite thumbnail;

        [Header("파스트트래블 포탈 (브라우즈 모드)")]
        [Tooltip("이 지역 지도에 표시할 포탈 목록. 지역 선택 후 아이콘 클릭 시 해당 씬으로 이동.")]
        public List<PortalEntry> portals = new();

        public string GetRecommendedLevelText()
        {
            if (recommendedLevelMax > recommendedLevelMin)
                return $"Lv. {recommendedLevelMin} ~ {recommendedLevelMax}";
            return $"Lv. {recommendedLevelMin}";
        }
    }
}
