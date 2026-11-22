# 실행 환경과 통합

Unity 6000.3.21f1, URP/Core 17.3.0, PC Forward+, RenderGraph, Linear, Render Scale 1, PC RP MSAA 설정 4x. PC RP는 이미 Depth/Opaque Texture를 사용한다. Editor 시작 조사에서 등록된 Console 오류는 0건이었다. 대조 API는 설치본 UniversalRendererRenderGraph.cs와 Core RenderGraphModule이다.

PC_Renderer에는 SSAO, DBuffer Decal, CameraOccluderFade, SpeedRadialBlur, Lake Underwater가 연결되어 있다. 실제 수역의 공기 광학 소유권은 기존 Water Scene Resolve와 조정한다. 전역 Compatibility 설정은 그대로 사용한다.

[수학·통합 작업 상태](TaskStatus.md)
