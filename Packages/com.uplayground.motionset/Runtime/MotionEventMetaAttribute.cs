using System;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// MotionEvent 추가 팝업 카탈로그와 타임라인 스타일(색/아이콘) 메타.
    /// 구체 이벤트 클래스에 부착하면 에디터가 TypeCache 스캔으로 자동 수집한다.
    /// 미부착 타입도 팝업에 노출되며 기본값(Utility 분류, 회색 ▸)으로 표시된다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class MotionEventMetaAttribute : Attribute
    {
        /// <summary>팝업/타임라인 표시 이름.</summary>
        public string DisplayName { get; }

        public MotionEventMetaAttribute(string displayName)
        {
            DisplayName = displayName;
        }

        /// <summary>팝업 그룹 이름. 같은 이름끼리 묶인다.</summary>
        public string Category { get; set; } = "Utility";

        /// <summary>카테고리 정렬 순서. 같은 이름 카테고리에 여러 값이 오면 최솟값을 쓴다.</summary>
        public int CategoryOrder { get; set; } = 40;

        /// <summary>팝업 항목 아래 표시되는 한 줄 설명.</summary>
        public string Description { get; set; }

        /// <summary>팝업 검색 별칭.</summary>
        public string[] Aliases { get; set; }

        /// <summary>타임라인 바/팝업 아이콘. 미지정 시 "▸".</summary>
        public string Icon { get; set; }

        /// <summary>타임라인 바 색 (r, g, b — 0~1). 미지정 시 회색.</summary>
        public float[] Color { get; set; }
    }
}
