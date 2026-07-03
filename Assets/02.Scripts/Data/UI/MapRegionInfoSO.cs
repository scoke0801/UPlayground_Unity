using UnityEngine;

namespace UPlayGround.Data.UI
{
    /// <summary>
    /// 맵 화면 좌하단 "지역 정보" 패널에 표시할 지역 메타데이터.
    /// 맵(씬)별로 하나씩 만들어 UI_Map의 _regionInfo에 연결한다.
    /// </summary>
    [CreateAssetMenu(fileName = "MapRegionInfo", menuName = "UPlayGround/Map/Region Info")]
    public class MapRegionInfoSO : ScriptableObject
    {
        [Header("이름")]
        public string continentName;   // 예: 벨리안 대륙
        public string regionName;       // 예: 그레이우드 평원

        [Header("권장 레벨")]
        public int recommendedLevelMin;
        public int recommendedLevelMax;

        [Header("설명 / 썸네일")]
        [TextArea] public string description;
        public Sprite thumbnail;

        public string GetRecommendedLevelText()
        {
            if (recommendedLevelMax > recommendedLevelMin)
                return $"Lv. {recommendedLevelMin} ~ {recommendedLevelMax}";
            return $"Lv. {recommendedLevelMin}";
        }
    }
}
