using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Quest;

namespace UPlayGround.Data.UI
{
    /// <summary>
    /// 퀘스트 목표 타입별 월드 마커 아이콘·색상 매핑.
    /// 목표의 성격(대화 / 전투 / 그 외)을 실루엣과 색으로 구분해,
    /// 플레이어가 마커만 보고 무엇을 하러 가는지 알 수 있게 한다.
    /// 매핑에 없는 타입은 기본값을 쓴다.
    /// </summary>
    [CreateAssetMenu(fileName = "QuestMarkerIconSet", menuName = "UPlayGround/UI/Quest Marker Icon Set")]
    public class QuestMarkerIconSetSO : ScriptableObject
    {
        /// <summary>목표 타입 하나에 대한 아이콘·색 지정.</summary>
        [Serializable]
        public class Entry
        {
            public QuestObjectiveType type;

            [Tooltip("비우면 기본 아이콘을 쓴다.")]
            public Sprite icon;

            public Color color = Color.white;
        }

        [Header("기본값")]
        [Tooltip("매핑에 없는 목표 타입이 사용할 아이콘")]
        [SerializeField] private Sprite _defaultIcon;

        [SerializeField] private Color _defaultColor = new Color(1f, 0.85f, 0.2f, 1f);

        [Header("목표 타입별 매핑")]
        [Tooltip("위에서부터 처음 일치하는 항목을 쓴다. 같은 타입을 중복 등록하지 않는다.")]
        [SerializeField] private List<Entry> _entries = new List<Entry>();

        /// <summary>
        /// 목표 타입에 대응하는 아이콘과 색상을 해석한다. 매핑이 없으면 기본값을 돌려준다.
        /// 퀘스트 상태 변화 시에만 호출되는 경로라 선형 탐색으로 충분하다.
        /// </summary>
        public void Resolve(QuestObjectiveType type, out Sprite icon, out Color color)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                Entry entry = _entries[i];
                if (entry == null || entry.type != type)
                    continue;

                icon = entry.icon != null ? entry.icon : _defaultIcon;
                color = entry.color;
                return;
            }

            icon = _defaultIcon;
            color = _defaultColor;
        }
    }
}
