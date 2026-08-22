using System;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.UI;

namespace UPlayGround.Data.UI
{
    [CreateAssetMenu(fileName = "MinimapIconConfig", menuName = "UPlayGround/UI/Minimap Icon Config")]
    public class MinimapIconConfigSO : ScriptableObject
    {
        [Serializable]
        public struct IconEntry
        {
            public Sprite sprite;
            public Color  color;
            [Range(1f, 40f)]
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

        // ── 정적 마커 아이콘 ──────────────────────────────────────
        [Header("정적 마커 아이콘")]
        [Tooltip("마을 입구 / 거점 마커")]
        public IconEntry town;
        [Tooltip("포탈 / 워프 지점 마커")]
        public IconEntry portal;
        [Tooltip("고정 NPC 마커 (액터 시스템과 별개)")]
        public IconEntry staticNpc;
        [Tooltip("MinimapMarkerType.Custom 마커")]
        public IconEntry customMarker;

        // ── 사용자 마커 아이콘 ────────────────────────────────────
        [Header("사용자 마커 아이콘")]
        [Tooltip("플레이어가 맵에 직접 찍는 핀 마커")]
        public IconEntry userMarker;

        [Header("Cycle")]
        public IconEntry remains;
        public IconEntry activeRestPoint;
        public bool showRemainsMarker = true;

        // ── 표시 옵션 ────────────────────────────────────────────
        [Header("표시 옵션 — 퀘스트 / 적")]
        [Tooltip("활성 퀘스트 목표 마커 표시")]
        public bool showQuestMarkers = true;

        [Tooltip("적 아이콘 표시")]
        public bool showEnemies = true;

        [Tooltip("true: 플레이어를 인식한 적만 표시 / false: 모든 적 표시")]
        public bool showOnlyDetectedEnemies = false;

        [Header("표시 옵션 — 액터")]
        [Tooltip("NPC 액터 아이콘 표시 (씬에 배치된 NpcActor)")]
        public bool showNpcs = true;

        [Tooltip("채집 오브젝트 아이콘 표시")]
        public bool showGathering = true;

        [Header("표시 옵션 — 정적 마커")]
        [Tooltip("마을 마커 표시")]
        public bool showTowns = true;
        [Tooltip("포탈 마커 표시")]
        public bool showPortals = true;
        [Tooltip("고정 NPC 마커 표시 (MinimapMarkerType.Npc)")]
        public bool showStaticNpcs = true;
        [Tooltip("사용자가 직접 찍은 마커 표시")]
        public bool showUserMarkers = true;

        // ── 맵 이미지 설정 ───────────────────────────────────────
        [Header("맵 이미지 (MapImage)")]
        [Tooltip("MinimapCaptureEditor로 촬영한 배경 스프라이트")]
        public Sprite backgroundSprite;

        [Tooltip("캡처 당시 월드 중심 좌표 (XZ 평면)")]
        public Vector2 captureCenter;

        [Tooltip("캡처 범위 (월드 유닛, 정사각형 기준 한 변 길이). 기존 Config 호환용입니다.")]
        public float captureWorldSize = 200f;

        [Tooltip("캡처 범위 (월드 유닛, X=가로, Y=세로). 0 이하면 captureWorldSize를 사용합니다.")]
        public Vector2 captureWorldSizeXY;

        [Tooltip("미니맵 이미지 줌 배율. 클수록 플레이어 주변을 확대해서 표시.")]
        [Range(0.5f, 100f)]
        public float mapZoom = 1f;

        [Header("맵 이미지 좌표 보정")]
        [Tooltip("배경 스프라이트가 월드 X축과 좌우 반대로 보일 때 사용")]
        public bool flipX;

        [Tooltip("배경 스프라이트가 월드 Z축과 상하 반대로 보일 때 사용")]
        public bool flipY;

        [Tooltip("배경 스프라이트가 월드 축과 어긋난 경우 중심 기준으로 회전 보정합니다.")]
        public float rotationDegrees;

        // ── 확대 맵 모드 (M키 토글) ──────────────────────────────
        [Header("확대 맵 모드 (M키 토글)")]
        [Tooltip("확대 맵 시 마스크 크기 (픽셀)")]
        [Range(100f, 800f)]
        public float expandedMapSize = 500f;

        [Tooltip("확대 맵 시 이미지 줌 배율")]
        [Range(0.5f, 100f)]
        public float expandedMapZoom = 3f;

        [Tooltip("확대/축소 전환 애니메이션 시간 (초)")]
        [Range(0f, 0.5f)]
        public float expandTransitionDuration = 0.2f;

        // ── 좌표 변환 ────────────────────────────────────────────

        /// <summary>
        /// 월드 XZ 좌표를 미니맵 UI 픽셀 좌표로 변환합니다.
        /// </summary>
        public Vector2 WorldToMapImagePos(Vector3 worldPos, float minimapDisplaySize)
            => WorldToMapImagePos(worldPos, new Vector2(minimapDisplaySize, minimapDisplaySize));

