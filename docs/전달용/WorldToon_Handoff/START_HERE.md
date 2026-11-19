# WorldToon 전달 패키지

**작성일:** 2026-11-16 · **버전:** 1.0 · **언어:** 한국어

## 파일

| 파일 | 용도 |
|---|---|
| `WorldToon_Design.html` | 브라우저에서 읽는 설계서. 목차·표·코드·공식 출처 포함 |
| `WorldToon_Design.md` | 동일 본문의 Markdown. Claude/Codex 작업 컨텍스트용 |
| `IMPLEMENTATION_REQUEST.md` | 구현 에이전트에 전달할 시작 요청문 |
| `ACCEPTANCE_CHECKLIST.md` | 구현 후 상태를 기록할 검증 템플릿 |

## 전달 방법

Claude 또는 Codex에게 `WorldToon_Design.md`와 `IMPLEMENTATION_REQUEST.md`를 함께 제공하고,
실제 Unity 프로젝트 루트에서 시작한다. 구현 요청문을 채팅에 붙여 넣어도 된다.

첫 목표는 **프로젝트 조사(G0) + 작은 Mesh Toon(G1)**이다.
플레이어 lilToon과 원본 자산은 그대로 보호한다.
일반 NPC/Props는 Mesh, 식생은 Foliage, 지형은 Terrain4로 분리한다.
한 에이전트가 구현을 소유하고 다른 에이전트는 변경과 테스트를 검토한다.

## UPlayground 프로젝트 연결 (2026-11-18)

이 패키지는 범용 원문이며 이 저장소에서는 아래 문서와 함께 읽는다. 충돌 시 통합 계약이 우선한다.

- [기존 시스템 통합 계약](../../../Assets/docs/design/world-art/environment/WORLDTOON_EXISTING_INTEGRATION.md): 기존 lilToon·반응형 환경·식생·지형과의 관계, 산출물 경로(`Docs/WorldToon/` 대체), 빌드·승인 규칙 충돌 해소
- [작업 상태](../../work-status/worldtoon.md): G0~G7·T01~T28 진행 기록
- 구현 시작 전 [공통 하네스](../../../Tools/Harness/README.md)와 AGENTS.md의 단계 종료 규칙을 따른다.

## 상태와 제한

이 패키지는 **설계·작업 지시 문서**다. 실행 가능한 Unity 패키지나 완성 셰이더를 포함하지 않는다.
실제 프로젝트 소스에 접속하여 버전·구성·외형·성능을 검증한 결과물은 아니다.
정확한 버전과 렌더링 경로는 G0에서 파일로 확인한다.
성능 개선을 보장하지 않으며, 개선이 없는 대상은 원본을 유지하도록 설계되어 있다.

HTML은 외부 스크립트·폰트·이미지 없이 읽을 수 있다.
문서 끝의 공식 자료 링크를 여는 경우에만 인터넷 접속이 필요하다.
