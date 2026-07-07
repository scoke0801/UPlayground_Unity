using UnityEngine;
using UnityEngine.Rendering;

namespace UPlayGround.Gameplay.World
{
    /// <summary>
    /// 씬별 낮밤 조명 레퍼런스 바인딩 (데이터 전용 — 로직 없음).
    /// WorldLightingManager가 씬 로드 후 이 컴포넌트를 찾아 WorldLightingController에 레퍼런스를 넘긴다.
    /// 이 컴포넌트가 없으면 태양광(RenderSettings.sun/방향광)과 전역 Volume을 자동 검색한다.
    /// </summary>
    [DisallowMultipleComponent]
    public class WorldLightingSceneBinding : MonoBehaviour
    {
        [Tooltip("이 씬에서는 낮밤 조명 제어를 끈다(실내/타이틀 등).")]
        public bool disableWorldLighting = false;

        [Tooltip("태양 방향광. 비우면 RenderSettings.sun → 씬의 방향광 순으로 자동 검색.")]
        public Light sunLight;

        [Tooltip("달 방향광(선택). 밤에만 활성화된다.")]
        public Light moonLight;

        [Tooltip("전역 후처리 Volume(선택). 비우면 씬의 Global Volume을 자동 검색.")]
        public Volume globalVolume;
    }
}
