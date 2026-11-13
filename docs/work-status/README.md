# 작업 상태 문서

설계에 따른 구현·검증이 시작된 작업의 상세 상태를 `<작업명>.md`로 관리한다. 설계는 `Assets/docs/design/`, 저작 지침은 `Assets/docs/guide/`에 유지한다. 기존에 다른 위치가 정본인 설계는 복제하지 않고 해당 정본을 연결한다.

각 상태 문서에는 다음을 기록한다.

- 설계 정본과 분야 위키 링크
- 현재 단계·판정 및 반영된 코드·에셋
- 검증 주체·결과·원본 근거 링크
- 미수행·실패·차단 원인, 남은 확인과 다음 행동

설계 문서에서도 해당 상태 문서를 연결한다. 단계 완료 시 상태 문서를 먼저 갱신하고 분야 위키의 현재 판단 요약과 링크를 갱신한다. 완료 후에도 이 폴더에 유지한다. 기존 혼합 문서는 해당 문서를 수정할 때 분리하며 인용 링크와 근거를 보존한다. 단순 문구 수정에는 새 상태 문서를 만들지 않는다.

## 설계 폴더에서 옮겨 온 상태·검증 기록

`Assets/docs/design/`에 섞여 있던 구현 기록·단계 인계·검증 명세를 작업별 하위 폴더로 옮겼다. 설계 정본은 각 폴더의 문서가 연결한 `design/` 문서이며 분류 기준은 [설계 문서 분류](../wiki/design-folder-map.md)에 있다.

| 폴더 | 내용 | 분야 위키 |
|---|---|---|
| [living-world/](living-world/) | 생명의 호수 생활 확장 A~E 인계, 통합(I)·검증(V) | [월드·아트](../wiki/world-art.md) |
| [npc-life-ai/](npc-life-ai/STATUS.md) | NPC 생활 AI 단계 기록과 진행 상태 | [NPC](../wiki/npc.md) |
| [red-tree-blessing/](red-tree-blessing/) | 붉은 나무의 축복 R02~R09 구현 기록 | [콘텐츠](../wiki/content.md) |
| [scenario-gameplay/](scenario-gameplay/) | 시나리오 맵 플레이 개편 인수·추적 | [콘텐츠](../wiki/content.md) |
| [dialogue-presentation/](dialogue-presentation/) | 화린 대화 연출 검수 명세 | [콘텐츠](../wiki/content.md) |
| [pet-system/](pet-system/) | 펫 1·2차 구현과 검수 | [UI·플레이어](../wiki/player-ui.md) |
| [npc-ambient-speech/](npc-ambient-speech/) | NPC 일상 말풍선 사용자 테스트 | [NPC](../wiki/npc.md) |
| [open-world/](open-world/) | 연속 월드 인수·탐험 테스트 명세 | [월드·아트](../wiki/world-art.md) |
| [regional-sky/](regional-sky/) | 지역 하늘 구름 흐름·후속 검증 명세 | [월드·아트](../wiki/world-art.md) |

운영 기준은 [공통 하네스](../../Tools/Harness/README.md), 분야 진입점은 [작업 위키](../wiki/README.md)를 따른다.