        /// <summary>
        /// 월드 XZ 좌표를 맵 이미지 UI 픽셀 좌표로 변환합니다.
        /// </summary>
        public Vector2 WorldToMapImagePos(Vector3 worldPos, Vector2 mapDisplaySize)
        {
            Vector2 normalized = WorldToNormalizedMapPos(worldPos);
            return Vector2.Scale(normalized, mapDisplaySize);
        }

        /// <summary>
        /// 미니맵 UI 픽셀 좌표를 월드 XZ 좌표로 되돌립니다.
        /// </summary>
        public Vector3 MapImagePosToWorld(Vector2 mapImagePos, float minimapDisplaySize, float worldY = 0f)
            => MapImagePosToWorld(mapImagePos, new Vector2(minimapDisplaySize, minimapDisplaySize), worldY);

        /// <summary>
        /// 맵 이미지 UI 픽셀 좌표를 월드 XZ 좌표로 되돌립니다.
        /// </summary>
        public Vector3 MapImagePosToWorld(Vector2 mapImagePos, Vector2 mapDisplaySize, float worldY = 0f)
        {
            Vector2 normalized = new(
                Mathf.Abs(mapDisplaySize.x) > Mathf.Epsilon ? mapImagePos.x / mapDisplaySize.x : 0f,
                Mathf.Abs(mapDisplaySize.y) > Mathf.Epsilon ? mapImagePos.y / mapDisplaySize.y : 0f);
            Vector2 worldXZ = NormalizedMapPosToWorld(normalized);
            return new Vector3(worldXZ.x, worldY, worldXZ.y);
        }

        public float GetBackgroundAspect()
        {
            if (backgroundSprite == null || backgroundSprite.rect.height <= Mathf.Epsilon)
                return 1f;

            return backgroundSprite.rect.width / backgroundSprite.rect.height;
        }

        public Vector2 GetMapDisplaySizeByHeight(float displayHeight)
            => new(displayHeight * GetBackgroundAspect(), displayHeight);

        private Vector2 WorldToNormalizedMapPos(Vector3 worldPos)
        {
            Vector2 safeWorldSize = GetCaptureWorldSize();
            Vector2 normalized = new(
                (worldPos.x - captureCenter.x) / safeWorldSize.x,
                (worldPos.z - captureCenter.y) / safeWorldSize.y);

            normalized = ApplyAxisFlip(normalized);
            return Rotate(normalized, rotationDegrees);
        }

        private Vector2 NormalizedMapPosToWorld(Vector2 normalized)
        {
            Vector2 safeWorldSize = GetCaptureWorldSize();

            normalized = Rotate(normalized, -rotationDegrees);
            normalized = ApplyAxisFlip(normalized);

            return new Vector2(
                normalized.x * safeWorldSize.x + captureCenter.x,
                normalized.y * safeWorldSize.y + captureCenter.y);
        }

        private Vector2 GetCaptureWorldSize()
        {
            if (Mathf.Abs(captureWorldSizeXY.x) > Mathf.Epsilon &&
                Mathf.Abs(captureWorldSizeXY.y) > Mathf.Epsilon)
            {
                return new Vector2(
                    Mathf.Max(Mathf.Abs(captureWorldSizeXY.x), Mathf.Epsilon),
                    Mathf.Max(Mathf.Abs(captureWorldSizeXY.y), Mathf.Epsilon));
            }

            float size = Mathf.Max(Mathf.Abs(captureWorldSize), Mathf.Epsilon);
            return new Vector2(size, size);
        }

        private Vector2 ApplyAxisFlip(Vector2 value)
        {
            if (flipX) value.x = -value.x;
            if (flipY) value.y = -value.y;
            return value;
        }

        private static Vector2 Rotate(Vector2 value, float degrees)
        {
            if (Mathf.Abs(degrees) <= Mathf.Epsilon)
                return value;

            float radians = degrees * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);

            return new Vector2(
                value.x * cos - value.y * sin,
                value.x * sin + value.y * cos);
        }

        // ── 아이콘 조회 ──────────────────────────────────────────

        public IconEntry GetActorIconEntry(ActorType actorType)
        {
            if ((actorType & ActorType.Player)  != 0) return player;
            if ((actorType & ActorType.Monster) != 0) return enemy;
            if ((actorType & ActorType.NPC)     != 0) return npc;
            return gathering;
        }

        /// <summary>정적 마커 타입에 해당하는 IconEntry를 반환합니다.</summary>
        public IconEntry GetStaticMarkerEntry(MinimapMarkerType type) => type switch
        {
            MinimapMarkerType.Town   => town,
            MinimapMarkerType.Portal => portal,
            MinimapMarkerType.Npc    => staticNpc,
            _                        => customMarker,
        };

        /// <summary>정적 마커 타입의 표시 여부를 반환합니다.</summary>
        public bool IsStaticMarkerVisible(MinimapMarkerType type) => type switch
        {
            MinimapMarkerType.Town   => showTowns,
            MinimapMarkerType.Portal => showPortals,
            MinimapMarkerType.Npc    => showStaticNpcs,
            MinimapMarkerType.Custom => true,
            _                        => false,
        };
    }
}
