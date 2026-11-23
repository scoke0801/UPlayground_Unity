# 실행 환경과 통합

Unity 6000.3.21f1, URP/Core 17.3.0, PC Forward+, RenderGraph, Linear, Render Scale 1, PC RP MSAA 설정 4x. PC RP는 이미 Depth/Opaque Texture를 사용한다. Editor 시작 조사에서 등록된 Console 오류는 0건이었다. 대조 API는 설치본 UniversalRendererRenderGraph.cs와 Core RenderGraphModule이다.

PC_Renderer에는 SSAO, DBuffer Decal, CameraOccluderFade, SpeedRadialBlur, Lake Underwater가 연결되어 있다. Toon Fog를 Lake Underwater 앞에 등록한다. 같은 이벤트에서 카메라 상태를 먼저 게시해야 수역 Resolve가 공기 구간에 이 값을 사용할 수 있다. [RenderGraph 패스 공식 문서](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/render-graph-write-render-pass.html) · [깊이에서 월드 위치를 복원하는 공식 예제](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/writing-shaders-urp-reconstruct-world-position.html)

## 적용과 저작

1. [PC Renderer](../../../Assets/Settings/PC_Renderer.asset)의 Toon Fog가 활성인지 확인한다. 기능의 Shader에 Toon Fog Composite가 연결되어 있다.
2. [DroughtVillage](../../../Assets/01.Scenes/Scenario/DroughtVillage.unity)의 루트 `Toon Fog World`가 적용 예다. Base 프로필·품질 에셋·환경 입력을 지정한다. 환경 입력은 기존 날씨 생산자, 플레이어 Transform 서비스와 교전 판정 서비스를 읽는다. 프로필 기준 고도 63m는 마을 진입점 높이를 기준으로 정한 초기값이다.
3. 안개를 사용할 씬의 Lighting에서 Built-in Fog를 끈다. 켜져 있으면 해당 카메라가 중립 경로로 돌아가며 World Inspector에 `LegacyFog`가 표시된다. 씬별 전환을 통해 기존 안개와 합성 소유권을 정한다.
4. 지역을 추가할 때 `ToonFogRegion`에 프로필, 순위, 고유 ID, 외부 전환 거리와 상자 또는 구체를 지정한다. 상자는 전단 없는 변환을, 구체는 균일 스케일을 사용한다. 국소 안개는 `ToonFogLocalVolume`에 상자 또는 타원체, 밀도, 경계 폭, 노이즈와 바람을 지정한다.
5. 카메라별 제외나 기준점 재지정에는 `ToonFogCameraPolicy`를 사용한다. 순간이동 시에는 `ToonFogWorld.ResetTransition()`을 호출할 수 있다. 기본 전환 거리 이상 이동하면 카메라와 기준점 이동을 감지한다.
6. Legacy Lake Water에서 실제 굴절이 필요하면 `captureLegacyRefraction`을 켠다. 관리 수역은 기존 Water Scene Resolve가 처리한다. 임의 투명 셰이더에는 [재질별 계약](MaterialCoverage.md)의 블렌드 모드를 확인한 뒤 공통 어댑터를 연결한다.

## 카메라와 수명

PC 원근 Base Game과 선택한 Scene View를 지원 대상으로 둔다. 거울·Preview·Overlay·직교·XR은 중립 경로를 사용한다. 절대 높이와 색상 거리는 미터를 사용하고, 외부 tint는 Linear RGB로 받는다. Camera Setup 및 Reset은 동일한 RenderGraph에서 패스를 기록하며, 카메라별 LUT와 슬롯은 독립적으로 관리한다.

월드별 적용을 끄려면 `Toon Fog World`의 Fog Enabled를 끈다. Renderer 전체의 적용은 해당 Feature에서 제어한다.

[단계별 상태](TaskStatus.md) · [재현과 비교](Validation.md) · [성능](Performance.md)
