using System;
using UnityEngine;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.UI
{
    [CreateAssetMenu(fileName = "MinimapIconConfig", menuName = "UPlayGround/UI/MinimapIconConfig")]
    public class MinimapIconConfigSO : ScriptableObject
    {
        [Serializable]
        public struct IconEntry
        {
            public Sprite sprite;
            public Color  color;
            [Range(8f, 40f)]
            public float size;
        }

        // ── 액터 타입별 기본 아이콘 ──────────────────────────────
        [Header("액터 아이콘")]
        public IconEntry player;
        [Tooltip("적 기본 (비전투 상태)")]
        public IconEntry enemy;
        [Tooltip("적 전투 상태 (플레이어를 인식한 경우)")]
        public IconEntry enemyDetected;
        public IconEntry npc;
        public IconEntry gathering;

        // ── 퀘스트·마커 아이콘 ───────────────────────────────────
        [Header("퀘스트 마커 아이콘")]
        [Tooltip("ReachLocation 목표 지점 마커")]
        public IconEntry questTarget;
        [Tooltip("NPC 전달/대화 목표 마커")]
        public IconEntry questNpc;
        [Tooltip("MinimapMarkerType.Custom 마커")]
        public IconEntry customMarker;

        // ── 표시 옵션 ────────────────────────────────────────────
        [Header("표시 옵션")]
        [Tooltip("활성 퀘스트 목표 마커 표시")]
        public bool showQuestMarkers = true;

        [Tooltip("적 아이콘 표시")]
        public bool showEnemies = true;

        [Tooltip("true: 플레이어를 인식한 적만 표시 / false: 모든 적 표시")]
        public bool showOnlyDetectedEnemies = false;

        [Tooltip("NPC 아이콘 표시")]
        public bool showNpcs = true;

        [Tooltip("채집 오브젝트 아이콘 표시")]
        public bool showGathering = true;

        // ── 미니맵 표시 모드 ─────────────────────────────────────
        [Header("표시 모드")]
        [Tooltip("아이콘 전용 모드: 플레이어 중심\n맵 이미지 모드: 촬영된 배경 이미지 위에 아이콘")]
        public MinimapDisplayMode displayMode = MinimapDisplayMode.IconOnly;

        [Header("아이콘 전용 모드 (IconOnly)")]
        [Tooltip("월드 1유닛 = 미니맵 N픽셀 (클수록 확대)")]
        [Range(0.01f, 1f)]
        public float worldToMinimapScale = 0.05f;

        [Tooltip("미니맵 원형 반지름 (픽셀)")]
        [Range(50f, 300f)]
        public float minimapRadius = 100f;

        [Tooltip("플레이어 방향이 항상 위를 향하도록 회전")]
        public bool rotateWithPlayer = true;

        [Header("맵 이미지 모드 (MapImage)")]
        [Tooltip("MinimapCaptureEditor로 촬영한 배경 스프라이트")]
        public Sprite backgroundSprite;

        [Tooltip("캡처 당시 월드 중심 좌표 (XZ 평면)")]
        public Vector2 captureCenter;

        [Tooltip("캡처 범위 (월드 유닛, 정사각형 기준 한 변 길이)")]
        public float captureWorldSize = 200f;

        // ── 좌표 변환 ────────────────────────────────────────────

        /// <summary>
        /// 맵 이미지 모드에서 월드 XZ 좌표를 미니맵 UI 픽셀 좌표로 변환합니다.
        /// </summary>
        public Vector2 WorldToMapImagePos(Vector3 worldPos, float minimapDisplaySize)
        {
            float nx = (worldPos.x - captureCenter.x) / captureWorldSize;
            float ny = (worldPos.z - captureCenter.y) / captureWorldSize;
            return new Vector2(nx * minimapDisplaySize, ny * minimapDisplaySize);
        }

        // ── 아이콘 조회 ──────────────────────────────────────────

        public IconEntry GetActorIconEntry(ActorType actorType)
        {
            if ((actorType & ActorType.Player)  != 0) return player;
            if ((actorType & ActorType.Monster) != 0) return enemy;
            if ((actorType & ActorType.NPC)     != 0) return npc;
            return gathering;
        }
    }

    public enum MinimapDisplayMode
    {
        IconOnly,
        MapImage,
    }
}
